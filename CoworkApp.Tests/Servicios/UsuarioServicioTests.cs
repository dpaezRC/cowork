using CoworkApp.Core.Dominio;
using CoworkApp.Core.Servicios;
using CoworkApp.Core.Validacion;
using CoworkApp.Tests.Fakes;
using Xunit;

namespace CoworkApp.Tests.Servicios;

public sealed class UsuarioServicioTests
{
    [Fact]
    public async Task Crear_Valida_RegistraYAuditaElAlta()
    {
        var usuarios = new UsuarioRepositorioFake();
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(usuarios: usuarios, auditoria: auditoria);

        var usuario = await servicio.CrearAsync("  Nombre  ", " Apellido ", "cliente@cowork.com", null, "secreta", Roles.Cliente);

        Assert.Equal(1, usuario.IdUsuario);
        Assert.Equal("Nombre", usuario.Nombre);
        Assert.Equal("Apellido", usuario.Apellido);
        Assert.Equal("cliente@cowork.com", usuario.Email);
        Assert.Null(usuario.Telefono);
        Assert.Equal(Roles.Cliente, usuario.Rol);
        Assert.Equal(Estados.Activo, usuario.Estado);

        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Alta", registro.Accion);
        Assert.Equal("Usuario", registro.Entidad);
        Assert.Equal(1, registro.IdEntidad);
        Assert.Null(registro.IdUsuario);
        Assert.Equal(
            "Email=cliente@cowork.com; Nombre=Nombre; Apellido=Apellido; Telefono=None; Rol=Cliente; Estado=Activo",
            registro.Detalle);
    }

    [Fact]
    public async Task Crear_ConTelefono_NormalizaYAuditaElValor()
    {
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(auditoria: auditoria);

        var usuario = await servicio.CrearAsync("Nombre", "Apellido", "admin@cowork.com", "  555-1234  ", "secreta", Roles.Administrador);

        Assert.Equal("555-1234", usuario.Telefono);
        Assert.Equal(
            "Email=admin@cowork.com; Nombre=Nombre; Apellido=Apellido; Telefono=555-1234; Rol=Administrador; Estado=Activo",
            auditoria.Registros[0].Detalle);
    }

    [Fact]
    public async Task Crear_SinNombre_LanzaObligatorio()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync("  ", "Apellido", "cliente@cowork.com", null, "secreta", Roles.Cliente));

        Assert.Equal(UsuarioServicio.MensajeNombreObligatorio, excepcion.Message);
    }

    [Fact]
    public async Task Crear_SinApellido_LanzaObligatorio()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync("Nombre", "  ", "cliente@cowork.com", null, "secreta", Roles.Cliente));

        Assert.Equal(UsuarioServicio.MensajeApellidoObligatorio, excepcion.Message);
    }

    [Fact]
    public async Task Crear_SinContrasena_LanzaObligatoria()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync("Nombre", "Apellido", "cliente@cowork.com", null, string.Empty, Roles.Cliente));

        Assert.Equal(UsuarioServicio.MensajeContrasenaObligatoria, excepcion.Message);
    }

    [Fact]
    public async Task Crear_EmailInvalido_LanzaEmail()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync("Nombre", "Apellido", "no-es-email", null, "secreta", Roles.Cliente));

        Assert.Equal(Validador.EmailInvalido, excepcion.Message);
    }

    [Fact]
    public async Task Crear_RolNoGestionable_LanzaRol()
    {
        var usuarios = new UsuarioRepositorioFake();
        var servicio = CrearServicio(usuarios: usuarios);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync("Nombre", "Apellido", "cliente@cowork.com", null, "secreta", "Supervisor"));

        Assert.Equal("Rol invalido. Valores permitidos: Cliente, Administrador.", excepcion.Message);
        Assert.Equal(0, usuarios.VecesAgregado);
    }

    [Fact]
    public async Task Crear_EmailDuplicado_LanzaDuplicadoSinRegistrar()
    {
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "cliente@cowork.com", rol: Roles.Cliente);
        var servicio = CrearServicio(usuarios: usuarios);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync("Nombre", "Apellido", "CLIENTE@cowork.com", null, "secreta", Roles.Cliente));

        Assert.Equal(UsuarioServicio.MensajeEmailDuplicado, excepcion.Message);
        Assert.Equal(0, usuarios.VecesAgregado);
    }

    [Fact]
    public async Task Crear_ConflictoUnicidad_LanzaDuplicado()
    {
        var usuarios = new UsuarioRepositorioFake { LanzarConflictoUnicidadEnAgregar = true };
        var servicio = CrearServicio(usuarios: usuarios);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync("Nombre", "Apellido", "cliente@cowork.com", null, "secreta", Roles.Cliente));

        Assert.Equal(UsuarioServicio.MensajeEmailDuplicado, excepcion.Message);
    }

    [Fact]
    public async Task Actualizar_ConCambios_DevuelveTrueYAudita()
    {
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "cliente@cowork.com", "Nombre", "Apellido", "123", Roles.Cliente);
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(usuarios: usuarios, auditoria: auditoria);

        var cambio = await servicio.ActualizarAsync(
            1,
            "Nombre",
            "Nuevo Apellido",
            "nuevo@cowork.com",
            "555",
            Estados.Inactivo,
            "nueva");

        Assert.True(cambio);
        Assert.Equal("Nuevo Apellido", usuarios.Usuarios[0].Apellido);
        Assert.Equal("nuevo@cowork.com", usuarios.Usuarios[0].Email);
        Assert.Equal("555", usuarios.Usuarios[0].Telefono);
        Assert.Equal(Estados.Inactivo, usuarios.Usuarios[0].Estado);
        Assert.Equal(1, usuarios.VecesActualizado);

        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Modificacion", registro.Accion);
        Assert.Equal("Usuario", registro.Entidad);
        Assert.Equal(1, registro.IdEntidad);
        Assert.Equal(
            "Apellido: Apellido -> Nuevo Apellido; Email: cliente@cowork.com -> nuevo@cowork.com; Telefono: 123 -> 555; Contrasena: (oculta) -> actualizada; Estado: Activo -> Inactivo",
            registro.Detalle);
    }

    [Fact]
    public async Task Actualizar_SinCambios_DevuelveFalse()
    {
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "cliente@cowork.com", "Nombre", "Apellido", "123", Roles.Cliente);
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(usuarios: usuarios, auditoria: auditoria);

        var cambio = await servicio.ActualizarAsync(1, "Nombre", "Apellido", "cliente@cowork.com", "123", Estados.Activo);

        Assert.False(cambio);
        Assert.Empty(auditoria.Registros);
        Assert.Equal(0, usuarios.VecesActualizado);
    }

    [Fact]
    public async Task Actualizar_TelefonoVacio_AuditaCambioANone()
    {
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "cliente@cowork.com", "Nombre", "Apellido", "123", Roles.Cliente);
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(usuarios: usuarios, auditoria: auditoria);

        var cambio = await servicio.ActualizarAsync(1, "Nombre", "Apellido", "cliente@cowork.com", "  ", Estados.Activo);

        Assert.True(cambio);
        Assert.Null(usuarios.Usuarios[0].Telefono);
        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Telefono: 123 -> None", registro.Detalle);
    }

    [Fact]
    public async Task Actualizar_RolEmpleado_LanzaMensajeRolEmpleado()
    {
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "empleado@cowork.com", "Nombre", "Apellido", null, Roles.Empleado);
        var servicio = CrearServicio(usuarios: usuarios);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(1, "Nombre", "Apellido", "empleado@cowork.com", null, Estados.Activo));

        Assert.Equal(UsuarioServicio.MensajeRolEmpleado, excepcion.Message);
    }

    [Fact]
    public async Task Actualizar_Inexistente_LanzaNoExiste()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(99, "Nombre", "Apellido", "cliente@cowork.com", null, Estados.Activo));

        Assert.Equal(UsuarioServicio.MensajeNoExiste, excepcion.Message);
    }

    [Fact]
    public async Task Actualizar_EmailDuplicado_LanzaDuplicado()
    {
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "cliente@cowork.com", "Nombre", "Apellido", null, Roles.Cliente);
        usuarios.Sembrar(2, "otro@cowork.com", "Otro", "Usuario", null, Roles.Administrador);
        var servicio = CrearServicio(usuarios: usuarios);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(1, "Nombre", "Apellido", "otro@cowork.com", null, Estados.Activo));

        Assert.Equal(UsuarioServicio.MensajeEmailDuplicado, excepcion.Message);
    }

    [Fact]
    public async Task Actualizar_EstadoInvalido_LanzaEstado()
    {
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "cliente@cowork.com", "Nombre", "Apellido", null, Roles.Cliente);
        var servicio = CrearServicio(usuarios: usuarios);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(1, "Nombre", "Apellido", "cliente@cowork.com", null, "Suspendido"));

        Assert.Equal("Estado invalido. Valores permitidos: Activo, Inactivo.", excepcion.Message);
    }

    [Fact]
    public async Task Actualizar_ConflictoUnicidad_LanzaDuplicado()
    {
        var usuarios = new UsuarioRepositorioFake { LanzarConflictoUnicidadEnActualizar = true };
        usuarios.Sembrar(1, "cliente@cowork.com", "Nombre", "Apellido", "123", Roles.Cliente);
        var servicio = CrearServicio(usuarios: usuarios);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(1, "Nombre", "Apellido", "nuevo@cowork.com", "123", Estados.Activo));

        Assert.Equal(UsuarioServicio.MensajeEmailDuplicado, excepcion.Message);
    }

    [Fact]
    public async Task DarDeBaja_Activo_DejaInactivoYAudita()
    {
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "cliente@cowork.com", "Nombre", "Apellido", null, Roles.Cliente, Estados.Activo);
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(usuarios: usuarios, auditoria: auditoria);

        var baja = await servicio.DarDeBajaAsync(1);

        Assert.True(baja);
        Assert.Equal(Estados.Inactivo, usuarios.Usuarios[0].Estado);
        Assert.Equal(1, usuarios.VecesActualizado);
        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Baja", registro.Accion);
        Assert.Equal("Usuario", registro.Entidad);
        Assert.Equal("Estado: Activo -> Inactivo", registro.Detalle);
    }

    [Fact]
    public async Task DarDeBaja_RolEmpleado_LanzaMensajeRolEmpleado()
    {
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "empleado@cowork.com", "Nombre", "Apellido", null, Roles.Empleado);
        var servicio = CrearServicio(usuarios: usuarios);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.DarDeBajaAsync(1));

        Assert.Equal(UsuarioServicio.MensajeRolEmpleado, excepcion.Message);
        Assert.Equal(0, usuarios.VecesActualizado);
    }

    [Fact]
    public async Task DarDeBaja_YaInactivo_DevuelveFalse()
    {
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "cliente@cowork.com", "Nombre", "Apellido", null, Roles.Cliente, Estados.Inactivo);
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(usuarios: usuarios, auditoria: auditoria);

        var baja = await servicio.DarDeBajaAsync(1);

        Assert.False(baja);
        Assert.Empty(auditoria.Registros);
        Assert.Equal(0, usuarios.VecesActualizado);
    }

    [Fact]
    public async Task DarDeBaja_Inexistente_LanzaNoExiste()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.DarDeBajaAsync(99));

        Assert.Equal(UsuarioServicio.MensajeNoExiste, excepcion.Message);
    }

    private static UsuarioServicio CrearServicio(
        UsuarioRepositorioFake? usuarios = null,
        AuditoriaRepositorioFake? auditoria = null)
    {
        return new UsuarioServicio(
            usuarios ?? new UsuarioRepositorioFake(),
            auditoria ?? new AuditoriaRepositorioFake(),
            new TransaccionDbFake());
    }
}
