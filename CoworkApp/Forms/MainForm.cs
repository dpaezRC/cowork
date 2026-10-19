using CoworkApp.Core.Datos;
using CoworkApp.Core.Dominio;
using CoworkApp.Core.Seguridad;
using CoworkApp.Core.Servicios;
using CoworkApp.Core.Validacion;
using CoworkApp.Views;

namespace CoworkApp.Forms;

public sealed class MainForm : Form
{
    private readonly SesionActual _sesion;
    private readonly Db _db;
    private readonly FlowLayoutPanel _panelLateral = new();
    private readonly Panel _panelContenido = new();

    public bool CerrarSesionSolicitado { get; private set; }

    public MainForm(SesionActual sesion, Db db)
    {
        _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
        _db = db ?? throw new ArgumentNullException(nameof(db));

        ConfigurarFormulario();
        ConstruirControles();
        CargarModulos();
    }

    private void ConfigurarFormulario()
    {
        Text = "CoworkApp";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);
        ClientSize = new Size(1000, 640);
    }

    private void ConstruirControles()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var encabezado = CrearEncabezado();
        layout.Controls.Add(encabezado, 0, 0);
        layout.SetColumnSpan(encabezado, 2);

        _panelLateral.Dock = DockStyle.Fill;
        _panelLateral.FlowDirection = FlowDirection.TopDown;
        _panelLateral.WrapContents = false;
        _panelLateral.AutoScroll = true;
        _panelLateral.BackColor = Color.FromArgb(243, 244, 246);
        _panelLateral.Padding = new Padding(16);
        layout.Controls.Add(_panelLateral, 0, 1);

        _panelContenido.Dock = DockStyle.Fill;
        _panelContenido.BackColor = Color.White;
        _panelContenido.Padding = new Padding(24);
        layout.Controls.Add(_panelContenido, 1, 1);

        Controls.Add(layout);
    }

    private TableLayoutPanel CrearEncabezado()
    {
        var encabezado = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.FromArgb(31, 41, 55),
            Padding = new Padding(24, 0, 24, 0),
            Margin = Padding.Empty
        };
        encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        encabezado.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var identidad = new Label
        {
            Text = $"{_sesion.Nombre} ({_sesion.Rol})",
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        encabezado.Controls.Add(identidad, 0, 0);

        var cerrarSesion = new Button
        {
            Text = "Cerrar sesión",
            AutoSize = true,
            Anchor = AnchorStyles.Right
        };
        cerrarSesion.Click += (_, _) =>
        {
            CerrarSesionSolicitado = true;
            Close();
        };
        encabezado.Controls.Add(cerrarSesion, 1, 0);

        return encabezado;
    }

    private void CargarModulos()
    {
        var modulos = MenuServicio.ObtenerModulos(_sesion.Rol);

        if (modulos.Count == 0)
        {
            MostrarMensaje("Aun no tenes modulos habilitados para tu rol.");
            return;
        }

        foreach (var definicion in modulos)
        {
            var boton = new Button
            {
                Text = definicion.Descripcion,
                Width = 180,
                Height = 44,
                Margin = new Padding(0, 0, 0, 8),
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat
            };
            boton.Click += (_, _) => MostrarModulo(definicion);
            _panelLateral.Controls.Add(boton);
        }

        MostrarMensaje("Selecciona un modulo.");
    }

    private void MostrarModulo(DefinicionModulo definicion)
    {
        try
        {
            MenuServicio.ValidarAcceso(_sesion.Rol, definicion.Modulo);
        }
        catch (ReglaNegocioException ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Acceso denegado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        LimpiarContenido();

        if (definicion.Modulo == Modulo.Espacios)
        {
            _panelContenido.Controls.Add(new EspaciosView(_db));
            return;
        }

        if (definicion.Modulo == Modulo.Personal)
        {
            _panelContenido.Controls.Add(new PersonalView(_db));
            return;
        }

        if (definicion.Modulo == Modulo.Usuarios)
        {
            _panelContenido.Controls.Add(new UsuariosView(_db));
            return;
        }

        if (definicion.Modulo == Modulo.Reservas)
        {
            _panelContenido.Controls.Add(new ReservasView(_db, _sesion));
            return;
        }

        if (definicion.Modulo == Modulo.ControlOperativo)
        {
            _panelContenido.Controls.Add(new ControlOperativoView(_db, _sesion));
            return;
        }

        _panelContenido.Controls.Add(new ModuloPendienteControl(definicion.Descripcion));
    }

    private void MostrarMensaje(string mensaje)
    {
        LimpiarContenido();
        _panelContenido.Controls.Add(new Label
        {
            Text = mensaje,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = SystemColors.GrayText
        });
    }

    private void LimpiarContenido()
    {
        while (_panelContenido.Controls.Count > 0)
        {
            var control = _panelContenido.Controls[0];
            _panelContenido.Controls.RemoveAt(0);
            control.Dispose();
        }
    }
}
