using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public sealed class OficinaRepositorio : IOficinaRepositorio
{
    private const string SqlListar = """
        SELECT IdOficina, IdPiso, Nombre, Capacidad, TarifaHora, HoraApertura, HoraCierre, Estado, FechaAlta
        FROM dbo.Oficina
        ORDER BY IdOficina;
        """;

    private const string SqlListarDetalle = """
        SELECT o.IdOficina, o.Nombre, e.Nombre AS Edificio, p.Nombre AS Piso,
               o.Capacidad, o.TarifaHora, o.HoraApertura, o.HoraCierre, o.Estado
        FROM dbo.Oficina o
        INNER JOIN dbo.Piso p ON p.IdPiso = o.IdPiso
        INNER JOIN dbo.Edificio e ON e.IdEdificio = p.IdEdificio
        ORDER BY o.IdOficina;
        """;

    private const string SqlObtener = """
        SELECT IdOficina, IdPiso, Nombre, Capacidad, TarifaHora, HoraApertura, HoraCierre, Estado, FechaAlta
        FROM dbo.Oficina
        WHERE IdOficina = @IdOficina;
        """;

    private const string SqlListarPorEdificio = """
        SELECT o.IdOficina, o.IdPiso, o.Nombre, o.Capacidad, o.TarifaHora, o.HoraApertura, o.HoraCierre, o.Estado, o.FechaAlta
        FROM dbo.Oficina o
        INNER JOIN dbo.Piso p ON p.IdPiso = o.IdPiso
        WHERE p.IdEdificio = @IdEdificio
        ORDER BY o.Nombre;
        """;

    private const string SqlBuscarPorNombre = """
        SELECT IdOficina, IdPiso, Nombre, Capacidad, TarifaHora, HoraApertura, HoraCierre, Estado, FechaAlta
        FROM dbo.Oficina
        WHERE IdPiso = @IdPiso AND LOWER(Nombre) = LOWER(@Nombre);
        """;

    private const string SqlNombreExiste = """
        SELECT COUNT(*)
        FROM dbo.Oficina
        WHERE IdPiso = @IdPiso
          AND LOWER(Nombre) = LOWER(@Nombre)
          AND (@ExcluirId IS NULL OR IdOficina <> @ExcluirId);
        """;

    private const string SqlAgregar = """
        INSERT INTO dbo.Oficina (IdPiso, Nombre, Capacidad, TarifaHora, HoraApertura, HoraCierre)
        OUTPUT INSERTED.IdOficina
        VALUES (@IdPiso, @Nombre, @Capacidad, @TarifaHora, @HoraApertura, @HoraCierre);
        """;

    private const string SqlActualizar = """
        UPDATE dbo.Oficina
        SET Nombre = @Nombre,
            Capacidad = @Capacidad,
            TarifaHora = @TarifaHora,
            HoraApertura = @HoraApertura,
            HoraCierre = @HoraCierre,
            Estado = @Estado
        WHERE IdOficina = @IdOficina;
        """;

    private const string SqlActualizarEstado = """
        UPDATE dbo.Oficina
        SET Estado = @Estado
        WHERE IdOficina = @IdOficina;
        """;

    private readonly Db _db;

    public OficinaRepositorio(Db db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<List<Oficina>> ListarAsync(CancellationToken ct = default)
    {
        return _db.ConsultarAsync(SqlListar, Mapear, ct: ct);
    }

    public Task<List<OficinaDetalle>> ListarDetalleAsync(CancellationToken ct = default)
    {
        return _db.ConsultarAsync(SqlListarDetalle, MapearDetalle, ct: ct);
    }

    public Task<Oficina?> ObtenerAsync(
        int idOficina,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.ConsultarUnoAsync(
            SqlObtener,
            Mapear,
            new[] { DbParametros.Int("@IdOficina", idOficina) },
            conexion,
            transaccion,
            ct);
    }

    public Task<List<Oficina>> ListarPorEdificioAsync(int idEdificio, CancellationToken ct = default)
    {
        return _db.ConsultarAsync(
            SqlListarPorEdificio,
            Mapear,
            new[] { DbParametros.Int("@IdEdificio", idEdificio) },
            ct: ct);
    }

    public Task<Oficina?> BuscarPorNombreAsync(int idPiso, string nombre, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(nombre);

        return _db.ConsultarUnoAsync(
            SqlBuscarPorNombre,
            Mapear,
            new[]
            {
                DbParametros.Int("@IdPiso", idPiso),
                DbParametros.NVarChar("@Nombre", nombre.Trim(), 100)
            },
            ct: ct);
    }

    public async Task<bool> NombreExisteAsync(
        int idPiso,
        string nombre,
        int? excluirId,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var total = await _db.EscalarAsync<int>(
            SqlNombreExiste,
            new[]
            {
                DbParametros.Int("@IdPiso", idPiso),
                DbParametros.NVarChar("@Nombre", nombre.Trim(), 100),
                DbParametros.Int("@ExcluirId", excluirId)
            },
            conexion,
            transaccion,
            ct).ConfigureAwait(false);

        return total > 0;
    }

    public Task<int> AgregarAsync(
        int idPiso,
        string nombre,
        int capacidad,
        decimal tarifa,
        TimeOnly horaApertura,
        TimeOnly horaCierre,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return UnicidadSql.EjecutarAsync(() => _db.EscalarAsync<int>(
            SqlAgregar,
            new[]
            {
                DbParametros.Int("@IdPiso", idPiso),
                DbParametros.NVarChar("@Nombre", nombre, 100),
                DbParametros.Int("@Capacidad", capacidad),
                DbParametros.Decimal("@TarifaHora", tarifa),
                DbParametros.Time("@HoraApertura", horaApertura),
                DbParametros.Time("@HoraCierre", horaCierre)
            },
            conexion,
            transaccion,
            ct));
    }

    public Task<int> ActualizarAsync(
        int idOficina,
        string nombre,
        int capacidad,
        decimal tarifa,
        TimeOnly horaApertura,
        TimeOnly horaCierre,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return UnicidadSql.EjecutarAsync(() => _db.EjecutarAsync(
            SqlActualizar,
            new[]
            {
                DbParametros.Int("@IdOficina", idOficina),
                DbParametros.NVarChar("@Nombre", nombre, 100),
                DbParametros.Int("@Capacidad", capacidad),
                DbParametros.Decimal("@TarifaHora", tarifa),
                DbParametros.Time("@HoraApertura", horaApertura),
                DbParametros.Time("@HoraCierre", horaCierre),
                DbParametros.VarChar("@Estado", estado, 15)
            },
            conexion,
            transaccion,
            ct));
    }

    public Task<int> ActualizarEstadoAsync(
        int idOficina,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.EjecutarAsync(
            SqlActualizarEstado,
            new[]
            {
                DbParametros.Int("@IdOficina", idOficina),
                DbParametros.VarChar("@Estado", estado, 15)
            },
            conexion,
            transaccion,
            ct);
    }

    private static Oficina Mapear(SqlDataReader lector)
    {
        return new Oficina
        {
            IdOficina = lector.GetInt32(0),
            IdPiso = lector.GetInt32(1),
            Nombre = lector.GetString(2),
            Capacidad = lector.GetInt32(3),
            TarifaHora = lector.GetDecimal(4),
            HoraApertura = TimeOnly.FromTimeSpan(lector.GetTimeSpan(5)),
            HoraCierre = TimeOnly.FromTimeSpan(lector.GetTimeSpan(6)),
            Estado = lector.GetString(7),
            FechaAlta = lector.GetDateTime(8)
        };
    }

    private static OficinaDetalle MapearDetalle(SqlDataReader lector)
    {
        return new OficinaDetalle
        {
            IdOficina = lector.GetInt32(0),
            Nombre = lector.GetString(1),
            Edificio = lector.GetString(2),
            Piso = lector.GetString(3),
            Capacidad = lector.GetInt32(4),
            TarifaHora = lector.GetDecimal(5),
            HoraApertura = TimeOnly.FromTimeSpan(lector.GetTimeSpan(6)),
            HoraCierre = TimeOnly.FromTimeSpan(lector.GetTimeSpan(7)),
            Estado = lector.GetString(8)
        };
    }
}
