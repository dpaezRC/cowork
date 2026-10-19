using CoworkApp.Core.Modelos;
using CoworkApp.Core.Servicios;
using CoworkApp.Core.Validacion;
using CoworkApp.Forms;

namespace CoworkApp.Views;

public sealed class AsignacionesView : UserControl
{
    private readonly EmpleadoServicio _empleadoServicio;
    private readonly AsignacionServicio _asignacionServicio;
    private readonly OficinaServicio _oficinaServicio;
    private readonly ComboBox _empleado = new();
    private readonly DataGridView _grilla = new();
    private readonly Button _btnAsignar = new() { Text = "Asignar oficina", AutoSize = true };
    private readonly Button _btnQuitar = new() { Text = "Quitar", AutoSize = true };
    private readonly Button _btnActualizar = new() { Text = "Actualizar", AutoSize = true };

    private bool _cargando;

    public AsignacionesView(
        EmpleadoServicio empleadoServicio,
        AsignacionServicio asignacionServicio,
        OficinaServicio oficinaServicio)
    {
        _empleadoServicio = empleadoServicio;
        _asignacionServicio = asignacionServicio;
        _oficinaServicio = oficinaServicio;
        Dock = DockStyle.Fill;

        ConstruirControles();

        Load += async (_, _) => await CargarEmpleadosAsync();
    }

    private IReadOnlyList<Control> Controles => [_empleado, _btnAsignar, _btnQuitar, _btnActualizar];

    private EmpleadoDetalle? EmpleadoSeleccionado => (_empleado.SelectedItem as OpcionEmpleado)?.Detalle;

    private AsignacionDetalle? AsignacionSeleccionada => _grilla.SelectedRows.Count > 0 ? _grilla.SelectedRows[0].Tag as AsignacionDetalle : null;

    private void ConstruirControles()
    {
        _empleado.DropDownStyle = ComboBoxStyle.DropDownList;
        _empleado.Width = 280;
        _empleado.SelectedIndexChanged += async (_, _) => await CargarAsignacionesAsync();

        _btnAsignar.Click += async (_, _) => await AsignarAsync();
        _btnQuitar.Click += async (_, _) => await QuitarAsync();
        _btnActualizar.Click += async (_, _) => await CargarAsignacionesAsync();

        var barra = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(4),
            WrapContents = false
        };
        barra.Controls.Add(new Label { Text = "Empleado:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4) });
        barra.Controls.Add(_empleado);
        barra.Controls.Add(_btnAsignar);
        barra.Controls.Add(_btnQuitar);
        barra.Controls.Add(_btnActualizar);

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
        _grilla.Columns.Add(CrearColumna("Oficina", "Oficina", 200));
        _grilla.Columns.Add(CrearColumna("Edificio", "Edificio", 200));
        _grilla.Columns.Add(CrearColumna("Franja", "Franja", 140, DataGridViewAutoSizeColumnMode.None));

        Controls.Add(_grilla);
        Controls.Add(barra);
    }

    private async Task CargarEmpleadosAsync()
    {
        _cargando = true;
        try
        {
            await VistaAsync.EjecutarAsync(
                this,
                Controles,
                async () =>
                {
                    var empleados = await _empleadoServicio.ListarAsync();
                    _empleado.Items.Clear();
                    foreach (var empleado in empleados)
                    {
                        _empleado.Items.Add(new OpcionEmpleado(empleado, $"{empleado.Nombre} {empleado.Apellido} (ID {empleado.IdEmpleado})"));
                    }

                    if (_empleado.Items.Count > 0)
                    {
                        _empleado.SelectedIndex = 0;
                    }
                },
                "No se pudieron cargar los empleados.");
        }
        finally
        {
            _cargando = false;
        }

        await CargarAsignacionesAsync();
    }

    private async Task CargarAsignacionesAsync()
    {
        if (_cargando)
        {
            return;
        }

        _grilla.Rows.Clear();
        var empleado = EmpleadoSeleccionado;
        if (empleado is null)
        {
            return;
        }

        await VistaAsync.EjecutarAsync(
            this,
            Controles,
            async () =>
            {
                var asignaciones = await _asignacionServicio.ListarAsync(empleado.IdEmpleado);
                _grilla.Rows.Clear();
                foreach (var asignacion in asignaciones)
                {
                    var franja = $"{asignacion.HoraInicio:HH:mm}-{asignacion.HoraFin:HH:mm}";
                    var indice = _grilla.Rows.Add(asignacion.IdEmpleadoOficina, asignacion.Oficina, asignacion.Edificio, franja);
                    _grilla.Rows[indice].Tag = asignacion;
                }
            },
            "No se pudieron cargar las asignaciones.");
    }

    private async Task AsignarAsync()
    {
        var empleado = EmpleadoSeleccionado;
        if (empleado is null)
        {
            MessageBox.Show(this, "No hay empleados registrados.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var oficinas = await _oficinaServicio.ListarPorEdificioAsync(empleado.IdEdificio);
        if (oficinas.Count == 0)
        {
            MessageBox.Show(
                this,
                $"El edificio '{empleado.Edificio}' no tiene oficinas registradas.",
                "Aviso",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var formulario = new AsignacionForm(oficinas);
        if (formulario.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var creada = await VistaAsync.EjecutarAsync(
            this,
            Controles,
            async () =>
            {
                var inicio = Validador.Hora(formulario.HoraInicio);
                var fin = Validador.Hora(formulario.HoraFin);
                await _asignacionServicio.AsignarAsync(empleado.IdEmpleado, formulario.IdOficina, inicio, fin);
            },
            "No se pudo asignar la oficina.");

        if (creada)
        {
            await CargarAsignacionesAsync();
        }
    }

    private async Task QuitarAsync()
    {
        var asignacion = AsignacionSeleccionada;
        if (asignacion is null)
        {
            return;
        }

        var franja = $"{asignacion.HoraInicio:HH:mm}-{asignacion.HoraFin:HH:mm}";
        var confirmacion = MessageBox.Show(
            this,
            $"¿Quitar la asignacion de '{asignacion.Oficina}' ({franja})?",
            "Quitar asignacion",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirmacion != DialogResult.Yes)
        {
            return;
        }

        var quitada = await VistaAsync.EjecutarAsync(
            this,
            Controles,
            () => _asignacionServicio.QuitarAsync(asignacion.IdEmpleadoOficina),
            "No se pudo quitar la asignacion.");

        if (quitada)
        {
            await CargarAsignacionesAsync();
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

    private sealed record OpcionEmpleado(EmpleadoDetalle Detalle, string Texto)
    {
        public override string ToString() => Texto;
    }
}
