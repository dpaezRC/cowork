using CoworkApp.Core.Modelos;

namespace CoworkApp.Forms;

public sealed class PisoEditarForm : Form
{
    private readonly bool _esNuevo;
    private readonly ComboBox _edificio = new();
    private readonly TextBox _nombre = new();
    private readonly ComboBox _estado = new();

    public PisoEditarForm(Piso? piso, IReadOnlyList<Edificio> edificios, int? idEdificioSeleccionado)
    {
        _esNuevo = piso is null;

        Text = _esNuevo ? "Nuevo piso" : "Editar piso";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 220);

        ConstruirControles(piso, edificios, idEdificioSeleccionado);
    }

    public int? IdEdificio => _edificio.SelectedItem is Edificio edificio ? edificio.IdEdificio : null;

    public string Nombre => _nombre.Text;

    public string Estado => _estado.SelectedItem?.ToString() ?? string.Empty;

    private void ConstruirControles(Piso? piso, IReadOnlyList<Edificio> edificios, int? idEdificioSeleccionado)
    {
        var contenedor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(24)
        };
        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _edificio.Dock = DockStyle.Fill;
        _edificio.DropDownStyle = ComboBoxStyle.DropDownList;
        _edificio.DisplayMember = nameof(Edificio.Nombre);
        foreach (var edificio in edificios)
        {
            _edificio.Items.Add(edificio);
        }

        _nombre.Dock = DockStyle.Fill;
        _estado.Dock = DockStyle.Fill;
        _estado.DropDownStyle = ComboBoxStyle.DropDownList;
        _estado.Items.AddRange(["Activo", "Inactivo"]);

        contenedor.Controls.Add(CrearEtiqueta("Edificio"), 0, 0);
        contenedor.Controls.Add(_edificio, 1, 0);
        contenedor.Controls.Add(CrearEtiqueta("Nombre"), 0, 1);
        contenedor.Controls.Add(_nombre, 1, 1);
        contenedor.Controls.Add(CrearEtiqueta("Estado"), 0, 2);
        contenedor.Controls.Add(_estado, 1, 2);

        if (_esNuevo)
        {
            _estado.SelectedItem = "Activo";
            _estado.Enabled = false;
            SeleccionarEdificio(idEdificioSeleccionado);
        }
        else
        {
            SeleccionarEdificio(piso!.IdEdificio);
            _edificio.Enabled = false;
            _nombre.Text = piso.Nombre;
            _estado.SelectedItem = piso.Estado;
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
        contenedor.Controls.Add(acciones, 0, 3);
        contenedor.SetColumnSpan(acciones, 2);

        Controls.Add(contenedor);

        AcceptButton = aceptar;
        CancelButton = cancelar;
    }

    private void SeleccionarEdificio(int? idEdificio)
    {
        if (idEdificio is null)
        {
            return;
        }

        foreach (var item in _edificio.Items)
        {
            if (item is Edificio edificio && edificio.IdEdificio == idEdificio.Value)
            {
                _edificio.SelectedItem = item;
                return;
            }
        }
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
