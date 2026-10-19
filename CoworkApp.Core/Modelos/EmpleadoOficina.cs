namespace CoworkApp.Core.Modelos;

public sealed class EmpleadoOficina
{
    public int IdEmpleadoOficina { get; set; }
    public int IdEmpleado { get; set; }
    public int IdOficina { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}
