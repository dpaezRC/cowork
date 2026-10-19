using CoworkApp.Core.Datos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Tests.Fakes;

public sealed class TransaccionDbFake : ITransaccionDb
{
    public Task<T> EnTransaccionAsync<T>(
        Func<SqlConnection, SqlTransaction, CancellationToken, Task<T>> accion,
        CancellationToken ct = default)
    {
        return accion(null!, null!, ct);
    }
}
