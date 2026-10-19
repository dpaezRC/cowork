namespace CoworkApp.Core.Modelos;

public sealed class Piso
{
    public int IdPiso { get; set; }
    public int IdEdificio { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}
