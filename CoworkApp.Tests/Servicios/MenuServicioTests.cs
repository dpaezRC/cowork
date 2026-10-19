using CoworkApp.Core.Dominio;
using CoworkApp.Core.Servicios;
using CoworkApp.Core.Validacion;
using Xunit;

namespace CoworkApp.Tests.Servicios;

public sealed class MenuServicioTests
{
    [Fact]
    public void Administrador_TieneEspaciosPersonalUsuariosYReservas()
    {
        var modulos = MenuServicio.ObtenerModulos(Roles.Administrador).Select(m => m.Modulo).ToArray();

        Assert.Equal(
            new[] { Modulo.Espacios, Modulo.Personal, Modulo.Usuarios, Modulo.Reservas },
            modulos);
    }

    [Fact]
    public void Cliente_SoloTieneReservas()
    {
        var modulos = MenuServicio.ObtenerModulos(Roles.Cliente).Select(m => m.Modulo).ToArray();

        Assert.Equal(new[] { Modulo.Reservas }, modulos);
    }

    [Fact]
    public void Empleado_SoloTieneControlOperativo()
    {
        var modulos = MenuServicio.ObtenerModulos(Roles.Empleado).Select(m => m.Modulo).ToArray();

        Assert.Equal(new[] { Modulo.ControlOperativo }, modulos);
    }

    [Theory]
    [InlineData("Invitado")]
    [InlineData(null)]
    public void RolDesconocido_NoTieneModulos(string? rol)
    {
        Assert.Empty(MenuServicio.ObtenerModulos(rol));
    }

    [Theory]
    [InlineData(Roles.Administrador, Modulo.Espacios)]
    [InlineData(Roles.Administrador, Modulo.Reservas)]
    [InlineData(Roles.Cliente, Modulo.Reservas)]
    [InlineData(Roles.Empleado, Modulo.ControlOperativo)]
    public void AccesoPermitido_NoLanza(string rol, Modulo modulo)
    {
        Assert.True(MenuServicio.PuedeAcceder(rol, modulo));

        MenuServicio.ValidarAcceso(rol, modulo);
    }

    [Theory]
    [InlineData(Roles.Cliente, Modulo.Usuarios)]
    [InlineData(Roles.Cliente, Modulo.ControlOperativo)]
    [InlineData(Roles.Empleado, Modulo.Espacios)]
    [InlineData(Roles.Administrador, Modulo.ControlOperativo)]
    public void AccesoNoPermitido_LanzaReglaDeNegocio(string rol, Modulo modulo)
    {
        Assert.False(MenuServicio.PuedeAcceder(rol, modulo));

        var excepcion = Assert.Throws<ReglaNegocioException>(() => MenuServicio.ValidarAcceso(rol, modulo));

        Assert.Equal(MenuServicio.MensajeSinPermisos, excepcion.Message);
    }
}
