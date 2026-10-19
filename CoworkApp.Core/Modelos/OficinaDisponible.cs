namespace CoworkApp.Core.Modelos;

public sealed class OficinaDisponible
{
    public int IdOficina { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Edificio { get; set; } = string.Empty;
    public string Piso { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public int Disponibles { get; set; }
    public decimal Tarifa { get; set; }
}
