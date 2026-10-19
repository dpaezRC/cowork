using System.Data;
using CoworkApp.Core.Datos;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CoworkApp.Tests.Datos;

public sealed class DbParametrosTests
{
    [Fact]
    public void NVarChar_AsignaTipoTamanioYValor()
    {
        var parametro = DbParametros.NVarChar("@Nombre", "Cowork", 100);

        Assert.Equal("@Nombre", parametro.ParameterName);
        Assert.Equal(SqlDbType.NVarChar, parametro.SqlDbType);
        Assert.Equal(100, parametro.Size);
        Assert.Equal("Cowork", parametro.Value);
    }

    [Fact]
    public void NVarChar_Nulo_UsaDBNull()
    {
        var parametro = DbParametros.NVarChar("@Telefono", null, 30);

        Assert.Equal(SqlDbType.NVarChar, parametro.SqlDbType);
        Assert.Equal(30, parametro.Size);
        Assert.IsType<DBNull>(parametro.Value);
    }

    [Fact]
    public void VarChar_AsignaTipoTamanioYValor()
    {
        var parametro = DbParametros.VarChar("@Estado", "Activo", 10);

        Assert.Equal(SqlDbType.VarChar, parametro.SqlDbType);
        Assert.Equal(10, parametro.Size);
        Assert.Equal("Activo", parametro.Value);
    }

    [Fact]
    public void Int_AsignaTipoYValor()
    {
        var parametro = DbParametros.Int("@IdUsuario", 7);

        Assert.Equal(SqlDbType.Int, parametro.SqlDbType);
        Assert.Equal(7, parametro.Value);
    }

    [Fact]
    public void Int_Nulo_UsaDBNull()
    {
        var parametro = DbParametros.Int("@IdEntidad", null);

        Assert.Equal(SqlDbType.Int, parametro.SqlDbType);
        Assert.IsType<DBNull>(parametro.Value);
    }

    [Fact]
    public void BigInt_AsignaTipoYValor()
    {
        var parametro = DbParametros.BigInt("@IdAuditoria", 9000000000L);

        Assert.Equal(SqlDbType.BigInt, parametro.SqlDbType);
        Assert.Equal(9000000000L, parametro.Value);
    }

    [Fact]
    public void BigInt_Nulo_UsaDBNull()
    {
        var parametro = DbParametros.BigInt("@IdAuditoria", null);

        Assert.Equal(SqlDbType.BigInt, parametro.SqlDbType);
        Assert.IsType<DBNull>(parametro.Value);
    }

    [Fact]
    public void Decimal_AsignaPrecisionEscalaYValor()
    {
        var parametro = DbParametros.Decimal("@Importe", 123.45m);

        Assert.Equal(SqlDbType.Decimal, parametro.SqlDbType);
        Assert.Equal((byte)18, parametro.Precision);
        Assert.Equal((byte)2, parametro.Scale);
        Assert.Equal(123.45m, parametro.Value);
    }

    [Fact]
    public void Decimal_PermitePrecisionYEscalaPersonalizadas()
    {
        var parametro = DbParametros.Decimal("@Importe", 1.2345m, 10, 4);

        Assert.Equal((byte)10, parametro.Precision);
        Assert.Equal((byte)4, parametro.Scale);
    }

    [Fact]
    public void Decimal_Nulo_UsaDBNull()
    {
        var parametro = DbParametros.Decimal("@Importe", null);

        Assert.Equal(SqlDbType.Decimal, parametro.SqlDbType);
        Assert.IsType<DBNull>(parametro.Value);
    }

    [Fact]
    public void Date_ConvierteDateOnlyAValorSql()
    {
        var parametro = DbParametros.Date("@Fecha", new DateOnly(2026, 10, 19));

        Assert.Equal(SqlDbType.Date, parametro.SqlDbType);
        var valor = Assert.IsType<DateTime>(parametro.Value);
        Assert.Equal(new DateTime(2026, 10, 19), valor);
    }

    [Fact]
    public void Date_Nulo_UsaDBNull()
    {
        var parametro = DbParametros.Date("@Fecha", null);

        Assert.Equal(SqlDbType.Date, parametro.SqlDbType);
        Assert.IsType<DBNull>(parametro.Value);
    }

    [Fact]
    public void Time_ConvierteTimeOnlyAValorSql()
    {
        var parametro = DbParametros.Time("@HoraInicio", new TimeOnly(8, 30));

        Assert.Equal(SqlDbType.Time, parametro.SqlDbType);
        var valor = Assert.IsType<TimeSpan>(parametro.Value);
        Assert.Equal(new TimeSpan(8, 30, 0), valor);
    }

    [Fact]
    public void Time_Nulo_UsaDBNull()
    {
        var parametro = DbParametros.Time("@HoraInicio", null);

        Assert.Equal(SqlDbType.Time, parametro.SqlDbType);
        Assert.IsType<DBNull>(parametro.Value);
    }

    [Fact]
    public void DateTime2_AsignaEscalaYValor()
    {
        var fecha = new DateTime(2026, 10, 19, 14, 30, 0);
        var parametro = DbParametros.DateTime2("@FechaPago", fecha);

        Assert.Equal(SqlDbType.DateTime2, parametro.SqlDbType);
        Assert.Equal((byte)0, parametro.Scale);
        Assert.Equal(fecha, parametro.Value);
    }

    [Fact]
    public void DateTime2_Nulo_UsaDBNull()
    {
        var parametro = DbParametros.DateTime2("@FechaCancelacion", null);

        Assert.Equal(SqlDbType.DateTime2, parametro.SqlDbType);
        Assert.IsType<DBNull>(parametro.Value);
    }

    [Fact]
    public void Bit_AsignaTipoYValor()
    {
        var parametro = DbParametros.Bit("@Activo", true);

        Assert.Equal(SqlDbType.Bit, parametro.SqlDbType);
        Assert.Equal(true, parametro.Value);
    }

    [Fact]
    public void Bit_Nulo_UsaDBNull()
    {
        var parametro = DbParametros.Bit("@Activo", null);

        Assert.Equal(SqlDbType.Bit, parametro.SqlDbType);
        Assert.IsType<DBNull>(parametro.Value);
    }
}
