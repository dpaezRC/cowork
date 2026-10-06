# 01 · Refactor de base de datos — 12 tickets

> Documento 2 de 4. Contexto en `00-analisis-funcional.md`, historias en
> `02-historias-de-usuario.md`.

---

## Cómo leer estos tickets

- **ID** — `REF-01` … `REF-12`. Ojo: el requisito funcional **R4** (tipo de oficina, fuera
  de scope) y el ticket **REF-04** (horarios) no son lo mismo.
- **Requisito** — qué punto del §3 de `00-analisis-funcional.md` cierra.
- **Invariantes** — las reglas que la base de datos impone por sí sola. Una app mal escrita
  no puede violarlas.
- **AC** — criterios de aceptación verificables.

**Regla general:** todo script debe ser idempotente, siguiendo el estilo ya usado en
`InitCoworking.sql` (`IF OBJECT_ID(...) IS NULL BEGIN ... END`), y ejecutarse dentro de una
transacción cuando haya `ALTER` sobre columnas indexadas.

---

# Sprint 0 · Datos base

These tres tickets no agregan dominio nuevo: arreglan lo que ya está mal. Van primero
porque todo lo demás los usa como cimiento.

---

## `REF-01` · Unicode y collation

**Requisito:** ninguno — corrige §4.1
**Prioridad:** alta (pérdida silenciosa de datos)
**Depende de:** nada

### Problema

`Edificios`, `Oficinas`, `Roles`, `EstadosTurno` y `Usuarios` usan `VARCHAR`, y la semilla
inserta literales `N'...'`. El prefijo `N` se descarta al convertir a `VARCHAR` y los
caracteres no-ASCII se pierden sin error.

### Cambios

```sql
-- Edificios
ALTER TABLE dbo.Edificios ALTER COLUMN Nombre    NVARCHAR(100) NOT NULL;
ALTER TABLE dbo.Edificios ALTER COLUMN Direccion NVARCHAR(255) NOT NULL;
ALTER TABLE dbo.Edificios ALTER COLUMN Ciudad    NVARCHAR(100) NOT NULL;

-- Oficinas
ALTER TABLE dbo.Oficinas ALTER COLUMN Nombre NVARCHAR(100) NOT NULL;
ALTER TABLE dbo.Oficinas ALTER COLUMN Tipo   NVARCHAR(100) NOT NULL;

-- Roles y EstadosTurno
ALTER TABLE dbo.Roles       ALTER COLUMN Nombre NVARCHAR(50) NOT NULL;
ALTER TABLE dbo.EstadosTurno ALTER COLUMN Nombre NVARCHAR(50) NOT NULL;

-- Usuarios
ALTER TABLE dbo.Usuarios ALTER COLUMN Nombre   NVARCHAR(100) NOT NULL;
ALTER TABLE dbo.Usuarios ALTER COLUMN Email    NVARCHAR(150) NOT NULL;
ALTER TABLE dbo.Usuarios ALTER COLUMN Telefono NVARCHAR(30)  NULL;
```

> **Todas las tablas creadas a partir de ahora usan `NVARCHAR` sin excepción**, para no
> repetir este ticket.

### Invariantes

- Ninguna columna de texto del dominio queda en `VARCHAR`.

### AC

- [ ] `INSERT` de `N'Edificio Ñandú'` se relee idéntico.
- [ ] Una búsqueda con `LIKE N'%ñandú%'` encuentra el registro.
- [ ] Ningún `CREATE TABLE` posterior declara `VARCHAR` en columnas de texto.

### Notas

Si alguna de estas columnas estuviera indexada, `ALTER COLUMN` falla. En el estado actual
sólo `IX_*` sobre columnas numéricas, así que debería aplicar limpio.

---

## `REF-02` · Endurecer `Usuarios`

**Requisito:** ninguno — corrige §4.2
**Prioridad:** media-alta
**Depende de:** `REF-01`

### Cambios

```sql
ALTER TABLE dbo.Usuarios ADD     AlgoritmoHash     VARCHAR(20)   NOT NULL DEFAULT 'PBKDF2-SHA256';
ALTER TABLE dbo.Usuarios ADD     Documento        VARCHAR(30)   NULL;
ALTER TABLE dbo.Usuarios ADD     Activo           BIT           NOT NULL DEFAULT 1;
ALTER TABLE dbo.Usuarios ADD     IntentosFallidos TINYINT       NOT NULL DEFAULT 0;
ALTER TABLE dbo.Usuarios ADD     BloqueadoHasta   DATETIME2     NULL;
ALTER TABLE dbo.Usuarios ADD     ActualizadoAt    DATETIME2     NOT NULL DEFAULT SYSDATETIME();

ALTER TABLE dbo.Usuarios ALTER COLUMN PasswordHash NVARCHAR(255) NOT NULL;

CREATE UNIQUE INDEX UX_Usuarios_Documento ON dbo.Usuarios(Documento)
    WHERE Documento IS NOT NULL;
```

### Invariantes

- Un usuario con `Activo = 0` no puede autenticarse (§`US-25`).
- 5 intentos fallidos bloquean la cuenta hasta `BloqueadoHasta`.
- `Documento` es único cuando existe: dos asistentes no pueden compartir identidad.
- `AlgoritmoHash` es obligatorio, así que un hash viejo queda siempre interpretable.

### AC

- [ ] `PasswordHash` es `NVARCHAR` y existe `AlgoritmoHash` con default.
- [ ] Insertar dos usuarios con el mismo `Documento` falla.
- [ ] Un usuario con `Activo = 0` falla el login aunque el hash sea correcto.

---

## `REF-03` · Precisión temporal

**Requisito:** ninguno — corrige §4.6 y prepara el cálculo de minutos
**Prioridad:** media
**Depende de:** nada

### Cambios

```sql
ALTER TABLE dbo.Turnos ALTER COLUMN HoraInicio       DATETIME2(0) NOT NULL;
ALTER TABLE dbo.Turnos ALTER COLUMN HoraFinEstimada  DATETIME2(0) NULL;
ALTER TABLE dbo.Turnos ALTER COLUMN HoraFinReal      DATETIME2(0) NULL;
ALTER TABLE dbo.Turnos ALTER COLUMN CreadoAt         DATETIME2(3) NOT NULL DEFAULT SYSDATETIME();
```

### Invariantes

- La facturación nunca depende de segundos ni de zona horaria del servidor.
- `DATEDIFF(MINUTE, HoraInicio, HoraFin)` es el único cálculo de minutos del sistema.

### AC

- [ ] `HoraFinEstimada` queda `NOT NULL` en `REF-10` (aquí sólo se ajusta la precisión).
- [ ] Un turno de 90 minutos a las 14:00 termina a las `15:30:00` exactos, sin segundos.
- [ ] Un turno que cruza un cambio de horario de verano sigue facturando minutos reales.

### Notas

La app debe calcular minutos con `DATEDIFF(MINUTE, ...)` y **nunca** con
`(Fin - Inicio).TotalMinutes` sin redondeo: los `DATETIME2(0)` eliminan los segundos, pero
el redondeo de `TIME(7)` a minutos tiene que ser explícito.

---

# Sprint 1 · Fundaciones del dominio

---

## `REF-04` · Horarios por oficina

**Requisito:** cierra **R5** y **R6**
**Prioridad:** alta
**Depende de:** `REF-01`

### Cambios

```sql
CREATE TABLE dbo.HorariosOficina (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    OficinaId     INT       NOT NULL,
    DiaSemana     TINYINT   NOT NULL,
    HoraApertura  TIME      NOT NULL,
    HoraCierre    TIME      NOT NULL,
    Activo        BIT       NOT NULL DEFAULT 1,
    CONSTRAINT FK_HorariosOficina_Oficina
        FOREIGN KEY (OficinaId) REFERENCES dbo.Oficinas(Id) ON DELETE CASCADE,
    CONSTRAINT CK_Horarios_Dia
        CHECK (DiaSemana BETWEEN 1 AND 7),
    CONSTRAINT CK_Horarios_Rango
        CHECK (HoraCierre > HoraApertura),
    CONSTRAINT UQ_Horarios_Oficina_Dia
        UNIQUE (OficinaId, DiaSemana)
);

CREATE TABLE dbo.HorariosExcepcion (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    OficinaId     INT          NOT NULL,
    Fecha         DATE         NOT NULL,
    Cerrada       BIT          NOT NULL DEFAULT 1,
    HoraApertura  TIME         NULL,
    HoraCierre    TIME         NULL,
    Motivo        NVARCHAR(150) NULL,
    CONSTRAINT FK_HorariosExcepcion_Oficina
        FOREIGN KEY (OficinaId) REFERENCES dbo.Oficinas(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_HorariosExcepcion
        UNIQUE (OficinaId, Fecha),
    CONSTRAINT CK_Excepcion_Rango
        CHECK (Cerrada = 1 OR (HoraApertura IS NOT NULL AND HoraCierre IS NOT NULL
                           AND HoraCierre > HoraApertura))
);
```

`DiaSemana` sigue la convención de SQL Server: `1` = lunes … `7` = domingo.

### Invariantes

- Una oficina no puede tener dos horarios para el mismo día.
- No se puede cerrar antes de abrir.
- Una excepción `Cerrada = 0` **debe** tener horario; si no, el `CHECK` la rechaza.
- **Una oficina sin ningún `HorariosOficina` activo no acepta turnos** — se valida en
  `dbo.usp_ReservarTurno` (`REF-11`).
- Una `HorariosExcepcion` pisa al horario semanal de ese día.

### AC

- [ ] Insertar dos filas con el mismo `(OficinaId, DiaSemana)` falla.
- [ ] Insertar `HoraCierre <= HoraApertura` falla.
- [ ] `Cerrada = 0` sin horario completo falla.
- [ ] Una excepción de cierre bloquea la reserva de ese día aunque el horario semanal esté
      abierto.
- [ ] Una oficina sin horarios no deja crear turnos.

### Notas

R6 se cierra acá por descarte, no por construcción: se decidió que el **único** límite de
duración es el horario de la oficina (decisión §6.6 del análisis). No agregar
`DuracionMaximaMinutos` a `Oficinas` salvo que el negocio lo pida.

---

## `REF-05` · Administradores de oficina

**Requisito:** cierra **R8**
**Prioridad:** alta
**Depende de:** `REF-01`

### Cambios

```sql
CREATE TABLE dbo.AdministradoresOficina (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId   INT NOT NULL,
    OficinaId   INT NOT NULL,
    EsPrincipal BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_AdminOficina_Usuario
        FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AdminOficina_Oficina
        FOREIGN KEY (OficinaId) REFERENCES dbo.Oficinas(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Admin_Oficina
        UNIQUE (UsuarioId, OficinaId)
);

CREATE NONCLUSTERED INDEX IX_AdminOficina_Oficina ON dbo.AdministradoresOficina(OficinaId);
```

### Trigger de validación de rol

Un `INSTEAD OF INSERT, UPDATE` que rechace la asignación si el usuario no tiene rol
`supervisor`:

```sql
CREATE TRIGGER dbo.TR_AdministradoresOficina_Rol
ON dbo.AdministradoresOficina
INSTEAD OF INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.Usuarios u ON u.Id = i.UsuarioId
        JOIN dbo.Roles r    ON r.Id = u.RolId
        WHERE r.Nombre <> N'supervisor'
    )
    BEGIN
        RAISERROR('Sólo los usuarios con rol supervisor pueden administrar una oficina.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    INSERT INTO dbo.AdministradoresOficina (UsuarioId, OficinaId, EsPrincipal)
    SELECT i.UsuarioId, i.OficinaId, i.EsPrincipal FROM inserted i;
END
```

### Invariantes

- Sólo usuarios con rol `supervisor` pueden estar asignados.
- Un usuario no se asigna dos veces a la misma oficina.
- **Una oficina sin al menos un administrador no puede tener `Activa = 1`.** Esto se valida
  con un trigger `AFTER UPDATE, INSERT` sobre `Oficinas`.
- `SupervisorEdificios` **no se toca**: es un ámbito distinto (decisión §6.4).

### AC

- [ ] Asignar un usuario con rol `cliente` falla con el mensaje del trigger.
- [ ] Asignar el mismo usuario dos veces a la misma oficina falla.
- [ ] Poner `Activa = 1` en una oficina sin administrador falla.
- [ ] Una oficina puede tener N administradores sin límite.

---

## `REF-06` · Precio por asiento y versionado

**Requisito:** cierra **R7**
**Prioridad:** alta
**Depende de:** `REF-01`

### Cambios

```sql
CREATE TABLE dbo.PreciosOficina (
    Id                   INT IDENTITY(1,1) PRIMARY KEY,
    OficinaId            INT           NOT NULL,
    PrecioAsientoMinuto  DECIMAL(10,4) NOT NULL,
    VigenteDesde         DATE          NOT NULL,
    VigenteHasta         DATE          NULL,
    Activo               BIT           NOT NULL DEFAULT 1,
    CONSTRAINT FK_PreciosOficina_Oficina
        FOREIGN KEY (OficinaId) REFERENCES dbo.Oficinas(Id) ON DELETE CASCADE,
    CONSTRAINT CK_Precios_NoNegativo CHECK (PrecioAsientoMinuto >= 0),
    CONSTRAINT CK_Precios_Vigencia
        CHECK (VigenteHasta IS NULL OR VigenteHasta > VigenteDesde)
);

-- Como máximo un precio vigente por oficina.
CREATE UNIQUE INDEX UX_PreciosOficina_Vigente ON dbo.PreciosOficina(OficinaId)
    WHERE VigenteHasta IS NULL AND Activo = 1;
```

`Oficinas.PrecioPorMinuto` **se conserva** como caché desnormalizada para no romper
`Form1.cs:39`, pero se marca deprecada. La fuente de verdad es `PreciosOficina`.

### Invariantes

- Exactamente un precio vigente por oficina: ni cero, ni dos.
- Ningún rango de vigencia con `VigenteHasta <= VigenteDesde`.
- El precio aplicado a un turno se congela en `Turnos.PrecioAsientoMinutoSnapshot`
  (`REF-10`), por lo que cambiar un precio **jamás** altera un turno histórico.

### AC

- [ ] Insertar dos precios con `VigenteHasta IS NULL` para la misma oficina falla.
- [ ] Consultar el precio vigente de una fecha devuelve **exactamente 1 fila**, siempre.
- [ ] Dar de alta un precio con `VigenteDesde` en el futuro no cambia el precio de hoy.
- [ ] Un turno de marzo conserva su importe después de cambiar el precio en junio.

---

# Sprint 2 · El núcleo: sobrecupo y facturación

---

## `REF-07` · Estados de turno y de validación

**Requisito:** cierra **R10**
**Prioridad:** alta
**Depende de:** `REF-01`

### Cambios — `EstadosTurno`

```sql
INSERT INTO dbo.EstadosTurno (Nombre) VALUES
    (N'pendiente'),        -- reserva creada, espera auditoría
    (N'aprobado'),         -- el administrador dio el OK
    (N'en_curso'),         -- el turno está en marcha
    (N'pendiente_cierre'), -- terminó, hay asistentes sin decidir
    (N'finalizado'),       -- cerrado y facturado
    (N'rechazado'),        -- el administrador lo rechazó
    (N'cancelado'),        -- lo canceló el cliente
    (N'vencido'),          -- nunca llegó
    (N'no_show');          -- verificación negativa

-- Reemplazar los estados originales.
DELETE FROM dbo.EstadosTurno WHERE Nombre IN (N'activo');
```

> **Migración de datos:** si existen turnos en estado `activo`, hay que mapearlos a
> `en_curso` **antes** de borrar. `DELETE FROM EstadosTurno` va protegido por la FK desde
> `Turnos.EstadoId`.

### Cambios — catálogo de validación

```sql
CREATE TABLE dbo.EstadosValidacion (
    Id     INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL UNIQUE
);

INSERT INTO dbo.EstadosValidacion (Nombre) VALUES
    (N'ok'),         -- auditado, conforme
    (N'observado'),  -- conforme con observaciones
    (N'rechazado');

ALTER TABLE dbo.ValidacionesSupervisor
    ADD EstadoId INT NULL;

UPDATE dbo.ValidacionesSupervisor
SET EstadoId = (SELECT Id FROM dbo.EstadosValidacion WHERE Nombre = N'ok')
WHERE EstadoVerificacion IN ('ok', 'OK', 'aprobado');

ALTER TABLE dbo.ValidacionesSupervisor ALTER COLUMN EstadoId INT NOT NULL;

ALTER TABLE dbo.ValidacionesSupervisor
    ADD CONSTRAINT FK_Validaciones_Estado
        FOREIGN KEY (EstadoId) REFERENCES dbo.EstadosValidacion(Id);

-- Sólo una validación abierta por turno.
CREATE UNIQUE INDEX UX_Validaciones_TurnoAbierta
    ON dbo.ValidacionesSupervisor(TurnoId)
    WHERE EstadoId = (SELECT Id FROM dbo.EstadosValidacion WHERE Nombre = N'ok');

-- Deprecada.
ALTER TABLE dbo.ValidacionesSupervisor DROP COLUMN EstadoVerificacion;
```

> El índice filtrado con `WHERE EstadoId = (SELECT ...)` **no es válido en SQL Server**: un
> `WHERE` de índice no admite subconsulta. Hay que reemplazarlo por el Id literal, o por
> una columna calculada persistida:

```sql
-- Alternativa válida: columna calculada
ALTER TABLE dbo.ValidacionesSupervisor
    ADD EsCierre BIT AS CONVERT(BIT, CASE WHEN EstadoId = 2 THEN 1 ELSE 0 END) PERSISTED;

CREATE UNIQUE INDEX UX_Validaciones_TurnoAbierta
    ON dbo.ValidacionesSupervisor(TurnoId) WHERE EsCierre = 1;
```

### Máquina de estados

```
                    ┌──────────► rechazado ──────────┐
                    │                                 │
  [crear] ──► pendiente ──► aprobado ──► en_curso ──► pendiente_cierre ──► finalizado
                    │                                                        ▲
                    └──────────────── cancelado / vencido / no_show ──────────┘
```

### Invariantes

- Un turno `rechazado` **nunca** puede pasar a `aprobado` ni `en_curso` (trigger sobre
  `Turnos`).
- `cancelado`, `rechazado`, `vencido` y `no_show` son **terminales**: no admiten salida.
- Sólo un turno `aprobado` puede pasar a `en_curso`.
- Un turno no puede llegar a `finalizado` sin haber pasado por `pendiente_cierre`.
- `EstadoVerificacion` deja de ser texto libre.

### AC

- [ ] Llevar un turno `rechazado` a `en_curso` falla (error de transición).
- [ ] Un turno `cancelado` no puede pasar a ningún otro estado.
- [ ] Insertar dos validaciones `ok` para el mismo turno falla.
- [ ] `EstadoVerificacion` ya no existe en la tabla.

---

## `REF-08` · Asistencia y sobrecupo

**Requisito:** cierra **R11** — el ticket más importante del backlog
**Prioridad:** alta
**Depende de:** `REF-07`

### Cambios

```sql
CREATE TABLE dbo.AsistentesTurno (
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    TurnoId           INT          NOT NULL,
    Orden             SMALLINT     NOT NULL,
    ClienteId         INT          NULL,
    Nombre            NVARCHAR(100) NOT NULL,
    Documento         VARCHAR(30)   NULL,
    EsExceso          BIT          NOT NULL DEFAULT 0,
    Admitido          BIT          NULL,      -- NULL = pendiente de decisión
    MotivoRechazo     NVARCHAR(200) NULL,
    ImputadoAFactura  BIT          NOT NULL DEFAULT 0,
    DecididoPor       INT          NULL,
    DecididoAt        DATETIME2(3)  NULL,
    RegistradoPor     INT          NULL,
    CreadoAt          DATETIME2(3)  NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Asistentes_Turno
        FOREIGN KEY (TurnoId) REFERENCES dbo.Turnos(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Asistentes_Cliente
        FOREIGN KEY (ClienteId) REFERENCES dbo.Usuarios(Id),
    CONSTRAINT FK_Asistentes_DecididoPor
        FOREIGN KEY (DecididoPor) REFERENCES dbo.Usuarios(Id),
    CONSTRAINT FK_Asistentes_RegistradoPor
        FOREIGN KEY (RegistradoPor) REFERENCES dbo.Usuarios(Id),
    CONSTRAINT UQ_Asistente_Orden UNIQUE (TurnoId, Orden),
    -- Admitido implica facturado: nadie entra gratis.
    CONSTRAINT CK_Asistente_AdmitidoImputado
        CHECK (ImputadoAFactura = 0 OR Admitido = 1),
    -- Rechazado siempre con motivo.
    CONSTRAINT CK_Asistente_RechazoConMotivo
        CHECK (Admitido IS NULL OR Admitido = 1 OR MotivoRechazo IS NOT NULL),
    -- Si se decidió, tiene autor y fecha.
    CONSTRAINT CK_Asistente_DecisionAuditada
        CHECK (Admitido IS NULL OR (DecididoPor IS NOT NULL AND DecididoAt IS NOT NULL))
);
```

`Admitido` es **nullable a propósito**: representa tres estados.

| `Admitido` | Significado |
| --- | --- |
| `NULL` | Pendiente de decisión del administrador |
| `1` | Admitido → **obligatoriamente** facturado |
| `0` | Rechazado → motivo obligatorio, sin cargo |

### Procedimiento de admisión — donde vive el "si hay espacio"

```sql
CREATE PROCEDURE dbo.usp_AdmitirAsistente
    @AsistenteId INT,
    @DecididoPor INT,
    @Motivo      NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @TurnoId INT, @EsExceso BIT, @OficinaId INT,
            @Capacidad INT, @Admitidos INT, @EsDecisivo BIT;

    -- UPDLOCK: serializa las decisiones concurrentes sobre el mismo turno,
    -- que es donde está la condición de carrera.
    SELECT  @TurnoId = a.TurnoId,
            @EsExceso = a.EsExceso,
            @OficinaId = t.OficinaId,
            @EsDecisivo = CASE WHEN a.Admitido IS NULL THEN 1 ELSE 0 END
    FROM    dbo.AsistentesTurno a WITH (UPDLOCK, ROWLOCK)
            INNER JOIN dbo.Turnos t ON t.Id = a.TurnoId
    WHERE   a.Id = @AsistenteId;

    IF @TurnoId IS NULL
        THROW 50001, 'El asistente no existe.', 1;

    IF @EsDecisivo = 0
        THROW 50002, 'La decisión ya fue tomada.', 1;

    -- Si es un rechazo, el motivo es obligatorio (el CHECK también lo exige).
    IF @Motivo IS NULL AND @EsExceso = 1
        THROW 50003, 'Rechazar un excedente exige un motivo.', 1;

    -- "Si hay espacio": se cuenta contra la capacidad de la oficina.
    SELECT @Capacidad = o.CapacidadTotal
    FROM   dbo.Oficinas o WITH (UPDLOCK, HOLDLOCK)
    WHERE  o.Id = @OficinaId;

    SELECT @Admitidos = COUNT(*)
    FROM   dbo.AsistentesTurno
    WHERE  TurnoId = @TurnoId AND Admitido = 1;

    IF @Admitidos >= @Capacidad
        THROW 50004, 'La oficina no tiene espacio disponible para admitir a otra persona.', 1;

    UPDATE dbo.AsistentesTurno
    SET    Admitido         = 1,
           MotivoRechazo    = NULL,
           ImputadoAFactura = 1,   -- admitido implica facturado
           DecididoPor      = @DecididoPor,
           DecididoAt       = SYSDATETIME()
    WHERE  Id = @AsistenteId;

    COMMIT TRANSACTION;
END
```

El procedimiento gemelo `dbo.usp_RechazarAsistente` exige `Motivo` y deja
`ImputadoAFactura = 0`.

### Invariantes

Éstas son **la traducción literal del requerimiento R11**, y las impone la base, no la app:

| Invariante | Cómo se garantiza |
| --- | --- |
| Admitido ⇒ se imputa a la factura | `CK_Asistente_AdmitidoImputado` |
| Rechazado ⇒ motivo obligatorio | `CK_Asistente_RechazoConMotivo` |
| Toda decisión tiene autor y fecha | `CK_Asistente_DecisionAuditada` |
| **Nadie por encima de la capacidad** | `usp_AdmitirAsistente` con `UPDLOCK, HOLDLOCK` |
| Un rejected nunca genera cargo | `ImputadoAFactura = 0` en el rechazo |
| Orden correlativo por turno | `UQ_Asistente_Orden` |

Además, en `REF-09`:

> **No se puede cerrar un turno con asistentes sin decidir.** Si existe alguna fila con
> `Admitido IS NULL`, el cierre falla. El turno queda en `pendiente_cierre`.

### AC

- [ ] Insertar un asistente con `Admitido = 1` e `ImputadoAFactura = 0` → falla el `CHECK`.
- [ ] Insertar un asistente con `Admitido = 0` y `MotivoRechazo = NULL` → falla el `CHECK`.
- [ ] Insertar un asistente con `Admitido = 1` sin `DecididoPor` → falla el `CHECK`.
- [ ] Admitir cuando la oficina ya tiene `Admitidos = CapacidadTotal` → error 50004.
- [ ] Registrar `CantidadAsientos + 1` asistentes: el último queda automáticamente con
      `EsExceso = 1` y `Admitido = NULL` (lógica de la app, ver `US-17`).
- [ ] 2 decisiones concurrentes sobre una oficina con 1 lugar libre → exactamente 1 admite,
      la otra recibe el error 50004.

---

## `REF-09` · Facturación, una factura por turno

**Requisito:** cierra **R12**
**Prioridad:** alta
**Depende de:** `REF-08`, `REF-10`

### Cambios

> **Orden de creación:** `EstadosFactura` va **primero**, porque `Facturas.EstadoId` la
> referencia en su `CONSTRAINT`.

```sql
CREATE TABLE dbo.EstadosFactura (
    Id     INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(30) NOT NULL UNIQUE
);

INSERT INTO dbo.EstadosFactura (Nombre) VALUES
    (N'pendiente'), (N'emitida'), (N'pagada'), (N'anulada');

CREATE TABLE dbo.Facturas (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    TurnoId         INT           NOT NULL,
    Numero          NVARCHAR(20)  NOT NULL,
    ClienteId       INT           NOT NULL,
    FechaEmision    DATE          NOT NULL CONSTRAINT DF_Facturas_Fecha
                    DEFAULT CONVERT(DATE, SYSDATETIME()),
    AsientosBase    INT           NOT NULL,
    MinutosBase     INT           NOT NULL,
    ImporteBase     DECIMAL(12,2) NOT NULL,
    AsientosExceso  INT           NOT NULL CONSTRAINT DF_Facturas_Exceso DEFAULT 0,
    ImporteExceso   DECIMAL(12,2) NOT NULL CONSTRAINT DF_Facturas_ImpExceso DEFAULT 0.00,
    Total           DECIMAL(12,2) NOT NULL,
    EstadoId        INT           NOT NULL,
    CreadoAt        DATETIME2(3)  NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Facturas_Turno
        FOREIGN KEY (TurnoId) REFERENCES dbo.Turnos(Id),
    CONSTRAINT FK_Facturas_Cliente
        FOREIGN KEY (ClienteId) REFERENCES dbo.Usuarios(Id),
    CONSTRAINT FK_Facturas_Estado
        FOREIGN KEY (EstadoId) REFERENCES dbo.EstadosFactura(Id),
    CONSTRAINT UQ_Facturas_Turno   UNIQUE (TurnoId),
    CONSTRAINT UQ_Facturas_Numero  UNIQUE (Numero),
    CONSTRAINT CK_Facturas_Totales CHECK (Total = ImporteBase + ImporteExceso)
);
```

`FacturaItems` queda **fuera de alcance** (decisión §6.3): con una factura por turno, el
desglose cabe en dos columnas.

### Fórmulas

Con un solo precio (decisión §6.7) y minutos compartidos (decisión §6.8):

```sql
MinutosFacturables = DATEDIFF(MINUTE, t.HoraInicio,
                              COALESCE(t.HoraFinReal, t.HoraFinEstimada))

AsientosBase   = t.CantidadAsientos
ImporteBase    = AsientosBase   * MinutosFacturables * t.PrecioAsientoMinutoSnapshot

AsientosExceso = COUNT(a.Id) WHERE a.EsExceso = 1 AND a.Admitido = 1
ImporteExceso  = AsientosExceso * MinutosFacturables * t.PrecioAsientoMinutoSnapshot

Total          = ImporteBase + ImporteExceso
```

### Procedimiento de cierre

```sql
CREATE PROCEDURE dbo.usp_FinalizarTurno
    @TurnoId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    -- Guard: no cerrar con asistentes sin decidir.
    IF EXISTS (SELECT 1 FROM dbo.AsistentesTurno
               WHERE TurnoId = @TurnoId AND Admitido IS NULL)
        THROW 50005, 'No se puede finalizar: hay asistentes pendientes de decisión.', 1;

    DECLARE @ClienteId INT, @Cantidad INT, @Precio DECIMAL(10,4),
            @Minutos INT, @Exceso INT, @Inicio DATETIME2(0),
            @FinEstimada DATETIME2(0), @FinReal DATETIME2(0), @Fecha DATE;

    SELECT  @ClienteId   = t.ClienteId,
            @Cantidad    = t.CantidadAsientos,
            @Precio      = t.PrecioAsientoMinutoSnapshot,
            @Inicio      = t.HoraInicio,
            @FinEstimada = t.HoraFinEstimada,
            @FinReal     = t.HoraFinReal,
            @Fecha       = CONVERT(DATE, t.HoraFinEstimada)
    FROM    dbo.Turnos t WITH (UPDLOCK, ROWLOCK)
    WHERE   t.Id = @TurnoId;

    SET @Minutos = DATEDIFF(MINUTE, @Inicio, COALESCE(@FinReal, @FinEstimada));

    SELECT @Exceso = COUNT(*)
    FROM   dbo.AsistentesTurno
    WHERE  TurnoId = @TurnoId AND EsExceso = 1 AND Admitido = 1;

    INSERT INTO dbo.Facturas
        (TurnoId, Numero, ClienteId, FechaEmision,
         AsientosBase, MinutosBase, ImporteBase,
         AsientosExceso, ImporteExceso, Total, EstadoId)
    VALUES
        (@TurnoId,
         CONCAT('F-', FORMAT(@Fecha, 'yyyyMMdd'), '-', RIGHT('000000' + CAST(@TurnoId AS VARCHAR), 6)),
         @ClienteId, @Fecha,
         @Cantidad,  @Minutos,
         CAST(@Cantidad * @Minutos * @Precio AS DECIMAL(12,2)),
         @Exceso,    @Minutos,
         CAST(@Exceso   * @Minutos * @Precio AS DECIMAL(12,2)),
         CAST((@Cantidad + @Exceso) * @Minutos * @Precio AS DECIMAL(12,2)),
         (SELECT Id FROM dbo.EstadosFactura WHERE Nombre = N'pendiente'));

    UPDATE dbo.Turnos
    SET    EstadoId            = (SELECT Id FROM dbo.EstadosTurno WHERE Nombre = N'finalizado'),
           MinutosFacturables  = @Minutos,
           AsientosFacturados  = @Cantidad + @Exceso,
           ImporteExceso       = CAST(@Exceso * @Minutos * @Precio AS DECIMAL(12,2)),
           HoraFinReal         = COALESCE(@FinReal, @FinEstimada),
           MontoTotal          = CAST((@Cantidad + @Exceso) * @Minutos * @Precio AS DECIMAL(12,2))
    WHERE  Id = @TurnoId;

    COMMIT TRANSACTION;
END
```

### Inmutabilidad de la factura emitida

```sql
CREATE TRIGGER dbo.TR_Facturas_EmitidaInmutable
ON dbo.Facturas
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Una factura emitida o pagada SÓLO puede pasar a 'anulada'.
    -- En un DELETE no hay filas en inserted, así que el NOT EXISTS da TRUE
    -- y borrar una factura emitida también falla.
    IF EXISTS (
        SELECT 1
        FROM deleted d
        JOIN dbo.EstadosFactura e ON e.Id = d.EstadoId
        WHERE e.Nombre IN (N'emитida', N'pagada')
          AND NOT EXISTS (
              SELECT 1
              FROM inserted i
              JOIN dbo.EstadosFactura e2 ON e2.Id = i.EstadoId
              WHERE i.Id = d.Id AND e2.Nombre = N'anulada'
          )
    )
    BEGIN
        RAISERROR('Una factura emitida o pagada no se puede modificar ni borrar. '
                  + 'Sólo puede anularse.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END
```

| Operación sobre factura `emitida` | Resultado |
| --- | --- |
| `UPDATE Total = ...` | **Falla** — no hay transición a `anulada` |
| `UPDATE EstadoId → anulada` | Permitido |
| `DELETE` | **Falla** — `inserted` está vacío |
| `UPDATE` sobre factura `pendiente` | Permitido |

> **Por qué `AFTER` y no `INSTEAD OF`:** un trigger `INSTEAD OF UPDATE` tendría que
> reescribir la actualización completa a mano y es fácil olvidar una columna. `AFTER` con
> `ROLLBACK` es más simple y cubre `DELETE` sin necesidad de un segundo trigger.


### Invariantes

- **Un turno no puede tener dos facturas** (`UQ_Facturas_Turno`).
- `Total = ImporteBase + ImporteExceso`, verificado por `CHECK` — el desglose nunca puede
  dejar de cuadrar.
- `AsientosExceso` cuenta **sólo** excedentes con `Admitido = 1`: los rechazados no se
  facturan jamás.
- **No se cierra un turno con asistentes sin decidir** (error 50005).
- Una factura `emitida` o `pagada` es inmutable: sólo puede pasar a `anulada`.
- Los minutos del excedente son los mismos que los de la base (decisión §6.8), por lo que
  un turno alargado cobra el excedente más caro. **Consecuencia aceptada
  conscientemente.**

### AC

- [ ] `usp_FinalizarTurno` sobre un turno con un asistente `Admitido = NULL` falla con 50005.
- [ ] Un turno sin excedentes genera factura con `AsientosExceso = 0` e
      `ImporteExceso = 0.00`.
- [ ] Un turno con 2 excedentes admitidos y 1 rechazado factura exactamente 2 extras.
- [ ] Intentar `UPDATE` sobre una factura `emitida` falla.
- [ ] Anular una factura emitida es la única operación permitida.
- [ ] `Total` nunca puede no cuadrar con el desglose.
- [ ] Reejecutar `usp_FinalizarTurno` sobre el mismo turno falla por `UQ_Facturas_Turno`.

---

## `REF-10` · Snapshot de precio e integridad de `Turnos`

**Requisito:** apoya R7, R9 y R12
**Prioridad:** alta
**Depende de:** `REF-06`, `REF-04`

### Cambios

```sql
ALTER TABLE dbo.Turnos ADD PrecioAsientoMinutoSnapshot DECIMAL(10,4) NOT NULL
    CONSTRAINT DF_Turnos_Precio DEFAULT 0;
ALTER TABLE dbo.Turnos ADD MinutosFacturables INT NOT NULL
    CONSTRAINT DF_Turnos_Minutos DEFAULT 0;
ALTER TABLE dbo.Turnos ADD AsientosReservados   INT NOT NULL
    CONSTRAINT DF_Turnos_Reservados DEFAULT 0;
ALTER TABLE dbo.Turnos ADD AsientosFacturados   INT NOT NULL
    CONSTRAINT DF_Turnos_Facturados DEFAULT 0;
ALTER TABLE dbo.Turnos ADD ImporteExceso DECIMAL(12,2) NOT NULL
    CONSTRAINT DF_Turnos_ImpExceso DEFAULT 0.00;

-- HoraFinEstimada deja de ser nullable (corrige §4.6).
ALTER TABLE dbo.Turnos ALTER COLUMN HoraFinEstimada DATETIME2(0) NOT NULL;

ALTER TABLE dbo.Turnos ADD CONSTRAINT CK_Turnos_RangoHorario
    CHECK (HoraFinEstimada > HoraInicio);
ALTER TABLE dbo.Turnos ADD CONSTRAINT CK_Turnos_Asientos
    CHECK (CantidadAsientos > 0);
ALTER TABLE dbo.Turnos ADD CONSTRAINT CK_Turnos_Minutos
    CHECK (MinutosFacturables >= 0);
```

`MontoTotal` pasa a ser un valor **calculado por el procedure de cierre**, no escrito a
mano por la UI. No se la convierte en columna computada porque depende de
`AsistentesTurno` y eso SQL Server no lo resuelve con un `COMPUTED COLUMN` simple.

### Guardas de integridad sobre `Turnos`

Un `CHECK` de SQL Server no puede cruzar tablas, así que la comparación contra
`CapacidadTotal` y la máquina de estados se resuelven con triggers.

> ⚠️ **El trigger debe ser `INSTEAD OF INSERT`, nunca `INSTEAD OF INSERT, UPDATE`.**
> `usp_FinalizarTurno` (`REF-09`) hace un `UPDATE` sobre `Turnos` para cerrar el turno. Con
> un `INSTEAD OF INSERT, UPDATE`, ese `UPDATE` se traga la operación y la reemplaza por el
> `INSERT` del cuerpo del trigger: se insertaría un **turno duplicado** y la factura quedaría
> apuntando al turno viejo. El síntoma sería un fallo de `UQ_Facturas_Turno` o, peor,
> importes que no cuadran sin error visible.

**Guarda 1 — capacidad al crear un turno:**

```sql
CREATE TRIGGER dbo.TR_Turnos_Capacidad
ON dbo.Turnos
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE i.CantidadAsientos > (SELECT o.CapacidadTotal
                                    FROM dbo.Oficinas o WHERE o.Id = i.OficinaId)
    )
    BEGIN
        RAISERROR('La cantidad de asientos supera la capacidad de la oficina.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    INSERT INTO dbo.Turnos
        (ClienteId, OficinaId, CantidadAsientos, HoraInicio, HoraFinEstimada,
         EstadoId, PrecioAsientoMinutoSnapshot, AsientosReservados, CreadoAt)
    SELECT i.ClienteId, i.OficinaId, i.CantidadAsientos, i.HoraInicio,
           i.HoraFinEstimada, i.EstadoId, i.PrecioAsientoMinutoSnapshot,
           i.CantidadAsientos, SYSDATETIME()
    FROM inserted i;
END
```

**Guarda 2 — máquina de estados (`AFTER UPDATE`, no intercepta nada):**

```sql
CREATE TRIGGER dbo.TR_Turnos_Estados
ON dbo.Turnos
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Pendiente    INT = (SELECT Id FROM dbo.EstadosTurno WHERE Nombre = N'pendiente'),
            @Aprobado     INT = (SELECT Id FROM dbo.EstadosTurno WHERE Nombre = N'aprobado'),
            @EnCurso      INT = (SELECT Id FROM dbo.EstadosTurno WHERE Nombre = N'en_curso'),
            @PendCierre   INT = (SELECT Id FROM dbo.EstadosTurno WHERE Nombre = N'pendiente_cierre'),
            @Finalizado   INT = (SELECT Id FROM dbo.EstadosTurno WHERE Nombre = N'finalizado');

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.Id = i.Id
        -- Los estados terminales no admiten salida.
        WHERE d.EstadoId IN (@Pendiente, @Finalizado) AND i.EstadoId <> d.EstadoId
        -- No se puede pasar a en_curso sin haber sido aprobado.
           OR (i.EstadoId = @EnCurso AND d.EstadoId <> @Aprobado)
        -- No se puede finalizar sin pasar por pendiente_cierre.
           OR (i.EstadoId = @Finalizado AND d.EstadoId <> @PendCierre)
    )
    BEGIN
        RAISERROR('Transición de estado de turno no permitida.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END
```

**Guarda 3 — reducir la capacidad de una oficina:**

```sql
CREATE TRIGGER dbo.TR_Oficinas_Capacidad
ON dbo.Oficinas
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.Id = i.Id
        WHERE i.CapacidadTotal < d.CapacidadTotal
          AND EXISTS (
              SELECT 1
              FROM dbo.Turnos t
              JOIN dbo.EstadosTurno e ON e.Id = t.EstadoId
              WHERE t.OficinaId = i.Id
                AND t.CantidadAsientos > i.CapacidadTotal
                AND e.Nombre IN (N'pendiente', N'aprobado', N'en_curso', N'pendiente_cierre')
          )
    )
    BEGIN
        RAISERROR('No se puede reducir la capacidad: hay turnos vigentes que la superan.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END
```

> **Regla general para triggers `INSTEAD OF` en este schema:** enumeran columnas de forma
> explícita, así que cualquier columna nueva requiere actualizarlos o el `INSERT` se
> completa con valores por defecto en silencio. Preferí `AFTER` + `ROLLBACK` salvo que haya
> que impedir la operación (como el `INSERT` de `Turnos`, que no tiene alternativa).

### Invariantes

- **El precio de un turno es inmutable.** Una vez creado, `PrecioAsientoMinutoSnapshot` no
  se recalcula jamás.
- `HoraFinEstimada > HoraInicio`, siempre.
- `CantidadAsientos <= CapacidadTotal` de la oficina.
- `AsientosFacturados >= AsientosReservados` (sólo crece, por excedentes admitidos).
- `MinutosFacturables` se fija en el cierre y no se recalcula.

### AC

- [ ] Cambiar `PreciosOficina` para 2027 no altera un céntimo de un turno de 2026.
- [ ] `HoraFinEstimada <= HoraInicio` falla.
- [ ] Reservar más asientos que la capacidad de la oficina falla.
- [ ] `AsientosFacturados < AsientosReservados` falla.

---

## `REF-11` · Guardas de disponibilidad

**Requisito:** cierra **R9**
**Prioridad:** alta
**Depende de:** `REF-04`, `REF-05`, `REF-10`

### Índice

```sql
CREATE NONCLUSTERED INDEX IX_Turnos_Oficina_Horas
    ON dbo.Turnos(OficinaId, HoraInicio, HoraFinEstimada)
    INCLUDE (CantidadAsientos, EstadoId);
```

Cubre la consulta de solapamiento y la de disponibilidad, sin cubrir la tabla completa.

### Estados bloqueantes

Un turno consume un asiento mientras está `pendiente`, `aprobado`, `en_curso` o
`pendiente_cierre`. `cancelado`, `rechazado`, `vencido`, `no_show` y `finalizado` liberan.

```sql
DECLARE @EstadosBloqueantes TABLE (Id INT PRIMARY KEY);
INSERT INTO @EstadosBloqueantes (Id)
SELECT Id FROM dbo.EstadosTurno
WHERE Nombre IN (N'pendiente', N'aprobado', N'en_curso', N'pendiente_cierre');
```

### Procedimiento de reserva

```sql
CREATE PROCEDURE dbo.usp_ReservarTurno
    @ClienteId           INT,
    @OficinaId           INT,
    @CantidadAsientos    INT,
    @HoraInicio          DATETIME2(0),
    @HoraFinEstimada     DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @Capacidad INT, @Ocupados INT, @Precio DECIMAL(10,4), @TurnoId INT;

    -- UPDLOCK,HOLDLOCK sobre la oficina: es la fila que todos los que compiten
    -- por un asiento de esta oficina necesitan. Serializa la validación.
    SELECT @Capacidad = o.CapacidadTotal
    FROM   dbo.Oficinas o WITH (UPDLOCK, HOLDLOCK)
    WHERE  o.Id = @OficinaId AND o.Activa = 1;

    IF @Capacidad IS NULL
        THROW 50010, 'La oficina no existe o no está activa.', 1;

    -- (a) Tiene al menos un administrador.
    IF NOT EXISTS (SELECT 1 FROM dbo.AdministradoresOficina WHERE OficinaId = @OficinaId)
        THROW 50011, 'La oficina no tiene administrador asignado.', 1;

    -- (b) Hay horario activo para ese día de la semana.
    DECLARE @Dia TINYINT = DATEPART(WEEKDAY, @HoraInicio);
    IF NOT EXISTS (SELECT 1 FROM dbo.HorariosOficina
                   WHERE OficinaId = @OficinaId AND DiaSemana = @Dia AND Activo = 1)
        THROW 50012, 'La oficina no atiende ese día.', 1;

    -- (c) No es día de excepción cerrada.
    IF EXISTS (SELECT 1 FROM dbo.HorariosExcepcion
               WHERE OficinaId = @OficinaId
                 AND Fecha = CONVERT(DATE, @HoraInicio) AND Cerrada = 1)
        THROW 50013, 'La oficina está cerrada por excepción en esa fecha.', 1;

    -- (d) La franja completa cae dentro del horario (o de la excepción reducida).
    DECLARE @Apertura TIME, @Cierre TIME;

    SELECT TOP 1 @Apertura = ISNULL(e.HoraApertura, h.HoraApertura),
                  @Cierre   = ISNULL(e.HoraCierre,   h.HoraCierre)
    FROM   dbo.HorariosOficina h
    LEFT   JOIN dbo.HorariosExcepcion e
           ON e.OficinaId = h.OficinaId
          AND e.Fecha = CONVERT(DATE, @HoraInicio)
    WHERE  h.OficinaId = @OficinaId AND h.DiaSemana = @Dia AND h.Activo = 1;

    IF @Apertura IS NULL
        THROW 50014, 'La oficina no tiene horario vigente para esa fecha.', 1;

    IF CAST(@HoraInicio  AS TIME) < @Apertura
        OR CAST(@HoraFinEstimada AS TIME) > @Cierre
        OR @HoraFinEstimada <= @HoraInicio
        THROW 50015, 'La franja horaria cae fuera del horario de atención.', 1;

    -- (e) Capacidad disponible. Ésta es la condición de carrera real.
    SELECT @Ocupados = ISNULL(SUM(t.CantidadAsientos), 0)
    FROM   dbo.Turnos t
    JOIN   @EstadosBloqueantes b ON b.Id = t.EstadoId
    WHERE  t.OficinaId = @OficinaId
      AND  t.HoraInicio       < @HoraFinEstimada      -- solapamiento
      AND  t.HoraFinEstimada  > @HoraInicio;

    IF @Ocupados + @CantidadAsientos > @Capacidad
        THROW 50016, 'No hay suficientes asientos disponibles en esa franja.', 1;

    -- (f) Congelar el precio vigente. Un solo precio (decisión §6.7).
    SELECT TOP 1 @Precio = p.PrecioAsientoMinuto
    FROM   dbo.PreciosOficina p
    WHERE  p.OficinaId = @OficinaId AND p.Activo = 1
      AND  p.VigenteDesde <= CONVERT(DATE, @HoraInicio)
      AND  (p.VigenteHasta IS NULL OR p.VigenteHasta >= CONVERT(DATE, @HoraInicio))
    ORDER  BY p.VigenteDesde DESC;

    IF @Precio IS NULL
        THROW 50017, 'La oficina no tiene un precio vigente para esa fecha.', 1;

    INSERT INTO dbo.Turnos
        (ClienteId, OficinaId, CantidadAsientos, HoraInicio, HoraFinEstimada,
         EstadoId, PrecioAsientoMinutoSnapshot, AsientosReservados, MinutosFacturables)
    VALUES
        (@ClienteId, @OficinaId, @CantidadAsientos, @HoraInicio, @HoraFinEstimada,
         (SELECT Id FROM dbo.EstadosTurno WHERE Nombre = N'pendiente'),
         @Precio, @CantidadAsientos,
         DATEDIFF(MINUTE, @HoraInicio, @HoraFinEstimada));

    SET @TurnoId = SCOPE_IDENTITY();

    COMMIT TRANSACTION;
    RETURN @TurnoId;
END
```

### Invariantes

Una reserva **sólo** se persiste si se cumplen las seis condiciones:

1. La oficina existe y está activa.
2. Tiene al menos un administrador asignado.
3. Atiende el día de la semana de la reserva.
4. No es día de excepción cerrada.
5. La franja completa cabe dentro del horario vigente.
6. Los turnos solapados más lo nuevo no superan `CapacidadTotal`.

Y el precio queda congelado en el mismo `INSERT`.

### AC

- [ ] Reservar fuera del horario de atención falla con 50015.
- [ ] Reservar en un día de excepción cerrada falla con 50013.
- [ ] Reservar en una oficina sin administrador falla con 50011.
- [ ] Reservar en una oficina sin horario activo falla con 50012.
- [ ] Reservar sin precio vigente falla con 50017.
- [ ] **20 requests concurrentes sobre una oficina de 6 asientos: exactamente 6 se aceptan**
      y 14 reciben el error 50016.
- [ ] El turno creado queda `pendiente` y con el snapshot de precio escrito.
- [ ] La consulta de solapamiento usa `IX_Turnos_Oficina_Horas`.

---

## `REF-12` · Migraciones versionadas

**Requisito:** ninguno — habilita todos los anteriores
**Prioridad:** media
**Depende de:** todos

### Cambios

Partir el monolito `InitCoworking.sql` (324 líneas) en scripts numerados:

```
CoworkApp/Scripts/
  001_Schema_Base.sql          -- tablas originales, sin cambios
  002_Seed.sql                 -- roles, estados, edificios, pisos, oficinas
  003_Refactor_Unicode.sql     -- REF-01, REF-02, REF-03
  004_HorariosOficina.sql      -- REF-04
  005_AdministradoresOficina.sql -- REF-05
  006_PreciosOficina.sql       -- REF-06
  007_EstadosValidacion.sql    -- REF-07
  008_AsistentesTurno.sql      -- REF-08
  009_Facturas.sql             -- REF-09, REF-10
  010_GuardasDisponibilidad.sql -- REF-11
  011_Verificacion.sql         -- sanity checks finales
```

### Tabla de control

```sql
CREATE TABLE dbo.SchemaVersion (
    Version    VARCHAR(20)    NOT NULL PRIMARY KEY,
    AppliedAt  DATETIME2(3)   NOT NULL CONSTRAINT DF_SchemaVersion_Applied
                DEFAULT SYSDATETIME(),
    Checksum   VARBINARY(32)  NOT NULL
);
```

Un runner aplica los scripts pendientes en orden y registra `Version` + `Checksum`. Si el
checksum de un script ya aplicado cambia, el runner **falla**: alguien editó un script
histórico.

### Invariantes

- Correr el runner dos veces no cambia nada.
- Un script nunca se modifica una vez aplicado; se corrige con un script nuevo.
- Los scripts son idempotentes por sí mismos (`IF OBJECT_ID(...) IS NULL`), así se pueden
  reejecutar a mano sin romper nada.

### AC

- [ ] Correr el runner dos veces sobre la misma base no falla ni duplica datos.
- [ ] Editar un script ya aplicado y volver a correr el runner falla por checksum.
- [ ] Una base nueva llega al estado final corriendo sólo los scripts, sin pasos manuales.
- [ ] `InitCoworking.sql` deja de existir como monolito.

---

## Grafo de dependencias

```
REF-01 ─┬─► REF-02 ─┐
REF-03 ─┘            ├─► REF-04 ─┬─► REF-05 ─┐
                       └─► REF-06 ─┘         │
                                              ├─► REF-11 ─┐
                        REF-07 ──► REF-08 ─┐   │           │
                                          ├─► REF-09 ─┐   │
                                 REF-10 ───┘           │   │
                                                      ▼   ▼
                                                   REF-12
```

| Sprint | Tickets | Entregable |
| --- | --- | --- |
| 0 | `REF-01`, `REF-02`, `REF-03` | Datos correctos y seguros |
| 1 | `REF-04`, `REF-05`, `REF-06` | Dominio completo: horarios, admins, precios |
| 2 | `REF-07`, `REF-08` | Estados y sobrecupo |
| 3 | `REF-09`, `REF-10` | Facturación e integridad |
| 4 | `REF-11`, `REF-12` | Concurrencia segura y migraciones |

---

## Checklist de verificación global

- [ ] Ninguna columna de texto del dominio es `VARCHAR`.
- [ ] Ningún estado es texto libre.
- [ ] `Turnos.PrecioAsientoMinutoSnapshot` existe y no se recalcula.
- [ ] Existe `IX_Turnos_Oficina_Horas`.
- [ ] La capacidad no se puede superar ni por trigger ni por procedimiento.
- [ ] No se puede cerrar un turno con asistentes sin decidir.
- [ ] Un rechazo nunca genera cargo.
- [ ] Una factura emitida es inmutable.
- [ ] Todos los scripts son idempotentes.
