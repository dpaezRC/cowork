namespace CoworkApp.Core.Modelos;

public sealed class FranjaCobertura
{
    public int IdOficina { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}
