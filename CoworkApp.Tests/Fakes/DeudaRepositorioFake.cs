using CoworkApp.Core.Datos;
using CoworkApp.Core.Dominio;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Tests.Fakes;

public sealed class DeudaRepositorioFake : IDeudaRepositorio
{
    private readonly Dictionary<int, Deuda> _porReserva = new();
    private int _siguienteId = 1;

    public IReadOnlyDictionary<int, Deuda> Deudas => _porReserva;

    public int VecesAgregado { get; private set; }

    public int VecesActualizado { get; private set; }

    public Deuda Sembrar(int idReserva, decimal importeExigible, string estado = Estados.Pendiente)
    {
        var deuda = new Deuda
        {
            IdDeuda = _siguienteId++,
            IdReserva = idReserva,
            ImporteExigible = importeExigible,
            FechaGeneracion = new DateTime(2030, 6, 16, 8, 0, 0),
            Estado = estado
        };
        _porReserva[idReserva] = deuda;
        return deuda;
    }

    public Task<Deuda?> ObtenerPorReservaAsync(
        int idReserva,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_porReserva.TryGetValue(idReserva, out var deuda) ? deuda : null);
    }

    public Task<int> AgregarAsync(
        int idReserva,
        decimal importeExigible,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        VecesAgregado++;
        var deuda = new Deuda
        {
            IdDeuda = _siguienteId++,
            IdReserva = idReserva,
            ImporteExigible = importeExigible,
            FechaGeneracion = new DateTime(2030, 6, 16, 8, 0, 0),
            Estado = estado
        };
        _porReserva[idReserva] = deuda;
        return Task.FromResult(deuda.IdDeuda);
    }

    public Task<int> ActualizarEstadoAsync(
        int idDeuda,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var deuda = _porReserva.Values.First(actual => actual.IdDeuda == idDeuda);
        deuda.Estado = estado;
        VecesActualizado++;
        return Task.FromResult(1);
    }
}
