namespace CoworkApp.Features.A.EdificiosOficinas.ListarOficinas;

/// <summary>
/// Lo que ve la UI: una fila del listado de oficinas activas.
/// </summary>
public sealed record ListarOficinasRow(
    int Id,
    string Nombre,
    string Tipo,
    int CapacidadTotal,
    decimal PrecioPorMinuto,
    string Edificio,
    int PisoNumero,
    string? PisoDescripcion,
    bool Activa);
