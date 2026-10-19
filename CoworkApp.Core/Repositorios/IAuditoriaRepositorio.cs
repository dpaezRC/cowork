using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public interface IAuditoriaRepositorio
{
    Task RegistrarAsync(
        string accion,
        string entidad,
        int? idEntidad,
        string? detalle,
        int? idUsuario,
        SqlConnection conexion,
        SqlTransaction transaccion,
        CancellationToken ct = default);
}
