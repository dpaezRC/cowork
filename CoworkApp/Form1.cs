using CoworkApp.Data;
using Microsoft.Data.SqlClient;
using System.Configuration;
using System.Data;

namespace CoworkApp
{
    public partial class Form1 : Form
    {
        private readonly Database _db;

        public Form1()
        {
            InitializeComponent();
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection' en App.config.");
            _db = new Database(connectionString);
        }

        private async void btnProbarConexion_Click(object sender, EventArgs e)
        {
            btnProbarConexion.Enabled = false;
            try
            {
                bool conectado = await _db.TestConnectionAsync();
                if (!conectado)
                {
                    MessageBox.Show("No se pudo establecer conexión a SQL Server.", "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                using var conn = _db.GetConnection();
                await conn.OpenAsync();
                const string query = @"
                    SELECT 
                        o.Id AS [Id Oficina],
                        o.Nombre AS [Nombre Oficina],
                        o.Tipo AS [Tipo],
                        o.CapacidadTotal AS [Capacidad Total],
                        o.PrecioPorMinuto AS [Precio/Minuto (€)],
                        e.Nombre AS [Edificio],
                        p.Numero AS [Piso Nº],
                        p.Descripcion AS [Descripción Piso],
                        CASE WHEN o.Activa = 1 THEN 'Sí' ELSE 'No' END AS [Activa]
                    FROM dbo.Oficinas o
                    INNER JOIN dbo.Pisos p ON o.PisoId = p.Id
                    INNER JOIN dbo.Edificios e ON p.EdificioId = e.Id
                    WHERE o.Activa = 1
                    ORDER BY e.Nombre, p.Numero, o.Nombre;";
                using var cmd = new SqlCommand(query, conn);
                using var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dgvOficinas.DataSource = dt;
                if (dgvOficinas.Columns["Precio/Minuto (€)"] is { } colPrecio)
                    colPrecio.DefaultCellStyle.Format = "N4";
                MessageBox.Show($"Conexión exitosa. Oficinas activas cargadas: {dt.Rows.Count}", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error de SQL Server: {ex.Message}", "Error SQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnProbarConexion.Enabled = true;
            }
        }
    }
}
