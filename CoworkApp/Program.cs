using CoworkApp.Features.A.EdificiosOficinas.ListarOficinas;
using CoworkApp.Features.H.Login;
using CoworkApp.Shared.Configuration;
using CoworkApp.Shared.Data;
using CoworkApp.Shared.Security;
using CoworkApp.Shared.Time;
using CoworkApp.Views.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoworkApp;

static class Program
{
    /// <summary>
    /// Único punto donde se construye el grafo de dependencias (AGENTS.md regla 1)
    /// y donde se enruta por rol después del login.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        if (!IntentarCrearConfiguracion(out var configuracion))
            return;

        using var servicios = ConstruirServicios(configuracion);

        if (!SembrarUsuariosIniciales(servicios))
            return;

        var sesion = servicios.GetRequiredService<CurrentUser>();

        while (true)
        {
            using (var login = servicios.GetRequiredService<LoginForm>())
            {
                if (login.ShowDialog() != DialogResult.OK)
                    return;
            }

            var formulario = servicios.GetRequiredService<Form1>();
            Application.Run(formulario);

            if (formulario.Motivo == MotivoCierre.Usuario)
                return;

            // SesionCerrada o Inactividad: vuelta al login con la sesión limpia.
            sesion.CerrarSesion();
        }
    }

    private static bool IntentarCrearConfiguracion(out IConfiguration configuracion)
    {
        configuracion = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        if (configuracion.GetConnectionString("Default") is not null)
            return true;

        MessageBox.Show(
            "Falta la cadena de conexión en appsettings.json.\n\n" +
            "Copiá appsettings.example.json como appsettings.json y completá los datos.",
            "Configuración faltante", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    /// <summary>
    /// Primer arranque (§7.2): si dbo.Usuarios está vacía se crean las tres
    /// credenciales y se muestran una única vez. Se espera acá antes de crear
    /// cualquier Form, así que no hay hilo de UI que bloquear.
    /// </summary>
    private static bool SembrarUsuariosIniciales(ServiceProvider servicios)
    {
        try
        {
            var credenciales = servicios.GetRequiredService<UsuarioSeeder>()
                .EjecutarSiHaceFalta()
                .GetAwaiter()
                .GetResult();

            if (credenciales.Count == 0)
                return true;

            var texto = string.Join(
                Environment.NewLine + Environment.NewLine,
                credenciales.Select(c =>
                    $"Rol: {c.Rol}{Environment.NewLine}" +
                    $"Email: {c.Email}{Environment.NewLine}" +
                    $"Contraseña: {c.Password}"));

            MessageBox.Show(
                "Se crearon los usuarios iniciales. Guardá estas credenciales: " +
                "no se vuelven a mostrar." + Environment.NewLine + Environment.NewLine + texto,
                "Primer arranque", MessageBoxButtons.OK, MessageBoxIcon.Information);

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudieron crear los usuarios iniciales:{Environment.NewLine}{ex.Message}",
                "Error de semilla", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private static ServiceProvider ConstruirServicios(IConfiguration configuracion)
    {
        int minutosInactividad = configuracion.GetValue("Sesion:MinutosInactividad",
            SesionOptions.MinutosPorDefecto);

        return new ServiceCollection()
            .AddSingleton(configuracion)
            .AddSingleton(new DatabaseOptions(configuracion.GetConnectionString("Default")!))
            .AddSingleton(new SesionOptions(minutosInactividad))
            .AddSingleton<IClock, SystemClock>()
            .AddSingleton<Db>()
            .AddSingleton<CurrentUser>()
            .AddSingleton<PasswordHasher>()
            .AddSingleton<UsuarioSeeder>()

            // US-04 — listado de oficinas
            .AddSingleton<ListarOficinasHandler>()

            // US-25 — login
            .AddSingleton<LoginValidator>()
            .AddSingleton<LoginHandler>()

            .AddTransient<LoginForm>()
            .AddTransient<Form1>()
            .BuildServiceProvider();
    }
}
