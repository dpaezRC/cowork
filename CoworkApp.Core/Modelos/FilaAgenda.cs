namespace CoworkApp.Core.Modelos;

public sealed class FilaAgenda
{
    public int IdReserva { get; set; }
    public string Oficina { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public string Horario { get; set; } = string.Empty;
    public int Asientos { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string EstadoPago { get; set; } = string.Empty;
    public decimal? Importe { get; set; }
    public string? Ingreso { get; set; }
    public string? Salida { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public bool PendienteSalida { get; set; }
}
