# 02 · Historias de usuario — 26 historias en 8 epics

> Documento 3 de 4. Contexto en `00-analisis-funcional.md`, schema en `01-refactor-db.md`.

---

## Convenciones

| Elemento | Significado |
| --- | --- |
| `US-##` | Identificador de la historia |
| **Como** | El actor. Los tres roles del schema: `admin`, `supervisor` (que actúa como administrador de oficina) y `cliente` |
| *Quiero* | La necesidad |
| **Para** | El valor de negocio |
| **AC** | Criterios de aceptación, verificables |
| **REF-##** | Ticket de base de datos del que depende |

---

## Mapa de epics

| Epic | Historias | Cierra requisitos |
| --- | --- | --- |
| **A** · Edificios y oficinas | `US-01` … `US-04` | R1, R2, R3 |
| **B** · Horarios | `US-05` … `US-07` | R5, R6 |
| **C** · Administración de oficinas | `US-08` … `US-09` | R8 |
| **D** · Solicitud de turno | `US-10` … `US-13` | R9 |
| **E** · Auditoría y OK | `US-14` … `US-16` | R10 |
| **F** · Asistencia y sobrecupo | `US-17` … `US-21` | **R11** |
| **G** · Facturación | `US-22` … `US-24` | R12 |
| **H** · Transversal | `US-25` … `US-26` | — |

```
A ─► B ─► C ──┐
               ├─► D ─► E ─► F ─► G
H ─────────────┘
```

---

# Epic A · Edificios y oficinas

## `US-01` · Dar de alta y editar edificios

**Como** `admin`
**Quiero** registrar y mantener los edificios de la empresa con su nombre, dirección y ciudad
**Para** tener el inventario de propiedades administered.

**AC**
- [ ] Los tres campos son obligatorios.
- [ ] Un edificio se puede editar sin recrear sus pisos ni oficinas.
- [ ] Los caracteres acentuados se guardan y se releen correctamente (depende de `REF-01`).
- [ ] No se puede guardar un edificio sin nombre.
- [ ] El listado muestra nombre, dirección, ciudad y cantidad de oficinas.

**Depende de:** `REF-01`

> **Punto abierto (§7.3 del análisis):** no se decidió si un cliente es persona física o
> empresa. Esta historia sólo cubre edificios.

---

## `US-02` · Gestionar los pisos de un edificio

**Como** `admin`
**Quiero** dar de alta y editar los pisos de cada edificio
**Para** localizar dónde está cada oficina.

**AC**
- [ ] Un piso pertenece a exactamente un edificio.
- [ ] No se puede repetir `(EdificioId, Numero)`.
- [ ] El número de piso es un entero positivo.
- [ ] La descripción es opcional.
- [ ] Al eliminar un edificio se avisa de que sus pisos y oficinas se eliminarán en cascada.

**Depende de:** — (ya existe `Pisos`, sólo falta la UI)

---

## `US-03` · Crear una oficina

**Como** `admin`
**Quiero** registrar una oficina con su nombre, piso, tipo, capacidad y precio por asiento y minuto
**Para** poder ofrecer turnos en ella.

**AC**
- [ ] La oficina pertenece a un piso, que a su vez a un edificio.
- [ ] `CapacidadTotal` es un entero mayor a cero.
- [ ] El precio es mayor o igual a cero, con hasta 4 decimales.
- [ ] `Tipo` es **texto libre** de hasta 100 caracteres (decisión §6.5 — no hay catálogo
      A/B/C/D).
- [ ] La oficina nace `Activa = 0` hasta tener administrador y horario.
- [ ] Crear la oficina exige un precio en `PreciosOficina` con vigencia desde hoy.

**Depende de:** `REF-05`, `REF-06`

---

## `US-04` · Listar y filtrar oficinas

**Como** `admin`
**Quiero** ver todas las oficinas con su precio vigente, horario y administrador
**Para** tener una vista operativa del negocio.

**AC**
- [ ] Filtros por edificio, piso, tipo y estado (activa / inactiva).
- [ ] Cada fila muestra: edificio, piso, nombre, tipo, capacidad, **precio vigente**,
      horario semanal y administrador.
- [ ] Las oficinas sin horario o sin administrador se señalan visualmente.
- [ ] Permite editar y desactivar, no sólo listar.

**Depende de:** `REF-04`, `REF-05`, `REF-06`

> **Mejora sobre el código actual:** `Form1.cs:33` tiene la consulta de oficinas
> embebida en un `string` dentro del evento del botón, y el resultado se muestra con un
> `MessageBox`. Esta historia es la que justifica extraer todo eso a un repositorio.

---

# Epic B · Horarios

## `US-05` · Definir el horario de atención por día de semana

**Como** `admin`
**Quiero** definir la hora de apertura y cierre de cada oficina, día por día
**Para** controlar qué turnos se pueden solicitar.

**AC**
- [ ] No se puede cerrar antes de abrir (validado por `CK_Horarios_Rango`).
- [ ] No se puede definir dos veces el mismo día para la misma oficina.
- [ ] Se pueden definir varios días en una misma operación.
- [ ] Una oficina puede tener sólo algunos días habilitados: los no habilitados no aceptan
      turnos.
- [ ] Cada día se puede activar o desactivar sin borrarlo.

**Depende de:** `REF-04`

---

## `US-06` · Registrar feriados y excepciones

**Como** `admin`
**Quiero** marcar fechas en que una oficina está cerrada o tiene horario reducido
**Para** no recibir reservas en días no operativos.

**AC**
- [ ] Una excepción closed tiene prioridad sobre el horario semanal de ese día.
- [ ] Se puede registrar cierre total (festivo) u horario reducido (medio día).
- [ ] Si el horario es reducido, se indican las dos horas y se validan con la misma regla que
      el horario semanal.
- [ ] Una fecha no se puede registrar dos veces para la misma oficina.
- [ ] La excepción tiene un motivo visible para el cliente.

**Depende de:** `REF-04`

---

## `US-07` · Ver el horario antes de reservar

**Como** `cliente`
**Quiero** ver el horario de atención de una oficina antes de solicitar un turno
**Para** no pedir un turno imposible.

**AC**
- [ ] Muestra el horario de los próximos 7 días.
- [ ] Las fechas con excepción aparecen marcadas como cerradas o reducidas.
- [ ] Si la oficina no tiene horario cargado, se indica explícitamente que no acepta
      reservas.

**Depende de:** `REF-04`

---

# Epic C · Administración de oficinas

## `US-08` · Asignar administradores a una oficina

**Como** `admin`
**Quiero** asignar uno o varios empleados como administradores de cada oficina
**Para** que la operación no dependa de una sola persona.

**AC**
- [ ] Se pueden asignar **N** administradores a una misma oficina.
- [ ] Sólo usuarios con rol `supervisor` pueden asignarse (lo bloquea el trigger de
      `REF-05`).
- [ ] No se puede asignar dos veces el mismo usuario a la misma oficina.
- [ ] Uno de los administradores puede marcarse como principal.
- [ ] **Una oficina sin al menos un administrador no puede activarse.**
- [ ] Al desasignar al último administrador, la oficina se desactiva automáticamente.

**Depende de:** `REF-05`

> **Contexto (`US-08` ≠ `US-09`):** un administrador de oficina puede no ser supervisor de su
> edificio. Son dos ámbitos distintos —decisión §6.4—. `SupervisorEdificios` sigue existiendo
> y no se toca.

---

## `US-09` · Ver sólo las oficinas que administro

**Como** `administrador de oficina`
**Quiero** que el sistema me muestre exclusivamente las oficinas que administro
**Para** no ver ni operar sobre las demás.

**AC**
- [ ] El filtro se aplica **en la consulta SQL**, no ocultando botones en la interfaz.
- [ ] Un administrador de la oficina A no puede auditar ni registrar asistencia de un turno
      de la oficina B, ni siquiera manipulando el `Id` a mano.
- [ ] `admin` ve todas las oficinas sin filtro.
- [ ] `cliente` no accede a ninguna pantalla de administración.

**Depende de:** `REF-05`

> **Ésta es la historia que protege `US-18`.** Sin ella, el sobrecupo —la historia de más
> valor del sistema— queda accesible a cualquiera que abra el `.exe`.

---

# Epic D · Solicitud de turno

## `US-10` · Ver disponibilidad en tiempo real

**Como** `cliente`
**Quiero** ver cuántos asientos hay libres en una oficina para una franja concreta
**Para** decidir si puedo reservar.

**AC**
- [ ] Muestra `libres = CapacidadTotal − Σ(CantidadAsientos de turnos solapados)`.
- [ ] Sólo se cuentan los turnos en estado bloqueante: `pendiente`, `aprobado`, `en_curso`,
      `pendiente_cierre`.
- [ ] Los turnos `cancelado`, `rechazado`, `vencido`, `no_show` y `finalizado` liberan el
      asiento.
- [ ] La consulta usa `IX_Turnos_Oficina_Horas` y no un recorrido de tabla completa.
- [ ] La disponibilidad se refresca sin recargar la pantalla completa.

**Depende de:** `REF-11`

---

## `US-11` · Solicitar un turno

**Como** `cliente`
**Quiero** solicitar un turno indicando cantidad de asientos y franja horaria
**Para** asegurar un espacio de trabajo.

**AC**
- [ ] La franja completa debe caer dentro del horario de atención (error 50015 si no).
- [ ] La cantidad de asientos no puede superar los disponibles (error 50016).
- [ ] No se puede reservar en un día de excepción cerrada (error 50013) ni en un día sin
      horario (error 50012).
- [ ] El turno se crea en estado `pendiente`.
- [ ] Se guarda el snapshot del precio vigente en el mismo `INSERT`.
- [ ] Si el turno queda pendiente, la capacidad se consume igual y no se puede volver a
      reservar ese asiento.
- [ ] Un cliente no puede tener dos turnos solapados en la misma oficina.

**Depende de:** `REF-11`, `REF-10`

---

## `US-12` · Ver el precio estimado antes de confirmar

**Como** `cliente`
**Quiero** ver cuánto me va a costar el turno antes de confirmar
**Para** decidir sin sorpresas.

**AC**
- [ ] Muestra `asientos × minutos × precio por asiento y minuto`.
- [ ] Muestra la vigencia del precio aplicado, no sólo el número.
- [ ] Si el precio cambia mientras el cliente mira la pantalla, el importe se recalcula al
      confirmar y se avisa del cambio.
- [ ] El importe mostrado coincide con el `ImporteBase` de la factura final cuando no hay
      excedentes.

**Depende de:** `REF-06`, `REF-10`

---

## `US-13` · Cancelar un turno

**Como** `cliente`
**Quiero** cancelar un turno antes de que empiece
**Para** liberar la reserva si mis planes cambian.

**AC**
- [ ] Sólo se puede cancelar desde `pendiente` o `aprobado`.
- [ ] Un turno `en_curso`, `pendiente_cierre` o `finalizado` **no** se puede cancelar.
- [ ] Al cancelar, el asiento se libera inmediatamente para otros clientes.
- [ ] Queda registrado quién canceló y cuándo.
- [ ] Si el turno ya tiene factura, la cancelación no borra la factura: se anula.

**Depende de:** `REF-07`

---

# Epic E · Auditoría y OK

## `US-14` · Ver los turnos del día

**Como** `administrador de oficina`
**Quiero** ver los turnos que llegan a mis oficinas hoy
**Para** hacer la auditoría.

**AC**
- [ ] Lista con: cliente, oficina, cantidad reservada, franja, hora de llegada real y estado.
- [ ] Sólo incluye oficinas que administro (`US-09`).
- [ ] Destaca los turnos `pendiente` que aún no tienen OK.
- [ ] Permite filtrar por estado.

**Depende de:** `REF-05`, `REF-07`

---

## `US-15` · Dar el OK al turno

**Como** `administrador de oficina`
**Quiero** aprobar un turno verificado
**Para** habilitar el acceso del cliente.

**AC**
- [ ] Sólo un turno `pendiente` puede aprobarse.
- [ ] Se registra el supervisor que aprueba, la fecha y las observaciones.
- [ ] Un turno `rechazado` **no** puede aprobarse después (lo bloquea el trigger de `REF-07`).
- [ ] Tras el OK, el turno pasa a `aprobado` y queda habilitado para el check-in.
- [ ] El cliente queda notificado.

**Depende de:** `REF-07`

---

## `US-16` · Rechazar un turno

**Como** `administrador de oficina`
**Quiero** rechazar un turno con un motivo
**Para** que el cliente no asista y se libere la capacidad.

**AC**
- [ ] El motivo es **obligatorio**.
- [ ] El turno pasa a `rechazado`, que es terminal: no admite salida.
- [ ] El asiento se libera para otros clientes.
- [ ] El motivo es visible para el cliente.
- [ ] El cliente queda notificado.

**Depende de:** `REF-07`

---

# Epic F · Asistencia y sobrecupo — el núcleo

> Este es el epic que justifica el proyecto. Requisitos R10 y R11, decisiones §6.1 y §6.8.

## `US-17` · Registrar quién llega

**Como** `administrador de oficina`
**Quiero** registrar las personas que realmente llegan al turno
**Para** compararlas contra lo reservado y detectar el sobrecupo.

**AC**
- [ ] La pantalla lista los `CantidadAsientos` reservados, con su nombre esperado.
- [ ] Los primeros `CantidadAsientos` quedan con `EsExceso = 0`.
- [ ] A partir del asiento `N+1`, el sistema **marca automáticamente** `EsExceso = 1` y deja
      `Admitido = NULL` — pendiente de decisión.
- [ ] Se puede registrar un asistente sin cuenta de usuario (sólo nombre y documento): es
      habitual que venga una persona invitada.
- [ ] Queda registrado quién cargó cada asistente (`RegistradoPor`).
- [ ] Se puede corregir un asistente mal cargado antes de decidir sobre él.

**Depende de:** `REF-08`

> **El marcado de excedente es automático, pero la admisión no** (decisión §6.1). El
> sistema identifica el sobrecupo; la decisión de a quién admite es del administrador.

---

## `US-18` · Decidir a quién admite del excedente

**Como** `administrador de oficina`
**Quiero** admitir o rechazar persona por persona al excedente
**Para** decidir con criterio quién puede usar el espacio.

**AC**
- [ ] Cada excedente aparece con tres estados posibles: pendiente, admitido, rechazado.
- [ ] **Si la oficina ya tiene `Admitidos = CapacidadTotal`, la acción de admitir falla con el
      error 50004.** Ésta es la traducción literal del "si hay espacio" del requerimiento.
- [ ] Al admitir, el sistema lo imputa a la factura automáticamente. **Nunca se admite a
      alguien sin cobrar.**
- [ ] Al rechazar, el motivo es obligatorio.
- [ ] Una vez decidida, la decisión **no se puede cambiar** (`usp_AdmitirAsistente` falla con
      50002).
- [ ] Se muestra en vivo: `reservados / admitidos / capacidad`.

**Depende de:** `REF-08`

> **Por qué el sistema no puede admitir solo** —decisión §6.1—: admitir a alguien tiene
> costo económico y puede generar conflicto con el cliente. La invariantes que la base
> **sí** impone es que no se admitan más personas que asientos.

---

## `US-19` · Rechazar excedentes cuando no hay lugar

**Como** `administrador de oficina`
**Quiero** rechazar a las personas que no pueden entrar porque la oficina está llena
**Para** que queden registradas y no se cobre por ellas.

**AC**
- [ ] El rechazo exige un motivo.
- [ ] Los rechazados **no generan ningún cargo**: `ImputadoAFactura = 0`.
- [ ] Quedan registrados con su motivo, para el reporte del turno.
- [ ] Los rechazados no ocupan asiento para el cálculo de admitted.
- [ ] Se pueden consultar los motivos de rechazo de un turno.

**Depende de:** `REF-08`

---

## `US-20` · Ver el conteo en vivo

**Como** `administrador de oficina`
**Quiero** ver cuántos asientos están reservados, admitidos y disponibles mientras registro
**Para** saber si todavía puedo admitir a alguien.

**AC**
- [ ] Muestra `reservados / admitidos / capacidad` actualizado sin recargar la pantalla.
- [ ] El contador de excedentes pendientes de decisión es visible.
- [ ] El botón de admitir se deshabilita visualmente cuando la capacidad está agotada —pero
      **la validación real sigue estando en la base**, no en la UI.

**Depende de:** `REF-08`

> La UI deshabilita el botón por comodidad, pero la garantía la da `usp_AdmitirAsistente`.
> Un deshabilitado en cliente no es un control de concurrencia.

---

## `US-21` · Bloquear el cierre con decisiones pendientes

**Como** `sistema`
**Quiero** impedir que un turno se cierre mientras haya asistentes sin decidir
**Para** no facturar un turno incompleto.

**AC**
- [ ] `usp_FinalizarTurno` falla con el error 50005 si existe algún asistente con
      `Admitido = NULL`.
- [ ] El turno queda visible en estado `pendiente_cierre` mientras haya pendientes.
- [ ] La pantalla muestra cuántos asistentes quedan por decidir y un acceso directo a la
      pantalla de decisiones.
- [ ] Una vez decididos todos, el turno se puede finalizar y facturar.

**Depende de:** `REF-09`

> **Decisión §6.1, segunda parte:** el bloqueo es explícito. Se evaluó la alternativa de
> cerrar y tratar los pendientes como rechazados por omisión, y se descartó porque
> factura silencio.

---

# Epic G · Facturación

## `US-22` · Generar la factura al cerrar el turno

**Como** `sistema`
**Quiero** generar una factura automáticamente al finalizar cada turno
**Para** que el cobro refleje exactamente lo que ocurrió.

**AC**
- [ ] **Un turno genera exactamente una factura** (`UNIQUE(TurnoId)`).
- [ ] `ImporteBase = AsientosReservados × MinutosFacturables × precio snapshot`.
- [ ] `AsientosExceso` cuenta **sólo** los excedentes con `Admitido = 1`.
- [ ] `ImporteExceso = AsientosExceso × MinutosFacturables × precio snapshot` — el mismo
      precio que los reservados (decisión §6.7).
- [ ] `Total = ImporteBase + ImporteExceso`, garantizado por `CK_Facturas_Totales`.
- [ ] Un turno sin excedentes factura `ImporteExceso = 0.00`.
- [ ] Si el turno terminó más tarde de lo previsto, los minutos se calculan con
      `HoraFinReal` y **el excedente se cobra por los mismos minutos** que la base (decisión
      §6.8).
- [ ] La factura nace en estado `pendiente`.

**Depende de:** `REF-09`, `REF-10`

---

## `US-23` · Ver el detalle de la factura

**Como** `cliente`
**Quiero** ver el desglose de lo que me facturan
**Para** entender el importe.

**AC**
- [ ] Muestra asientos base, minutos, importe base.
- [ ] Muestra asientos de excedente y su importe, **por separado**.
- [ ] Se puede ver **quién** fue el excedente y por qué se cobró, trazando hasta
      `AsistentesTurno`.
- [ ] Se muestra el precio unitario aplicado y su vigencia.
- [ ] El importe mostrado coincide exactamente con `Facturas.Total`.

**Depende de:** `REF-09`

> La trazabilidad hasta la persona es lo que justifica `DECIDidoPor`, `EsExceso` y
> `ImputadoAFactura` en `AsistentesTurno`: sin esas columnas, el cliente no podría
> impugnar un cargo y el sistema no podría auditarlo.

---

## `US-24` · Gestionar el estado de las facturas

**Como** `admin`
**Quiero** ver el estado de las facturas y anular las que correspondan
**Para** llevar el control administrativo del cobro.

**AC**
- [ ] Estados disponibles: `pendiente`, `emitida`, `pagada`, `anulada`.
- [ ] Una factura `emitida` o `pagada` **no se puede modificar ni borrar**: sólo anular
      (lo bloquea `TR_Facturas_EmitidaInmutable`).
- [ ] Anular una factura requiere motivo.
- [ ] Al anular un turno con factura, el turno también se cierra como `cancelado`.
- [ ] Filtros por estado, cliente y rango de fechas.
- [ ] Se puede exportar el listado.

**Depende de:** `REF-09`

---

# Epic H · Transversal

## `US-25` · Iniciar sesión

**Como** `usuario`
**Quiero** iniciar sesión con email y contraseña
**Para** que cada persona vea sólo lo que le corresponde.

**AC**
- [ ] El password se verifica contra `PasswordHash` usando el algoritmo de
      `AlgoritmoHash` (PBKDF2-SHA256 por defecto).
- [ ] La contraseña **nunca** se almacena ni se registra en logs.
- [ ] Un usuario con `Activo = 0` no puede autenticarse.
- [ ] 5 intentos fallidos bloquean la cuenta hasta `BloqueadoHasta`.
- [ ] El error de login es genérico: no revela si el email existe.
- [ ] La sesión se cierra al vencer el tiempo de inactividad.
- [ ] `admin`, `supervisor` y `cliente` ven menús distintos.

**Depende de:** `REF-02`

> **Ésta es la primera historia a implementar**, aunque `REF-02` sea SQL puro. Sin login,
> no hay forma de proteger nada de lo demás. Y `InitCoworking.sql` hoy no inserta ningún
> usuario, así que el sistema hoy es inaccesible.

---

## `US-26` · Dashboard operativo

**Como** `admin`
**Quiero** un resumen con los turnos del día, la ocupación por oficina y los ingresos
**Para** tener el estado del negocio de un vistazo.

**AC**
- [ ] Turnos del día: total, aprobados, pendientes de auditoría y rechazados.
- [ ] Ocupación por oficina: `Σ reserved / CapacidadTotal`, con estado visual.
- [ ] Ingresos del período: suma de `Facturas.Total` no anuladas.
- [ ] Sólo incluye oficinas activas y turnos no cancelados.
- [ ] Si el usuario es `supervisor`, el dashboard se limita a sus oficinas (`US-09`).

**Depende de:** `US-04`, `US-14`, `US-24`

---

## Trazabilidad

| Requisito | Cubierto por |
| --- | --- |
| R1 Edificios | `US-01` |
| R2 Pisos | `US-02` |
| R3 Oficina con precio, piso, edificio | `US-03`, `US-04` |
| R4 Tipo A/B/C/D | **Fuera de scope** — `Tipo` es texto libre |
| R5 Horario de apertura y cierre | `US-05`, `US-06`, `US-07` |
| R6 Límite de duración | `US-11` — el único límite es el horario de la oficina |
| R7 Precio = asientos × tiempo | `US-03`, `US-12`, `US-22` |
| R8 Varios empleados por oficina | `US-08`, `US-09` |
| R9 Cliente solicita turno con X asientos | `US-10`, `US-11`, `US-13` |
| R10 Admin audita y da el OK | `US-14`, `US-15`, `US-16` |
| R11 Sobrecupo: cobrar si hay espacio, si no fuera | `US-17`, `US-18`, `US-19`, `US-20`, `US-21` |
| R12 Factura final | `US-22`, `US-23`, `US-24` |

**Requisitos cubiertos: 11 de 12.** R4 queda explícitamente fuera de scope.
