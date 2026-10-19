using CoworkApp.Core.Modelos;
using CoworkApp.Core.Servicios;
using CoworkApp.Forms;

namespace CoworkApp.Views;

public sealed class EdificiosView : UserControl
{
    private readonly EdificioServicio _servicio;
    private readonly DataGridView _grilla = new();
    private readonly Button _btnNuevo = new() { Text = "Nuevo", AutoSize = true };
    private readonly Button _btnEditar = new() { Text = "Editar", AutoSize = true };
    private readonly Button _btnBaja = new() { Text = "Dar de baja", AutoSize = true };
    private readonly Button _btnActualizar = new() { Text = "Actualizar", AutoSize = true };

    public EdificiosView(EdificioServicio servicio)
    {
        _servicio = servicio;
        Dock = DockStyle.Fill;

        ConstruirControles();

        Load += async (_, _) => await CargarAsync();
    }

    private IReadOnlyList<Control> Controles => [_btnNuevo, _btnEditar, _btnBaja, _btnActualizar];

    private Edificio? Seleccionado => _grilla.SelectedRows.Count > 0 ? _grilla.SelectedRows[0].Tag as Edificio : null;

    private void ConstruirControles()
    {
        _grilla.Dock = DockStyle.Fill;
        _grilla.ReadOnly = true;
        _grilla.AllowUserToAddRows = false;
        _grilla.AllowUserToDeleteRows = false;
        _grilla.AllowUserToResizeRows = false;
        _grilla.MultiSelect = false;
        _grilla.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grilla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grilla.RowHeadersVisible = false;
        _grilla.AutoGenerateColumns = false;
        _grilla.Columns.Add(CrearColumna("Id", "ID", 60, DataGridViewAutoSizeColumnMode.None));
        _grilla.Columns.Add(CrearColumna("Nombre", "Nombre", 200));
        _grilla.Columns.Add(CrearColumna("Direccion", "Direccion", 240));
        _grilla.Columns.Add(CrearColumna("Estado", "Estado", 100));
        _grilla.CellDoubleClick += async (_, args) =>
        {
            if (args.RowIndex >= 0)
            {
                await EditarAsync();
            }
        };

        _btnNuevo.Click += async (_, _) => await NuevoAsync();
        _btnEditar.Click += async (_, _) => await EditarAsync();
        _btnBaja.Click += async (_, _) => await DarDeBajaAsync();
        _btnActualizar.Click += async (_, _) => await CargarAsync();

        var barra = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(4),
            WrapContents = false
        };
        barra.Controls.Add(_btnNuevo);
        barra.Controls.Add(_btnEditar);
        barra.Controls.Add(_btnBaja);
        barra.Controls.Add(_btnActualizar);

        Controls.Add(_grilla);
        Controls.Add(barra);
    }

    private async Task CargarAsync()
    {
        await VistaAsync.EjecutarAsync(
            this,
            Controles,
            async () =>
            {
                var edificios = await _servicio.ListarAsync();
                _grilla.Rows.Clear();
                foreach (var edificio in edificios)
                {
                    var indice = _grilla.Rows.Add(edificio.IdEdificio, edificio.Nombre, edificio.Direccion, edificio.Estado);
                    _grilla.Rows[indice].Tag = edificio;
                }
            },
            "No se pudieron cargar los edificios.");
    }

    private async Task NuevoAsync()
    {
        using var formulario = new EdificioEditarForm(null);
        if (formulario.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var creado = await VistaAsync.EjecutarAsync(
            this,
            Controles,
            () => _servicio.CrearAsync(formulario.Nombre, formulario.Direccion),
            "No se pudo registrar el edificio.");

        if (creado)
        {
            await CargarAsync();
        }
    }

    private async Task EditarAsync()
    {
        var edificio = Seleccionado;
        if (edificio is null)
        {
            return;
        }

        using var formulario = new EdificioEditarForm(edificio);
        if (formulario.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var actualizado = await VistaAsync.EjecutarAsync(
            this,
            Controles,
            () => _servicio.ActualizarAsync(edificio.IdEdificio, formulario.Nombre, formulario.Direccion, formulario.Estado),
            "No se pudo actualizar el edificio.");

        if (actualizado)
        {
            await CargarAsync();
        }
    }

    private async Task DarDeBajaAsync()
    {
        var edificio = Seleccionado;
        if (edificio is null)
        {
            return;
        }

        var confirmacion = MessageBox.Show(
            this,
            $"El edificio '{edificio.Nombre}' pasara al estado Inactivo.\n\n¿Confirmas la baja?",
            "Dar de baja edificio",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirmacion != DialogResult.Yes)
        {
            return;
        }

        var dadoDeBaja = await VistaAsync.EjecutarAsync(
            this,
            Controles,
            () => _servicio.DarDeBajaAsync(edificio.IdEdificio),
            "No se pudo dar de baja el edificio.");

        if (dadoDeBaja)
        {
            await CargarAsync();
        }
    }

    private static DataGridViewTextBoxColumn CrearColumna(string nombre, string encabezado, int ancho, DataGridViewAutoSizeColumnMode modo = DataGridViewAutoSizeColumnMode.Fill)
    {
        return new DataGridViewTextBoxColumn
        {
            Name = nombre,
            HeaderText = encabezado,
            Width = ancho,
            AutoSizeMode = modo,
            ReadOnly = true
        };
    }
}
