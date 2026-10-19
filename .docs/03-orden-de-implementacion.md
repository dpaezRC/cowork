# 03 · Orden de implementación

> Documento 4 de 4. Contexto en `00-analisis-funcional.md`, tickets en
> `01-refactor-db.md`, historias en `02-historias-de-usuario.md`.

---

## 1. Punto de partida real

La app tiene **una** pantalla y **cero** capas lógicas.

| Archivo | Líneas | Problema |
| --- | --- | --- |
| `CoworkApp/Program.cs` | 16 | Sólo arranca `Form1` |
| `CoworkApp/Form1.cs` | 72 | Una consulta SQL embebida en el evento del botón |
| `CoworkApp/Data/Database.cs` | 40 | Sólo expone `SqlConnection` |
| `CoworkApp/Scripts/InitCoworking.sql` | 324 | Todo el dominio en un monolito |

`Form1.cs:33` tiene el `SELECT` de oficinas dentro de un `string` crudo en
`btnProbarConexion_Click`. **No hay dónde poner lógica de negocio, ni un modelo, ni un
servicio de autenticación.** Cualquier historia que no sea "ver oficinas" exige primero una
estructura que hoy no existe.

Esto condiciona todo el plan: **el Sprint 0 de SQL no alcanza por sí solo.**

---

## 2. La regla de oro: autorización antes que capacidad

Hay dos historias que van primero aunque su ticket de DB sea trivial:

- **`US-25`** — login
- **`US-08` / `US-09`** — asignación y ámbito de oficina

**Motivo:** `US-18` —decidir a quién se admite del excedente— es la historia de más valor
del sistema, y es también la que mueve dinero. Si se implementa el circuito de sobrecupo
antes que la autorización, queda accesible a cualquiera que abra el `.exe`. Un bug de
seguridad barato de evitar ahora y carísimo después.

El error de orden inverso es tentador: "primero hago que funcione, después le pongo
seguridad". En una app Windows local ese razonamiento falla — el `.exe` se publica como
artefacto de CI en **cada push a `dev` y `main`** (`.github/workflows/ci.yml`), o sea que
cualquiera con acceso al repo lo descarga y lo ejecuta.

---

## 3. Secuencia

Cada bloque lista las historias, los tickets de DB que habilita y qué se puede verificar al
terminar.

### Fase 0 · Cimientos de la app

Sin dominio, sin lógica. Desbloquea todo lo demás.

| Tarea | Detalle |
| --- | --- |
| Estructura de carpetas | `Data/`, `Models/`, `Repositories/`, `Services/`, `Forms/` |
| Migrar la cadena de conexión | `App.config` → `appsettings.json`, con `Microsoft.Extensions.Configuration` |
| `Models/` | Clases POCO que mapeen las tablas existentes |
| `Repositories/` | Extraer la consulta de `Form1.cs:33` a `OficinaRepository` |
| `BaseRepository` | Helper de parámetros y transacción, con `CancellationToken` |
| **Depende de** | — |
| **Verificable** | La app arranca, carga oficinas en el `DataGridView` sin cambiar una línea de SQL |

> La cadena de conexión vive hoy hardcodeada en `App.config:5` con el password
> `yourStrong#Password` de `sa`. Es un default de desarrollo, pero conviene moverla a
> `appsettings.json` **y a `.gitignore`** antes de que alguien la cambie de verdad.

---

### Fase 1 · `US-25` + `REF-02` — Login

**El primer incremento real de la app.**

| | |
| --- | --- |
| Historias | `US-25` |
| DB | `REF-01`, `REF-02`, `REF-03` |
| App | `AuthService`, `Session`, `LoginForm`, enrutado por rol en `Program.Main` |

**Verificable**
- [ ] Un usuario `Activo = 0` no entra.
- [ ] 5 intentos fallidos bloquean la cuenta.
- [ ] Cada rol ve un menú distinto.

> **Pendiente de definir (§7.2 del análisis):** `InitCoworking.sql` no inserta ningún
> usuario. Hay que elegir entre credenciales de prueba versionadas —mala práctica— o un
> seeder que las genere en el primer arranque. **Recomendado: el seeder.**
>
> El hash también tiene que existir: PBKDF2-SHA256 con sal por usuario. `Database.cs` hoy
> no tiene nada de esto.

---

### Fase 2 · `US-01..07` — Maestro del inventario

**Bajo riesgo, alto volumen de datos de prueba.** Es lo que después permite probar el resto
con datos realistas.

| | |
| --- | --- |
| Historias | `US-01`, `US-02`, `US-03`, `US-04`, `US-05`, `US-06`, `US-07` |
| DB | `REF-04` (horarios), `REF-05` (administradores), `REF-06` (precios) |
| App | CRUD de edificios, pisos, oficinas; pantalla de horarios; catálogo público |

**Verificable**
- [ ] Se puede crear un edificio con tildes y se releen bien.
- [ ] Una oficina sin horario ni administrador no se puede activar.
- [ ] No se puede guardar un horario que cierre antes de abrir.
- [ ] El cliente ve el horario de una oficina.

> Se incluye `US-08` en el paquete de **base de datos** acá (`REF-05`), pero su **UI**
> va en la Fase 3, porque la pantalla de asignación sólo tiene sentido con login y filtro
> de ámbito ya funcionando.

---

### Fase 3 · `US-08`, `US-09` — Ámbito de oficina

**Barato ahora, imposible después.**

| | |
| --- | --- |
| Historias | `US-08`, `US-09` |
| DB | — (ya está `REF-05`) |
| App | Pantalla de asignación de administradores, `CurrentUser.OficinasAdministradas` |

**Verificable**
- [ ] Asignar un `cliente` como administrador falla.
- [ ] Un supervisor no ve oficinas que no administra.
- [ ] **Manipular a mano el `Id` de un turno ajeno no da acceso** (la prueba de que el
      filtro está en SQL y no en la UI).

---

### Fase 4 · `US-10..13` — Reserva de turnos

**El camino feliz completo.**

| | |
| --- | --- |
| Historias | `US-10`, `US-11`, `US-12`, `US-13` |
| DB | `REF-07` (estados), `REF-10` (snapshot), `REF-11` (disponibilidad) |
| App | `DisponibilidadService`, `TurnoRepository`, pantalla de reserva y cancelación |

**Verificable**
- [ ] La franja tiene que caber dentro del horario.
- [ ] No se puede reservar en un día de excepción cerrada.
- [ ] El precio queda congelado al reservar.
- [ ] **20 reservas concurrentes sobre 6 asientos → exactamente 6 aceptadas.**
- [ ] Cancelar libera el asiento.

> El test de concurrencia de la Fase 4 es el que valida de verdad `usp_ReservarTurno`.
> Sin él, los `UPDLOCK`/`HOLDLOCK` quedan sin verificar y la sobreventa es un bug
> silencioso hasta que aparece en producción.

---

### Fase 5 · `US-14..21` — Auditoría y sobrecupo

**El núcleo de negocio.**

| | |
| --- | --- |
| Historias | `US-14` … `US-21` |
| DB | `REF-08` (asistencia y sobrecupo) |
| App | `AsistenciaService`, `TurnoRepository`, pantalla de auditoría y de registro |

**Verificable**
- [ ] `US-14` Lista sólo los turnos de las oficinas del administrador.
- [ ] `US-15` El OK registra supervisor, fecha y observaciones.
- [ ] `US-16` El rechazo exige motivo y libera la capacidad.
- [ ] `US-17` A partir del asiento N+1 el excedente se marca solo, **pero la admisión no es
      automática**.
- [ ] `US-18` Admitir con la oficina llena falla con 50004; admitir con lugar imputa a la
      factura.
- [ ] `US-19` Un rechazado no genera cargo y conserva su motivo.
- [ ] `US-20` El contador se actualiza sin recargar.
- [ ] `US-21` No se puede cerrar con asistentes sin decidir.

---

### Fase 6 · `US-22..24` — Facturación

Al final, porque depende del cierre de turno.

| | |
| --- | --- |
| Historias | `US-22`, `US-23`, `US-24` |
| DB | `REF-09` (facturación) |
| App | `FacturacionService`, pantalla de factura y de listado |

**Verificable**
- [ ] Un turno genera exactamente una factura.
- [ ] `Total = ImporteBase + ImporteExceso`, siempre.
- [ ] Los excedentes rechazados no se facturan.
- [ ] No se puede modificar una factura emitida: sólo anular.
- [ ] El cliente ve el detalle y puede rastrear quién fue el excedente.

---

### Fase 7 · `REF-12` + `US-26` — Cierre

| | |
| --- | --- |
| Historias | `US-26` |
| DB | `REF-12` (migraciones versionadas) |
| App | Dashboard |

`REF-12` puede empezar mucho antes, en la Fase 1: partir el monolito SQL es independiente
del resto y **cuanto antes, mejor**, porque a partir de la Fase 2 hay migrations que
dependen del orden. Si se deja para el final, el `InitCoworking.sql` ya tiene las tablas
modificadas y la separación es más riesgosa.

**Verificable**
- [ ] El runner se puede correr dos veces sin efecto.
- [ ] Editar un script aplicado falla por checksum.
- [ ] Una base nueva llega al estado final sin pasos manuales.

---

## 4. Vista consolidada

| Fase | Contenido | Historias | DB |
| --- | --- | --- | --- |
| 0 | Cimientos de la app | — | — |
| 1 | Login | `US-25` | `REF-01`, `REF-02`, `REF-03`, `REF-12` |
| 2 | Maestro del inventario | `US-01` … `US-07` | `REF-04`, `REF-05`, `REF-06` |
| 3 | Ámbito de oficina | `US-08`, `US-09` | — |
| 4 | Reserva de turnos | `US-10` … `US-13` | `REF-07`, `REF-10`, `REF-11` |
| 5 | Auditoría y sobrecupo | `US-14` … `US-21` | `REF-08` |
| 6 | Facturación | `US-22` … `US-24` | `REF-09` |
| 7 | Cierre | `US-26` | — |

```
F0 ──► F1 ──┬─► F2 ──► F3 ──► F4 ──► F5 ──► F6 ──► F7
             │        └────────────────────────────┘
             └─► REF-12 (puede arrancar acá, en paralelo)
```

`REF-12` es transversal: puede empezar en la Fase 1.

---

## 5. Alternativa considerada y descartada

**Hacer los 12 tickets de SQL primero y después la app.** Fue la primera opción que
planteé.

| A favor | En contra |
| --- | --- |
| Tres sprints de SQL sin tocar C#, sin riesgo de romper la app | Sin feedback de app durante 3 sprints: si el schema no sirve, el error se descubre tarde y caro |
| Las historias de UI se escriben sobre un schema estable | `US-25` (login) y `US-09` (ámbito) se demoran, y son la condición de seguridad de `US-18` |
| Cada ticket es verificable aislado | El CI sólo compila; nada valida el schema hasta que hay app |

**Descartada.** El orden intercalado de la §3 da el mismo resultado en menos tiempo total,
porque nunca se avanza en una capa esperando a la otra.

---

## 6. Riesgos

| Riesgo | Impacto | Mitigación |
| --- | --- | --- |
| La condición de carrera del sobrecupo no está probada | Sobreventa → pérdida de plata | Test de concurrencia en la Fase 4 (`§4`), con 20 requests simultáneos |
| El password `sa` de `App.config:5` está versionado | Acceso total a la base | Moverlo a `appsettings.json` + `.gitignore` en la Fase 0 |
| El `.exe` se publica en cada push | La app queda ejecutable por cualquiera con el repo | Por eso login y ámbito van en las Fases 1 y 3, no después |
| Los triggers `INSTEAD OF` enumeran columnas explícitas | Agregar una columna a `Turnos` rompe el `INSERT` en silencio | Nota explícita en `REF-10`; considerar `OUTPUT` o `INSTEAD OF` con `SELECT *` |
| No hay usuarios en la semilla | El sistema es inaccesible | Definir en Fase 1: seeder vs. credenciales de prueba |
| El seed no tiene horarios ni precios | Ninguna oficina puede recibir turnos | Migrar el seed en la Fase 2 con horarios L-V y precios |
| `Tipo` queda texto libre | Dos oficinas con `"privada"` y `"Privada"` son distintas | Se acepta explícitamente (decisión §6.5); si molesta, agregar el catálogo después |
| No hay canal de notificaciones definido | `US-13`, `US-16`, `US-22` avisan "al cliente" sin canal | Diferir la notificación sin romper el flujo; documentar como deuda (§7.4) |

---

## 7. Lo primero para arrancar

Si hubiera que hacer una sola cosa hoy, no sería `REF-01`. Sería:

> **Definir el seeder de usuarios y el mecanismo de hash de contraseñas.**

Sin eso, `US-25` no se puede implementar, y `US-25` bloquea la seguridad de `US-18`. Es
el único punto del plan que además de código necesita una decisión que todavía no está
tomada.

Segundo: separar `InitCoworking.sql`. Es trabajo mecánico, independiente de todo lo demás, y
cada día que pasa sin hacerlo agrega riesgo.
