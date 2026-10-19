namespace CoworkApp.Forms;

public sealed class EntradaTextoForm : Form
{
    private readonly TextBox _valor = new();

    public EntradaTextoForm(string titulo, string etiqueta, string? valorInicial = null)
    {
        Text = titulo;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(400, 150);

        var contenedor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(24)
        };
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _valor.Dock = DockStyle.Fill;
        _valor.Text = valorInicial ?? string.Empty;

        var aceptar = new Button { Text = "Aceptar", DialogResult = DialogResult.OK, AutoSize = true, Padding = new Padding(16, 4, 16, 4) };
        var cancelar = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, AutoSize = true, Padding = new Padding(16, 4, 16, 4) };

        var acciones = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        acciones.Controls.Add(aceptar);
        acciones.Controls.Add(cancelar);

        contenedor.Controls.Add(new Label
        {
            Text = etiqueta,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        contenedor.Controls.Add(_valor, 0, 1);
        contenedor.Controls.Add(acciones, 0, 2);

        Controls.Add(contenedor);

        AcceptButton = aceptar;
        CancelButton = cancelar;
    }

    public string Valor => _valor.Text;
}
