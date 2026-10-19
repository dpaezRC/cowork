# Desarrollo

## Requisitos

- .NET SDK 10.0.
- Docker Desktop con Docker Compose para levantar SQL Server 2022.
- Windows para ejecutar la aplicacion WinForms. El proyecto compila en macOS/Linux con `EnableWindowsTargeting`, pero la ejecucion de la UI requiere Windows.

## Base de datos

La aplicacion usa la base `cowork` en `localhost:1433`, la misma que utiliza la aplicacion Python. No uses la base `iset` ni el esquema anterior.

Para levantar SQL Server y aplicar el esquema:

```bash
docker compose up -d
```

El servicio `db-init` crea la base `cowork` si no existe y aplica `CoworkApp/Scripts/001_Esquema.sql`. El esquema asume una base vacia y no es idempotente: si la base ya contiene las tablas, no vuelvas a ejecutar el script sobre ella.

## Configuracion

1. Copia `CoworkApp/appsettings.example.json` a `CoworkApp/appsettings.json`.
2. Completa la contrasena real de SQL Server en `ConnectionString`.
3. Mantene `Database=cowork`.
4. `SeedDesarrollo` es opt-in:
   - `false`: no se cargan datos demo.
   - `true`: al iniciar la Fase 1 se ejecutara el seeder de desarrollo.

`appsettings.json` esta ignorado por Git y no debe versionarse. No compartas ni publiques la cadena de conexion.

## Seeder de desarrollo

El seeder solo actua si la tabla `dbo.Usuario` esta vacia. En una unica transaccion crea:

- Edificio "Edificio Central" y su piso "Piso 1".
- Oficina "Oficina 101" con capacidad 4, tarifa 1000.00 y horario 08:00-21:00.
- Usuarios demo Administrador, Empleado y Cliente con hashes PBKDF2 compatibles con Python.
- Empleado vinculado al edificio y asignado a la oficina en el horario 08:00-21:00.

Credenciales demo:

| Rol | Email | Contrasena |
| --- | --- | --- |
| Administrador | `admin@cowork.local` | `Admin1234!` |
| Empleado | `empleado@cowork.local` | `Empleado1234!` |
| Cliente | `cliente@cowork.local` | `Cliente1234!` |

Estas credenciales son solo para desarrollo local. No las uses en produccion.

## Fase 1: login y shell

Al iniciar la aplicacion:

1. Carga `appsettings.json` y crea la conexion a la base `cowork`.
2. Si `SeedDesarrollo` es `true`, ejecuta el seeder antes del login. Cuando carga datos demo muestra una unica vez un `MessageBox` con las credenciales de desarrollo; si `dbo.Usuario` ya tiene datos, omite el seeder sin mostrar nada. Si el seeder falla, informa el error y no continua.
3. Muestra el formulario de login. La autenticacion es case-insensitive, verifica el hash PBKDF2 y no distingue un email inexistente de una contrasena incorrecta. Al autenticar, guarda la identidad en memoria (`SesionActual`).
4. Abre el shell de navegacion con los modulos permitidos por rol. Los modulos se muestran como placeholders "En construcción" hasta implementar los CRUD. El boton "Cerrar sesión" vuelve al login; cerrar la ventana sale de la aplicacion.

Los modulos CRUD (Espacios, Personal, Usuarios, Reservas y Control operativo) se implementan en fases posteriores.

La interfaz WinForms requiere Windows para ejecutarse. En macOS/Linux el proyecto compila con `EnableWindowsTargeting`, pero no se puede abrir la UI.

## Comandos

```bash
dotnet restore CoworkApp.sln
dotnet build CoworkApp.sln -c Debug
dotnet test CoworkApp.Tests/CoworkApp.Tests.csproj -c Debug
```
