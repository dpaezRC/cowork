using CoworkApp.Core.Datos;
using CoworkApp.Core.Dominio;
using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public sealed class EmpleadoRepositorio : IEmpleadoRepositorio
{
    private const string SqlListarDetalle = """
        SELECT e.IdEmpleado, e.IdUsuario, u.Nombre, u.Apellido, u.Email, u.Telefono,
               e.IdEdificio, ed.Nombre AS Edificio, e.Estado, e.FechaAlta
        FROM dbo.Empleado e
        INNER JOIN dbo.Usuario u ON u.IdUsuario = e.IdUsuario
        INNER JOIN dbo.Edificio ed ON ed.IdEdificio = e.IdEdificio
        ORDER BY e.IdEmpleado;
        """;

    private const string SqlObtenerDetalle = """
        SELECT e.IdEmpleado, e.IdUsuario, u.Nombre, u.Apellido, u.Email, u.Telefono,
               e.IdEdificio, ed.Nombre AS Edificio, e.Estado, e.FechaAlta
        FROM dbo.Empleado e
        INNER JOIN dbo.Usuario u ON u.IdUsuario = e.IdUsuario
        INNER JOIN dbo.Edificio ed ON ed.IdEdificio = e.IdEdificio
        WHERE e.IdEmpleado = @IdEmpleado;
        """;

    private const string SqlObtenerDetallePorUsuario = """
        SELECT e.IdEmpleado, e.IdUsuario, u.Nombre, u.Apellido, u.Email, u.Telefono,
               e.IdEdificio, ed.Nombre AS Edificio, e.Estado, e.FechaAlta
        FROM dbo.Empleado e
        INNER JOIN dbo.Usuario u ON u.IdUsuario = e.IdUsuario
        INNER JOIN dbo.Edificio ed ON ed.IdEdificio = e.IdEdificio
        WHERE e.IdUsuario = @IdUsuario;
        """;

    private const string SqlAgregar = """
        INSERT INTO dbo.Empleado (IdUsuario, IdEdificio, Estado)
        OUTPUT INSERTED.IdEmpleado
        VALUES (@IdUsuario, @IdEdificio, @Estado);
        """;

    private const string SqlActualizar = """
        UPDATE dbo.Empleado
        SET IdEdificio = @IdEdificio,
            Estado = @Estado
        WHERE IdEmpleado = @IdEmpleado;
        """;

    private const string SqlActualizarEstado = """
        UPDATE dbo.Empleado
        SET Estado = @Estado
        WHERE IdEmpleado = @IdEmpleado;
        """;

    private readonly Db _db;

    public EmpleadoRepositorio(Db db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<List<EmpleadoDetalle>> ListarDetalleAsync(CancellationToken ct = default)
    {
        return _db.ConsultarAsync(SqlListarDetalle, MapearDetalle, ct: ct);
    }

    public Task<EmpleadoDetalle?> ObtenerDetalleAsync(
        int idEmpleado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.ConsultarUnoAsync(
            SqlObtenerDetalle,
            MapearDetalle,
            new[] { DbParametros.Int("@IdEmpleado", idEmpleado) },
            conexion,
            transaccion,
            ct);
    }

    public Task<EmpleadoDetalle?> ObtenerDetallePorUsuarioAsync(
        int idUsuario,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.ConsultarUnoAsync(
            SqlObtenerDetallePorUsuario,
            MapearDetalle,
            new[] { DbParametros.Int("@IdUsuario", idUsuario) },
            conexion,
            transaccion,
            ct);
    }

    public Task<int> AgregarAsync(
        int idUsuario,
        int idEdificio,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return UnicidadSql.EjecutarAsync(() => _db.EscalarAsync<int>(
            SqlAgregar,
            new[]
            {
                DbParametros.Int("@IdUsuario", idUsuario),
                DbParametros.Int("@IdEdificio", idEdificio),
                DbParametros.VarChar("@Estado", Estados.Activo, 10)
            },
            conexion,
            transaccion,
            ct));
    }

    public Task<int> ActualizarAsync(
        int idEmpleado,
        int idEdificio,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return UnicidadSql.EjecutarAsync(() => _db.EjecutarAsync(
            SqlActualizar,
            new[]
            {
                DbParametros.Int("@IdEmpleado", idEmpleado),
                DbParametros.Int("@IdEdificio", idEdificio),
                DbParametros.VarChar("@Estado", estado, 10)
            },
            conexion,
            transaccion,
            ct));
    }

    public Task<int> ActualizarEstadoAsync(
        int idEmpleado,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.EjecutarAsync(
            SqlActualizarEstado,
            new[]
            {
                DbParametros.Int("@IdEmpleado", idEmpleado),
                DbParametros.VarChar("@Estado", estado, 10)
            },
            conexion,
            transaccion,
            ct);
    }

    private static EmpleadoDetalle MapearDetalle(SqlDataReader lector)
    {
        return new EmpleadoDetalle
        {
            IdEmpleado = lector.GetInt32(0),
            IdUsuario = lector.GetInt32(1),
            Nombre = lector.GetString(2),
            Apellido = lector.GetString(3),
            Email = lector.GetString(4),
            Telefono = lector.IsDBNull(5) ? null : lector.GetString(5),
            IdEdificio = lector.GetInt32(6),
            Edificio = lector.GetString(7),
            Estado = lector.GetString(8),
            FechaAlta = lector.GetDateTime(9)
        };
    }
}
