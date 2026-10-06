using CoworkApp.Shared.Data;
using CoworkApp.Shared.Results;
using CoworkApp.Shared.Security;
using CoworkApp.Shared.Time;

namespace CoworkApp.Features.H.Login;

/// <summary>
/// US-25 — autenticación contra dbo.Usuarios.
///
/// El endurecimiento de la tabla (Activo, IntentosFallidos, BloqueadoHasta,
/// AlgoritmoHash) lo trae REF-02; aquí sólo se aplica, porque no hay stored
/// procedure de login. El error siempre es el mismo: no revela si el email
/// existe, si está inactivo o si fue bloqueado.
/// </summary>
public sealed class LoginHandler
{
    private const string MensajeGenerico = "Email o contraseña incorrectos.";
    private const int MaximoIntentos = 5;
    private const int MinutosDeBloqueo = 15;

    private const string SqlUsuario = """
        SELECT
            u.Id             AS Id,
            u.Nombre         AS Nombre,
            u.Email          AS Email,
            u.PasswordHash   AS PasswordHash,
            u.AlgoritmoHash  AS AlgoritmoHash,
            u.Activo         AS Activo,
            u.BloqueadoHasta AS BloqueadoHasta,
            r.Nombre         AS Rol
        FROM dbo.Usuarios u
        INNER JOIN dbo.Roles r ON r.Id = u.RolId
        WHERE u.Email = @Email;
        """;

    private const string SqlIntentoFallido = """
        UPDATE dbo.Usuarios
        SET IntentosFallidos = CASE WHEN IntentosFallidos + 1 >= @Maximo THEN 0 ELSE IntentosFallidos + 1 END,
            BloqueadoHasta   = CASE WHEN IntentosFallidos + 1 >= @Maximo
                                    THEN DATEADD(MINUTE, @Minutos, @Ahora)
                                    ELSE BloqueadoHasta END,
            ActualizadoAt    = @Ahora
        WHERE Id = @Id;
        """;

    private const string SqlExito = """
        UPDATE dbo.Usuarios
        SET IntentosFallidos = 0,
            BloqueadoHasta   = NULL,
            ActualizadoAt    = @Ahora
        WHERE Id = @Id;
        """;

    private readonly Db _db;
    private readonly PasswordHasher _hasher;
    private readonly LoginValidator _validator;
    private readonly IClock _clock;

    public LoginHandler(Db db, PasswordHasher hasher, LoginValidator validator, IClock clock)
    {
        _db = db;
        _hasher = hasher;
        _validator = validator;
        _clock = clock;
    }

    public async Task<Result<LoginRow>> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validacion = _validator.Validate(request);
        if (!validacion.IsValid)
            return Result<LoginRow>.Failure(string.Join(Environment.NewLine, validacion.Errors.Select(e => e.ErrorMessage)));

        var usuario = (await _db.QueryAsync<UsuarioRow>(SqlUsuario, new { request.Email }, cancellationToken))
            .SingleOrDefault();

        if (usuario is null)
        {
            // Hace el mismo trabajo de CPU que un hash real para que un email
            // inexistente no responda más rápido que uno existente.
            _hasher.Hash(request.Password);
            return Result<LoginRow>.Failure(MensajeGenerico);
        }

        DateTime ahora = _clock.Now;

        if (usuario.BloqueadoHasta is { } bloqueadoHasta && bloqueadoHasta > ahora)
            return Result<LoginRow>.Failure(MensajeGenerico);

        if (usuario.AlgoritmoHash != PasswordHasher.Algoritmo)
            return Result<LoginRow>.Failure(MensajeGenerico);

        if (!_hasher.Verify(request.Password, usuario.PasswordHash))
        {
            await _db.ExecuteAsync(SqlIntentoFallido, new
            {
                Id = usuario.Id,
                Maximo = MaximoIntentos,
                Minutos = MinutosDeBloqueo,
                Ahora = ahora,
            }, cancellationToken);

            return Result<LoginRow>.Failure(MensajeGenerico);
        }

        if (!usuario.Activo)
            return Result<LoginRow>.Failure(MensajeGenerico);

        await _db.ExecuteAsync(SqlExito, new { Id = usuario.Id, Ahora = ahora }, cancellationToken);

        return Result<LoginRow>.Success(
            new LoginRow(usuario.Id, usuario.Nombre, usuario.Email, usuario.Rol));
    }

    private sealed record UsuarioRow(
        int Id,
        string Nombre,
        string Email,
        string PasswordHash,
        string AlgoritmoHash,
        bool Activo,
        DateTime? BloqueadoHasta,
        string Rol);
}
