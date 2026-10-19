using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Tests.Fakes;

public sealed class EdificioRepositorioFake : IEdificioRepositorio
{
    private readonly List<Edificio> _edificios = new();
    private int _siguienteId = 1;

    public IReadOnlyList<Edificio> Edificios => _edificios;

    public int VecesAgregado { get; private set; }

    public Edificio Sembrar(int id, string nombre, string direccion, string estado = "Activo")
    {
        var edificio = new Edificio
        {
            IdEdificio = id,
            Nombre = nombre,
            Direccion = direccion,
            Estado = estado,
            FechaAlta = new DateTime(2024, 1, 1)
        };
        _edificios.Add(edificio);
        _siguienteId = Math.Max(_siguienteId, id + 1);
        return edificio;
    }

    public Task<List<Edificio>> ListarAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_edificios.OrderBy(edificio => edificio.Nombre).ToList());
    }

    public Task<Edificio?> ObtenerAsync(
        int idEdificio,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_edificios.FirstOrDefault(edificio => edificio.IdEdificio == idEdificio));
    }

    public Task<Edificio?> BuscarPorNombreAsync(string nombre, CancellationToken ct = default)
    {
        var buscado = normalizar(nombre);
        return Task.FromResult(_edificios.FirstOrDefault(edificio => normalizar(edificio.Nombre) == buscado));
    }

    public Task<bool> NombreExisteAsync(
        string nombre,
        int? excluirId,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var buscado = normalizar(nombre);
        var existe = _edificios.Any(edificio =>
            edificio.IdEdificio != excluirId && normalizar(edificio.Nombre) == buscado);
        return Task.FromResult(existe);
    }

    public Task<int> AgregarAsync(
        string nombre,
        string direccion,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        VecesAgregado++;
        var id = _siguienteId++;
        _edificios.Add(new Edificio
        {
            IdEdificio = id,
            Nombre = nombre,
            Direccion = direccion,
            Estado = "Activo",
            FechaAlta = new DateTime(2024, 1, 1)
        });
        return Task.FromResult(id);
    }

    public Task<int> ActualizarAsync(
        int idEdificio,
        string nombre,
        string direccion,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var edificio = _edificios.First(actual => actual.IdEdificio == idEdificio);
        edificio.Nombre = nombre;
        edificio.Direccion = direccion;
        edificio.Estado = estado;
        return Task.FromResult(1);
    }

    public Task<int> ActualizarEstadoAsync(
        int idEdificio,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var edificio = _edificios.First(actual => actual.IdEdificio == idEdificio);
        edificio.Estado = estado;
        return Task.FromResult(1);
    }

    private static string normalizar(string valor) => valor.Trim().ToLowerInvariant();
}
