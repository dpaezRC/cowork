namespace CoworkApp.Views;

public sealed class ModuloPendienteControl : UserControl
{
    public ModuloPendienteControl(string nombreModulo)
    {
        Dock = DockStyle.Fill;

        var titulo = new Label
        {
            Text = nombreModulo,
            Dock = DockStyle.Top,
            Height = 48,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var estado = new Label
        {
            Text = "En construcción",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = SystemColors.GrayText
        };

        Controls.Add(estado);
        Controls.Add(titulo);
    }
}
