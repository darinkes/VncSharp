namespace VncSharpTest
{
    partial class Form1
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.remoteDesktop1 = new VncSharp.RemoteDesktop();
            this.SuspendLayout();
            // 
            // remoteDesktop1
            // 
            this.remoteDesktop1.AutoScroll = true;
            this.remoteDesktop1.AutoScrollMinSize = new System.Drawing.Size(608, 427);
            this.remoteDesktop1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.remoteDesktop1.Location = new System.Drawing.Point(0, 0);
            this.remoteDesktop1.Name = "remoteDesktop1";
            this.remoteDesktop1.Size = new System.Drawing.Size(1008, 747);
            this.remoteDesktop1.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 747);
            this.Controls.Add(this.remoteDesktop1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private VncSharp.RemoteDesktop remoteDesktop1;
    }
}

