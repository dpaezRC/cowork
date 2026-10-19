using System.Data;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Datos;

public sealed class Db : ITransaccionDb, IRelojServidor
{
    public Db(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("La cadena de conexion es obligatoria.", nameof(connectionString));
        }

        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public async Task<int> EjecutarAsync(
        string sql,
        IReadOnlyCollection<SqlParameter>? parametros = null,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return await EjecutarConConexionAsync(conexion, transaccion, async (conexionActual, transaccionActual) =>
        {
            await using var comando = CrearComando(sql, conexionActual, transaccionActual, parametros);
            return await comando.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);
    }

    public async Task<T> EscalarAsync<T>(
        string sql,
        IReadOnlyCollection<SqlParameter>? parametros = null,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return await EjecutarConConexionAsync(conexion, transaccion, async (conexionActual, transaccionActual) =>
        {
            await using var comando = CrearComando(sql, conexionActual, transaccionActual, parametros);
            var valor = await comando.ExecuteScalarAsync(ct).ConfigureAwait(false);

            if (valor is null or DBNull)
            {
                throw new InvalidOperationException("La consulta escalar no devolvio un valor.");
            }

            return (T)valor;
        }, ct).ConfigureAwait(false);
    }

    public async Task<List<T>> ConsultarAsync<T>(
        string sql,
        Func<SqlDataReader, T> mapeo,
        IReadOnlyCollection<SqlParameter>? parametros = null,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(mapeo);

        return await EjecutarConConexionAsync(conexion, transaccion, async (conexionActual, transaccionActual) =>
        {
            await using var comando = CrearComando(sql, conexionActual, transaccionActual, parametros);
            await using var lector = await comando.ExecuteReaderAsync(ct).ConfigureAwait(false);
            var resultado = new List<T>();

            while (await lector.ReadAsync(ct).ConfigureAwait(false))
            {
                resultado.Add(mapeo(lector));
            }

            return resultado;
        }, ct).ConfigureAwait(false);
    }

    public async Task<T?> ConsultarUnoAsync<T>(
        string sql,
        Func<SqlDataReader, T> mapeo,
        IReadOnlyCollection<SqlParameter>? parametros = null,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
        where T : class
    {
        var resultados = await ConsultarAsync(sql, mapeo, parametros, conexion, transaccion, ct).ConfigureAwait(false);
        return resultados.Count == 0 ? null : resultados[0];
    }

    public async Task<DateTime> AhoraServidorAsync(
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return await EscalarAsync<DateTime>(
            "SELECT SYSDATETIME()",
            conexion: conexion,
            transaccion: transaccion,
            ct: ct).ConfigureAwait(false);
    }

    public async Task<T> EnTransaccionAsync<T>(
        Func<SqlConnection, SqlTransaction, CancellationToken, Task<T>> accion,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(accion);

        await using var conexion = new SqlConnection(ConnectionString);
        await conexion.OpenAsync(ct).ConfigureAwait(false);
        await using var transaccion = (SqlTransaction)await conexion.BeginTransactionAsync(ct).ConfigureAwait(false);

        try
        {
            var resultado = await accion(conexion, transaccion, ct).ConfigureAwait(false);
            await transaccion.CommitAsync(ct).ConfigureAwait(false);
            return resultado;
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<T> EjecutarConConexionAsync<T>(
        SqlConnection? conexion,
        SqlTransaction? transaccion,
        Func<SqlConnection, SqlTransaction?, Task<T>> accion,
        CancellationToken ct)
    {
        if (transaccion is not null && conexion is null)
        {
            throw new ArgumentException("Se requiere la conexion cuando se usa una transaccion.", nameof(conexion));
        }

        if (conexion is not null)
        {
            if (conexion.State != ConnectionState.Open)
            {
                await conexion.OpenAsync(ct).ConfigureAwait(false);
            }

            return await accion(conexion, transaccion).ConfigureAwait(false);
        }

        await using var conexionPropia = new SqlConnection(ConnectionString);
        await conexionPropia.OpenAsync(ct).ConfigureAwait(false);
        return await accion(conexionPropia, null).ConfigureAwait(false);
    }

    private static SqlCommand CrearComando(
        string sql,
        SqlConnection conexion,
        SqlTransaction? transaccion,
        IReadOnlyCollection<SqlParameter>? parametros)
    {
        var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        comando.Transaction = transaccion;

        if (parametros is not null)
        {
            foreach (var parametro in parametros)
            {
                comando.Parameters.Add(parametro);
            }
        }

        return comando;
    }
}
