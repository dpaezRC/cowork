namespace CoworkApp.Core.Modelos;

public sealed class ReservaUso
{
    public int IdReservaUso { get; set; }
    public int IdReserva { get; set; }
    public DateTime? HoraIngreso { get; set; }
    public DateTime? HoraSalida { get; set; }
    public int IdEmpleado { get; set; }
    public string? Incidencias { get; set; }
    public DateTime FechaRegistro { get; set; }
}
