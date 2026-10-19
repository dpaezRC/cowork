namespace CoworkApp.Core.Modelos;

public sealed class Auditoria
{
    public long IdAuditoria { get; set; }
    public int? IdUsuario { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public int? IdEntidad { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Detalle { get; set; }
}
