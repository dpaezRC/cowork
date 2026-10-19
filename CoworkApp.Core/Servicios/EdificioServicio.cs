using CoworkApp.Core.Datos;
using CoworkApp.Core.Dominio;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Validacion;

namespace CoworkApp.Core.Servicios;

public sealed class EdificioServicio
{
    public const string MensajeDuplicado = "Ya existe un edificio con ese nombre.";
    public const string MensajeNombreObligatorio = "El nombre del edificio es obligatorio.";
    public const string MensajeDireccionObligatoria = "La direccion del edificio es obligatoria.";
    public const string MensajeNoExiste = "El edificio no existe.";

    private readonly IEdificioRepositorio _edificios;
    private readonly IAuditoriaRepositorio _auditoria;
    private readonly ITransaccionDb _transaccion;

    public EdificioServicio(
        IEdificioRepositorio edificios,
        IAuditoriaRepositorio auditoria,
        ITransaccionDb transaccion)
    {
        _edificios = edificios ?? throw new ArgumentNullException(nameof(edificios));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _transaccion = transaccion ?? throw new ArgumentNullException(nameof(transaccion));
    }

    public Task<List<Edificio>> ListarAsync(CancellationToken ct = default)
    {
        return _edificios.ListarAsync(ct);
    }

    public Task<Edificio?> ObtenerAsync(int idEdificio, CancellationToken ct = default)
    {
        return _edificios.ObtenerAsync(idEdificio, ct: ct);
    }

    public Task<Edificio?> BuscarPorNombreAsync(string nombre, CancellationToken ct = default)
    {
        return _edificios.BuscarPorNombreAsync(nombre, ct);
    }

    public async Task<Edificio> CrearAsync(string nombre, string direccion, CancellationToken ct = default)
    {
        var nombreLimpio = Validador.Requerido(nombre, MensajeNombreObligatorio);
        var direccionLimpia = Validador.Requerido(direccion, MensajeDireccionObligatoria);

        try
        {
            return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
            {
                if (await _edificios.NombreExisteAsync(nombreLimpio, null, conexion, transaccion, token).ConfigureAwait(false))
                {
                    throw new ReglaNegocioException(MensajeDuplicado);
                }

                var idEdificio = await _edificios.AgregarAsync(nombreLimpio, direccionLimpia, conexion, transaccion, token).ConfigureAwait(false);

                await _auditoria.RegistrarAsync(
                    "Alta",
                    "Edificio",
                    idEdificio,
                    Detalles.Alta(
                    [
                        new("Nombre", nombreLimpio),
                        new("Direccion", direccionLimpia),
                        new("Estado", Estados.Activo)
                    ]),
                    null,
                    conexion,
                    transaccion,
                    token).ConfigureAwait(false);

                return new Edificio
                {
                    IdEdificio = idEdificio,
                    Nombre = nombreLimpio,
                    Direccion = direccionLimpia,
                    Estado = Estados.Activo
                };
            }, ct).ConfigureAwait(false);
        }
        catch (ConflictoUnicidadException ex)
        {
            throw new ReglaNegocioException(MensajeDuplicado, ex);
        }
    }

    public async Task<bool> ActualizarAsync(
        int idEdificio,
        string nombre,
        string direccion,
        string estado,
        CancellationToken ct = default)
    {
        var nombreLimpio = Validador.Requerido(nombre, MensajeNombreObligatorio);
        var direccionLimpia = Validador.Requerido(direccion, MensajeDireccionObligatoria);
        var estadoValidado = Validador.Estado(estado, Estados.Edificio);

        try
        {
            return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
            {
                var edificio = await _edificios.ObtenerAsync(idEdificio, conexion, transaccion, token).ConfigureAwait(false);
                if (edificio is null)
                {
                    throw new ReglaNegocioException(MensajeNoExiste);
                }

                if (await _edificios.NombreExisteAsync(nombreLimpio, idEdificio, conexion, transaccion, token).ConfigureAwait(false))
                {
                    throw new ReglaNegocioException(MensajeDuplicado);
                }

                var cambios = new List<(string Campo, object? Anterior, object? Nuevo)>();
                if (edificio.Nombre != nombreLimpio)
                {
                    cambios.Add(("Nombre", edificio.Nombre, nombreLimpio));
                }

                if (edificio.Direccion != direccionLimpia)
                {
                    cambios.Add(("Direccion", edificio.Direccion, direccionLimpia));
                }

                if (edificio.Estado != estadoValidado)
                {
                    cambios.Add(("Estado", edificio.Estado, estadoValidado));
                }

                if (cambios.Count == 0)
                {
                    return false;
                }

                await _edificios.ActualizarAsync(idEdificio, nombreLimpio, direccionLimpia, estadoValidado, conexion, transaccion, token).ConfigureAwait(false);

                await _auditoria.RegistrarAsync(
                    "Modificacion",
                    "Edificio",
                    idEdificio,
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

    public async Task<bool> DarDeBajaAsync(int idEdificio, CancellationToken ct = default)
    {
        return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
        {
            var edificio = await _edificios.ObtenerAsync(idEdificio, conexion, transaccion, token).ConfigureAwait(false);
            if (edificio is null)
            {
                throw new ReglaNegocioException(MensajeNoExiste);
            }

            if (edificio.Estado == Estados.Inactivo)
            {
                return false;
            }

            var estadoAnterior = edificio.Estado;

            await _edificios.ActualizarEstadoAsync(idEdificio, Estados.Inactivo, conexion, transaccion, token).ConfigureAwait(false);

            await _auditoria.RegistrarAsync(
                "Baja",
                "Edificio",
                idEdificio,
                Detalles.Modificacion([("Estado", estadoAnterior, Estados.Inactivo)]),
                null,
                conexion,
                transaccion,
                token).ConfigureAwait(false);

            return true;
        }, ct).ConfigureAwait(false);
    }
}
