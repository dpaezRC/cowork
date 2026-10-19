using CoworkApp.Core.Modelos;
using CoworkApp.Core.Servicios;
using CoworkApp.Core.Validacion;
using CoworkApp.Forms;

namespace CoworkApp.Views;

public sealed class OficinasView : UserControl
{
    private readonly EdificioServicio _edificioServicio;
    private readonly PisoServicio _pisoServicio;
    private readonly OficinaServicio _oficinaServicio;
    private readonly CascadaServicio _cascadaServicio;
    private readonly DataGridView _grilla = new();
    private readonly Button _btnNuevo = new() { Text = "Nueva (cascada)", AutoSize = true };
    private readonly Button _btnEditar = new() { Text = "Editar", AutoSize = true };
    private readonly Button _btnBaja = new() { Text = "Dar de baja", AutoSize = true };
    private readonly Button _btnActualizar = new() { Text = "Actualizar", AutoSize = true };

    public OficinasView(
        EdificioServicio edificioServicio,
        PisoServicio pisoServicio,
        OficinaServicio oficinaServicio,
        CascadaServicio cascadaServicio)
    {
        _edificioServicio = edificioServicio;
        _pisoServicio = pisoServicio;
        _oficinaServicio = oficinaServicio;
        _cascadaServicio = cascadaServicio;
        Dock = DockStyle.Fill;

        ConstruirControles();

        Load += async (_, _) => await CargarAsync();
    }

    private IReadOnlyList<Control> Controles => [_btnNuevo, _btnEditar, _btnBaja, _btnActualizar];

    private OficinaDetalle? Seleccionada => _grilla.SelectedRows.Count > 0 ? _grilla.SelectedRows[0].Tag as OficinaDetalle : null;

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
        _grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 60, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, ReadOnly = true });
        _grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Oficina", HeaderText = "Oficina", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
        _grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Piso", HeaderText = "Piso", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
        _grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Edificio", HeaderText = "Edificio", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
        _grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Capacidad", HeaderText = "Capacidad", Width = 90, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, ReadOnly = true });
        _grilla.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Tarifa",
            HeaderText = "Tarifa",
            Width = 100,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            ReadOnly = true,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        _grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Horario", HeaderText = "Horario", Width = 120, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, ReadOnly = true });
        _grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", Width = 110, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, ReadOnly = true });
        _grilla.CellDoubleClick += async (_, args) =>
        {
            if (args.RowIndex >= 0)
            {
                await EditarAsync();
            }
        };

        _btnNuevo.Click += async (_, _) => await NuevaAsync();
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
                var oficinas = await _oficinaServicio.ListarDetalleAsync();
                _grilla.Rows.Clear();
                foreach (var oficina in oficinas)
                {
                    var horario = $"{oficina.HoraApertura:HH:mm}-{oficina.HoraCierre:HH:mm}";
                    var indice = _grilla.Rows.Add(
                        oficina.IdOficina,
                        oficina.Nombre,
                        oficina.Piso,
                        oficina.Edificio,
                        oficina.Capacidad,
                        oficina.TarifaHora,
                        horario,
                        oficina.Estado);
                    _grilla.Rows[indice].Tag = oficina;
                }
            },
            "No se pudieron cargar las oficinas.");
    }

    private async Task NuevaAsync()
    {
        using var formulario = new OficinaCascadaForm(_edificioServicio, _pisoServicio, _oficinaServicio, _cascadaServicio);
        if (formulario.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await CargarAsync();
    }

    private async Task EditarAsync()
    {
        var detalle = Seleccionada;
        if (detalle is null)
        {
            return;
        }

        var oficina = await _oficinaServicio.ObtenerAsync(detalle.IdOficina);
        if (oficina is null)
        {
            MessageBox.Show(this, OficinaServicio.MensajeNoExiste, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var formulario = new OficinaEditarForm(oficina);
        if (formulario.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var actualizado = await VistaAsync.EjecutarAsync(
            this,
            Controles,
            async () =>
            {
                var capacidad = Validador.Capacidad(formulario.Capacidad);
                var tarifa = Validador.Tarifa(formulario.Tarifa);
                await _oficinaServicio.ActualizarAsync(
                    oficina.IdOficina,
                    formulario.Nombre,
                    capacidad,
                    tarifa,
                    formulario.HoraApertura,
                    formulario.HoraCierre,
                    formulario.Estado);
            },
            "No se pudo actualizar la oficina.");

        if (actualizado)
        {
            await CargarAsync();
        }
    }

    private async Task DarDeBajaAsync()
    {
        var detalle = Seleccionada;
        if (detalle is null)
        {
            return;
        }

        var confirmacion = MessageBox.Show(
            this,
            $"La oficina '{detalle.Nombre}' pasara al estado Baja.\n\n¿Confirmas la baja?",
            "Dar de baja oficina",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirmacion != DialogResult.Yes)
        {
            return;
        }

        var dadoDeBaja = await VistaAsync.EjecutarAsync(
            this,
            Controles,
            () => _oficinaServicio.DarDeBajaAsync(detalle.IdOficina),
            "No se pudo dar de baja la oficina.");

        if (dadoDeBaja)
        {
            await CargarAsync();
        }
    }
}
