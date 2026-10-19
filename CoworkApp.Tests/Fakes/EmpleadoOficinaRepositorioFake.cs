using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Tests.Fakes;

public sealed class EmpleadoOficinaRepositorioFake : IEmpleadoOficinaRepositorio
{
    private readonly List<AsignacionDetalle> _asignaciones = new();
    private int _siguienteId = 1;

    public IReadOnlyList<AsignacionDetalle> Asignaciones => _asignaciones;

    public int VecesAgregado { get; private set; }

    public int VecesEliminado { get; private set; }

    public bool LanzarConflictoUnicidadEnAgregar { get; set; }

    public AsignacionDetalle Sembrar(
        int id,
        int idEmpleado,
        int idOficina,
        string oficina = "Oficina 101",
        string edificio = "Edificio Central",
        TimeOnly? horaInicio = null,
        TimeOnly? horaFin = null)
    {
        var asignacion = new AsignacionDetalle
        {
            IdEmpleadoOficina = id,
            IdEmpleado = idEmpleado,
            IdOficina = idOficina,
            Oficina = oficina,
            Edificio = edificio,
            HoraInicio = horaInicio ?? new TimeOnly(9, 0),
            HoraFin = horaFin ?? new TimeOnly(17, 0)
        };
        _asignaciones.Add(asignacion);
        _siguienteId = Math.Max(_siguienteId, id + 1);
        return asignacion;
    }

    public Task<List<AsignacionDetalle>> ListarPorEmpleadoAsync(
        int idEmpleado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_asignaciones
            .Where(asignacion => asignacion.IdEmpleado == idEmpleado)
            .OrderBy(asignacion => asignacion.HoraInicio)
            .ThenBy(asignacion => asignacion.IdEmpleadoOficina)
            .ToList());
    }

    public Task<AsignacionDetalle?> ObtenerDetalleAsync(
        int idAsignacion,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_asignaciones.FirstOrDefault(asignacion => asignacion.IdEmpleadoOficina == idAsignacion));
    }

    public Task<bool> ExisteFranjaAsync(
        int idEmpleado,
        int idOficina,
        TimeOnly horaInicio,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var existe = _asignaciones.Any(asignacion =>
            asignacion.IdEmpleado == idEmpleado &&
            asignacion.IdOficina == idOficina &&
            asignacion.HoraInicio == horaInicio);
        return Task.FromResult(existe);
    }

    public Task<bool> TieneAsignacionesAsync(
        int idEmpleado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_asignaciones.Any(asignacion => asignacion.IdEmpleado == idEmpleado));
    }

    public Task<int> AgregarAsync(
        int idEmpleado,
        int idOficina,
        TimeOnly horaInicio,
        TimeOnly horaFin,
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
        _asignaciones.Add(new AsignacionDetalle
        {
            IdEmpleadoOficina = id,
            IdEmpleado = idEmpleado,
            IdOficina = idOficina,
            HoraInicio = horaInicio,
            HoraFin = horaFin
        });
        return Task.FromResult(id);
    }

    public Task<int> EliminarAsync(
        int idAsignacion,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var asignacion = _asignaciones.First(actual => actual.IdEmpleadoOficina == idAsignacion);
        _asignaciones.Remove(asignacion);
        VecesEliminado++;
        return Task.FromResult(1);
    }
}
