using CoworkApp.Core.Modelos;

namespace CoworkApp.Forms;

public sealed class EmpleadoForm : Form
{
    private readonly bool _esNuevo;
    private readonly TextBox _nombre = new();
    private readonly TextBox _apellido = new();
    private readonly TextBox _email = new();
    private readonly TextBox _telefono = new();
    private readonly TextBox _contrasena = new();
    private readonly ComboBox _edificio = new();
    private readonly ComboBox _estado = new();

    public EmpleadoForm(IReadOnlyList<Edificio> edificios, EmpleadoDetalle? empleado)
    {
        ArgumentNullException.ThrowIfNull(edificios);

        _esNuevo = empleado is null;

        Text = _esNuevo ? "Registrar empleado" : "Editar empleado";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 400);

        ConstruirControles(edificios, empleado);
    }

    public string Nombre => _nombre.Text;

    public string Apellido => _apellido.Text;

    public string Email => _email.Text;

    public string Telefono => _telefono.Text;

    public string Contrasena => _contrasena.Text;

    public string Estado => _estado.SelectedItem?.ToString() ?? string.Empty;

    public int IdEdificio => (_edificio.SelectedItem as Edificio)?.IdEdificio ?? 0;

    private void ConstruirControles(IReadOnlyList<Edificio> edificios, EmpleadoDetalle? empleado)
    {
        var contenedor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(24)
        };
        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var fila = 0; fila < 7; fila++)
        {
            contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        }

        contenedor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        foreach (var campo in new Control[] { _nombre, _apellido, _email, _telefono, _contrasena })
        {
            campo.Dock = DockStyle.Fill;
        }

        _contrasena.UseSystemPasswordChar = true;

        _edificio.Dock = DockStyle.Fill;
        _edificio.DropDownStyle = ComboBoxStyle.DropDownList;
        _edificio.DisplayMember = nameof(Edificio.Nombre);
        foreach (var edificio in edificios)
        {
            _edificio.Items.Add(edificio);
        }

        _estado.Dock = DockStyle.Fill;
        _estado.DropDownStyle = ComboBoxStyle.DropDownList;
        _estado.Items.AddRange(["Activo", "Inactivo"]);

        AgregarFila(contenedor, 0, "Nombre", _nombre);
        AgregarFila(contenedor, 1, "Apellido", _apellido);
        AgregarFila(contenedor, 2, "Email", _email);
        AgregarFila(contenedor, 3, "Telefono", _telefono);
        AgregarFila(contenedor, 4, _esNuevo ? "Contrasena" : "Nueva contrasena", _contrasena);
        AgregarFila(contenedor, 5, "Edificio", _edificio);
        AgregarFila(contenedor, 6, "Estado", _estado);

        if (_esNuevo)
        {
            _edificio.SelectedIndex = 0;
            _estado.SelectedItem = "Activo";
            _estado.Enabled = false;
        }
        else
        {
            _nombre.Text = empleado!.Nombre;
            _apellido.Text = empleado.Apellido;
            _email.Text = empleado.Email;
            _telefono.Text = empleado.Telefono ?? string.Empty;
            _estado.SelectedItem = empleado.Estado;
            SeleccionarEdificio(empleado.IdEdificio);
        }

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
        contenedor.Controls.Add(acciones, 0, 7);
        contenedor.SetColumnSpan(acciones, 2);

        Controls.Add(contenedor);

        AcceptButton = aceptar;
        CancelButton = cancelar;
    }

    private void SeleccionarEdificio(int idEdificio)
    {
        for (var indice = 0; indice < _edificio.Items.Count; indice++)
        {
            if (_edificio.Items[indice] is Edificio edificio && edificio.IdEdificio == idEdificio)
            {
                _edificio.SelectedIndex = indice;
                return;
            }
        }

        if (_edificio.Items.Count > 0)
        {
            _edificio.SelectedIndex = 0;
        }
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
