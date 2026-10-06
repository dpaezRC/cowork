-- ========================================================
-- 003_Refactor_Unicode.sql — REF-01 (Unicode y collation)
--                            + REF-02 (endurecer Usuarios)
--                            + REF-03 (precisión temporal)
-- Idempotente: cada paso sólo se ejecuta si la columna todavía
-- está en el tipo viejo / el campo todavía no existe.
--
-- IMPORTANTE — SQL Server compila el batch completo antes de
-- ejecutarlo: una columna agregada con ALTER en el mismo batch
-- no es visible para los statements siguientes (da error 207).
-- Por eso cada bloque va en su propio batch, con GO, y cada
-- batch cierra su propia transacción. Re-ejecutar es inocuo.
-- ========================================================
USE iset;
GO

-- ------------------------------------------------------
-- REF-01 · Unicode y collation
--
-- Nota: Roles.Nombre, EstadosTurno.Nombre y Usuarios.Email
-- tienen un constraint UNIQUE creado por el UNIQUE NOT NULL
-- inline del 001_Schema_Base.sql, y eso bloquea el ALTER
-- COLUMN (el doc asumía que sólo había índices numéricos).
-- Se suelta el constraint, se cambia el tipo y se recrea con
-- nombre estable. Todo dentro de la misma transacción.
-- ------------------------------------------------------
BEGIN TRY
	BEGIN TRANSACTION;

	IF EXISTS (
		SELECT 1 FROM sys.columns c
		JOIN sys.types t ON c.user_type_id = t.user_type_id
		WHERE c.object_id = OBJECT_ID(N'dbo.Edificios') AND c.name = N'Nombre' AND t.name = 'varchar'
	) ALTER TABLE dbo.Edificios ALTER COLUMN Nombre NVARCHAR(100) NOT NULL;

	IF EXISTS (
		SELECT 1 FROM sys.columns c
		JOIN sys.types t ON c.user_type_id = t.user_type_id
		WHERE c.object_id = OBJECT_ID(N'dbo.Edificios') AND c.name = N'Ciudad' AND t.name = 'varchar'
	) ALTER TABLE dbo.Edificios ALTER COLUMN Ciudad NVARCHAR(100) NOT NULL;

	IF EXISTS (
		SELECT 1 FROM sys.columns c
		JOIN sys.types t ON c.user_type_id = t.user_type_id
		WHERE c.object_id = OBJECT_ID(N'dbo.Oficinas') AND c.name = N'Nombre' AND t.name = 'varchar'
	) ALTER TABLE dbo.Oficinas ALTER COLUMN Nombre NVARCHAR(100) NOT NULL;

	IF EXISTS (
		SELECT 1 FROM sys.columns c
		JOIN sys.types t ON c.user_type_id = t.user_type_id
		WHERE c.object_id = OBJECT_ID(N'dbo.Oficinas') AND c.name = N'Tipo' AND t.name = 'varchar'
	) ALTER TABLE dbo.Oficinas ALTER COLUMN Tipo NVARCHAR(100) NOT NULL;

	-- Roles · Nombre (tiene UNIQUE)
	IF EXISTS (
		SELECT 1 FROM sys.columns c
		JOIN sys.types t ON c.user_type_id = t.user_type_id
		WHERE c.object_id = OBJECT_ID(N'dbo.Roles') AND c.name = N'Nombre' AND t.name = 'varchar'
	)
	BEGIN
		DECLARE @UqRoles sysname;
		SELECT @UqRoles = kc.name
		FROM sys.key_constraints kc
		JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
		JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
		WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Roles') AND kc.type = 'UQ' AND c.name = N'Nombre';

		IF @UqRoles IS NOT NULL
		BEGIN
			DECLARE @DropRoles nvarchar(400) = N'ALTER TABLE dbo.Roles DROP CONSTRAINT ' + QUOTENAME(@UqRoles);
			EXEC(@DropRoles);
		END

		ALTER TABLE dbo.Roles ALTER COLUMN Nombre NVARCHAR(50) NOT NULL;
		ALTER TABLE dbo.Roles ADD CONSTRAINT UQ_Roles_Nombre UNIQUE (Nombre);
	END

	-- EstadosTurno · Nombre (tiene UNIQUE)
	IF EXISTS (
		SELECT 1 FROM sys.columns c
		JOIN sys.types t ON c.user_type_id = t.user_type_id
		WHERE c.object_id = OBJECT_ID(N'dbo.EstadosTurno') AND c.name = N'Nombre' AND t.name = 'varchar'
	)
	BEGIN
		DECLARE @UqEstados sysname;
		SELECT @UqEstados = kc.name
		FROM sys.key_constraints kc
		JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
		JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
		WHERE kc.parent_object_id = OBJECT_ID(N'dbo.EstadosTurno') AND kc.type = 'UQ' AND c.name = N'Nombre';

		IF @UqEstados IS NOT NULL
		BEGIN
			DECLARE @DropEstadosTurno nvarchar(400) = N'ALTER TABLE dbo.EstadosTurno DROP CONSTRAINT ' + QUOTENAME(@UqEstados);
			EXEC(@DropEstadosTurno);
		END

		ALTER TABLE dbo.EstadosTurno ALTER COLUMN Nombre NVARCHAR(50) NOT NULL;
		ALTER TABLE dbo.EstadosTurno ADD CONSTRAINT UQ_EstadosTurno_Nombre UNIQUE (Nombre);
	END

	-- Usuarios · Nombre
	IF EXISTS (
		SELECT 1 FROM sys.columns c
		JOIN sys.types t ON c.user_type_id = t.user_type_id
		WHERE c.object_id = OBJECT_ID(N'dbo.Usuarios') AND c.name = N'Nombre' AND t.name = 'varchar'
	) ALTER TABLE dbo.Usuarios ALTER COLUMN Nombre NVARCHAR(100) NOT NULL;

	-- Usuarios · Email (tiene UNIQUE)
	IF EXISTS (
		SELECT 1 FROM sys.columns c
		JOIN sys.types t ON c.user_type_id = t.user_type_id
		WHERE c.object_id = OBJECT_ID(N'dbo.Usuarios') AND c.name = N'Email' AND t.name = 'varchar'
	)
	BEGIN
		DECLARE @UqEmail sysname;
		SELECT @UqEmail = kc.name
		FROM sys.key_constraints kc
		JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
		JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
		WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Usuarios') AND kc.type = 'UQ' AND c.name = N'Email';

		IF @UqEmail IS NOT NULL
		BEGIN
			DECLARE @DropUsuarios nvarchar(400) = N'ALTER TABLE dbo.Usuarios DROP CONSTRAINT ' + QUOTENAME(@UqEmail);
			EXEC(@DropUsuarios);
		END

		ALTER TABLE dbo.Usuarios ALTER COLUMN Email NVARCHAR(150) NOT NULL;
		ALTER TABLE dbo.Usuarios ADD CONSTRAINT UQ_Usuarios_Email UNIQUE (Email);
	END

	IF EXISTS (
		SELECT 1 FROM sys.columns c
		JOIN sys.types t ON c.user_type_id = t.user_type_id
		WHERE c.object_id = OBJECT_ID(N'dbo.Usuarios') AND c.name = N'Telefono' AND t.name = 'varchar'
	) ALTER TABLE dbo.Usuarios ALTER COLUMN Telefono NVARCHAR(30) NULL;

	-- Fuera de la lista de cambios de REF-01, pero su AC es tajante:
	-- "Ninguna columna de texto del dominio queda en VARCHAR".
	IF EXISTS (
		SELECT 1 FROM sys.columns c
		JOIN sys.types t ON c.user_type_id = t.user_type_id
		WHERE c.object_id = OBJECT_ID(N'dbo.Pisos') AND c.name = N'Descripcion' AND t.name = 'varchar'
	) ALTER TABLE dbo.Pisos ALTER COLUMN Descripcion NVARCHAR(100) NULL;

	IF EXISTS (
		SELECT 1 FROM sys.columns c
		JOIN sys.types t ON c.user_type_id = t.user_type_id
		WHERE c.object_id = OBJECT_ID(N'dbo.ValidacionesSupervisor') AND c.name = N'EstadoVerificacion' AND t.name = 'varchar'
	) ALTER TABLE dbo.ValidacionesSupervisor ALTER COLUMN EstadoVerificacion NVARCHAR(50) NOT NULL;

	IF EXISTS (
		SELECT 1 FROM sys.columns c
		JOIN sys.types t ON c.user_type_id = t.user_type_id
		WHERE c.object_id = OBJECT_ID(N'dbo.ValidacionesSupervisor') AND c.name = N'Observaciones' AND t.name = 'varchar'
	) ALTER TABLE dbo.ValidacionesSupervisor ALTER COLUMN Observaciones NVARCHAR(MAX) NULL;

	COMMIT TRANSACTION;
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
	THROW;
END CATCH;
GO

-- ------------------------------------------------------
-- REF-02 · Endurecer Usuarios (nuevas columnas)
-- ------------------------------------------------------
BEGIN TRY
	BEGIN TRANSACTION;

	IF COL_LENGTH('dbo.Usuarios', 'AlgoritmoHash') IS NULL
		ALTER TABLE dbo.Usuarios ADD AlgoritmoHash VARCHAR(20) NOT NULL DEFAULT 'PBKDF2-SHA256';

	IF COL_LENGTH('dbo.Usuarios', 'Documento') IS NULL
		ALTER TABLE dbo.Usuarios ADD Documento VARCHAR(30) NULL;

	IF COL_LENGTH('dbo.Usuarios', 'Activo') IS NULL
		ALTER TABLE dbo.Usuarios ADD Activo BIT NOT NULL DEFAULT 1;

	IF COL_LENGTH('dbo.Usuarios', 'IntentosFallidos') IS NULL
		ALTER TABLE dbo.Usuarios ADD IntentosFallidos TINYINT NOT NULL DEFAULT 0;

	IF COL_LENGTH('dbo.Usuarios', 'BloqueadoHasta') IS NULL
		ALTER TABLE dbo.Usuarios ADD BloqueadoHasta DATETIME2 NULL;

	IF COL_LENGTH('dbo.Usuarios', 'ActualizadoAt') IS NULL
		ALTER TABLE dbo.Usuarios ADD ActualizadoAt DATETIME2 NOT NULL DEFAULT SYSDATETIME();

	IF EXISTS (
		SELECT 1 FROM sys.columns c
		JOIN sys.types t ON c.user_type_id = t.user_type_id
		WHERE c.object_id = OBJECT_ID(N'dbo.Usuarios') AND c.name = N'PasswordHash' AND t.name = 'varchar'
	) ALTER TABLE dbo.Usuarios ALTER COLUMN PasswordHash NVARCHAR(255) NOT NULL;

	COMMIT TRANSACTION;
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
	THROW;
END CATCH;
GO

-- ------------------------------------------------------
-- REF-02 · índice único de Documento (batch aparte: la
-- columna Documento recién creada no es visible si este
-- CREATE INDEX compila en el mismo batch que el ALTER).
-- Los índices filtrados exigen QUOTED_IDENTIFIER ON, que
-- sqlcmd trae en OFF por defecto.
-- ------------------------------------------------------
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (
	SELECT 1 FROM sys.indexes
	WHERE name = 'UX_Usuarios_Documento' AND object_id = OBJECT_ID(N'dbo.Usuarios')
) CREATE UNIQUE INDEX UX_Usuarios_Documento ON dbo.Usuarios(Documento)
	WHERE Documento IS NOT NULL;
GO

-- ------------------------------------------------------
-- REF-03 · Precisión temporal
-- ------------------------------------------------------
BEGIN TRY
	BEGIN TRANSACTION;

	IF EXISTS (
		SELECT 1 FROM sys.columns
		WHERE object_id = OBJECT_ID(N'dbo.Turnos') AND name = 'HoraInicio' AND scale <> 0
	) ALTER TABLE dbo.Turnos ALTER COLUMN HoraInicio DATETIME2(0) NOT NULL;

	IF EXISTS (
		SELECT 1 FROM sys.columns
		WHERE object_id = OBJECT_ID(N'dbo.Turnos') AND name = 'HoraFinEstimada' AND scale <> 0
	) ALTER TABLE dbo.Turnos ALTER COLUMN HoraFinEstimada DATETIME2(0) NULL;

	IF EXISTS (
		SELECT 1 FROM sys.columns
		WHERE object_id = OBJECT_ID(N'dbo.Turnos') AND name = 'HoraFinReal' AND scale <> 0
	) ALTER TABLE dbo.Turnos ALTER COLUMN HoraFinReal DATETIME2(0) NULL;

	IF EXISTS (
		SELECT 1 FROM sys.columns
		WHERE object_id = OBJECT_ID(N'dbo.Turnos') AND name = 'CreadoAt' AND scale <> 3
	) ALTER TABLE dbo.Turnos ALTER COLUMN CreadoAt DATETIME2(3) NOT NULL;

	COMMIT TRANSACTION;
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
	THROW;
END CATCH;
GO

-- ------------------------------------------------------
-- Verificación: ninguna fila = todo aplicado.
-- AlgoritmoHash y Documento son identificadores técnicos,
-- no texto de dominio, y por eso se excluyen.
-- ------------------------------------------------------
SELECT
	OBJECT_NAME(c.object_id) AS Tabla,
	c.name AS Columna,
	t.name AS Tipo
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE t.name = 'varchar'
	AND OBJECT_NAME(c.object_id) IN (
		'Roles', 'EstadosTurno', 'Usuarios', 'Edificios', 'Pisos',
		'Oficinas', 'Turnos', 'SupervisorEdificios', 'ValidacionesSupervisor'
	)
	AND c.name NOT IN ('AlgoritmoHash', 'Documento');
GO
