using CoworkApp.Core.Servicios;
using CoworkApp.Core.Validacion;
using CoworkApp.Tests.Fakes;
using Xunit;

namespace CoworkApp.Tests.Servicios;

public sealed class AsignacionServicioTests
{
    private static readonly TimeOnly Inicio = new(9, 0);
    private static readonly TimeOnly Fin = new(17, 0);

    [Fact]
    public async Task Asignar_Valida_RegistraYAuditaElAlta()
    {
        var empleados = new EmpleadoRepositorioFake();
        empleados.Sembrar(1, 1, idEdificio: 1);
        var oficinas = new OficinaRepositorioFake();
        oficinas.Sembrar(1, 1, "Oficina 101");
        var pisos = new PisoRepositorioFake();
        pisos.Sembrar(1, 1, "Piso 1");
        var asignaciones = new EmpleadoOficinaRepositorioFake();
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(empleados, oficinas, pisos, asignaciones, auditoria);

        var asignacion = await servicio.AsignarAsync(1, 1, Inicio, Fin);

        Assert.Equal(1, asignacion.IdEmpleadoOficina);
        Assert.Equal(1, asignacion.IdEmpleado);
        Assert.Equal(1, asignacion.IdOficina);
        Assert.Equal(Inicio, asignacion.HoraInicio);
        Assert.Equal(Fin, asignacion.HoraFin);
        Assert.Equal(1, asignaciones.VecesAgregado);

        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Alta", registro.Accion);
        Assert.Equal("EmpleadoOficina", registro.Entidad);
        Assert.Equal(1, registro.IdEntidad);
        Assert.Null(registro.IdUsuario);
        Assert.Equal("IdEmpleado=1; IdOficina=1; HoraInicio=09:00; HoraFin=17:00", registro.Detalle);
    }

    [Fact]
    public async Task Asignar_HorarioInvalido_LanzaHorario()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.AsignarAsync(1, 1, Fin, Inicio));

        Assert.Equal(AsignacionServicio.MensajeHorarioInvalido, excepcion.Message);
    }

    [Fact]
    public async Task Asignar_EmpleadoInexistente_LanzaEmpleadoNoExiste()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.AsignarAsync(99, 1, Inicio, Fin));

        Assert.Equal(AsignacionServicio.MensajeEmpleadoNoExiste, excepcion.Message);
    }

    [Fact]
    public async Task Asignar_OficinaInexistente_LanzaOficinaNoExiste()
    {
        var empleados = new EmpleadoRepositorioFake();
        empleados.Sembrar(1, 1, idEdificio: 1);
        var servicio = CrearServicio(empleados: empleados);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.AsignarAsync(1, 99, Inicio, Fin));

        Assert.Equal(AsignacionServicio.MensajeOficinaNoExiste, excepcion.Message);
    }

    [Fact]
    public async Task Asignar_OficinaDeOtroEdificio_LanzaRegla()
    {
        var empleados = new EmpleadoRepositorioFake();
        empleados.Sembrar(1, 1, idEdificio: 1);
        var oficinas = new OficinaRepositorioFake();
        oficinas.Sembrar(1, 2, "Oficina 201");
        var pisos = new PisoRepositorioFake();
        pisos.Sembrar(2, 5, "Piso 1");
        var servicio = CrearServicio(empleados: empleados, oficinas: oficinas, pisos: pisos);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.AsignarAsync(1, 1, Inicio, Fin));

        Assert.Equal(AsignacionServicio.MensajeOficinaOtroEdificio, excepcion.Message);
    }

    [Fact]
    public async Task Asignar_FranjaDuplicada_LanzaDuplicada()
    {
        var empleados = new EmpleadoRepositorioFake();
        empleados.Sembrar(1, 1, idEdificio: 1);
        var oficinas = new OficinaRepositorioFake();
        oficinas.Sembrar(1, 1, "Oficina 101");
        var pisos = new PisoRepositorioFake();
        pisos.Sembrar(1, 1, "Piso 1");
        var asignaciones = new EmpleadoOficinaRepositorioFake();
        asignaciones.Sembrar(1, 1, 1, horaInicio: Inicio, horaFin: Fin);
        var servicio = CrearServicio(empleados, oficinas, pisos, asignaciones);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.AsignarAsync(1, 1, Inicio, new TimeOnly(18, 0)));

        Assert.Equal(AsignacionServicio.MensajeFranjaDuplicada, excepcion.Message);
        Assert.Equal(0, asignaciones.VecesAgregado);
    }

    [Fact]
    public async Task Asignar_ConflictoUnicidad_LanzaDuplicada()
    {
        var empleados = new EmpleadoRepositorioFake();
        empleados.Sembrar(1, 1, idEdificio: 1);
        var oficinas = new OficinaRepositorioFake();
        oficinas.Sembrar(1, 1, "Oficina 101");
        var pisos = new PisoRepositorioFake();
        pisos.Sembrar(1, 1, "Piso 1");
        var asignaciones = new EmpleadoOficinaRepositorioFake { LanzarConflictoUnicidadEnAgregar = true };
        var servicio = CrearServicio(empleados, oficinas, pisos, asignaciones);

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.AsignarAsync(1, 1, Inicio, Fin));

        Assert.Equal(AsignacionServicio.MensajeFranjaDuplicada, excepcion.Message);
    }

    [Fact]
    public async Task Quitar_Existente_DevuelveTrueYAudita()
    {
        var asignaciones = new EmpleadoOficinaRepositorioFake();
        asignaciones.Sembrar(1, 1, 1, horaInicio: Inicio, horaFin: Fin);
        var auditoria = new AuditoriaRepositorioFake();
        var servicio = CrearServicio(asignaciones: asignaciones, auditoria: auditoria);

        var quitado = await servicio.QuitarAsync(1);

        Assert.True(quitado);
        Assert.Equal(1, asignaciones.VecesEliminado);
        var registro = Assert.Single(auditoria.Registros);
        Assert.Equal("Baja", registro.Accion);
        Assert.Equal("EmpleadoOficina", registro.Entidad);
        Assert.Equal(1, registro.IdEntidad);
        Assert.Equal("IdEmpleado=1; IdOficina=1; HoraInicio=09:00; HoraFin=17:00", registro.Detalle);
    }

    [Fact]
    public async Task Quitar_Inexistente_LanzaNoExiste()
    {
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.QuitarAsync(99));

        Assert.Equal(AsignacionServicio.MensajeAsignacionNoExiste, excepcion.Message);
    }

    private static AsignacionServicio CrearServicio(
        EmpleadoRepositorioFake? empleados = null,
        OficinaRepositorioFake? oficinas = null,
        PisoRepositorioFake? pisos = null,
        EmpleadoOficinaRepositorioFake? asignaciones = null,
        AuditoriaRepositorioFake? auditoria = null)
    {
        return new AsignacionServicio(
            empleados ?? new EmpleadoRepositorioFake(),
            oficinas ?? new OficinaRepositorioFake(),
            pisos ?? new PisoRepositorioFake(),
            asignaciones ?? new EmpleadoOficinaRepositorioFake(),
            auditoria ?? new AuditoriaRepositorioFake(),
            new TransaccionDbFake());
    }
}
