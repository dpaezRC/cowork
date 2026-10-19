namespace CoworkApp.Core.Modelos;

public sealed class Deuda
{
    public int IdDeuda { get; set; }
    public int IdReserva { get; set; }
    public decimal ImporteExigible { get; set; }
    public DateTime FechaGeneracion { get; set; }
    public string Estado { get; set; } = string.Empty;
}
