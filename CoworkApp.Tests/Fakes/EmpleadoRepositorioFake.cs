using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Tests.Fakes;

public sealed class EmpleadoRepositorioFake : IEmpleadoRepositorio
{
    private readonly List<EmpleadoDetalle> _empleados = new();
    private int _siguienteId = 1;

    public IReadOnlyList<EmpleadoDetalle> Empleados => _empleados;

    public int VecesAgregado { get; private set; }

    public int VecesActualizado { get; private set; }

    public bool LanzarConflictoUnicidadEnAgregar { get; set; }

    public EmpleadoDetalle Sembrar(
        int id,
        int idUsuario,
        string nombre = "Nombre",
        string apellido = "Apellido",
        string email = "empleado@cowork.com",
        string? telefono = null,
        int idEdificio = 1,
        string edificio = "Edificio Central",
        string estado = "Activo")
    {
        var empleado = new EmpleadoDetalle
        {
            IdEmpleado = id,
            IdUsuario = idUsuario,
            Nombre = nombre,
            Apellido = apellido,
            Email = email,
            Telefono = telefono,
            IdEdificio = idEdificio,
            Edificio = edificio,
            Estado = estado,
            FechaAlta = new DateTime(2024, 1, 1)
        };
        _empleados.Add(empleado);
        _siguienteId = Math.Max(_siguienteId, id + 1);
        return empleado;
    }

    public Task<List<EmpleadoDetalle>> ListarDetalleAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_empleados.OrderBy(empleado => empleado.IdEmpleado).ToList());
    }

    public Task<EmpleadoDetalle?> ObtenerDetalleAsync(
        int idEmpleado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_empleados.FirstOrDefault(empleado => empleado.IdEmpleado == idEmpleado));
    }

    public Task<EmpleadoDetalle?> ObtenerDetallePorUsuarioAsync(
        int idUsuario,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(_empleados.FirstOrDefault(empleado => empleado.IdUsuario == idUsuario));
    }

    public Task<int> AgregarAsync(
        int idUsuario,
        int idEdificio,
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
        _empleados.Add(new EmpleadoDetalle
        {
            IdEmpleado = id,
            IdUsuario = idUsuario,
            IdEdificio = idEdificio,
            Estado = "Activo",
            FechaAlta = new DateTime(2024, 1, 1)
        });
        return Task.FromResult(id);
    }

    public Task<int> ActualizarAsync(
        int idEmpleado,
        int idEdificio,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var empleado = _empleados.First(actual => actual.IdEmpleado == idEmpleado);
        empleado.IdEdificio = idEdificio;
        empleado.Estado = estado;
        VecesActualizado++;
        return Task.FromResult(1);
    }

    public Task<int> ActualizarEstadoAsync(
        int idEmpleado,
        string estado,
        SqlConnection? conexion = null,
        SqlTransaction? transaccion = null,
        CancellationToken ct = default)
    {
        var empleado = _empleados.First(actual => actual.IdEmpleado == idEmpleado);
        empleado.Estado = estado;
        VecesActualizado++;
        return Task.FromResult(1);
    }
}
