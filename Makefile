DOTNET      ?= dotnet
CONFIG      ?= Debug
RID         ?= win-x64
PUBLISH_DIR ?= publish
SOLUTION    := CoworkApp.sln
APP_PROJECT := CoworkApp/CoworkApp.csproj
TESTS       := CoworkApp.Tests/CoworkApp.Tests.csproj
RM          ?= rm -rf

.DEFAULT_GOAL := help
.PHONY: help config restore build test run publish clean db-up db-init db-reset db-down db-logs setup

help:
	@echo "CoworkApp - comandos disponibles:"
	@echo ""
	@echo "  make config     Copia appsettings.example.json a appsettings.json (si falta)"
	@echo "  make restore    Restaura las dependencias NuGet"
	@echo "  make build      Compila la solucion (CONFIG=$(CONFIG))"
	@echo "  make test       Ejecuta los tests xUnit"
	@echo "  make run        Ejecuta la app (solo Windows)"
	@echo "  make publish    Publica un .exe self-contained en $(PUBLISH_DIR)/ (RID=$(RID))"
	@echo "  make clean      Limpia bin/obj y $(PUBLISH_DIR)/"
	@echo ""
	@echo "  make setup      config + restore + db-up + db-init"
	@echo "  make db-up      Levanta SQL Server con Docker"
	@echo "  make db-init    Crea la base cowork y aplica 001_Esquema.sql si falta"
	@echo "  make db-reset   Borra la base y vuelve a aplicar el esquema (destructivo)"
	@echo "  make db-logs    Sigue los logs de SQL Server"
	@echo "  make db-down    Detiene los contenedores"
	@echo ""
	@echo "Variables: DOTNET, CONFIG, RID, PUBLISH_DIR"

config:
	@if [ -f CoworkApp/appsettings.json ]; then \
		echo "CoworkApp/appsettings.json ya existe; no se modifica."; \
	else \
		cp CoworkApp/appsettings.example.json CoworkApp/appsettings.json; \
		echo "Creado CoworkApp/appsettings.json."; \
		echo "Editalo y complete la contrasena de SQL Server antes de ejecutar la app."; \
	fi

restore:
	$(DOTNET) restore $(SOLUTION)

build:
	$(DOTNET) build $(SOLUTION) -c $(CONFIG)

test:
	$(DOTNET) test $(TESTS) -c $(CONFIG)

run:
ifeq ($(OS),Windows_NT)
	$(DOTNET) run --project $(APP_PROJECT) -c $(CONFIG)
else
	@echo "La interfaz WinForms solo se ejecuta en Windows."
	@echo "En macOS/Linux podes compilar y publicar con 'make publish'."
	@echo "Despues copia $(PUBLISH_DIR)/ a Windows y ejecuta CoworkApp.exe."
	@exit 1
endif

publish:
	@if [ ! -f CoworkApp/appsettings.json ]; then \
		echo "Falta CoworkApp/appsettings.json."; \
		echo "Ejecuta 'make config' y completa la cadena de conexion antes de publicar."; \
		exit 1; \
	fi
	$(DOTNET) publish $(APP_PROJECT) -c Release -r $(RID) --self-contained true \
		-p:PublishSingleFile=true \
		-p:PublishTrimmed=false \
		-o $(PUBLISH_DIR)
	@echo "Publicado en $(PUBLISH_DIR)/CoworkApp.exe"
	@if [ -f $(PUBLISH_DIR)/appsettings.json ]; then \
		echo "Configuracion incluida: $(PUBLISH_DIR)/appsettings.json"; \
	else \
		echo "Advertencia: no se copio appsettings.json a $(PUBLISH_DIR)/."; \
	fi

clean:
	$(DOTNET) clean $(SOLUTION) -c $(CONFIG)
	$(RM) $(PUBLISH_DIR)

db-up:
	docker compose up -d sqlserver

db-init:
	@docker compose up -d sqlserver
	@PASSWORD=$$(docker compose exec -T sqlserver printenv MSSQL_SA_PASSWORD 2>/dev/null); \
	if docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$$PASSWORD" -C -d cowork -Q "SET NOCOUNT ON; SELECT 1 FROM sys.tables WHERE name = 'Usuario'" 2>/dev/null | grep -q 1; then \
		echo "El esquema ya esta aplicado; no se hace nada."; \
	else \
		echo "Aplicando 001_Esquema.sql sobre la base cowork..."; \
		docker compose run --rm db-init; \
	fi

db-reset:
	@printf "Esto elimina la base cowork y todos sus datos. Escribi 'si' para continuar: "; \
	read ok; \
	if [ "$$ok" != "si" ]; then echo "Cancelado."; exit 1; fi
	docker compose down
	$(RM) .docker/data/sqlserver_dev
	@$(MAKE) db-init

db-down:
	docker compose down

db-logs:
	docker compose logs -f sqlserver

setup:
	@$(MAKE) config
	@$(MAKE) restore
	@$(MAKE) db-up
	@$(MAKE) db-init
	@echo ""
	@echo "Entorno listo. Edita CoworkApp/appsettings.json y luego ejecuta:"
	@echo "  make run        (Windows)"
	@echo "  make publish    (genera el .exe para Windows)"
