using CoworkApp.Core.Modelos;
using CoworkApp.Core.Servicios;
using CoworkApp.Core.Validacion;
using CoworkApp.Views;

namespace CoworkApp.Forms;

public sealed class OficinaCascadaForm : Form
{
    private static readonly TimeOnly AperturaPorDefecto = new(8, 0);
    private static readonly TimeOnly CierrePorDefecto = new(21, 0);

    private readonly EdificioServicio _edificioServicio;
    private readonly PisoServicio _pisoServicio;
    private readonly OficinaServicio _oficinaServicio;
    private readonly CascadaServicio _cascadaServicio;

    private readonly ComboBox _edificio = new();
    private readonly TextBox _nuevoEdificioNombre = new();
    private readonly TextBox _nuevoEdificioDireccion = new();
    private readonly ComboBox _piso = new();
    private readonly TextBox _nuevoPisoNombre = new();
    private readonly TextBox _oficinaNombre = new();
    private readonly TextBox _capacidad = new();
    private readonly TextBox _tarifa = new();
    private readonly DateTimePicker _apertura = new();
    private readonly DateTimePicker _cierre = new();
    private readonly Button _aceptar = new() { Text = "Registrar", AutoSize = true, Padding = new Padding(16, 4, 16, 4) };
    private readonly Button _cancelar = new() { Text = "Cancelar", DialogResult = DialogResult.Cancel, AutoSize = true, Padding = new Padding(16, 4, 16, 4) };

    private bool _cargando;

    public OficinaCascadaForm(
        EdificioServicio edificioServicio,
        PisoServicio pisoServicio,
        OficinaServicio oficinaServicio,
        CascadaServicio cascadaServicio)
    {
        _edificioServicio = edificioServicio;
        _pisoServicio = pisoServicio;
        _oficinaServicio = oficinaServicio;
        _cascadaServicio = cascadaServicio;

        Text = "Registrar oficina (edificio / piso / oficina)";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 480);

        ConstruirControles();

        Load += async (_, _) => await InicializarAsync();
    }

    private void ConstruirControles()
    {
        var contenedor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 10,
            Padding = new Padding(20)
        };
        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var fila = 0; fila < 9; fila++)
        {
            contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        }

        contenedor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _edificio.Dock = DockStyle.Fill;
        _edificio.DropDownStyle = ComboBoxStyle.DropDownList;
        _edificio.SelectedIndexChanged += async (_, _) => await CargarPisosAsync();

        _piso.Dock = DockStyle.Fill;
        _piso.DropDownStyle = ComboBoxStyle.DropDownList;
        _piso.SelectedIndexChanged += (_, _) => ActualizarHabilitacion();

        _nuevoEdificioNombre.Dock = DockStyle.Fill;
        _nuevoEdificioDireccion.Dock = DockStyle.Fill;
        _nuevoPisoNombre.Dock = DockStyle.Fill;
        _oficinaNombre.Dock = DockStyle.Fill;
        _capacidad.Dock = DockStyle.Fill;
        _tarifa.Dock = DockStyle.Fill;

        ConfigurarHora(_apertura, AperturaPorDefecto);
        ConfigurarHora(_cierre, CierrePorDefecto);

        AgregarFila(contenedor, 0, "Edificio", _edificio);
        AgregarFila(contenedor, 1, "Nuevo edificio", _nuevoEdificioNombre);
        AgregarFila(contenedor, 2, "Direccion edificio", _nuevoEdificioDireccion);
        AgregarFila(contenedor, 3, "Piso", _piso);
        AgregarFila(contenedor, 4, "Nuevo piso", _nuevoPisoNombre);
        AgregarFila(contenedor, 5, "Oficina", _oficinaNombre);
        AgregarFila(contenedor, 6, "Capacidad", _capacidad);
        AgregarFila(contenedor, 7, "Tarifa por hora", _tarifa);
        AgregarFila(contenedor, 8, "Hora apertura", _apertura);
        AgregarFila(contenedor, 9, "Hora cierre", _cierre);

        var acciones = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        _aceptar.Click += async (_, _) => await ConfirmarAsync();
        acciones.Controls.Add(_aceptar);
        acciones.Controls.Add(_cancelar);

        contenedor.RowCount = 11;
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        contenedor.Controls.Add(acciones, 0, 10);
        contenedor.SetColumnSpan(acciones, 2);

        Controls.Add(contenedor);

        AcceptButton = _aceptar;
        CancelButton = _cancelar;
    }

    private async Task InicializarAsync()
    {
        await VistaAsync.EjecutarAsync(
            this,
            [_aceptar, _edificio, _piso],
            async () =>
            {
                var edificios = await _edificioServicio.ListarAsync();
                _edificio.Items.Clear();
                foreach (var edificio in edificios)
                {
                    _edificio.Items.Add(new OpcionEdificio(edificio.IdEdificio, $"{edificio.Nombre} ({edificio.Estado})"));
                }

                _edificio.Items.Add(new OpcionEdificio(null, "Nuevo edificio..."));
                _edificio.SelectedIndex = 0;
            },
            "No se pudieron cargar los edificios.");

        await CargarPisosAsync();
    }

    private async Task CargarPisosAsync()
    {
        _cargando = true;
        try
        {
            await VistaAsync.EjecutarAsync(
                this,
                [_aceptar, _piso],
                async () =>
                {
                    _piso.Items.Clear();
                    if (_edificio.SelectedItem is OpcionEdificio { Id: int idEdificio })
                    {
                        var pisos = await _pisoServicio.ListarPorEdificioAsync(idEdificio);
                        foreach (var piso in pisos)
                        {
                            _piso.Items.Add(new OpcionPiso(piso.IdPiso, $"{piso.Nombre} ({piso.Estado})"));
                        }
                    }

                    _piso.Items.Add(new OpcionPiso(null, "Nuevo piso..."));
                    _piso.SelectedIndex = 0;
                },
                "No se pudieron cargar los pisos.");
        }
        finally
        {
            _cargando = false;
            ActualizarHabilitacion();
        }
    }

    private void ActualizarHabilitacion()
    {
        var edificioNuevo = _edificio.SelectedItem is not OpcionEdificio { Id: not null };
        var pisoNuevo = _piso.SelectedItem is not OpcionPiso { Id: not null };

        _nuevoEdificioNombre.Enabled = edificioNuevo;
        _nuevoEdificioDireccion.Enabled = edificioNuevo;
        _nuevoPisoNombre.Enabled = pisoNuevo;

        if (!edificioNuevo)
        {
            _nuevoEdificioNombre.Clear();
            _nuevoEdificioDireccion.Clear();
        }

        if (!pisoNuevo)
        {
            _nuevoPisoNombre.Clear();
        }
    }

    private async Task ConfirmarAsync()
    {
        if (_cargando)
        {
            return;
        }

        var controles = new List<Control>
        {
            _aceptar, _cancelar, _edificio, _piso,
            _nuevoEdificioNombre, _nuevoEdificioDireccion, _nuevoPisoNombre,
            _oficinaNombre, _capacidad, _tarifa, _apertura, _cierre
        };

        var registrado = await VistaAsync.EjecutarAsync(this, controles, RegistrarAsync, "No se pudo registrar la oficina.");
        if (registrado)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private async Task RegistrarAsync()
    {
        var nombre = Validador.Requerido(_oficinaNombre.Text, OficinaServicio.MensajeNombreObligatorio);
        var capacidad = Validador.Capacidad(_capacidad.Text);
        var tarifa = Validador.Tarifa(_tarifa.Text);
        var apertura = TimeOnly.FromDateTime(_apertura.Value);
        var cierre = TimeOnly.FromDateTime(_cierre.Value);

        if (cierre <= apertura)
        {
            throw new ReglaNegocioException(OficinaServicio.MensajeHorarioInvalido);
        }

        var edificio = await ResolverEdificioAsync();
        var piso = await ResolverPisoAsync(edificio);

        if (piso.Id is int idPiso)
        {
            var existente = await _oficinaServicio.BuscarPorNombreAsync(idPiso, nombre);
            if (existente is not null)
            {
                throw new ReglaNegocioException(
                    $"Ya existe una oficina con el nombre '{nombre}' en el piso seleccionado (ID {existente.IdOficina}). No se registro una nueva oficina.");
            }
        }

        await _cascadaServicio.RegistrarAsync(
            edificio,
            piso,
            new OficinaCascada(nombre, capacidad, tarifa, apertura, cierre));
    }

    private async Task<EdificioCascada> ResolverEdificioAsync()
    {
        if (_edificio.SelectedItem is OpcionEdificio { Id: int idEdificio })
        {
            return new EdificioCascada(idEdificio, string.Empty, string.Empty);
        }

        var nombre = Validador.Requerido(_nuevoEdificioNombre.Text, EdificioServicio.MensajeNombreObligatorio);
        var existente = await _edificioServicio.BuscarPorNombreAsync(nombre);
        if (existente is not null)
        {
            Avisar($"Ya existe un edificio con el nombre '{existente.Nombre}'. Se usara el existente (ID {existente.IdEdificio}).");
            return new EdificioCascada(existente.IdEdificio, string.Empty, string.Empty);
        }

        var direccion = Validador.Requerido(_nuevoEdificioDireccion.Text, EdificioServicio.MensajeDireccionObligatoria);
        return new EdificioCascada(null, nombre, direccion);
    }

    private async Task<PisoCascada> ResolverPisoAsync(EdificioCascada edificio)
    {
        if (_piso.SelectedItem is OpcionPiso { Id: int idPiso })
        {
            return new PisoCascada(idPiso, string.Empty);
        }

        var nombre = Validador.Requerido(_nuevoPisoNombre.Text, PisoServicio.MensajeNombreObligatorio);
        if (edificio.Id is int idEdificio)
        {
            var existente = await _pisoServicio.BuscarPorNombreAsync(idEdificio, nombre);
            if (existente is not null)
            {
                Avisar($"Ya existe un piso con el nombre '{existente.Nombre}' en el edificio. Se usara el existente (ID {existente.IdPiso}).");
                return new PisoCascada(existente.IdPiso, string.Empty);
            }
        }

        return new PisoCascada(null, nombre);
    }

    private void Avisar(string mensaje)
    {
        MessageBox.Show(this, mensaje, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void ConfigurarHora(DateTimePicker selector, TimeOnly hora)
    {
        selector.Dock = DockStyle.Fill;
        selector.Format = DateTimePickerFormat.Custom;
        selector.CustomFormat = "HH:mm";
        selector.ShowUpDown = true;
        selector.Value = DateTime.Today.Add(hora.ToTimeSpan());
    }

    private static void AgregarFila(TableLayoutPanel contenedor, int fila, string etiqueta, Control control)
    {
        contenedor.Controls.Add(
            new Label { Text = etiqueta, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft },
            0,
            fila);
        contenedor.Controls.Add(control, 1, fila);
    }

    private sealed record OpcionEdificio(int? Id, string Texto)
    {
        public override string ToString() => Texto;
    }

    private sealed record OpcionPiso(int? Id, string Texto)
    {
        public override string ToString() => Texto;
    }
}
