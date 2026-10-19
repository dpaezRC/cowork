using CoworkApp.Core.Datos;
using CoworkApp.Core.Dominio;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Tests.Fakes;

public sealed class ReservaRepositorioFake : IReservaRepositorio
{
    private readonly List<ReservaDetalle> _reservas = new();
    private int _siguienteId = 1;

    public IReadOnlyList<ReservaDetalle> Reservas => _reservas;

    public HashSet<int> ReservasConIngreso { get; } = new();

    public List<FranjaCobertura> Franjas { get; } = new();

    public int VecesAgregado { get; private set; }

    public int VecesActualizado { get; private set; }

    public int VecesCancelado { get; private set; }

    public bool LanzarConflictoUnicidadEnAgregar { get; set; }

    public ReservaDetalle Sembrar(
        int id,
        int idUsuario,
        int idOficina,
        DateOnly fecha,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        int asientos = 4,
        decimal tarifa = 1000.00m,
        string estado = Estados.Confirmada,
        string estadoPago = Estados.Pendiente,
        decimal? importe = null,
        string oficina = "Oficina 101",
        string edificio = "Edificio Central",
        string piso = "Piso 1",
        int capacidad = 10,
        TimeOnly? apertura = null,
        TimeOnly? cierre = null)
    {
        var reserva = new ReservaDetalle
        {
            IdReserva = id,
            IdUsuario = idUsuario,
            IdOficina = idOficina,
            Fecha = fecha,
            HoraInicio = horaInicio,
            HoraFin = horaFin,
            CantidadAsientos = asientos,
            TarifaHoraAplicada = tarifa,
            ImporteExigible = importe,
            Estado = estado,
            EstadoPago = estadoPago,
            Oficina = oficina,
            Edificio = edificio,
            Piso = piso,
            OficinaCapacidad = capacidad,
            OficinaHoraApertura = apertura ?? new TimeOnly(8, 0),
            OficinaHoraCierre = cierre ?? new TimeOnly(21, 0)
        };
        _reservas.Add(reserva);
        _siguienteId = Math.Max(_siguienteId, id + 1);
        return reserva;
    }

    public void SembrarFranja(int idOficina, TimeOnly horaInicio, TimeOnly horaFin)
    {
        Franjas.Add(new FranjaCobertura { IdOficina = idOficina, HoraInicio = horaInicio, HoraFin = horaFin });
    }

    public Task<int> AgregarAsync(
        Reserva reserva,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        if (LanzarConflictoUnicidadEnAgregar)
        {
            throw new ConflictoUnicidadException();
        }

        VecesAgregado++;
        var id = _siguienteId++;
        _reservas.Add(new ReservaDetalle
        {
            IdReserva = id,
            IdUsuario = reserva.IdUsuario,
            IdOficina = reserva.IdOficina,
            Fecha = reserva.Fecha,
            HoraInicio = reserva.HoraInicio,
            HoraFin = reserva.HoraFin,
            CantidadAsientos = reserva.CantidadAsientos,
            TarifaHoraAplicada = reserva.TarifaHoraAplicada,
            ImporteExigible = reserva.ImporteExigible,
            Estado = reserva.Estado,
            EstadoPago = reserva.EstadoPago
        });
        return Task.FromResult(id);
    }

    public Task<ReservaDetalle?> ObtenerAsync(
        int idReserva,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_reservas.FirstOrDefault(reserva => reserva.IdReserva == idReserva));
    }

    public Task<List<ReservaDetalle>> ListarPorUsuarioAsync(
        int idUsuario,
        string? estado = null,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_reservas
            .Where(reserva => reserva.IdUsuario == idUsuario && (estado is null || reserva.Estado == estado))
            .OrderBy(reserva => reserva.Fecha)
            .ThenBy(reserva => reserva.HoraInicio)
            .ThenBy(reserva => reserva.IdReserva)
            .ToList());
    }

    public Task<List<ReservaDetalle>> ListarPorOficinasYFechaAsync(
        IReadOnlyCollection<int> idsOficinas,
        DateOnly fecha,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_reservas
            .Where(reserva => idsOficinas.Contains(reserva.IdOficina) && reserva.Fecha == fecha)
            .OrderBy(reserva => reserva.HoraInicio)
            .ThenBy(reserva => reserva.IdReserva)
            .ToList());
    }

    public Task<List<ReservaDetalle>> ListarConfirmadasSinIngresoAsync(
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_reservas
            .Where(reserva => reserva.Estado == Estados.Confirmada && !ReservasConIngreso.Contains(reserva.IdReserva))
            .OrderBy(reserva => reserva.Fecha)
            .ThenBy(reserva => reserva.HoraInicio)
            .ThenBy(reserva => reserva.IdReserva)
            .ToList());
    }

    public Task<List<ReservaActiva>> ListarActivasPorFechaAsync(
        DateOnly fecha,
        IReadOnlyCollection<int> idsOficinas,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_reservas
            .Where(reserva => reserva.Fecha == fecha
                && idsOficinas.Contains(reserva.IdOficina)
                && (reserva.Estado == Estados.Confirmada || reserva.Estado == Estados.EnCurso))
            .Select(Activa)
            .ToList());
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
        return Task.FromResult(_reservas
            .Where(reserva => reserva.IdOficina == idOficina
                && reserva.Fecha == fecha
                && (reserva.Estado == Estados.Confirmada || reserva.Estado == Estados.EnCurso)
                && reserva.HoraInicio < horaFin
                && reserva.HoraFin > horaInicio)
            .OrderBy(reserva => reserva.HoraInicio)
            .Select(Activa)
            .ToList());
    }

    public Task<List<FranjaCobertura>> ListarFranjasCoberturaAsync(
        IReadOnlyCollection<int> idsOficinas,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(Franjas.Where(franja => idsOficinas.Contains(franja.IdOficina)).ToList());
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
        var reserva = _reservas.First(actual => actual.IdReserva == idReserva);
        reserva.Estado = estado;
        reserva.EstadoPago = estadoPago;
        reserva.ImporteExigible = importeExigible;
        VecesActualizado++;
        return Task.FromResult(1);
    }

    public Task<int> CancelarAsync(
        int idReserva,
        DateTime fechaCancelacion,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var reserva = _reservas.First(actual => actual.IdReserva == idReserva);
        reserva.Estado = Estados.Cancelada;
        VecesCancelado++;
        return Task.FromResult(1);
    }

    private static ReservaActiva Activa(ReservaDetalle reserva)
    {
        return new ReservaActiva
        {
            IdOficina = reserva.IdOficina,
            HoraInicio = reserva.HoraInicio,
            HoraFin = reserva.HoraFin,
            CantidadAsientos = reserva.CantidadAsientos
        };
    }
}
