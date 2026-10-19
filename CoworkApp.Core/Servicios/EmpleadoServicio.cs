using CoworkApp.Core.Datos;
using CoworkApp.Core.Dominio;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Seguridad;
using CoworkApp.Core.Validacion;

namespace CoworkApp.Core.Servicios;

public sealed class EmpleadoServicio
{
    public const string MensajeEmailDuplicado = "Ya existe un usuario con ese email.";
    public const string MensajeEdificioConAsignaciones = "No se puede cambiar el edificio: el empleado tiene oficinas asignadas. Quita las asignaciones primero.";
    public const string MensajeNombreObligatorio = "El nombre del empleado es obligatorio.";
    public const string MensajeApellidoObligatorio = "El apellido del empleado es obligatorio.";
    public const string MensajeContrasenaObligatoria = "La contrasena es obligatoria.";
    public const string MensajeEdificioNoExiste = "El edificio seleccionado no existe.";
    public const string MensajeNoExiste = "El empleado no existe.";
    public const string RolEmpleado = Roles.Empleado;

    private readonly IEmpleadoRepositorio _empleados;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IEdificioRepositorio _edificios;
    private readonly IEmpleadoOficinaRepositorio _asignaciones;
    private readonly IAuditoriaRepositorio _auditoria;
    private readonly ITransaccionDb _transaccion;

    public EmpleadoServicio(
        IEmpleadoRepositorio empleados,
        IUsuarioRepositorio usuarios,
        IEdificioRepositorio edificios,
        IEmpleadoOficinaRepositorio asignaciones,
        IAuditoriaRepositorio auditoria,
        ITransaccionDb transaccion)
    {
        _empleados = empleados ?? throw new ArgumentNullException(nameof(empleados));
        _usuarios = usuarios ?? throw new ArgumentNullException(nameof(usuarios));
        _edificios = edificios ?? throw new ArgumentNullException(nameof(edificios));
        _asignaciones = asignaciones ?? throw new ArgumentNullException(nameof(asignaciones));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _transaccion = transaccion ?? throw new ArgumentNullException(nameof(transaccion));
    }

    public Task<List<EmpleadoDetalle>> ListarAsync(CancellationToken ct = default)
    {
        return _empleados.ListarDetalleAsync(ct);
    }

    public Task<EmpleadoDetalle?> ObtenerAsync(int idEmpleado, CancellationToken ct = default)
    {
        return _empleados.ObtenerDetalleAsync(idEmpleado, ct: ct);
    }

    public async Task<EmpleadoDetalle> CrearAsync(
        string nombre,
        string apellido,
        string email,
        string? telefono,
        string contrasena,
        int idEdificio,
        CancellationToken ct = default)
    {
        var nombreLimpio = (nombre ?? string.Empty).Trim();
        var apellidoLimpio = (apellido ?? string.Empty).Trim();
        var emailLimpio = Validador.Email(email);
        var telefonoLimpio = NormalizarTelefono(telefono);

        if (nombreLimpio.Length == 0)
        {
            throw new ReglaNegocioException(MensajeNombreObligatorio);
        }

        if (apellidoLimpio.Length == 0)
        {
            throw new ReglaNegocioException(MensajeApellidoObligatorio);
        }

        if (string.IsNullOrEmpty(contrasena))
        {
            throw new ReglaNegocioException(MensajeContrasenaObligatoria);
        }

        try
        {
            return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
            {
                var edificio = await _edificios.ObtenerAsync(idEdificio, conexion, transaccion, token).ConfigureAwait(false);
                if (edificio is null)
                {
                    throw new ReglaNegocioException(MensajeEdificioNoExiste);
                }

                if (await _usuarios.EmailExisteAsync(emailLimpio, null, conexion, transaccion, token).ConfigureAwait(false))
                {
                    throw new ReglaNegocioException(MensajeEmailDuplicado);
                }

                var idUsuario = await _usuarios.AgregarAsync(
                    emailLimpio,
                    PasswordHasher.Hash(contrasena),
                    nombreLimpio,
                    apellidoLimpio,
                    telefonoLimpio,
                    RolEmpleado,
                    Estados.Activo,
                    conexion,
                    transaccion,
                    token).ConfigureAwait(false);

                await _auditoria.RegistrarAsync(
                    "Alta",
                    "Usuario",
                    idUsuario,
                    Detalles.Alta(
                    [
                        new("Email", emailLimpio),
                        new("Nombre", nombreLimpio),
                        new("Apellido", apellidoLimpio),
                        new("Telefono", telefonoLimpio ?? "None"),
                        new("Rol", RolEmpleado),
                        new("Estado", Estados.Activo)
                    ]),
                    null,
                    conexion,
                    transaccion,
                    token).ConfigureAwait(false);

                var idEmpleado = await _empleados.AgregarAsync(idUsuario, idEdificio, conexion, transaccion, token).ConfigureAwait(false);

                await _auditoria.RegistrarAsync(
                    "Alta",
                    "Empleado",
                    idEmpleado,
                    Detalles.Alta(
                    [
                        new("IdUsuario", idUsuario),
                        new("IdEdificio", idEdificio),
                        new("Estado", Estados.Activo)
                    ]),
                    null,
                    conexion,
                    transaccion,
                    token).ConfigureAwait(false);

                return new EmpleadoDetalle
                {
                    IdEmpleado = idEmpleado,
                    IdUsuario = idUsuario,
                    Nombre = nombreLimpio,
                    Apellido = apellidoLimpio,
                    Email = emailLimpio,
                    Telefono = telefonoLimpio,
                    IdEdificio = idEdificio,
                    Edificio = edificio.Nombre,
                    Estado = Estados.Activo,
                    FechaAlta = DateTime.Now
                };
            }, ct).ConfigureAwait(false);
        }
        catch (ConflictoUnicidadException ex)
        {
            throw new ReglaNegocioException(MensajeEmailDuplicado, ex);
        }
    }

    public async Task<bool> ActualizarAsync(
        int idEmpleado,
        string nombre,
        string apellido,
        string email,
        string? telefono,
        int idEdificio,
        string estado,
        string? contrasena = null,
        CancellationToken ct = default)
    {
        var nombreLimpio = (nombre ?? string.Empty).Trim();
        var apellidoLimpio = (apellido ?? string.Empty).Trim();
        var emailLimpio = Validador.Email(email);
        var telefonoLimpio = NormalizarTelefono(telefono);
        var contrasenaLimpia = (contrasena ?? string.Empty).Trim();

        if (nombreLimpio.Length == 0)
        {
            throw new ReglaNegocioException(MensajeNombreObligatorio);
        }

        if (apellidoLimpio.Length == 0)
        {
            throw new ReglaNegocioException(MensajeApellidoObligatorio);
        }

        var estadoValidado = Validador.Estado(estado, Estados.Empleado);

        try
        {
            return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
            {
                var empleado = await _empleados.ObtenerDetalleAsync(idEmpleado, conexion, transaccion, token).ConfigureAwait(false);
                if (empleado is null)
                {
                    throw new ReglaNegocioException(MensajeNoExiste);
                }

                if (await _usuarios.EmailExisteAsync(emailLimpio, empleado.IdUsuario, conexion, transaccion, token).ConfigureAwait(false))
                {
                    throw new ReglaNegocioException(MensajeEmailDuplicado);
                }

                if (idEdificio != empleado.IdEdificio
                    && await _asignaciones.TieneAsignacionesAsync(idEmpleado, conexion, transaccion, token).ConfigureAwait(false))
                {
                    throw new ReglaNegocioException(MensajeEdificioConAsignaciones);
                }

                var cambios = new List<(string Campo, object? Anterior, object? Nuevo)>();
                if (empleado.Nombre != nombreLimpio)
                {
                    cambios.Add(("Nombre", empleado.Nombre, nombreLimpio));
                }

                if (empleado.Apellido != apellidoLimpio)
                {
                    cambios.Add(("Apellido", empleado.Apellido, apellidoLimpio));
                }

                if (empleado.Email != emailLimpio)
                {
                    cambios.Add(("Email", empleado.Email, emailLimpio));
                }

                if (empleado.Telefono != telefonoLimpio)
                {
                    cambios.Add(("Telefono", empleado.Telefono ?? "None", telefonoLimpio ?? "None"));
                }

                string? hashNuevo = null;
                if (contrasenaLimpia.Length > 0)
                {
                    hashNuevo = PasswordHasher.Hash(contrasenaLimpia);
                    cambios.Add(("Contrasena", "(oculta)", "actualizada"));
                }

                if (empleado.IdEdificio != idEdificio)
                {
                    cambios.Add(("IdEdificio", empleado.IdEdificio, idEdificio));
                }

                var estadoCambio = empleado.Estado != estadoValidado;
                if (estadoCambio)
                {
                    cambios.Add(("Estado", empleado.Estado, estadoValidado));
                }

                if (cambios.Count == 0)
                {
                    return false;
                }

                var usuario = await _usuarios.ObtenerAsync(empleado.IdUsuario, conexion, transaccion, token).ConfigureAwait(false);
                var estadoUsuario = estadoCambio ? estadoValidado : usuario?.Estado ?? estadoValidado;

                await _usuarios.ActualizarAsync(
                    empleado.IdUsuario,
                    nombreLimpio,
                    apellidoLimpio,
                    emailLimpio,
                    telefonoLimpio,
                    estadoUsuario,
                    hashNuevo,
                    conexion,
                    transaccion,
                    token).ConfigureAwait(false);

                await _empleados.ActualizarAsync(idEmpleado, idEdificio, estadoValidado, conexion, transaccion, token).ConfigureAwait(false);

                await _auditoria.RegistrarAsync(
                    "Modificacion",
                    "Empleado",
                    idEmpleado,
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
            throw new ReglaNegocioException(MensajeEmailDuplicado, ex);
        }
    }

    public async Task<bool> DarDeBajaAsync(int idEmpleado, CancellationToken ct = default)
    {
        return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
        {
            var empleado = await _empleados.ObtenerDetalleAsync(idEmpleado, conexion, transaccion, token).ConfigureAwait(false);
            if (empleado is null)
            {
                throw new ReglaNegocioException(MensajeNoExiste);
            }

            if (empleado.Estado == Estados.Inactivo)
            {
                return false;
            }

            var estadoAnterior = empleado.Estado;

            await _empleados.ActualizarEstadoAsync(idEmpleado, Estados.Inactivo, conexion, transaccion, token).ConfigureAwait(false);

            await _usuarios.ActualizarAsync(
                empleado.IdUsuario,
                empleado.Nombre,
                empleado.Apellido,
                empleado.Email,
                empleado.Telefono,
                Estados.Inactivo,
                null,
                conexion,
                transaccion,
                token).ConfigureAwait(false);

            await _auditoria.RegistrarAsync(
                "Baja",
                "Empleado",
                idEmpleado,
                Detalles.Modificacion([("Estado", estadoAnterior, Estados.Inactivo)]),
                null,
                conexion,
                transaccion,
                token).ConfigureAwait(false);

            return true;
        }, ct).ConfigureAwait(false);
    }

    private static string? NormalizarTelefono(string? telefono)
    {
        var texto = (telefono ?? string.Empty).Trim();
        return texto.Length == 0 ? null : texto;
    }
}
