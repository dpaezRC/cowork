namespace CoworkApp.Core.Modelos;

public sealed class Usuario
{
    public int IdUsuario { get; set; }
    public string Email { get; set; } = string.Empty;
    public string HashContrasena { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Rol { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
}
