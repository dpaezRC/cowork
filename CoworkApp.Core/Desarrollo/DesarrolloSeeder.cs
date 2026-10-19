using CoworkApp.Core.Datos;
using CoworkApp.Core.Dominio;
using CoworkApp.Core.Seguridad;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Desarrollo;

public sealed class DesarrolloSeeder
{
    private const string EmailAdministrador = "admin@cowork.local";
    private const string ContrasenaAdministrador = "Admin1234!";
    private const string EmailEmpleado = "empleado@cowork.local";
    private const string ContrasenaEmpleado = "Empleado1234!";
    private const string EmailCliente = "cliente@cowork.local";
    private const string ContrasenaCliente = "Cliente1234!";

    private readonly Db _db;

    public DesarrolloSeeder(Db db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<ResultadoDesarrolloSeeder> EjecutarAsync(CancellationToken ct = default)
    {
        return _db.EnTransaccionAsync(async (conexion, transaccion, token) =>
        {
            var usuariosExistentes = await _db.EscalarAsync<int>(
                "SELECT COUNT(*) FROM dbo.Usuario WITH (UPDLOCK, HOLDLOCK)",
                conexion: conexion,
                transaccion: transaccion,
                ct: token).ConfigureAwait(false);

            if (usuariosExistentes > 0)
            {
                return ResultadoDesarrolloSeeder.Omitido;
            }

            var idEdificio = await InsertarEdificioAsync(conexion, transaccion, token).ConfigureAwait(false);
            var idPiso = await InsertarPisoAsync(conexion, transaccion, idEdificio, token).ConfigureAwait(false);
            var idOficina = await InsertarOficinaAsync(conexion, transaccion, idPiso, token).ConfigureAwait(false);

            _ = await InsertarUsuarioAsync(
                conexion,
                transaccion,
                EmailAdministrador,
                PasswordHasher.Hash(ContrasenaAdministrador),
                "Administrador",
                "Demo",
                Roles.Administrador,
                token).ConfigureAwait(false);

            var idEmpleadoUsuario = await InsertarUsuarioAsync(
                conexion,
                transaccion,
                EmailEmpleado,
                PasswordHasher.Hash(ContrasenaEmpleado),
                "Empleado",
                "Demo",
                Roles.Empleado,
                token).ConfigureAwait(false);

            _ = await InsertarUsuarioAsync(
                conexion,
                transaccion,
                EmailCliente,
                PasswordHasher.Hash(ContrasenaCliente),
                "Cliente",
                "Demo",
                Roles.Cliente,
                token).ConfigureAwait(false);

            var idEmpleado = await InsertarEmpleadoAsync(conexion, transaccion, idEmpleadoUsuario, idEdificio, token).ConfigureAwait(false);
            await InsertarEmpleadoOficinaAsync(conexion, transaccion, idEmpleado, idOficina, token).ConfigureAwait(false);

            return new ResultadoDesarrolloSeeder(
                true,
                [
                    new CredencialDemo(Roles.Administrador, EmailAdministrador, ContrasenaAdministrador),
                    new CredencialDemo(Roles.Empleado, EmailEmpleado, ContrasenaEmpleado),
                    new CredencialDemo(Roles.Cliente, EmailCliente, ContrasenaCliente)
                ]);
        }, ct);
    }

    private Task<int> InsertarEdificioAsync(SqlConnection conexion, SqlTransaction transaccion, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.Edificio (Nombre, Direccion, Estado)
            OUTPUT INSERTED.IdEdificio
            VALUES (@Nombre, @Direccion, @Estado);
            """;

        return _db.EscalarAsync<int>(
            sql,
            new[]
            {
                DbParametros.NVarChar("@Nombre", "Edificio Central", 100),
                DbParametros.NVarChar("@Direccion", "Av. Central 100", 200),
                DbParametros.VarChar("@Estado", Estados.Activo, 10)
            },
            conexion,
            transaccion,
            ct);
    }

    private Task<int> InsertarPisoAsync(SqlConnection conexion, SqlTransaction transaccion, int idEdificio, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.Piso (IdEdificio, Nombre, Estado)
            OUTPUT INSERTED.IdPiso
            VALUES (@IdEdificio, @Nombre, @Estado);
            """;

        return _db.EscalarAsync<int>(
            sql,
            new[]
            {
                DbParametros.Int("@IdEdificio", idEdificio),
                DbParametros.NVarChar("@Nombre", "Piso 1", 50),
                DbParametros.VarChar("@Estado", Estados.Activo, 10)
            },
            conexion,
            transaccion,
            ct);
    }

    private Task<int> InsertarOficinaAsync(SqlConnection conexion, SqlTransaction transaccion, int idPiso, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.Oficina
                (IdPiso, Nombre, Capacidad, TarifaHora, HoraApertura, HoraCierre, Estado)
            OUTPUT INSERTED.IdOficina
            VALUES
                (@IdPiso, @Nombre, @Capacidad, @TarifaHora, @HoraApertura, @HoraCierre, @Estado);
            """;

        return _db.EscalarAsync<int>(
            sql,
            new[]
            {
                DbParametros.Int("@IdPiso", idPiso),
                DbParametros.NVarChar("@Nombre", "Oficina 101", 100),
                DbParametros.Int("@Capacidad", 4),
                DbParametros.Decimal("@TarifaHora", 1000.00m),
                DbParametros.Time("@HoraApertura", new TimeOnly(8, 0)),
                DbParametros.Time("@HoraCierre", new TimeOnly(21, 0)),
                DbParametros.VarChar("@Estado", Estados.Habilitada, 15)
            },
            conexion,
            transaccion,
            ct);
    }

    private Task<int> InsertarUsuarioAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        string email,
        string hashContrasena,
        string nombre,
        string apellido,
        string rol,
        CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.Usuario
                (Email, HashContrasena, Nombre, Apellido, Telefono, Rol, Estado)
            OUTPUT INSERTED.IdUsuario
            VALUES
                (@Email, @HashContrasena, @Nombre, @Apellido, @Telefono, @Rol, @Estado);
            """;

        return _db.EscalarAsync<int>(
            sql,
            new[]
            {
                DbParametros.NVarChar("@Email", email, 254),
                DbParametros.VarChar("@HashContrasena", hashContrasena, 255),
                DbParametros.NVarChar("@Nombre", nombre, 100),
                DbParametros.NVarChar("@Apellido", apellido, 100),
                DbParametros.NVarChar("@Telefono", null, 30),
                DbParametros.VarChar("@Rol", rol, 15),
                DbParametros.VarChar("@Estado", Estados.Activo, 10)
            },
            conexion,
            transaccion,
            ct);
    }

    private Task<int> InsertarEmpleadoAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        int idUsuario,
        int idEdificio,
        CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.Empleado (IdUsuario, IdEdificio, Estado)
            OUTPUT INSERTED.IdEmpleado
            VALUES (@IdUsuario, @IdEdificio, @Estado);
            """;

        return _db.EscalarAsync<int>(
            sql,
            new[]
            {
                DbParametros.Int("@IdUsuario", idUsuario),
                DbParametros.Int("@IdEdificio", idEdificio),
                DbParametros.VarChar("@Estado", Estados.Activo, 10)
            },
            conexion,
            transaccion,
            ct);
    }

    private Task InsertarEmpleadoOficinaAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        int idEmpleado,
        int idOficina,
        CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.EmpleadoOficina (IdEmpleado, IdOficina, HoraInicio, HoraFin)
            VALUES (@IdEmpleado, @IdOficina, @HoraInicio, @HoraFin);
            """;

        return _db.EjecutarAsync(
            sql,
            new[]
            {
                DbParametros.Int("@IdEmpleado", idEmpleado),
                DbParametros.Int("@IdOficina", idOficina),
                DbParametros.Time("@HoraInicio", new TimeOnly(8, 0)),
                DbParametros.Time("@HoraFin", new TimeOnly(21, 0))
            },
            conexion,
            transaccion,
            ct);
    }
}
