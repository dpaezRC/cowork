using CoworkApp.Core.Dominio;
using CoworkApp.Core.Modelos;

namespace CoworkApp.Forms;

public sealed class UsuarioForm : Form
{
    private readonly bool _esNuevo;
    private readonly TextBox _nombre = new();
    private readonly TextBox _apellido = new();
    private readonly TextBox _email = new();
    private readonly TextBox _telefono = new();
    private readonly TextBox _contrasena = new();
    private readonly ComboBox _rol = new();
    private readonly ComboBox _estado = new();

    public UsuarioForm(IReadOnlyList<string> rolesGestionables, Usuario? usuario)
    {
        ArgumentNullException.ThrowIfNull(rolesGestionables);

        _esNuevo = usuario is null;

        Text = _esNuevo ? "Registrar usuario" : "Editar usuario";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 420);

        ConstruirControles(rolesGestionables, usuario);
    }

    public string Nombre => _nombre.Text;

    public string Apellido => _apellido.Text;

    public string Email => _email.Text;

    public string Telefono => _telefono.Text;

    public string Contrasena => _contrasena.Text;

    public string Rol => _rol.SelectedItem?.ToString() ?? string.Empty;

    public string Estado => _estado.SelectedItem?.ToString() ?? string.Empty;

    private void ConstruirControles(IReadOnlyList<string> rolesGestionables, Usuario? usuario)
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

        _rol.Dock = DockStyle.Fill;
        _rol.DropDownStyle = ComboBoxStyle.DropDownList;

        _estado.Dock = DockStyle.Fill;
        _estado.DropDownStyle = ComboBoxStyle.DropDownList;
        _estado.Items.AddRange([.. Estados.Usuario]);

        AgregarFila(contenedor, 0, "Nombre", _nombre);
        AgregarFila(contenedor, 1, "Apellido", _apellido);
        AgregarFila(contenedor, 2, "Email", _email);
        AgregarFila(contenedor, 3, "Telefono", _telefono);
        AgregarFila(contenedor, 4, _esNuevo ? "Contrasena" : "Nueva contrasena", _contrasena);
        AgregarFila(contenedor, 5, "Rol", _rol);
        AgregarFila(contenedor, 6, "Estado", _estado);

        if (_esNuevo)
        {
            foreach (var rol in rolesGestionables)
            {
                _rol.Items.Add(rol);
            }

            if (_rol.Items.Count > 0)
            {
                _rol.SelectedIndex = 0;
            }

            _estado.SelectedItem = Estados.Activo;
            _estado.Enabled = false;
        }
        else
        {
            _nombre.Text = usuario!.Nombre;
            _apellido.Text = usuario.Apellido;
            _email.Text = usuario.Email;
            _telefono.Text = usuario.Telefono ?? string.Empty;
            _rol.Items.Add(usuario.Rol);
            _rol.SelectedIndex = 0;
            _rol.Enabled = false;
            _estado.SelectedItem = usuario.Estado;
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

    private static void AgregarFila(TableLayoutPanel contenedor, int fila, string etiqueta, Control control)
    {
        contenedor.Controls.Add(
            new Label { Text = etiqueta, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft },
            0,
            fila);
        contenedor.Controls.Add(control, 1, fila);
    }
}
