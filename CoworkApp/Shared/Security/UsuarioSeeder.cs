using System.Security.Cryptography;
using System.Text;
using CoworkApp.Shared.Data;

namespace CoworkApp.Shared.Security;

public sealed record CredencialesGeneradas(string Nombre, string Email, string Password, string Rol);

/// <summary>
/// Crea las credenciales iniciales sólo si dbo.Usuarios está vacía
/// (punto abierto §7.2: nada de passwords versionados). Se ejecuta una vez
/// en el arranque y devuelve las contraseñas generadas para que la UI las
/// muestre una sola vez.
/// </summary>
public sealed class UsuarioSeeder
{
    private static readonly (string Rol, string Email, string Nombre)[] Semilla =
    [
        ("admin", "admin@cowork.local", "Admin"),
        ("supervisor", "supervisor@cowork.local", "Supervisor"),
        ("cliente", "cliente@cowork.local", "Cliente"),
    ];

    private const string SqlExisteAlguno = "SELECT COUNT(*) FROM dbo.Usuarios;";
    private const string SqlRoles = "SELECT Id, Nombre FROM dbo.Roles WHERE Nombre IN ('admin', 'supervisor', 'cliente');";
    private const string SqlInsert = """
        INSERT INTO dbo.Usuarios (Nombre, Email, PasswordHash, AlgoritmoHash, RolId, Activo)
        VALUES (@Nombre, @Email, @PasswordHash, @Algoritmo, @RolId, 1);
        """;

    private readonly Db _db;
    private readonly PasswordHasher _hasher;

    public UsuarioSeeder(Db db, PasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<IReadOnlyList<CredencialesGeneradas>> EjecutarSiHaceFalta(
        CancellationToken cancellationToken = default)
    {
        int usuarios = await _db.ExecuteScalarAsync<int>(SqlExisteAlguno, cancellationToken: cancellationToken);
        if (usuarios > 0)
            return [];

        var roles = (await _db.QueryAsync<RolRow>(SqlRoles, cancellationToken: cancellationToken))
            .ToDictionary(r => r.Nombre, r => r.Id);

        var credenciales = new List<CredencialesGeneradas>(Semilla.Length);

        foreach (var (rol, email, nombre) in Semilla)
        {
            if (!roles.TryGetValue(rol, out int rolId))
                throw new InvalidOperationException(
                    $"Falta el rol '{rol}' en dbo.Roles. Ejecutá Scripts/002_Seed.sql sobre la base.");

            string password = GenerarPassword();
            await _db.ExecuteAsync(SqlInsert, new
            {
                Nombre = nombre,
                Email = email,
                PasswordHash = _hasher.Hash(password),
                Algoritmo = PasswordHasher.Algoritmo,
                RolId = rolId,
            }, cancellationToken);

            credenciales.Add(new CredencialesGeneradas(nombre, email, password, rol));
        }

        return credenciales;
    }

    private static string GenerarPassword()
    {
        // Sin caracteres ambiguos (0/O, 1/l/I) para que se pueda transcribir a mano.
        const string alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#%+=";

        Span<char> caracteres = stackalloc char[16];
        for (int i = 0; i < caracteres.Length; i++)
            caracteres[i] = alfabeto[RandomNumberGenerator.GetInt32(alfabeto.Length)];

        return new string(caracteres);
    }

    private sealed record RolRow(int Id, string Nombre);
}
