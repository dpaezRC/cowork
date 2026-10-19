using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Tests.Fakes;

public sealed class PisoRepositorioFake : IPisoRepositorio
{
    private readonly List<Piso> _pisos = new();
    private int _siguienteId = 1;

    public IReadOnlyList<Piso> Pisos => _pisos;

    public int VecesAgregado { get; private set; }

    public Piso Sembrar(int id, int idEdificio, string nombre, string estado = "Activo")
    {
        var piso = new Piso
        {
            IdPiso = id,
            IdEdificio = idEdificio,
            Nombre = nombre,
            Estado = estado
        };
        _pisos.Add(piso);
        _siguienteId = Math.Max(_siguienteId, id + 1);
        return piso;
    }

    public Task<List<Piso>> ListarPorEdificioAsync(int idEdificio, CancellationToken ct = default)
    {
        return Task.FromResult(_pisos
            .Where(piso => piso.IdEdificio == idEdificio)
            .OrderBy(piso => piso.Nombre)
            .ToList());
    }

    public Task<List<PisoDetalle>> ListarDetallePorEdificioAsync(int idEdificio, CancellationToken ct = default)
    {
        var detalle = _pisos
            .Where(piso => piso.IdEdificio == idEdificio)
            .OrderBy(piso => piso.Nombre)
            .Select(piso => new PisoDetalle
            {
                IdPiso = piso.IdPiso,
                Edificio = string.Empty,
                Nombre = piso.Nombre,
                Estado = piso.Estado
            })
            .ToList();
        return Task.FromResult(detalle);
    }

    public Task<Piso?> ObtenerAsync(
        int idPiso,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_pisos.FirstOrDefault(piso => piso.IdPiso == idPiso));
    }

    public Task<Piso?> BuscarPorNombreAsync(int idEdificio, string nombre, CancellationToken ct = default)
    {
        var buscado = normalizar(nombre);
        return Task.FromResult(_pisos.FirstOrDefault(
            piso => piso.IdEdificio == idEdificio && normalizar(piso.Nombre) == buscado));
    }

    public Task<bool> NombreExisteAsync(
        int idEdificio,
        string nombre,
        int? excluirId,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var buscado = normalizar(nombre);
        var existe = _pisos.Any(piso =>
            piso.IdEdificio == idEdificio &&
            piso.IdPiso != excluirId &&
            normalizar(piso.Nombre) == buscado);
        return Task.FromResult(existe);
    }

    public Task<int> AgregarAsync(
        int idEdificio,
        string nombre,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        VecesAgregado++;
        var id = _siguienteId++;
        _pisos.Add(new Piso
        {
            IdPiso = id,
            IdEdificio = idEdificio,
            Nombre = nombre,
            Estado = "Activo"
        });
        return Task.FromResult(id);
    }

    public Task<int> ActualizarAsync(
        int idPiso,
        string nombre,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var piso = _pisos.First(actual => actual.IdPiso == idPiso);
        piso.Nombre = nombre;
        piso.Estado = estado;
        return Task.FromResult(1);
    }

    public Task<int> ActualizarEstadoAsync(
        int idPiso,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var piso = _pisos.First(actual => actual.IdPiso == idPiso);
        piso.Estado = estado;
        return Task.FromResult(1);
    }

    private static string normalizar(string valor) => valor.Trim().ToLowerInvariant();
}
