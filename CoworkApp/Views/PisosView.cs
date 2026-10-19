using CoworkApp.Core.Modelos;
using CoworkApp.Core.Servicios;
using CoworkApp.Forms;

namespace CoworkApp.Views;

public sealed class PisosView : UserControl
{
    private readonly EdificioServicio _edificioServicio;
    private readonly PisoServicio _pisoServicio;
    private readonly ComboBox _filtroEdificio = new();
    private readonly DataGridView _grilla = new();
    private readonly Button _btnNuevo = new() { Text = "Nuevo", AutoSize = true };
    private readonly Button _btnEditar = new() { Text = "Editar", AutoSize = true };
    private readonly Button _btnBaja = new() { Text = "Dar de baja", AutoSize = true };
    private readonly Button _btnActualizar = new() { Text = "Actualizar", AutoSize = true };

    private List<Edificio> _edificios = [];

    public PisosView(EdificioServicio edificioServicio, PisoServicio pisoServicio)
    {
        _edificioServicio = edificioServicio;
        _pisoServicio = pisoServicio;
        Dock = DockStyle.Fill;

        ConstruirControles();

        Load += async (_, _) => await CargarAsync();
    }

    private IReadOnlyList<Control> Controles => [_btnNuevo, _btnEditar, _btnBaja, _btnActualizar, _filtroEdificio];

    private int? IdEdificioSeleccionado => _filtroEdificio.SelectedItem is Edificio edificio ? edificio.IdEdificio : null;

    private Piso? Seleccionado => _grilla.SelectedRows.Count > 0 ? _grilla.SelectedRows[0].Tag as Piso : null;

    private void ConstruirControles()
    {
        _filtroEdificio.DropDownStyle = ComboBoxStyle.DropDownList;
        _filtroEdificio.DisplayMember = nameof(Edificio.Nombre);
        _filtroEdificio.Width = 260;
        _filtroEdificio.SelectedIndexChanged += FiltroCambiado;

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
        _grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 70, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, ReadOnly = true });
        _grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Piso", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
        _grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", Width = 120, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, ReadOnly = true });
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
            Height = 44,
            Padding = new Padding(4),
            WrapContents = false
        };
        barra.Controls.Add(new Label { Text = "Edificio:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 8, 4, 0) });
        barra.Controls.Add(_filtroEdificio);
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
                var seleccion = IdEdificioSeleccionado;
                _edificios = await _edificioServicio.ListarAsync();

                _filtroEdificio.SelectedIndexChanged -= FiltroCambiado;
                _filtroEdificio.Items.Clear();
                foreach (var edificio in _edificios)
                {
                    _filtroEdificio.Items.Add(edificio);
                }

                var indice = 0;
                for (var i = 0; i < _edificios.Count; i++)
                {
                    if (_edificios[i].IdEdificio == seleccion)
                    {
                        indice = i;
                        break;
                    }
                }

                if (_edificios.Count > 0)
                {
                    _filtroEdificio.SelectedIndex = indice;
                }

                _filtroEdificio.SelectedIndexChanged += FiltroCambiado;
            },
            "No se pudieron cargar los edificios.");

        await CargarPisosAsync();
    }

    private async void FiltroCambiado(object? sender, EventArgs e)
    {
        await CargarPisosAsync();
    }

    private async Task CargarPisosAsync()
    {
        await VistaAsync.EjecutarAsync(
            this,
            Controles,
            async () =>
            {
                _grilla.Rows.Clear();
                if (IdEdificioSeleccionado is not int idEdificio)
                {
                    return;
                }

                var pisos = await _pisoServicio.ListarPorEdificioAsync(idEdificio);
                foreach (var piso in pisos)
                {
                    var indice = _grilla.Rows.Add(piso.IdPiso, piso.Nombre, piso.Estado);
                    _grilla.Rows[indice].Tag = piso;
                }
            },
            "No se pudieron cargar los pisos.");
    }

    private async Task NuevoAsync()
    {
        if (_edificios.Count == 0)
        {
            MessageBox.Show(this, "No hay edificios registrados. Registra un edificio primero.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var formulario = new PisoEditarForm(null, _edificios, IdEdificioSeleccionado);
        if (formulario.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (formulario.IdEdificio is not int idEdificio)
        {
            MessageBox.Show(this, "Debes seleccionar un edificio.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var creado = await VistaAsync.EjecutarAsync(
            this,
            Controles,
            () => _pisoServicio.CrearAsync(idEdificio, formulario.Nombre),
            "No se pudo registrar el piso.");

        if (creado)
        {
            await CargarAsync();
            SeleccionarEdificio(idEdificio);
        }
    }

    private async Task EditarAsync()
    {
        var piso = Seleccionado;
        if (piso is null)
        {
            return;
        }

        using var formulario = new PisoEditarForm(piso, _edificios, piso.IdEdificio);
        if (formulario.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var actualizado = await VistaAsync.EjecutarAsync(
            this,
            Controles,
            () => _pisoServicio.ActualizarAsync(piso.IdPiso, formulario.Nombre, formulario.Estado),
            "No se pudo actualizar el piso.");

        if (actualizado)
        {
            await CargarPisosAsync();
        }
    }

    private async Task DarDeBajaAsync()
    {
        var piso = Seleccionado;
        if (piso is null)
        {
            return;
        }

        var confirmacion = MessageBox.Show(
            this,
            $"El piso '{piso.Nombre}' pasara al estado Inactivo.\n\n¿Confirmas la baja?",
            "Dar de baja piso",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirmacion != DialogResult.Yes)
        {
            return;
        }

        var dadoDeBaja = await VistaAsync.EjecutarAsync(
            this,
            Controles,
            () => _pisoServicio.DarDeBajaAsync(piso.IdPiso),
            "No se pudo dar de baja el piso.");

        if (dadoDeBaja)
        {
            await CargarPisosAsync();
        }
    }

    private void SeleccionarEdificio(int idEdificio)
    {
        for (var i = 0; i < _filtroEdificio.Items.Count; i++)
        {
            if (_filtroEdificio.Items[i] is Edificio edificio && edificio.IdEdificio == idEdificio)
            {
                _filtroEdificio.SelectedIndex = i;
                return;
            }
        }
    }
}
