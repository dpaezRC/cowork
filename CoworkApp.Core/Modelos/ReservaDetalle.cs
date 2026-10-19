namespace CoworkApp.Core.Modelos;

public sealed class ReservaDetalle
{
    public int IdReserva { get; set; }
    public int IdUsuario { get; set; }
    public int IdOficina { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public int CantidadAsientos { get; set; }
    public decimal TarifaHoraAplicada { get; set; }
    public decimal? ImporteExigible { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string EstadoPago { get; set; } = string.Empty;
    public string Oficina { get; set; } = string.Empty;
    public string Edificio { get; set; } = string.Empty;
    public string Piso { get; set; } = string.Empty;
    public int OficinaCapacidad { get; set; }
    public TimeOnly OficinaHoraApertura { get; set; }
    public TimeOnly OficinaHoraCierre { get; set; }
}
