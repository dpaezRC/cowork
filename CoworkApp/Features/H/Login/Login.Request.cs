namespace CoworkApp.Features.H.Login;

/// <summary>
/// US-25 — contrato de entrada del inicio de sesión.
/// </summary>
public sealed record LoginRequest(string Email, string Password);
