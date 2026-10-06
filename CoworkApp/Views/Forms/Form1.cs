using CoworkApp.Features.A.EdificiosOficinas.ListarOficinas;
using CoworkApp.Shared.Configuration;
using CoworkApp.Shared.Security;
using CoworkApp.Shared.Time;

namespace CoworkApp.Views.Forms
{
    public enum MotivoCierre
    {
        /// <summary>El usuario cerró la ventana con la X: termina la app.</summary>
        Usuario,
        /// <summary>Pidió cerrar sesión: vuelve al login.</summary>
        SesionCerrada,
        /// <summary>Venció el tiempo de inactividad (US-25): vuelve al login.</summary>
        Inactividad,
    }

    public partial class Form1 : Form
    {
        private static readonly Dictionary<string, string> Encabezados = new()
        {
            ["Id"] = "Id Oficina",
            ["Nombre"] = "Nombre Oficina",
            ["Tipo"] = "Tipo",
            ["CapacidadTotal"] = "Capacidad Total",
            ["PrecioPorMinuto"] = "Precio/Minuto (€)",
            ["Edificio"] = "Edificio",
            ["PisoNumero"] = "Piso Nº",
            ["PisoDescripcion"] = "Descripción Piso",
            ["Activa"] = "Activa",
        };

        private readonly ListarOficinasHandler _listarOficinas;
        private readonly CurrentUser _usuario;
        private readonly IClock _reloj;
        private readonly TimeSpan _inactividadMaxima;
        private readonly System.Windows.Forms.Timer _timerSesion;
        private readonly FiltroActividad _filtroActividad;

        /// <summary>Por qué se cerró: decide si la app termina o vuelve al login.</summary>
        public MotivoCierre Motivo { get; private set; } = MotivoCierre.Usuario;

        public Form1(
            ListarOficinasHandler listarOficinas,
            CurrentUser usuario,
            IClock reloj,
            SesionOptions sesion)
        {
            _listarOficinas = listarOficinas;
            _usuario = usuario;
            _reloj = reloj;
            _inactividadMaxima = TimeSpan.FromMinutes(sesion.MinutosInactividad);

            InitializeComponent();

            lblSesion.Text = $"{_usuario.Nombre} · {_usuario.Rol}";
            Text = $"CoworkApp - {_usuario.Nombre}";
            ConfigurarMenuSegunRol();

            dgvOficinas.CellFormatting += DgvOficinas_CellFormatting;

            _usuario.RegistrarActividad(_reloj.Now);
            _filtroActividad = new FiltroActividad(() => _usuario.RegistrarActividad(_reloj.Now));
            Application.AddMessageFilter(_filtroActividad);

            _timerSesion = new System.Windows.Forms.Timer { Interval = 30_000 };
            _timerSesion.Tick += TimerSesion_Tick;
            _timerSesion.Start();
        }

        private void ConfigurarMenuSegunRol()
        {
            // Los items disponibles hoy son sólo éstos; el menú distinto por rol
            // se completa a medida que entren las demás historias.
            mnuOficinas.Text = _usuario.EsCliente switch
            {
                true => "&Catálogo de oficinas",
                false when _usuario.EsAdmin => "&Edificios y oficinas",
                false => "&Oficinas",
            };
        }

        private async void btnProbarConexion_Click(object sender, EventArgs e)
        {
            btnProbarConexion.Enabled = false;
            try
            {
                var oficinas = await _listarOficinas.HandleAsync(new ListarOficinasRequest());
                Pintar(oficinas);
                MessageBox.Show(
                    $"Conexión exitosa. Oficinas activas cargadas: {oficinas.Count}",
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo cargar el listado de oficinas: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnProbarConexion.Enabled = true;
            }
        }

        private void Pintar(IReadOnlyList<ListarOficinasRow> oficinas)
        {
            dgvOficinas.DataSource = oficinas.ToList();

            foreach (var (nombrePropiedad, texto) in Encabezados)
            {
                if (dgvOficinas.Columns[nombrePropiedad] is { } columna)
                    columna.HeaderText = texto;
            }

            if (dgvOficinas.Columns["PrecioPorMinuto"] is { } colPrecio)
                colPrecio.DefaultCellStyle.Format = "N4";
        }

        private void DgvOficinas_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvOficinas.Columns[e.ColumnIndex]?.Name == "Activa" && e.Value is bool activa)
            {
                e.Value = activa ? "Sí" : "No";
                e.FormattingApplied = true;
            }
        }

        private void mnuCerrarSesion_Click(object? sender, EventArgs e)
        {
            _usuario.CerrarSesion();
            Motivo = MotivoCierre.SesionCerrada;
            Close();
        }

        private void TimerSesion_Tick(object? sender, EventArgs e)
        {
            if (!_usuario.ExpiradaPorInactividad(_reloj.Now, _inactividadMaxima))
                return;

            _timerSesion.Stop();
            _usuario.CerrarSesion();
            Motivo = MotivoCierre.Inactividad;

            MessageBox.Show(
                $"La sesión se cerró por inactividad ({_inactividadMaxima.TotalMinutes:0} minutos).",
                "Sesión cerrada", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _timerSesion.Stop();
            _timerSesion.Dispose();
            Application.RemoveMessageFilter(_filtroActividad);
            base.OnFormClosed(e);
        }

        /// <summary>
        /// Cualquier tecla o click, incluidos los que se hacen sobre controles
        /// hijos, renueva la actividad de la sesión.
        /// </summary>
        private sealed class FiltroActividad(Action actividad) : IMessageFilter
        {
            private const int WmMouseMove = 0x0200;
            private const int WmKeyDown = 0x0100;
            private const int WmLButtonDown = 0x0201;
            private const int WmRButtonDown = 0x0204;
            private const int WmMouseWheel = 0x020A;

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg is WmMouseMove or WmKeyDown or WmLButtonDown or WmRButtonDown or WmMouseWheel)
                    actividad();
                return false;
            }
        }
    }
}
