using CoworkApp.Core.Datos;
using CoworkApp.Core.Dominio;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Validacion;

namespace CoworkApp.Core.Servicios;

public sealed class OficinaServicio
{
    public const string MensajeDuplicado = "Ya existe una oficina con ese nombre en el piso seleccionado.";
    public const string MensajeNombreObligatorio = "El nombre de la oficina es obligatorio.";
    public const string MensajeCapacidadPositiva = "La capacidad debe ser mayor que cero.";
    public const string MensajeTarifaNegativa = "La tarifa no puede ser negativa.";
    public const string MensajeTarifaDecimales = "La tarifa no puede tener mas de 2 decimales.";
    public const string MensajeHorarioInvalido = "La hora de cierre debe ser posterior a la hora de apertura.";
    public const string MensajePisoNoExiste = "El piso seleccionado no existe.";
    public const string MensajeNoExiste = "La oficina no existe.";

    private readonly IOficinaRepositorio _oficinas;
    private readonly IPisoRepositorio _pisos;
    private readonly IAuditoriaRepositorio _auditoria;
    private readonly ITransaccionDb _transaccion;

    public OficinaServicio(
        IOficinaRepositorio oficinas,
        IPisoRepositorio pisos,
        IAuditoriaRepositorio auditoria,
        ITransaccionDb transaccion)
    {
        _oficinas = oficinas ?? throw new ArgumentNullException(nameof(oficinas));
        _pisos = pisos ?? throw new ArgumentNullException(nameof(pisos));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _transaccion = transaccion ?? throw new ArgumentNullException(nameof(transaccion));
    }

    public Task<List<Oficina>> ListarAsync(CancellationToken ct = default)
    {
        return _oficinas.ListarAsync(ct);
    }

    public Task<List<OficinaDetalle>> ListarDetalleAsync(CancellationToken ct = default)
    {
        return _oficinas.ListarDetalleAsync(ct);
    }

    public Task<Oficina?> ObtenerAsync(int idOficina, CancellationToken ct = default)
    {
        return _oficinas.ObtenerAsync(idOficina, ct: ct);
    }

    public Task<List<Oficina>> ListarPorEdificioAsync(int idEdificio, CancellationToken ct = default)
    {
        return _oficinas.ListarPorEdificioAsync(idEdificio, ct);
    }

    public Task<Oficina?> BuscarPorNombreAsync(int idPiso, string nombre, CancellationToken ct = default)
    {
        return _oficinas.BuscarPorNombreAsync(idPiso, nombre, ct);
    }

    public async Task<Oficina> CrearAsync(
        int idPiso,
        string nombre,
        int capacidad,
        decimal tarifa,
        TimeOnly horaApertura,
        TimeOnly horaCierre,
        CancellationToken ct = default)
    {
        var nombreLimpio = Validador.Requerido(nombre, MensajeNombreObligatorio);
        ValidarCapacidad(capacidad);
        ValidarTarifa(tarifa);
        ValidarHorario(horaApertura, horaCierre);

        try
        {
            return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
            {
                if (await _pisos.ObtenerAsync(idPiso, conexion, transaccion, token).ConfigureAwait(false) is null)
                {
                    throw new ReglaNegocioException(MensajePisoNoExiste);
                }

                if (await _oficinas.NombreExisteAsync(idPiso, nombreLimpio, null, conexion, transaccion, token).ConfigureAwait(false))
                {
                    throw new ReglaNegocioException(MensajeDuplicado);
                }

                var idOficina = await _oficinas.AgregarAsync(idPiso, nombreLimpio, capacidad, tarifa, horaApertura, horaCierre, conexion, transaccion, token).ConfigureAwait(false);

                await _auditoria.RegistrarAsync(
                    "Alta",
                    "Oficina",
                    idOficina,
                    Detalles.Alta(
                    [
                        new("IdPiso", idPiso),
                        new("Nombre", nombreLimpio),
                        new("Capacidad", capacidad),
                        new("TarifaHora", tarifa),
                        new("HoraApertura", horaApertura.ToString("HH:mm")),
                        new("HoraCierre", horaCierre.ToString("HH:mm")),
                        new("Estado", Estados.Habilitada)
                    ]),
                    null,
                    conexion,
                    transaccion,
                    token).ConfigureAwait(false);

                return new Oficina
                {
                    IdOficina = idOficina,
                    IdPiso = idPiso,
                    Nombre = nombreLimpio,
                    Capacidad = capacidad,
                    TarifaHora = tarifa,
                    HoraApertura = horaApertura,
                    HoraCierre = horaCierre,
                    Estado = Estados.Habilitada
                };
            }, ct).ConfigureAwait(false);
        }
        catch (ConflictoUnicidadException ex)
        {
            throw new ReglaNegocioException(MensajeDuplicado, ex);
        }
    }

    public async Task<bool> ActualizarAsync(
        int idOficina,
        string nombre,
        int capacidad,
        decimal tarifa,
        TimeOnly horaApertura,
        TimeOnly horaCierre,
        string estado,
        CancellationToken ct = default)
    {
        var nombreLimpio = Validador.Requerido(nombre, MensajeNombreObligatorio);
        ValidarCapacidad(capacidad);
        ValidarTarifa(tarifa);
        ValidarHorario(horaApertura, horaCierre);
        var estadoValidado = Validador.Estado(estado, Estados.Oficina);

        try
        {
            return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
            {
                var oficina = await _oficinas.ObtenerAsync(idOficina, conexion, transaccion, token).ConfigureAwait(false);
                if (oficina is null)
                {
                    throw new ReglaNegocioException(MensajeNoExiste);
                }

                if (await _oficinas.NombreExisteAsync(oficina.IdPiso, nombreLimpio, idOficina, conexion, transaccion, token).ConfigureAwait(false))
                {
                    throw new ReglaNegocioException(MensajeDuplicado);
                }

                var cambios = new List<(string Campo, object? Anterior, object? Nuevo)>();
                if (oficina.Nombre != nombreLimpio)
                {
                    cambios.Add(("Nombre", oficina.Nombre, nombreLimpio));
                }

                if (oficina.Capacidad != capacidad)
                {
                    cambios.Add(("Capacidad", oficina.Capacidad, capacidad));
                }

                if (oficina.TarifaHora != tarifa)
                {
                    cambios.Add(("TarifaHora", oficina.TarifaHora, tarifa));
                }

                if (oficina.HoraApertura != horaApertura)
                {
                    cambios.Add(("HoraApertura", oficina.HoraApertura.ToString("HH:mm"), horaApertura.ToString("HH:mm")));
                }

                if (oficina.HoraCierre != horaCierre)
                {
                    cambios.Add(("HoraCierre", oficina.HoraCierre.ToString("HH:mm"), horaCierre.ToString("HH:mm")));
                }

                if (oficina.Estado != estadoValidado)
                {
                    cambios.Add(("Estado", oficina.Estado, estadoValidado));
                }

                if (cambios.Count == 0)
                {
                    return false;
                }

                await _oficinas.ActualizarAsync(idOficina, nombreLimpio, capacidad, tarifa, horaApertura, horaCierre, estadoValidado, conexion, transaccion, token).ConfigureAwait(false);

                await _auditoria.RegistrarAsync(
                    "Modificacion",
                    "Oficina",
                    idOficina,
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

    public async Task<bool> DarDeBajaAsync(int idOficina, CancellationToken ct = default)
    {
        return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
        {
            var oficina = await _oficinas.ObtenerAsync(idOficina, conexion, transaccion, token).ConfigureAwait(false);
            if (oficina is null)
            {
                throw new ReglaNegocioException(MensajeNoExiste);
            }

            if (oficina.Estado == Estados.Baja)
            {
                return false;
            }

            var estadoAnterior = oficina.Estado;

            await _oficinas.ActualizarEstadoAsync(idOficina, Estados.Baja, conexion, transaccion, token).ConfigureAwait(false);

            await _auditoria.RegistrarAsync(
                "Baja",
                "Oficina",
                idOficina,
                Detalles.Modificacion([("Estado", estadoAnterior, Estados.Baja)]),
                null,
                conexion,
                transaccion,
                token).ConfigureAwait(false);

            return true;
        }, ct).ConfigureAwait(false);
    }

    private static void ValidarCapacidad(int capacidad)
    {
        if (capacidad <= 0)
        {
            throw new ReglaNegocioException(MensajeCapacidadPositiva);
        }
    }

    private static void ValidarTarifa(decimal tarifa)
    {
        if (tarifa < 0)
        {
            throw new ReglaNegocioException(MensajeTarifaNegativa);
        }

        if (ObtenerEscala(tarifa) > 2)
        {
            throw new ReglaNegocioException(MensajeTarifaDecimales);
        }
    }

    private static void ValidarHorario(TimeOnly horaApertura, TimeOnly horaCierre)
    {
        if (horaCierre <= horaApertura)
        {
            throw new ReglaNegocioException(MensajeHorarioInvalido);
        }
    }

    private static int ObtenerEscala(decimal valor)
    {
        return (decimal.GetBits(valor)[3] >> 16) & 0xFF;
    }
}
