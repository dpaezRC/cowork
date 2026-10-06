using CoworkApp.Shared.Security;
using Shouldly;
using Xunit;

namespace CoworkApp.Tests.Shared.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void El_hash_no_contiene_la_password_en_claro()
    {
        string hash = _hasher.Hash("SuperSecreta123!");

        hash.ShouldNotContain("SuperSecreta123!");
        hash.ShouldStartWith("PBKDF2-SHA256$");
    }

    [Fact]
    public void El_hash_tiene_el_formato_algoritmo_iteraciones_sal_hash()
    {
        string hash = _hasher.Hash("SuperSecreta123!");

        string[] partes = hash.Split('$');
        partes.Length.ShouldBe(4);
        partes[0].ShouldBe(PasswordHasher.Algoritmo);
        int.Parse(partes[1]).ShouldBePositive();
        Convert.FromBase64String(partes[2]).Length.ShouldBePositive();
        Convert.FromBase64String(partes[3]).Length.ShouldBe(32);
    }

    [Fact]
    public void Verify_con_la_password_correcta_devuelve_true()
    {
        string hash = _hasher.Hash("SuperSecreta123!");

        _hasher.Verify("SuperSecreta123!", hash).ShouldBeTrue();
    }

    [Fact]
    public void Verify_con_password_incorrecta_devuelve_false()
    {
        string hash = _hasher.Hash("SuperSecreta123!");

        _hasher.Verify("supersecreta123!", hash).ShouldBeFalse();
        _hasher.Verify("", hash).ShouldBeFalse();
    }

    [Fact]
    public void Dos_hashes_de_la_misma_password_usan_sal_distinta()
    {
        string primero = _hasher.Hash("SuperSecreta123!");
        string segundo = _hasher.Hash("SuperSecreta123!");

        primero.ShouldNotBe(segundo);
        _hasher.Verify("SuperSecreta123!", primero).ShouldBeTrue();
        _hasher.Verify("SuperSecreta123!", segundo).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("PBKDF2-SHA256")]
    [InlineData("PBKDF2-SHA256$abc$YWJj")]
    [InlineData("OTRO-ALGORITMO$1000$YWJj$ZGVm")]
    [InlineData("PBKDF2-SHA256$0$YWJj$ZGVm")]
    [InlineData("PBKDF2-SHA256$100000$no-base64$ZGVm")]
    public void Verify_con_hash_malformado_devuelve_false(string hash)
    {
        _hasher.Verify("SuperSecreta123!", hash).ShouldBeFalse();
    }

    [Fact]
    public void Un_hash_manipulado_no_verifica()
    {
        string hash = _hasher.Hash("SuperSecreta123!");
        string[] partes = hash.Split('$');
        string hashManipulado = string.Join('$',
            partes[0], partes[1], partes[2],
            Convert.ToBase64String(new byte[32]));

        _hasher.Verify("SuperSecreta123!", hashManipulado).ShouldBeFalse();
    }
}
