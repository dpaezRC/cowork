using System.Globalization;
using CoworkApp.Core.Datos;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Seguridad;
using CoworkApp.Core.Servicios;
using CoworkApp.Forms;

namespace CoworkApp.Views;

public sealed class ControlOperativoView : UserControl
{
    private readonly ControlOperativoServicio _servicio;
    private readonly SesionActual _sesion;

    private readonly DateTimePicker _fecha = new();
    private readonly Button _btnVerAgenda = new() { Text = "Ver agenda", AutoSize = true };
    private readonly Button _btnIngreso = new() { Text = "Registrar ingreso", AutoSize = true };
    private readonly Button _btnSalida = new() { Text = "Registrar salida", AutoSize = true };
    private readonly Button _btnPago = new() { Text = "Registrar o validar pago", AutoSize = true };
    private readonly DataGridView _grilla = new();

    public ControlOperativoView(Db db, SesionActual sesion)
    {
        ArgumentNullException.ThrowIfNull(db);
        _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
        Dock = DockStyle.Fill;

        _servicio = new ControlOperativoServicio(
            new EmpleadoRepositorio(db),
            new EmpleadoOficinaRepositorio(db),
            new ReservaRepositorio(db),
            new ReservaUsoRepositorio(db),
            new PagoRepositorio(db),
            new DeudaRepositorio(db),
            new AuditoriaRepositorio(db),
            db,
            db);

        ConstruirControles();

        Load += async (_, _) => await InicializarAsync();
    }

    private FilaAgenda? Seleccionada =>
        _grilla.SelectedRows.Count > 0 ? _grilla.SelectedRows[0].Tag as FilaAgenda : null;

    private void ConstruirControles()
    {
        var barra = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(4), Height = 44 };
        barra.Controls.Add(new Label { Text = "Fecha:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
        _fecha.Format = DateTimePickerFormat.Custom;
        _fecha.CustomFormat = "yyyy-MM-dd";
        _fecha.Width = 110;
        _fecha.Value = DateTime.Today;
        barra.Controls.Add(_fecha);
        _btnVerAgenda.Click += async (_, _) => await VerAgendaAsync(true);
        barra.Controls.Add(_btnVerAgenda);

        var acciones = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(4), Height = 44 };
        _btnIngreso.Click += async (_, _) => await RegistrarIngresoAsync();
        _btnSalida.Click += async (_, _) => await RegistrarSalidaAsync();
        _btnPago.Click += async (_, _) => await RegistrarPagoAsync();
        acciones.Controls.Add(_btnIngreso);
        acciones.Controls.Add(_btnSalida);
        acciones.Controls.Add(_btnPago);

        PrepararGrilla();
        _grilla.Columns.Add(CrearColumna("Id", "ID", 50, DataGridViewAutoSizeColumnMode.None));
        _grilla.Columns.Add(CrearColumna("Oficina", "Oficina", 130));
        _grilla.Columns.Add(CrearColumna("Horario", "Horario", 100, DataGridViewAutoSizeColumnMode.None));
        _grilla.Columns.Add(CrearColumna("Asientos", "Asientos", 70, DataGridViewAutoSizeColumnMode.None));
        _grilla.Columns.Add(CrearColumna("Estado", "Estado", 100, DataGridViewAutoSizeColumnMode.None));
        _grilla.Columns.Add(CrearColumna("Pago", "Pago", 110, DataGridViewAutoSizeColumnMode.None));
        _grilla.Columns.Add(CrearColumna("Importe", "Importe", 80, DataGridViewAutoSizeColumnMode.None));
        _grilla.Columns.Add(CrearColumna("Ingreso", "Ingreso", 70, DataGridViewAutoSizeColumnMode.None));
        _grilla.Columns.Add(CrearColumna("Salida", "Salida", 70, DataGridViewAutoSizeColumnMode.None));
        _grilla.Columns.Add(CrearColumna("Motivo", "Motivo", 220));

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(barra, 0, 0);
        layout.Controls.Add(acciones, 0, 1);
        layout.Controls.Add(_grilla, 0, 2);

        Controls.Add(layout);
    }

    private async Task InicializarAsync()
    {
        DateTime? ahora = null;
        var obtenido = await VistaAsync.EjecutarAsync(
            this,
            [_fecha, _btnVerAgenda],
            async () => ahora = await _servicio.AhoraServidorAsync(),
            "No se pudo obtener la fecha del servidor.");

        if (obtenido && ahora is not null)
        {
            _fecha.Value = ahora.Value.Date;
        }

        await VerAgendaAsync(false);
    }

    private async Task VerAgendaAsync(bool avisarVacio)
    {
        var fecha = DateOnly.FromDateTime(_fecha.Value);

        List<FilaAgenda>? filas = null;
        var cargado = await VistaAsync.EjecutarAsync(
            this,
            ControlesAcciones(),
            async () => filas = await _servicio.AbrirAgendaAsync(_sesion.IdUsuario!.Value, fecha),
            "No se pudo cargar la agenda.");
        if (!cargado)
        {
            return;
        }

        _grilla.Rows.Clear();
        foreach (var fila in filas!)
        {
            var indice = _grilla.Rows.Add(
                fila.IdReserva,
                fila.Oficina,
                fila.Horario,
                fila.Asientos,
                fila.Estado,
                fila.EstadoPago,
                fila.Importe is null ? "-" : FormatoMonto(fila.Importe.Value),
                fila.Ingreso ?? "-",
                fila.Salida ?? "-",
                fila.Motivo);
            _grilla.Rows[indice].Tag = fila;
        }

        if (avisarVacio && filas!.Count == 0)
        {
            MessageBox.Show(this, "No hay reservas para tus oficinas en la fecha elegida.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async Task RegistrarIngresoAsync()
    {
        var fila = Seleccionada;
        if (fila is null)
        {
            Advertencia("Selecciona una reserva de la agenda.");
            return;
        }

        if (!PedirHora("Hora de ingreso", out var hora))
        {
            return;
        }

        ResultadoIngreso? resultado = null;
        var ok = await VistaAsync.EjecutarAsync(
            this,
            ControlesAcciones(),
            async () => resultado = await _servicio.RegistrarIngresoAsync(_sesion.IdUsuario!.Value, fila.IdReserva, hora),
            "No se pudo registrar el ingreso.");
        if (!ok || resultado is null)
        {
            return;
        }

        var mensaje = resultado.Correccion
            ? $"Ingreso de correccion registrado para la reserva ID {resultado.IdReserva} (En curso)."
            : $"Ingreso registrado para la reserva ID {resultado.IdReserva} (En curso).";
        if (resultado.DeudaAnulada)
        {
            mensaje += "\n\nLa deuda por inasistencia quedo anulada.";
        }

        MessageBox.Show(this, mensaje, "Registrar ingreso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        await VerAgendaAsync(false);
    }

    private async Task RegistrarSalidaAsync()
    {
        var fila = Seleccionada;
        if (fila is null)
        {
            Advertencia("Selecciona una reserva de la agenda.");
            return;
        }

        if (!PedirHora("Hora de salida", out var hora))
        {
            return;
        }

        ResultadoSalida? resultado = null;
        var ok = await VistaAsync.EjecutarAsync(
            this,
            ControlesAcciones(),
            async () => resultado = await _servicio.RegistrarSalidaAsync(_sesion.IdUsuario!.Value, fila.IdReserva, hora),
            "No se pudo registrar la salida.");
        if (!ok || resultado is null)
        {
            return;
        }

        MessageBox.Show(
            this,
            $"Salida registrada para la reserva ID {resultado.IdReserva} (Finalizada).\n\n" +
            $"Importe exigible: {FormatoMonto(resultado.Importe)} ({resultado.Facturables} min facturables). Motivo: {resultado.Motivo}.",
            "Registrar salida",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        await VerAgendaAsync(false);
    }

    private async Task RegistrarPagoAsync()
    {
        var fila = Seleccionada;
        if (fila is null)
        {
            Advertencia("Selecciona una reserva de la agenda.");
            return;
        }

        decimal importe;
        using (var entrada = new EntradaTextoForm("Registrar pago", "Importe confirmado"))
        {
            if (entrada.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (!TryImporte(entrada.Valor, out importe))
            {
                Advertencia("Ingresa un importe valido.");
                return;
            }
        }

        string? referencia;
        using (var entrada = new EntradaTextoForm("Registrar pago", "Referencia (opcional)"))
        {
            if (entrada.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var texto = entrada.Valor.Trim();
            referencia = texto.Length == 0 ? null : texto;
        }

        ResultadoPago? resultado = null;
        var ok = await VistaAsync.EjecutarAsync(
            this,
            ControlesAcciones(),
            async () => resultado = await _servicio.RegistrarPagoAsync(_sesion.IdUsuario!.Value, fila.IdReserva, importe, referencia),
            "No se pudo registrar el pago.");
        if (!ok || resultado is null)
        {
            return;
        }

        var mensaje = $"Pago registrado para la reserva ID {resultado.IdReserva} por {FormatoMonto(resultado.Importe)}.";
        if (resultado.DeudaPagada)
        {
            mensaje += "\n\nLa deuda asociada quedo marcada como Pagada.";
        }

        MessageBox.Show(this, mensaje, "Registrar pago", MessageBoxButtons.OK, MessageBoxIcon.Information);
        await VerAgendaAsync(false);
    }

    private bool PedirHora(string campo, out TimeOnly hora)
    {
        hora = default;
        using var entrada = new EntradaTextoForm(campo, $"{campo} (HH:MM)");
        if (entrada.ShowDialog(this) != DialogResult.OK)
        {
            return false;
        }

        if (!TimeOnly.TryParseExact(entrada.Valor.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out hora))
        {
            Advertencia("Ingresa un horario valido en formato HH:MM.");
            return false;
        }

        return true;
    }

    private static bool TryImporte(string texto, out decimal importe)
    {
        return decimal.TryParse(
            texto.Trim().Replace(',', '.'),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out importe);
    }

    private static string FormatoMonto(decimal valor)
    {
        return valor.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private Control[] ControlesAcciones() =>
        [_fecha, _btnVerAgenda, _btnIngreso, _btnSalida, _btnPago, _grilla];

    private void Advertencia(string mensaje)
    {
        MessageBox.Show(this, mensaje, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void PrepararGrilla()
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
