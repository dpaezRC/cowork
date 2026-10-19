using System.Globalization;
using System.Text.RegularExpressions;

namespace CoworkApp.Core.Validacion;

public static class Validador
{
    public const string EmailInvalido = "El email no tiene un formato valido.";
    public const string CapacidadNoEntera = "La capacidad debe ser un numero entero.";
    public const string CapacidadNoPositiva = "La capacidad debe ser mayor que cero.";
    public const string TarifaInvalida = "La tarifa debe ser un numero valido.";
    public const string TarifaNegativa = "La tarifa no puede ser negativa.";
    public const string TarifaDecimales = "La tarifa no puede tener mas de 2 decimales.";
    public const string ImporteInvalido = "El importe debe ser un numero valido.";
    public const string ImporteNoPositivo = "El importe debe ser mayor que cero.";
    public const string ImporteDecimales = "El importe no puede tener mas de 2 decimales.";
    public const string HoraFormato = "El horario debe tener el formato HH:MM (24 horas).";
    public const string FechaFormato = "La fecha debe tener el formato YYYY-MM-DD.";
    public const string FechaInvalida = "La fecha no es valida.";
    public const string ContrasenaObligatoria = "La contrasena es obligatoria.";

    private static readonly Regex HoraRegex = new(@"^([01]\d|2[0-3]):[0-5]\d$", RegexOptions.Compiled);
    private static readonly Regex FechaRegex = new(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static string Email(string? valor)
    {
        var texto = valor?.Trim() ?? string.Empty;
        if (texto.Length > 254 || !EmailRegex.IsMatch(texto))
        {
            throw new ReglaNegocioException(EmailInvalido);
        }

        return texto;
    }

    public static string Requerido(string? valor, string mensaje)
    {
        var texto = valor?.Trim() ?? string.Empty;
        if (texto.Length == 0)
        {
            throw new ReglaNegocioException(mensaje);
        }

        return texto;
    }

    public static string Longitud(string? valor, int maximo, string mensaje)
    {
        if (maximo < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximo));
        }

        var texto = valor?.Trim() ?? string.Empty;
        if (texto.Length > maximo)
        {
            throw new ReglaNegocioException(mensaje);
        }

        return texto;
    }

    public static int Capacidad(string? valor)
    {
        var texto = valor?.Trim() ?? string.Empty;
        if (!int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var capacidad))
        {
            throw new ReglaNegocioException(CapacidadNoEntera);
        }

        if (capacidad <= 0)
        {
            throw new ReglaNegocioException(CapacidadNoPositiva);
        }

        return capacidad;
    }

    public static decimal Tarifa(string? valor)
    {
        var tarifa = ParsearDecimal(valor, TarifaInvalida);
        if (tarifa < 0)
        {
            throw new ReglaNegocioException(TarifaNegativa);
        }

        if (ObtenerEscala(tarifa) > 2)
        {
            throw new ReglaNegocioException(TarifaDecimales);
        }

        return tarifa;
    }

    public static decimal Importe(string? valor)
    {
        var importe = ParsearDecimal(valor, ImporteInvalido);
        if (importe <= 0)
        {
            throw new ReglaNegocioException(ImporteNoPositivo);
        }

        if (ObtenerEscala(importe) > 2)
        {
            throw new ReglaNegocioException(ImporteDecimales);
        }

        return importe;
    }

    public static TimeOnly Hora(string? valor)
    {
        var texto = valor?.Trim() ?? string.Empty;
        if (!HoraRegex.IsMatch(texto))
        {
            throw new ReglaNegocioException(HoraFormato);
        }

        var partes = texto.Split(':');
        return new TimeOnly(int.Parse(partes[0], CultureInfo.InvariantCulture), int.Parse(partes[1], CultureInfo.InvariantCulture));
    }

    public static DateOnly Fecha(string? valor)
    {
        var texto = valor?.Trim() ?? string.Empty;
        if (!FechaRegex.IsMatch(texto))
        {
            throw new ReglaNegocioException(FechaFormato);
        }

        if (!DateOnly.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
        {
            throw new ReglaNegocioException(FechaInvalida);
        }

        return fecha;
    }

    public static string Estado(string? estado, IReadOnlyCollection<string> permitidos)
    {
        if (estado is null || !permitidos.Contains(estado))
        {
            throw new ReglaNegocioException($"Estado invalido. Valores permitidos: {string.Join(", ", permitidos)}.");
        }

        return estado;
    }

    public static string Rol(string? rol, IReadOnlyCollection<string> permitidos)
    {
        if (rol is null || !permitidos.Contains(rol))
        {
            throw new ReglaNegocioException($"Rol invalido. Valores permitidos: {string.Join(", ", permitidos)}.");
        }

        return rol;
    }

    public static string Contrasena(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
        {
            throw new ReglaNegocioException(ContrasenaObligatoria);
        }

        return valor;
    }

    private static decimal ParsearDecimal(string? valor, string mensaje)
    {
        var texto = valor?.Trim() ?? string.Empty;
        const NumberStyles estilos = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

        if (!decimal.TryParse(texto, estilos, CultureInfo.InvariantCulture, out var numero))
        {
            throw new ReglaNegocioException(mensaje);
        }

        return numero;
    }

    private static int ObtenerEscala(decimal valor)
    {
        return (decimal.GetBits(valor)[3] >> 16) & 0xFF;
    }
}
