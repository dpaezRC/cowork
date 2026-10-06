-- ========================================================
-- SISTEMA DE GESTIÓN DE COWORKING - INICIALIZACIÓN
-- ========================================================
IF DB_ID('iset') IS NULL BEGIN
CREATE DATABASE iset;

END
GO
USE iset;

GO IF NOT EXISTS (
	SELECT
		1
	FROM
		sys.objects
	WHERE
		object_id = OBJECT_ID(N'[dbo].[Roles]')
		AND
	TYPE = N'U'
) BEGIN
CREATE TABLE Roles (Id INT IDENTITY(1, 1) PRIMARY KEY, Nombre VARCHAR(50) UNIQUE NOT NULL);

END
GO IF NOT EXISTS (
	SELECT
		1
	FROM
		sys.objects
	WHERE
		object_id = OBJECT_ID(N'[dbo].[EstadosTurno]')
		AND
	TYPE = N'U'
) BEGIN
CREATE TABLE EstadosTurno (Id INT IDENTITY(1, 1) PRIMARY KEY, Nombre VARCHAR(50) UNIQUE NOT NULL);

END
GO IF NOT EXISTS (
	SELECT
		1
	FROM
		sys.objects
	WHERE
		object_id = OBJECT_ID(N'[dbo].[Usuarios]')
		AND
	TYPE = N'U'
) BEGIN
CREATE TABLE Usuarios (
	Id INT IDENTITY(1, 1) PRIMARY KEY,
	Nombre VARCHAR(100) NOT NULL,
	Email VARCHAR(150) UNIQUE NOT NULL,
	PasswordHash VARCHAR(255) NOT NULL,
	Telefono VARCHAR(30) NULL,
	RolId INT NOT NULL FOREIGN KEY REFERENCES Roles (Id),
	CreadoAt DATETIME2 DEFAULT SYSDATETIME()
);

END
GO IF NOT EXISTS (
	SELECT
		1
	FROM
		sys.objects
	WHERE
		object_id = OBJECT_ID(N'[dbo].[Edificios]')
		AND
	TYPE = N'U'
) BEGIN
CREATE TABLE Edificios (Id INT IDENTITY(1, 1) PRIMARY KEY, Nombre VARCHAR(100) NOT NULL, Direccion NVARCHAR(255) NOT NULL, Ciudad VARCHAR(100) NOT NULL);

END
GO IF NOT EXISTS (
	SELECT
		1
	FROM
		sys.objects
	WHERE
		object_id = OBJECT_ID(N'[dbo].[SupervisorEdificios]')
		AND
	TYPE = N'U'
) BEGIN
CREATE TABLE SupervisorEdificios (
	Id INT IDENTITY(1, 1) PRIMARY KEY,
	UsuarioId INT NOT NULL FOREIGN KEY REFERENCES Usuarios (Id) ON DELETE CASCADE,
	EdificioId INT NOT NULL FOREIGN KEY REFERENCES Edificios (Id) ON DELETE CASCADE,
	CONSTRAINT UQ_Supervisor_Edificio UNIQUE (UsuarioId, EdificioId)
);

END
GO IF NOT EXISTS (
	SELECT
		1
	FROM
		sys.objects
	WHERE
		object_id = OBJECT_ID(N'[dbo].[Pisos]')
		AND
	TYPE = N'U'
) BEGIN
CREATE TABLE Pisos (
	Id INT IDENTITY(1, 1) PRIMARY KEY,
	EdificioId INT NOT NULL FOREIGN KEY REFERENCES Edificios (Id) ON DELETE CASCADE,
	Numero INT NOT NULL,
	Descripcion VARCHAR(100) NULL,
	CONSTRAINT UQ_Edificio_Piso UNIQUE (EdificioId, Numero)
);

END
GO IF NOT EXISTS (
	SELECT
		1
	FROM
		sys.objects
	WHERE
		object_id = OBJECT_ID(N'[dbo].[Oficinas]')
		AND
	TYPE = N'U'
) BEGIN
CREATE TABLE Oficinas (
	Id INT IDENTITY(1, 1) PRIMARY KEY,
	PisoId INT NOT NULL FOREIGN KEY REFERENCES Pisos (Id) ON DELETE CASCADE,
	Nombre VARCHAR(100) NOT NULL,
	Tipo VARCHAR(50) NOT NULL,
	CapacidadTotal INT NOT NULL CHECK (CapacidadTotal > 0),
	PrecioPorMinuto DECIMAL(10, 4) NOT NULL CHECK (PrecioPorMinuto >= 0),
	Activa BIT DEFAULT 1
);

END
GO IF NOT EXISTS (
	SELECT
		1
	FROM
		sys.objects
	WHERE
		object_id = OBJECT_ID(N'[dbo].[Turnos]')
		AND
	TYPE = N'U'
) BEGIN
CREATE TABLE Turnos (
	Id INT IDENTITY(1, 1) PRIMARY KEY,
	ClienteId INT NOT NULL FOREIGN KEY REFERENCES Usuarios (Id),
	OficinaId INT NOT NULL FOREIGN KEY REFERENCES Oficinas (Id),
	CantidadAsientos INT NOT NULL CHECK (CantidadAsientos > 0),
	HoraInicio DATETIME2 NOT NULL,
	HoraFinEstimada DATETIME2 NULL,
	HoraFinReal DATETIME2 NULL,
	EstadoId INT NOT NULL FOREIGN KEY REFERENCES EstadosTurno (Id),
	MontoTotal DECIMAL(10, 2) DEFAULT 0.00,
	CreadoAt DATETIME2 DEFAULT SYSDATETIME()
);

END
GO IF NOT EXISTS (
	SELECT
		1
	FROM
		sys.objects
	WHERE
		object_id = OBJECT_ID(N'[dbo].[ValidacionesSupervisor]')
		AND
	TYPE = N'U'
) BEGIN
CREATE TABLE ValidacionesSupervisor (
	Id INT IDENTITY(1, 1) PRIMARY KEY,
	SupervisorId INT NOT NULL FOREIGN KEY REFERENCES Usuarios (Id),
	TurnoId INT NOT NULL FOREIGN KEY REFERENCES Turnos (Id) ON DELETE CASCADE,
	EstadoVerificacion VARCHAR(50) NOT NULL,
	Observaciones VARCHAR(MAX) NULL,
	VerificadoAt DATETIME2 DEFAULT SYSDATETIME()
);

END
GO IF NOT EXISTS (
	SELECT
		1
	FROM
		sys.indexes
	WHERE
		name = 'IX_Turnos_ClienteId'
		AND object_id = OBJECT_ID('Turnos')
) CREATE NONCLUSTERED
INDEX IX_Turnos_ClienteId ON Turnos (ClienteId);

IF NOT EXISTS (
	SELECT
		1
	FROM
		sys.indexes
	WHERE
		name = 'IX_Turnos_OficinaId'
		AND object_id = OBJECT_ID('Turnos')
) CREATE NONCLUSTERED
INDEX IX_Turnos_OficinaId ON Turnos (OficinaId);

IF NOT EXISTS (
	SELECT
		1
	FROM
		sys.indexes
	WHERE
		name = 'IX_SupervisorEdificios_UsuarioId'
		AND object_id = OBJECT_ID('SupervisorEdificios')
) CREATE NONCLUSTERED
INDEX IX_SupervisorEdificios_UsuarioId ON SupervisorEdificios (UsuarioId);

IF NOT EXISTS (
	SELECT
		1
	FROM
		sys.indexes
	WHERE
		name = 'IX_Pisos_EdificioId'
		AND object_id = OBJECT_ID('Pisos')
) CREATE NONCLUSTERED
INDEX IX_Pisos_EdificioId ON Pisos (EdificioId);

IF NOT EXISTS (
	SELECT
		1
	FROM
		sys.indexes
	WHERE
		name = 'IX_Oficinas_PisoId'
		AND object_id = OBJECT_ID('Oficinas')
) CREATE NONCLUSTERED
INDEX IX_Oficinas_PisoId ON Oficinas (PisoId);

GO IF NOT EXISTS (
	SELECT
		1
	FROM
		dbo.Roles
)
INSERT INTO
	dbo.Roles (Nombre)
VALUES
	('admin'),
	('supervisor'),
	('cliente');

IF NOT EXISTS (
	SELECT
		1
	FROM
		dbo.EstadosTurno
)
INSERT INTO
	dbo.EstadosTurno (Nombre)
VALUES
	('activo'),
	('finalizado'),
	('cancelado'),
	('vencido');

IF NOT EXISTS (
	SELECT
		1
	FROM
		dbo.Edificios
)
INSERT INTO
	dbo.Edificios (Nombre, Direccion, Ciudad)
VALUES
	(N'Torre Central', N'Av. Principal 123', N'Madrid'),
	(N'Edificio Norte', N'Calle Secundaria 45', N'Madrid');

IF NOT EXISTS (
	SELECT
		1
	FROM
		dbo.Pisos
)
INSERT INTO
	dbo.Pisos (EdificioId, Numero, Descripcion)
VALUES
	(1, 1, N'Planta Baja'),
	(1, 2, N'Primera Planta'),
	(2, 1, N'Planta Baja');

IF NOT EXISTS (
	SELECT
		1
	FROM
		dbo.Oficinas
)
INSERT INTO
	dbo.Oficinas (PisoId, Nombre, Tipo, CapacidadTotal, PrecioPorMinuto, Activa)
VALUES
	(1, N'Open Space A', N'escritorio_compartido', 6, 0.0500, 1),
	(2, N'Oficina Privada 201', N'oficina_privada', 4, 0.1200, 1),
	(3, N'Sala de Reuniones Norte', N'sala_reuniones', 8, 0.1500, 1);

GO
SELECT
	'Roles' AS Tabla,
	COUNT(*) AS Registros
FROM
	dbo.Roles
UNION ALL
SELECT
	'EstadosTurno',
	COUNT(*)
FROM
	dbo.EstadosTurno
UNION ALL
SELECT
	'Edificios',
	COUNT(*)
FROM
	dbo.Edificios
UNION ALL
SELECT
	'Pisos',
	COUNT(*)
FROM
	dbo.Pisos
UNION ALL
SELECT
	'Oficinas',
	COUNT(*)
FROM
	dbo.Oficinas;

GO