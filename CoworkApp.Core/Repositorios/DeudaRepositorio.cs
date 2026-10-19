using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public sealed class DeudaRepositorio : IDeudaRepositorio
{
    private const string SqlObtenerPorReserva = """
        SELECT IdDeuda, IdReserva, ImporteExigible, FechaGeneracion, Estado
        FROM dbo.Deuda
        WHERE IdReserva = @IdReserva;
        """;

    private const string SqlAgregar = """
        INSERT INTO dbo.Deuda (IdReserva, ImporteExigible, Estado)
        OUTPUT INSERTED.IdDeuda
        VALUES (@IdReserva, @ImporteExigible, @Estado);
        """;

    private const string SqlActualizarEstado = """
        UPDATE dbo.Deuda
        SET Estado = @Estado
        WHERE IdDeuda = @IdDeuda;
        """;

    private readonly Db _db;

    public DeudaRepositorio(Db db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<Deuda?> ObtenerPorReservaAsync(
        int idReserva,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.ConsultarUnoAsync(
            SqlObtenerPorReserva,
            Mapear,
            new[] { DbParametros.Int("@IdReserva", idReserva) },
            conexion,
            transaccion,
            ct);
    }

    public Task<int> AgregarAsync(
        int idReserva,
        decimal importeExigible,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return UnicidadSql.EjecutarAsync(() => _db.EscalarAsync<int>(
            SqlAgregar,
            new[]
            {
                DbParametros.Int("@IdReserva", idReserva),
                DbParametros.Decimal("@ImporteExigible", importeExigible),
                DbParametros.VarChar("@Estado", estado, 10)
            },
            conexion,
            transaccion,
            ct));
    }

    public Task<int> ActualizarEstadoAsync(
        int idDeuda,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.EjecutarAsync(
            SqlActualizarEstado,
            new[]
            {
                DbParametros.Int("@IdDeuda", idDeuda),
                DbParametros.VarChar("@Estado", estado, 10)
            },
            conexion,
            transaccion,
            ct);
    }

    private static Deuda Mapear(SqlDataReader lector)
    {
        return new Deuda
        {
            IdDeuda = lector.GetInt32(0),
            IdReserva = lector.GetInt32(1),
            ImporteExigible = lector.GetDecimal(2),
            FechaGeneracion = lector.GetDateTime(3),
            Estado = lector.GetString(4)
        };
    }
}
