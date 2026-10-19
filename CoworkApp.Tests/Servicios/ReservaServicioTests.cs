using CoworkApp.Core.Modelos;
using CoworkApp.Core.Servicios;
using CoworkApp.Core.Validacion;
using CoworkApp.Tests.Fakes;
using Xunit;

namespace CoworkApp.Tests.Servicios;

public sealed class ReservaServicioTests
{
    private static readonly DateOnly Fecha = new(2030, 6, 16);
    private static readonly TimeOnly Inicio = new(9, 0);
    private static readonly TimeOnly Fin = new(11, 0);

    [Fact]
    public async Task BuscarDisponibilidad_DevuelveOficinaConCapacidadYCobertura()
    {
        var oficinas = new OficinaRepositorioFake();
        oficinas.Sembrar(1, 1, "Oficina 101", capacidad: 10, tarifa: 1000.00m);
        var reservas = new ReservaRepositorioFake();
        reservas.SembrarFranja(1, new TimeOnly(8, 0), new TimeOnly(21, 0));
        var servicio = CrearServicio(oficinas: oficinas, reservas: reservas);

        var disponibles = await servicio.BuscarDisponibilidadAsync(Fecha, Inicio, Fin, 4);

        var oficina = Assert.Single(disponibles);
        Assert.Equal(1, oficina.IdOficina);
        Assert.Equal(10, oficina.Disponibles);
        Assert.Equal(1000.00m, oficina.Tarifa);
    }

    [Fact]
    public async Task BuscarDisponibilidad_SinCobertura_ExcluyeOficina()
    {
        var oficinas = new OficinaRepositorioFake();
        oficinas.Sembrar(1, 1, "Oficina 101");
        var reservas = new ReservaRepositorioFake();
        var servicio = CrearServicio(oficinas: oficinas, reservas: reservas);

        var disponibles = await servicio.BuscarDisponibilidadAsync(Fecha, Inicio, Fin, 4);

        Assert.Empty(disponibles);
    }

    [Fact]
    public async Task BuscarDisponibilidad_DuracionMenorA60_LanzaRegla()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.BuscarDisponibilidadAsync(Fecha, new TimeOnly(9, 0), new TimeOnly(9, 30), 4));

        Assert.Equal(ReservaServicio.MensajeDuracionMinima, excepcion.Message);
    }

    [Fact]
    public async Task Crear_Exito_RegistraConfirmadaYAudita()
    {
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(2, "cliente@cowork.com", rol: "Cliente", estado: "Activo");
        var oficinas = new OficinaRepositorioFake();
        oficinas.Sembrar(1, 1, "Oficina 101", capacidad: 10, tarifa: 1000.00m);
        var reservas = new ReservaRepositorioFake();
        reservas.SembrarFranja(1, new TimeOnly(8, 0), new TimeOnly(21, 0));
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(usuarios, oficinas, reservas, auditoria);

        var reserva = await servicio.CrearAsync(2, 1, Fecha, Inicio, Fin, 4);

        Assert.Equal(1, reserva.IdReserva);
        Assert.Equal("Confirmada", reserva.Estado);
        Assert.Equal("Pendiente", reserva.EstadoPago);
        Assert.Equal(1000.00m, reserva.TarifaHoraAplicada);
        Assert.Equal(1, reservas.VecesAgregado);

        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Alta", registro.Accion);
        Assert.Equal("Reserva", registro.Entidad);
        Assert.Equal(1, registro.IdEntidad);
        Assert.Null(registro.IdUsuario);
        Assert.Equal(
            "IdUsuario=2; IdOficina=1; Fecha=2030-06-16; HoraInicio=09:00; HoraFin=11:00; CantidadAsientos=4; TarifaHoraAplicada=1000.00; Estado=Confirmada; EstadoPago=Pendiente",
            registro.Detalle);
    }

    [Fact]
    public async Task Crear_ClienteInexistente_LanzaNoExiste()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(99, 1, Fecha, Inicio, Fin, 4));

        Assert.Equal(ReservaServicio.MensajeClienteNoExiste, excepcion.Message);
    }

    [Fact]
    public async Task Crear_UsuarioNoCliente_LanzaSoloCliente()
    {
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(2, "empleado@cowork.com", rol: "Empleado", estado: "Activo");
        var servicio = CrearServicio(usuarios: usuarios);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(2, 1, Fecha, Inicio, Fin, 4));

        Assert.Equal(ReservaServicio.MensajeSoloCliente, excepcion.Message);
    }

    [Fact]
    public async Task Crear_OficinaNoHabilitada_LanzaNoHabilitada()
    {
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(2, "cliente@cowork.com", rol: "Cliente", estado: "Activo");
        var oficinas = new OficinaRepositorioFake();
        oficinas.Sembrar(1, 1, "Oficina 101", estado: "Deshabilitada");
        var servicio = CrearServicio(usuarios, oficinas);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(2, 1, Fecha, Inicio, Fin, 4));

        Assert.Equal(ReservaServicio.MensajeOficinaNoHabilitada, excepcion.Message);
    }

    [Fact]
    public async Task Crear_SinCobertura_LanzaSinCobertura()
    {
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(2, "cliente@cowork.com", rol: "Cliente", estado: "Activo");
        var oficinas = new OficinaRepositorioFake();
        oficinas.Sembrar(1, 1, "Oficina 101", capacidad: 10);
        var reservas = new ReservaRepositorioFake();
        var servicio = CrearServicio(usuarios, oficinas, reservas);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(2, 1, Fecha, Inicio, Fin, 4));

        Assert.Equal(ReservaServicio.MensajeSinCobertura, excepcion.Message);
    }

    [Fact]
    public async Task Crear_SinAsientosDisponibles_LanzaSinAsientos()
    {
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(2, "cliente@cowork.com", rol: "Cliente", estado: "Activo");
        var oficinas = new OficinaRepositorioFake();
        oficinas.Sembrar(1, 1, "Oficina 101", capacidad: 4);
        var reservas = new ReservaRepositorioFake();
        reservas.SembrarFranja(1, new TimeOnly(8, 0), new TimeOnly(21, 0));
        reservas.Sembrar(50, 3, 1, Fecha, Inicio, Fin, asientos: 4);
        var servicio = CrearServicio(usuarios, oficinas, reservas);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(2, 1, Fecha, Inicio, Fin, 4));

        Assert.Equal(ReservaServicio.MensajeSinAsientos, excepcion.Message);
    }

    [Fact]
    public async Task ListarClientes_FiltraSoloClientesActivos()
    {
        var usuarios = new UsuarioRepositorioFake();
        usuarios.Sembrar(1, "admin@cowork.com", rol: "Administrador", estado: "Activo");
        usuarios.Sembrar(2, "cliente@cowork.com", rol: "Cliente", estado: "Activo");
        usuarios.Sembrar(3, "inactivo@cowork.com", rol: "Cliente", estado: "Inactivo");
        var servicio = CrearServicio(usuarios: usuarios);

        var clientes = await servicio.ListarClientesAsync();

        var cliente = Assert.Single(clientes);
        Assert.Equal(2, cliente.IdUsuario);
    }

    [Fact]
    public async Task Cancelar_Exito_CancelaYAuditaModificacion()
    {
        var reservas = new ReservaRepositorioFake();
        reservas.Sembrar(10, 2, 1, Fecha, Inicio, Fin);
        var auditoria = new AuditoriaRepositorioFake();
        var reloj = new RelojServidorFake { Ahora = new DateTime(2030, 6, 16, 4, 0, 0) };
        var servicio = CrearServicio(reservas: reservas, auditoria: auditoria, reloj: reloj);

        var cancelada = await servicio.CancelarAsync(10, 2);

        Assert.True(cancelada);
        Assert.Equal(1, reservas.VecesCancelado);
        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Cancelacion", registro.Accion);
        Assert.Equal("Reserva", registro.Entidad);
        Assert.Equal("Estado: Confirmada -> Cancelada", registro.Detalle);
    }

    [Fact]
    public async Task Cancelar_FueraDePlazo_LanzaMensaje()
    {
        var reservas = new ReservaRepositorioFake();
        reservas.Sembrar(10, 2, 1, Fecha, Inicio, Fin);
        var reloj = new RelojServidorFake { Ahora = new DateTime(2030, 6, 16, 6, 0, 0) };
        var servicio = CrearServicio(reservas: reservas, reloj: reloj);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CancelarAsync(10, 2));

        Assert.Equal(ReservaServicio.MensajeFueraDePlazo, excepcion.Message);
        Assert.Equal(0, reservas.VecesCancelado);
    }

    [Fact]
    public async Task Cancelar_NoPertenece_LanzaRegla()
    {
        var reservas = new ReservaRepositorioFake();
        reservas.Sembrar(10, 2, 1, Fecha, Inicio, Fin);
        var servicio = CrearServicio(reservas: reservas);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CancelarAsync(10, 7));

        Assert.Equal(ReservaServicio.MensajeNoPertenece, excepcion.Message);
    }

    [Fact]
    public void ImporteEstimado_RedondeaHaciaArriba()
    {
        var importe = ReservaServicio.ImporteEstimado(4, 1000.00m, new TimeOnly(9, 0), new TimeOnly(10, 30));

        Assert.Equal(6000.00m, importe);
    }

    private static ReservaServicio CrearServicio(
        UsuarioRepositorioFake? usuarios = null,
        OficinaRepositorioFake? oficinas = null,
        ReservaRepositorioFake? reservas = null,
        AuditoriaRepositorioFake? auditoria = null,
        RelojServidorFake? reloj = null)
    {
        return new ReservaServicio(
            usuarios ?? new UsuarioRepositorioFake(),
            oficinas ?? new OficinaRepositorioFake(),
            reservas ?? new ReservaRepositorioFake(),
            auditoria ?? new AuditoriaRepositorioFake(),
            new TransaccionDbFake(),
            reloj ?? new RelojServidorFake());
    }
}
