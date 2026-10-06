namespace CoworkApp
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
            ((System.ComponentModel.ISupportInitialize)dgvOficinas).BeginInit();
            SuspendLayout();
            btnProbarConexion.Dock = DockStyle.Top;
            btnProbarConexion.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnProbarConexion.Location = new Point(0,0);
            btnProbarConexion.Name = "btnProbarConexion";
            btnProbarConexion.Size = new Size(900,45);
            btnProbarConexion.TabIndex = 0;
            btnProbarConexion.Text = "Probar conexión y cargar oficinas";
            btnProbarConexion.UseVisualStyleBackColor = true;
            btnProbarConexion.Click += btnProbarConexion_Click;
            dgvOficinas.AllowUserToAddRows = false;
            dgvOficinas.AllowUserToDeleteRows = false;
            dgvOficinas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvOficinas.BackgroundColor = SystemColors.Window;
            dgvOficinas.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvOficinas.Dock = DockStyle.Fill;
            dgvOficinas.Location = new Point(0,45);
            dgvOficinas.Name = "dgvOficinas";
            dgvOficinas.ReadOnly = true;
            dgvOficinas.RowHeadersWidth = 51;
            dgvOficinas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvOficinas.Size = new Size(900,455);
            dgvOficinas.TabIndex = 1;
            AutoScaleDimensions = new SizeF(8F,20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(900,500);
            Controls.Add(dgvOficinas);
            Controls.Add(btnProbarConexion);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "CoworkApp - Gestión de Coworking";
            ((System.ComponentModel.ISupportInitialize)dgvOficinas).EndInit();
            ResumeLayout(false);
        }
        #endregion
        private Button btnProbarConexion;
        private DataGridView dgvOficinas;
    }
}
