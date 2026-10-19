using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public sealed class EdificioRepositorio : IEdificioRepositorio
{
    private const string SqlListar = """
        SELECT IdEdificio, Nombre, Direccion, Estado, FechaAlta
        FROM dbo.Edificio
        ORDER BY Nombre;
        """;

    private const string SqlObtener = """
        SELECT IdEdificio, Nombre, Direccion, Estado, FechaAlta
        FROM dbo.Edificio
        WHERE IdEdificio = @IdEdificio;
        """;

    private const string SqlBuscarPorNombre = """
        SELECT IdEdificio, Nombre, Direccion, Estado, FechaAlta
        FROM dbo.Edificio
        WHERE LOWER(Nombre) = LOWER(@Nombre);
        """;

    private const string SqlNombreExiste = """
        SELECT COUNT(*)
        FROM dbo.Edificio
        WHERE LOWER(Nombre) = LOWER(@Nombre)
          AND (@ExcluirId IS NULL OR IdEdificio <> @ExcluirId);
        """;

    private const string SqlAgregar = """
        INSERT INTO dbo.Edificio (Nombre, Direccion)
        OUTPUT INSERTED.IdEdificio
        VALUES (@Nombre, @Direccion);
        """;

    private const string SqlActualizar = """
        UPDATE dbo.Edificio
        SET Nombre = @Nombre, Direccion = @Direccion, Estado = @Estado
        WHERE IdEdificio = @IdEdificio;
        """;

    private const string SqlActualizarEstado = """
        UPDATE dbo.Edificio
        SET Estado = @Estado
        WHERE IdEdificio = @IdEdificio;
        """;

    private readonly Db _db;

    public EdificioRepositorio(Db db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<List<Edificio>> ListarAsync(CancellationToken ct = default)
    {
        return _db.ConsultarAsync(SqlListar, Mapear, ct: ct);
    }

    public Task<Edificio?> ObtenerAsync(
        int idEdificio,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.ConsultarUnoAsync(
            SqlObtener,
            Mapear,
            new[] { DbParametros.Int("@IdEdificio", idEdificio) },
            conexion,
            transaccion,
            ct);
    }

    public Task<Edificio?> BuscarPorNombreAsync(string nombre, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(nombre);

        return _db.ConsultarUnoAsync(
            SqlBuscarPorNombre,
            Mapear,
            new[] { DbParametros.NVarChar("@Nombre", nombre.Trim(), 100) },
            ct: ct);
    }

    public async Task<bool> NombreExisteAsync(
        string nombre,
        int? excluirId,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var total = await _db.EscalarAsync<int>(
            SqlNombreExiste,
            new[]
            {
                DbParametros.NVarChar("@Nombre", nombre.Trim(), 100),
                DbParametros.Int("@ExcluirId", excluirId)
            },
            conexion,
            transaccion,
            ct).ConfigureAwait(false);

        return total > 0;
    }

    public Task<int> AgregarAsync(
        string nombre,
        string direccion,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return UnicidadSql.EjecutarAsync(() => _db.EscalarAsync<int>(
            SqlAgregar,
            new[]
            {
                DbParametros.NVarChar("@Nombre", nombre, 100),
                DbParametros.NVarChar("@Direccion", direccion, 200)
            },
            conexion,
            transaccion,
            ct));
    }

    public Task<int> ActualizarAsync(
        int idEdificio,
        string nombre,
        string direccion,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return UnicidadSql.EjecutarAsync(() => _db.EjecutarAsync(
            SqlActualizar,
            new[]
            {
                DbParametros.Int("@IdEdificio", idEdificio),
                DbParametros.NVarChar("@Nombre", nombre, 100),
                DbParametros.NVarChar("@Direccion", direccion, 200),
                DbParametros.VarChar("@Estado", estado, 10)
            },
            conexion,
            transaccion,
            ct));
    }

    public Task<int> ActualizarEstadoAsync(
        int idEdificio,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.EjecutarAsync(
            SqlActualizarEstado,
            new[]
            {
                DbParametros.Int("@IdEdificio", idEdificio),
                DbParametros.VarChar("@Estado", estado, 10)
            },
            conexion,
            transaccion,
            ct);
    }

    private static Edificio Mapear(SqlDataReader lector)
    {
        return new Edificio
        {
            IdEdificio = lector.GetInt32(0),
            Nombre = lector.GetString(1),
            Direccion = lector.GetString(2),
            Estado = lector.GetString(3),
            FechaAlta = lector.GetDateTime(4)
        };
    }
}
