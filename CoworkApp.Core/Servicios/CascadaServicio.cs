using CoworkApp.Core.Datos;
using CoworkApp.Core.Dominio;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Validacion;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Servicios;

public sealed class CascadaServicio
{
    public const string MensajeDuplicado = "No se pudo registrar la oficina: ya existe un registro con el mismo nombre.";
    public const string MensajeEdificioNoExiste = "El edificio seleccionado no existe.";
    public const string MensajePisoNoExiste = "El piso seleccionado no existe.";

    private readonly IEdificioRepositorio _edificios;
    private readonly IPisoRepositorio _pisos;
    private readonly IOficinaRepositorio _oficinas;
    private readonly IAuditoriaRepositorio _auditoria;
    private readonly ITransaccionDb _transaccion;

    public CascadaServicio(
        IEdificioRepositorio edificios,
        IPisoRepositorio pisos,
        IOficinaRepositorio oficinas,
        IAuditoriaRepositorio auditoria,
        ITransaccionDb transaccion)
    {
        _edificios = edificios ?? throw new ArgumentNullException(nameof(edificios));
        _pisos = pisos ?? throw new ArgumentNullException(nameof(pisos));
        _oficinas = oficinas ?? throw new ArgumentNullException(nameof(oficinas));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _transaccion = transaccion ?? throw new ArgumentNullException(nameof(transaccion));
    }

    public async Task<Oficina> RegistrarAsync(
        EdificioCascada edificio,
        PisoCascada piso,
        OficinaCascada oficina,
        CancellationToken ct = default)
    {
        try
        {
            return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
            {
                var idEdificio = await ResolverEdificioAsync(edificio, conexion, transaccion, token).ConfigureAwait(false);
                var idPiso = await ResolverPisoAsync(piso, idEdificio, conexion, transaccion, token).ConfigureAwait(false);

                var idOficina = await _oficinas.AgregarAsync(
                    idPiso,
                    oficina.Nombre,
                    oficina.Capacidad,
                    oficina.Tarifa,
                    oficina.HoraApertura,
                    oficina.HoraCierre,
                    conexion,
                    transaccion,
                    token).ConfigureAwait(false);

                await _auditoria.RegistrarAsync(
                    "Alta",
                    "Oficina",
                    idOficina,
                    Detalles.Alta(
                    [
                        new("IdPiso", idPiso),
                        new("Nombre", oficina.Nombre),
                        new("Capacidad", oficina.Capacidad),
                        new("TarifaHora", oficina.Tarifa),
                        new("HoraApertura", oficina.HoraApertura.ToString("HH:mm")),
                        new("HoraCierre", oficina.HoraCierre.ToString("HH:mm")),
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
                    Nombre = oficina.Nombre,
                    Capacidad = oficina.Capacidad,
                    TarifaHora = oficina.Tarifa,
                    HoraApertura = oficina.HoraApertura,
                    HoraCierre = oficina.HoraCierre,
                    Estado = Estados.Habilitada
                };
            }, ct).ConfigureAwait(false);
        }
        catch (ConflictoUnicidadException ex)
        {
            throw new ReglaNegocioException(MensajeDuplicado, ex);
        }
    }

    private async Task<int> ResolverEdificioAsync(
        EdificioCascada edificio,
        SqlConnection conexion,
        SqlTransaction transaccion,
        CancellationToken ct)
    {
        if (edificio.Id.HasValue)
        {
            var existente = await _edificios.ObtenerAsync(edificio.Id.Value, conexion, transaccion, ct).ConfigureAwait(false);
            if (existente is null)
            {
                throw new ReglaNegocioException(MensajeEdificioNoExiste);
            }

            return existente.IdEdificio;
        }

        var idEdificio = await _edificios.AgregarAsync(edificio.Nombre, edificio.Direccion, conexion, transaccion, ct).ConfigureAwait(false);

        await _auditoria.RegistrarAsync(
            "Alta",
            "Edificio",
            idEdificio,
            Detalles.Alta(
            [
                new("Nombre", edificio.Nombre),
                new("Direccion", edificio.Direccion),
                new("Estado", Estados.Activo)
            ]),
            null,
            conexion,
            transaccion,
            ct).ConfigureAwait(false);

        return idEdificio;
    }

    private async Task<int> ResolverPisoAsync(
        PisoCascada piso,
        int idEdificio,
        SqlConnection conexion,
        SqlTransaction transaccion,
        CancellationToken ct)
    {
        if (piso.Id.HasValue)
        {
            var existente = await _pisos.ObtenerAsync(piso.Id.Value, conexion, transaccion, ct).ConfigureAwait(false);
            if (existente is null)
            {
                throw new ReglaNegocioException(MensajePisoNoExiste);
            }

            return existente.IdPiso;
        }

        var idPiso = await _pisos.AgregarAsync(idEdificio, piso.Nombre, conexion, transaccion, ct).ConfigureAwait(false);

        await _auditoria.RegistrarAsync(
            "Alta",
            "Piso",
            idPiso,
            Detalles.Alta(
            [
                new("IdEdificio", idEdificio),
                new("Nombre", piso.Nombre),
                new("Estado", Estados.Activo)
            ]),
            null,
            conexion,
            transaccion,
            ct).ConfigureAwait(false);

        return idPiso;
    }
}
