namespace CoworkApp.Core.Modelos;

public sealed class Empleado
{
    public int IdEmpleado { get; set; }
    public int IdUsuario { get; set; }
    public int IdEdificio { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
}
