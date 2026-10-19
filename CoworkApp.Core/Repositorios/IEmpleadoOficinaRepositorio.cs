using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public interface IEmpleadoOficinaRepositorio
{
    Task<List<AsignacionDetalle>> ListarPorEmpleadoAsync(
        int idEmpleado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<AsignacionDetalle?> ObtenerDetalleAsync(
        int idAsignacion,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<bool> ExisteFranjaAsync(
        int idEmpleado,
        int idOficina,
        TimeOnly horaInicio,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<bool> TieneAsignacionesAsync(
        int idEmpleado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> AgregarAsync(
        int idEmpleado,
        int idOficina,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> EliminarAsync(
        int idAsignacion,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);
}
