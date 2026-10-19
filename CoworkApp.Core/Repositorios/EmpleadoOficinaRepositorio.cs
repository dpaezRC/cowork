using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public sealed class EmpleadoOficinaRepositorio : IEmpleadoOficinaRepositorio
{
    private const string SqlListarPorEmpleado = """
        SELECT eo.IdEmpleadoOficina, eo.IdEmpleado, eo.IdOficina, o.Nombre AS Oficina,
               e.Nombre AS Edificio, eo.HoraInicio, eo.HoraFin
        FROM dbo.EmpleadoOficina eo
        INNER JOIN dbo.Oficina o ON o.IdOficina = eo.IdOficina
        INNER JOIN dbo.Piso p ON p.IdPiso = o.IdPiso
        INNER JOIN dbo.Edificio e ON e.IdEdificio = p.IdEdificio
        WHERE eo.IdEmpleado = @IdEmpleado
        ORDER BY eo.HoraInicio, eo.IdEmpleadoOficina;
        """;

    private const string SqlObtenerDetalle = """
        SELECT eo.IdEmpleadoOficina, eo.IdEmpleado, eo.IdOficina, o.Nombre AS Oficina,
               e.Nombre AS Edificio, eo.HoraInicio, eo.HoraFin
        FROM dbo.EmpleadoOficina eo
        INNER JOIN dbo.Oficina o ON o.IdOficina = eo.IdOficina
        INNER JOIN dbo.Piso p ON p.IdPiso = o.IdPiso
        INNER JOIN dbo.Edificio e ON e.IdEdificio = p.IdEdificio
        WHERE eo.IdEmpleadoOficina = @IdEmpleadoOficina;
        """;

    private const string SqlExisteFranja = """
        SELECT COUNT(*)
        FROM dbo.EmpleadoOficina
        WHERE IdEmpleado = @IdEmpleado
          AND IdOficina = @IdOficina
          AND HoraInicio = @HoraInicio;
        """;

    private const string SqlTieneAsignaciones = """
        SELECT COUNT(*)
        FROM dbo.EmpleadoOficina
        WHERE IdEmpleado = @IdEmpleado;
        """;

    private const string SqlAgregar = """
        INSERT INTO dbo.EmpleadoOficina (IdEmpleado, IdOficina, HoraInicio, HoraFin)
        OUTPUT INSERTED.IdEmpleadoOficina
        VALUES (@IdEmpleado, @IdOficina, @HoraInicio, @HoraFin);
        """;

    private const string SqlEliminar = """
        DELETE FROM dbo.EmpleadoOficina
        WHERE IdEmpleadoOficina = @IdEmpleadoOficina;
        """;

    private readonly Db _db;

    public EmpleadoOficinaRepositorio(Db db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<List<AsignacionDetalle>> ListarPorEmpleadoAsync(
        int idEmpleado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.ConsultarAsync(
            SqlListarPorEmpleado,
            MapearDetalle,
            new[] { DbParametros.Int("@IdEmpleado", idEmpleado) },
            conexion,
            transaccion,
            ct);
    }

    public Task<AsignacionDetalle?> ObtenerDetalleAsync(
        int idAsignacion,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.ConsultarUnoAsync(
            SqlObtenerDetalle,
            MapearDetalle,
            new[] { DbParametros.Int("@IdEmpleadoOficina", idAsignacion) },
            conexion,
            transaccion,
            ct);
    }

    public async Task<bool> ExisteFranjaAsync(
        int idEmpleado,
        int idOficina,
        TimeOnly horaInicio,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var total = await _db.EscalarAsync<int>(
            SqlExisteFranja,
            new[]
            {
                DbParametros.Int("@IdEmpleado", idEmpleado),
                DbParametros.Int("@IdOficina", idOficina),
                DbParametros.Time("@HoraInicio", horaInicio)
            },
            conexion,
            transaccion,
            ct).ConfigureAwait(false);

        return total > 0;
    }

    public async Task<bool> TieneAsignacionesAsync(
        int idEmpleado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var total = await _db.EscalarAsync<int>(
            SqlTieneAsignaciones,
            new[] { DbParametros.Int("@IdEmpleado", idEmpleado) },
            conexion,
            transaccion,
            ct).ConfigureAwait(false);

        return total > 0;
    }

    public Task<int> AgregarAsync(
        int idEmpleado,
        int idOficina,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return UnicidadSql.EjecutarAsync(() => _db.EscalarAsync<int>(
            SqlAgregar,
            new[]
            {
                DbParametros.Int("@IdEmpleado", idEmpleado),
                DbParametros.Int("@IdOficina", idOficina),
                DbParametros.Time("@HoraInicio", horaInicio),
                DbParametros.Time("@HoraFin", horaFin)
            },
            conexion,
            transaccion,
            ct));
    }

    public Task<int> EliminarAsync(
        int idAsignacion,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.EjecutarAsync(
            SqlEliminar,
            new[] { DbParametros.Int("@IdEmpleadoOficina", idAsignacion) },
            conexion,
            transaccion,
            ct);
    }

    private static AsignacionDetalle MapearDetalle(SqlDataReader lector)
    {
        return new AsignacionDetalle
        {
            IdEmpleadoOficina = lector.GetInt32(0),
            IdEmpleado = lector.GetInt32(1),
            IdOficina = lector.GetInt32(2),
            Oficina = lector.GetString(3),
            Edificio = lector.GetString(4),
            HoraInicio = TimeOnly.FromTimeSpan(lector.GetTimeSpan(5)),
            HoraFin = TimeOnly.FromTimeSpan(lector.GetTimeSpan(6))
        };
    }
}
