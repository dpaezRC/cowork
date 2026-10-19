namespace CoworkApp.Core.Dominio;

public static class Estados
{
    public const string Activo = "Activo";
    public const string Inactivo = "Inactivo";
    public const string Habilitada = "Habilitada";
    public const string Deshabilitada = "Deshabilitada";
    public const string Baja = "Baja";
    public const string Confirmada = "Confirmada";
    public const string EnCurso = "EnCurso";
    public const string Finalizada = "Finalizada";
    public const string Cancelada = "Cancelada";
    public const string NoPresentada = "NoPresentada";
    public const string Pendiente = "Pendiente";
    public const string Pagado = "Pagado";
    public const string DeudaPendiente = "DeudaPendiente";
    public const string SinCargo = "SinCargo";
    public const string Anulada = "Anulada";
    public const string Pagada = "Pagada";

    public static readonly IReadOnlyList<string> Edificio = [Activo, Inactivo];
    public static readonly IReadOnlyList<string> Piso = [Activo, Inactivo];
    public static readonly IReadOnlyList<string> Oficina = [Habilitada, Deshabilitada, Baja];
    public static readonly IReadOnlyList<string> Usuario = [Activo, Inactivo];
    public static readonly IReadOnlyList<string> Empleado = [Activo, Inactivo];
    public static readonly IReadOnlyList<string> Reserva = [Confirmada, EnCurso, Finalizada, Cancelada, NoPresentada];
    public static readonly IReadOnlyList<string> EstadoPago = [Pendiente, Pagado, DeudaPendiente, SinCargo];
    public static readonly IReadOnlyList<string> Deuda = [Pendiente, Anulada, Pagada];
}
