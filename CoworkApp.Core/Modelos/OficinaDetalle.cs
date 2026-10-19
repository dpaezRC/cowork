namespace CoworkApp.Core.Modelos;

public sealed class OficinaDetalle
{
    public int IdOficina { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Edificio { get; set; } = string.Empty;
    public string Piso { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public decimal TarifaHora { get; set; }
    public TimeOnly HoraApertura { get; set; }
    public TimeOnly HoraCierre { get; set; }
    public string Estado { get; set; } = string.Empty;
}
