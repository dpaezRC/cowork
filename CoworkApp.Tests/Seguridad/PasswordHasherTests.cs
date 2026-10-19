using CoworkApp.Core.Seguridad;
using Xunit;

namespace CoworkApp.Tests.Seguridad;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_GeneraElFormatoDePython()
    {
        var hash = PasswordHasher.Hash("ClaveSegura123!");

        var partes = hash.Split('$');
        Assert.Equal(4, partes.Length);
        Assert.Equal("pbkdf2_sha256", partes[0]);
        Assert.Equal("100000", partes[1]);
        Assert.Equal(32, partes[2].Length);
        Assert.Equal(64, partes[3].Length);
        Assert.Matches("^[0-9a-f]+$", partes[2]);
        Assert.Matches("^[0-9a-f]+$", partes[3]);
    }

    [Fact]
    public void Hash_YVerify_HacenRoundTrip()
    {
        var hash = PasswordHasher.Hash("ClaveSegura123!");

        Assert.True(PasswordHasher.Verify("ClaveSegura123!", hash));
    }

    [Fact]
    public void Verify_ContrasenaIncorrecta_DevuelveFalse()
    {
        var hash = PasswordHasher.Hash("ClaveSegura123!");

        Assert.False(PasswordHasher.Verify("ClaveSegura124!", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("pbkdf2_sha256$100000$00112233445566778899aabbccddeeff")]
    [InlineData("pbkdf2_sha1$100000$00112233445566778899aabbccddeeff$adf53351f49a4e49ff8357beb6eac7152ac391fb8d0e8edd1367279b8b85bf96")]
    [InlineData("pbkdf2_sha256$abc$00112233445566778899aabbccddeeff$adf53351f49a4e49ff8357beb6eac7152ac391fb8d0e8edd1367279b8b85bf96")]
    [InlineData("pbkdf2_sha256$0$00112233445566778899aabbccddeeff$adf53351f49a4e49ff8357beb6eac7152ac391fb8d0e8edd1367279b8b85bf96")]
    [InlineData("pbkdf2_sha256$100000$zz$adf53351f49a4e49ff8357beb6eac7152ac391fb8d0e8edd1367279b8b85bf96")]
    [InlineData("pbkdf2_sha256$-1$00112233445566778899aabbccddeeff$adf53351f49a4e49ff8357beb6eac7152ac391fb8d0e8edd1367279b8b85bf96")]
    public void Verify_HashMalformado_DevuelveFalseSinExcepciones(string hash)
    {
        Assert.False(PasswordHasher.Verify("ClavePrueba123!", hash));
    }

    [Fact]
    public void Verify_HashAlterado_DevuelveFalse()
    {
        var hash = PasswordHasher.Hash("ClaveSegura123!");
        var ultimo = hash[^1];
        var alterado = hash[..^1] + (ultimo == '0' ? '1' : '0');

        Assert.False(PasswordHasher.Verify("ClaveSegura123!", alterado));
    }

    [Fact]
    public void Verify_EsCompatibleConUnVectorGeneradoConPython()
    {
        const string hashPython =
            "pbkdf2_sha256$100000$00112233445566778899aabbccddeeff$" +
            "adf53351f49a4e49ff8357beb6eac7152ac391fb8d0e8edd1367279b8b85bf96";

        Assert.True(PasswordHasher.Verify("ClavePrueba123!", hashPython));
        Assert.False(PasswordHasher.Verify("ClavePrueba124!", hashPython));
    }

    [Fact]
    public void Verify_AceptaCualquierIteracionValidaDelHash()
    {
        const string hashPython =
            "pbkdf2_sha256$5000$ffeeddccbbaa99887766554433221100$" +
            "56fa1d8a1fcfb48edfc1cccb01049036628883199299cb613a5dd36630932832";

        Assert.True(PasswordHasher.Verify("OtraClave456!", hashPython));
    }
}
