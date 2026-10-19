namespace CoworkApp.Core.Modelos;

public sealed class Oficina
{
    public int IdOficina { get; set; }
    public int IdPiso { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public decimal TarifaHora { get; set; }
    public TimeOnly HoraApertura { get; set; }
    public TimeOnly HoraCierre { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
}
