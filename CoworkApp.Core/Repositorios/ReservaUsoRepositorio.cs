using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public sealed class ReservaUsoRepositorio : IReservaUsoRepositorio
{
    private const string SqlObtenerPorReserva = """
        SELECT IdReservaUso, IdReserva, HoraIngreso, HoraSalida, IdEmpleado, Incidencias, FechaRegistro
        FROM dbo.ReservaUso
        WHERE IdReserva = @IdReserva;
        """;

    private const string SqlAgregar = """
        INSERT INTO dbo.ReservaUso (IdReserva, HoraIngreso, HoraSalida, IdEmpleado, Incidencias)
        OUTPUT INSERTED.IdReservaUso
        VALUES (@IdReserva, @HoraIngreso, @HoraSalida, @IdEmpleado, @Incidencias);
        """;

    private const string SqlActualizar = """
        UPDATE dbo.ReservaUso
        SET HoraIngreso = @HoraIngreso,
            HoraSalida = @HoraSalida,
            IdEmpleado = @IdEmpleado
        WHERE IdReservaUso = @IdReservaUso;
        """;

    private readonly Db _db;

    public ReservaUsoRepositorio(Db db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<ReservaUso?> ObtenerPorReservaAsync(
        int idReserva,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.ConsultarUnoAsync(
            SqlObtenerPorReserva,
            Mapear,
            new[] { DbParametros.Int("@IdReserva", idReserva) },
            conexion,
            transaccion,
            ct);
    }

    public Task<int> AgregarAsync(
        ReservaUso uso,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(uso);

        return UnicidadSql.EjecutarAsync(() => _db.EscalarAsync<int>(
            SqlAgregar,
            new[]
            {
                DbParametros.Int("@IdReserva", uso.IdReserva),
                DbParametros.DateTime2("@HoraIngreso", uso.HoraIngreso),
                DbParametros.DateTime2("@HoraSalida", uso.HoraSalida),
                DbParametros.Int("@IdEmpleado", uso.IdEmpleado),
                DbParametros.NVarChar("@Incidencias", uso.Incidencias, 500)
            },
            conexion,
            transaccion,
            ct));
    }

    public Task<int> ActualizarAsync(
        int idReservaUso,
        DateTime? horaIngreso,
        DateTime? horaSalida,
        int idEmpleado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.EjecutarAsync(
            SqlActualizar,
            new[]
            {
                DbParametros.Int("@IdReservaUso", idReservaUso),
                DbParametros.DateTime2("@HoraIngreso", horaIngreso),
                DbParametros.DateTime2("@HoraSalida", horaSalida),
                DbParametros.Int("@IdEmpleado", idEmpleado)
            },
            conexion,
            transaccion,
            ct);
    }

    private static ReservaUso Mapear(SqlDataReader lector)
    {
        return new ReservaUso
        {
            IdReservaUso = lector.GetInt32(0),
            IdReserva = lector.GetInt32(1),
            HoraIngreso = lector.IsDBNull(2) ? null : lector.GetDateTime(2),
            HoraSalida = lector.IsDBNull(3) ? null : lector.GetDateTime(3),
            IdEmpleado = lector.GetInt32(4),
            Incidencias = lector.IsDBNull(5) ? null : lector.GetString(5),
            FechaRegistro = lector.GetDateTime(6)
        };
    }
}
