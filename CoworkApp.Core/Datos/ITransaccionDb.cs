using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Datos;

public interface ITransaccionDb
{
    Task<T> EnTransaccionAsync<T>(
        Func<SqlConnection, SqlTransaction, CancellationToken, Task<T>> accion,
        CancellationToken ct = default);
}
