namespace CoworkApp.Core.Modelos;

public sealed class PisoDetalle
{
    public int IdPiso { get; set; }
    public string Edificio { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}
