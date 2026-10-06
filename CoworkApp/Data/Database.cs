using Microsoft.Data.SqlClient;
using System.Data;

namespace CoworkApp.Data
{
    public class Database
    {
        private readonly string _connectionString;

        public Database(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Obtiene una nueva conexión a SQL Server lista para utilizar en un bloque using.
        /// </summary>
        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Prueba de forma asíncrona si la conexión a la base de datos es válida.
        /// </summary>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync(cancellationToken);
                return conn.State == ConnectionState.Open;
            }
            catch
            {
                return false;
            }
        }
    }
}
