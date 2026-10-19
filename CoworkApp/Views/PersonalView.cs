using CoworkApp.Core.Datos;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Servicios;

namespace CoworkApp.Views;

public sealed class PersonalView : UserControl
{
    public PersonalView(Db db)
    {
        ArgumentNullException.ThrowIfNull(db);
        Dock = DockStyle.Fill;

        var auditoria = new AuditoriaRepositorio(db);
        var usuarios = new UsuarioRepositorio(db);
        var edificios = new EdificioRepositorio(db);
        var pisos = new PisoRepositorio(db);
        var oficinas = new OficinaRepositorio(db);
        var empleados = new EmpleadoRepositorio(db);
        var empleadoOficinas = new EmpleadoOficinaRepositorio(db);

        var empleadoServicio = new EmpleadoServicio(empleados, usuarios, edificios, empleadoOficinas, auditoria, db);
        var asignacionServicio = new AsignacionServicio(empleados, oficinas, pisos, empleadoOficinas, auditoria, db);
        var edificioServicio = new EdificioServicio(edificios, auditoria, db);
        var oficinaServicio = new OficinaServicio(oficinas, pisos, auditoria, db);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CrearPagina("Empleados", new EmpleadosView(empleadoServicio, edificioServicio)));
        tabs.TabPages.Add(CrearPagina("Asignaciones", new AsignacionesView(empleadoServicio, asignacionServicio, oficinaServicio)));

        Controls.Add(tabs);
    }

    private static TabPage CrearPagina(string titulo, Control contenido)
    {
        var pagina = new TabPage(titulo) { Padding = new Padding(8) };
        contenido.Dock = DockStyle.Fill;
        pagina.Controls.Add(contenido);
        return pagina;
    }
}
