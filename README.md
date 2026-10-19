# CoworkApp

Aplicacion de escritorio para la gestion de espacios de coworking. La UI esta hecha con **WinForms sobre .NET 10** y la persistencia usa **SQL Server 2022**.

## Stack

- .NET 10 (`net10.0-windows` para la app, `net10.0` para Core y Tests)
- WinForms
- SQL Server 2022 en Docker
- xUnit para tests

## Requisitos

- **Windows 10/11** para ejecutar la aplicacion. WinForms no corre en macOS/Linux.
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) con Docker Compose
- GNU Make:
  - Windows: usar Git Bash o WSL. Tambien se puede instalar con `choco install make` o `scoop install make`.
  - macOS/Linux: normalmente ya viene instalado; si no, `brew install make` o el gestor de paquetes de la distro.

> En macOS/Linux se puede compilar, testear y publicar el `.exe` para Windows, pero la interfaz no se puede abrir.

## Inicio rapido

```bash
make setup
```

Esto crea `CoworkApp/appsettings.json`, restaura dependencias, levanta SQL Server y aplica el esquema.

Despues:

1. Editar `CoworkApp/appsettings.json` y completar la contrasena de SQL Server.
2. En Windows, ejecutar la app:

```bash
make run
```

3. Para generar un ejecutable standalone:

```bash
make publish
```

El resultado queda en `publish/CoworkApp.exe`.

## Configuracion

La app lee `appsettings.json` desde su directorio de salida. El archivo no se versiona porque contiene credenciales.

Para crearlo desde la plantilla:

```bash
make config
```

Contenido de `CoworkApp/appsettings.json`:

```json
{
  "ConnectionString": "Server=localhost,1433;Database=cowork;User Id=sa;Password=CAMBIAR_ESTA_CONTRASENA;Encrypt=False;TrustServerCertificate=True",
  "SeedDesarrollo": false
}
```

### Cadena de conexion

- `Server`: host y puerto de SQL Server. Con Docker en la misma maquina, `localhost,1433`.
- `Database`: debe ser `cowork`.
- `User Id` / `Password`: credenciales de SQL Server.
- `Encrypt=False;TrustServerCertificate=True`: necesario para el certificado autofirmado del contenedor local.

La contrasena debe coincidir con `MSSQL_SA_PASSWORD` en `docker-compose.yml` (por defecto `yourStrong#Password`). Si cambias una, cambia la otra.

### Seed de desarrollo

- `false`: no carga datos demo.
- `true`: al iniciar, si la tabla `dbo.Usuario` esta vacia, carga edificio, piso, oficina y usuarios de prueba.

Credenciales demo:

| Rol | Email | Contrasena |
| --- | --- | --- |
| Administrador | `admin@cowork.local` | `Admin1234!` |
| Empleado | `empleado@cowork.local` | `Empleado1234!` |
| Cliente | `cliente@cowork.local` | `Cliente1234!` |

Estas credenciales son solo para desarrollo local.

> No compartas ni commitees `appsettings.json`.

## Base de datos

La app usa la base `cowork` en SQL Server 2022. El esquema esta en `CoworkApp/Scripts/001_Esquema.sql`.

### Crear la base y aplicar el esquema

```bash
make db-up
make db-init
```

- `db-up` levanta solo el contenedor de SQL Server.
- `db-init` crea la base `cowork` si no existe y aplica el esquema. Si el esquema ya fue aplicado, no hace nada.

### Reiniciar la base desde cero

```bash
make db-reset
```

Pide confirmacion y **elimina todos los datos** de `.docker/data/sqlserver_dev`.

### Detener y ver logs

```bash
make db-down
make db-logs
```

### Nota sobre el esquema

`001_Esquema.sql` asume una base vacia y no es idempotente. Ejecutar `docker compose up -d` sin filtros vuelve a lanzar el servicio `db-init` y puede fallar si las tablas ya existen. Por eso se recomienda usar los comandos de Make (`make db-up`, `make db-init`) en lugar de `docker compose up -d` directamente.

## Comandos del Makefile

| Comando | Descripcion |
| --- | --- |
| `make help` | Muestra la ayuda (target por defecto) |
| `make config` | Crea `CoworkApp/appsettings.json` desde el ejemplo si falta |
| `make restore` | Restaura paquetes NuGet |
| `make build` | Compila la solucion en `CONFIG` (por defecto `Debug`) |
| `make test` | Ejecuta los tests |
| `make run` | Ejecuta la app en Windows |
| `make publish` | Publica un `.exe` self-contained `win-x64` en `publish/` |
| `make clean` | Limpia `bin/obj` y `publish/` |
| `make setup` | `config` + `restore` + `db-up` + `db-init` |
| `make db-up` | Levanta SQL Server |
| `make db-init` | Crea/aplica el esquema si falta |
| `make db-reset` | Borra la base y la recrea (destructivo) |
| `make db-down` | Detiene los contenedores |
| `make db-logs` | Sigue los logs de SQL Server |

Variables disponibles:

```bash
make build CONFIG=Release
make publish RID=win-x64 PUBLISH_DIR=dist
```

## Ejecutar como programa de escritorio

### En Windows

```bash
make config
make run
```

`make run` compila y abre la UI con `dotnet run`. Requiere el SDK de .NET instalado.

### Generar el ejecutable

```bash
make publish
```

Se genera `publish/CoworkApp.exe` como archivo unico y self-contained: en Windows no hace falta instalar .NET. La carpeta `publish/` incluye `appsettings.json`; revisa que la cadena de conexion sea la correcta para la maquina donde vas a ejecutar el programa.

Pasos para distribuir:

1. Ejecutar `make publish`.
2. Copiar la carpeta `publish/` a la maquina Windows.
3. Verificar/editar `publish/appsettings.json` si cambia el host de la base.
4. Ejecutar `CoworkApp.exe`.

### Publicar desde macOS/Linux para ejecutar en Windows

```bash
make publish
```

El proyecto tiene `EnableWindowsTargeting`, asi que la publicacion cruzada funciona. Luego copia `publish/` a Windows y ejecuta el `.exe`.

Si SQL Server corre en Docker sobre macOS y el `.exe` se ejecuta en una VM Windows, en `appsettings.json` usa la IP LAN del Mac (por ejemplo `Server=192.168.1.50,1433`) en lugar de `localhost`.

## Tests

```bash
make test
```

O directamente:

```bash
dotnet test CoworkApp.Tests/CoworkApp.Tests.csproj
```

## Estructura

```
CoworkApp/          UI WinForms y scripts SQL
CoworkApp.Core/     Dominio, repositorios, servicios y acceso a datos
CoworkApp.Tests/    Tests xUnit
docs/               Documentacion de desarrollo
.docs/              Analisis funcional e historias de usuario
docker-compose.yml  SQL Server + inicializacion del esquema
Makefile            Comandos de build, ejecucion y base de datos
```

## Problemas comunes

**La app dice que falta `appsettings.json`**

Ejecuta `make config` y edita `CoworkApp/appsettings.json`.

**No puede conectar a SQL Server**

- Verifica que el contenedor este arriba: `docker compose ps`.
- Revisa que la contrasena de `appsettings.json` coincida con `docker-compose.yml`.
- Confirma el puerto `1433`.
- En Docker Desktop, espera a que el contenedor este `healthy`.

**Error "There is already an object named ..."**

El esquema ya fue aplicado. Usa una base limpia con `make db-reset` (borra los datos existentes).

**`make` no se encuentra en Windows**

Instala Make (`choco install make` o `scoop install make`) y ejecuta los comandos desde Git Bash o WSL.

**`make run` falla en macOS/Linux**

Es esperado: WinForms solo se ejecuta en Windows. Usa `make publish` y corre el `.exe` en Windows.

**SQL Server no arranca en Apple Silicon**

`docker-compose.yml` usa `platform: linux/amd64`, por lo que Docker puede necesitar Rosetta y el arranque puede ser mas lento.

## Documentacion adicional

- `docs/desarrollo.md`: flujo de desarrollo, fases y seeder.
- `.docs/`: analisis funcional, historias de usuario y orden de implementacion.
