using System.Globalization;

namespace CoworkApp.Core.Datos;

public static class Detalles
{
    public static string Alta(IReadOnlyList<KeyValuePair<string, object?>> campos)
    {
        return string.Join("; ", campos.Select(campo => $"{campo.Key}={Formatear(campo.Value)}"));
    }

    public static string Modificacion(IReadOnlyList<(string Campo, object? Anterior, object? Nuevo)> cambios)
    {
        return string.Join("; ", cambios.Select(cambio => $"{cambio.Campo}: {Formatear(cambio.Anterior)} -> {Formatear(cambio.Nuevo)}"));
    }

    private static string Formatear(object? valor)
    {
        return Convert.ToString(valor, CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
