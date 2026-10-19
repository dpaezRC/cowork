# 04 · Árbol del sistema — CoworkApp

> Documento 5 de 5. Contexto en `00-analisis-funcional.md`, `01-refactor-db.md`,
> `02-historias-de-usuario.md` y `03-orden-de-implementacion.md`.
>
> Árboles ASCII derivados del código real. Se excluyen `bin/`, `obj/`, `.git/` y
> `.docker/data/` (artefactos y volumen de datos).

---

## 1. Vista general

```text
Sistema de gestión de coworking
│
├── CoworkApp                    UI de escritorio WinForms (.NET 10, win-x64)
│   └── depende de ──▶ CoworkApp.Core
│
├── CoworkApp.Core               Lógica de negocio y acceso a datos (net10.0)
│   └── depende de ──▶ Microsoft.Data.SqlClient ──▶ SQL Server 2022
│
└── CoworkApp.Tests              Pruebas unitarias xUnit
    └── depende de ──▶ CoworkApp.Core (con repositorios Fake)

Persistencia : SQL Server 2022 en localhost:1433, base `cowork`
Infraestructura : Docker Compose (sqlserver + db-init)
CI           : GitHub Actions (build + test + publish, windows-latest)
```

---

## 2. Árbol del repositorio

```text
cowork/
├── .docker/
│   └── data/
│       └── sqlserver_dev/                  # volumen persistente de SQL Server (ignorado por git)
│
├── .docs/                                  # análisis funcional y planificación
│   ├── 00-analisis-funcional.md
│   ├── 01-refactor-db.md
│   ├── 02-historias-de-usuario.md
│   ├── 03-orden-de-implementacion.md
│   └── 04-arbol-sistema.md                 # este documento
│
├── .github/
│   └── workflows/
│       └── ci.yml                          # pipeline build/test/publish
│
├── CoworkApp/                              # proyecto WinForms (net10.0-windows)
│   ├── Configuration/
│   │   └── AppConfig.cs                    # carga appsettings.json (ConnectionString, SeedDesarrollo)
│   ├── Data/                               # (vacía — la data vive en Core/Datos)
│   ├── Forms/                              # shell y diálogos modales
│   │   ├── LoginForm.cs                    # autenticación inicial
│   │   ├── MainForm.cs                     # shell: menú lateral + panel de contenido
│   │   ├── EdificioEditarForm.cs           # alta/edición de edificio
│   │   ├── PisoEditarForm.cs               # alta/edición de piso
│   │   ├── OficinaEditarForm.cs            # alta/edición de oficina
│   │   ├── OficinaCascadaForm.cs           # alta en cascada edificio→piso→oficina
│   │   ├── EmpleadoForm.cs                 # alta/edición de empleado
│   │   ├── AsignacionForm.cs               # asignación empleado↔oficina + franja
│   │   ├── UsuarioForm.cs                  # alta/edición de usuario
│   │   └── EntradaTextoForm.cs             # diálogo genérico de un campo de texto
│   ├── Scripts/
│   │   └── 001_Esquema.sql                 # esquema físico completo (11 tablas + 7 índices)
│   ├── Views/                              # módulos CRUD embebidos en el shell
│   │   ├── VistaAsync.cs                   # helper: ejecuta operación async con manejo de errores
│   │   ├── EspaciosView.cs                 # módulo Espacios: tabs Edificios | Pisos | Oficinas
│   │   ├── EdificiosView.cs                # CRUD edificios
│   │   ├── PisosView.cs                    # CRUD pisos
│   │   ├── OficinasView.cs                 # CRUD oficinas
│   │   ├── PersonalView.cs                 # módulo Personal: tabs Empleados | Asignaciones
│   │   ├── EmpleadosView.cs                # CRUD empleados
│   │   ├── AsignacionesView.cs             # CRUD asignaciones empleado↔oficina
│   │   ├── UsuariosView.cs                 # CRUD usuarios
│   │   ├── ReservasView.cs                 # módulo Reservas (cliente/admin)
│   │   ├── ControlOperativoView.cs         # módulo Control operativo (empleado)
│   │   ├── ModuloPendienteControl.cs       # placeholder "En construcción"
│   │   └── ViewModels/                     # (vacía)
│   ├── Program.cs                          # punto de entrada: config → seeder → login → shell
│   ├── CoworkApp.csproj                    # WinExe, net10.0-windows, UseWindowsForms
│   ├── CoworkApp.csproj.user               # preferencias locales del IDE (no relevante en runtime)
│   └── appsettings.example.json            # plantilla de configuración (sin secretos)
│
├── CoworkApp.Core/                         # biblioteca de dominio (net10.0)
│   ├── Datos/                              # acceso a SQL Server
│   │   ├── Db.cs                           # fachada de conexión/consultas
│   │   ├── DbParametros.cs                 # helper de parámetros SQL
│   │   ├── Detalles.cs                     # helpers de lectura de filas
│   │   ├── ITransaccionDb.cs               # contrato de transacción
│   │   ├── IRelojServidor.cs               # abstracción de hora del servidor
│   │   ├── UnicidadSql.cs                  # detección de violaciones de unicidad
│   │   └── ConflictoUnicidadException.cs   # excepción de violación de unicidad
│   ├── Desarrollo/
│   │   ├── DesarrolloSeeder.cs             # datos demo (solo si dbo.Usuario está vacía)
│   │   └── ResultadoDesarrolloSeeder.cs    # resultado + credenciales demo
│   ├── Dominio/
│   │   ├── Modulos.cs                      # enum Modulo + DefinicionModulo
│   │   ├── Roles.cs                        # Administrador | Cliente | Empleado
│   │   └── Estados.cs                      # constantes de estado por entidad
│   ├── Modelos/                            # DTOs / registros de dominio
│   │   ├── Edificio.cs, Piso.cs, Oficina.cs
│   │   ├── Usuario.cs, Empleado.cs, EmpleadoDetalle.cs, EmpleadoOficina.cs
│   │   ├── Reserva.cs, ReservaDetalle.cs, ReservaActiva.cs, ReservaUso.cs
│   │   ├── FranjaCobertura.cs, FilaAgenda.cs, OficinaDisponible.cs
│   │   ├── Pago.cs, Deuda.cs, Auditoria.cs
│   │   ├── AsignacionDetalle.cs, OficinaDetalle.cs, PisoDetalle.cs
│   │   ├── ResultadoIngreso.cs, ResultadoSalida.cs, ResultadoPago.cs
│   │   └── CascadaDatos.cs
│   ├── Repositorios/                       # interfaz + implementación SQL por agregado
│   │   ├── IEdificioRepositorio.cs   / EdificioRepositorio.cs
│   │   ├── IPisoRepositorio.cs        / PisoRepositorio.cs
│   │   ├── IOficinaRepositorio.cs     / OficinaRepositorio.cs
│   │   ├── IUsuarioRepositorio.cs     / UsuarioRepositorio.cs
│   │   ├── IEmpleadoRepositorio.cs    / EmpleadoRepositorio.cs
│   │   ├── IEmpleadoOficinaRepositorio.cs / EmpleadoOficinaRepositorio.cs
│   │   ├── IReservaRepositorio.cs     / ReservaRepositorio.cs
│   │   ├── IReservaUsoRepositorio.cs  / ReservaUsoRepositorio.cs
│   │   ├── IPagoRepositorio.cs        / PagoRepositorio.cs
│   │   ├── IDeudaRepositorio.cs       / DeudaRepositorio.cs
│   │   └── IAuditoriaRepositorio.cs   / AuditoriaRepositorio.cs
│   ├── Seguridad/
│   │   ├── PasswordHasher.cs               # hash PBKDF2 compatible con Python
│   │   └── SesionActual.cs                 # identidad en memoria del usuario logueado
│   ├── Servicios/                          # reglas de negocio (orquestan repositorios)
│   │   ├── AutenticacionServicio.cs
│   │   ├── MenuServicio.cs                 # módulos permitidos por rol
│   │   ├── EdificioServicio.cs
│   │   ├── PisoServicio.cs
│   │   ├── OficinaServicio.cs
│   │   ├── CascadaServicio.cs              # alta edificio→piso→oficina
│   │   ├── UsuarioServicio.cs
│   │   ├── EmpleadoServicio.cs
│   │   ├── AsignacionServicio.cs
│   │   ├── ReservaServicio.cs
│   │   └── ControlOperativoServicio.cs     # ingreso/salida/pago/incidencias
│   └── Validacion/
│       ├── Validador.cs                    # validaciones de entrada
│       └── ReglaNegocioException.cs        # excepción de regla de negocio
│
├── CoworkApp.Tests/                        # pruebas unitarias (xUnit)
│   ├── Datos/
│   │   ├── DbParametrosTests.cs
│   │   └── DetallesTests.cs
│   ├── Fakes/                              # dobles de prueba de repositorios/transacción
│   │   ├── EdificioRepositorioFake.cs
│   │   ├── PisoRepositorioFake.cs
│   │   ├── OficinaRepositorioFake.cs
│   │   ├── UsuarioRepositorioFake.cs
│   │   ├── EmpleadoRepositorioFake.cs
│   │   ├── EmpleadoOficinaRepositorioFake.cs
│   │   ├── ReservaRepositorioFake.cs
│   │   ├── ReservaUsoRepositorioFake.cs
│   │   ├── PagoRepositorioFake.cs
│   │   ├── DeudaRepositorioFake.cs
│   │   ├── AuditoriaRepositorioFake.cs
│   │   ├── TransaccionDbFake.cs
│   │   └── RelojServidorFake.cs
│   ├── Seguridad/
│   │   ├── PasswordHasherTests.cs
│   │   └── SesionActualTests.cs
│   ├── Servicios/
│   │   ├── AutenticacionServicioTests.cs
│   │   ├── MenuServicioTests.cs
│   │   ├── EdificioServicioTests.cs
│   │   ├── PisoServicioTests.cs
│   │   ├── OficinaServicioTests.cs
│   │   ├── CascadaServicioTests.cs
│   │   ├── UsuarioServicioTests.cs
│   │   ├── EmpleadoServicioTests.cs
│   │   ├── AsignacionServicioTests.cs
│   │   ├── ReservaServicioTests.cs
│   │   └── ControlOperativoServicioTests.cs
│   ├── Validacion/
│   │   └── ValidadorTests.cs
│   └── CoworkApp.Tests.csproj
│
├── CoworkApp.sln                           # solución: 3 proyectos
├── docker-compose.yml                      # SQL Server 2022 + db-init
├── .gitignore
├── LICENSE
└── docs/
    └── desarrollo.md                       # guía de desarrollo y comandos
```

---

## 3. Proyectos y dependencias

```text
CoworkApp.sln
│
├── CoworkApp (WinExe)                        net10.0-windows · UseWindowsForms · win-x64
│   ├── ProjectReference ──▶ CoworkApp.Core
│   └── Content ──▶ appsettings.example.json · appsettings.json (si existe)
│
├── CoworkApp.Core (Library)                  net10.0
│   └── PackageReference ──▶ Microsoft.Data.SqlClient 5.2.2
│
└── CoworkApp.Tests (xUnit)                   net10.0 · IsTestProject
    ├── ProjectReference ──▶ CoworkApp.Core
    ├── PackageReference ──▶ Microsoft.NET.Test.Sdk 18.10.1
    ├── PackageReference ──▶ xunit 2.9.3
    └── PackageReference ──▶ xunit.runner.visualstudio 3.1.5

Grafo de dependencias
  CoworkApp ──▶ CoworkApp.Core ──▶ Microsoft.Data.SqlClient ──▶ SQL Server
  CoworkApp.Tests ──▶ CoworkApp.Core   (los Tests NO referencian CoworkApp)
```

---

## 4. Arquitectura por capas

```text
┌──────────────────────────────────────────────────────────────────────┐
│  Punto de entrada — Program.cs (STAThread)                            │
│    │                                                                  │
│    ├─▶ Configuration/AppConfig.Cargar()   appsettings.json            │
│    │        └─ ConnectionString · SeedDesarrollo                      │
│    │                                                                  │
│    ├─▶ Datos/Db(connectionString)          fachada SQL Server         │
│    │                                                                  │
│    ├─▶ [si SeedDesarrollo] Desarrollo/DesarrolloSeeder                │
│    │        └─ siembra demo solo si dbo.Usuario está vacía            │
│    │                                                                  │
│    └─▶ bucle de sesión                                                │
│         ├─ LoginForm  ──▶ Servicios/AutenticacionServicio ──▶ Usuario │
│         │                   └─ Seguridad/PasswordHasher               │
│         │                   └─ Seguridad/SesionActual (identidad)     │
│         └─ MainForm   ──▶ Servicios/MenuServicio (módulos por rol)    │
│              └─ panel de contenido ──▶ Views/<Modulo>View             │
└──────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌──────────────────────────────────────────────────────────────────────┐
│  Capa de UI      Forms/ (shell + diálogos)   Views/ (módulos CRUD)    │
│                  helper: Views/VistaAsync                             │
└──────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌──────────────────────────────────────────────────────────────────────┐
│  Capa de negocio Servicios/  (validan con Validacion/, orquestan)     │
│                  Dominio/ (Modulos, Roles, Estados)                   │
│                  Modelos/ (DTOs)                                      │
└──────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌──────────────────────────────────────────────────────────────────────┐
│  Capa de datos   Repositorios/ (interfaz + impl SQL)                  │
│                  Datos/ (Db, DbParametros, Detalles, transacción)     │
│                  Desarrollo/ (seeder)                                 │
└──────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
                        SQL Server 2022 · base `cowork`
```

---

## 5. Módulos de UI y control de acceso

```text
MainForm
│  (menú lateral filtrado por MenuServicio.ObtenerModulos(rol))
│
├── Módulo Espacios ─────────────── roles: Administrador
│   └── EspaciosView
│       ├── Tab "Edificios"  ──▶ EdificiosView  ──▶ EdificioEditarForm
│       ├── Tab "Pisos"      ──▶ PisosView      ──▶ PisoEditarForm
│       └── Tab "Oficinas"   ──▶ OficinasView   ──▶ OficinaEditarForm
│                                                 └─ OficinaCascadaForm
│
├── Módulo Personal ─────────────── roles: Administrador
│   └── PersonalView
│       ├── Tab "Empleados"     ──▶ EmpleadosView     ──▶ EmpleadoForm
│       └── Tab "Asignaciones"  ──▶ AsignacionesView  ──▶ AsignacionForm
│
├── Módulo Usuarios ────────────── roles: Administrador
│   └── UsuariosView ──▶ UsuarioForm
│
├── Módulo Reservas ────────────── roles: Administrador · Cliente
│   └── ReservasView
│
└── Módulo ControlOperativo ────── roles: Empleado
    └── ControlOperativoView ──▶ EntradaTextoForm (pago · referencia · horarios)

Si un módulo no está implementado o el rol no accede:
  ModuloPendienteControl  ·  MainForm.MostrarMensaje(...)
```

---

## 6. Dominio y base de datos

```text
Jerarquía física del inventario
  Edificio ──< Piso ──< Oficina ──< EmpleadoOficina >── Empleado
                                        (franja horaria: cobertura y alcance)

Personas y autenticación
  Usuario ──1:1── Empleado
     │                │
     │                └──> pertenece a un Edificio (FK)
     └── Rol ∈ {Administrador, Cliente, Empleado}

Operación de reservas
  Usuario ──< Reserva >── Oficina
                 │
                 ├──1:1── ReservaUso   (ingreso/salida/incidencias · empleado)
                 ├──1:1── Pago         (importe · empleado que confirma)
                 └──1:1── Deuda        (importe exigible · estado)

Auditoría transversal
  Auditoria >── Usuario (nullable: NULL = acción del sistema)
      Accion · Entidad · IdEntidad (referencia polimórfica, sin FK)

Estados por entidad (Dominio/Estados.cs)
  Edificio    : Activo · Inactivo
  Piso        : Activo · Inactivo
  Oficina     : Habilitada · Deshabilitada · Baja
  Usuario     : Activo · Inactivo
  Empleado    : Activo · Inactivo
  Reserva     : Confirmada · EnCurso · Finalizada · Cancelada · NoPresentada
  EstadoPago  : Pendiente · Pagado · DeudaPendiente · SinCargo
  Deuda       : Pendiente · Anulada · Pagada

Tablas SQL (CoworkApp/Scripts/001_Esquema.sql)
  dbo.Edificio · dbo.Piso · dbo.Oficina · dbo.Usuario · dbo.Empleado
  dbo.EmpleadoOficina · dbo.Reserva · dbo.ReservaUso · dbo.Pago
  dbo.Deuda · dbo.Auditoria

Índices
  IX_Reserva_Disponibilidad (IdOficina, Fecha, HoraInicio, HoraFin)
  IX_Reserva_Usuario (IdUsuario, Fecha)
  IX_Reserva_Agenda (Fecha, Estado)
  IX_EmpleadoOficina_Cobertura (IdOficina, HoraInicio, HoraFin)
  IX_Deuda_Estado (Estado)
  IX_Auditoria_Fecha (FechaHora)
  IX_Auditoria_Entidad (Entidad, IdEntidad)
```

---

## 7. Árbol de pruebas

```text
CoworkApp.Tests
│
├── Modelo de prueba: dobles en Fakes/ (sin base de datos real)
│   └── repositorios Fake + TransaccionDbFake + RelojServidorFake
│
├── Datos/         DbParametrosTests · DetallesTests
├── Seguridad/     PasswordHasherTests · SesionActualTests
├── Validacion/    ValidadorTests
│
└── Servicios/     1 test file por servicio
    AutenticacionServicioTests · MenuServicioTests · EdificioServicioTests
    PisoServicioTests · OficinaServicioTests · CascadaServicioTests
    UsuarioServicioTests · EmpleadoServicioTests · AsignacionServicioTests
    ReservaServicioTests · ControlOperativoServicioTests

Ejecución: dotnet test CoworkApp.Tests/CoworkApp.Tests.csproj -c Debug
```

---

## 8. Infraestructura, CI y despliegue

```text
docker-compose.yml
│
├── servicio sqlserver
│   ├── imagen: mcr.microsoft.com/mssql/server:2022-latest (linux/amd64)
│   ├── puerto: 1433:1433
│   ├── volumen: ./.docker/data/sqlserver_dev ──▶ /var/opt/mssql
│   └── healthcheck: sqlcmd "SELECT 1"
│
└── servicio db-init (depende de sqlserver healthy)
    ├── monta ./CoworkApp/Scripts ──▶ /scripts (ro)
    ├── crea la base `cowork` si no existe
    └── aplica 001_Esquema.sql (no idempotente sobre base no vacía)

.github/workflows/ci.yml  (push/PR sobre main y dev)
│
├── runs-on: windows-latest
├── setup .NET 10.0.x (cache por *.csproj)
├── dotnet restore
├── dotnet build --no-restore -c Release
├── [push] dotnet publish CoworkApp -c Release -r win-x64
│         --self-contained true · PublishSingleFile true · PublishTrimmed false
└── [push] upload-artifact CoworkApp-win-x64-<rama> (retención 14 días)

Configuración local (no versionada)
  CoworkApp/appsettings.example.json ──▶ CoworkApp/appsettings.json
    ConnectionString: Server=localhost,1433;Database=cowork;...
    SeedDesarrollo  : false | true
```

---

## 9. Flujo de ejecución

```text
Program.Main()
│
├─ ApplicationConfiguration.Initialize()
│
├─ AppConfig.Cargar()
│   ├─ falta appsettings.json ──▶ MessageBox "Configuracion faltante" ──▶ return
│   └─ JSON inválido / sin ConnectionString ──▶ MessageBox "Configuracion invalida" ──▶ return
│
├─ new Db(connectionString)
│
├─ si SeedDesarrollo ──▶ DesarrolloSeeder.EjecutarAsync()
│   ├─ dbo.Usuario vacía  ──▶ crea edificio/piso/oficina/usuarios demo + empleado asignado
│   │                          └─ MessageBox con credenciales demo
│   ├─ dbo.Usuario con datos ──▶ omite (no muestra nada)
│   └─ error ──▶ MessageBox "Error del seeder" ──▶ return
│
└─ bucle de sesión
    │
    ├─ new SesionActual()
    ├─ new AutenticacionServicio(new UsuarioRepositorio(db))
    │
    └─ while (true)
        ├─ LoginForm.ShowDialog()
        │   ├─ != OK ──▶ return (salir)
        │   └─ OK ──▶ SesionActual guarda identidad (Nombre, Rol)
        │
        ├─ MainForm.ShowDialog()
        │   ├─ ConstruirControles()  (encabezado + menú lateral + contenido)
        │   ├─ CargarModulos()       MenuServicio.ObtenerModulos(rol)
        │   └─ MostrarModulo(def)    valida acceso ──▶ View del módulo
        │
        ├─ si NO CerrarSesionSolicitado ──▶ return (salir)
        │
        └─ SesionActual.Cerrar()  ──▶ vuelve al LoginForm

Operación típica dentro de un módulo (patrón CRUD)
  Vista del módulo
    ├─ Servicio.Validar / reglas de negocio (Validacion/)
    ├─ Repositorio → Db → SQL Server
    ├─ VistaAsync.EjecutarAsync(contenedor, controles, operacion, mensaje)
    │   ├─ ReglaNegocioException ──▶ "Operacion no permitida"
    │   └─ Exception genérica  ──▶ "Error"
    └─ AuditoriaRepositorio registra la acción (dbo.Auditoria)
```
