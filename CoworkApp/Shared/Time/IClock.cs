namespace CoworkApp.Shared.Time;

/// <summary>
/// AGENTS.md regla 6: nada llama DateTime.Now directamente.
/// </summary>
public interface IClock
{
    DateTime Now { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}
