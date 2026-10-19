using System.Text.Json;

namespace CoworkApp.Configuration;

public sealed class AppConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool SeedDesarrollo { get; set; }

    public static string RutaPredeterminada => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public static AppConfig Cargar()
    {
        var ruta = RutaPredeterminada;
        if (!File.Exists(ruta))
        {
            throw new FileNotFoundException("No se encontro appsettings.json en el directorio de salida.", ruta);
        }

        var json = File.ReadAllText(ruta);
        var configuracion = JsonSerializer.Deserialize<AppConfig>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (configuracion is null)
        {
            throw new InvalidOperationException("appsettings.json no contiene una configuracion valida.");
        }

        if (string.IsNullOrWhiteSpace(configuracion.ConnectionString))
        {
            throw new InvalidOperationException("La propiedad ConnectionString es obligatoria en appsettings.json.");
        }

        return configuracion;
    }
}
