namespace CoworkApp.Features.H.Login;

/// <summary>
/// US-25 — lo que ve la UI después de autenticarse con éxito.
/// </summary>
public sealed record LoginRow(
    int UsuarioId,
    string Nombre,
    string Email,
    string Rol);
