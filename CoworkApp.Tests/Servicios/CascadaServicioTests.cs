using CoworkApp.Core.Modelos;
using CoworkApp.Core.Servicios;
using CoworkApp.Core.Validacion;
using CoworkApp.Tests.Fakes;
using Xunit;

namespace CoworkApp.Tests.Servicios;

public sealed class CascadaServicioTests
{
    [Fact]
    public async Task Registrar_TodoNuevo_CreaLosTresNivelesYAudita()
    {
        var edificios = new EdificioRepositorioFake();
        var pisos = new PisoRepositorioFake();
        var oficinas = new OficinaRepositorioFake();
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = new CascadaServicio(edificios, pisos, oficinas, auditoria, new TransaccionDbFake());

        var oficina = await servicio.RegistrarAsync(
            new EdificioCascada(null, "Central", "Av. Central 100"),
            new PisoCascada(null, "Piso 1"),
            new OficinaCascada("Oficina 101", 4, 1000.00m, new TimeOnly(8, 0), new TimeOnly(21, 0)));

        Assert.Equal(1, oficina.IdPiso);
        Assert.Equal("Habilitada", oficina.Estado);
        Assert.Equal(3, auditoria.Registros.Count);

        var altaEdificio = Assert.Single(auditoria.PorEntidad("Edificio"));
        Assert.Equal("Alta", altaEdificio.Accion);
        Assert.Equal("Nombre=Central; Direccion=Av. Central 100; Estado=Activo", altaEdificio.Detalle);

        var altaPiso = Assert.Single(auditoria.PorEntidad("Piso"));
        Assert.Equal("Alta", altaPiso.Accion);
        Assert.Equal("IdEdificio=1; Nombre=Piso 1; Estado=Activo", altaPiso.Detalle);

        var altaOficina = Assert.Single(auditoria.PorEntidad("Oficina"));
        Assert.Equal("Alta", altaOficina.Accion);
        Assert.Equal(
            "IdPiso=1; Nombre=Oficina 101; Capacidad=4; TarifaHora=1000.00; HoraApertura=08:00; HoraCierre=21:00; Estado=Habilitada",
            altaOficina.Detalle);
    }

    [Fact]
    public async Task Registrar_ReutilizaEdificioYPiso_NoLosVuelveACrear()
    {
        var edificios = new EdificioRepositorioFake();
        edificios.Sembrar(5, "Central", "Av. Central 100");
        var pisos = new PisoRepositorioFake();
        pisos.Sembrar(7, 5, "Piso 1");
        var oficinas = new OficinaRepositorioFake();
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = new CascadaServicio(edificios, pisos, oficinas, auditoria, new TransaccionDbFake());

        var oficina = await servicio.RegistrarAsync(
            new EdificioCascada(5, string.Empty, string.Empty),
            new PisoCascada(7, string.Empty),
            new OficinaCascada("Oficina 101", 4, 1000.00m, new TimeOnly(8, 0), new TimeOnly(21, 0)));

        Assert.Equal(7, oficina.IdPiso);
        Assert.Equal(0, edificios.VecesAgregado);
        Assert.Equal(0, pisos.VecesAgregado);
        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Oficina", registro.Entidad);
        Assert.Equal("Alta", registro.Accion);
    }

    [Fact]
    public async Task Registrar_OficinaDuplicada_LanzaMensajeGenerico()
    {
        var edificios = new EdificioRepositorioFake();
        edificios.Sembrar(5, "Central", "Av. Central 100");
        var pisos = new PisoRepositorioFake();
        pisos.Sembrar(7, 5, "Piso 1");
        var oficinas = new OficinaRepositorioFake { LanzarConflictoUnicidadEnAgregar = true };
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = new CascadaServicio(edificios, pisos, oficinas, auditoria, new TransaccionDbFake());

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.RegistrarAsync(
            new EdificioCascada(5, string.Empty, string.Empty),
            new PisoCascada(7, string.Empty),
            new OficinaCascada("Oficina 101", 4, 1000.00m, new TimeOnly(8, 0), new TimeOnly(21, 0))));

        Assert.Equal(CascadaServicio.MensajeDuplicado, excepcion.Message);
    }

    [Fact]
    public async Task Registrar_EdificioInexistente_LanzaEdificioNoExiste()
    {
        var oficinas = new OficinaRepositorioFake();
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = new CascadaServicio(
            new EdificioRepositorioFake(),
            new PisoRepositorioFake(),
            oficinas,
            auditoria,
            new TransaccionDbFake());

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.RegistrarAsync(
            new EdificioCascada(99, string.Empty, string.Empty),
            new PisoCascada(null, "Piso 1"),
            new OficinaCascada("Oficina 101", 4, 1000.00m, new TimeOnly(8, 0), new TimeOnly(21, 0))));

        Assert.Equal(CascadaServicio.MensajeEdificioNoExiste, excepcion.Message);
        Assert.Equal(0, oficinas.VecesAgregado);
        Assert.Empty(auditoria.Registros);
    }

    [Fact]
    public async Task Registrar_PisoInexistente_LanzaPisoNoExiste()
    {
        var edificios = new EdificioRepositorioFake();
        edificios.Sembrar(5, "Central", "Av. Central 100");
        var oficinas = new OficinaRepositorioFake();
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = new CascadaServicio(
            edificios,
            new PisoRepositorioFake(),
            oficinas,
            auditoria,
            new TransaccionDbFake());

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.RegistrarAsync(
            new EdificioCascada(5, string.Empty, string.Empty),
            new PisoCascada(99, string.Empty),
            new OficinaCascada("Oficina 101", 4, 1000.00m, new TimeOnly(8, 0), new TimeOnly(21, 0))));

        Assert.Equal(CascadaServicio.MensajePisoNoExiste, excepcion.Message);
        Assert.Equal(0, oficinas.VecesAgregado);
        Assert.Empty(auditoria.Registros);
    }
}
