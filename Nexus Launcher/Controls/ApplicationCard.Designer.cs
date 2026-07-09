namespace Nexus_Launcher.Controls
{
    partial class ApplicationCard
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
            this.components = new System.ComponentModel.Container();
            this.panelControl1 = new DevExpress.XtraEditors.PanelControl();
            this.pictureEdit2 = new DevExpress.XtraEditors.PictureEdit();
            this.dropDownButton1 = new DevExpress.XtraEditors.DropDownButton();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.pictureEdit1 = new DevExpress.XtraEditors.PictureEdit();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit2.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit1.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // panelControl1
            // 
            this.panelControl1.Controls.Add(this.pictureEdit2);
            this.panelControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelControl1.Location = new System.Drawing.Point(0, 0);
            this.panelControl1.Name = "panelControl1";
            this.panelControl1.Size = new System.Drawing.Size(841, 217);
            this.panelControl1.TabIndex = 1;
            // 
            // pictureEdit2
            // 
            this.pictureEdit2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureEdit2.Location = new System.Drawing.Point(2, 2);
            this.pictureEdit2.Name = "pictureEdit2";
            this.pictureEdit2.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.pictureEdit2.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.pictureEdit2.Properties.ShowEditMenuItem = DevExpress.Utils.DefaultBoolean.False;
            this.pictureEdit2.Properties.ShowMenu = false;
            this.pictureEdit2.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Stretch;
            this.pictureEdit2.Size = new System.Drawing.Size(837, 213);
            this.pictureEdit2.TabIndex = 0;
            // 
            // dropDownButton1
            // 
            this.dropDownButton1.DropDownArrowStyle = DevExpress.XtraEditors.DropDownArrowStyle.Hide;
            this.dropDownButton1.Location = new System.Drawing.Point(183, 374);
            this.dropDownButton1.Name = "dropDownButton1";
            this.dropDownButton1.ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False;
            this.dropDownButton1.Size = new System.Drawing.Size(200, 44);
            this.dropDownButton1.TabIndex = 2;
            this.dropDownButton1.Text = "Play";
            this.dropDownButton1.Click += new System.EventHandler(this.dropDownButton1_Click);
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 16F);
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(183, 234);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(54, 25);
            this.labelControl1.TabIndex = 3;
            this.labelControl1.Text = "Name";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // pictureEdit1
            // 
            this.pictureEdit1.Location = new System.Drawing.Point(19, 223);
            this.pictureEdit1.Name = "pictureEdit1";
            this.pictureEdit1.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.pictureEdit1.Properties.ShowEditMenuItem = DevExpress.Utils.DefaultBoolean.False;
            this.pictureEdit1.Properties.ShowMenu = false;
            this.pictureEdit1.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Stretch;
            this.pictureEdit1.Size = new System.Drawing.Size(158, 224);
            this.pictureEdit1.TabIndex = 0;
            // 
            // labelControl2
            // 
            this.labelControl2.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Location = new System.Drawing.Point(183, 315);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(57, 19);
            this.labelControl2.TabIndex = 4;
            this.labelControl2.Text = "DEBUG:";
            // 
            // ApplicationCard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.labelControl2);
            this.Controls.Add(this.labelControl1);
            this.Controls.Add(this.dropDownButton1);
            this.Controls.Add(this.panelControl1);
            this.Controls.Add(this.pictureEdit1);
            this.DoubleBuffered = true;
            this.Name = "ApplicationCard";
            this.Size = new System.Drawing.Size(841, 542);
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).EndInit();
            this.panelControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit2.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit1.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraEditors.DropDownButton dropDownButton1;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private System.Windows.Forms.Timer timer1;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        public DevExpress.XtraEditors.PictureEdit pictureEdit2;
        public DevExpress.XtraEditors.PictureEdit pictureEdit1;
    }
}
