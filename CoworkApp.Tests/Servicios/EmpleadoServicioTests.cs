using CoworkApp.Core.Servicios;
using CoworkApp.Core.Validacion;
using CoworkApp.Tests.Fakes;
using Xunit;

namespace CoworkApp.Tests.Servicios;

public sealed class EmpleadoServicioTests
{
    [Fact]
    public async Task Crear_Valida_RegistraYAuditaElAlta()
    {
        var edificios = new EdificioRepositorioFake();
        edificios.Sembrar(1, "Central", "Calle 1");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(edificios: edificios, auditoria: auditoria);

        var empleado = await servicio.CrearAsync("  Nombre  ", " Apellido ", "empleado@cowork.com", null, "secreta", 1);

        Assert.Equal(1, empleado.IdEmpleado);
        Assert.Equal(1, empleado.IdUsuario);
        Assert.Equal("Nombre", empleado.Nombre);
        Assert.Equal("Apellido", empleado.Apellido);
        Assert.Equal("empleado@cowork.com", empleado.Email);
        Assert.Null(empleado.Telefono);
        Assert.Equal("Central", empleado.Edificio);
        Assert.Equal("Activo", empleado.Estado);

        Assert.Equal(2, auditoria.Registros.Count);
        var altaUsuario = auditoria.Registros[0];
        Assert.Equal("Alta", altaUsuario.Accion);
        Assert.Equal("Usuario", altaUsuario.Entidad);
        Assert.Equal(1, altaUsuario.IdEntidad);
        Assert.Null(altaUsuario.IdUsuario);
        Assert.Equal(
            "Email=empleado@cowork.com; Nombre=Nombre; Apellido=Apellido; Telefono=None; Rol=Empleado; Estado=Activo",
            altaUsuario.Detalle);

        var altaEmpleado = auditoria.Registros[1];
        Assert.Equal("Alta", altaEmpleado.Accion);
        Assert.Equal("Empleado", altaEmpleado.Entidad);
        Assert.Equal(1, altaEmpleado.IdEntidad);
        Assert.Equal("IdUsuario=1; IdEdificio=1; Estado=Activo", altaEmpleado.Detalle);
    }

    [Fact]
    public async Task Crear_ConTelefono_NormalizaYAuditaElValor()
    {
        var edificios = new EdificioRepositorioFake();
        edificios.Sembrar(1, "Central", "Calle 1");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(edificios: edificios, auditoria: auditoria);

        var empleado = await servicio.CrearAsync("Nombre", "Apellido", "empleado@cowork.com", "  555-1234  ", "secreta", 1);

        Assert.Equal("555-1234", empleado.Telefono);
        Assert.Equal(
            "Email=empleado@cowork.com; Nombre=Nombre; Apellido=Apellido; Telefono=555-1234; Rol=Empleado; Estado=Activo",
            auditoria.Registros[0].Detalle);
    }

    [Fact]
    public async Task Crear_SinNombre_LanzaObligatorio()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync("  ", "Apellido", "empleado@cowork.com", null, "secreta", 1));

        Assert.Equal(EmpleadoServicio.MensajeNombreObligatorio, excepcion.Message);
    }

    [Fact]
    public async Task Crear_SinApellido_LanzaObligatorio()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync("Nombre", "  ", "empleado@cowork.com", null, "secreta", 1));

        Assert.Equal(EmpleadoServicio.MensajeApellidoObligatorio, excepcion.Message);
    }

    [Fact]
    public async Task Crear_SinContrasena_LanzaObligatoria()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync("Nombre", "Apellido", "empleado@cowork.com", null, string.Empty, 1));

        Assert.Equal(EmpleadoServicio.MensajeContrasenaObligatoria, excepcion.Message);
    }

    [Fact]
    public async Task Crear_EmailInvalido_LanzaEmail()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync("Nombre", "Apellido", "no-es-email", null, "secreta", 1));

        Assert.Equal(Validador.EmailInvalido, excepcion.Message);
    }

    [Fact]
    public async Task Crear_EdificioInexistente_LanzaEdificioNoExiste()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync("Nombre", "Apellido", "empleado@cowork.com", null, "secreta", 99));

        Assert.Equal(EmpleadoServicio.MensajeEdificioNoExiste, excepcion.Message);
    }

    [Fact]
    public async Task Crear_EmailDuplicado_LanzaDuplicadoSinRegistrar()
    {
        var edificios = new EdificioRepositorioFake();
        edificios.Sembrar(1, "Central", "Calle 1");
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "empleado@cowork.com");
        var empleados = new EmpleadoRepositorioFake();
        var servicio = CrearServicio(empleados: empleados, usuarios: usuarios, edificios: edificios);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync("Nombre", "Apellido", "EMPLEADO@cowork.com", null, "secreta", 1));

        Assert.Equal(EmpleadoServicio.MensajeEmailDuplicado, excepcion.Message);
        Assert.Equal(0, empleados.VecesAgregado);
        Assert.Equal(0, usuarios.VecesAgregado);
    }

    [Fact]
    public async Task Crear_ConflictoUnicidad_LanzaDuplicado()
    {
        var edificios = new EdificioRepositorioFake();
        edificios.Sembrar(1, "Central", "Calle 1");
        var usuarios = new UsuarioRepositorioFake { LanzarConflictoUnicidadEnAgregar = true };
        var servicio = CrearServicio(usuarios: usuarios, edificios: edificios);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync("Nombre", "Apellido", "empleado@cowork.com", null, "secreta", 1));

        Assert.Equal(EmpleadoServicio.MensajeEmailDuplicado, excepcion.Message);
    }

    [Fact]
    public async Task Actualizar_ConCambios_DevuelveTrueYAudita()
    {
        var empleados = new EmpleadoRepositorioFake();
        empleados.Sembrar(1, 1, "Nombre", "Apellido", "empleado@cowork.com", "123", 1, "Central");
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "empleado@cowork.com", "Nombre", "Apellido", "123");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(empleados: empleados, usuarios: usuarios, auditoria: auditoria);

        var cambio = await servicio.ActualizarAsync(
            1,
            "Nombre",
            "Nuevo Apellido",
            "nuevo@cowork.com",
            "555",
            2,
            "Inactivo",
            "nueva");

        Assert.True(cambio);
        Assert.Equal("Inactivo", empleados.Empleados[0].Estado);
        Assert.Equal(2, empleados.Empleados[0].IdEdificio);
        Assert.Equal("Inactivo", usuarios.Usuarios[0].Estado);
        Assert.Equal(1, empleados.VecesActualizado);
        Assert.Equal(1, usuarios.VecesActualizado);

        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Modificacion", registro.Accion);
        Assert.Equal("Empleado", registro.Entidad);
        Assert.Equal(1, registro.IdEntidad);
        Assert.Equal(
            "Apellido: Apellido -> Nuevo Apellido; Email: empleado@cowork.com -> nuevo@cowork.com; Telefono: 123 -> 555; Contrasena: (oculta) -> actualizada; IdEdificio: 1 -> 2; Estado: Activo -> Inactivo",
            registro.Detalle);
    }

    [Fact]
    public async Task Actualizar_SinCambios_DevuelveFalse()
    {
        var empleados = new EmpleadoRepositorioFake();
        empleados.Sembrar(1, 1, "Nombre", "Apellido", "empleado@cowork.com", "123", 1, "Central");
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "empleado@cowork.com", "Nombre", "Apellido", "123");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(empleados: empleados, usuarios: usuarios, auditoria: auditoria);

        var cambio = await servicio.ActualizarAsync(1, "Nombre", "Apellido", "empleado@cowork.com", "123", 1, "Activo");

        Assert.False(cambio);
        Assert.Empty(auditoria.Registros);
        Assert.Equal(0, empleados.VecesActualizado);
        Assert.Equal(0, usuarios.VecesActualizado);
    }

    [Fact]
    public async Task Actualizar_TelefonoVacio_AuditaCambioANone()
    {
        var empleados = new EmpleadoRepositorioFake();
        empleados.Sembrar(1, 1, "Nombre", "Apellido", "empleado@cowork.com", "123", 1, "Central");
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "empleado@cowork.com", "Nombre", "Apellido", "123");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(empleados: empleados, usuarios: usuarios, auditoria: auditoria);

        var cambio = await servicio.ActualizarAsync(1, "Nombre", "Apellido", "empleado@cowork.com", "  ", 1, "Activo");

        Assert.True(cambio);
        Assert.Null(usuarios.Usuarios[0].Telefono);
        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Telefono: 123 -> None", registro.Detalle);
    }

    [Fact]
    public async Task Actualizar_EdificioConAsignaciones_LanzaRegla()
    {
        var empleados = new EmpleadoRepositorioFake();
        empleados.Sembrar(1, 1, "Nombre", "Apellido", "empleado@cowork.com", null, 1, "Central");
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "empleado@cowork.com", "Nombre", "Apellido");
        var asignaciones = new EmpleadoOficinaRepositorioFake();
        asignaciones.Sembrar(1, 1, 1);
        var servicio = CrearServicio(empleados: empleados, usuarios: usuarios, asignaciones: asignaciones);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(1, "Nombre", "Apellido", "empleado@cowork.com", null, 2, "Activo"));

        Assert.Equal(EmpleadoServicio.MensajeEdificioConAsignaciones, excepcion.Message);
    }

    [Fact]
    public async Task Actualizar_Inexistente_LanzaNoExiste()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(99, "Nombre", "Apellido", "empleado@cowork.com", null, 1, "Activo"));

        Assert.Equal(EmpleadoServicio.MensajeNoExiste, excepcion.Message);
    }

    [Fact]
    public async Task Actualizar_EmailDuplicado_LanzaDuplicado()
    {
        var empleados = new EmpleadoRepositorioFake();
        empleados.Sembrar(1, 1, "Nombre", "Apellido", "empleado@cowork.com", null, 1, "Central");
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "empleado@cowork.com", "Nombre", "Apellido");
        usuarios.Sembrar(2, "otro@cowork.com", "Otro", "Usuario");
        var servicio = CrearServicio(empleados: empleados, usuarios: usuarios);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(1, "Nombre", "Apellido", "otro@cowork.com", null, 1, "Activo"));

        Assert.Equal(EmpleadoServicio.MensajeEmailDuplicado, excepcion.Message);
    }

    [Fact]
    public async Task Actualizar_EstadoInvalido_LanzaEstado()
    {
        var empleados = new EmpleadoRepositorioFake();
        empleados.Sembrar(1, 1, "Nombre", "Apellido", "empleado@cowork.com", null, 1, "Central");
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "empleado@cowork.com", "Nombre", "Apellido");
        var servicio = CrearServicio(empleados: empleados, usuarios: usuarios);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(1, "Nombre", "Apellido", "empleado@cowork.com", null, 1, "Suspendido"));

        Assert.Equal("Estado invalido. Valores permitidos: Activo, Inactivo.", excepcion.Message);
    }

    [Fact]
    public async Task DarDeBaja_Activo_DejaInactivoYAudita()
    {
        var empleados = new EmpleadoRepositorioFake();
        empleados.Sembrar(1, 1, "Nombre", "Apellido", "empleado@cowork.com", null, 1, "Central", "Activo");
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "empleado@cowork.com", "Nombre", "Apellido", estado: "Activo");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(empleados: empleados, usuarios: usuarios, auditoria: auditoria);

        var baja = await servicio.DarDeBajaAsync(1);

        Assert.True(baja);
        Assert.Equal("Inactivo", empleados.Empleados[0].Estado);
        Assert.Equal("Inactivo", usuarios.Usuarios[0].Estado);
        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Baja", registro.Accion);
        Assert.Equal("Empleado", registro.Entidad);
        Assert.Equal("Estado: Activo -> Inactivo", registro.Detalle);
    }

    [Fact]
    public async Task DarDeBaja_YaInactivo_DevuelveFalse()
    {
        var empleados = new EmpleadoRepositorioFake();
        empleados.Sembrar(1, 1, "Nombre", "Apellido", "empleado@cowork.com", null, 1, "Central", "Inactivo");
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "empleado@cowork.com", "Nombre", "Apellido", estado: "Inactivo");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(empleados: empleados, usuarios: usuarios, auditoria: auditoria);

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

        Assert.Equal(EmpleadoServicio.MensajeNoExiste, excepcion.Message);
    }

    private static EmpleadoServicio CrearServicio(
        EmpleadoRepositorioFake? empleados = null,
        UsuarioRepositorioFake? usuarios = null,
        EdificioRepositorioFake? edificios = null,
        EmpleadoOficinaRepositorioFake? asignaciones = null,
        AuditoriaRepositorioFake? auditoria = null)
    {
        return new EmpleadoServicio(
            empleados ?? new EmpleadoRepositorioFake(),
            usuarios ?? new UsuarioRepositorioFake(),
            edificios ?? new EdificioRepositorioFake(),
            asignaciones ?? new EmpleadoOficinaRepositorioFake(),
            auditoria ?? new AuditoriaRepositorioFake(),
            new TransaccionDbFake());
    }
}
