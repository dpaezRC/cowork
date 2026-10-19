using CoworkApp.Core.Modelos;
using CoworkApp.Core.Servicios;
using CoworkApp.Forms;

namespace CoworkApp.Views;

public sealed class EmpleadosView : UserControl
{
    private readonly EmpleadoServicio _empleadoServicio;
    private readonly EdificioServicio _edificioServicio;
    private readonly DataGridView _grilla = new();
    private readonly Button _btnNuevo = new() { Text = "Nuevo", AutoSize = true };
    private readonly Button _btnEditar = new() { Text = "Editar", AutoSize = true };
    private readonly Button _btnBaja = new() { Text = "Dar de baja", AutoSize = true };
    private readonly Button _btnActualizar = new() { Text = "Actualizar", AutoSize = true };

    public EmpleadosView(EmpleadoServicio empleadoServicio, EdificioServicio edificioServicio)
    {
        _empleadoServicio = empleadoServicio;
        _edificioServicio = edificioServicio;
        Dock = DockStyle.Fill;

        ConstruirControles();

        Load += async (_, _) => await CargarAsync();
    }

    private IReadOnlyList<Control> Controles => [_btnNuevo, _btnEditar, _btnBaja, _btnActualizar];

    private EmpleadoDetalle? Seleccionado => _grilla.SelectedRows.Count > 0 ? _grilla.SelectedRows[0].Tag as EmpleadoDetalle : null;

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
        _grilla.Columns.Add(CrearColumna("Nombre", "Nombre", 140));
        _grilla.Columns.Add(CrearColumna("Apellido", "Apellido", 140));
        _grilla.Columns.Add(CrearColumna("Email", "Email", 200));
        _grilla.Columns.Add(CrearColumna("Telefono", "Telefono", 110, DataGridViewAutoSizeColumnMode.None));
        _grilla.Columns.Add(CrearColumna("Edificio", "Edificio", 160));
        _grilla.Columns.Add(CrearColumna("Estado", "Estado", 100, DataGridViewAutoSizeColumnMode.None));
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
                var empleados = await _empleadoServicio.ListarAsync();
                _grilla.Rows.Clear();
                foreach (var empleado in empleados)
                {
                    var indice = _grilla.Rows.Add(
                        empleado.IdEmpleado,
                        empleado.Nombre,
                        empleado.Apellido,
                        empleado.Email,
                        empleado.Telefono ?? string.Empty,
                        empleado.Edificio,
                        empleado.Estado);
                    _grilla.Rows[indice].Tag = empleado;
                }
            },
            "No se pudieron cargar los empleados.");
    }

    private async Task NuevoAsync()
    {
        var edificios = await _edificioServicio.ListarAsync();
        if (edificios.Count == 0)
        {
            MessageBox.Show(this, "No hay edificios registrados. Registra un edificio primero.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var formulario = new EmpleadoForm(edificios, null);
        if (formulario.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var creado = await VistaAsync.EjecutarAsync(
            this,
            Controles,
            () => _empleadoServicio.CrearAsync(
                formulario.Nombre,
                formulario.Apellido,
                formulario.Email,
                formulario.Telefono,
                formulario.Contrasena,
                formulario.IdEdificio),
            "No se pudo registrar el empleado.");

        if (creado)
        {
            await CargarAsync();
        }
    }

    private async Task EditarAsync()
    {
        var detalle = Seleccionado;
        if (detalle is null)
        {
            return;
        }

        var edificios = await _edificioServicio.ListarAsync();
        if (edificios.Count == 0)
        {
            MessageBox.Show(this, "No hay edificios registrados. Registra un edificio primero.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var formulario = new EmpleadoForm(edificios, detalle);
        if (formulario.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var cambio = false;
        var actualizado = await VistaAsync.EjecutarAsync(
            this,
            Controles,
            async () => cambio = await _empleadoServicio.ActualizarAsync(
                detalle.IdEmpleado,
                formulario.Nombre,
                formulario.Apellido,
                formulario.Email,
                formulario.Telefono,
                formulario.IdEdificio,
                formulario.Estado,
                formulario.Contrasena),
            "No se pudo actualizar el empleado.");

        if (!actualizado)
        {
            return;
        }

        if (!cambio)
        {
            MessageBox.Show(this, "No se detectaron cambios.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        await CargarAsync();
    }

    private async Task DarDeBajaAsync()
    {
        var detalle = Seleccionado;
        if (detalle is null)
        {
            return;
        }

        if (detalle.Estado == "Inactivo")
        {
            MessageBox.Show(
                this,
                $"El empleado '{detalle.Nombre} {detalle.Apellido}' ya esta dado de baja.",
                "Aviso",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var confirmacion = MessageBox.Show(
            this,
            "El empleado y su cuenta de usuario pasaran a 'Inactivo'. Sus asignaciones de oficina se conservan." +
            $"\n\n¿Dar de baja al empleado '{detalle.Nombre} {detalle.Apellido}'?",
            "Dar de baja empleado",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirmacion != DialogResult.Yes)
        {
            return;
        }

        var dadoDeBaja = await VistaAsync.EjecutarAsync(
            this,
            Controles,
            () => _empleadoServicio.DarDeBajaAsync(detalle.IdEmpleado),
            "No se pudo dar de baja el empleado.");

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
