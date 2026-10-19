namespace CoworkApp.Core.Desarrollo;

public sealed record CredencialDemo(string Rol, string Email, string Contrasena);

public sealed record ResultadoDesarrolloSeeder(bool Ejecutado, IReadOnlyList<CredencialDemo> Credenciales)
{
    public static ResultadoDesarrolloSeeder Omitido { get; } = new(false, Array.Empty<CredencialDemo>());
}
