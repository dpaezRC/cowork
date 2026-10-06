using CoworkApp.Features.H.Login;
using CoworkApp.Shared.Security;
using CoworkApp.Shared.Time;

namespace CoworkApp.Views.Forms
{
    /// <summary>
    /// US-25 — sólo arma el Request, lo manda al Handler y pinta el resultado
    /// (AGENTS.md §2.2: cero lógica de negocio en los Forms).
    /// </summary>
    public partial class LoginForm : Form
    {
        private readonly LoginHandler _handler;
        private readonly CurrentUser _usuario;
        private readonly IClock _reloj;

        public LoginForm(LoginHandler handler, CurrentUser usuario, IClock reloj)
        {
            _handler = handler;
            _usuario = usuario;
            _reloj = reloj;
            InitializeComponent();
        }

        private async void btnIngresar_Click(object sender, EventArgs e)
        {
            btnIngresar.Enabled = false;
            try
            {
                var resultado = await _handler.HandleAsync(
                    new LoginRequest(txtEmail.Text.Trim(), txtPassword.Text));

                if (!resultado.IsSuccess)
                {
                    MessageBox.Show(resultado.Error, "No se pudo iniciar sesión",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                var login = resultado.Value!;
                _usuario.Establecer(login.UsuarioId, login.Nombre, login.Email, login.Rol, _reloj.Now);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnIngresar.Enabled = true;
            }
        }
    }
}
