using CoworkApp.Core.Dominio;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Seguridad;
using CoworkApp.Core.Servicios;
using CoworkApp.Core.Validacion;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CoworkApp.Tests.Servicios;

public sealed class AutenticacionServicioTests
{
    private const string Contrasena = "ClaveSegura123!";

    [Fact]
    public async Task CredencialesValidas_DevuelveElUsuario()
    {
        var usuario = CrearUsuario();
        var servicio = new AutenticacionServicio(new RepositorioFake(usuario));

        var autenticado = await servicio.AutenticarAsync("persona@cowork.local", Contrasena);

        Assert.Same(usuario, autenticado);
    }

    [Fact]
    public async Task ContrasenaIncorrecta_LanzaCredencialesInvalidas()
    {
        var servicio = new AutenticacionServicio(new RepositorioFake(CrearUsuario()));

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.AutenticarAsync("persona@cowork.local", "ClaveIncorrecta!"));

        Assert.Equal(AutenticacionServicio.MensajeCredencialesInvalidas, excepcion.Message);
    }

    [Fact]
    public async Task EmailInexistente_LanzaElMensajeGenerico()
    {
        var servicio = new AutenticacionServicio(new RepositorioFake(CrearUsuario()));

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.AutenticarAsync("nadie@cowork.local", Contrasena));

        Assert.Equal(AutenticacionServicio.MensajeCredencialesInvalidas, excepcion.Message);
    }

    [Fact]
    public async Task CuentaInactivaConCredencialesValidas_LanzaCuentaInactiva()
    {
        var servicio = new AutenticacionServicio(new RepositorioFake(CrearUsuario(estado: Estados.Inactivo)));

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.AutenticarAsync("persona@cowork.local", Contrasena));

        Assert.Equal(AutenticacionServicio.MensajeCuentaInactiva, excepcion.Message);
    }

    [Fact]
    public async Task CuentaInactivaConContrasenaIncorrecta_LanzaCredencialesInvalidas()
    {
        var servicio = new AutenticacionServicio(new RepositorioFake(CrearUsuario(estado: Estados.Inactivo)));

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.AutenticarAsync("persona@cowork.local", "ClaveIncorrecta!"));

        Assert.Equal(AutenticacionServicio.MensajeCredencialesInvalidas, excepcion.Message);
    }

    [Fact]
    public async Task EmailConMayusculasYEspacios_SeNormaliza()
    {
        var usuario = CrearUsuario();
        var servicio = new AutenticacionServicio(new RepositorioFake(usuario));

        var autenticado = await servicio.AutenticarAsync("  Persona@Cowork.Local  ", Contrasena);

        Assert.Same(usuario, autenticado);
    }

    [Fact]
    public async Task MensajesDeError_NoExponenElHash()
    {
        var usuario = CrearUsuario(estado: Estados.Inactivo);
        var servicio = new AutenticacionServicio(new RepositorioFake(usuario));

        var credenciales = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.AutenticarAsync("persona@cowork.local", "ClaveIncorrecta!"));
        var inactiva = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.AutenticarAsync("persona@cowork.local", Contrasena));

        Assert.DoesNotContain(usuario.HashContrasena, credenciales.Message);
        Assert.DoesNotContain(usuario.HashContrasena, inactiva.Message);
    }

    private static Usuario CrearUsuario(
        string email = "persona@cowork.local",
        string? estado = null,
        string contrasena = Contrasena,
        int idUsuario = 1)
    {
        return new Usuario
        {
            IdUsuario = idUsuario,
            Email = email,
            HashContrasena = PasswordHasher.Hash(contrasena),
            Nombre = "Persona",
            Apellido = "Demo",
            Rol = Roles.Cliente,
            Estado = estado ?? Estados.Activo,
            FechaAlta = new DateTime(2024, 1, 1)
        };
    }

    private sealed class RepositorioFake : IUsuarioRepositorio
    {
        private readonly List<Usuario> _usuarios;

        public RepositorioFake(params Usuario[] usuarios)
        {
            _usuarios = usuarios.ToList();
        }

        public Task<List<Usuario>> ListarAsync(CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }

        public Task<Usuario?> ObtenerAsync(
            int idUsuario,
            SqlConnection? conexion = null,
            SqlTransaction? transaccion = null,
            CancellationToken ct = default)
        {
            return Task.FromResult(_usuarios.FirstOrDefault(u => u.IdUsuario == idUsuario));
        }

        public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken ct = default)
        {
            var normalizado = email.Trim().ToLowerInvariant();
            var usuario = _usuarios.FirstOrDefault(u => u.Email.ToLowerInvariant() == normalizado);
            return Task.FromResult(usuario);
        }

        public Task<bool> EmailExisteAsync(
            string email,
            int? excluirId,
            SqlConnection? conexion = null,
            SqlTransaction? transaccion = null,
            CancellationToken ct = default)
        {
            var normalizado = email.Trim().ToLowerInvariant();
            var existe = _usuarios.Any(u => u.IdUsuario != excluirId && u.Email.ToLowerInvariant() == normalizado);
            return Task.FromResult(existe);
        }

        public Task<int> AgregarAsync(
            string email,
            string hashContrasena,
            string nombre,
            string apellido,
            string? telefono,
            string rol,
            string estado,
            SqlConnection? conexion = null,
            SqlTransaction? transaccion = null,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }

        public Task<int> ActualizarAsync(
            int idUsuario,
            string nombre,
            string apellido,
            string email,
            string? telefono,
            string estado,
            string? hashContrasena,
            SqlConnection? conexion = null,
            SqlTransaction? transaccion = null,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }
}
