/* ============================================================================
   Sistema de gestion de espacios de coworking
   Esquema fisico - SQL Server 2019+ / Azure SQL
   Base: TSDS-Taller_ii_Coworking_1.1.0
   Zona horaria de operacion: America/Argentina/Tucuman (UTC-3, sin DST)
   Asume una base vacia: no es idempotente.
   ============================================================================ */

SET NOCOUNT ON;


/* ---------------------------------------------------------------- Edificio */
CREATE TABLE dbo.Edificio (
    IdEdificio  INT IDENTITY(1,1) NOT NULL,
    Nombre      NVARCHAR(100)     NOT NULL,
    Direccion   NVARCHAR(200)     NOT NULL,
    Estado      VARCHAR(10)       NOT NULL CONSTRAINT DF_Edificio_Estado    DEFAULT ('Activo'),
    FechaAlta   DATETIME2(0)      NOT NULL CONSTRAINT DF_Edificio_FechaAlta DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Edificio        PRIMARY KEY (IdEdificio),
    CONSTRAINT UQ_Edificio_Nombre UNIQUE (Nombre),
    CONSTRAINT CK_Edificio_Estado CHECK (Estado IN ('Activo','Inactivo'))
);


/* ------------------------------------------------------------------- Piso */
CREATE TABLE dbo.Piso (
    IdPiso      INT IDENTITY(1,1) NOT NULL,
    IdEdificio  INT               NOT NULL,
    Nombre      NVARCHAR(50)      NOT NULL,
    Estado      VARCHAR(10)       NOT NULL CONSTRAINT DF_Piso_Estado DEFAULT ('Activo'),
    CONSTRAINT PK_Piso                  PRIMARY KEY (IdPiso),
    CONSTRAINT UQ_Piso_Edificio_Nombre  UNIQUE (IdEdificio, Nombre),
    CONSTRAINT CK_Piso_Estado           CHECK (Estado IN ('Activo','Inactivo')),
    CONSTRAINT FK_Piso_Edificio         FOREIGN KEY (IdEdificio) REFERENCES dbo.Edificio (IdEdificio)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);


/* ---------------------------------------------------------------- Oficina */
CREATE TABLE dbo.Oficina (
    IdOficina     INT IDENTITY(1,1) NOT NULL,
    IdPiso        INT               NOT NULL,
    Nombre        NVARCHAR(100)     NOT NULL,
    Capacidad     INT               NOT NULL,
    TarifaHora    DECIMAL(18,2)     NOT NULL,
    HoraApertura  TIME(0)           NOT NULL CONSTRAINT DF_Oficina_HoraApertura DEFAULT ('08:00'),
    HoraCierre    TIME(0)           NOT NULL CONSTRAINT DF_Oficina_HoraCierre   DEFAULT ('21:00'),
    Estado        VARCHAR(15)       NOT NULL CONSTRAINT DF_Oficina_Estado       DEFAULT ('Habilitada'),
    FechaAlta     DATETIME2(0)      NOT NULL CONSTRAINT DF_Oficina_FechaAlta    DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Oficina             PRIMARY KEY (IdOficina),
    CONSTRAINT UQ_Oficina_Piso_Nombre UNIQUE (IdPiso, Nombre),
    CONSTRAINT CK_Oficina_Capacidad   CHECK (Capacidad > 0),
    CONSTRAINT CK_Oficina_TarifaHora  CHECK (TarifaHora >= 0),
    CONSTRAINT CK_Oficina_Horario     CHECK (HoraCierre > HoraApertura),
    CONSTRAINT CK_Oficina_Estado      CHECK (Estado IN ('Habilitada','Deshabilitada','Baja')),
    CONSTRAINT FK_Oficina_Piso        FOREIGN KEY (IdPiso) REFERENCES dbo.Piso (IdPiso)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);


/* ---------------------------------------------------------------- Usuario */
CREATE TABLE dbo.Usuario (
    IdUsuario      INT IDENTITY(1,1) NOT NULL,
    Email          NVARCHAR(254)     NOT NULL,
    HashContrasena VARCHAR(255)      NOT NULL,
    Nombre         NVARCHAR(100)     NOT NULL,
    Apellido       NVARCHAR(100)     NOT NULL,
    Telefono       NVARCHAR(30)      NULL,
    Rol            VARCHAR(15)       NOT NULL,
    Estado         VARCHAR(10)       NOT NULL CONSTRAINT DF_Usuario_Estado    DEFAULT ('Activo'),
    FechaAlta      DATETIME2(0)      NOT NULL CONSTRAINT DF_Usuario_FechaAlta DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Usuario        PRIMARY KEY (IdUsuario),
    CONSTRAINT UQ_Usuario_Email  UNIQUE (Email),
    CONSTRAINT CK_Usuario_Rol    CHECK (Rol IN ('Administrador','Cliente','Empleado')),
    CONSTRAINT CK_Usuario_Estado CHECK (Estado IN ('Activo','Inactivo'))
);


/* --------------------------------------------------------------- Empleado */
CREATE TABLE dbo.Empleado (
    IdEmpleado  INT IDENTITY(1,1) NOT NULL,
    IdUsuario   INT               NOT NULL,
    IdEdificio  INT               NOT NULL,
    Estado      VARCHAR(10)       NOT NULL CONSTRAINT DF_Empleado_Estado    DEFAULT ('Activo'),
    FechaAlta   DATETIME2(0)      NOT NULL CONSTRAINT DF_Empleado_FechaAlta DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Empleado         PRIMARY KEY (IdEmpleado),
    CONSTRAINT UQ_Empleado_Usuario UNIQUE (IdUsuario),
    CONSTRAINT CK_Empleado_Estado  CHECK (Estado IN ('Activo','Inactivo')),
    CONSTRAINT FK_Empleado_Usuario FOREIGN KEY (IdUsuario)  REFERENCES dbo.Usuario (IdUsuario)
        ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT FK_Empleado_Edificio FOREIGN KEY (IdEdificio) REFERENCES dbo.Edificio (IdEdificio)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);


/* ------------------------------------------------------- EmpleadoOficina */
/* Asignacion con franja horaria: define cobertura (RN-12) y alcance de acceso (RN-40). */
CREATE TABLE dbo.EmpleadoOficina (
    IdEmpleadoOficina INT IDENTITY(1,1) NOT NULL,
    IdEmpleado        INT               NOT NULL,
    IdOficina         INT               NOT NULL,
    HoraInicio        TIME(0)           NOT NULL,
    HoraFin           TIME(0)           NOT NULL,
    CONSTRAINT PK_EmpleadoOficina          PRIMARY KEY (IdEmpleadoOficina),
    CONSTRAINT UQ_EmpleadoOficina_Franja   UNIQUE (IdEmpleado, IdOficina, HoraInicio),
    CONSTRAINT CK_EmpleadoOficina_Horario  CHECK (HoraFin > HoraInicio),
    CONSTRAINT FK_EmpleadoOficina_Empleado FOREIGN KEY (IdEmpleado) REFERENCES dbo.Empleado (IdEmpleado)
        ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT FK_EmpleadoOficina_Oficina  FOREIGN KEY (IdOficina)  REFERENCES dbo.Oficina (IdOficina)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);


/* ---------------------------------------------------------------- Reserva */
CREATE TABLE dbo.Reserva (
    IdReserva          INT IDENTITY(1,1) NOT NULL,
    IdUsuario          INT               NOT NULL,
    IdOficina          INT               NOT NULL,
    Fecha              DATE              NOT NULL,
    HoraInicio         TIME(0)           NOT NULL,
    HoraFin            TIME(0)           NOT NULL,
    CantidadAsientos   INT               NOT NULL,
    TarifaHoraAplicada DECIMAL(18,2)     NOT NULL,
    ImporteExigible    DECIMAL(18,2)     NULL,
    Estado             VARCHAR(15)       NOT NULL CONSTRAINT DF_Reserva_Estado        DEFAULT ('Confirmada'),
    EstadoPago         VARCHAR(15)       NOT NULL CONSTRAINT DF_Reserva_EstadoPago    DEFAULT ('Pendiente'),
    FechaCreacion      DATETIME2(0)      NOT NULL CONSTRAINT DF_Reserva_FechaCreacion DEFAULT (SYSDATETIME()),
    FechaCancelacion   DATETIME2(0)      NULL,
    CONSTRAINT PK_Reserva                PRIMARY KEY (IdReserva),
    CONSTRAINT CK_Reserva_Horario        CHECK (HoraFin > HoraInicio),
    CONSTRAINT CK_Reserva_DuracionMinima CHECK (DATEDIFF(MINUTE, HoraInicio, HoraFin) >= 60),
    CONSTRAINT CK_Reserva_Asientos       CHECK (CantidadAsientos > 0),
    CONSTRAINT CK_Reserva_Estado         CHECK (Estado IN ('Confirmada','EnCurso','Finalizada','Cancelada','NoPresentada')),
    CONSTRAINT CK_Reserva_EstadoPago     CHECK (EstadoPago IN ('Pendiente','Pagado','DeudaPendiente','SinCargo')),
    CONSTRAINT FK_Reserva_Usuario        FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario (IdUsuario)
        ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT FK_Reserva_Oficina        FOREIGN KEY (IdOficina) REFERENCES dbo.Oficina (IdOficina)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);


/* ------------------------------------------------------------- ReservaUso */
CREATE TABLE dbo.ReservaUso (
    IdReservaUso   INT IDENTITY(1,1) NOT NULL,
    IdReserva      INT               NOT NULL,
    HoraIngreso    DATETIME2(0)      NULL,
    HoraSalida     DATETIME2(0)      NULL,
    IdEmpleado     INT               NOT NULL,
    Incidencias    NVARCHAR(500)     NULL,
    FechaRegistro  DATETIME2(0)      NOT NULL CONSTRAINT DF_ReservaUso_FechaRegistro DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_ReservaUso           PRIMARY KEY (IdReservaUso),
    CONSTRAINT UQ_ReservaUso_Reserva   UNIQUE (IdReserva),
    CONSTRAINT CK_ReservaUso_Horarios  CHECK (HoraSalida IS NULL OR HoraIngreso IS NULL OR HoraSalida >= HoraIngreso),
    CONSTRAINT FK_ReservaUso_Reserva   FOREIGN KEY (IdReserva)  REFERENCES dbo.Reserva (IdReserva)
        ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT FK_ReservaUso_Empleado  FOREIGN KEY (IdEmpleado) REFERENCES dbo.Empleado (IdEmpleado)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);


/* ------------------------------------------------------------------- Pago */
CREATE TABLE dbo.Pago (
    IdPago             INT IDENTITY(1,1) NOT NULL,
    IdReserva          INT               NOT NULL,
    Importe            DECIMAL(18,2)     NOT NULL,
    FechaPago          DATETIME2(0)      NOT NULL,
    IdEmpleadoConfirma INT               NOT NULL,
    Referencia         NVARCHAR(100)     NULL,
    FechaRegistro      DATETIME2(0)      NOT NULL CONSTRAINT DF_Pago_FechaRegistro DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Pago          PRIMARY KEY (IdPago),
    CONSTRAINT UQ_Pago_Reserva  UNIQUE (IdReserva),
    CONSTRAINT CK_Pago_Importe  CHECK (Importe > 0),
    CONSTRAINT FK_Pago_Reserva  FOREIGN KEY (IdReserva)          REFERENCES dbo.Reserva (IdReserva)
        ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT FK_Pago_Empleado FOREIGN KEY (IdEmpleadoConfirma) REFERENCES dbo.Empleado (IdEmpleado)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);


/* ------------------------------------------------------------------ Deuda */
CREATE TABLE dbo.Deuda (
    IdDeuda         INT IDENTITY(1,1) NOT NULL,
    IdReserva       INT               NOT NULL,
    ImporteExigible DECIMAL(18,2)     NOT NULL,
    FechaGeneracion DATETIME2(0)      NOT NULL CONSTRAINT DF_Deuda_FechaGeneracion DEFAULT (SYSDATETIME()),
    Estado          VARCHAR(10)       NOT NULL CONSTRAINT DF_Deuda_Estado          DEFAULT ('Pendiente'),
    CONSTRAINT PK_Deuda         PRIMARY KEY (IdDeuda),
    CONSTRAINT UQ_Deuda_Reserva UNIQUE (IdReserva),
    CONSTRAINT CK_Deuda_Importe CHECK (ImporteExigible > 0),
    CONSTRAINT CK_Deuda_Estado  CHECK (Estado IN ('Pendiente','Anulada','Pagada')),
    CONSTRAINT FK_Deuda_Reserva FOREIGN KEY (IdReserva) REFERENCES dbo.Reserva (IdReserva)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);


/* -------------------------------------------------------------- Auditoria */
CREATE TABLE dbo.Auditoria (
    IdAuditoria BIGINT IDENTITY(1,1) NOT NULL,
    IdUsuario   INT                  NULL,   -- NULL = accion del sistema
    Accion      VARCHAR(50)          NOT NULL,
    Entidad     VARCHAR(50)          NOT NULL,
    IdEntidad   INT                  NULL,   -- referencia polimorfica, sin FK
    FechaHora   DATETIME2(0)         NOT NULL CONSTRAINT DF_Auditoria_FechaHora DEFAULT (SYSDATETIME()),
    Detalle     NVARCHAR(MAX)        NULL,
    CONSTRAINT PK_Auditoria         PRIMARY KEY (IdAuditoria),
    CONSTRAINT FK_Auditoria_Usuario FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario (IdUsuario)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);


/* ---------------------------------------------------------------- Indices */
CREATE INDEX IX_Reserva_Disponibilidad
    ON dbo.Reserva (IdOficina, Fecha, HoraInicio, HoraFin)
    INCLUDE (CantidadAsientos, Estado);


CREATE INDEX IX_Reserva_Usuario
    ON dbo.Reserva (IdUsuario, Fecha);


CREATE INDEX IX_Reserva_Agenda
    ON dbo.Reserva (Fecha, Estado);


CREATE INDEX IX_EmpleadoOficina_Cobertura
    ON dbo.EmpleadoOficina (IdOficina, HoraInicio, HoraFin);


CREATE INDEX IX_Deuda_Estado
    ON dbo.Deuda (Estado);


CREATE INDEX IX_Auditoria_Fecha
    ON dbo.Auditoria (FechaHora);


CREATE INDEX IX_Auditoria_Entidad
    ON dbo.Auditoria (Entidad, IdEntidad);

