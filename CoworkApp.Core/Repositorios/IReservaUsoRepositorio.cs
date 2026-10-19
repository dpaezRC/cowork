using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public interface IReservaUsoRepositorio
{
    Task<ReservaUso?> ObtenerPorReservaAsync(
        int idReserva,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> AgregarAsync(
        ReservaUso uso,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> ActualizarAsync(
        int idReservaUso,
        DateTime? horaIngreso,
        DateTime? horaSalida,
        int idEmpleado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);
}
