namespace CoworkApp.Core.Modelos;

public sealed class Pago
{
    public int IdPago { get; set; }
    public int IdReserva { get; set; }
    public decimal Importe { get; set; }
    public DateTime FechaPago { get; set; }
    public int IdEmpleadoConfirma { get; set; }
    public string? Referencia { get; set; }
    public DateTime FechaRegistro { get; set; }
}
