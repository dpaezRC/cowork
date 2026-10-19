namespace CoworkApp.Core.Dominio;

public enum Modulo
{
    Espacios,
    Personal,
    Usuarios,
    Reservas,
    ControlOperativo
}

public sealed record DefinicionModulo(Modulo Modulo, string Descripcion, IReadOnlyList<string> Roles);
