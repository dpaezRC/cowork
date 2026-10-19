using CoworkApp.Core.Dominio;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Seguridad;
using CoworkApp.Core.Validacion;

namespace CoworkApp.Core.Servicios;

public sealed class AutenticacionServicio
{
    public const string MensajeCredencialesInvalidas = "Credenciales invalidas. Verifica tu email y contrasena.";
    public const string MensajeCuentaInactiva = "La cuenta esta inactiva. Contacta al administrador.";

    private readonly IUsuarioRepositorio _repositorio;

    public AutenticacionServicio(IUsuarioRepositorio repositorio)
    {
        _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
    }

    public async Task<Usuario> AutenticarAsync(string email, string contrasena, CancellationToken ct = default)
    {
        var usuario = await _repositorio.BuscarPorEmailAsync(email, ct).ConfigureAwait(false);

        if (usuario is null || !PasswordHasher.Verify(contrasena, usuario.HashContrasena))
        {
            throw new ReglaNegocioException(MensajeCredencialesInvalidas);
        }

        if (usuario.Estado != Estados.Activo)
        {
            throw new ReglaNegocioException(MensajeCuentaInactiva);
        }

        return usuario;
    }
}
