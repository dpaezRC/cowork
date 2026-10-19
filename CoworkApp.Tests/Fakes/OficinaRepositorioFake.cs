using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Tests.Fakes;

public sealed class OficinaRepositorioFake : IOficinaRepositorio
{
    private readonly List<Oficina> _oficinas = new();
    private int _siguienteId = 1;

    public IReadOnlyList<Oficina> Oficinas => _oficinas;

    public int VecesAgregado { get; private set; }

    public bool LanzarConflictoUnicidadEnAgregar { get; set; }

    public Oficina Sembrar(
        int id,
        int idPiso,
        string nombre,
        int capacidad = 4,
        decimal tarifa = 1000.00m,
        string estado = "Habilitada")
    {
        var oficina = new Oficina
        {
            IdOficina = id,
            IdPiso = idPiso,
            Nombre = nombre,
            Capacidad = capacidad,
            TarifaHora = tarifa,
            HoraApertura = new TimeOnly(8, 0),
            HoraCierre = new TimeOnly(21, 0),
            Estado = estado,
            FechaAlta = new DateTime(2024, 1, 1)
        };
        _oficinas.Add(oficina);
        _siguienteId = Math.Max(_siguienteId, id + 1);
        return oficina;
    }

    public Task<List<Oficina>> ListarAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_oficinas.OrderBy(oficina => oficina.IdOficina).ToList());
    }

    public Task<List<OficinaDetalle>> ListarDetalleAsync(CancellationToken ct = default)
    {
        var detalle = _oficinas
            .OrderBy(oficina => oficina.IdOficina)
            .Select(oficina => new OficinaDetalle
            {
                IdOficina = oficina.IdOficina,
                Nombre = oficina.Nombre,
                Edificio = string.Empty,
                Piso = string.Empty,
                Capacidad = oficina.Capacidad,
                TarifaHora = oficina.TarifaHora,
                HoraApertura = oficina.HoraApertura,
                HoraCierre = oficina.HoraCierre,
                Estado = oficina.Estado
            })
            .ToList();
        return Task.FromResult(detalle);
    }

    public Task<Oficina?> ObtenerAsync(
        int idOficina,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_oficinas.FirstOrDefault(oficina => oficina.IdOficina == idOficina));
    }

    public Task<List<Oficina>> ListarPorEdificioAsync(int idEdificio, CancellationToken ct = default)
    {
        return Task.FromResult(_oficinas
            .Where(oficina => oficina.IdPiso == idEdificio)
            .OrderBy(oficina => oficina.Nombre)
            .ToList());
    }

    public Task<Oficina?> BuscarPorNombreAsync(int idPiso, string nombre, CancellationToken ct = default)
    {
        var buscado = normalizar(nombre);
        return Task.FromResult(_oficinas.FirstOrDefault(
            oficina => oficina.IdPiso == idPiso && normalizar(oficina.Nombre) == buscado));
    }

    public Task<bool> NombreExisteAsync(
        int idPiso,
        string nombre,
        int? excluirId,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var buscado = normalizar(nombre);
        var existe = _oficinas.Any(oficina =>
            oficina.IdPiso == idPiso &&
            oficina.IdOficina != excluirId &&
            normalizar(oficina.Nombre) == buscado);
        return Task.FromResult(existe);
    }

    public Task<int> AgregarAsync(
        int idPiso,
        string nombre,
        int capacidad,
        decimal tarifa,
        TimeOnly horaApertura,
        TimeOnly horaCierre,
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
        _oficinas.Add(new Oficina
        {
            IdOficina = id,
            IdPiso = idPiso,
            Nombre = nombre,
            Capacidad = capacidad,
            TarifaHora = tarifa,
            HoraApertura = horaApertura,
            HoraCierre = horaCierre,
            Estado = "Habilitada",
            FechaAlta = new DateTime(2024, 1, 1)
        });
        return Task.FromResult(id);
    }

    public Task<int> ActualizarAsync(
        int idOficina,
        string nombre,
        int capacidad,
        decimal tarifa,
        TimeOnly horaApertura,
        TimeOnly horaCierre,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var oficina = _oficinas.First(actual => actual.IdOficina == idOficina);
        oficina.Nombre = nombre;
        oficina.Capacidad = capacidad;
        oficina.TarifaHora = tarifa;
        oficina.HoraApertura = horaApertura;
        oficina.HoraCierre = horaCierre;
        oficina.Estado = estado;
        return Task.FromResult(1);
    }

    public Task<int> ActualizarEstadoAsync(
        int idOficina,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var oficina = _oficinas.First(actual => actual.IdOficina == idOficina);
        oficina.Estado = estado;
        return Task.FromResult(1);
    }

    private static string normalizar(string valor) => valor.Trim().ToLowerInvariant();
}
