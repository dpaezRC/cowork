using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public interface IOficinaRepositorio
{
    Task<List<Oficina>> ListarAsync(CancellationToken ct = default);

    Task<List<OficinaDetalle>> ListarDetalleAsync(CancellationToken ct = default);

    Task<Oficina?> ObtenerAsync(
        int idOficina,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<List<Oficina>> ListarPorEdificioAsync(int idEdificio, CancellationToken ct = default);

    Task<Oficina?> BuscarPorNombreAsync(int idPiso, string nombre, CancellationToken ct = default);

    Task<bool> NombreExisteAsync(
        int idPiso,
        string nombre,
        int? excluirId,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> AgregarAsync(
        int idPiso,
        string nombre,
        int capacidad,
        decimal tarifa,
        TimeOnly horaApertura,
        TimeOnly horaCierre,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> ActualizarAsync(
        int idOficina,
        string nombre,
        int capacidad,
        decimal tarifa,
        TimeOnly horaApertura,
        TimeOnly horaCierre,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> ActualizarEstadoAsync(
        int idOficina,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);
}
