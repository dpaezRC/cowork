using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Tests.Fakes;

public sealed class PagoRepositorioFake : IPagoRepositorio
{
    private readonly Dictionary<int, Pago> _porReserva = new();
    private int _siguienteId = 1;

    public IReadOnlyDictionary<int, Pago> Pagos => _porReserva;

    public int VecesAgregado { get; private set; }

    public bool LanzarConflictoUnicidadEnAgregar { get; set; }

    public Pago Sembrar(int idReserva, decimal importe, string? referencia = null)
    {
        var pago = new Pago
        {
            IdPago = _siguienteId++,
            IdReserva = idReserva,
            Importe = importe,
            FechaPago = new DateTime(2030, 6, 16, 8, 0, 0),
            IdEmpleadoConfirma = 1,
            Referencia = referencia,
            FechaRegistro = new DateTime(2030, 6, 16, 8, 0, 0)
        };
        _porReserva[idReserva] = pago;
        return pago;
    }

    public Task<Pago?> ObtenerPorReservaAsync(
        int idReserva,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_porReserva.TryGetValue(idReserva, out var pago) ? pago : null);
    }

    public Task<int> AgregarAsync(
        Pago pago,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        if (LanzarConflictoUnicidadEnAgregar)
        {
            throw new ConflictoUnicidadException();
        }

        VecesAgregado++;
        pago.IdPago = _siguienteId++;
        _porReserva[pago.IdReserva] = pago;
        return Task.FromResult(pago.IdPago);
    }
}
