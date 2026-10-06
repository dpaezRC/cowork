namespace CoworkApp.Shared.Security;

/// <summary>
/// Sesión del usuario logueado (US-25). No es estático: vive en el contenedor
/// de dependencias y lo comparten el LoginForm y el formulario principal.
/// </summary>
public sealed class CurrentUser
{
    public int UsuarioId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Rol { get; private set; } = string.Empty;

    public bool Autenticado { get; private set; }
    public DateTime UltimaActividad { get; private set; }

    public bool EsAdmin => Autenticado && Rol == "admin";
    public bool EsSupervisor => Autenticado && Rol == "supervisor";
    public bool EsCliente => Autenticado && Rol == "cliente";

    public void Establecer(int usuarioId, string nombre, string email, string rol, DateTime ahora)
    {
        UsuarioId = usuarioId;
        Nombre = nombre;
        Email = email;
        Rol = rol;
        Autenticado = true;
        UltimaActividad = ahora;
    }

    public void RegistrarActividad(DateTime ahora) => UltimaActividad = ahora;

    public void CerrarSesion() => Autenticado = false;

    public bool ExpiradaPorInactividad(DateTime ahora, TimeSpan inactividadMaxima)
        => Autenticado && ahora - UltimaActividad > inactividadMaxima;
}
