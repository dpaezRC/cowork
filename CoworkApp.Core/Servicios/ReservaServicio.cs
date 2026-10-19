using CoworkApp.Core.Datos;
using CoworkApp.Core.Dominio;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Validacion;

namespace CoworkApp.Core.Servicios;

public sealed class ReservaServicio
{
    public const int DuracionMinimaMinutos = 60;
    public const int HorasPlazoCancelacion = 4;

    public const string MensajeFinPosteriorAlInicio = "La hora de fin debe ser posterior a la hora de inicio.";
    public const string MensajeDuracionMinima = "La reserva debe durar al menos 60 minutos.";
    public const string MensajeAsientosPositivos = "La cantidad de asientos debe ser mayor que cero.";
    public const string MensajeInicioAnterior = "El inicio de la reserva no puede ser anterior a la fecha y hora actuales.";
    public const string MensajeClienteNoExiste = "El cliente seleccionado no existe.";
    public const string MensajeSoloCliente = "Solo se pueden crear reservas para usuarios con rol Cliente.";
    public const string MensajeClienteInactivo = "El cliente seleccionado no esta activo.";
    public const string MensajeOficinaNoExiste = "La oficina seleccionada no existe.";
    public const string MensajeOficinaNoHabilitada = "La oficina no esta habilitada para reservar.";
    public const string MensajeSinCobertura = "La oficina no tiene cobertura de empleados durante todo el intervalo solicitado.";
    public const string MensajeSinAsientos = "No hay asientos disponibles suficientes en el intervalo solicitado.";
    public const string MensajeReservaNoExiste = "La reserva no existe.";
    public const string MensajeNoPertenece = "La reserva no pertenece al cliente seleccionado.";
    public const string MensajeSoloConfirmada = "Solo se pueden cancelar reservas en estado Confirmada.";

    public const string MensajeFueraDePlazo =
        "Se ha superado el plazo de cancelación de 4 horas previas al inicio. Ya no es posible cancelar la reserva. " +
        "Recordá que, si no asistís, deberás abonar igualmente el espacio reservado.";

    private readonly IUsuarioRepositorio _usuarios;
    private readonly IOficinaRepositorio _oficinas;
    private readonly IReservaRepositorio _reservas;
    private readonly IAuditoriaRepositorio _auditoria;
    private readonly ITransaccionDb _transaccion;
    private readonly IRelojServidor _reloj;

    public ReservaServicio(
        IUsuarioRepositorio usuarios,
        IOficinaRepositorio oficinas,
        IReservaRepositorio reservas,
        IAuditoriaRepositorio auditoria,
        ITransaccionDb transaccion,
        IRelojServidor reloj)
    {
        _usuarios = usuarios ?? throw new ArgumentNullException(nameof(usuarios));
        _oficinas = oficinas ?? throw new ArgumentNullException(nameof(oficinas));
        _reservas = reservas ?? throw new ArgumentNullException(nameof(reservas));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _transaccion = transaccion ?? throw new ArgumentNullException(nameof(transaccion));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
    }

    public async Task<List<Usuario>> ListarClientesAsync(CancellationToken ct = default)
    {
        var usuarios = await _usuarios.ListarAsync(ct).ConfigureAwait(false);
        return usuarios.Where(usuario => usuario.Rol == Roles.Cliente && usuario.Estado == Estados.Activo).ToList();
    }

    public Task<Usuario?> ObtenerUsuarioAsync(int idUsuario, CancellationToken ct = default)
    {
        return _usuarios.ObtenerAsync(idUsuario, ct: ct);
    }

    public async Task<List<OficinaDisponible>> BuscarDisponibilidadAsync(
        DateOnly fecha,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        int asientos,
        CancellationToken ct = default)
    {
        var ahora = await _reloj.AhoraServidorAsync(ct: ct).ConfigureAwait(false);
        ValidarPeriodo(fecha, horaInicio, horaFin, asientos, ahora);

        var oficinas = (await _oficinas.ListarDetalleAsync(ct).ConfigureAwait(false))
            .Where(oficina => oficina.Estado == Estados.Habilitada)
            .ToList();
        var ids = oficinas.Select(oficina => oficina.IdOficina).ToList();
        var reservas = await _reservas.ListarActivasPorFechaAsync(fecha, ids, ct: ct).ConfigureAwait(false);
        var franjas = await _reservas.ListarFranjasCoberturaAsync(ids, ct: ct).ConfigureAwait(false);

        var resultados = new List<OficinaDisponible>();
        foreach (var oficina in oficinas)
        {
            if (!DentroDelHorario(oficina.HoraApertura, oficina.HoraCierre, horaInicio, horaFin))
            {
                continue;
            }

            if (asientos > oficina.Capacidad)
            {
                continue;
            }

            var superpuestas = reservas
                .Where(reserva => reserva.IdOficina == oficina.IdOficina && reserva.HoraInicio < horaFin && reserva.HoraFin > horaInicio)
                .ToList();
            var disponibles = AsientosDisponibles(oficina.Capacidad, horaInicio, horaFin, superpuestas);
            if (disponibles < asientos)
            {
                continue;
            }

            var franjasOficina = franjas
                .Where(franja => franja.IdOficina == oficina.IdOficina)
                .Select(franja => (franja.HoraInicio, franja.HoraFin))
                .ToList();
            if (!CoberturaContinua(franjasOficina, horaInicio, horaFin))
            {
                continue;
            }

            resultados.Add(new OficinaDisponible
            {
                IdOficina = oficina.IdOficina,
                Nombre = oficina.Nombre,
                Edificio = oficina.Edificio,
                Piso = oficina.Piso,
                Capacidad = oficina.Capacidad,
                Disponibles = disponibles,
                Tarifa = oficina.TarifaHora
            });
        }

        return resultados.OrderBy(resultado => resultado.IdOficina).ToList();
    }

    public async Task<Reserva> CrearAsync(
        int idUsuario,
        int idOficina,
        DateOnly fecha,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        int asientos,
        CancellationToken ct = default)
    {
        return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
        {
            var ahora = await _reloj.AhoraServidorAsync(conexion, transaccion, token).ConfigureAwait(false);

            var usuario = await _usuarios.ObtenerAsync(idUsuario, conexion, transaccion, token).ConfigureAwait(false);
            if (usuario is null)
            {
                throw new ReglaNegocioException(MensajeClienteNoExiste);
            }

            if (usuario.Rol != Roles.Cliente)
            {
                throw new ReglaNegocioException(MensajeSoloCliente);
            }

            if (usuario.Estado != Estados.Activo)
            {
                throw new ReglaNegocioException(MensajeClienteInactivo);
            }

            var oficina = await _oficinas.ObtenerAsync(idOficina, conexion, transaccion, token).ConfigureAwait(false);
            if (oficina is null)
            {
                throw new ReglaNegocioException(MensajeOficinaNoExiste);
            }

            if (oficina.Estado != Estados.Habilitada)
            {
                throw new ReglaNegocioException(MensajeOficinaNoHabilitada);
            }

            ValidarPeriodo(fecha, horaInicio, horaFin, asientos, ahora);

            if (!DentroDelHorario(oficina.HoraApertura, oficina.HoraCierre, horaInicio, horaFin))
            {
                throw new ReglaNegocioException(MensajeHorario(oficina.HoraApertura, oficina.HoraCierre));
            }

            if (asientos > oficina.Capacidad)
            {
                throw new ReglaNegocioException($"La cantidad de asientos supera la capacidad de la oficina ({oficina.Capacidad}).");
            }

            var franjas = await _reservas.ListarFranjasCoberturaAsync([idOficina], conexion, transaccion, token).ConfigureAwait(false);
            if (!CoberturaContinua(franjas.Select(franja => (franja.HoraInicio, franja.HoraFin)).ToList(), horaInicio, horaFin))
            {
                throw new ReglaNegocioException(MensajeSinCobertura);
            }

            var superpuestas = await _reservas
                .ListarActivasSuperpuestasConBloqueoAsync(idOficina, fecha, horaInicio, horaFin, conexion, transaccion, token)
                .ConfigureAwait(false);
            if (!HayCapacidad(oficina.Capacidad, asientos, horaInicio, horaFin, superpuestas))
            {
                throw new ReglaNegocioException(MensajeSinAsientos);
            }

            var reserva = new Reserva
            {
                IdUsuario = idUsuario,
                IdOficina = idOficina,
                Fecha = fecha,
                HoraInicio = horaInicio,
                HoraFin = horaFin,
                CantidadAsientos = asientos,
                TarifaHoraAplicada = oficina.TarifaHora,
                ImporteExigible = null,
                Estado = Estados.Confirmada,
                EstadoPago = Estados.Pendiente
            };

            var idReserva = await _reservas.AgregarAsync(reserva, conexion, transaccion, token).ConfigureAwait(false);
            reserva.IdReserva = idReserva;

            await _auditoria.RegistrarAsync(
                "Alta",
                "Reserva",
                idReserva,
                Detalles.Alta(
                [
                    new("IdUsuario", idUsuario),
                    new("IdOficina", idOficina),
                    new("Fecha", fecha.ToString("yyyy-MM-dd")),
                    new("HoraInicio", horaInicio.ToString("HH:mm")),
                    new("HoraFin", horaFin.ToString("HH:mm")),
                    new("CantidadAsientos", asientos),
                    new("TarifaHoraAplicada", oficina.TarifaHora),
                    new("Estado", Estados.Confirmada),
                    new("EstadoPago", Estados.Pendiente)
                ]),
                null,
                conexion,
                transaccion,
                token).ConfigureAwait(false);

            return reserva;
        }, ct).ConfigureAwait(false);
    }

    public Task<List<ReservaDetalle>> ListarPorClienteAsync(
        int idUsuario,
        string? estado = null,
        CancellationToken ct = default)
    {
        return _reservas.ListarPorUsuarioAsync(idUsuario, estado, ct: ct);
    }

    public async Task<bool> CancelarAsync(int idReserva, int idUsuario, CancellationToken ct = default)
    {
        return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
        {
            var ahora = await _reloj.AhoraServidorAsync(conexion, transaccion, token).ConfigureAwait(false);

            var reserva = await _reservas.ObtenerAsync(idReserva, conexion, transaccion, token).ConfigureAwait(false);
            if (reserva is null)
            {
                throw new ReglaNegocioException(MensajeReservaNoExiste);
            }

            if (reserva.IdUsuario != idUsuario)
            {
                throw new ReglaNegocioException(MensajeNoPertenece);
            }

            if (reserva.Estado != Estados.Confirmada)
            {
                throw new ReglaNegocioException(MensajeSoloConfirmada);
            }

            var limite = reserva.Fecha.ToDateTime(reserva.HoraInicio).AddHours(-HorasPlazoCancelacion);
            if (ahora > limite)
            {
                throw new ReglaNegocioException(MensajeFueraDePlazo);
            }

            await _reservas.CancelarAsync(idReserva, ahora, conexion, transaccion, token).ConfigureAwait(false);

            await _auditoria.RegistrarAsync(
                "Cancelacion",
                "Reserva",
                idReserva,
                Detalles.Modificacion([("Estado", Estados.Confirmada, Estados.Cancelada)]),
                null,
                conexion,
                transaccion,
                token).ConfigureAwait(false);

            return true;
        }, ct).ConfigureAwait(false);
    }

    public static decimal ImporteEstimado(int asientos, decimal tarifa, TimeOnly horaInicio, TimeOnly horaFin)
    {
        var minutos = Minutos(horaInicio, horaFin);
        return Math.Round(asientos * tarifa * minutos / 60m, 2, MidpointRounding.AwayFromZero);
    }

    private static void ValidarPeriodo(DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin, int asientos, DateTime ahora)
    {
        if (horaFin <= horaInicio)
        {
            throw new ReglaNegocioException(MensajeFinPosteriorAlInicio);
        }

        if (Minutos(horaInicio, horaFin) < DuracionMinimaMinutos)
        {
            throw new ReglaNegocioException(MensajeDuracionMinima);
        }

        if (asientos <= 0)
        {
            throw new ReglaNegocioException(MensajeAsientosPositivos);
        }

        if (fecha.ToDateTime(horaInicio) < ahora)
        {
            throw new ReglaNegocioException(MensajeInicioAnterior);
        }
    }

    private static bool DentroDelHorario(TimeOnly apertura, TimeOnly cierre, TimeOnly horaInicio, TimeOnly horaFin)
    {
        return apertura <= horaInicio && horaFin <= cierre;
    }

    private static string MensajeHorario(TimeOnly apertura, TimeOnly cierre)
    {
        return $"El horario solicitado esta fuera del horario de funcionamiento de la oficina ({apertura:HH:mm}-{cierre:HH:mm}).";
    }

    private static int Minutos(TimeOnly horaInicio, TimeOnly horaFin)
    {
        return (horaFin.Hour * 60 + horaFin.Minute) - (horaInicio.Hour * 60 + horaInicio.Minute);
    }

    private static List<TimeOnly> PuntosDeControl(TimeOnly horaInicio, TimeOnly horaFin, IReadOnlyList<ReservaActiva> superpuestas)
    {
        var puntos = new List<TimeOnly> { horaInicio };
        foreach (var reserva in superpuestas)
        {
            if (horaInicio < reserva.HoraInicio && reserva.HoraInicio < horaFin)
            {
                puntos.Add(reserva.HoraInicio);
            }
        }

        return puntos;
    }

    private static int AsientosDisponibles(int capacidad, TimeOnly horaInicio, TimeOnly horaFin, IReadOnlyList<ReservaActiva> superpuestas)
    {
        var maxComprometidos = 0;
        foreach (var punto in PuntosDeControl(horaInicio, horaFin, superpuestas))
        {
            var comprometidos = superpuestas
                .Where(reserva => reserva.HoraInicio <= punto && punto < reserva.HoraFin)
                .Sum(reserva => reserva.CantidadAsientos);
            maxComprometidos = Math.Max(maxComprometidos, comprometidos);
        }

        return capacidad - maxComprometidos;
    }

    private static bool HayCapacidad(int capacidad, int asientos, TimeOnly horaInicio, TimeOnly horaFin, IReadOnlyList<ReservaActiva> superpuestas)
    {
        return AsientosDisponibles(capacidad, horaInicio, horaFin, superpuestas) >= asientos;
    }

    private static bool CoberturaContinua(IReadOnlyList<(TimeOnly HoraInicio, TimeOnly HoraFin)> franjas, TimeOnly horaInicio, TimeOnly horaFin)
    {
        var actual = horaInicio;
        foreach (var franja in franjas.OrderBy(franja => franja.HoraInicio).ThenBy(franja => franja.HoraFin))
        {
            if (franja.HoraInicio <= actual && actual < franja.HoraFin)
            {
                actual = actual > franja.HoraFin ? actual : franja.HoraFin;
                if (actual >= horaFin)
                {
                    return true;
                }
            }
            else if (franja.HoraInicio > actual)
            {
                return false;
            }
        }

        return actual >= horaFin;
    }
}
