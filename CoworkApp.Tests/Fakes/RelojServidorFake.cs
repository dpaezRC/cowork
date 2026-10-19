using CoworkApp.Core.Datos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Tests.Fakes;

public sealed class RelojServidorFake : IRelojServidor
{
    public DateTime Ahora { get; set; } = new(2030, 6, 15, 8, 0, 0);

    public Task<DateTime> AhoraServidorAsync(
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(Ahora);
    }
}
