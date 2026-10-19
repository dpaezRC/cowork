using CoworkApp.Core.Modelos;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Repositorios;

public interface IReservaRepositorio
{
    Task<int> AgregarAsync(
        Reserva reserva,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<ReservaDetalle?> ObtenerAsync(
        int idReserva,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<List<ReservaDetalle>> ListarPorUsuarioAsync(
        int idUsuario,
        string? estado = null,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<List<ReservaDetalle>> ListarPorOficinasYFechaAsync(
        IReadOnlyCollection<int> idsOficinas,
        DateOnly fecha,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<List<ReservaDetalle>> ListarConfirmadasSinIngresoAsync(
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<List<ReservaActiva>> ListarActivasPorFechaAsync(
        DateOnly fecha,
        IReadOnlyCollection<int> idsOficinas,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<List<ReservaActiva>> ListarActivasSuperpuestasConBloqueoAsync(
        int idOficina,
        DateOnly fecha,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<List<FranjaCobertura>> ListarFranjasCoberturaAsync(
        IReadOnlyCollection<int> idsOficinas,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> ActualizarAsync(
        int idReserva,
        string estado,
        string estadoPago,
        decimal? importeExigible,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);

    Task<int> CancelarAsync(
        int idReserva,
        DateTime fechaCancelacion,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default);
}
