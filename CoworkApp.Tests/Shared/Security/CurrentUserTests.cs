using CoworkApp.Shared.Security;
using Shouldly;
using Xunit;

namespace CoworkApp.Tests.Shared.Security;

public class CurrentUserTests
{
    private static readonly DateTime Ahora = new(2026, 10, 6, 10, 0, 0);
    private static readonly TimeSpan InactividadMaxima = TimeSpan.FromMinutes(30);

    private static CurrentUser UsuarioAutenticado()
    {
        var usuario = new CurrentUser();
        usuario.Establecer(1, "Ana", "ana@cowork.local", "admin", Ahora);
        return usuario;
    }

    [Fact]
    public void Una_sesión_recién_establecida_no_está_expirada()
    {
        var usuario = UsuarioAutenticado();

        usuario.Autenticado.ShouldBeTrue();
        usuario.ExpiradaPorInactividad(Ahora.AddMinutes(29), InactividadMaxima).ShouldBeFalse();
    }

    [Fact]
    public void Después_del_plazo_de_inactividad_la_sesión_expira()
    {
        var usuario = UsuarioAutenticado();

        usuario.ExpiradaPorInactividad(Ahora.AddMinutes(31), InactividadMaxima).ShouldBeTrue();
    }

    [Fact]
    public void Cualquier_actividad_renueva_el_plazo()
    {
        var usuario = UsuarioAutenticado();

        usuario.RegistrarActividad(Ahora.AddMinutes(29));

        usuario.ExpiradaPorInactividad(Ahora.AddMinutes(45), InactividadMaxima).ShouldBeFalse();
        usuario.ExpiradaPorInactividad(Ahora.AddMinutes(60), InactividadMaxima).ShouldBeTrue();
    }

    [Fact]
    public void Una_sesión_cerrada_nunca_expira_ni_queda_autenticada()
    {
        var usuario = UsuarioAutenticado();
        usuario.CerrarSesion();

        usuario.Autenticado.ShouldBeFalse();
        usuario.ExpiradaPorInactividad(Ahora.AddDays(1), InactividadMaxima).ShouldBeFalse();
    }

    [Fact]
    public void El_rol_alimenta_el_acceso_por_rol()
    {
        var usuario = UsuarioAutenticado();

        usuario.EsAdmin.ShouldBeTrue();
        usuario.EsSupervisor.ShouldBeFalse();
        usuario.EsCliente.ShouldBeFalse();
    }
}
