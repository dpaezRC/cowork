using CoworkApp.Core.Dominio;

namespace CoworkApp.Core.Seguridad;

public sealed class SesionActual
{
    public int? IdUsuario { get; private set; }
    public string? Nombre { get; private set; }
    public string? Email { get; private set; }
    public string? Rol { get; private set; }

    public bool Autenticada => IdUsuario.HasValue;
    public bool EsAdministrador => Rol == Roles.Administrador;
    public bool EsCliente => Rol == Roles.Cliente;
    public bool EsEmpleado => Rol == Roles.Empleado;

    public void Iniciar(int idUsuario, string nombre, string email, string rol)
    {
        IdUsuario = idUsuario;
        Nombre = nombre;
        Email = email;
        Rol = rol;
    }

    public void Cerrar()
    {
        IdUsuario = null;
        Nombre = null;
        Email = null;
        Rol = null;
    }
}
