using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public sealed class PagoRepositorio : IPagoRepositorio
{
    private const string SqlObtenerPorReserva = """
        SELECT IdPago, IdReserva, Importe, FechaPago, IdEmpleadoConfirma, Referencia, FechaRegistro
        FROM dbo.Pago
        WHERE IdReserva = @IdReserva;
        """;

    private const string SqlAgregar = """
        INSERT INTO dbo.Pago (IdReserva, Importe, FechaPago, IdEmpleadoConfirma, Referencia)
        OUTPUT INSERTED.IdPago
        VALUES (@IdReserva, @Importe, @FechaPago, @IdEmpleadoConfirma, @Referencia);
        """;

    private readonly Db _db;

    public PagoRepositorio(Db db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<Pago?> ObtenerPorReservaAsync(
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
        Pago pago,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pago);

        return UnicidadSql.EjecutarAsync(() => _db.EscalarAsync<int>(
            SqlAgregar,
            new[]
            {
                DbParametros.Int("@IdReserva", pago.IdReserva),
                DbParametros.Decimal("@Importe", pago.Importe),
                DbParametros.DateTime2("@FechaPago", pago.FechaPago),
                DbParametros.Int("@IdEmpleadoConfirma", pago.IdEmpleadoConfirma),
                DbParametros.NVarChar("@Referencia", pago.Referencia, 100)
            },
            conexion,
            transaccion,
            ct));
    }

    private static Pago Mapear(SqlDataReader lector)
    {
        return new Pago
        {
            IdPago = lector.GetInt32(0),
            IdReserva = lector.GetInt32(1),
            Importe = lector.GetDecimal(2),
            FechaPago = lector.GetDateTime(3),
            IdEmpleadoConfirma = lector.GetInt32(4),
            Referencia = lector.IsDBNull(5) ? null : lector.GetString(5),
            FechaRegistro = lector.GetDateTime(6)
        };
    }
}
