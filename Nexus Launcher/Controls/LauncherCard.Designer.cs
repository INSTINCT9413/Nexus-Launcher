namespace Nexus_Launcher.Controls
{
    partial class LauncherCard
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
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.simpleButton1 = new DevExpress.XtraEditors.SimpleButton();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.pictureEdit1 = new DevExpress.XtraEditors.PictureEdit();
            this.stepProgress1 = new HorizonUI.StepProgress();
            this.stepProgress2 = new HorizonUI.StepProgress();
            this.stepProgress3 = new HorizonUI.StepProgress();
            this.stepProgress4 = new HorizonUI.StepProgress();
            this.xtraTabControl1 = new DevExpress.XtraTab.XtraTabControl();
            this.xtraTabPage1 = new DevExpress.XtraTab.XtraTabPage();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.xtraTabPage4 = new DevExpress.XtraTab.XtraTabPage();
            this.xtraTabPage3 = new DevExpress.XtraTab.XtraTabPage();
            this.xtraTabPage2 = new DevExpress.XtraTab.XtraTabPage();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xtraTabControl1)).BeginInit();
            this.xtraTabControl1.SuspendLayout();
            this.xtraTabPage1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.xtraTabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelControl1
            // 
            this.labelControl1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 18F);
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Horizontal;
            this.labelControl1.Location = new System.Drawing.Point(16, 16);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(167, 29);
            this.labelControl1.TabIndex = 1;
            this.labelControl1.Text = "Launcher Name";
            this.labelControl1.TextChanged += new System.EventHandler(this.labelControl1_TextChanged);
            // 
            // simpleButton1
            // 
            this.simpleButton1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.simpleButton1.Location = new System.Drawing.Point(363, 455);
            this.simpleButton1.Name = "simpleButton1";
            this.simpleButton1.ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False;
            this.simpleButton1.Size = new System.Drawing.Size(119, 45);
            this.simpleButton1.TabIndex = 7;
            this.simpleButton1.Text = "Reset";
            this.simpleButton1.Click += new System.EventHandler(this.simpleButton1_Click);
            // 
            // labelControl2
            // 
            this.labelControl2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.labelControl2.Appearance.Font = new System.Drawing.Font("Tahoma", 18F);
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Location = new System.Drawing.Point(390, 408);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(60, 29);
            this.labelControl2.TabIndex = 8;
            this.labelControl2.Text = "Stage";
            // 
            // pictureEdit1
            // 
            this.pictureEdit1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.pictureEdit1.EditValue = global::Nexus_Launcher.Properties.Resources.Steam_icon_logo_svg;
            this.pictureEdit1.Location = new System.Drawing.Point(322, 87);
            this.pictureEdit1.Name = "pictureEdit1";
            this.pictureEdit1.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.pictureEdit1.Properties.Appearance.BorderColor = System.Drawing.Color.Transparent;
            this.pictureEdit1.Properties.Appearance.ForeColor = System.Drawing.Color.Transparent;
            this.pictureEdit1.Properties.Appearance.Options.UseBackColor = true;
            this.pictureEdit1.Properties.Appearance.Options.UseBorderColor = true;
            this.pictureEdit1.Properties.Appearance.Options.UseForeColor = true;
            this.pictureEdit1.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.pictureEdit1.Properties.ReadOnly = true;
            this.pictureEdit1.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.pictureEdit1.Properties.ShowMenu = false;
            this.pictureEdit1.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom;
            this.pictureEdit1.Size = new System.Drawing.Size(200, 200);
            this.pictureEdit1.TabIndex = 0;
            // 
            // stepProgress1
            // 
            this.stepProgress1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.stepProgress1.BackColor = System.Drawing.Color.Transparent;
            this.stepProgress1.ImageTintColor = System.Drawing.Color.FromArgb(((int)(((byte)(124)))), ((int)(((byte)(252)))), ((int)(((byte)(0)))));
            this.stepProgress1.Location = new System.Drawing.Point(230, 360);
            this.stepProgress1.Name = "stepProgress1";
            this.stepProgress1.ProgressColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(205)))), ((int)(((byte)(50)))));
            this.stepProgress1.Size = new System.Drawing.Size(111, 32);
            this.stepProgress1.TabIndex = 2;
            this.stepProgress1.Text = "stepProgress1";
            this.stepProgress1.Value = 0;
            // 
            // stepProgress2
            // 
            this.stepProgress2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.stepProgress2.BackColor = System.Drawing.Color.Transparent;
            this.stepProgress2.ImageTintColor = System.Drawing.Color.FromArgb(((int)(((byte)(124)))), ((int)(((byte)(252)))), ((int)(((byte)(0)))));
            this.stepProgress2.Location = new System.Drawing.Point(341, 360);
            this.stepProgress2.Name = "stepProgress2";
            this.stepProgress2.ProgressColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(205)))), ((int)(((byte)(50)))));
            this.stepProgress2.Size = new System.Drawing.Size(121, 32);
            this.stepProgress2.TabIndex = 3;
            this.stepProgress2.Text = "stepProgress2";
            this.stepProgress2.Value = 0;
            // 
            // stepProgress3
            // 
            this.stepProgress3.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.stepProgress3.BackColor = System.Drawing.Color.Transparent;
            this.stepProgress3.ImageTintColor = System.Drawing.Color.FromArgb(((int)(((byte)(124)))), ((int)(((byte)(252)))), ((int)(((byte)(0)))));
            this.stepProgress3.Location = new System.Drawing.Point(462, 360);
            this.stepProgress3.Name = "stepProgress3";
            this.stepProgress3.ProgressColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(205)))), ((int)(((byte)(50)))));
            this.stepProgress3.Size = new System.Drawing.Size(121, 32);
            this.stepProgress3.TabIndex = 4;
            this.stepProgress3.Text = "stepProgress3";
            this.stepProgress3.Value = 0;
            // 
            // stepProgress4
            // 
            this.stepProgress4.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.stepProgress4.BackColor = System.Drawing.Color.Transparent;
            this.stepProgress4.ImageTintColor = System.Drawing.Color.FromArgb(((int)(((byte)(124)))), ((int)(((byte)(252)))), ((int)(((byte)(0)))));
            this.stepProgress4.Location = new System.Drawing.Point(583, 360);
            this.stepProgress4.Name = "stepProgress4";
            this.stepProgress4.ProgressColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(205)))), ((int)(((byte)(50)))));
            this.stepProgress4.Size = new System.Drawing.Size(32, 32);
            this.stepProgress4.TabIndex = 5;
            this.stepProgress4.Text = "stepProgress4";
            this.stepProgress4.Value = 0;
            // 
            // xtraTabControl1
            // 
            this.xtraTabControl1.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.xtraTabControl1.Appearance.Options.UseBackColor = true;
            this.xtraTabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.xtraTabControl1.Location = new System.Drawing.Point(0, 0);
            this.xtraTabControl1.Name = "xtraTabControl1";
            this.xtraTabControl1.SelectedTabPage = this.xtraTabPage1;
            this.xtraTabControl1.ShowHeaderFocus = DevExpress.Utils.DefaultBoolean.False;
            this.xtraTabControl1.Size = new System.Drawing.Size(846, 618);
            this.xtraTabControl1.TabIndex = 9;
            this.xtraTabControl1.TabPages.AddRange(new DevExpress.XtraTab.XtraTabPage[] {
            this.xtraTabPage4,
            this.xtraTabPage3,
            this.xtraTabPage2,
            this.xtraTabPage1});
            this.xtraTabControl1.StyleChanged += new System.EventHandler(this.xtraTabControl1_StyleChanged);
            // 
            // xtraTabPage1
            // 
            this.xtraTabPage1.Controls.Add(this.tableLayoutPanel1);
            this.xtraTabPage1.Controls.Add(this.pictureEdit1);
            this.xtraTabPage1.Controls.Add(this.labelControl2);
            this.xtraTabPage1.Controls.Add(this.simpleButton1);
            this.xtraTabPage1.Controls.Add(this.stepProgress1);
            this.xtraTabPage1.Controls.Add(this.stepProgress4);
            this.xtraTabPage1.Controls.Add(this.stepProgress2);
            this.xtraTabPage1.Controls.Add(this.stepProgress3);
            this.xtraTabPage1.Name = "xtraTabPage1";
            this.xtraTabPage1.Size = new System.Drawing.Size(844, 589);
            this.xtraTabPage1.Text = "Client Reset";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.labelControl1, 0, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(322, 294);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(200, 62);
            this.tableLayoutPanel1.TabIndex = 9;
            // 
            // xtraTabPage4
            // 
            this.xtraTabPage4.Name = "xtraTabPage4";
            this.xtraTabPage4.Size = new System.Drawing.Size(844, 589);
            this.xtraTabPage4.Text = "Store Page";
            // 
            // xtraTabPage3
            // 
            this.xtraTabPage3.Name = "xtraTabPage3";
            this.xtraTabPage3.Size = new System.Drawing.Size(844, 589);
            this.xtraTabPage3.Tag = "addremove";
            this.xtraTabPage3.Text = "Add / Remove";
            // 
            // xtraTabPage2
            // 
            this.xtraTabPage2.Controls.Add(this.labelControl3);
            this.xtraTabPage2.Name = "xtraTabPage2";
            this.xtraTabPage2.Size = new System.Drawing.Size(844, 589);
            this.xtraTabPage2.Text = "Client Settings";
            // 
            // labelControl3
            // 
            this.labelControl3.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.labelControl3.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labelControl3.Appearance.Options.UseFont = true;
            this.labelControl3.Location = new System.Drawing.Point(347, 281);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(150, 24);
            this.labelControl3.TabIndex = 0;
            this.labelControl3.Text = "Work in progress";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // LauncherCard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.xtraTabControl1);
            this.DoubleBuffered = true;
            this.Name = "LauncherCard";
            this.Size = new System.Drawing.Size(846, 618);
            this.Load += new System.EventHandler(this.LauncherCard_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xtraTabControl1)).EndInit();
            this.xtraTabControl1.ResumeLayout(false);
            this.xtraTabPage1.ResumeLayout(false);
            this.xtraTabPage1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.xtraTabPage2.ResumeLayout(false);
            this.xtraTabPage2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.PictureEdit pictureEdit1;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private HorizonUI.StepProgress stepProgress1;
        private HorizonUI.StepProgress stepProgress2;
        private HorizonUI.StepProgress stepProgress3;
        private HorizonUI.StepProgress stepProgress4;
        private DevExpress.XtraEditors.SimpleButton simpleButton1;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraTab.XtraTabPage xtraTabPage1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private DevExpress.XtraTab.XtraTabPage xtraTabPage3;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        public DevExpress.XtraTab.XtraTabPage xtraTabPage4;
        public DevExpress.XtraTab.XtraTabControl xtraTabControl1;
        public DevExpress.XtraTab.XtraTabPage xtraTabPage2;
    }
}
