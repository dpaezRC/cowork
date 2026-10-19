namespace CoworkApp.Core.Modelos;

public sealed class AsignacionDetalle
{
    public int IdEmpleadoOficina { get; set; }
    public int IdEmpleado { get; set; }
    public int IdOficina { get; set; }
    public string Oficina { get; set; } = string.Empty;
    public string Edificio { get; set; } = string.Empty;
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}
