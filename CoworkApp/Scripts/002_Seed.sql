-- ========================================================
-- 002_Seed.sql — datos iniciales (REF-12)
-- Idempotente: cada bloque sólo inserta si la tabla está vacía.
-- ========================================================
USE iset;
GO

-- sqlcmd trae QUOTED_IDENTIFIER OFF; los índices filtrados lo exigen en ON.
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (
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
