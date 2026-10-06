using System.Data;
using CoworkApp.Shared.Configuration;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Shared.Data;

/// <summary>
/// Única puerta de acceso a SQL Server. Toda consulta pasa por acá:
/// abre la conexión, aplica Dapper y respeta el CancellationToken.
/// </summary>
public sealed class Db
{
    private readonly string _connectionString;

    public Db(DatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _connectionString = options.ConnectionString;
    }

    public SqlConnection CreateConnection() => new(_connectionString);

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);
            return conn.State == ConnectionState.Open;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);
        var rows = await conn.QueryAsync<T>(new CommandDefinition(sql, param, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<T>(new CommandDefinition(sql, param, cancellationToken: cancellationToken));
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);
        return await conn.ExecuteAsync(new CommandDefinition(sql, param, cancellationToken: cancellationToken));
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<T>(new CommandDefinition(sql, param, cancellationToken: cancellationToken));
    }
}
