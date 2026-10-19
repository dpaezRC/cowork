using CoworkApp.Core.Datos;
using CoworkApp.Core.Dominio;
using CoworkApp.Core.Modelos;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Servicios;
using CoworkApp.Forms;

namespace CoworkApp.Views;

public sealed class UsuariosView : UserControl
{
    private readonly UsuarioServicio _servicio;
    private readonly DataGridView _grilla = new();
    private readonly Button _btnNuevo = new() { Text = "Nuevo", AutoSize = true };
    private readonly Button _btnEditar = new() { Text = "Editar", AutoSize = true };
    private readonly Button _btnBaja = new() { Text = "Dar de baja", AutoSize = true };
    private readonly Button _btnActualizar = new() { Text = "Actualizar", AutoSize = true };

    public UsuariosView(Db db)
    {
        ArgumentNullException.ThrowIfNull(db);
        Dock = DockStyle.Fill;

        var auditoria = new AuditoriaRepositorio(db);
        var usuarios = new UsuarioRepositorio(db);
        _servicio = new UsuarioServicio(usuarios, auditoria, db);

        ConstruirControles();

        Load += async (_, _) => await CargarAsync();
    }

    private IReadOnlyList<Control> Controles => [_btnNuevo, _btnEditar, _btnBaja, _btnActualizar];

    private Usuario? Seleccionado => _grilla.SelectedRows.Count > 0 ? _grilla.SelectedRows[0].Tag as Usuario : null;

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
        _grilla.Columns.Add(CrearColumna("Rol", "Rol", 120, DataGridViewAutoSizeColumnMode.None));
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
                var usuarios = await _servicio.ListarAsync();
                _grilla.Rows.Clear();
                foreach (var usuario in usuarios)
                {
                    var indice = _grilla.Rows.Add(
                        usuario.IdUsuario,
                        usuario.Nombre,
                        usuario.Apellido,
                        usuario.Email,
                        usuario.Telefono ?? string.Empty,
                        usuario.Rol,
                        usuario.Estado);
                    _grilla.Rows[indice].Tag = usuario;
                }
            },
            "No se pudieron cargar los usuarios.");
    }

    private async Task NuevoAsync()
    {
        using var formulario = new UsuarioForm(Roles.Gestionables, null);
        if (formulario.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var creado = await VistaAsync.EjecutarAsync(
            this,
            Controles,
            () => _servicio.CrearAsync(
                formulario.Nombre,
                formulario.Apellido,
                formulario.Email,
                formulario.Telefono,
                formulario.Contrasena,
                formulario.Rol),
            "No se pudo registrar el usuario.");

        if (creado)
        {
            await CargarAsync();
        }
    }

    private async Task EditarAsync()
    {
        var usuario = Seleccionado;
        if (usuario is null)
        {
            return;
        }

        using var formulario = new UsuarioForm(Roles.Gestionables, usuario);
        if (formulario.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var cambio = false;
        var actualizado = await VistaAsync.EjecutarAsync(
            this,
            Controles,
            async () => cambio = await _servicio.ActualizarAsync(
                usuario.IdUsuario,
                formulario.Nombre,
                formulario.Apellido,
                formulario.Email,
                formulario.Telefono,
                formulario.Estado,
                formulario.Contrasena),
            "No se pudo actualizar el usuario.");

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
        var usuario = Seleccionado;
        if (usuario is null)
        {
            return;
        }

        if (usuario.Estado == Estados.Inactivo)
        {
            MessageBox.Show(
                this,
                $"El usuario '{usuario.Nombre} {usuario.Apellido}' ya esta dado de baja.",
                "Aviso",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var confirmacion = MessageBox.Show(
            this,
            $"El usuario '{usuario.Nombre} {usuario.Apellido}' pasara a 'Inactivo'. El registro se conserva." +
            $"\n\n¿Dar de baja al usuario '{usuario.Nombre} {usuario.Apellido}'?",
            "Dar de baja usuario",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirmacion != DialogResult.Yes)
        {
            return;
        }

        var dadoDeBaja = await VistaAsync.EjecutarAsync(
            this,
            Controles,
            () => _servicio.DarDeBajaAsync(usuario.IdUsuario),
            "No se pudo dar de baja el usuario.");

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
