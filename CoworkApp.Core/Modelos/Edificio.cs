namespace CoworkApp.Core.Modelos;

public sealed class Edificio
{
    public int IdEdificio { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
}
