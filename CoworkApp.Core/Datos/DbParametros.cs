using System.Data;
using Microsoft.Data.SqlClient;

namespace CoworkApp.Core.Datos;

public static class DbParametros
{
    public static SqlParameter NVarChar(string nombre, string? valor, int tamanio)
        => Crear(nombre, SqlDbType.NVarChar, valor, tamanio);

    public static SqlParameter VarChar(string nombre, string? valor, int tamanio)
        => Crear(nombre, SqlDbType.VarChar, valor, tamanio);

    public static SqlParameter Int(string nombre, int? valor)
        => Crear(nombre, SqlDbType.Int, valor);

    public static SqlParameter BigInt(string nombre, long? valor)
        => Crear(nombre, SqlDbType.BigInt, valor);

    public static SqlParameter Decimal(string nombre, decimal? valor, byte precision = 18, byte escala = 2)
    {
        var parametro = Crear(nombre, SqlDbType.Decimal, valor);
        parametro.Precision = precision;
        parametro.Scale = escala;
        return parametro;
    }

    public static SqlParameter Date(string nombre, DateOnly? valor)
        => Crear(nombre, SqlDbType.Date, valor?.ToDateTime(TimeOnly.MinValue));

    public static SqlParameter Time(string nombre, TimeOnly? valor)
        => Crear(nombre, SqlDbType.Time, valor?.ToTimeSpan());

    public static SqlParameter DateTime2(string nombre, DateTime? valor, byte escala = 0)
    {
        var parametro = Crear(nombre, SqlDbType.DateTime2, valor);
        parametro.Scale = escala;
        return parametro;
    }

    public static SqlParameter Bit(string nombre, bool? valor)
        => Crear(nombre, SqlDbType.Bit, valor);

    private static SqlParameter Crear(string nombre, SqlDbType tipo, object? valor, int? tamanio = null)
    {
        var parametro = new SqlParameter(nombre, tipo)
        {
            Value = valor ?? DBNull.Value
        };

        if (tamanio.HasValue)
        {
            parametro.Size = tamanio.Value;
        }

        return parametro;
    }
}
