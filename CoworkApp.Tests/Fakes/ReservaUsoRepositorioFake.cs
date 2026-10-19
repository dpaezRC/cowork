using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Tests.Fakes;

public sealed class ReservaUsoRepositorioFake : IReservaUsoRepositorio
{
    private readonly Dictionary<int, ReservaUso> _porReserva = new();
    private int _siguienteId = 1;

    public IReadOnlyDictionary<int, ReservaUso> Usos => _porReserva;

    public int VecesAgregado { get; private set; }

    public int VecesActualizado { get; private set; }

    public bool LanzarConflictoUnicidadEnAgregar { get; set; }

    public ReservaUso Sembrar(
        int idReserva,
        DateTime? horaIngreso = null,
        DateTime? horaSalida = null,
        int idEmpleado = 1)
    {
        var uso = new ReservaUso
        {
            IdReservaUso = _siguienteId++,
            IdReserva = idReserva,
            HoraIngreso = horaIngreso,
            HoraSalida = horaSalida,
            IdEmpleado = idEmpleado,
            FechaRegistro = new DateTime(2024, 1, 1)
        };
        _porReserva[idReserva] = uso;
        return uso;
    }

    public Task<ReservaUso?> ObtenerPorReservaAsync(
        int idReserva,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_porReserva.TryGetValue(idReserva, out var uso) ? uso : null);
    }

    public Task<int> AgregarAsync(
        ReservaUso uso,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        if (LanzarConflictoUnicidadEnAgregar)
        {
            throw new ConflictoUnicidadException();
        }

        VecesAgregado++;
        uso.IdReservaUso = _siguienteId++;
        _porReserva[uso.IdReserva] = uso;
        return Task.FromResult(uso.IdReservaUso);
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
        var uso = _porReserva.Values.First(actual => actual.IdReservaUso == idReservaUso);
        uso.HoraIngreso = horaIngreso;
        uso.HoraSalida = horaSalida;
        uso.IdEmpleado = idEmpleado;
        VecesActualizado++;
        return Task.FromResult(1);
    }
}
