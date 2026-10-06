namespace CoworkApp.Shared.Results;

/// <summary>
/// Resultado de una operación con error de negocio. El Handler devuelve esto
/// en vez de lanzar excepciones ni dialogar con el usuario (AGENTS.md regla 3).
/// </summary>
public sealed record Result<T>
{
    private Result(T? value, string? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(string error) => new(default, error);
}
