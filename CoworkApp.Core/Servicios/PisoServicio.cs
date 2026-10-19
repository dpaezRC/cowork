using CoworkApp.Core.Datos;
using CoworkApp.Core.Dominio;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Validacion;

namespace CoworkApp.Core.Servicios;

public sealed class PisoServicio
{
    public const string MensajeDuplicado = "Ya existe un piso con ese nombre en el edificio seleccionado.";
    public const string MensajeNombreObligatorio = "El nombre del piso es obligatorio.";
    public const string MensajeEdificioNoExiste = "El edificio seleccionado no existe.";
    public const string MensajeNoExiste = "El piso no existe.";

    private readonly IPisoRepositorio _pisos;
    private readonly IEdificioRepositorio _edificios;
    private readonly IAuditoriaRepositorio _auditoria;
    private readonly ITransaccionDb _transaccion;

    public PisoServicio(
        IPisoRepositorio pisos,
        IEdificioRepositorio edificios,
        IAuditoriaRepositorio auditoria,
        ITransaccionDb transaccion)
    {
        _pisos = pisos ?? throw new ArgumentNullException(nameof(pisos));
        _edificios = edificios ?? throw new ArgumentNullException(nameof(edificios));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _transaccion = transaccion ?? throw new ArgumentNullException(nameof(transaccion));
    }

    public Task<List<Piso>> ListarPorEdificioAsync(int idEdificio, CancellationToken ct = default)
    {
        return _pisos.ListarPorEdificioAsync(idEdificio, ct);
    }

    public Task<List<PisoDetalle>> ListarDetallePorEdificioAsync(int idEdificio, CancellationToken ct = default)
    {
        return _pisos.ListarDetallePorEdificioAsync(idEdificio, ct);
    }

    public Task<Piso?> ObtenerAsync(int idPiso, CancellationToken ct = default)
    {
        return _pisos.ObtenerAsync(idPiso, ct: ct);
    }

    public Task<Piso?> BuscarPorNombreAsync(int idEdificio, string nombre, CancellationToken ct = default)
    {
        return _pisos.BuscarPorNombreAsync(idEdificio, nombre, ct);
    }

    public async Task<Piso> CrearAsync(int idEdificio, string nombre, CancellationToken ct = default)
    {
        var nombreLimpio = Validador.Requerido(nombre, MensajeNombreObligatorio);

        try
        {
            return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
            {
                if (await _edificios.ObtenerAsync(idEdificio, conexion, transaccion, token).ConfigureAwait(false) is null)
                {
                    throw new ReglaNegocioException(MensajeEdificioNoExiste);
                }

                if (await _pisos.NombreExisteAsync(idEdificio, nombreLimpio, null, conexion, transaccion, token).ConfigureAwait(false))
                {
                    throw new ReglaNegocioException(MensajeDuplicado);
                }

                var idPiso = await _pisos.AgregarAsync(idEdificio, nombreLimpio, conexion, transaccion, token).ConfigureAwait(false);

                await _auditoria.RegistrarAsync(
                    "Alta",
                    "Piso",
                    idPiso,
                    Detalles.Alta(
                    [
                        new("IdEdificio", idEdificio),
                        new("Nombre", nombreLimpio),
                        new("Estado", Estados.Activo)
                    ]),
                    null,
                    conexion,
                    transaccion,
                    token).ConfigureAwait(false);

                return new Piso
                {
                    IdPiso = idPiso,
                    IdEdificio = idEdificio,
                    Nombre = nombreLimpio,
                    Estado = Estados.Activo
                };
            }, ct).ConfigureAwait(false);
        }
        catch (ConflictoUnicidadException ex)
        {
            throw new ReglaNegocioException(MensajeDuplicado, ex);
        }
    }

    public async Task<bool> ActualizarAsync(int idPiso, string nombre, string estado, CancellationToken ct = default)
    {
        var nombreLimpio = Validador.Requerido(nombre, MensajeNombreObligatorio);
        var estadoValidado = Validador.Estado(estado, Estados.Piso);

        try
        {
            return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
            {
                var piso = await _pisos.ObtenerAsync(idPiso, conexion, transaccion, token).ConfigureAwait(false);
                if (piso is null)
                {
                    throw new ReglaNegocioException(MensajeNoExiste);
                }

                if (await _pisos.NombreExisteAsync(piso.IdEdificio, nombreLimpio, idPiso, conexion, transaccion, token).ConfigureAwait(false))
                {
                    throw new ReglaNegocioException(MensajeDuplicado);
                }

                var cambios = new List<(string Campo, object? Anterior, object? Nuevo)>();
                if (piso.Nombre != nombreLimpio)
                {
                    cambios.Add(("Nombre", piso.Nombre, nombreLimpio));
                }

                if (piso.Estado != estadoValidado)
                {
                    cambios.Add(("Estado", piso.Estado, estadoValidado));
                }

                if (cambios.Count == 0)
                {
                    return false;
                }

                await _pisos.ActualizarAsync(idPiso, nombreLimpio, estadoValidado, conexion, transaccion, token).ConfigureAwait(false);

                await _auditoria.RegistrarAsync(
                    "Modificacion",
                    "Piso",
                    idPiso,
                    Detalles.Modificacion(cambios),
                    null,
                    conexion,
                    transaccion,
                    token).ConfigureAwait(false);

                return true;
            }, ct).ConfigureAwait(false);
        }
        catch (ConflictoUnicidadException ex)
        {
            throw new ReglaNegocioException(MensajeDuplicado, ex);
        }
    }

    public async Task<bool> DarDeBajaAsync(int idPiso, CancellationToken ct = default)
    {
        return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
        {
            var piso = await _pisos.ObtenerAsync(idPiso, conexion, transaccion, token).ConfigureAwait(false);
            if (piso is null)
            {
                throw new ReglaNegocioException(MensajeNoExiste);
            }

            if (piso.Estado == Estados.Inactivo)
            {
                return false;
            }

            var estadoAnterior = piso.Estado;

            await _pisos.ActualizarEstadoAsync(idPiso, Estados.Inactivo, conexion, transaccion, token).ConfigureAwait(false);

            await _auditoria.RegistrarAsync(
                "Baja",
                "Piso",
                idPiso,
                Detalles.Modificacion([("Estado", estadoAnterior, Estados.Inactivo)]),
                null,
                conexion,
                transaccion,
                token).ConfigureAwait(false);

            return true;
        }, ct).ConfigureAwait(false);
    }
}
