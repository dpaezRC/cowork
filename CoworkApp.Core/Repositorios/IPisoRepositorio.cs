using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public interface IPisoRepositorio
{
    Task<List<Piso>> ListarPorEdificioAsync(int idEdificio, CancellationToken ct = default);

    Task<List<PisoDetalle>> ListarDetallePorEdificioAsync(int idEdificio, CancellationToken ct = default);

    Task<Piso?> ObtenerAsync(
        int idPiso,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<Piso?> BuscarPorNombreAsync(int idEdificio, string nombre, CancellationToken ct = default);

    Task<bool> NombreExisteAsync(
        int idEdificio,
        string nombre,
        int? excluirId,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> AgregarAsync(
        int idEdificio,
        string nombre,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> ActualizarAsync(
        int idPiso,
        string nombre,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> ActualizarEstadoAsync(
        int idPiso,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);
}
