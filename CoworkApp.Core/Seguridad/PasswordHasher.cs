using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CoworkApp.Core.Seguridad;

public static class PasswordHasher
{
    public const string Algoritmo = "pbkdf2_sha256";
    public const int Iteraciones = 100_000;
    public const int BytesSalt = 16;
    public const int BytesHash = 32;

    public static string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(BytesSalt);
        var derivado = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iteraciones,
            HashAlgorithmName.SHA256,
            BytesHash);

        return $"{Algoritmo}${Iteraciones}${Convert.ToHexString(salt).ToLowerInvariant()}${Convert.ToHexString(derivado).ToLowerInvariant()}";
    }

    public static bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(hash))
        {
            return false;
        }

        try
        {
            var partes = hash.Split('$');
            if (partes.Length != 4 || partes[0] != Algoritmo)
            {
                return false;
            }

            if (!int.TryParse(partes[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var iteraciones)
                || iteraciones <= 0)
            {
                return false;
            }

            var salt = Convert.FromHexString(partes[2]);
            var esperado = Convert.FromHexString(partes[3]);
            if (salt.Length == 0)
            {
                return false;
            }

            var derivado = Rfc2898DeriveBytes.Pbkdf2(
                password ?? string.Empty,
                salt,
                iteraciones,
                HashAlgorithmName.SHA256,
                BytesHash);

            return CryptographicOperations.FixedTimeEquals(derivado, esperado);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or CryptographicException or OverflowException)
        {
            return false;
        }
    }
}
