namespace CoworkApp.Views.Forms
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }
        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            btnProbarConexion = new Button();
            dgvOficinas = new DataGridView();
            lblSesion = new Label();
            menuStrip = new MenuStrip();
            mnuOficinas = new ToolStripMenuItem();
            mnuCerrarSesion = new ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)dgvOficinas).BeginInit();
            menuStrip.SuspendLayout();
            SuspendLayout();
            btnProbarConexion.Dock = DockStyle.Top;
            btnProbarConexion.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnProbarConexion.Location = new Point(0,28);
            btnProbarConexion.Name = "btnProbarConexion";
            btnProbarConexion.Size = new Size(900,45);
            btnProbarConexion.TabIndex = 1;
            btnProbarConexion.Text = "Probar conexión y cargar oficinas";
            btnProbarConexion.UseVisualStyleBackColor = true;
            btnProbarConexion.Click += btnProbarConexion_Click;
            dgvOficinas.AllowUserToAddRows = false;
            dgvOficinas.AllowUserToDeleteRows = false;
            dgvOficinas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvOficinas.BackgroundColor = SystemColors.Window;
            dgvOficinas.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvOficinas.Dock = DockStyle.Fill;
            dgvOficinas.Location = new Point(0,73);
            dgvOficinas.Name = "dgvOficinas";
            dgvOficinas.ReadOnly = true;
            dgvOficinas.RowHeadersWidth = 51;
            dgvOficinas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvOficinas.Size = new Size(900,400);
            dgvOficinas.TabIndex = 2;
            lblSesion.Dock = DockStyle.Bottom;
            lblSesion.Location = new Point(0,473);
            lblSesion.Name = "lblSesion";
            lblSesion.Padding = new Padding(8, 0, 8, 0);
            lblSesion.Size = new Size(900,27);
            lblSesion.TabIndex = 3;
            lblSesion.Text = "";
            lblSesion.TextAlign = ContentAlignment.MiddleRight;
            mnuOficinas.Name = "mnuOficinas";
            mnuOficinas.Size = new Size(80, 28);
            mnuOficinas.Text = "&Oficinas";
            mnuCerrarSesion.Name = "mnuCerrarSesion";
            mnuCerrarSesion.Size = new Size(130, 28);
            mnuCerrarSesion.Text = "&Cerrar sesión";
            mnuCerrarSesion.Click += mnuCerrarSesion_Click;
            menuStrip.Dock = DockStyle.Top;
            menuStrip.ImageScalingSize = new Size(20, 20);
            menuStrip.Items.AddRange(new ToolStripItem[] { mnuOficinas, mnuCerrarSesion });
            menuStrip.Location = new Point(0,0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(900,28);
            menuStrip.TabIndex = 4;
            AutoScaleDimensions = new SizeF(8F,20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(900,500);
            MainMenuStrip = menuStrip;
            Controls.Add(dgvOficinas);
            Controls.Add(btnProbarConexion);
            Controls.Add(lblSesion);
            Controls.Add(menuStrip);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "CoworkApp - Gestión de Coworking";
            ((System.ComponentModel.ISupportInitialize)dgvOficinas).EndInit();
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            ResumeLayout(false);
        }
        #endregion
        private Button btnProbarConexion;
        private DataGridView dgvOficinas;
        private Label lblSesion;
        private MenuStrip menuStrip;
        private ToolStripMenuItem mnuOficinas;
        private ToolStripMenuItem mnuCerrarSesion;
    }
}
