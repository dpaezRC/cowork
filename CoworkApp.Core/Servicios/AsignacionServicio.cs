using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Validacion;

namespace CoworkApp.Core.Servicios;

public sealed class AsignacionServicio
{
    public const string MensajeFranjaDuplicada = "El empleado ya tiene esa oficina asignada con la misma hora de inicio.";
    public const string MensajeHorarioInvalido = "La hora de fin debe ser posterior a la hora de inicio.";
    public const string MensajeEmpleadoNoExiste = "El empleado no existe.";
    public const string MensajeOficinaNoExiste = "La oficina no existe.";
    public const string MensajeOficinaOtroEdificio = "La oficina no pertenece al edificio del empleado.";
    public const string MensajeAsignacionNoExiste = "La asignacion no existe.";

    private readonly IEmpleadoRepositorio _empleados;
    private readonly IOficinaRepositorio _oficinas;
    private readonly IPisoRepositorio _pisos;
    private readonly IEmpleadoOficinaRepositorio _asignaciones;
    private readonly IAuditoriaRepositorio _auditoria;
    private readonly ITransaccionDb _transaccion;

    public AsignacionServicio(
        IEmpleadoRepositorio empleados,
        IOficinaRepositorio oficinas,
        IPisoRepositorio pisos,
        IEmpleadoOficinaRepositorio asignaciones,
        IAuditoriaRepositorio auditoria,
        ITransaccionDb transaccion)
    {
        _empleados = empleados ?? throw new ArgumentNullException(nameof(empleados));
        _oficinas = oficinas ?? throw new ArgumentNullException(nameof(oficinas));
        _pisos = pisos ?? throw new ArgumentNullException(nameof(pisos));
        _asignaciones = asignaciones ?? throw new ArgumentNullException(nameof(asignaciones));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _transaccion = transaccion ?? throw new ArgumentNullException(nameof(transaccion));
    }

    public Task<List<AsignacionDetalle>> ListarAsync(int idEmpleado, CancellationToken ct = default)
    {
        return _asignaciones.ListarPorEmpleadoAsync(idEmpleado, ct: ct);
    }

    public async Task<EmpleadoOficina> AsignarAsync(
        int idEmpleado,
        int idOficina,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        CancellationToken ct = default)
    {
        if (horaFin <= horaInicio)
        {
            throw new ReglaNegocioException(MensajeHorarioInvalido);
        }

        try
        {
            return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
            {
                var empleado = await _empleados.ObtenerDetalleAsync(idEmpleado, conexion, transaccion, token).ConfigureAwait(false);
                if (empleado is null)
                {
                    throw new ReglaNegocioException(MensajeEmpleadoNoExiste);
                }

                var oficina = await _oficinas.ObtenerAsync(idOficina, conexion, transaccion, token).ConfigureAwait(false);
                if (oficina is null)
                {
                    throw new ReglaNegocioException(MensajeOficinaNoExiste);
                }

                var piso = await _pisos.ObtenerAsync(oficina.IdPiso, conexion, transaccion, token).ConfigureAwait(false);
                if (piso is null || piso.IdEdificio != empleado.IdEdificio)
                {
                    throw new ReglaNegocioException(MensajeOficinaOtroEdificio);
                }

                if (await _asignaciones.ExisteFranjaAsync(idEmpleado, idOficina, horaInicio, conexion, transaccion, token).ConfigureAwait(false))
                {
                    throw new ReglaNegocioException(MensajeFranjaDuplicada);
                }

                var idAsignacion = await _asignaciones.AgregarAsync(
                    idEmpleado,
                    idOficina,
                    horaInicio,
                    horaFin,
                    conexion,
                    transaccion,
                    token).ConfigureAwait(false);

                await _auditoria.RegistrarAsync(
                    "Alta",
                    "EmpleadoOficina",
                    idAsignacion,
                    Detalles.Alta(
                    [
                        new("IdEmpleado", idEmpleado),
                        new("IdOficina", idOficina),
                        new("HoraInicio", horaInicio.ToString("HH:mm")),
                        new("HoraFin", horaFin.ToString("HH:mm"))
                    ]),
                    null,
                    conexion,
                    transaccion,
                    token).ConfigureAwait(false);

                return new EmpleadoOficina
                {
                    IdEmpleadoOficina = idAsignacion,
                    IdEmpleado = idEmpleado,
                    IdOficina = idOficina,
                    HoraInicio = horaInicio,
                    HoraFin = horaFin
                };
            }, ct).ConfigureAwait(false);
        }
        catch (ConflictoUnicidadException ex)
        {
            throw new ReglaNegocioException(MensajeFranjaDuplicada, ex);
        }
    }

    public async Task<bool> QuitarAsync(int idAsignacion, CancellationToken ct = default)
    {
        return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
        {
            var asignacion = await _asignaciones.ObtenerDetalleAsync(idAsignacion, conexion, transaccion, token).ConfigureAwait(false);
            if (asignacion is null)
            {
                throw new ReglaNegocioException(MensajeAsignacionNoExiste);
            }

            var detalle = Detalles.Alta(
            [
                new("IdEmpleado", asignacion.IdEmpleado),
                new("IdOficina", asignacion.IdOficina),
                new("HoraInicio", asignacion.HoraInicio.ToString("HH:mm")),
                new("HoraFin", asignacion.HoraFin.ToString("HH:mm"))
            ]);

            await _asignaciones.EliminarAsync(idAsignacion, conexion, transaccion, token).ConfigureAwait(false);

            await _auditoria.RegistrarAsync(
                "Baja",
                "EmpleadoOficina",
                idAsignacion,
                detalle,
                null,
                conexion,
                transaccion,
                token).ConfigureAwait(false);

            return true;
        }, ct).ConfigureAwait(false);
    }
}
