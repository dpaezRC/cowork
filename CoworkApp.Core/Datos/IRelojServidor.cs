using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Datos;

public interface IRelojServidor
{
    Task<DateTime> AhoraServidorAsync(
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);
}
