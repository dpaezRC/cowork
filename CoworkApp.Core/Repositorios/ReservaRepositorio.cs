using CoworkApp.Core.Datos;
using CoworkApp.Core.Dominio;
using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public sealed class ReservaRepositorio : IReservaRepositorio
{
    private const string SqlSeleccionDetalle = """
        SELECT r.IdReserva, r.IdUsuario, r.IdOficina, r.Fecha, r.HoraInicio, r.HoraFin, r.CantidadAsientos,
               r.TarifaHoraAplicada, r.ImporteExigible, r.Estado, r.EstadoPago,
               o.Nombre AS Oficina, e.Nombre AS Edificio, p.Nombre AS Piso,
               o.Capacidad, o.HoraApertura, o.HoraCierre
        FROM dbo.Reserva r
        INNER JOIN dbo.Oficina o ON o.IdOficina = r.IdOficina
        INNER JOIN dbo.Piso p ON p.IdPiso = o.IdPiso
        INNER JOIN dbo.Edificio e ON e.IdEdificio = p.IdEdificio
        """;

    private const string SqlObtener = SqlSeleccionDetalle + """

        WHERE r.IdReserva = @IdReserva;
        """;

    private const string SqlListarPorUsuario = SqlSeleccionDetalle + """

        WHERE r.IdUsuario = @IdUsuario
          AND (@Estado IS NULL OR r.Estado = @Estado)
        ORDER BY r.Fecha, r.HoraInicio, r.IdReserva;
        """;

    private const string SqlAgregar = """
        INSERT INTO dbo.Reserva (IdUsuario, IdOficina, Fecha, HoraInicio, HoraFin, CantidadAsientos,
                                 TarifaHoraAplicada, ImporteExigible, Estado, EstadoPago, FechaCancelacion)
        OUTPUT INSERTED.IdReserva
        VALUES (@IdUsuario, @IdOficina, @Fecha, @HoraInicio, @HoraFin, @CantidadAsientos,
                @TarifaHoraAplicada, @ImporteExigible, @Estado, @EstadoPago, @FechaCancelacion);
        """;

    private const string SqlActualizar = """
        UPDATE dbo.Reserva
        SET Estado = @Estado,
            EstadoPago = @EstadoPago,
            ImporteExigible = @ImporteExigible
        WHERE IdReserva = @IdReserva;
        """;

    private const string SqlCancelar = """
        UPDATE dbo.Reserva
        SET Estado = @Estado,
            FechaCancelacion = @FechaCancelacion
        WHERE IdReserva = @IdReserva;
        """;

    private const string SqlListarConfirmadasSinIngreso = SqlSeleccionDetalle + """

        LEFT JOIN dbo.ReservaUso u ON u.IdReserva = r.IdReserva
        WHERE r.Estado = @Estado AND u.HoraIngreso IS NULL
        ORDER BY r.Fecha, r.HoraInicio, r.IdReserva;
        """;

    private const string SqlListarFranjasCobertura = """
        SELECT eo.IdOficina, eo.HoraInicio, eo.HoraFin
        FROM dbo.EmpleadoOficina eo
        INNER JOIN dbo.Empleado e ON e.IdEmpleado = eo.IdEmpleado
        WHERE e.Estado = @EstadoEmpleado
        """;

    private const string SqlListarActivasSuperpuestas = """
        SELECT HoraInicio, HoraFin, CantidadAsientos
        FROM dbo.Reserva WITH (UPDLOCK, HOLDLOCK)
        WHERE IdOficina = @IdOficina AND Fecha = @Fecha
          AND Estado IN ('Confirmada', 'EnCurso')
          AND HoraInicio < @HoraFin AND HoraFin > @HoraInicio
        ORDER BY HoraInicio;
        """;

    private readonly Db _db;

    public ReservaRepositorio(Db db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<int> AgregarAsync(
        Reserva reserva,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reserva);

        return UnicidadSql.EjecutarAsync(() => _db.EscalarAsync<int>(
            SqlAgregar,
            new[]
            {
                DbParametros.Int("@IdUsuario", reserva.IdUsuario),
                DbParametros.Int("@IdOficina", reserva.IdOficina),
                DbParametros.Date("@Fecha", reserva.Fecha),
                DbParametros.Time("@HoraInicio", reserva.HoraInicio),
                DbParametros.Time("@HoraFin", reserva.HoraFin),
                DbParametros.Int("@CantidadAsientos", reserva.CantidadAsientos),
                DbParametros.Decimal("@TarifaHoraAplicada", reserva.TarifaHoraAplicada),
                DbParametros.Decimal("@ImporteExigible", reserva.ImporteExigible),
                DbParametros.VarChar("@Estado", reserva.Estado, 15),
                DbParametros.VarChar("@EstadoPago", reserva.EstadoPago, 15),
                DbParametros.DateTime2("@FechaCancelacion", null)
            },
            conexion,
            transaccion,
            ct));
    }

    public Task<ReservaDetalle?> ObtenerAsync(
        int idReserva,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.ConsultarUnoAsync(
            SqlObtener,
            MapearDetalle,
            new[] { DbParametros.Int("@IdReserva", idReserva) },
            conexion,
            transaccion,
            ct);
    }

    public Task<List<ReservaDetalle>> ListarPorUsuarioAsync(
        int idUsuario,
        string? estado = null,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.ConsultarAsync(
            SqlListarPorUsuario,
            MapearDetalle,
            new[]
            {
                DbParametros.Int("@IdUsuario", idUsuario),
                DbParametros.VarChar("@Estado", estado, 15)
            },
            conexion,
            transaccion,
            ct);
    }

    public Task<List<ReservaDetalle>> ListarPorOficinasYFechaAsync(
        IReadOnlyCollection<int> idsOficinas,
        DateOnly fecha,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(idsOficinas);

        if (idsOficinas.Count == 0)
        {
            return Task.FromResult(new List<ReservaDetalle>());
        }

        var parametros = new List<SqlParameter>
        {
            DbParametros.Date("@Fecha", fecha)
        };
        var filtro = AgregarIds(idsOficinas, "@Oficina", parametros);

        var sql = SqlSeleccionDetalle + $"""

            WHERE r.IdOficina IN ({filtro}) AND r.Fecha = @Fecha
            ORDER BY r.HoraInicio, r.IdReserva;
            """;

        return _db.ConsultarAsync(sql, MapearDetalle, parametros, conexion, transaccion, ct);
    }

    public Task<List<ReservaDetalle>> ListarConfirmadasSinIngresoAsync(
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.ConsultarAsync(
            SqlListarConfirmadasSinIngreso,
            MapearDetalle,
            new[] { DbParametros.VarChar("@Estado", Estados.Confirmada, 15) },
            conexion,
            transaccion,
            ct);
    }

    public Task<List<ReservaActiva>> ListarActivasPorFechaAsync(
        DateOnly fecha,
        IReadOnlyCollection<int> idsOficinas,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(idsOficinas);

        if (idsOficinas.Count == 0)
        {
            return Task.FromResult(new List<ReservaActiva>());
        }

        var parametros = new List<SqlParameter>
        {
            DbParametros.Date("@Fecha", fecha)
        };
        var filtro = AgregarIds(idsOficinas, "@Oficina", parametros);

        var sql = $"""
            SELECT r.IdOficina, r.HoraInicio, r.HoraFin, r.CantidadAsientos
            FROM dbo.Reserva r
            WHERE r.Fecha = @Fecha
              AND r.IdOficina IN ({filtro})
              AND r.Estado IN ('{Estados.Confirmada}', '{Estados.EnCurso}');
            """;

        return _db.ConsultarAsync(sql, MapearActiva, parametros, conexion, transaccion, ct);
    }

    public Task<List<ReservaActiva>> ListarActivasSuperpuestasConBloqueoAsync(
        int idOficina,
        DateOnly fecha,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.ConsultarAsync(
            SqlListarActivasSuperpuestas,
            MapearActiva,
            new[]
            {
                DbParametros.Int("@IdOficina", idOficina),
                DbParametros.Date("@Fecha", fecha),
                DbParametros.Time("@HoraInicio", horaInicio),
                DbParametros.Time("@HoraFin", horaFin)
            },
            conexion,
            transaccion,
            ct);
    }

    public Task<List<FranjaCobertura>> ListarFranjasCoberturaAsync(
        IReadOnlyCollection<int> idsOficinas,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(idsOficinas);

        if (idsOficinas.Count == 0)
        {
            return Task.FromResult(new List<FranjaCobertura>());
        }

        var parametros = new List<SqlParameter>
        {
            DbParametros.VarChar("@EstadoEmpleado", Estados.Activo, 10)
        };
        var filtro = AgregarIds(idsOficinas, "@Oficina", parametros);

        var sql = SqlListarFranjasCobertura + $"""

            AND eo.IdOficina IN ({filtro});
            """;

        return _db.ConsultarAsync(sql, MapearFranja, parametros, conexion, transaccion, ct);
    }

    public Task<int> ActualizarAsync(
        int idReserva,
        string estado,
        string estadoPago,
        decimal? importeExigible,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.EjecutarAsync(
            SqlActualizar,
            new[]
            {
                DbParametros.Int("@IdReserva", idReserva),
                DbParametros.VarChar("@Estado", estado, 15),
                DbParametros.VarChar("@EstadoPago", estadoPago, 15),
                DbParametros.Decimal("@ImporteExigible", importeExigible)
            },
            conexion,
            transaccion,
            ct);
    }

    public Task<int> CancelarAsync(
        int idReserva,
        DateTime fechaCancelacion,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return _db.EjecutarAsync(
            SqlCancelar,
            new[]
            {
                DbParametros.Int("@IdReserva", idReserva),
                DbParametros.VarChar("@Estado", Estados.Cancelada, 15),
                DbParametros.DateTime2("@FechaCancelacion", fechaCancelacion)
            },
            conexion,
            transaccion,
            ct);
    }

    private static string AgregarIds(IReadOnlyCollection<int> ids, string prefijo, List<SqlParameter> parametros)
    {
        var nombres = new List<string>(ids.Count);
        var indice = 0;
        foreach (var id in ids)
        {
            var nombre = $"{prefijo}{indice++}";
            nombres.Add(nombre);
            parametros.Add(DbParametros.Int(nombre, id));
        }

        return string.Join(", ", nombres);
    }

    private static ReservaDetalle MapearDetalle(SqlDataReader lector)
    {
        return new ReservaDetalle
        {
            IdReserva = lector.GetInt32(0),
            IdUsuario = lector.GetInt32(1),
            IdOficina = lector.GetInt32(2),
            Fecha = DateOnly.FromDateTime(lector.GetDateTime(3)),
            HoraInicio = TimeOnly.FromTimeSpan(lector.GetTimeSpan(4)),
            HoraFin = TimeOnly.FromTimeSpan(lector.GetTimeSpan(5)),
            CantidadAsientos = lector.GetInt32(6),
            TarifaHoraAplicada = lector.GetDecimal(7),
            ImporteExigible = lector.IsDBNull(8) ? null : lector.GetDecimal(8),
            Estado = lector.GetString(9),
            EstadoPago = lector.GetString(10),
            Oficina = lector.GetString(11),
            Edificio = lector.GetString(12),
            Piso = lector.GetString(13),
            OficinaCapacidad = lector.GetInt32(14),
            OficinaHoraApertura = TimeOnly.FromTimeSpan(lector.GetTimeSpan(15)),
            OficinaHoraCierre = TimeOnly.FromTimeSpan(lector.GetTimeSpan(16))
        };
    }

    private static ReservaActiva MapearActiva(SqlDataReader lector)
    {
        return new ReservaActiva
        {
            IdOficina = lector.GetInt32(0),
            HoraInicio = TimeOnly.FromTimeSpan(lector.GetTimeSpan(1)),
            HoraFin = TimeOnly.FromTimeSpan(lector.GetTimeSpan(2)),
            CantidadAsientos = lector.GetInt32(3)
        };
    }

    private static FranjaCobertura MapearFranja(SqlDataReader lector)
    {
        return new FranjaCobertura
        {
            IdOficina = lector.GetInt32(0),
            HoraInicio = TimeOnly.FromTimeSpan(lector.GetTimeSpan(1)),
            HoraFin = TimeOnly.FromTimeSpan(lector.GetTimeSpan(2))
        };
    }
}
