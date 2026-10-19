using CoworkApp.Core.Seguridad;
using CoworkApp.Core.Servicios;
using CoworkApp.Core.Validacion;

namespace CoworkApp.Forms;

public sealed class LoginForm : Form
{
    private readonly AutenticacionServicio _autenticacion;
    private readonly SesionActual _sesion;
    private readonly TextBox _email = new();
    private readonly TextBox _contrasena = new();
    private readonly Button _btnIngresar = new();
    private readonly Button _btnSalir = new();
    private readonly Label _mensajeError = new();
    private bool _procesando;

    public LoginForm(AutenticacionServicio autenticacion, SesionActual sesion)
    {
        _autenticacion = autenticacion ?? throw new ArgumentNullException(nameof(autenticacion));
        _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));

        ConfigurarFormulario();
        ConstruirControles();
    }

    private void ConfigurarFormulario()
    {
        Text = "CoworkApp - Iniciar sesión";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(440, 300);
    }

    private void ConstruirControles()
    {
        var contenedor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(24)
        };

        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        var titulo = new Label
        {
            Text = "Iniciar sesión",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        contenedor.Controls.Add(titulo, 0, 0);
        contenedor.SetColumnSpan(titulo, 2);

        _email.Dock = DockStyle.Fill;
        _email.Margin = new Padding(0, 6, 0, 6);
        contenedor.Controls.Add(CrearEtiqueta("Email"), 0, 1);
        contenedor.Controls.Add(_email, 1, 1);

        _contrasena.Dock = DockStyle.Fill;
        _contrasena.UseSystemPasswordChar = true;
        _contrasena.Margin = new Padding(0, 6, 0, 6);
        contenedor.Controls.Add(CrearEtiqueta("Contraseña"), 0, 2);
        contenedor.Controls.Add(_contrasena, 1, 2);

        _mensajeError.Dock = DockStyle.Fill;
        _mensajeError.ForeColor = Color.Firebrick;
        _mensajeError.TextAlign = ContentAlignment.MiddleLeft;
        contenedor.Controls.Add(_mensajeError, 0, 3);
        contenedor.SetColumnSpan(_mensajeError, 2);

        _btnIngresar.Text = "Ingresar";
        _btnIngresar.AutoSize = true;
        _btnIngresar.Padding = new Padding(16, 4, 16, 4);
        _btnIngresar.Click += Ingresar_Click;

        _btnSalir.Text = "Salir";
        _btnSalir.AutoSize = true;
        _btnSalir.Padding = new Padding(16, 4, 16, 4);
        _btnSalir.DialogResult = DialogResult.Cancel;

        var acciones = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        acciones.Controls.Add(_btnIngresar);
        acciones.Controls.Add(_btnSalir);
        contenedor.Controls.Add(acciones, 0, 4);
        contenedor.SetColumnSpan(acciones, 2);

        Controls.Add(contenedor);

        AcceptButton = _btnIngresar;
        CancelButton = _btnSalir;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ActiveControl = _email;
    }

    private static Label CrearEtiqueta(string texto)
    {
        return new Label
        {
            Text = texto,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private async void Ingresar_Click(object? sender, EventArgs e)
    {
        if (_procesando)
        {
            return;
        }

        _procesando = true;
        _mensajeError.Text = string.Empty;
        EstablecerHabilitado(false);

        try
        {
            var usuario = await _autenticacion.AutenticarAsync(_email.Text, _contrasena.Text);
            _sesion.Iniciar(usuario.IdUsuario, $"{usuario.Nombre} {usuario.Apellido}", usuario.Email, usuario.Rol);
            DialogResult = DialogResult.OK;
        }
        catch (ReglaNegocioException ex)
        {
            _mensajeError.Text = ex.Message;
        }
        catch (Exception)
        {
            _mensajeError.Text = "No se pudo iniciar sesion. Intenta nuevamente.";
        }
        finally
        {
            _procesando = false;
            EstablecerHabilitado(true);
        }
    }

    private void EstablecerHabilitado(bool habilitado)
    {
        _email.Enabled = habilitado;
        _contrasena.Enabled = habilitado;
        _btnIngresar.Enabled = habilitado;
        _btnSalir.Enabled = habilitado;
        UseWaitCursor = !habilitado;
    }
}
