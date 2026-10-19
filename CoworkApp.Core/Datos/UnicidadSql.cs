using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Datos;

internal static class UnicidadSql
{
    public static async Task<int> EjecutarAsync(Func<Task<int>> accion)
    {
        try
        {
            return await accion().ConfigureAwait(false);
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            throw new ConflictoUnicidadException(ex);
        }
    }
}
