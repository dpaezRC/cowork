using CoworkApp.Core.Datos;
using Xunit;

namespace CoworkApp.Tests.Datos;

public sealed class DetallesTests
{
    [Fact]
    public void Alta_UneCamposConElFormatoClaveValor()
    {
        var detalle = Detalles.Alta(
        [
            new("Nombre", "Edificio Central"),
            new("Direccion", "Av. Central 100"),
            new("Estado", "Activo")
        ]);

        Assert.Equal("Nombre=Edificio Central; Direccion=Av. Central 100; Estado=Activo", detalle);
    }

    [Fact]
    public void Alta_FormateaEnterosYDecimalesDeFormaInvariante()
    {
        var detalle = Detalles.Alta(
        [
            new("IdPiso", 3),
            new("Capacidad", 4),
            new("TarifaHora", 1000.00m)
        ]);

        Assert.Equal("IdPiso=3; Capacidad=4; TarifaHora=1000.00", detalle);
    }

    [Fact]
    public void Modificacion_UneCambiosConFlecha()
    {
        var detalle = Detalles.Modificacion([("Estado", "Activo", "Inactivo")]);

        Assert.Equal("Estado: Activo -> Inactivo", detalle);
    }

    [Fact]
    public void Modificacion_UneVariosCambios()
    {
        var detalle = Detalles.Modificacion(
        [
            ("Nombre", "Viejo", "Nuevo"),
            ("Capacidad", 4, 6)
        ]);

        Assert.Equal("Nombre: Viejo -> Nuevo; Capacidad: 4 -> 6", detalle);
    }
}
