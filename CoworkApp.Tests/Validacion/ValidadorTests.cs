using CoworkApp.Core.Dominio;
using CoworkApp.Core.Validacion;
using Xunit;

namespace CoworkApp.Tests.Validacion;

public sealed class ValidadorTests
{
    [Fact]
    public void Email_Valido_DevuelveElTextoNormalizado()
    {
        Assert.Equal("persona@cowork.local", Validador.Email("  persona@cowork.local  "));
    }

    [Theory]
    [InlineData("")]
    [InlineData("sin-arroba")]
    [InlineData("persona@dominio")]
    [InlineData("persona con espacios@dominio.com")]
    public void Email_Invalido_LanzaReglaDeNegocio(string email)
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() => Validador.Email(email));

        Assert.Equal(Validador.EmailInvalido, excepcion.Message);
    }

    [Fact]
    public void Email_MasLargoQue254_LanzaReglaDeNegocio()
    {
        var email = new string('a', 250) + "@b.com";

        var excepcion = Assert.Throws<ReglaNegocioException>(() => Validador.Email(email));

        Assert.Equal(Validador.EmailInvalido, excepcion.Message);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("4", 4)]
    [InlineData(" 25 ", 25)]
    public void Capacidad_Valida_DevuelveEntero(string valor, int esperado)
    {
        Assert.Equal(esperado, Validador.Capacidad(valor));
    }

    [Theory]
    [InlineData("abc", "La capacidad debe ser un numero entero.")]
    [InlineData("1.5", "La capacidad debe ser un numero entero.")]
    [InlineData("0", "La capacidad debe ser mayor que cero.")]
    [InlineData("-2", "La capacidad debe ser mayor que cero.")]
    public void Capacidad_Invalida_LanzaMensajeCorrespondiente(string valor, string mensaje)
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() => Validador.Capacidad(valor));

        Assert.Equal(mensaje, excepcion.Message);
    }

    [Fact]
    public void Tarifa_Valida_DevuelveDecimal()
    {
        Assert.Equal(1000.50m, Validador.Tarifa("1000.50"));
        Assert.Equal(0m, Validador.Tarifa("0"));
    }

    [Theory]
    [InlineData("-0.01", "La tarifa no puede ser negativa.")]
    [InlineData("1.234", "La tarifa no puede tener mas de 2 decimales.")]
    [InlineData("abc", "La tarifa debe ser un numero valido.")]
    [InlineData("", "La tarifa debe ser un numero valido.")]
    public void Tarifa_Invalida_LanzaMensajeCorrespondiente(string valor, string mensaje)
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() => Validador.Tarifa(valor));

        Assert.Equal(mensaje, excepcion.Message);
    }

    [Fact]
    public void Importe_Valido_DevuelveDecimal()
    {
        Assert.Equal(100.25m, Validador.Importe("100.25"));
    }

    [Theory]
    [InlineData("0", "El importe debe ser mayor que cero.")]
    [InlineData("-5", "El importe debe ser mayor que cero.")]
    [InlineData("1.234", "El importe no puede tener mas de 2 decimales.")]
    [InlineData("abc", "El importe debe ser un numero valido.")]
    public void Importe_Invalido_LanzaMensajeCorrespondiente(string valor, string mensaje)
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() => Validador.Importe(valor));

        Assert.Equal(mensaje, excepcion.Message);
    }

    [Theory]
    [InlineData("08:30", 8, 30)]
    [InlineData("00:00", 0, 0)]
    [InlineData("23:59", 23, 59)]
    public void Hora_Valida_DevuelveTimeOnly(string valor, int hora, int minuto)
    {
        Assert.Equal(new TimeOnly(hora, minuto), Validador.Hora(valor));
    }

    [Theory]
    [InlineData("8:30")]
    [InlineData("24:00")]
    [InlineData("08:60")]
    [InlineData("abc")]
    public void Hora_Invalida_LanzaMensajeDeFormato(string valor)
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() => Validador.Hora(valor));

        Assert.Equal("El horario debe tener el formato HH:MM (24 horas).", excepcion.Message);
    }

    [Fact]
    public void Fecha_Valida_DevuelveDateOnly()
    {
        Assert.Equal(new DateOnly(2026, 10, 19), Validador.Fecha("2026-10-19"));
    }

    [Theory]
    [InlineData("19/10/2026", "La fecha debe tener el formato YYYY-MM-DD.")]
    [InlineData("2026-2-3", "La fecha debe tener el formato YYYY-MM-DD.")]
    [InlineData("2026-02-30", "La fecha no es valida.")]
    public void Fecha_Invalida_LanzaMensajeCorrespondiente(string valor, string mensaje)
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() => Validador.Fecha(valor));

        Assert.Equal(mensaje, excepcion.Message);
    }

    [Fact]
    public void Estado_Valido_DevuelveElEstado()
    {
        Assert.Equal(Estados.Activo, Validador.Estado("Activo", Estados.Usuario));
        Assert.Equal(Estados.Confirmada, Validador.Estado("Confirmada", Estados.Reserva));
    }

    [Fact]
    public void Estado_Invalido_LanzaMensajeConValoresPermitidos()
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() => Validador.Estado("Suspendido", Estados.Usuario));

        Assert.Equal("Estado invalido. Valores permitidos: Activo, Inactivo.", excepcion.Message);
    }

    [Fact]
    public void Rol_Valido_DevuelveElRol()
    {
        Assert.Equal(Roles.Cliente, Validador.Rol("Cliente", Roles.Gestionables));
    }

    [Fact]
    public void Rol_Invalido_LanzaMensajeConValoresPermitidos()
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() => Validador.Rol("Empleado", Roles.Gestionables));

        Assert.Equal("Rol invalido. Valores permitidos: Cliente, Administrador.", excepcion.Message);
    }

    [Fact]
    public void Requerido_DevuelveElTextoNormalizado()
    {
        Assert.Equal("Cowork", Validador.Requerido("  Cowork  ", "El nombre es obligatorio."));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Requerido_Vacio_LanzaElMensajeIndicado(string valor)
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() => Validador.Requerido(valor, "El nombre es obligatorio."));

        Assert.Equal("El nombre es obligatorio.", excepcion.Message);
    }

    [Fact]
    public void Longitud_Excedida_LanzaElMensajeIndicado()
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(
            () => Validador.Longitud("123456", 5, "El texto no puede superar los 5 caracteres."));

        Assert.Equal("El texto no puede superar los 5 caracteres.", excepcion.Message);
    }

    [Fact]
    public void Longitud_Valida_DevuelveElTextoNormalizado()
    {
        Assert.Equal("12345", Validador.Longitud(" 12345 ", 5, "mensaje"));
    }

    [Fact]
    public void Contrasena_Vacia_LanzaMensajeObligatorio()
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() => Validador.Contrasena(""));

        Assert.Equal("La contrasena es obligatoria.", excepcion.Message);
    }
}
