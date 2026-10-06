namespace CoworkApp.Features.A.EdificiosOficinas.ListarOficinas;

/// <summary>
/// Contrato de entrada del listado de oficinas. Sin filtros todavía:
/// el único criterio (sólo activas) vive en el SQL del handler.
/// </summary>
public sealed record ListarOficinasRequest;
