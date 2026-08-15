namespace Nexus_Launcher.Controls
{
    partial class LibraryDetailControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTitle = new DevExpress.XtraEditors.LabelControl();
            this.lblLauncher = new DevExpress.XtraEditors.LabelControl();
            this.btnPlay = new DevExpress.XtraEditors.SimpleButton();
            this.btnFavorite = new DevExpress.XtraEditors.SimpleButton();
            this.btnStore = new DevExpress.XtraEditors.SimpleButton();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.Appearance.Font = new System.Drawing.Font("Microsoft Sans Serif", 25F);
            this.lblTitle.Appearance.Options.UseFont = true;
            this.lblTitle.Location = new System.Drawing.Point(19, 13);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(191, 38);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "labelControl1";
            // 
            // lblLauncher
            // 
            this.lblLauncher.Appearance.Font = new System.Drawing.Font("Microsoft Sans Serif", 12.25F);
            this.lblLauncher.Appearance.Options.UseFont = true;
            this.lblLauncher.Location = new System.Drawing.Point(19, 53);
            this.lblLauncher.Name = "lblLauncher";
            this.lblLauncher.Size = new System.Drawing.Size(93, 20);
            this.lblLauncher.TabIndex = 1;
            this.lblLauncher.Text = "labelControl1";
            // 
            // btnPlay
            // 
            this.btnPlay.Appearance.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.25F);
            this.btnPlay.Appearance.Options.UseFont = true;
            this.btnPlay.Location = new System.Drawing.Point(19, 79);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Light;
            this.btnPlay.Size = new System.Drawing.Size(106, 58);
            this.btnPlay.TabIndex = 2;
            this.btnPlay.Text = "Play Now";
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
            // 
            // btnFavorite
            // 
            this.btnFavorite.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnFavorite.Location = new System.Drawing.Point(19, 139);
            this.btnFavorite.Name = "btnFavorite";
            this.btnFavorite.Size = new System.Drawing.Size(75, 28);
            this.btnFavorite.TabIndex = 3;
            this.btnFavorite.Text = "simpleButton2";
            this.btnFavorite.Visible = false;
            // 
            // btnStore
            // 
            this.btnStore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnStore.Location = new System.Drawing.Point(100, 139);
            this.btnStore.Name = "btnStore";
            this.btnStore.Size = new System.Drawing.Size(75, 28);
            this.btnStore.TabIndex = 4;
            this.btnStore.Text = "simpleButton3";
            this.btnStore.Visible = false;
            // 
            // LibraryDetailControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnStore);
            this.Controls.Add(this.btnFavorite);
            this.Controls.Add(this.btnPlay);
            this.Controls.Add(this.lblLauncher);
            this.Controls.Add(this.lblTitle);
            this.DoubleBuffered = true;
            this.Name = "LibraryDetailControl";
            this.Size = new System.Drawing.Size(350, 175);
            this.Load += new System.EventHandler(this.LibraryHoverCard_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.XtraEditors.LabelControl lblTitle;
        private DevExpress.XtraEditors.LabelControl lblLauncher;
        private DevExpress.XtraEditors.SimpleButton btnPlay;
        private DevExpress.XtraEditors.SimpleButton btnFavorite;
        private DevExpress.XtraEditors.SimpleButton btnStore;
    }
}
