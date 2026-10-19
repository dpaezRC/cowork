using System.Globalization;
using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Seguridad;
using CoworkApp.Core.Servicios;

namespace CoworkApp.Views;

public sealed class ReservasView : UserControl
{
    private static readonly string[] EstadosReserva =
        ["Confirmada", "EnCurso", "Finalizada", "Cancelada", "NoPresentada"];

    private readonly ReservaServicio _servicio;
    private readonly SesionActual _sesion;

    private readonly ComboBox _cliente = new();

    private readonly DateTimePicker _fecha = new();
    private readonly TextBox _horaInicio = new();
    private readonly TextBox _horaFin = new();
    private readonly NumericUpDown _asientos = new();
    private readonly Button _btnBuscar = new() { Text = "Buscar disponibilidad", AutoSize = true };
    private readonly Button _btnReservar = new() { Text = "Confirmar reserva", AutoSize = true };
    private readonly DataGridView _grillaDisponibles = new();
    private readonly Label _resumen = new();

    private readonly ComboBox _filtroEstado = new();
    private readonly Button _btnActualizarLista = new() { Text = "Actualizar", AutoSize = true };
    private readonly DataGridView _grillaReservas = new();

    private readonly DataGridView _grillaCancelar = new();
    private readonly Button _btnCancelar = new() { Text = "Cancelar reserva", AutoSize = true };

    private readonly List<OficinaDisponible> _disponibles = new();
    private bool _cargando;

    public ReservasView(Db db, SesionActual sesion)
    {
        ArgumentNullException.ThrowIfNull(db);
        _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
        Dock = DockStyle.Fill;

        _servicio = new ReservaServicio(
            new UsuarioRepositorio(db),
            new OficinaRepositorio(db),
            new ReservaRepositorio(db),
            new AuditoriaRepositorio(db),
            db,
            db);

        ConstruirControles();

        Load += async (_, _) => await InicializarAsync();
    }

    private Usuario? ClienteSeleccionado => _cliente.SelectedItem as Usuario;

    private OficinaDisponible? OficinaSeleccionada =>
        _grillaDisponibles.SelectedRows.Count > 0 ? _grillaDisponibles.SelectedRows[0].Tag as OficinaDisponible : null;

    private ReservaDetalle? ReservaCancelable =>
        _grillaCancelar.SelectedRows.Count > 0 ? _grillaCancelar.SelectedRows[0].Tag as ReservaDetalle : null;

    private void ConstruirControles()
    {
        var raiz = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var filaCliente = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(4) };
        filaCliente.Controls.Add(new Label { Text = "Cliente:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) });
        _cliente.DropDownStyle = ComboBoxStyle.DropDownList;
        _cliente.DisplayMember = nameof(Usuario.Nombre);
        _cliente.Width = 280;
        _cliente.SelectedIndexChanged += async (_, _) =>
        {
            if (!_cargando)
            {
                await RefrescarOperacionesAsync(false);
            }
        };
        filaCliente.Controls.Add(_cliente);
        raiz.Controls.Add(filaCliente, 0, 0);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CrearPagina("Buscar y reservar", ConstruirBuscar()));
        tabs.TabPages.Add(CrearPagina("Mis reservas", ConstruirListar()));
        tabs.TabPages.Add(CrearPagina("Cancelar", ConstruirCancelar()));
        raiz.Controls.Add(tabs, 0, 1);

        Controls.Add(raiz);
    }

    private Control ConstruirBuscar()
    {
        var campos = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(4), Height = 36 };
        campos.Controls.Add(new Label { Text = "Fecha:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
        _fecha.Format = DateTimePickerFormat.Custom;
        _fecha.CustomFormat = "yyyy-MM-dd";
        _fecha.Width = 110;
        _fecha.Value = DateTime.Today;
        campos.Controls.Add(_fecha);
        campos.Controls.Add(new Label { Text = "Inicio (HH:MM):", AutoSize = true, Margin = new Padding(12, 8, 4, 0) });
        _horaInicio.Width = 70;
        _horaInicio.Margin = new Padding(0, 4, 0, 0);
        campos.Controls.Add(_horaInicio);
        campos.Controls.Add(new Label { Text = "Fin (HH:MM):", AutoSize = true, Margin = new Padding(12, 8, 4, 0) });
        _horaFin.Width = 70;
        _horaFin.Margin = new Padding(0, 4, 0, 0);
        campos.Controls.Add(_horaFin);
        campos.Controls.Add(new Label { Text = "Asientos:", AutoSize = true, Margin = new Padding(12, 8, 4, 0) });
        _asientos.Minimum = 1;
        _asientos.Maximum = 1000;
        _asientos.Value = 1;
        _asientos.Width = 70;
        _asientos.Margin = new Padding(0, 4, 0, 0);
        campos.Controls.Add(_asientos);

        var acciones = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(4), Height = 40 };
        _btnBuscar.Click += async (_, _) => await BuscarAsync();
        _btnReservar.Click += async (_, _) => await ReservarAsync();
        acciones.Controls.Add(_btnBuscar);
        acciones.Controls.Add(_btnReservar);

        _resumen.Dock = DockStyle.Fill;
        _resumen.TextAlign = ContentAlignment.MiddleLeft;
        _resumen.Text = "Selecciona una oficina disponible.";
        _resumen.ForeColor = SystemColors.GrayText;

        PrepararGrilla(_grillaDisponibles);
        _grillaDisponibles.Columns.Add(CrearColumna("Id", "ID", 50, DataGridViewAutoSizeColumnMode.None));
        _grillaDisponibles.Columns.Add(CrearColumna("Oficina", "Oficina", 140));
        _grillaDisponibles.Columns.Add(CrearColumna("Ubicacion", "Edificio/Piso", 170));
        _grillaDisponibles.Columns.Add(CrearColumna("Capacidad", "Capacidad", 80, DataGridViewAutoSizeColumnMode.None));
        _grillaDisponibles.Columns.Add(CrearColumna("Disponibles", "Disponibles", 90, DataGridViewAutoSizeColumnMode.None));
        _grillaDisponibles.Columns.Add(CrearColumna("Tarifa", "Tarifa/hora", 90, DataGridViewAutoSizeColumnMode.None));
        _grillaDisponibles.SelectionChanged += (_, _) => ActualizarResumen();

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(campos, 0, 0);
        layout.Controls.Add(acciones, 0, 1);
        layout.Controls.Add(_resumen, 0, 2);
        layout.Controls.Add(_grillaDisponibles, 0, 3);
        return layout;
    }

    private Control ConstruirListar()
    {
        var barra = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(4), Height = 40 };
        barra.Controls.Add(new Label { Text = "Estado:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
        _filtroEstado.DropDownStyle = ComboBoxStyle.DropDownList;
        _filtroEstado.Width = 150;
        _filtroEstado.Items.Add("Todos");
        _filtroEstado.Items.AddRange(EstadosReserva);
        _filtroEstado.SelectedIndex = 0;
        _filtroEstado.SelectedIndexChanged += async (_, _) => await CargarReservasAsync(true);
        barra.Controls.Add(_filtroEstado);
        _btnActualizarLista.Click += async (_, _) => await CargarReservasAsync(true);
        barra.Controls.Add(_btnActualizarLista);

        PrepararGrilla(_grillaReservas);
        _grillaReservas.Columns.Add(CrearColumna("Id", "ID", 50, DataGridViewAutoSizeColumnMode.None));
        _grillaReservas.Columns.Add(CrearColumna("Oficina", "Oficina", 140));
        _grillaReservas.Columns.Add(CrearColumna("Fecha", "Fecha", 100, DataGridViewAutoSizeColumnMode.None));
        _grillaReservas.Columns.Add(CrearColumna("Horario", "Horario", 110, DataGridViewAutoSizeColumnMode.None));
        _grillaReservas.Columns.Add(CrearColumna("Asientos", "Asientos", 80, DataGridViewAutoSizeColumnMode.None));
        _grillaReservas.Columns.Add(CrearColumna("Estado", "Estado", 100, DataGridViewAutoSizeColumnMode.None));
        _grillaReservas.Columns.Add(CrearColumna("Pago", "Pago", 100, DataGridViewAutoSizeColumnMode.None));
        _grillaReservas.Columns.Add(CrearColumna("Tarifa", "Tarifa", 90, DataGridViewAutoSizeColumnMode.None));

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(barra, 0, 0);
        layout.Controls.Add(_grillaReservas, 0, 1);
        return layout;
    }

    private Control ConstruirCancelar()
    {
        var barra = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(4), Height = 40 };
        _btnCancelar.Click += async (_, _) => await CancelarAsync();
        barra.Controls.Add(_btnCancelar);

        PrepararGrilla(_grillaCancelar);
        _grillaCancelar.Columns.Add(CrearColumna("Id", "ID", 50, DataGridViewAutoSizeColumnMode.None));
        _grillaCancelar.Columns.Add(CrearColumna("Oficina", "Oficina", 140));
        _grillaCancelar.Columns.Add(CrearColumna("Fecha", "Fecha", 100, DataGridViewAutoSizeColumnMode.None));
        _grillaCancelar.Columns.Add(CrearColumna("Horario", "Horario", 110, DataGridViewAutoSizeColumnMode.None));
        _grillaCancelar.Columns.Add(CrearColumna("Asientos", "Asientos", 80, DataGridViewAutoSizeColumnMode.None));
        _grillaCancelar.Columns.Add(CrearColumna("Estado", "Estado", 100, DataGridViewAutoSizeColumnMode.None));
        _grillaCancelar.Columns.Add(CrearColumna("Pago", "Pago", 100, DataGridViewAutoSizeColumnMode.None));
        _grillaCancelar.Columns.Add(CrearColumna("Tarifa", "Tarifa", 90, DataGridViewAutoSizeColumnMode.None));

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(barra, 0, 0);
        layout.Controls.Add(_grillaCancelar, 0, 1);
        return layout;
    }

    private async Task InicializarAsync()
    {
        _cargando = true;
        var cargado = await VistaAsync.EjecutarAsync(
            this,
            [_cliente],
            async () =>
            {
                if (_sesion.EsCliente)
                {
                    var usuario = await _servicio.ObtenerUsuarioAsync(_sesion.IdUsuario!.Value);
                    if (usuario is not null)
                    {
                        _cliente.Items.Add(usuario);
                    }
                }
                else
                {
                    foreach (var cliente in await _servicio.ListarClientesAsync())
                    {
                        _cliente.Items.Add(cliente);
                    }
                }

                _cliente.Enabled = !_sesion.EsCliente;
                if (_cliente.Items.Count > 0)
                {
                    _cliente.SelectedIndex = 0;
                }
            },
            "No se pudieron cargar los clientes.");

        _cargando = false;

        if (cargado)
        {
            await RefrescarOperacionesAsync(false);
        }
    }

    private async Task RefrescarOperacionesAsync(bool avisarVacio)
    {
        await CargarReservasAsync(avisarVacio);
        await CargarCancelablesAsync(avisarVacio);
    }

    private async Task BuscarAsync()
    {
        var cliente = ClienteSeleccionado;
        if (cliente is null)
        {
            Advertencia("Selecciona un cliente para reservar.");
            return;
        }

        if (!TryHora(_horaInicio.Text, out var horaInicio) || !TryHora(_horaFin.Text, out var horaFin))
        {
            Advertencia("Ingresa un horario valido en formato HH:MM.");
            return;
        }

        var fecha = DateOnly.FromDateTime(_fecha.Value);
        var asientos = (int)_asientos.Value;

        List<OficinaDisponible>? resultados = null;
        var buscado = await VistaAsync.EjecutarAsync(
            this,
            [_btnBuscar, _btnReservar, _fecha, _horaInicio, _horaFin, _asientos, _cliente],
            async () => resultados = await _servicio.BuscarDisponibilidadAsync(fecha, horaInicio, horaFin, asientos),
            "No se pudo buscar disponibilidad.");
        if (!buscado)
        {
            return;
        }

        _disponibles.Clear();
        _grillaDisponibles.Rows.Clear();
        foreach (var oficina in resultados!)
        {
            _disponibles.Add(oficina);
            var indice = _grillaDisponibles.Rows.Add(
                oficina.IdOficina,
                oficina.Nombre,
                $"{oficina.Edificio} / {oficina.Piso}",
                oficina.Capacidad,
                oficina.Disponibles,
                FormatoMonto(oficina.Tarifa));
            _grillaDisponibles.Rows[indice].Tag = oficina;
        }

        if (_disponibles.Count == 0)
        {
            Advertencia("No hay oficinas disponibles para el periodo y la cantidad de asientos solicitados.");
        }

        ActualizarResumen();
    }

    private void ActualizarResumen()
    {
        var oficina = OficinaSeleccionada;
        if (oficina is null)
        {
            _resumen.Text = "Selecciona una oficina disponible.";
            _resumen.ForeColor = SystemColors.GrayText;
            return;
        }

        var detalle =
            $"Oficina {oficina.Nombre} (ID {oficina.IdOficina}) - {oficina.Edificio} / {oficina.Piso} - " +
            $"Capacidad {oficina.Capacidad} - Disponibles {oficina.Disponibles} - Tarifa {FormatoMonto(oficina.Tarifa)}/hora";

        if (TryHora(_horaInicio.Text, out var horaInicio) && TryHora(_horaFin.Text, out var horaFin) && horaFin > horaInicio)
        {
            var importe = ReservaServicio.ImporteEstimado((int)_asientos.Value, oficina.Tarifa, horaInicio, horaFin);
            detalle += $" - Importe estimado {FormatoMonto(importe)}";
        }

        _resumen.Text = detalle;
        _resumen.ForeColor = SystemColors.ControlText;
    }

    private async Task ReservarAsync()
    {
        var cliente = ClienteSeleccionado;
        if (cliente is null)
        {
            Advertencia("Selecciona un cliente para reservar.");
            return;
        }

        var oficina = OficinaSeleccionada;
        if (oficina is null)
        {
            Advertencia("Selecciona una oficina disponible.");
            return;
        }

        if (!TryHora(_horaInicio.Text, out var horaInicio) || !TryHora(_horaFin.Text, out var horaFin))
        {
            Advertencia("Ingresa un horario valido en formato HH:MM.");
            return;
        }

        var fecha = DateOnly.FromDateTime(_fecha.Value);
        var asientos = (int)_asientos.Value;
        var importe = ReservaServicio.ImporteEstimado(asientos, oficina.Tarifa, horaInicio, horaFin);

        var confirmacion = MessageBox.Show(
            this,
            $"Cliente: {cliente.Nombre} {cliente.Apellido} (ID {cliente.IdUsuario})\n" +
            $"Oficina: {oficina.Nombre} (ID {oficina.IdOficina})\n" +
            $"Ubicacion: {oficina.Edificio} / {oficina.Piso}\n" +
            $"Fecha: {fecha:yyyy-MM-dd}\n" +
            $"Horario: {horaInicio.ToString("HH\\:mm")}-{horaFin.ToString("HH\\:mm")}\n" +
            $"Asientos: {asientos}\n" +
            $"Tarifa por hora: {FormatoMonto(oficina.Tarifa)}\n" +
            $"Importe estimado: {FormatoMonto(importe)}\n\n" +
            "El importe estimado es solo informativo y no se persiste.\n\n" +
            "¿Confirmar la reserva?",
            "Confirmar reserva",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirmacion != DialogResult.Yes)
        {
            return;
        }

        Reserva? reserva = null;
        var creado = await VistaAsync.EjecutarAsync(
            this,
            [_btnBuscar, _btnReservar, _fecha, _horaInicio, _horaFin, _asientos, _cliente],
            async () => reserva = await _servicio.CrearAsync(cliente.IdUsuario, oficina.IdOficina, fecha, horaInicio, horaFin, asientos),
            "No se pudo registrar la reserva.");
        if (!creado || reserva is null)
        {
            return;
        }

        MessageBox.Show(this, $"Reserva confirmada (ID {reserva.IdReserva}).", "Reserva", MessageBoxButtons.OK, MessageBoxIcon.Information);
        await RefrescarOperacionesAsync(false);
    }

    private async Task CargarReservasAsync(bool avisarVacio)
    {
        var cliente = ClienteSeleccionado;
        if (cliente is null)
        {
            _grillaReservas.Rows.Clear();
            return;
        }

        var filtro = _filtroEstado.SelectedIndex <= 0 ? null : _filtroEstado.SelectedItem?.ToString();

        List<ReservaDetalle>? reservas = null;
        var cargado = await VistaAsync.EjecutarAsync(
            this,
            [_btnActualizarLista, _filtroEstado],
            async () => reservas = await _servicio.ListarPorClienteAsync(cliente.IdUsuario, filtro),
            "No se pudieron cargar las reservas.");
        if (!cargado)
        {
            return;
        }

        _grillaReservas.Rows.Clear();
        foreach (var reserva in reservas!)
        {
            var indice = _grillaReservas.Rows.Add(
                reserva.IdReserva,
                reserva.Oficina,
                reserva.Fecha.ToString("yyyy-MM-dd"),
                $"{reserva.HoraInicio:HH\\:mm}-{reserva.HoraFin:HH\\:mm}",
                reserva.CantidadAsientos,
                reserva.Estado,
                reserva.EstadoPago,
                FormatoMonto(reserva.TarifaHoraAplicada));
            _grillaReservas.Rows[indice].Tag = reserva;
        }

        if (avisarVacio && reservas!.Count == 0)
        {
            Advertencia("El cliente no tiene reservas para el filtro seleccionado.");
        }
    }

    private async Task CargarCancelablesAsync(bool avisarVacio)
    {
        var cliente = ClienteSeleccionado;
        if (cliente is null)
        {
            _grillaCancelar.Rows.Clear();
            return;
        }

        List<ReservaDetalle>? reservas = null;
        var cargado = await VistaAsync.EjecutarAsync(
            this,
            [_btnCancelar],
            async () => reservas = await _servicio.ListarPorClienteAsync(cliente.IdUsuario, "Confirmada"),
            "No se pudieron cargar las reservas confirmadas.");
        if (!cargado)
        {
            return;
        }

        _grillaCancelar.Rows.Clear();
        foreach (var reserva in reservas!)
        {
            var indice = _grillaCancelar.Rows.Add(
                reserva.IdReserva,
                reserva.Oficina,
                reserva.Fecha.ToString("yyyy-MM-dd"),
                $"{reserva.HoraInicio:HH\\:mm}-{reserva.HoraFin:HH\\:mm}",
                reserva.CantidadAsientos,
                reserva.Estado,
                reserva.EstadoPago,
                FormatoMonto(reserva.TarifaHoraAplicada));
            _grillaCancelar.Rows[indice].Tag = reserva;
        }

        if (avisarVacio && reservas!.Count == 0)
        {
            Advertencia("El cliente no tiene reservas confirmadas para cancelar.");
        }
    }

    private async Task CancelarAsync()
    {
        var cliente = ClienteSeleccionado;
        if (cliente is null)
        {
            Advertencia("Selecciona un cliente.");
            return;
        }

        var reserva = ReservaCancelable;
        if (reserva is null)
        {
            Advertencia("Selecciona una reserva confirmada para cancelar.");
            return;
        }

        var confirmacion = MessageBox.Show(
            this,
            $"¿Cancelar la reserva ID {reserva.IdReserva} de '{reserva.Oficina}'?",
            "Cancelar reserva",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirmacion != DialogResult.Yes)
        {
            return;
        }

        var cancelado = await VistaAsync.EjecutarAsync(
            this,
            [_btnCancelar],
            () => _servicio.CancelarAsync(reserva.IdReserva, cliente.IdUsuario),
            "No se pudo cancelar la reserva.");
        if (!cancelado)
        {
            return;
        }

        MessageBox.Show(this, $"Reserva ID {reserva.IdReserva} cancelada.", "Cancelar reserva", MessageBoxButtons.OK, MessageBoxIcon.Information);
        await RefrescarOperacionesAsync(false);
    }

    private static bool TryHora(string texto, out TimeOnly hora)
    {
        return TimeOnly.TryParseExact(texto.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out hora);
    }

    private static string FormatoMonto(decimal valor)
    {
        return valor.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private void Advertencia(string mensaje)
    {
        MessageBox.Show(this, mensaje, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void PrepararGrilla(DataGridView grilla)
    {
        grilla.Dock = DockStyle.Fill;
        grilla.ReadOnly = true;
        grilla.AllowUserToAddRows = false;
        grilla.AllowUserToDeleteRows = false;
        grilla.AllowUserToResizeRows = false;
        grilla.MultiSelect = false;
        grilla.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grilla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grilla.RowHeadersVisible = false;
        grilla.AutoGenerateColumns = false;
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

    private static TabPage CrearPagina(string titulo, Control contenido)
    {
        var pagina = new TabPage(titulo) { Padding = new Padding(8) };
        contenido.Dock = DockStyle.Fill;
        pagina.Controls.Add(contenido);
        return pagina;
    }
}
