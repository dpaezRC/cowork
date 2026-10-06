namespace CoworkApp.Shared.Configuration;

public sealed record SesionOptions(int MinutosInactividad)
{
    public const int MinutosPorDefecto = 30;
}
