using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public interface IPagoRepositorio
{
    Task<Pago?> ObtenerPorReservaAsync(
        int idReserva,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> AgregarAsync(
        Pago pago,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);
}
