using CoworkApp.Features.H.Login;
using Shouldly;
using Xunit;

namespace CoworkApp.Tests.Features.Login;

public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Una_petición_completa_es_válida()
    {
        var resultado = _validator.Validate(new LoginRequest("admin@cowork.local", "Secreta123!"));

        resultado.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Un_email_vacío_es_inválido()
    {
        var resultado = _validator.Validate(new LoginRequest("", "Secreta123!"));

        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.ErrorMessage == "Ingresá tu email.");
    }

    [Fact]
    public void Una_password_vacía_es_inválida()
    {
        var resultado = _validator.Validate(new LoginRequest("admin@cowork.local", ""));

        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.ErrorMessage == "Ingresá tu contraseña.");
    }

    [Fact]
    public void Un_email_con_formato_raro_es_inválido()
    {
        var resultado = _validator.Validate(new LoginRequest("no-es-un-email", "Secreta123!"));

        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.ErrorMessage == "El email no tiene un formato válido.");
    }
}
