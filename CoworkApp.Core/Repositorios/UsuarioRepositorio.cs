using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public sealed class UsuarioRepositorio : IUsuarioRepositorio
{
    private const string SqlListar = """
        SELECT IdUsuario, Email, HashContrasena, Nombre, Apellido, Telefono, Rol, Estado, FechaAlta
        FROM dbo.Usuario
        ORDER BY IdUsuario;
        """;

    private const string SqlObtener = """
        SELECT IdUsuario, Email, HashContrasena, Nombre, Apellido, Telefono, Rol, Estado, FechaAlta
        FROM dbo.Usuario
        WHERE IdUsuario = @IdUsuario;
        """;

    private const string SqlBuscarPorEmail = """
        SELECT IdUsuario, Email, HashContrasena, Nombre, Apellido, Telefono, Rol, Estado, FechaAlta
        FROM dbo.Usuario
        WHERE LOWER(Email) = LOWER(@Email);
        """;

    private const string SqlEmailExiste = """
        SELECT COUNT(*)
        FROM dbo.Usuario
        WHERE LOWER(Email) = LOWER(@Email)
          AND (@ExcluirId IS NULL OR IdUsuario <> @ExcluirId);
        """;

    private const string SqlAgregar = """
        INSERT INTO dbo.Usuario (Email, HashContrasena, Nombre, Apellido, Telefono, Rol, Estado)
        OUTPUT INSERTED.IdUsuario
        VALUES (@Email, @HashContrasena, @Nombre, @Apellido, @Telefono, @Rol, @Estado);
        """;

    private const string SqlActualizar = """
        UPDATE dbo.Usuario
        SET Nombre = @Nombre,
            Apellido = @Apellido,
            Email = @Email,
            Telefono = @Telefono,
            Estado = @Estado,
            HashContrasena = COALESCE(@HashContrasena, HashContrasena)
        WHERE IdUsuario = @IdUsuario;
        """;

    private readonly Db _db;

    public UsuarioRepositorio(Db db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<List<Usuario>> ListarAsync(CancellationToken ct = default)
    {
        return _db.ConsultarAsync(SqlListar, Mapear, ct: ct);
    }

    public Task<Usuario?> ObtenerAsync(
        int idUsuario,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.ConsultarUnoAsync(
            SqlObtener,
            Mapear,
            new[] { DbParametros.Int("@IdUsuario", idUsuario) },
            conexion,
            transaccion,
            ct);
    }

    public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        return _db.ConsultarUnoAsync(
            SqlBuscarPorEmail,
            Mapear,
            new[] { DbParametros.NVarChar("@Email", email.Trim(), 254) },
            ct: ct);
    }

    public async Task<bool> EmailExisteAsync(
        string email,
        int? excluirId,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        var total = await _db.EscalarAsync<int>(
            SqlEmailExiste,
            new[]
            {
                DbParametros.NVarChar("@Email", email.Trim(), 254),
                DbParametros.Int("@ExcluirId", excluirId)
            },
            conexion,
            transaccion,
            ct).ConfigureAwait(false);

        return total > 0;
    }

    public Task<int> AgregarAsync(
        string email,
        string hashContrasena,
        string nombre,
        string apellido,
        string? telefono,
        string rol,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return UnicidadSql.EjecutarAsync(() => _db.EscalarAsync<int>(
            SqlAgregar,
            new[]
            {
                DbParametros.NVarChar("@Email", email, 254),
                DbParametros.VarChar("@HashContrasena", hashContrasena, 255),
                DbParametros.NVarChar("@Nombre", nombre, 100),
                DbParametros.NVarChar("@Apellido", apellido, 100),
                DbParametros.NVarChar("@Telefono", telefono, 30),
                DbParametros.VarChar("@Rol", rol, 15),
                DbParametros.VarChar("@Estado", estado, 10)
            },
            conexion,
            transaccion,
            ct));
    }

    public Task<int> ActualizarAsync(
        int idUsuario,
        string nombre,
        string apellido,
        string email,
        string? telefono,
        string estado,
        string? hashContrasena,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return UnicidadSql.EjecutarAsync(() => _db.EjecutarAsync(
            SqlActualizar,
            new[]
            {
                DbParametros.Int("@IdUsuario", idUsuario),
                DbParametros.NVarChar("@Nombre", nombre, 100),
                DbParametros.NVarChar("@Apellido", apellido, 100),
                DbParametros.NVarChar("@Email", email, 254),
                DbParametros.NVarChar("@Telefono", telefono, 30),
                DbParametros.VarChar("@Estado", estado, 10),
                DbParametros.VarChar("@HashContrasena", hashContrasena, 255)
            },
            conexion,
            transaccion,
            ct));
    }

    private static Usuario Mapear(SqlDataReader lector)
    {
        return new Usuario
        {
            IdUsuario = lector.GetInt32(0),
            Email = lector.GetString(1),
            HashContrasena = lector.GetString(2),
            Nombre = lector.GetString(3),
            Apellido = lector.GetString(4),
            Telefono = lector.IsDBNull(5) ? null : lector.GetString(5),
            Rol = lector.GetString(6),
            Estado = lector.GetString(7),
            FechaAlta = lector.GetDateTime(8)
        };
    }
}
