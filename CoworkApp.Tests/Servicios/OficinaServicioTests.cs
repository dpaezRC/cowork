using CoworkApp.Core.Servicios;
using CoworkApp.Core.Validacion;
using CoworkApp.Tests.Fakes;
using Xunit;

namespace CoworkApp.Tests.Servicios;

public sealed class OficinaServicioTests
{
    private static readonly TimeOnly Apertura = new(8, 0);
    private static readonly TimeOnly Cierre = new(21, 0);

    [Fact]
    public async Task Crear_Valida_RegistraYAuditaElAlta()
    {
        var pisos = new PisoRepositorioFake();
        pisos.Sembrar(1, 1, "Piso 1");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(pisos: pisos, auditoria: auditoria);

        var oficina = await servicio.CrearAsync(1, "  Oficina 101  ", 4, 1000.00m, Apertura, Cierre);

        Assert.True(oficina.IdOficina > 0);
        Assert.Equal(1, oficina.IdPiso);
        Assert.Equal("Oficina 101", oficina.Nombre);
        Assert.Equal("Habilitada", oficina.Estado);

        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Alta", registro.Accion);
        Assert.Equal("Oficina", registro.Entidad);
        Assert.Equal(
            "IdPiso=1; Nombre=Oficina 101; Capacidad=4; TarifaHora=1000.00; HoraApertura=08:00; HoraCierre=21:00; Estado=Habilitada",
            registro.Detalle);
    }

    [Fact]
    public async Task Crear_SinNombre_LanzaObligatorio()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(1, "  ", 4, 1000.00m, Apertura, Cierre));

        Assert.Equal(OficinaServicio.MensajeNombreObligatorio, excepcion.Message);
    }

    [Fact]
    public async Task Crear_CapacidadNoPositiva_LanzaCapacidad()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(1, "Oficina 101", 0, 1000.00m, Apertura, Cierre));

        Assert.Equal(OficinaServicio.MensajeCapacidadPositiva, excepcion.Message);
    }

    [Fact]
    public async Task Crear_TarifaNegativa_LanzaTarifaNegativa()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(1, "Oficina 101", 4, -1.00m, Apertura, Cierre));

        Assert.Equal(OficinaServicio.MensajeTarifaNegativa, excepcion.Message);
    }

    [Fact]
    public async Task Crear_TarifaConMasDeDosDecimales_LanzaDecimales()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(1, "Oficina 101", 4, 1000.123m, Apertura, Cierre));

        Assert.Equal(OficinaServicio.MensajeTarifaDecimales, excepcion.Message);
    }

    [Fact]
    public async Task Crear_HorarioInvalido_LanzaHorario()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(1, "Oficina 101", 4, 1000.00m, Cierre, Apertura));

        Assert.Equal(OficinaServicio.MensajeHorarioInvalido, excepcion.Message);
    }

    [Fact]
    public async Task Crear_PisoInexistente_LanzaPisoNoExiste()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(99, "Oficina 101", 4, 1000.00m, Apertura, Cierre));

        Assert.Equal(OficinaServicio.MensajePisoNoExiste, excepcion.Message);
    }

    [Fact]
    public async Task Crear_NombreDuplicadoEnElPiso_LanzaDuplicado()
    {
        var pisos = new PisoRepositorioFake();
        pisos.Sembrar(1, 1, "Piso 1");
        var oficinas = new OficinaRepositorioFake();
        oficinas.Sembrar(1, 1, "Oficina 101");
        var servicio = CrearServicio(oficinas, pisos);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(1, "oficina 101", 4, 1000.00m, Apertura, Cierre));

        Assert.Equal(OficinaServicio.MensajeDuplicado, excepcion.Message);
        Assert.Equal(0, oficinas.VecesAgregado);
    }

    [Fact]
    public async Task Actualizar_ConCambios_DevuelveTrueYAudita()
    {
        var oficinas = new OficinaRepositorioFake();
        oficinas.Sembrar(1, 1, "Oficina 101");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(oficinas, auditoria: auditoria);

        var cambio = await servicio.ActualizarAsync(1, "Oficina 101", 6, 1500.50m, new TimeOnly(9, 0), new TimeOnly(18, 0), "Deshabilitada");

        Assert.True(cambio);
        Assert.Equal(1, oficinas.Oficinas[0].IdPiso);
        Assert.Equal(6, oficinas.Oficinas[0].Capacidad);
        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Modificacion", registro.Accion);
        Assert.Equal(
            "Capacidad: 4 -> 6; TarifaHora: 1000.00 -> 1500.50; HoraApertura: 08:00 -> 09:00; HoraCierre: 21:00 -> 18:00; Estado: Habilitada -> Deshabilitada",
            registro.Detalle);
    }

    [Fact]
    public async Task Actualizar_SinCambios_DevuelveFalse()
    {
        var oficinas = new OficinaRepositorioFake();
        oficinas.Sembrar(1, 1, "Oficina 101");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(oficinas, auditoria: auditoria);

        var cambio = await servicio.ActualizarAsync(1, "Oficina 101", 4, 1000.00m, Apertura, Cierre, "Habilitada");

        Assert.False(cambio);
        Assert.Empty(auditoria.Registros);
    }

    [Fact]
    public async Task Actualizar_Inexistente_LanzaNoExiste()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(99, "Oficina 101", 4, 1000.00m, Apertura, Cierre, "Habilitada"));

        Assert.Equal(OficinaServicio.MensajeNoExiste, excepcion.Message);
    }

    [Fact]
    public async Task DarDeBaja_Habilitada_DejaBajaYAudita()
    {
        var oficinas = new OficinaRepositorioFake();
        oficinas.Sembrar(1, 1, "Oficina 101");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(oficinas, auditoria: auditoria);

        var baja = await servicio.DarDeBajaAsync(1);

        Assert.True(baja);
        Assert.Equal("Baja", oficinas.Oficinas[0].Estado);
        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Baja", registro.Accion);
        Assert.Equal("Estado: Habilitada -> Baja", registro.Detalle);
    }

    [Fact]
    public async Task DarDeBaja_YaBaja_DevuelveFalse()
    {
        var oficinas = new OficinaRepositorioFake();
        oficinas.Sembrar(1, 1, "Oficina 101", estado: "Baja");
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(oficinas, auditoria: auditoria);

        var baja = await servicio.DarDeBajaAsync(1);

        Assert.False(baja);
        Assert.Empty(auditoria.Registros);
    }

    [Fact]
    public async Task DarDeBaja_Inexistente_LanzaNoExiste()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.DarDeBajaAsync(99));

        Assert.Equal(OficinaServicio.MensajeNoExiste, excepcion.Message);
    }

    private static OficinaServicio CrearServicio(
        OficinaRepositorioFake? oficinas = null,
        PisoRepositorioFake? pisos = null,
        AuditoriaRepositorioFake? auditoria = null)
    {
        return new OficinaServicio(
            oficinas ?? new OficinaRepositorioFake(),
            pisos ?? new PisoRepositorioFake(),
            auditoria ?? new AuditoriaRepositorioFake(),
            new TransaccionDbFake());
    }
}
