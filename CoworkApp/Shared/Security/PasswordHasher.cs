using System.Security.Cryptography;

namespace CoworkApp.Shared.Security;

/// <summary>
/// PBKDF2-SHA256 con sal por usuario (US-25, REF-02).
/// El formato de la cadena es: ALGORITMO$ITERACIONES$SAL$HASH
/// y se guarda en dbo.Usuarios.PasswordHash; el algoritmo también
/// queda declarado en dbo.Usuarios.AlgoritmoHash.
/// </summary>
public sealed class PasswordHasher
{
    public const string Algoritmo = "PBKDF2-SHA256";

    // OWASP: 600.000 iteraciones de PBKDF2-HMAC-SHA256.
    // Las iteraciones viajan dentro de la cadena, así que se pueden
    // subir en el futuro sin romper los hashes ya guardados.
    private const int Iteraciones = 600_000;
    private const int TamanioSal = 16;
    private const int TamanioHash = 32;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        byte[] sal = RandomNumberGenerator.GetBytes(TamanioSal);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, sal, Iteraciones, HashAlgorithmName.SHA256, TamanioHash);

        return $"{Algoritmo}${Iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hashAlmacenado)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashAlmacenado))
            return false;

        string[] partes = hashAlmacenado.Split('$');
        if (partes.Length != 4 || partes[0] != Algoritmo)
            return false;

        if (!int.TryParse(partes[1], out int iteraciones) || iteraciones <= 0)
            return false;

        byte[] sal;
        byte[] esperado;
        try
        {
            sal = Convert.FromBase64String(partes[2]);
            esperado = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (sal.Length == 0 || esperado.Length == 0)
            return false;

        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(actual, esperado);
    }
}
