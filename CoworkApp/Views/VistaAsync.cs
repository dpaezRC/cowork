using CoworkApp.Core.Validacion;

namespace CoworkApp.Views;

internal static class VistaAsync
{
    public static async Task<bool> EjecutarAsync(
        Control contenedor,
        IReadOnlyList<Control> controles,
        Func<Task> operacion,
        string mensajeGenerico)
    {
        EstablecerHabilitado(controles, false);
        contenedor.UseWaitCursor = true;

        try
        {
            await operacion();
            return true;
        }
        catch (ReglaNegocioException ex)
        {
            MessageBox.Show(contenedor, ex.Message, "Operacion no permitida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception)
        {
            MessageBox.Show(contenedor, mensajeGenerico, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            EstablecerHabilitado(controles, true);
            contenedor.UseWaitCursor = false;
        }

        return false;
    }

    private static void EstablecerHabilitado(IReadOnlyList<Control> controles, bool habilitado)
    {
        foreach (var control in controles)
        {
            control.Enabled = habilitado;
        }
    }
}
