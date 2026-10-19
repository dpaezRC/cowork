namespace CoworkApp.Core.Dominio;

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Cliente = "Cliente";
    public const string Empleado = "Empleado";

    public static readonly IReadOnlyList<string> Todos = [Administrador, Cliente, Empleado];
    public static readonly IReadOnlyList<string> Gestionables = [Cliente, Administrador];
}
