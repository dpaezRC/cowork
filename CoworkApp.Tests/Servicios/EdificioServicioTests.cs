using CoworkApp.Core.Datos;
using CoworkApp.Core.Servicios;
using CoworkApp.Core.Validacion;
using CoworkApp.Tests.Fakes;
using Xunit;

namespace CoworkApp.Tests.Servicios;

public sealed class EdificioServicioTests
{
    [Fact]
    public async Task Crear_Valido_RegistraYAuditaElAlta()
    {
        var repositorio = new EdificioRepositorioFake();
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(repositorio, auditoria);

        var edificio = await servicio.CrearAsync("  Central  ", "  Av. Central 100  ");

        Assert.True(edificio.IdEdificio > 0);
        Assert.Equal("Central", edificio.Nombre);
        Assert.Equal("Av. Central 100", edificio.Direccion);
        Assert.Equal("Activo", edificio.Estado);

        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Alta", registro.Accion);
        Assert.Equal("Edificio", registro.Entidad);
        Assert.Equal(edificio.IdEdificio, registro.IdEntidad);
        Assert.Equal("Nombre=Central; Direccion=Av. Central 100; Estado=Activo", registro.Detalle);
        Assert.Null(registro.IdUsuario);
    }

    [Fact]
    public async Task Crear_SinNombre_LanzaObligatorio()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearAsync("   ", "Calle 1"));

        Assert.Equal(EdificioServicio.MensajeNombreObligatorio, excepcion.Message);
    }

    [Fact]
    public async Task Crear_SinDireccion_LanzaObligatoria()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearAsync("Central", "   "));

        Assert.Equal(EdificioServicio.MensajeDireccionObligatoria, excepcion.Message);
    }

    [Fact]
    public async Task Crear_NombreDuplicadoSinDistinguirMayusculas_LanzaDuplicado()
    {
        var repositorio = new EdificioRepositorioFake();
        repositorio.Sembrar(1, "Central", "Av. Central 100");
        var servicio = CrearServicio(repositorio);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync("central", "Otra direccion"));

        Assert.Equal(EdificioServicio.MensajeDuplicado, excepcion.Message);
        Assert.Equal(0, repositorio.VecesAgregado);
    }

    [Fact]
    public async Task Actualizar_ConCambios_DevuelveTrueYAudita()
    {
        var repositorio = new EdificioRepositorioFake();
        repositorio.Sembrar(1, "Central", "Av. Central 100");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(repositorio, auditoria);

        var cambio = await servicio.ActualizarAsync(1, "Central Norte", "Av. Central 100", "Inactivo");

        Assert.True(cambio);
        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Modificacion", registro.Accion);
        Assert.Equal("Edificio", registro.Entidad);
        Assert.Equal("Nombre: Central -> Central Norte; Estado: Activo -> Inactivo", registro.Detalle);
    }

    [Fact]
    public async Task Actualizar_SinCambios_DevuelveFalseSinAuditar()
    {
        var repositorio = new EdificioRepositorioFake();
        repositorio.Sembrar(1, "Central", "Av. Central 100");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(repositorio, auditoria);

        var cambio = await servicio.ActualizarAsync(1, "Central", "Av. Central 100", "Activo");

        Assert.False(cambio);
        Assert.Empty(auditoria.Registros);
    }

    [Fact]
    public async Task Actualizar_Inexistente_LanzaNoExiste()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(99, "Central", "Calle 1", "Activo"));

        Assert.Equal(EdificioServicio.MensajeNoExiste, excepcion.Message);
    }

    [Fact]
    public async Task Actualizar_NombreDuplicado_LanzaDuplicado()
    {
        var repositorio = new EdificioRepositorioFake();
        repositorio.Sembrar(1, "Central", "Av. Central 100");
        repositorio.Sembrar(2, "Norte", "Av. Norte 200");
        var servicio = CrearServicio(repositorio);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(2, "central", "Av. Norte 200", "Activo"));

        Assert.Equal(EdificioServicio.MensajeDuplicado, excepcion.Message);
    }

    [Fact]
    public async Task DarDeBaja_Activo_DejaInactivoYAudita()
    {
        var repositorio = new EdificioRepositorioFake();
        repositorio.Sembrar(1, "Central", "Av. Central 100");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(repositorio, auditoria);

        var baja = await servicio.DarDeBajaAsync(1);

        Assert.True(baja);
        Assert.Equal("Inactivo", repositorio.Edificios[0].Estado);
        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Baja", registro.Accion);
        Assert.Equal("Estado: Activo -> Inactivo", registro.Detalle);
    }

    [Fact]
    public async Task DarDeBaja_YaInactivo_DevuelveFalse()
    {
        var repositorio = new EdificioRepositorioFake();
        repositorio.Sembrar(1, "Central", "Av. Central 100", "Inactivo");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(repositorio, auditoria);

        var baja = await servicio.DarDeBajaAsync(1);

        Assert.False(baja);
        Assert.Empty(auditoria.Registros);
    }

    [Fact]
    public async Task DarDeBaja_Inexistente_LanzaNoExiste()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.DarDeBajaAsync(99));

        Assert.Equal(EdificioServicio.MensajeNoExiste, excepcion.Message);
    }

    private static EdificioServicio CrearServicio(
        EdificioRepositorioFake? repositorio = null,
        AuditoriaRepositorioFake? auditoria = null)
    {
        return new EdificioServicio(
            repositorio ?? new EdificioRepositorioFake(),
            auditoria ?? new AuditoriaRepositorioFake(),
            new TransaccionDbFake());
    }
}
