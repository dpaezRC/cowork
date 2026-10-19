using CoworkApp.Core.Dominio;
using CoworkApp.Core.Seguridad;
using Xunit;

namespace CoworkApp.Tests.Seguridad;

public sealed class SesionActualTests
{
    [Fact]
    public void SesionVacia_NoEstaAutenticada()
    {
        var sesion = new SesionActual();

        Assert.False(sesion.Autenticada);
        Assert.Null(sesion.IdUsuario);
        Assert.Null(sesion.Nombre);
        Assert.Null(sesion.Email);
        Assert.Null(sesion.Rol);
        Assert.False(sesion.EsAdministrador);
        Assert.False(sesion.EsCliente);
        Assert.False(sesion.EsEmpleado);
    }

    [Fact]
    public void IniciarAdministrador_CompletaLosDatosYActivaElRol()
    {
        var sesion = new SesionActual();

        sesion.Iniciar(1, "Administrador Demo", "admin@cowork.local", Roles.Administrador);

        Assert.True(sesion.Autenticada);
        Assert.Equal(1, sesion.IdUsuario);
        Assert.Equal("Administrador Demo", sesion.Nombre);
        Assert.Equal("admin@cowork.local", sesion.Email);
        Assert.Equal(Roles.Administrador, sesion.Rol);
        Assert.True(sesion.EsAdministrador);
        Assert.False(sesion.EsCliente);
        Assert.False(sesion.EsEmpleado);
    }

    [Fact]
    public void IniciarCliente_ActivaSoloElFlagDeCliente()
    {
        var sesion = new SesionActual();

        sesion.Iniciar(2, "Cliente Demo", "cliente@cowork.local", Roles.Cliente);

        Assert.True(sesion.Autenticada);
        Assert.True(sesion.EsCliente);
        Assert.False(sesion.EsAdministrador);
        Assert.False(sesion.EsEmpleado);
    }

    [Fact]
    public void IniciarEmpleado_ActivaSoloElFlagDeEmpleado()
    {
        var sesion = new SesionActual();

        sesion.Iniciar(3, "Empleado Demo", "empleado@cowork.local", Roles.Empleado);

        Assert.True(sesion.Autenticada);
        Assert.True(sesion.EsEmpleado);
        Assert.False(sesion.EsAdministrador);
        Assert.False(sesion.EsCliente);
    }

    [Fact]
    public void Cerrar_LimpiaLaSesion()
    {
        var sesion = new SesionActual();
        sesion.Iniciar(1, "Administrador Demo", "admin@cowork.local", Roles.Administrador);

        sesion.Cerrar();

        Assert.False(sesion.Autenticada);
        Assert.Null(sesion.IdUsuario);
        Assert.Null(sesion.Nombre);
        Assert.Null(sesion.Email);
        Assert.Null(sesion.Rol);
        Assert.False(sesion.EsAdministrador);
        Assert.False(sesion.EsCliente);
        Assert.False(sesion.EsEmpleado);
    }
}
