using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Tests.Fakes;

public sealed class UsuarioRepositorioFake : IUsuarioRepositorio
{
    private readonly List<Usuario> _usuarios = new();
    private int _siguienteId = 1;

    public IReadOnlyList<Usuario> Usuarios => _usuarios;

    public int VecesAgregado { get; private set; }

    public int VecesActualizado { get; private set; }

    public bool LanzarConflictoUnicidadEnAgregar { get; set; }

    public bool LanzarConflictoUnicidadEnActualizar { get; set; }

    public Usuario Sembrar(
        int id,
        string email,
        string nombre = "Nombre",
        string apellido = "Apellido",
        string? telefono = null,
        string rol = "Empleado",
        string estado = "Activo")
    {
        var usuario = new Usuario
        {
            IdUsuario = id,
            Email = email,
            HashContrasena = "hash",
            Nombre = nombre,
            Apellido = apellido,
            Telefono = telefono,
            Rol = rol,
            Estado = estado,
            FechaAlta = new DateTime(2024, 1, 1)
        };
        _usuarios.Add(usuario);
        _siguienteId = Math.Max(_siguienteId, id + 1);
        return usuario;
    }

    public Task<List<Usuario>> ListarAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_usuarios.OrderBy(usuario => usuario.IdUsuario).ToList());
    }

    public Task<Usuario?> ObtenerAsync(
        int idUsuario,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_usuarios.FirstOrDefault(usuario => usuario.IdUsuario == idUsuario));
    }

    public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken ct = default)
    {
        var buscado = normalizar(email);
        return Task.FromResult(_usuarios.FirstOrDefault(usuario => normalizar(usuario.Email) == buscado));
    }

    public Task<bool> EmailExisteAsync(
        string email,
        int? excluirId,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var buscado = normalizar(email);
        var existe = _usuarios.Any(usuario =>
            usuario.IdUsuario != excluirId &&
            normalizar(usuario.Email) == buscado);
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
        if (LanzarConflictoUnicidadEnAgregar)
        {
            throw new ConflictoUnicidadException();
        }

        VecesAgregado++;
        var id = _siguienteId++;
        _usuarios.Add(new Usuario
        {
            IdUsuario = id,
            Email = email,
            HashContrasena = hashContrasena,
            Nombre = nombre,
            Apellido = apellido,
            Telefono = telefono,
            Rol = rol,
            Estado = estado,
            FechaAlta = new DateTime(2024, 1, 1)
        });
        return Task.FromResult(id);
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
        if (LanzarConflictoUnicidadEnActualizar)
        {
            throw new ConflictoUnicidadException();
        }

        var usuario = _usuarios.First(actual => actual.IdUsuario == idUsuario);
        usuario.Nombre = nombre;
        usuario.Apellido = apellido;
        usuario.Email = email;
        usuario.Telefono = telefono;
        usuario.Estado = estado;
        if (hashContrasena is not null)
        {
            usuario.HashContrasena = hashContrasena;
        }

        VecesActualizado++;
        return Task.FromResult(1);
    }

    private static string normalizar(string valor) => valor.Trim().ToLowerInvariant();
}
