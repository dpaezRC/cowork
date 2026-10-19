using CoworkApp.Core.Modelos;

namespace CoworkApp.Forms;

public sealed class OficinaEditarForm : Form
{
    private readonly TextBox _nombre = new();
    private readonly TextBox _capacidad = new();
    private readonly TextBox _tarifa = new();
    private readonly DateTimePicker _apertura = new();
    private readonly DateTimePicker _cierre = new();
    private readonly ComboBox _estado = new();

    public OficinaEditarForm(Oficina oficina)
    {
        Text = "Editar oficina";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 340);

        ConstruirControles(oficina);
    }

    public string Nombre => _nombre.Text;

    public string Capacidad => _capacidad.Text;

    public string Tarifa => _tarifa.Text;

    public TimeOnly HoraApertura => TimeOnly.FromDateTime(_apertura.Value);

    public TimeOnly HoraCierre => TimeOnly.FromDateTime(_cierre.Value);

    public string Estado => _estado.SelectedItem?.ToString() ?? string.Empty;

    private void ConstruirControles(Oficina oficina)
    {
        var contenedor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(24)
        };
        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var fila = 0; fila < 6; fila++)
        {
            contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        }

        contenedor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _nombre.Dock = DockStyle.Fill;
        _nombre.Text = oficina.Nombre;

        _capacidad.Dock = DockStyle.Fill;
        _capacidad.Text = oficina.Capacidad.ToString("0");

        _tarifa.Dock = DockStyle.Fill;
        _tarifa.Text = oficina.TarifaHora.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        ConfigurarHora(_apertura, oficina.HoraApertura);
        ConfigurarHora(_cierre, oficina.HoraCierre);

        _estado.Dock = DockStyle.Fill;
        _estado.DropDownStyle = ComboBoxStyle.DropDownList;
        _estado.Items.AddRange(["Habilitada", "Deshabilitada", "Baja"]);
        _estado.SelectedItem = oficina.Estado;

        contenedor.Controls.Add(CrearEtiqueta("Nombre"), 0, 0);
        contenedor.Controls.Add(_nombre, 1, 0);
        contenedor.Controls.Add(CrearEtiqueta("Capacidad"), 0, 1);
        contenedor.Controls.Add(_capacidad, 1, 1);
        contenedor.Controls.Add(CrearEtiqueta("Tarifa por hora"), 0, 2);
        contenedor.Controls.Add(_tarifa, 1, 2);
        contenedor.Controls.Add(CrearEtiqueta("Hora apertura"), 0, 3);
        contenedor.Controls.Add(_apertura, 1, 3);
        contenedor.Controls.Add(CrearEtiqueta("Hora cierre"), 0, 4);
        contenedor.Controls.Add(_cierre, 1, 4);
        contenedor.Controls.Add(CrearEtiqueta("Estado"), 0, 5);
        contenedor.Controls.Add(_estado, 1, 5);

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
        contenedor.Controls.Add(acciones, 0, 6);
        contenedor.SetColumnSpan(acciones, 2);

        Controls.Add(contenedor);

        AcceptButton = aceptar;
        CancelButton = cancelar;
    }

    private static void ConfigurarHora(DateTimePicker selector, TimeOnly hora)
    {
        selector.Dock = DockStyle.Fill;
        selector.Format = DateTimePickerFormat.Custom;
        selector.CustomFormat = "HH:mm";
        selector.ShowUpDown = true;
        selector.Value = DateTime.Today.Add(hora.ToTimeSpan());
    }

    private static Label CrearEtiqueta(string texto)
    {
        return new Label
        {
            Text = texto,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }
}
