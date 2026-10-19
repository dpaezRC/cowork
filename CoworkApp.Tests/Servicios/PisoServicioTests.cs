using CoworkApp.Core.Servicios;
using CoworkApp.Core.Validacion;
using CoworkApp.Tests.Fakes;
using Xunit;

namespace CoworkApp.Tests.Servicios;

public sealed class PisoServicioTests
{
    [Fact]
    public async Task Crear_Valido_RegistraYAuditaElAlta()
    {
        var edificios = new EdificioRepositorioFake();
        edificios.Sembrar(1, "Central", "Av. Central 100");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(edificios: edificios, auditoria: auditoria);

        var piso = await servicio.CrearAsync(1, "  Piso 1  ");

        Assert.True(piso.IdPiso > 0);
        Assert.Equal(1, piso.IdEdificio);
        Assert.Equal("Piso 1", piso.Nombre);
        Assert.Equal("Activo", piso.Estado);

        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Alta", registro.Accion);
        Assert.Equal("Piso", registro.Entidad);
        Assert.Equal("IdEdificio=1; Nombre=Piso 1; Estado=Activo", registro.Detalle);
    }

    [Fact]
    public async Task Crear_SinNombre_LanzaObligatorio()
    {
        var edificios = new EdificioRepositorioFake();
        edificios.Sembrar(1, "Central", "Av. Central 100");
        var servicio = CrearServicio(edificios: edificios);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearAsync(1, "  "));

        Assert.Equal(PisoServicio.MensajeNombreObligatorio, excepcion.Message);
    }

    [Fact]
    public async Task Crear_EdificioInexistente_LanzaEdificioNoExiste()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearAsync(99, "Piso 1"));

        Assert.Equal(PisoServicio.MensajeEdificioNoExiste, excepcion.Message);
    }

    [Fact]
    public async Task Crear_NombreDuplicadoEnElEdificio_LanzaDuplicado()
    {
        var edificios = new EdificioRepositorioFake();
        edificios.Sembrar(1, "Central", "Av. Central 100");
        var pisos = new PisoRepositorioFake();
        pisos.Sembrar(1, 1, "Piso 1");
        var servicio = CrearServicio(pisos, edificios);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearAsync(1, "piso 1"));

        Assert.Equal(PisoServicio.MensajeDuplicado, excepcion.Message);
        Assert.Equal(0, pisos.VecesAgregado);
    }

    [Fact]
    public async Task Actualizar_ConCambios_DevuelveTrueYAudita()
    {
        var pisos = new PisoRepositorioFake();
        pisos.Sembrar(1, 1, "Piso 1");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(pisos, auditoria: auditoria);

        var cambio = await servicio.ActualizarAsync(1, "Piso 2", "Inactivo");

        Assert.True(cambio);
        Assert.Equal("Piso 2", pisos.Pisos[0].Nombre);
        Assert.Equal(1, pisos.Pisos[0].IdEdificio);
        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Modificacion", registro.Accion);
        Assert.Equal("Nombre: Piso 1 -> Piso 2; Estado: Activo -> Inactivo", registro.Detalle);
    }

    [Fact]
    public async Task Actualizar_SinCambios_DevuelveFalse()
    {
        var pisos = new PisoRepositorioFake();
        pisos.Sembrar(1, 1, "Piso 1");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(pisos, auditoria: auditoria);

        var cambio = await servicio.ActualizarAsync(1, "Piso 1", "Activo");

        Assert.False(cambio);
        Assert.Empty(auditoria.Registros);
    }

    [Fact]
    public async Task Actualizar_Inexistente_LanzaNoExiste()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(99, "Piso 1", "Activo"));

        Assert.Equal(PisoServicio.MensajeNoExiste, excepcion.Message);
    }

    [Fact]
    public async Task Actualizar_NombreDuplicado_LanzaDuplicado()
    {
        var pisos = new PisoRepositorioFake();
        pisos.Sembrar(1, 1, "Piso 1");
        pisos.Sembrar(2, 1, "Piso 2");
        var servicio = CrearServicio(pisos);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(2, "piso 1", "Activo"));

        Assert.Equal(PisoServicio.MensajeDuplicado, excepcion.Message);
    }

    [Fact]
    public async Task DarDeBaja_Activo_DejaInactivoYAudita()
    {
        var pisos = new PisoRepositorioFake();
        pisos.Sembrar(1, 1, "Piso 1");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(pisos, auditoria: auditoria);

        var baja = await servicio.DarDeBajaAsync(1);

        Assert.True(baja);
        Assert.Equal("Inactivo", pisos.Pisos[0].Estado);
        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Baja", registro.Accion);
        Assert.Equal("Estado: Activo -> Inactivo", registro.Detalle);
    }

    [Fact]
    public async Task DarDeBaja_YaInactivo_DevuelveFalse()
    {
        var pisos = new PisoRepositorioFake();
        pisos.Sembrar(1, 1, "Piso 1", "Inactivo");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(pisos, auditoria: auditoria);

        var baja = await servicio.DarDeBajaAsync(1);

        Assert.False(baja);
        Assert.Empty(auditoria.Registros);
    }

    [Fact]
    public async Task DarDeBaja_Inexistente_LanzaNoExiste()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.DarDeBajaAsync(99));

        Assert.Equal(PisoServicio.MensajeNoExiste, excepcion.Message);
    }

    private static PisoServicio CrearServicio(
        PisoRepositorioFake? pisos = null,
        EdificioRepositorioFake? edificios = null,
        AuditoriaRepositorioFake? auditoria = null)
    {
        return new PisoServicio(
            pisos ?? new PisoRepositorioFake(),
            edificios ?? new EdificioRepositorioFake(),
            auditoria ?? new AuditoriaRepositorioFake(),
            new TransaccionDbFake());
    }
}
