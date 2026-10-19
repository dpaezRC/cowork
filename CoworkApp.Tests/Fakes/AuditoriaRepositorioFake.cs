using CoworkApp.Core.Repositorios;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Tests.Fakes;

public sealed record RegistroAuditoria(string Accion, string Entidad, int? IdEntidad, string? Detalle, int? IdUsuario);

public sealed class AuditoriaRepositorioFake : IAuditoriaRepositorio
{
    private readonly List<RegistroAuditoria> _registros = new();

    public IReadOnlyList<RegistroAuditoria> Registros => _registros;

    public Task RegistrarAsync(
        string accion,
        string entidad,
        int? idEntidad,
        string? detalle,
        int? idUsuario,
        SqlConnection conexion,
        SqlTransaction transaccion,
        CancellationToken ct = default)
    {
        _registros.Add(new RegistroAuditoria(accion, entidad, idEntidad, detalle, idUsuario));
        return Task.CompletedTask;
    }

    public IReadOnlyList<RegistroAuditoria> PorEntidad(string entidad)
    {
        return _registros.Where(registro => registro.Entidad == entidad).ToList();
    }
}
