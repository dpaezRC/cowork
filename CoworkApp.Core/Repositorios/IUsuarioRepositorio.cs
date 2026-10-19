using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public interface IUsuarioRepositorio
{
    Task<List<Usuario>> ListarAsync(CancellationToken ct = default);

    Task<Usuario?> ObtenerAsync(
        int idUsuario,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken ct = default);

    Task<bool> EmailExisteAsync(
        string email,
        int? excluirId,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> AgregarAsync(
        string email,
        string hashContrasena,
        string nombre,
        string apellido,
        string? telefono,
        string rol,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> ActualizarAsync(
        int idUsuario,
        string nombre,
        string apellido,
        string email,
        string? telefono,
        string estado,
        string? hashContrasena,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);
}
