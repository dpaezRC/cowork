using CoworkApp.Core.Datos;
using CoworkApp.Core.Repositorios;
using CoworkApp.Core.Servicios;

namespace CoworkApp.Views;

public sealed class EspaciosView : UserControl
{
    public EspaciosView(Db db)
    {
        ArgumentNullException.ThrowIfNull(db);
        Dock = DockStyle.Fill;

        var auditoria = new AuditoriaRepositorio(db);
        var edificios = new EdificioRepositorio(db);
        var pisos = new PisoRepositorio(db);
        var oficinas = new OficinaRepositorio(db);

        var edificioServicio = new EdificioServicio(edificios, auditoria, db);
        var pisoServicio = new PisoServicio(pisos, edificios, auditoria, db);
        var oficinaServicio = new OficinaServicio(oficinas, pisos, auditoria, db);
        var cascadaServicio = new CascadaServicio(edificios, pisos, oficinas, auditoria, db);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CrearPagina(
            "Oficinas",
            new OficinasView(edificioServicio, pisoServicio, oficinaServicio, cascadaServicio)));
        tabs.TabPages.Add(CrearPagina("Edificios", new EdificiosView(edificioServicio)));
        tabs.TabPages.Add(CrearPagina("Pisos", new PisosView(edificioServicio, pisoServicio)));

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
