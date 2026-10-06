using CoworkApp.Shared.Data;

namespace CoworkApp.Features.A.EdificiosOficinas.ListarOficinas;

/// <summary>
/// US-04 — listado de oficinas activas con su piso y edificio.
/// Mismo comportamiento que el SELECT que vivía en Form1.cs:33.
/// </summary>
public sealed class ListarOficinasHandler
{
    private const string Sql = """
        SELECT
            o.Id             AS Id,
            o.Nombre         AS Nombre,
            o.Tipo           AS Tipo,
            o.CapacidadTotal AS CapacidadTotal,
            o.PrecioPorMinuto AS PrecioPorMinuto,
            e.Nombre         AS Edificio,
            p.Numero         AS PisoNumero,
            p.Descripcion    AS PisoDescripcion,
            o.Activa         AS Activa
        FROM dbo.Oficinas o
        INNER JOIN dbo.Pisos p     ON o.PisoId = p.Id
        INNER JOIN dbo.Edificios e ON p.EdificioId = e.Id
        WHERE o.Activa = 1
        ORDER BY e.Nombre, p.Numero, o.Nombre;
        """;

    private readonly Db _db;

    public ListarOficinasHandler(Db db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<ListarOficinasRow>> HandleAsync(
        ListarOficinasRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _db.QueryAsync<ListarOficinasRow>(Sql, cancellationToken: cancellationToken);
    }
}
