using CoworkApp.Core.Datos;
using CoworkApp.Core.Dominio;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Seguridad;
using CoworkApp.Core.Validacion;

namespace CoworkApp.Core.Servicios;

public sealed class UsuarioServicio
{
    public const string MensajeEmailDuplicado = "Ya existe un usuario con ese email.";
    public const string MensajeRolEmpleado = "Los usuarios con rol Empleado se gestionan desde el modulo Personal.";
    public const string MensajeNombreObligatorio = "El nombre es obligatorio.";
    public const string MensajeApellidoObligatorio = "El apellido es obligatorio.";
    public const string MensajeContrasenaObligatoria = "La contrasena es obligatoria.";
    public const string MensajeNoExiste = "El usuario no existe.";
    public const string RolEmpleado = Roles.Empleado;

    private readonly IUsuarioRepositorio _usuarios;
    private readonly IAuditoriaRepositorio _auditoria;
    private readonly ITransaccionDb _transaccion;

    public UsuarioServicio(
        IUsuarioRepositorio usuarios,
        IAuditoriaRepositorio auditoria,
        ITransaccionDb transaccion)
    {
        _usuarios = usuarios ?? throw new ArgumentNullException(nameof(usuarios));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _transaccion = transaccion ?? throw new ArgumentNullException(nameof(transaccion));
    }

    public Task<List<Usuario>> ListarAsync(CancellationToken ct = default)
    {
        return _usuarios.ListarAsync(ct);
    }

    public Task<Usuario?> ObtenerAsync(int idUsuario, CancellationToken ct = default)
    {
        return _usuarios.ObtenerAsync(idUsuario, ct: ct);
    }

    public async Task<Usuario> CrearAsync(
        string nombre,
        string apellido,
        string email,
        string? telefono,
        string contrasena,
        string rol,
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

        var rolValidado = Validador.Rol(rol, Roles.Gestionables);

        try
        {
            return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
            {
                if (await _usuarios.EmailExisteAsync(emailLimpio, null, conexion, transaccion, token).ConfigureAwait(false))
                {
                    throw new ReglaNegocioException(MensajeEmailDuplicado);
                }

                var hash = PasswordHasher.Hash(contrasena);
                var idUsuario = await _usuarios.AgregarAsync(
                    emailLimpio,
                    hash,
                    nombreLimpio,
                    apellidoLimpio,
                    telefonoLimpio,
                    rolValidado,
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
                        new("Rol", rolValidado),
                        new("Estado", Estados.Activo)
                    ]),
                    null,
                    conexion,
                    transaccion,
                    token).ConfigureAwait(false);

                return new Usuario
                {
                    IdUsuario = idUsuario,
                    Email = emailLimpio,
                    HashContrasena = hash,
                    Nombre = nombreLimpio,
                    Apellido = apellidoLimpio,
                    Telefono = telefonoLimpio,
                    Rol = rolValidado,
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
        int idUsuario,
        string nombre,
        string apellido,
        string email,
        string? telefono,
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

        var estadoValidado = Validador.Estado(estado, Estados.Usuario);

        try
        {
            return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
            {
                var usuario = await _usuarios.ObtenerAsync(idUsuario, conexion, transaccion, token).ConfigureAwait(false);
                if (usuario is null)
                {
                    throw new ReglaNegocioException(MensajeNoExiste);
                }

                if (usuario.Rol == RolEmpleado)
                {
                    throw new ReglaNegocioException(MensajeRolEmpleado);
                }

                if (await _usuarios.EmailExisteAsync(emailLimpio, idUsuario, conexion, transaccion, token).ConfigureAwait(false))
                {
                    throw new ReglaNegocioException(MensajeEmailDuplicado);
                }

                var cambios = new List<(string Campo, object? Anterior, object? Nuevo)>();
                if (usuario.Nombre != nombreLimpio)
                {
                    cambios.Add(("Nombre", usuario.Nombre, nombreLimpio));
                }

                if (usuario.Apellido != apellidoLimpio)
                {
                    cambios.Add(("Apellido", usuario.Apellido, apellidoLimpio));
                }

                if (usuario.Email != emailLimpio)
                {
                    cambios.Add(("Email", usuario.Email, emailLimpio));
                }

                if (usuario.Telefono != telefonoLimpio)
                {
                    cambios.Add(("Telefono", usuario.Telefono ?? "None", telefonoLimpio ?? "None"));
                }

                string? hashNuevo = null;
                if (contrasenaLimpia.Length > 0)
                {
                    hashNuevo = PasswordHasher.Hash(contrasenaLimpia);
                    cambios.Add(("Contrasena", "(oculta)", "actualizada"));
                }

                if (usuario.Estado != estadoValidado)
                {
                    cambios.Add(("Estado", usuario.Estado, estadoValidado));
                }

                if (cambios.Count == 0)
                {
                    return false;
                }

                await _usuarios.ActualizarAsync(
                    idUsuario,
                    nombreLimpio,
                    apellidoLimpio,
                    emailLimpio,
                    telefonoLimpio,
                    estadoValidado,
                    hashNuevo,
                    conexion,
                    transaccion,
                    token).ConfigureAwait(false);

                await _auditoria.RegistrarAsync(
                    "Modificacion",
                    "Usuario",
                    idUsuario,
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

    public async Task<bool> DarDeBajaAsync(int idUsuario, CancellationToken ct = default)
    {
        return await _transaccion.EnTransaccionAsync(async (conexion, transaccion, token) =>
        {
            var usuario = await _usuarios.ObtenerAsync(idUsuario, conexion, transaccion, token).ConfigureAwait(false);
            if (usuario is null)
            {
                throw new ReglaNegocioException(MensajeNoExiste);
            }

            if (usuario.Rol == RolEmpleado)
            {
                throw new ReglaNegocioException(MensajeRolEmpleado);
            }

            if (usuario.Estado == Estados.Inactivo)
            {
                return false;
            }

            var estadoAnterior = usuario.Estado;

            await _usuarios.ActualizarAsync(
                idUsuario,
                usuario.Nombre,
                usuario.Apellido,
                usuario.Email,
                usuario.Telefono,
                Estados.Inactivo,
                null,
                conexion,
                transaccion,
                token).ConfigureAwait(false);

            await _auditoria.RegistrarAsync(
                "Baja",
                "Usuario",
                idUsuario,
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
