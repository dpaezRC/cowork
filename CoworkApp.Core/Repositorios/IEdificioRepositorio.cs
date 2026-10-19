using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public interface IEdificioRepositorio
{
    Task<List<Edificio>> ListarAsync(CancellationToken ct = default);

    Task<Edificio?> ObtenerAsync(
        int idEdificio,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<Edificio?> BuscarPorNombreAsync(string nombre, CancellationToken ct = default);

    Task<bool> NombreExisteAsync(
        string nombre,
        int? excluirId,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> AgregarAsync(
        string nombre,
        string direccion,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> ActualizarAsync(
        int idEdificio,
        string nombre,
        string direccion,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> ActualizarEstadoAsync(
        int idEdificio,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);
}
