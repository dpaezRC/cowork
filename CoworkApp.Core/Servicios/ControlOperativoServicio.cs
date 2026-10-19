using System.Globalization;
using CoworkApp.Core.Datos;
using CoworkApp.Core.Dominio;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Validacion;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Servicios;

public sealed class ControlOperativoServicio
{
    public const int HorasCorreccion = 24;

    public const string MensajeUsuarioSinEmpleado = "El usuario de la sesion no tiene un empleado asociado.";
    public const string MensajeEmpleadoInactivo = "El empleado no esta activo.";
    public const string MensajeReservaNoExiste = "La reserva no existe.";
    public const string MensajeFueraDeAlcance = "La reserva pertenece a una oficina que no tenes asignada.";
    public const string MensajeIngresoEstado = "Solo se puede registrar el ingreso de una reserva Confirmada o No presentada.";
    public const string MensajeIngresoYaRegistrado = "La reserva ya tiene un ingreso registrado.";
    public const string MensajeUsoDuplicado = "La reserva ya tiene un registro de uso.";
    public const string MensajeSalidaEstado = "Solo se puede registrar la salida de una reserva En curso.";
    public const string MensajeSinIngreso = "La reserva no tiene un ingreso registrado.";
    public const string MensajeSalidaAnteriorAlIngreso = "La hora de salida no puede ser anterior a la hora de ingreso.";
    public const string MensajeCorreccionVencida = "El plazo de 24 horas para corregir la asistencia vencio. Solo el administrador puede corregirla.";
    public const string MensajePagoYaRegistrado = "La reserva ya tiene un pago registrado.";
    public const string MensajeSinImporteExigible = "La reserva no tiene un importe exigible determinado.";

    private readonly IEmpleadoRepositorio _empleados;
    private readonly IEmpleadoOficinaRepositorio _asignaciones;
    private readonly IReservaRepositorio _reservas;
    private readonly IReservaUsoRepositorio _usos;
    private readonly IPagoRepositorio _pagos;
    private readonly IDeudaRepositorio _deudas;
    private readonly IAuditoriaRepositorio _auditoria;
    private readonly ITransaccionDb _transaccion;
    private readonly IRelojServidor _reloj;

    public ControlOperativoServicio(
        IEmpleadoRepositorio empleados,
        IEmpleadoOficinaRepositorio asignaciones,
        IReservaRepositorio reservas,
        IReservaUsoRepositorio usos,
        IPagoRepositorio pagos,
        IDeudaRepositorio deudas,
        IAuditoriaRepositorio auditoria,
        ITransaccionDb transaccion,
        IRelojServidor reloj)
    {
        _empleados = empleados ?? throw new ArgumentNullException(nameof(empleados));
        _asignaciones = asignaciones ?? throw new ArgumentNullException(nameof(asignaciones));
        _reservas = reservas ?? throw new ArgumentNullException(nameof(reservas));
        _usos = usos ?? throw new ArgumentNullException(nameof(usos));
        _pagos = pagos ?? throw new ArgumentNullException(nameof(pagos));
        _deudas = deudas ?? throw new ArgumentNullException(nameof(deudas));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _transaccion = transaccion ?? throw new ArgumentNullException(nameof(transaccion));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
    }

    public Task<DateTime> AhoraServidorAsync(CancellationToken ct = default)
    {
        return _reloj.AhoraServidorAsync(ct: ct);
    }

    public async Task<List<FilaAgenda>> AbrirAgendaAsync(int idUsuario, DateOnly fecha, CancellationToken ct = default)
    {
        return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
        {
            var ahora = await _reloj.AhoraServidorAsync(conexion, transaccion, token).ConfigureAwait(false);
            var empleado = await ResolverEmpleadoAsync(conexion, transaccion, idUsuario, token).ConfigureAwait(false);
            await BarrerInasistenciasAsync(conexion, transaccion, ahora, token).ConfigureAwait(false);
            var idsOficinas = await OficinasAsignadasAsync(conexion, transaccion, empleado.IdEmpleado, token).ConfigureAwait(false);
            var reservas = await _reservas
                .ListarPorOficinasYFechaAsync(idsOficinas, fecha, conexion, transaccion, token)
                .ConfigureAwait(false);

            var filas = new List<FilaAgenda>();
            foreach (var reserva in reservas)
            {
                filas.Add(await CrearFilaAgendaAsync(conexion, transaccion, reserva, token).ConfigureAwait(false));
            }

            return filas;
        }, ct).ConfigureAwait(false);
    }

    public async Task<ResultadoIngreso> RegistrarIngresoAsync(
        int idUsuario,
        int idReserva,
        TimeOnly hora,
        CancellationToken ct = default)
    {
        return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
        {
            var ahora = await _reloj.AhoraServidorAsync(conexion, transaccion, token).ConfigureAwait(false);
            var empleado = await ResolverEmpleadoAsync(conexion, transaccion, idUsuario, token).ConfigureAwait(false);
            var reserva = await ObtenerReservaConAlcanceAsync(conexion, transaccion, empleado.IdEmpleado, idReserva, token).ConfigureAwait(false);

            if (reserva.Estado != Estados.Confirmada && reserva.Estado != Estados.NoPresentada)
            {
                throw new ReglaNegocioException(MensajeIngresoEstado);
            }

            var correccion = reserva.Estado == Estados.NoPresentada;
            if (correccion)
            {
                ValidarVentanaCorreccion(reserva, ahora);
            }

            if (hora < reserva.OficinaHoraApertura)
            {
                throw new ReglaNegocioException($"El ingreso no puede ser anterior a la apertura de la oficina ({reserva.OficinaHoraApertura:HH\\:mm}).");
            }

            var ingreso = reserva.Fecha.ToDateTime(hora);
            var uso = await _usos.ObtenerPorReservaAsync(idReserva, conexion, transaccion, token).ConfigureAwait(false);
            if (uso is not null && uso.HoraIngreso is not null)
            {
                throw new ReglaNegocioException(MensajeIngresoYaRegistrado);
            }

            int idUso;
            if (uso is null)
            {
                var nuevo = new ReservaUso
                {
                    IdReserva = idReserva,
                    HoraIngreso = ingreso,
                    HoraSalida = null,
                    IdEmpleado = empleado.IdEmpleado,
                    Incidencias = null
                };
                try
                {
                    idUso = await _usos.AgregarAsync(nuevo, conexion, transaccion, token).ConfigureAwait(false);
                }
                catch (ConflictoUnicidadException ex)
                {
                    throw new ReglaNegocioException(MensajeUsoDuplicado, ex);
                }
            }
            else
            {
                idUso = uso.IdReservaUso;
                await _usos.ActualizarAsync(uso.IdReservaUso, ingreso, uso.HoraSalida, empleado.IdEmpleado, conexion, transaccion, token).ConfigureAwait(false);
            }

            var estadoPago = correccion ? Estados.Pendiente : reserva.EstadoPago;
            decimal? importe = correccion ? null : reserva.ImporteExigible;
            await _reservas.ActualizarAsync(idReserva, Estados.EnCurso, estadoPago, importe, conexion, transaccion, token).ConfigureAwait(false);

            var deudaAnulada = false;
            if (correccion)
            {
                var deuda = await _deudas.ObtenerPorReservaAsync(idReserva, conexion, transaccion, token).ConfigureAwait(false);
                if (deuda is not null && deuda.Estado == Estados.Pendiente)
                {
                    await _deudas.ActualizarEstadoAsync(deuda.IdDeuda, Estados.Anulada, conexion, transaccion, token).ConfigureAwait(false);
                    deudaAnulada = true;
                }
            }

            await _auditoria.RegistrarAsync(
                correccion ? "CorreccionAsistencia" : "RegistroIngreso",
                "ReservaUso",
                idUso,
                Detalles.Alta(
                [
                    new("IdReserva", idReserva),
                    new("HoraIngreso", ingreso.ToString("yyyy-MM-dd HH:mm")),
                    new("IdEmpleado", empleado.IdEmpleado),
                    new("Estado", Estados.EnCurso)
                ]),
                idUsuario,
                conexion,
                transaccion,
                token).ConfigureAwait(false);

            return new ResultadoIngreso(idReserva, correccion, deudaAnulada);
        }, ct).ConfigureAwait(false);
    }

    public async Task<ResultadoSalida> RegistrarSalidaAsync(
        int idUsuario,
        int idReserva,
        TimeOnly hora,
        CancellationToken ct = default)
    {
        return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
        {
            var empleado = await ResolverEmpleadoAsync(conexion, transaccion, idUsuario, token).ConfigureAwait(false);
            var reserva = await ObtenerReservaConAlcanceAsync(conexion, transaccion, empleado.IdEmpleado, idReserva, token).ConfigureAwait(false);

            if (reserva.Estado != Estados.EnCurso)
            {
                throw new ReglaNegocioException(MensajeSalidaEstado);
            }

            var uso = await _usos.ObtenerPorReservaAsync(idReserva, conexion, transaccion, token).ConfigureAwait(false);
            if (uso is null || uso.HoraIngreso is null)
            {
                throw new ReglaNegocioException(MensajeSinIngreso);
            }

            var salida = reserva.Fecha.ToDateTime(hora);
            if (salida < uso.HoraIngreso.Value)
            {
                throw new ReglaNegocioException(MensajeSalidaAnteriorAlIngreso);
            }

            if (hora > reserva.OficinaHoraCierre)
            {
                throw new ReglaNegocioException($"La salida no puede ser posterior al cierre de la oficina ({reserva.OficinaHoraCierre:HH\\:mm}).");
            }

            await _usos.ActualizarAsync(uso.IdReservaUso, uso.HoraIngreso, salida, uso.IdEmpleado, conexion, transaccion, token).ConfigureAwait(false);
            uso.HoraSalida = salida;

            var facturables = MinutosFacturables(reserva, uso);
            var importe = Importe(reserva.CantidadAsientos, reserva.TarifaHoraAplicada, facturables);
            await _reservas.ActualizarAsync(idReserva, Estados.Finalizada, reserva.EstadoPago, importe, conexion, transaccion, token).ConfigureAwait(false);
            reserva.Estado = Estados.Finalizada;
            reserva.ImporteExigible = importe;
            var motivo = Motivo(reserva, uso);

            await _auditoria.RegistrarAsync(
                "RegistroSalida",
                "ReservaUso",
                uso.IdReservaUso,
                Detalles.Alta(
                [
                    new("IdReserva", idReserva),
                    new("HoraSalida", salida.ToString("yyyy-MM-dd HH:mm")),
                    new("MinutosFacturables", facturables),
                    new("ImporteExigible", importe),
                    new("Estado", Estados.Finalizada)
                ]),
                idUsuario,
                conexion,
                transaccion,
                token).ConfigureAwait(false);

            return new ResultadoSalida(idReserva, importe, facturables, motivo);
        }, ct).ConfigureAwait(false);
    }

    public async Task<ResultadoPago> RegistrarPagoAsync(
        int idUsuario,
        int idReserva,
        decimal importe,
        string? referencia = null,
        CancellationToken ct = default)
    {
        return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
        {
            var ahora = await _reloj.AhoraServidorAsync(conexion, transaccion, token).ConfigureAwait(false);
            var empleado = await ResolverEmpleadoAsync(conexion, transaccion, idUsuario, token).ConfigureAwait(false);
            var reserva = await ObtenerReservaConAlcanceAsync(conexion, transaccion, empleado.IdEmpleado, idReserva, token).ConfigureAwait(false);

            var existente = await _pagos.ObtenerPorReservaAsync(idReserva, conexion, transaccion, token).ConfigureAwait(false);
            if (reserva.EstadoPago == Estados.Pagado || existente is not null)
            {
                throw new ReglaNegocioException(MensajePagoYaRegistrado);
            }

            if (reserva.ImporteExigible is null || (reserva.Estado != Estados.Finalizada && reserva.Estado != Estados.NoPresentada))
            {
                throw new ReglaNegocioException(MensajeSinImporteExigible);
            }

            if (importe != reserva.ImporteExigible.Value)
            {
                var exigible = reserva.ImporteExigible.Value.ToString("0.00", CultureInfo.InvariantCulture);
                throw new ReglaNegocioException($"El importe informado no coincide con el importe exigible ({exigible}). No se admiten pagos parciales.");
            }

            var pago = new Pago
            {
                IdReserva = idReserva,
                Importe = importe,
                FechaPago = ahora,
                IdEmpleadoConfirma = empleado.IdEmpleado,
                Referencia = referencia
            };

            int idPago;
            try
            {
                idPago = await _pagos.AgregarAsync(pago, conexion, transaccion, token).ConfigureAwait(false);
            }
            catch (ConflictoUnicidadException ex)
            {
                throw new ReglaNegocioException(MensajePagoYaRegistrado, ex);
            }

            await _reservas.ActualizarAsync(idReserva, reserva.Estado, Estados.Pagado, reserva.ImporteExigible, conexion, transaccion, token).ConfigureAwait(false);

            var deudaPagada = false;
            var deuda = await _deudas.ObtenerPorReservaAsync(idReserva, conexion, transaccion, token).ConfigureAwait(false);
            if (deuda is not null && deuda.Estado == Estados.Pendiente)
            {
                await _deudas.ActualizarEstadoAsync(deuda.IdDeuda, Estados.Pagada, conexion, transaccion, token).ConfigureAwait(false);
                deudaPagada = true;
            }

            await _auditoria.RegistrarAsync(
                "RegistroPago",
                "Pago",
                idPago,
                Detalles.Alta(
                [
                    new("IdReserva", idReserva),
                    new("Importe", importe),
                    new("IdEmpleadoConfirma", empleado.IdEmpleado),
                    new("Referencia", referencia ?? string.Empty)
                ]),
                idUsuario,
                conexion,
                transaccion,
                token).ConfigureAwait(false);

            return new ResultadoPago(idReserva, importe, deudaPagada);
        }, ct).ConfigureAwait(false);
    }

    private async Task<int> BarrerInasistenciasAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        DateTime ahora,
        CancellationToken ct)
    {
        var marcadas = 0;
        var pendientes = await _reservas.ListarConfirmadasSinIngresoAsync(conexion, transaccion, ct).ConfigureAwait(false);

        foreach (var reserva in pendientes)
        {
            if (reserva.Fecha.ToDateTime(reserva.HoraFin) >= ahora)
            {
                continue;
            }

            var facturables = Minutos(reserva.HoraInicio, reserva.HoraFin);
            var importe = Importe(reserva.CantidadAsientos, reserva.TarifaHoraAplicada, facturables);
            await _reservas.ActualizarAsync(reserva.IdReserva, Estados.NoPresentada, Estados.DeudaPendiente, importe, conexion, transaccion, ct).ConfigureAwait(false);

            var deuda = await _deudas.ObtenerPorReservaAsync(reserva.IdReserva, conexion, transaccion, ct).ConfigureAwait(false);
            if (deuda is null)
            {
                await _deudas.AgregarAsync(reserva.IdReserva, importe, Estados.Pendiente, conexion, transaccion, ct).ConfigureAwait(false);
            }

            await _auditoria.RegistrarAsync(
                "MarcadoInasistencia",
                "Reserva",
                reserva.IdReserva,
                Detalles.Alta(
                [
                    new("IdReserva", reserva.IdReserva),
                    new("ImporteExigible", importe),
                    new("Estado", Estados.NoPresentada),
                    new("EstadoPago", Estados.DeudaPendiente)
                ]),
                null,
                conexion,
                transaccion,
                ct).ConfigureAwait(false);

            marcadas++;
        }

        return marcadas;
    }

    private async Task<EmpleadoDetalle> ResolverEmpleadoAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        int idUsuario,
        CancellationToken ct)
    {
        var empleado = await _empleados.ObtenerDetallePorUsuarioAsync(idUsuario, conexion, transaccion, ct).ConfigureAwait(false);
        if (empleado is null)
        {
            throw new ReglaNegocioException(MensajeUsuarioSinEmpleado);
        }

        if (empleado.Estado != Estados.Activo)
        {
            throw new ReglaNegocioException(MensajeEmpleadoInactivo);
        }

        return empleado;
    }

    private async Task<HashSet<int>> OficinasAsignadasAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        int idEmpleado,
        CancellationToken ct)
    {
        var asignaciones = await _asignaciones.ListarPorEmpleadoAsync(idEmpleado, conexion, transaccion, ct).ConfigureAwait(false);
        return asignaciones.Select(asignacion => asignacion.IdOficina).ToHashSet();
    }

    private async Task<ReservaDetalle> ObtenerReservaConAlcanceAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        int idEmpleado,
        int idReserva,
        CancellationToken ct)
    {
        var reserva = await _reservas.ObtenerAsync(idReserva, conexion, transaccion, ct).ConfigureAwait(false);
        if (reserva is null)
        {
            throw new ReglaNegocioException(MensajeReservaNoExiste);
        }

        var oficinas = await OficinasAsignadasAsync(conexion, transaccion, idEmpleado, ct).ConfigureAwait(false);
        if (!oficinas.Contains(reserva.IdOficina))
        {
            throw new ReglaNegocioException(MensajeFueraDeAlcance);
        }

        return reserva;
    }

    private static void ValidarVentanaCorreccion(ReservaDetalle reserva, DateTime ahora)
    {
        var fin = reserva.Fecha.ToDateTime(reserva.HoraFin);
        if (ahora > fin.AddHours(HorasCorreccion))
        {
            throw new ReglaNegocioException(MensajeCorreccionVencida);
        }
    }

    private static int MinutosFacturables(ReservaDetalle reserva, ReservaUso uso)
    {
        var reservados = Minutos(reserva.HoraInicio, reserva.HoraFin);
        var cierre = reserva.Fecha.ToDateTime(reserva.OficinaHoraCierre);
        var finEfectivo = uso.HoraSalida < cierre ? uso.HoraSalida!.Value : cierre;
        var real = MinutosTranscurridos(uso.HoraIngreso!.Value, finEfectivo);
        return Math.Max(reservados, real);
    }

    private static decimal Importe(int asientos, decimal tarifa, int minutos)
    {
        return Math.Round(asientos * tarifa * minutos / 60m, 2, MidpointRounding.AwayFromZero);
    }

    private static string Motivo(ReservaDetalle reserva, ReservaUso? uso)
    {
        if (reserva.Estado == Estados.NoPresentada)
        {
            return "No presentada: se factura el periodo reservado";
        }

        if (reserva.Estado == Estados.EnCurso)
        {
            return "En curso: falta registrar la salida";
        }

        if (reserva.Estado == Estados.Confirmada)
        {
            return "Sin uso registrado";
        }

        if (reserva.Estado == Estados.Cancelada)
        {
            return "Cancelada";
        }

        if (uso is null || uso.HoraIngreso is null || uso.HoraSalida is null)
        {
            return "Sin uso registrado";
        }

        var reservados = Minutos(reserva.HoraInicio, reserva.HoraFin);
        var cierre = reserva.Fecha.ToDateTime(reserva.OficinaHoraCierre);
        var finEfectivo = uso.HoraSalida.Value < cierre ? uso.HoraSalida.Value : cierre;
        var reales = MinutosTranscurridos(uso.HoraIngreso.Value, finEfectivo);
        if (reales > reservados)
        {
            return $"Uso excedido: {reales - reservados} min adicionales facturados";
        }

        var motivos = new List<string>();
        if (uso.HoraIngreso.Value.TimeOfDay > reserva.HoraInicio.ToTimeSpan())
        {
            motivos.Add("ingreso tardio no reduce");
        }

        if (uso.HoraSalida.Value.TimeOfDay < reserva.HoraFin.ToTimeSpan())
        {
            motivos.Add("salida anticipada no reduce");
        }

        return motivos.Count > 0 ? string.Join("; ", motivos) : "Sin diferencia";
    }

    private async Task<FilaAgenda> CrearFilaAgendaAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        ReservaDetalle reserva,
        CancellationToken ct)
    {
        var uso = await _usos.ObtenerPorReservaAsync(reserva.IdReserva, conexion, transaccion, ct).ConfigureAwait(false);
        return new FilaAgenda
        {
            IdReserva = reserva.IdReserva,
            Oficina = reserva.Oficina,
            Ubicacion = $"{reserva.Edificio} / {reserva.Piso}",
            Horario = $"{reserva.HoraInicio:HH\\:mm}-{reserva.HoraFin:HH\\:mm}",
            Asientos = reserva.CantidadAsientos,
            Estado = reserva.Estado,
            EstadoPago = reserva.EstadoPago,
            Importe = reserva.ImporteExigible,
            Ingreso = uso is not null && uso.HoraIngreso is not null ? uso.HoraIngreso.Value.ToString("HH:mm") : null,
            Salida = uso is not null && uso.HoraSalida is not null ? uso.HoraSalida.Value.ToString("HH:mm") : null,
            Motivo = Motivo(reserva, uso),
            PendienteSalida = reserva.Estado == Estados.EnCurso
        };
    }

    private static int Minutos(TimeOnly horaInicio, TimeOnly horaFin)
    {
        return (horaFin.Hour * 60 + horaFin.Minute) - (horaInicio.Hour * 60 + horaInicio.Minute);
    }

    private static int MinutosTranscurridos(DateTime desde, DateTime hasta)
    {
        return (int)Math.Floor((hasta - desde).TotalSeconds / 60);
    }
}
