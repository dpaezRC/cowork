using CoworkApp.Core.Dominio;
using CoworkApp.Core.Validacion;

namespace CoworkApp.Core.Servicios;

public static class MenuServicio
{
    public const string MensajeSinPermisos = "No tenes permisos para acceder a ese modulo.";

    private static readonly IReadOnlyList<DefinicionModulo> Todos =
    [
        new(Modulo.Espacios, "Gestion de espacios", [Roles.Administrador]),
        new(Modulo.Personal, "Gestion de personal", [Roles.Administrador]),
        new(Modulo.Usuarios, "Gestion de usuarios", [Roles.Administrador]),
        new(Modulo.Reservas, "Gestion de reservas", [Roles.Administrador, Roles.Cliente]),
        new(Modulo.ControlOperativo, "Control operativo", [Roles.Empleado])
    ];

    public static IReadOnlyList<DefinicionModulo> ObtenerModulos(string? rol)
    {
        var rolBuscado = rol ?? string.Empty;
        return Todos.Where(modulo => modulo.Roles.Contains(rolBuscado)).ToList();
    }

    public static bool PuedeAcceder(string? rol, Modulo modulo)
    {
        var rolBuscado = rol ?? string.Empty;
        return Todos.Any(definicion => definicion.Modulo == modulo && definicion.Roles.Contains(rolBuscado));
    }

    public static void ValidarAcceso(string? rol, Modulo modulo)
    {
        if (!PuedeAcceder(rol, modulo))
        {
            throw new ReglaNegocioException(MensajeSinPermisos);
        }
    }
}
