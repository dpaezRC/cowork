using CoworkApp.Core.Servicios;
using CoworkApp.Core.Validacion;
using CoworkApp.Tests.Fakes;
using Xunit;

namespace CoworkApp.Tests.Servicios;

public sealed class ControlOperativoServicioTests
{
    private static readonly DateOnly Fecha = new(2030, 6, 16);
    private static readonly TimeOnly Inicio = new(9, 0);
    private static readonly TimeOnly Fin = new(11, 0);

    [Fact]
    public async Task RegistrarIngreso_Exito_PoneEnCursoYAudita()
    {
        var escenario = CrearEscenario();
        escenario.Reloj.Ahora = new DateTime(2030, 6, 16, 9, 5, 0);
        escenario.Reservas.Sembrar(10, 5, 1, Fecha, Inicio, Fin);

        var resultado = await escenario.Servicio.RegistrarIngresoAsync(2, 10, new TimeOnly(9, 0));

        Assert.Equal(10, resultado.IdReserva);
        Assert.False(resultado.Correccion);
        Assert.False(resultado.DeudaAnulada);
        Assert.Equal("EnCurso", escenario.Reservas.Reservas[0].Estado);
        Assert.Equal(1, escenario.Usos.VecesAgregado);

        var registro = Assert.Single(escenario.Auditoria.Registros);
        Assert.Equal("RegistroIngreso", registro.Accion);
        Assert.Equal("ReservaUso", registro.Entidad);
        Assert.Equal(2, registro.IdUsuario);
        Assert.Equal("IdReserva=10; HoraIngreso=2030-06-16 09:00; IdEmpleado=1; Estado=EnCurso", registro.Detalle);
    }

    [Fact]
    public async Task RegistrarIngreso_Correccion_AnulaDeudaYReiniciaImporte()
    {
        var escenario = CrearEscenario();
        escenario.Reloj.Ahora = new DateTime(2030, 6, 17, 8, 0, 0);
        escenario.Reservas.Sembrar(10, 5, 1, Fecha, Inicio, Fin, estado: "NoPresentada", estadoPago: "DeudaPendiente", importe: 8000.00m);
        escenario.Deudas.Sembrar(10, 8000.00m);

        var resultado = await escenario.Servicio.RegistrarIngresoAsync(2, 10, new TimeOnly(9, 0));

        Assert.True(resultado.Correccion);
        Assert.True(resultado.DeudaAnulada);
        Assert.Equal("EnCurso", escenario.Reservas.Reservas[0].Estado);
        Assert.Equal("Pendiente", escenario.Reservas.Reservas[0].EstadoPago);
        Assert.Null(escenario.Reservas.Reservas[0].ImporteExigible);
        Assert.Equal("Anulada", escenario.Deudas.Deudas[10].Estado);

        var registro = Assert.Single(escenario.Auditoria.Registros);
        Assert.Equal("CorreccionAsistencia", registro.Accion);
    }

    [Fact]
    public async Task RegistrarIngreso_FueraDeAlcance_LanzaRegla()
    {
        var escenario = CrearEscenario();
        escenario.Reservas.Sembrar(10, 5, 2, Fecha, Inicio, Fin);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => escenario.Servicio.RegistrarIngresoAsync(2, 10, new TimeOnly(9, 0)));

        Assert.Equal(ControlOperativoServicio.MensajeFueraDeAlcance, excepcion.Message);
    }

    [Fact]
    public async Task RegistrarIngreso_AntesDeApertura_LanzaRegla()
    {
        var escenario = CrearEscenario();
        escenario.Reservas.Sembrar(10, 5, 1, Fecha, Inicio, Fin, apertura: new TimeOnly(8, 0));

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => escenario.Servicio.RegistrarIngresoAsync(2, 10, new TimeOnly(7, 30)));

        Assert.Contains("apertura", excepcion.Message);
    }

    [Fact]
    public async Task RegistrarSalida_Exito_CalculaImporteYFinaliza()
    {
        var escenario = CrearEscenario();
        escenario.Reservas.Sembrar(10, 5, 1, Fecha, Inicio, Fin, asientos: 4, tarifa: 1000.00m, estado: "EnCurso");
        escenario.Usos.Sembrar(10, new DateTime(2030, 6, 16, 9, 0, 0));

        var resultado = await escenario.Servicio.RegistrarSalidaAsync(2, 10, new TimeOnly(10, 30));

        Assert.Equal(8000.00m, resultado.Importe);
        Assert.Equal(120, resultado.Facturables);
        Assert.Equal("salida anticipada no reduce", resultado.Motivo);
        Assert.Equal("Finalizada", escenario.Reservas.Reservas[0].Estado);
        Assert.Equal(8000.00m, escenario.Reservas.Reservas[0].ImporteExigible);

        var registro = Assert.Single(escenario.Auditoria.Registros);
        Assert.Equal("RegistroSalida", registro.Accion);
        Assert.Equal("IdReserva=10; HoraSalida=2030-06-16 10:30; MinutosFacturables=120; ImporteExigible=8000.00; Estado=Finalizada", registro.Detalle);
    }

    [Fact]
    public async Task RegistrarSalida_SinIngreso_LanzaRegla()
    {
        var escenario = CrearEscenario();
        escenario.Reservas.Sembrar(10, 5, 1, Fecha, Inicio, Fin, estado: "EnCurso");

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => escenario.Servicio.RegistrarSalidaAsync(2, 10, new TimeOnly(10, 30)));

        Assert.Equal(ControlOperativoServicio.MensajeSinIngreso, excepcion.Message);
    }

    [Fact]
    public async Task RegistrarPago_Exito_MarcaPagadoYAudita()
    {
        var escenario = CrearEscenario();
        escenario.Reservas.Sembrar(10, 5, 1, Fecha, Inicio, Fin, estado: "Finalizada", estadoPago: "Pendiente", importe: 8000.00m);

        var resultado = await escenario.Servicio.RegistrarPagoAsync(2, 10, 8000.00m, "ref-1");

        Assert.Equal(8000.00m, resultado.Importe);
        Assert.False(resultado.DeudaPagada);
        Assert.Equal("Pagado", escenario.Reservas.Reservas[0].EstadoPago);
        Assert.Equal(1, escenario.Pagos.VecesAgregado);

        var registro = Assert.Single(escenario.Auditoria.Registros);
        Assert.Equal("RegistroPago", registro.Accion);
        Assert.Equal("Pago", registro.Entidad);
        Assert.Equal("IdReserva=10; Importe=8000.00; IdEmpleadoConfirma=1; Referencia=ref-1", registro.Detalle);
    }

    [Fact]
    public async Task RegistrarPago_ImporteDistinto_LanzaRegla()
    {
        var escenario = CrearEscenario();
        escenario.Reservas.Sembrar(10, 5, 1, Fecha, Inicio, Fin, estado: "Finalizada", estadoPago: "Pendiente", importe: 8000.00m);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => escenario.Servicio.RegistrarPagoAsync(2, 10, 5000.00m));

        Assert.Contains("8000.00", excepcion.Message);
    }

    [Fact]
    public async Task AbrirAgenda_BarridoMarcaNoPresentadaYListaFuturas()
    {
        var escenario = CrearEscenario();
        escenario.Reloj.Ahora = new DateTime(2030, 6, 16, 8, 0, 0);
        escenario.Reservas.Sembrar(10, 5, 1, new DateOnly(2030, 6, 10), Inicio, Fin, asientos: 4, tarifa: 1000.00m);
        escenario.Reservas.Sembrar(11, 5, 1, Fecha, new TimeOnly(12, 0), new TimeOnly(14, 0));

        var filas = await escenario.Servicio.AbrirAgendaAsync(2, Fecha);

        var reserva = Assert.Single(escenario.Reservas.Reservas, actual => actual.IdReserva == 10);
        Assert.Equal("NoPresentada", reserva.Estado);
        Assert.Equal("DeudaPendiente", reserva.EstadoPago);
        Assert.Equal(8000.00m, reserva.ImporteExigible);

        Assert.True(escenario.Deudas.Deudas.TryGetValue(10, out var creada));
        Assert.NotNull(creada);
        Assert.Equal(8000.00m, creada!.ImporteExigible);
        Assert.Equal("Pendiente", creada.Estado);

        var barrido = Assert.Single(escenario.Auditoria.Registros, registro => registro.Accion == "MarcadoInasistencia");
        Assert.Null(barrido.IdUsuario);
        Assert.Equal("IdReserva=10; ImporteExigible=8000.00; Estado=NoPresentada; EstadoPago=DeudaPendiente", barrido.Detalle);

        var fila = Assert.Single(filas);
        Assert.Equal(11, fila.IdReserva);
        Assert.Equal("Sin uso registrado", fila.Motivo);
    }

    private static Escenario CrearEscenario()
    {
        var empleados = new EmpleadoRepositorioFake();
        empleados.Sembrar(1, 2);
        var asignaciones = new EmpleadoOficinaRepositorioFake();
        asignaciones.Sembrar(1, 1, 1);
        var reservas = new ReservaRepositorioFake();
        var usos = new ReservaUsoRepositorioFake();
        var pagos = new PagoRepositorioFake();
        var deudas = new DeudaRepositorioFake();
        var auditoria = new AuditoriaRepositorioFake();
        var reloj = new RelojServidorFake();
        var servicio = new ControlOperativoServicio(
            empleados, asignaciones, reservas, usos, pagos, deudas, auditoria, new TransaccionDbFake(), reloj);
        return new Escenario(reservas, usos, pagos, deudas, auditoria, reloj, servicio);
    }

    private sealed record Escenario(
        ReservaRepositorioFake Reservas,
        ReservaUsoRepositorioFake Usos,
        PagoRepositorioFake Pagos,
        DeudaRepositorioFake Deudas,
        AuditoriaRepositorioFake Auditoria,
        RelojServidorFake Reloj,
        ControlOperativoServicio Servicio);
}
