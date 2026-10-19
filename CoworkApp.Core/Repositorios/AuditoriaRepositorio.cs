using CoworkApp.Core.Datos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public sealed class AuditoriaRepositorio : IAuditoriaRepositorio
{
    private const string SqlInsertar = """
        INSERT INTO dbo.Auditoria (IdUsuario, Accion, Entidad, IdEntidad, Detalle)
        VALUES (@IdUsuario, @Accion, @Entidad, @IdEntidad, @Detalle);
        """;

    private readonly Db _db;

    public AuditoriaRepositorio(Db db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task RegistrarAsync(
        string accion,
        string entidad,
        int? idEntidad,
        string? detalle,
        int? idUsuario,
        SqlConnection conexion,
        SqlTransaction transaccion,
        CancellationToken ct = default)
    {
        await _db.EjecutarAsync(
            SqlInsertar,
            new[]
            {
                DbParametros.Int("@IdUsuario", idUsuario),
                DbParametros.VarChar("@Accion", accion, 50),
                DbParametros.VarChar("@Entidad", entidad, 50),
                DbParametros.Int("@IdEntidad", idEntidad),
                DbParametros.NVarChar("@Detalle", detalle, -1)
            },
            conexion,
            transaccion,
            ct).ConfigureAwait(false);
    }
}
