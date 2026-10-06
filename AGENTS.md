# AGENTS.md

Convenciones de código para CoworkApp. **Este documento manda sobre
`docs/03-orden-de-implementacion.md` en cuanto a estructura de carpetas** — ver
§1.1.

---

## 1. Contexto

App Windows de escritorio para gestionar un negocio de coworking: edificios con
pisos y oficinas, turnos con N asientos, auditoría de quién llegó y facturación.

| Ítem | Valor |
| --- | --- |
| Framework | `net10.0-windows`, WinForms |
| Base de datos | SQL Server, base `iset` |
| Estado | WinForms `net10.0-windows`, .NET 10 |
| Acceso a datos | Dapper sobre `Microsoft.Data.SqlClient` |
| Tests | xUnit + Shouldly |

Los cuatro documentos de dominio viven en `docs/` y **son la fuente de verdad del
*qué***; este archivo dice el *cómo*:

| Doc | Qué define |
| --- | --- |
| `docs/00-analisis-funcional.md` | Requisitos `R#`, modelo de datos, decisiones de diseño (§6) |
| `docs/01-refactor-db.md` | Tickets de base de datos `REF-##`, invariantes, stored procedures |
| `docs/02-historias-de-usuario.md` | Historias `US-##` y su mapa de epics |
| `docs/03-orden-de-implementacion.md` | Orden de avance Fase 0 → Fase 7 |

**No copies SQL, fórmulas ni invariantes de este archivo.** Referenciá el ticket.

### 1.1 Fuentes de conflicto

`docs/03-orden-de-implementacion.md:58` propone carpetas por capa técnica
(`Data/`, `Models/`, `Repositories/`, `Services/`, `Forms/`). **Eso está
superado.** El backlog está organizado en epics y la estructura de §2 sigue a
los epics, no a las capas. El orden de trabajo de ese doc sigue valiendo; la
estructura de carpetas no.

---

## 2. Estructura

Una feature = un slice vertical = una historia. Todo lo que una historia
necesita vive junto, no repartido en capas.

```
CoworkApp/
├── Program.cs                       ← ÚNICO punto donde se hace `new` del grafo
├── appsettings.json                 ← gitignored (connection string)
├── Shared/
│   ├── Data/
│   │   ├── Db.cs                    ← apertura de conexión, Dapper
│   │   └── SqlError.cs              ← traduce THROW 5xxxx → excepción de dominio
│   ├── Security/
│   │   ├── Pbkdf2PasswordHasher.cs
│   │   └── CurrentUser.cs           ← sesión del usuario logueado (no estático)
│   ├── Configuration/
│   │   └── DatabaseOptions.cs
│   └── Results/                     ← Result<T> para errores de negocio
│
├── Features/
│   ├── A/EdificiosOficinas/
│   │   ├── AltaEdificio/            US-01
│   │   ├── GestionarPisos/          US-02
│   │   ├── AltaOficina/             US-03
│   │   └── ListarOficinas/          US-04   ← hoy vive en Form1.cs:33
│   ├── B/Horarios/
│   │   ├── HorarioSemana/           US-05
│   │   ├── Excepciones/             US-06
│   │   └── ConsultarHorario/        US-07
│   ├── C/Administracion/
│   │   ├── AsignarAdministradores/  US-08
│   │   └── VerMisOficinas/          US-09
│   ├── D/Turnos/
│   │   ├── Disponibilidad/          US-10
│   │   ├── SolicitarTurno/          US-11
│   │   ├── PrecioEstimado/          US-12
│   │   └── CancelarTurno/           US-13
│   ├── E/Auditoria/
│   │   ├── TurnosDelDia/            US-14
│   │   ├── AprobarTurno/            US-15
│   │   └── RechazarTurno/           US-16
│   ├── F/Sobrecupo/
│   │   ├── RegistrarLlegadas/       US-17
│   │   ├── DecidirExcedente/        US-18
│   │   ├── RechazarPorCapacidad/    US-19
│   │   ├── ConteoEnVivo/            US-20
│   │   └── CerrarTurno/             US-21
│   ├── G/Facturacion/
│   │   ├── GenerarFactura/          US-22
│   │   ├── VerFactura/              US-23
│   │   └── GestionarFacturas/       US-24
│   └── H/
│       ├── Login/                   US-25
│       └── Dashboard/               US-26
│
├── Views/
│   ├── Forms/                        ← WinForms + .Designer.cs
│   └── ViewModels/                   ← CommunityToolkit.Mvvm
├── Controls/                         ← UserControls reutilizables
└── Binders/                          ← IValueConverter
```

### 2.1 Anatomía de un slice

Cuatro roles de archivo. Juntá los que se usan juntos.

| Rol | Sufijo | Contenido |
| --- | --- | --- |
| Entrada | `*.Request.cs` | `record` inmutable, el contrato de entrada |
| Salida | `*.Row.cs` | `record` de lectura — lo que ve la UI |
| Lógica | `*.Handler.cs` | valida, consulta, aplica reglas de negocio |
| Validación | `*.Validator.cs` | FluentValidation |

Un `SELECT` corto con una fila simple puede ser `Handler` + `Row` en dos
archivos. **Partí el slice en más archivos cuando el handler pase de ~80
líneas o cuando haya más de un flujo** (reservar y cancelar no van juntos).

En `F/Sobrecupo/` cada historia es su propio slice porque son cinco decisiones
distintas con cinco AC distintos.

### 2.2 Formularios

Los `Form` **no** contienen lógica de negocio. Un Form hace tres cosas:

1. arma el `Request` desde los controles,
2. lo manda al `Handler`,
3. pinta el `Row` o el error.

Si un método de Form hace una consulta, un cálculo o una validación de dominio,
se mueve a un slice.

---

## 3. Reglas

**Dependencia:** `Views → Features → Shared`. Nunca al revés, nunca entre
slices de epic distinto.

| # | Regla |
| --- | --- |
| 1 | `Program.cs` es el único lugar donde se construye el grafo de dependencias. |
| 2 | Un slice no importa de otro. Si dos slices necesitan lo mismo, sube a `Shared/`. |
| 3 | El `MessageBox` sólo existe en `Views/`. El Handler devuelve un error; no dialoga. |
| 4 | Filtro por rol y por ámbito de oficina va en el `Handler`, dentro del SQL. Nunca en el Form. |
| 5 | Todo acceso a datos es `async` + acepta `CancellationToken`. Nada bloquea el hilo de UI. |
| 6 | `DateTime.Now` no se llama directamente: inyectar `IClock`. |
| 7 | Los modelos son `record` de solo lectura. Sin setters, sin entities mutables. |
| 8 | Conexión y transacción se obtienen de `Shared/Data/Db.cs`, no se crean a mano. |
| 9 | La cadena de conexión migra de `App.config` a `appsettings.json`, y la entrada en `.gitignore` ya está. `App.config:5` todavía tiene el password de `sa` versionado — es lo primero que se saca. |

---

## 4. Dónde vive cada invariante

Es la pregunta que decide el diseño. **Hay reglas que la base ya garantiza, y
reescribirlas en C# es duplicar un invariante en dos lugares donde equivocarse.**

| Invariante | Dónde vive | Papel de la app |
| --- | --- | --- |
| Nadie por encima de `CapacidadTotal` | `usp_AdmitirAsistente` con `UPDLOCK, HOLDLOCK` | Traduce el `50004`. **No** recalcula la capacidad. |
| Admitido ⇒ imputado a la factura | `CK_Asistente_AdmitidoImputado` | — |
| Rechazado ⇒ motivo obligatorio | `CK_Asistente_RechazoConMotivo` | Valida el input (FluentValidation) para no llegar al error. |
| `Total = ImporteBase + ImporteExceso` | `usp_FinalizarTurno` (`REF-09`) | Presenta el resultado. No suma por su cuenta. |
| Máquina de estados del turno | `TR_Turnos_Estados` | Sólo ofrece transiciones válidas en la UI. |
| Cerrar con asistentes sin decidir | `usp_FinalizarTurno` (`REF-09`) → `50005` | Traduce el `50005`. |
| Orden correlativo por turno | `UQ_Asistente_Orden` | — |
| **Marca `EsExceso = 1` al llegar al asiento N+1** | **La app** (`US-17`) | Único caso que el doc delega en la app. |

Regla general: **la app traduce errores, la base decide.** La excepción es
`EsExceso`, que el `US-17` define explícitamente como lógica de aplicación.

---

## 5. Errores de base de datos

El `THROW` de un SP tiene número y mensaje. Capturá por número, no por texto.

| Código | Fuente | Significado | Excepción de dominio |
| --- | --- | --- | --- |
| `50001` | `usp_AdmitirAsistente` | El asistente no existe | `EntidadNoEncontradaException` |
| `50002` | `usp_AdmitirAsistente` | La decisión ya fue tomada | `EstadoInvalidoException` |
| `50003` | `usp_AdmitirAsistente` | Rechazar un excedente exige motivo | `ValidacionException` |
| `50004` | `usp_AdmitirAsistente` | No hay espacio disponible | `CapacidadExcedidaException` |
| `50005` | `usp_FinalizarTurno` | Hay asistentes pendientes de decisión | `EstadoInvalidoException` |
| `50010` | `usp_ReservarTurno` | La oficina no existe o no está activa | `EntidadNoEncontradaException` |
| `50011` | `usp_ReservarTurno` | La oficina no tiene administrador asignado | `EstadoInvalidoException` |
| `50012` | `usp_ReservarTurno` | La oficina no atiende ese día | `ReglaNegocioException` |
| `50013` | `usp_ReservarTurno` | Oficina cerrada por excepción en esa fecha | `ReglaNegocioException` |
| `50014` | `usp_ReservarTurno` | No hay horario vigente para esa fecha | `ReglaNegocioException` |
| `50015` | `usp_ReservarTurno` | La franja cae fuera del horario de atención | `ReglaNegocioException` |
| `50016` | `usp_ReservarTurno` | No hay suficientes asientos en esa franja | `CapacidadExcedidaException` |
| `50017` | `usp_ReservarTurno` | No hay precio vigente para esa fecha | `ReglaNegocioException` |

### 5.1 Los triggers no tienen número

`TR_Turnos_Capacidad`, `TR_Turnos_Estados`, `TR_Oficinas_Capacidad`,
`TR_Facturas_EmitidaInmutable` y `TR_AdministradoresOficina_Rol` usan
`RAISERROR(..., 16, 1)`, que **no** produce un número de error asignable. No se
pueden capturar por código.

Por eso: **la app no debe llegar a esos errores.** Validar en el `Validator`
antes de llamar, y si igual aparece, loguear como `SqlException` genérica.

### 5.2 Excepciones de dominio

`Shared/Results/` define el resultado y `Shared/Data/SqlError.cs` hace la
traducción en un solo lugar. Ningún slice tiene su propio `try/catch` de
`SqlException` para mapear códigos a mano.

---

## 6. Dependencias

### 6.1 Permitidas

| Paquete | Versión | Licencia | Dónde |
| --- | --- | --- | --- |
| `Microsoft.Data.SqlClient` | `5.2.2` (fijado en `.csproj`) | MIT | `Shared/Data` |
| `Dapper` | `2.1.89` | Apache-2.0 | `Shared/Data` |
| `CommunityToolkit.Mvvm` | `8.4.2` | MIT | `Views/ViewModels` |
| `FluentValidation` | `12.1.1` | Apache-2.0 | `*.Validator.cs` |
| `Microsoft.Extensions.DependencyInjection` | `10.0.12` | MIT | `Program.cs` |
| `Microsoft.Extensions.Configuration.Json` | `10.0.12` | MIT | `Program.cs` |
| `Shouldly` | `4.3.0` | BSD-3 | tests |
| `xunit` | `2.9.3` | Apache-2.0 | tests |

Versiones fijadas contra el registro público de NuGet (2026-01). No usá rangos
abiertos (`*`, `8.x`) en el `.csproj`: fijá la versión y revisá el changelog a
mano. `SqlClient 5.2.2` está fijado porque así está en el repo; hay versión
nueva disponible (`7.1.1`) — actualizarlo es una tarea separada con sus propios
AC, no un side effect de agregar paquetes.

### 6.2 Prohibidas — y por qué

| Paquete | Problema |
| --- | --- |
| **MediatR ≥ 13** | Desde julio 2025 es RPL-1.5 o licencia comercial de Lucky Penny. Los handlers son clases normales con inyección de dependencias, no hace falta un mediator. |
| **AutoMapper ≥ 15** | Misma mudanza a licencia comercial. El mapeo `Row → Request` es un constructor de `record`. |
| **FluentAssertions ≥ 8** | Licencia Xceed, pago para uso comercial. Usar Shouldly. |
| **EF Core** | El diseño pone las invariantes de concurrencia en stored procedures con `UPDLOCK, HOLDLOCK`. EF pelea contra eso. |
| **Any package sin revisar la licencia** | Verificar la licencia antes de `dotnet add package`. Verificado contra el estado en 2026-01. |

---

## 7. Punto de partida

`Form1.cs:33` tiene el `SELECT` de oficinas dentro de un string crudo en
`btnProbarConexion_Click`. Es el primer slice que se crea:

```
Features/A/EdificiosOficinas/ListarOficinas/
├── ListarOficinas.Request.cs    record vacío (filtro de fecha, si hace falta)
├── ListarOficinas.Row.cs        el shape del SELECT de Form1.cs:33
├── ListarOficinas.Handler.cs    Dapper contra dbo.Oficinas + Pisos + Edificios
└── ListarOficinas.Validator.cs  sólo si hay filtros
```

Después de ese, el orden está en `docs/03-orden-de-implementacion.md`. Arrancar
por la Fase 0 y después Fase 1 (`US-25` / `Login`) — **no** por el sobrecupo,
que necesita autorización previa (§2 de ese doc).
