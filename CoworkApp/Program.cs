using System.Text;
using System.Text.Json;
using CoworkApp.Configuration;
using CoworkApp.Core.Datos;
using CoworkApp.Core.Desarrollo;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Seguridad;
using CoworkApp.Core.Servicios;
using CoworkApp.Forms;

namespace CoworkApp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        AppConfig configuracion;
        try
        {
            configuracion = AppConfig.Cargar();
        }
        catch (FileNotFoundException)
        {
            MessageBox.Show(
                "No se encontro el archivo appsettings.json en el directorio de salida." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Copia el archivo:{Environment.NewLine}" +
                $"CoworkApp{Path.DirectorySeparatorChar}appsettings.example.json" +
                $"{Environment.NewLine}hacia:{Environment.NewLine}" +
                $"CoworkApp{Path.DirectorySeparatorChar}appsettings.json" +
                $"{Environment.NewLine}{Environment.NewLine}" +
                "Luego completa la cadena de conexion a la base cowork.",
                "Configuracion faltante",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or IOException)
        {
            MessageBox.Show(
                $"No se pudo cargar appsettings.json.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Configuracion invalida",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var db = new Db(configuracion.ConnectionString);

        if (configuracion.SeedDesarrollo && !EjecutarSeeder(db))
        {
            return;
        }

        var sesion = new SesionActual();
        var autenticacion = new AutenticacionServicio(new UsuarioRepositorio(db));

        while (true)
        {
            using (var login = new LoginForm(autenticacion, sesion))
            {
                if (login.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
            }

            using (var principal = new MainForm(sesion, db))
            {
                principal.ShowDialog();

                if (!principal.CerrarSesionSolicitado)
                {
                    return;
                }
            }

            sesion.Cerrar();
        }
    }

    private static bool EjecutarSeeder(Db db)
    {
        try
        {
            var resultado = new DesarrolloSeeder(db).EjecutarAsync().GetAwaiter().GetResult();

            if (resultado.Ejecutado)
            {
                MessageBox.Show(
                    ConstruirMensajeCredenciales(resultado),
                    "Datos demo de desarrollo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo ejecutar el seeder de desarrollo.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Error del seeder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
    }

    private static string ConstruirMensajeCredenciales(ResultadoDesarrolloSeeder resultado)
    {
        var mensaje = new StringBuilder();
        mensaje.AppendLine("Se cargaron datos demo (solo para desarrollo).");
        mensaje.AppendLine();
        mensaje.AppendLine("Credenciales demo:");

        foreach (var credencial in resultado.Credenciales)
        {
            mensaje.AppendLine($"{credencial.Rol}: {credencial.Email} / {credencial.Contrasena}");
        }

        return mensaje.ToString().TrimEnd();
    }
}
