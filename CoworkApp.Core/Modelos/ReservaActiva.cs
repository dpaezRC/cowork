namespace CoworkApp.Core.Modelos;

public sealed class ReservaActiva
{
    public int IdOficina { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public int CantidadAsientos { get; set; }
}
