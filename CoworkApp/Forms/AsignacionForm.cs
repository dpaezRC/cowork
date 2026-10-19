using CoworkApp.Core.Modelos;

namespace CoworkApp.Forms;

public sealed class AsignacionForm : Form
{
    private readonly ComboBox _oficina = new();
    private readonly TextBox _horaInicio = new();
    private readonly TextBox _horaFin = new();

    public AsignacionForm(IReadOnlyList<Oficina> oficinas)
    {
        ArgumentNullException.ThrowIfNull(oficinas);

        Text = "Asignar oficina";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 240);

        ConstruirControles(oficinas);
    }

    public int IdOficina => (_oficina.SelectedItem as Oficina)?.IdOficina ?? 0;

    public string HoraInicio => _horaInicio.Text;

    public string HoraFin => _horaFin.Text;

    private void ConstruirControles(IReadOnlyList<Oficina> oficinas)
    {
        var contenedor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(24)
        };
        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var fila = 0; fila < 3; fila++)
        {
            contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        }

        contenedor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _oficina.Dock = DockStyle.Fill;
        _oficina.DropDownStyle = ComboBoxStyle.DropDownList;
        _oficina.DisplayMember = nameof(Oficina.Nombre);
        foreach (var oficina in oficinas)
        {
            _oficina.Items.Add(oficina);
        }

        if (_oficina.Items.Count > 0)
        {
            _oficina.SelectedIndex = 0;
        }

        _horaInicio.Dock = DockStyle.Fill;
        _horaInicio.Text = "09:00";

        _horaFin.Dock = DockStyle.Fill;
        _horaFin.Text = "17:00";

        AgregarFila(contenedor, 0, "Oficina", _oficina);
        AgregarFila(contenedor, 1, "Hora de inicio", _horaInicio);
        AgregarFila(contenedor, 2, "Hora de fin", _horaFin);

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
        contenedor.Controls.Add(acciones, 0, 3);
        contenedor.SetColumnSpan(acciones, 2);

        Controls.Add(contenedor);

        AcceptButton = aceptar;
        CancelButton = cancelar;
    }

    private static void AgregarFila(TableLayoutPanel contenedor, int fila, string etiqueta, Control control)
    {
        contenedor.Controls.Add(
            new Label { Text = etiqueta, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft },
            0,
            fila);
        contenedor.Controls.Add(control, 1, fila);
    }
}
