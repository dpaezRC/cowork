# 00 · Análisis funcional — Sistema de gestión de coworking

> Documento 1 de 4. Ver también `01-refactor-db.md`, `02-historias-de-usuario.md` y
> `03-orden-de-implementacion.md`.

---

## 1. Contexto

Una empresa es propietaria de varios edificios. Cada edificio tiene pisos y cada piso
tiene oficinas de coworking. El sistema tiene que resolver tres cosas:

1. **Gestionar el inventario**: edificios, pisos, oficinas, precios y personal a cargo.
2. **Gestionar turnos**: clientes piden un turno con *X* asientos en una oficina, en una
   franja horaria, y se les cobra en función de la cantidad de asientos y el tiempo.
3. **Auditar la operación**: un empleado administra una o varias oficinas, verifica el
   turno y las personas, da el OK, y decide qué hacer con la gente que llegó de más.

---

## 2. Estado actual del repositorio

| Ítem | Estado |
| --- | --- |
| Solución | `CoworkApp.sln` — un solo proyecto |
| Proyecto | WinForms, `net10.0-windows`, .NET 10 |
| Dependencia | `Microsoft.Data.SqlClient` 5.2.2 |
| Motor | SQL Server, base `iset` |
| Script | `CoworkApp/Scripts/InitCoworking.sql` — 324 líneas, monolítico |
| Persistencia | `CoworkApp/Data/Database.cs` — 40 líneas, sólo expone `SqlConnection` |
| UI | `Form1` — un botón "Probar conexión" y un `DataGridView` |

**Tablas existentes:** `Roles`, `EstadosTurno`, `Usuarios`, `Edificios`,
`SupervisorEdificios`, `Pisos`, `Oficinas`, `Turnos`, `ValidacionesSupervisor`.

**Semilla actual:** 3 roles (`admin`, `supervisor`, `cliente`), 4 estados de turno
(`activo`, `finalizado`, `cancelado`, `vencido`), 2 edificios, 3 pisos, 3 oficinas.

**Realidad de la app:** no hay login, no hay capa de repositorios, no hay servicios, no
hay lógica de negocio. Todo el conocimiento del dominio vive únicamente en el SQL.

---

## 3. Matriz de cobertura: requisitos vs. schema

| # | Requisito | ¿Existe? | Observación |
| --- | --- | --- | --- |
| R1 | Edificios | Sí | `Edificios(Nombre, Direccion, Ciudad)` |
| R2 | Pisos | Sí | `Pisos(EdificioId, Numero, Descripcion)` |
| R3 | Oficina con precio, piso y edificio | Parcial | Precio plano y sin versionado |
| R4 | Tipo de oficina (A/B/C/D) | **Fuera de scope** | Se mantiene texto libre. Ver §6.5 |
| R5 | Horario de apertura y cierre por oficina | **No** | No existe ninguna tabla de horarios |
| R6 | Límite de duración del turno | **No** | Nada valida `HoraFinEstimada` contra nada |
| R7 | Precio = cantidad de asientos × tiempo | Parcial | Sólo hay `PrecioPorMinuto`; falta la dimensión *asiento* |
| R8 | Uno o varios empleados administran cada oficina | **No** | `SupervisorEdificios` es supervisor ↔ **edificio**, no ↔ oficina |
| R9 | El cliente solicita turno con X asientos | Parcial | La tabla existe; sin validación de capacidad ni disponibilidad |
| R10 | El administrador audita el turno y las personas, da el OK | Parcial | `ValidacionesSupervisor` no modela turno pendiente ni detalle por persona |
| R11 | Sobrecupo: cobrar si hay espacio, si no.expulsar | **No** | Es el núcleo del negocio y no está modelado |
| R12 | Factura final del turno | Parcial | Un `MontoTotal` plano, sin desglose ni snapshot de precio |

**Cobertura aproximada: 40%.** Lo que falta es precisamente lo diferencial del negocio:
horarios, administración por oficina, asistencia con sobrecupo y facturación.

---

## 4. Bugs detectados en el schema actual

### 4.1 Truncamiento de caracteres no-ASCII — severidad alta

`Edificios.Nombre`, `Direccion`, `Ciudad`, `Oficinas.Nombre` y `Tipo` son `VARCHAR`, pero
la semilla inserta literales con prefijo `N`:

```sql
-- InitCoworking.sql:264
INSERT INTO dbo.Edificios (Nombre, Direccion, Ciudad)
VALUES (N'Torre Central', N'Av. Principal 123', N'Madrid');
```

El prefijo `N` marca el literal como Unicode, pero el destino `VARCHAR` lo convierte a la
página de códigos del servidor. Cualquier tilde, eñe o símbolo se pierde **en silencio**: el
`INSERT` tiene éxito y el dato queda corrupto. `Ciudad VARCHAR(100)` es el caso más
probable de romperse en producción.

### 4.2 `Usuarios` sin endurecimiento de seguridad — severidad media

- `PasswordHash VARCHAR(255)` — debería ser `NVARCHAR` y convivir con la columna de
  algoritmo que indique cómo verificarlo.
- No hay `Activo`: un empleado dado de baja conserva acceso.
- No hay `IntentosFallidos` ni `BloqueadoHasta`: login sin límite de intentos.
- No hay `Documento`: imposible identificar a un asistente sin cuenta de usuario.
- No hay `ActualizadoAt`: no se puede auditar cambios.

### 4.3 `ValidacionesSupervisor.EstadoVerificacion VARCHAR(50)` — severidad media

Texto libre donde debería haber FK a un catálogo. Esto garantiza divergencia de datos:
`"OK"`, `"ok"`, `"aprobado"`, `"Aprobado"` serán cuatro valores distintos para la misma
condición, y ninguna consulta puede filtrar por estado sin `LIKE`.

### 4.4 Cero control de sobrecupo — severidad alta

Nada impide que dos turnos solapados en la misma oficina sumen más que `CapacidadTotal`.
No es sólo un bug de datos: es una pérdida de plata directa, porque cada asiento de más
que entra sin control es un asiento que no se factura. Tampoco existe el índice que
permitiría ni siquiera *detectar* el solapamiento de forma eficiente:

```sql
-- Turnos tiene IX_Turnos_OficinaId y IX_Turnos_ClienteId por separado.
-- Falta el compuesto (OficinaId, HoraInicio, HoraFinEstimada).
```

### 4.5 Sin snapshot de precio — severidad alta

`Turnos` guarda `OficinaId` pero no el precio aplicado. Si mañana se cambia
`Oficinas.PrecioPorMinuto`, **los turnos históricos se re-facturan** al nuevo valor. En un
sistema con facturas esto es un defecto grave.

### 4.6 `Turnos.HoraFinEstimada` es nullable — severidad media

Un turno sin hora de fin no se puede validar contra el horario de la oficina, no se puede
calcular minutos facturables y no se puede cerrar. Debería ser `NOT NULL`.

### 4.7 Ausencia de auditoría temporal — severidad baja

Ninguna tabla tiene `ActualizadoAt`. No hay forma de saber cuándo se cambió el precio de
una oficina ni cuándo se reasignó un administrador.

### 4.8 Sin usuarios en la semilla — severidad media

`InitCoworking.sql` crea `Usuarios` y los 3 roles, pero **no inserta ningún usuario**. Hoy
no hay forma de autenticarse en el sistema, y la app ni siquiera tiene pantalla de login.

---

## 5. Modelo de datos objetivo

```
                          ┌──────────────┐
                          │   Usuarios   │  rol: admin | supervisor | cliente
                          └──────┬───────┘
                                 │
        ┌────────────────────────┼───────────────────────────┐
        │ N:M                    │ N:M                       │
┌───────▼─────────┐      ┌───────▼────────────┐       ┌───────▼──────┐
│SupervisorEdificios│      │AdministradoresOficina│      │    Turnos     │
│ (ámbito edificio) │      │  (ámbito oficina)   │      │  ClienteId   │
└───────┬─────────┘      └────────┬────────────┘       └───────┬──────┘
        │                         │                            │ 1:N
┌───────▼─────────┐      ┌────────▼────────────┐       ┌───────▼──────┐
│   Edificios     │1:N   │      Oficinas       │       │AsistentesTurno│
└───────┬─────────┘      │  PisoId, Capacidad, │       └───────┬──────┘
        │1:N             │  Tipo (texto libre) │               │
┌───────▼─────────┐      └───┬──────────┬───────┘        ┌──────▼───────┐
│     Pisos       │1:N        │1:N       │1:N             │   Facturas   │
└─────────────────┘    Horarios  Precios  │1:N            │  TurnoId UQ  │
                       Oficina  Oficina  │                └──────────────┘
                                  Asistentes
```

### 5.1 Tablas nuevas (7)

| Tabla | Para qué |
| --- | --- |
| `HorariosOficina` | Apertura/cierre por día de semana de cada oficina |
| `HorariosExcepcion` | Feriados y horarios reducidos, con prioridad sobre el semanal |
| `AdministradoresOficina` | N:M entre empleados y oficinas (R8) |
| `PreciosOficina` | Precio por asiento y minuto, versionado por vigencia (R7) |
| `AsistentesTurno` | Quién llega realmente, excedente admitido o rechazado (R11) |
| `Facturas` | Una factura por turno, con desglose base + excedente (R12) |
| `EstadosValidacion` | Catálogo para el resultado de la auditoría |

### 5.2 Tablas modificadas (6)

| Tabla | Cambio |
| --- | --- |
| `Edificios` | `VARCHAR` → `NVARCHAR` |
| `Oficinas` | `Tipo` → `NVARCHAR(100)`, `PrecioPorMinuto` pasa a ser caché deprecada |
| `Usuarios` | `NVARCHAR`, `Activo`, `Documento`, `IntentosFallidos`, `BloqueadoHasta`, `AlgoritmoHash`, `ActualizadoAt` |
| `EstadosTurno` | Se amplía de 4 a 9 estados |
| `Turnos` | `HoraFinEstimada NOT NULL`, snapshot de precio, contadores de asientos, `MontoTotal` calculado |
| `ValidacionesSupervisor` | `EstadoVerificacion` → `EstadoId` FK |

---

## 6. Decisiones de diseño tomadas

Estas decisiones se definieron con el usuario. Se documentan acá junto con las alternativas
descartadas, para que quede el criterio registrado y no se reabra la discusión sin contexto.

### 6.1 El sobrecupo lo decide un humano, no la base

**Elegido: decisión manual del administrador.** El administrador ve quién llegó de más y
admite o rechaza persona por persona.

**Consecuencia de diseño:** `AsistentesTurno.Admitido` es **nullable** y representa tres
estados, no dos: `NULL` = pendiente de decisión, `1` = admitido, `0` = rechazado.

**Lo que la base sigue imponiendo aunque la decisión sea humana:**

- Admitido implica facturado. Nadie entra gratis.
- Rechazado exige motivo obligatorio y no genera cargo.
- **No se puede admitir por encima de la capacidad.** Ésta es la invariante que traduce
  literalmente el "si hay espacio" del requerimiento. La valida
  `dbo.usp_AdmitirAsistente` dentro de la misma transacción que la inserción.
- No se puede cerrar un turno con asistentes sin decidir.

**Descartado:** admisión automática en base de datos. Se descartó porque delega en el
sistema una decisión con costo económico y potencial conflicto con el cliente.

### 6.2 Los asientos son un conteo, no identificadores individuales

**Elegido: sólo conteo.** `CapacidadTotal` es un número y cada asistente lleva un `Orden`
entre 1 y N.

**Descartado:** tabla `Asientos(OficinaId, Numero)` con asignación de butaca física. No
aporta nada a las reglas de negocio actuales.

### 6.3 Una factura por turno

**Elegido: 1 factura : 1 turno.** `Facturas.TurnoId` es `UNIQUE`. El desglose cabe en
columnas (`ImporteBase` + `ImporteExceso`), así que la tabla `FacturaItems` **queda fuera de
alcance**.

**Descartado:** factura consolidada por período (mensual). Se difiere: si más adelante hace
falta agrupar, la migración desde `Facturas(TurnoId UNIQUE)` es trivial.

### 6.4 Dos ámbitos de supervisión, un solo rol

**Elegido: mantener `SupervisorEdificios` y agregar `AdministradoresOficina`.**

- `SupervisorEdificios` → responsable del **edificio** (ámbito amplio, ya existe).
- `AdministradoresOficina` → responsable operativo de cada **oficina** (ámbito acotado).

Ambos usan el rol `supervisor`. Lo que cambia es el ámbito de asignación: un administrador
de oficina puede no ser supervisor de su edificio. Esto es coherente con el requerimiento
"administrada por uno o varios empleados".

**Descartado:** reusar `SupervisorEdificios` (no cumple el "varios por oficina") y
eliminarlo (un empleado sin oficinas asignadas quedaría sin alcance definido).

### 6.5 El tipo de oficina es texto libre

**Elegido: `Oficinas.Tipo` sigue siendo texto libre** (`NVARCHAR(100)`).

**Descartado:** el catálogo `TiposOficina` con códigos A/B/C/D. Se descartó explícitamente
como fuera de scope. El valor actual del seed (`escritorio_compartido`, `oficina_privada`,
`sala_reuniones`) se conserva tal cual; lo único que cambia es el tipo de columna, para
que acepte acentos.

### 6.6 El único límite de duración es el horario de la oficina

**Elegido: no existe `DuracionMaximaMinutos`.** La duración de un turno la acota
exclusivamente el horario de apertura y cierre de esa oficina en ese día. Si una oficina
abre de 8 a 20, un turno puede durar hasta 12 horas.

**Descartado:** duración máxima por oficina y duración máxima global del sistema. Ambas
se consideran sobre-ingeniería hasta que aparezca un requerimiento real de límites.

### 6.7 El excedente se factura al mismo precio que un asiento reservado

**Elegido: una sola tarifa.** El precio del excedente es el mismo
`PrecioAsientoMinutoSnapshot` del turno.

**Consecuencia:** hace falta un único snapshot de precio, no dos.

**Descartado:** recargo por excedente, o tarifa fija por persona extra.

### 6.8 Todos los asistentes comparten el fin de turno

**Elegido: los minutos facturables son los del turno, no los de cada persona.** Si
`HoraFinReal > HoraFinEstimada`, se cobra la diferencia a todos: a los reservados y también
al excedente admitido.

**Descartado:** `HoraIngreso`/`HoraSalida` por fila de `AsistentesTurno` con check-in /
check-out individual. Se descartó por complejidad de UI; es el candidato natural a
revisar si el negocio necesita fairness en sesiones largas.

---

## 7. Puntos abiertos

### 7.1 Los minutos del turno no tienen dueño

Ningún requisito dice qué acota `HoraFinEstimada` más allá del horario de la oficina. La
asunción implementada es que el cliente elige libremente cualquier franja dentro del
horario vigente. Si existe una regla comercial (ej. "máximo 4 horas" o "mínimo 1 hora"),
falta confirmarla.

### 7.2 Usuarios de la semilla

`InitCoworking.sql` no inserta usuarios. Hay que decidir entre passwords de prueba reales en
el repositorio o un seeder que genere las credenciales en el primer arranque. La segunda es
la opción más segura para no versionar credenciales.

### 7.3 moneda y datos fiscales

El seed usa `€` en los labels de la UI, pero no hay columna de moneda ni datos fiscales del
cliente (identificación fiscal, razón social). `US-01` necesita decidir si un cliente es
una persona física o una empresa.

### 7.4 Notificaciones al cliente

`US-13`, `US-16` y `US-22` asumen que el cliente se entera de la cancelación, el rechazo y
la factura. **No hay definido el canal**: email, notificación in-app, o nada. La
implementación actual puede diferir la notificación sin romper el flujo.

---

## 8. Convenciones para los tickets

- **R#** = requisito funcional (este documento, §3).
- **REF-##** = ticket de refactor de base de datos (`01-refactor-db.md`).
- **US-##** = historia de usuario (`02-historias-de-usuario.md`).

> **Aviso de numeración:** el requisito **R4** (tipo de oficina, fuera de scope) y el
> ticket **REF-04** (horarios por oficina) no son lo mismo. Al crear el backlog conviene
> renombrar los tickets con un prefijo inequívoco para evitar confusión.
