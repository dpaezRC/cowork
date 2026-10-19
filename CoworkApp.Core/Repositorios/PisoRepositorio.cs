using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public sealed class PisoRepositorio : IPisoRepositorio
{
    private const string SqlListarPorEdificio = """
        SELECT IdPiso, IdEdificio, Nombre, Estado
        FROM dbo.Piso
        WHERE IdEdificio = @IdEdificio
        ORDER BY Nombre;
        """;

    private const string SqlListarDetallePorEdificio = """
        SELECT p.IdPiso, e.Nombre AS Edificio, p.Nombre, p.Estado
        FROM dbo.Piso p
        INNER JOIN dbo.Edificio e ON e.IdEdificio = p.IdEdificio
        WHERE p.IdEdificio = @IdEdificio
        ORDER BY p.Nombre;
        """;

    private const string SqlObtener = """
        SELECT IdPiso, IdEdificio, Nombre, Estado
        FROM dbo.Piso
        WHERE IdPiso = @IdPiso;
        """;

    private const string SqlBuscarPorNombre = """
        SELECT IdPiso, IdEdificio, Nombre, Estado
        FROM dbo.Piso
        WHERE IdEdificio = @IdEdificio AND LOWER(Nombre) = LOWER(@Nombre);
        """;

    private const string SqlNombreExiste = """
        SELECT COUNT(*)
        FROM dbo.Piso
        WHERE IdEdificio = @IdEdificio
          AND LOWER(Nombre) = LOWER(@Nombre)
          AND (@ExcluirId IS NULL OR IdPiso <> @ExcluirId);
        """;

    private const string SqlAgregar = """
        INSERT INTO dbo.Piso (IdEdificio, Nombre)
        OUTPUT INSERTED.IdPiso
        VALUES (@IdEdificio, @Nombre);
        """;

    private const string SqlActualizar = """
        UPDATE dbo.Piso
        SET Nombre = @Nombre, Estado = @Estado
        WHERE IdPiso = @IdPiso;
        """;

    private const string SqlActualizarEstado = """
        UPDATE dbo.Piso
        SET Estado = @Estado
        WHERE IdPiso = @IdPiso;
        """;

    private readonly Db _db;

    public PisoRepositorio(Db db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<List<Piso>> ListarPorEdificioAsync(int idEdificio, CancellationToken ct = default)
    {
        return _db.ConsultarAsync(
            SqlListarPorEdificio,
            Mapear,
            new[] { DbParametros.Int("@IdEdificio", idEdificio) },
            ct: ct);
    }

    public Task<List<PisoDetalle>> ListarDetallePorEdificioAsync(int idEdificio, CancellationToken ct = default)
    {
        return _db.ConsultarAsync(
            SqlListarDetallePorEdificio,
            MapearDetalle,
            new[] { DbParametros.Int("@IdEdificio", idEdificio) },
            ct: ct);
    }

    public Task<Piso?> ObtenerAsync(
        int idPiso,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.ConsultarUnoAsync(
            SqlObtener,
            Mapear,
            new[] { DbParametros.Int("@IdPiso", idPiso) },
            conexion,
            transaccion,
            ct);
    }

    public Task<Piso?> BuscarPorNombreAsync(int idEdificio, string nombre, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(nombre);

        return _db.ConsultarUnoAsync(
            SqlBuscarPorNombre,
            Mapear,
            new[]
            {
                DbParametros.Int("@IdEdificio", idEdificio),
                DbParametros.NVarChar("@Nombre", nombre.Trim(), 50)
            },
            ct: ct);
    }

    public async Task<bool> NombreExisteAsync(
        int idEdificio,
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
                DbParametros.Int("@IdEdificio", idEdificio),
                DbParametros.NVarChar("@Nombre", nombre.Trim(), 50),
                DbParametros.Int("@ExcluirId", excluirId)
            },
            conexion,
            transaccion,
            ct).ConfigureAwait(false);

        return total > 0;
    }

    public Task<int> AgregarAsync(
        int idEdificio,
        string nombre,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return UnicidadSql.EjecutarAsync(() => _db.EscalarAsync<int>(
            SqlAgregar,
            new[]
            {
                DbParametros.Int("@IdEdificio", idEdificio),
                DbParametros.NVarChar("@Nombre", nombre, 50)
            },
            conexion,
            transaccion,
            ct));
    }

    public Task<int> ActualizarAsync(
        int idPiso,
        string nombre,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return UnicidadSql.EjecutarAsync(() => _db.EjecutarAsync(
            SqlActualizar,
            new[]
            {
                DbParametros.Int("@IdPiso", idPiso),
                DbParametros.NVarChar("@Nombre", nombre, 50),
                DbParametros.VarChar("@Estado", estado, 10)
            },
            conexion,
            transaccion,
            ct));
    }

    public Task<int> ActualizarEstadoAsync(
        int idPiso,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.EjecutarAsync(
            SqlActualizarEstado,
            new[]
            {
                DbParametros.Int("@IdPiso", idPiso),
                DbParametros.VarChar("@Estado", estado, 10)
            },
            conexion,
            transaccion,
            ct);
    }

    private static Piso Mapear(SqlDataReader lector)
    {
        return new Piso
        {
            IdPiso = lector.GetInt32(0),
            IdEdificio = lector.GetInt32(1),
            Nombre = lector.GetString(2),
            Estado = lector.GetString(3)
        };
    }

    private static PisoDetalle MapearDetalle(SqlDataReader lector)
    {
        return new PisoDetalle
        {
            IdPiso = lector.GetInt32(0),
            Edificio = lector.GetString(1),
            Nombre = lector.GetString(2),
            Estado = lector.GetString(3)
        };
    }
}
