namespace CoworkApp.Core.Modelos;

public sealed record ResultadoSalida(int IdReserva, decimal Importe, int Facturables, string Motivo);
