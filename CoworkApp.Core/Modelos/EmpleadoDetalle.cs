namespace CoworkApp.Core.Modelos;

public sealed class EmpleadoDetalle
{
    public int IdEmpleado { get; set; }
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public int IdEdificio { get; set; }
    public string Edificio { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
}
