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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ApplicationCard));
            this.panelControl1 = new DevExpress.XtraEditors.PanelControl();
            this.dropDownButton1 = new DevExpress.XtraEditors.DropDownButton();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.webView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.panelControl2 = new DevExpress.XtraEditors.PanelControl();
            this.webView22 = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.splitContainerControl1 = new DevExpress.XtraEditors.SplitContainerControl();
            this.dropDownButton3 = new DevExpress.XtraEditors.DropDownButton();
            this.pictureEdit1 = new DevExpress.XtraEditors.PictureEdit();
            this.dropDownButton2 = new DevExpress.XtraEditors.DropDownButton();
            this.pictureEdit2 = new DevExpress.XtraEditors.PictureEdit();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.webView21)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl2)).BeginInit();
            this.panelControl2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.webView22)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel1)).BeginInit();
            this.splitContainerControl1.Panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel2)).BeginInit();
            this.splitContainerControl1.Panel2.SuspendLayout();
            this.splitContainerControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit2.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // panelControl1
            // 
            this.panelControl1.Controls.Add(this.pictureEdit2);
            this.panelControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelControl1.Location = new System.Drawing.Point(0, 0);
            this.panelControl1.Name = "panelControl1";
            this.panelControl1.Size = new System.Drawing.Size(1095, 202);
            this.panelControl1.TabIndex = 1;
            // 
            // dropDownButton1
            // 
            this.dropDownButton1.DropDownArrowStyle = DevExpress.XtraEditors.DropDownArrowStyle.Hide;
            this.dropDownButton1.Location = new System.Drawing.Point(173, 351);
            this.dropDownButton1.Name = "dropDownButton1";
            this.dropDownButton1.ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False;
            this.dropDownButton1.Size = new System.Drawing.Size(200, 44);
            this.dropDownButton1.TabIndex = 2;
            this.dropDownButton1.Text = "Play";
            this.dropDownButton1.Click += new System.EventHandler(this.dropDownButton1_Click);
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 25F);
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(173, 223);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(84, 40);
            this.labelControl1.TabIndex = 3;
            this.labelControl1.Text = "Name";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // labelControl2
            // 
            this.labelControl2.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Location = new System.Drawing.Point(173, 285);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(57, 19);
            this.labelControl2.TabIndex = 4;
            this.labelControl2.Text = "DEBUG:";
            // 
            // webView21
            // 
            this.webView21.AllowExternalDrop = true;
            this.webView21.CreationProperties = null;
            this.webView21.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webView21.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webView21.Location = new System.Drawing.Point(2, 2);
            this.webView21.Name = "webView21";
            this.webView21.Size = new System.Drawing.Size(1091, 437);
            this.webView21.TabIndex = 5;
            this.webView21.ZoomFactor = 0.8D;
            this.webView21.NavigationStarting += new System.EventHandler<Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs>(this.webView21_NavigationStarting);
            this.webView21.NavigationCompleted += new System.EventHandler<Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs>(this.webView21_NavigationCompleted);
            // 
            // panelControl2
            // 
            this.panelControl2.Controls.Add(this.webView21);
            this.panelControl2.Controls.Add(this.webView22);
            this.panelControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelControl2.Location = new System.Drawing.Point(0, 0);
            this.panelControl2.Name = "panelControl2";
            this.panelControl2.Size = new System.Drawing.Size(1095, 441);
            this.panelControl2.TabIndex = 6;
            // 
            // webView22
            // 
            this.webView22.AllowExternalDrop = true;
            this.webView22.CreationProperties = null;
            this.webView22.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webView22.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webView22.Location = new System.Drawing.Point(2, 2);
            this.webView22.Name = "webView22";
            this.webView22.Size = new System.Drawing.Size(1091, 437);
            this.webView22.Source = new System.Uri("https://guardbyte.me/downloads/Nexus%20Launcher/loading.html", System.UriKind.Absolute);
            this.webView22.TabIndex = 6;
            this.webView22.Visible = false;
            this.webView22.ZoomFactor = 1D;
            // 
            // splitContainerControl1
            // 
            this.splitContainerControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerControl1.Horizontal = false;
            this.splitContainerControl1.Location = new System.Drawing.Point(0, 0);
            this.splitContainerControl1.Name = "splitContainerControl1";
            // 
            // splitContainerControl1.Panel1
            // 
            this.splitContainerControl1.Panel1.Controls.Add(this.dropDownButton3);
            this.splitContainerControl1.Panel1.Controls.Add(this.pictureEdit1);
            this.splitContainerControl1.Panel1.Controls.Add(this.dropDownButton2);
            this.splitContainerControl1.Panel1.Controls.Add(this.dropDownButton1);
            this.splitContainerControl1.Panel1.Controls.Add(this.panelControl1);
            this.splitContainerControl1.Panel1.Controls.Add(this.labelControl1);
            this.splitContainerControl1.Panel1.Controls.Add(this.labelControl2);
            this.splitContainerControl1.Panel1.Text = "Panel1";
            // 
            // splitContainerControl1.Panel2
            // 
            this.splitContainerControl1.Panel2.Controls.Add(this.panelControl2);
            this.splitContainerControl1.Panel2.Text = "Panel2";
            this.splitContainerControl1.ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.False;
            this.splitContainerControl1.Size = new System.Drawing.Size(1095, 871);
            this.splitContainerControl1.SplitterPosition = 420;
            this.splitContainerControl1.TabIndex = 9;
            // 
            // dropDownButton3
            // 
            this.dropDownButton3.DropDownArrowStyle = DevExpress.XtraEditors.DropDownArrowStyle.Hide;
            this.dropDownButton3.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("dropDownButton3.ImageOptions.Image")));
            this.dropDownButton3.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
            this.dropDownButton3.Location = new System.Drawing.Point(409, 371);
            this.dropDownButton3.Name = "dropDownButton3";
            this.dropDownButton3.ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False;
            this.dropDownButton3.Size = new System.Drawing.Size(24, 24);
            this.dropDownButton3.TabIndex = 8;
            this.dropDownButton3.ToolTip = "Expand the store page";
            this.dropDownButton3.ToolTipIconType = DevExpress.Utils.ToolTipIconType.Information;
            this.dropDownButton3.ToolTipTitle = "Expand";
            this.dropDownButton3.Click += new System.EventHandler(this.dropDownButton3_Click);
            // 
            // pictureEdit1
            // 
            this.pictureEdit1.Location = new System.Drawing.Point(0, 206);
            this.pictureEdit1.Name = "pictureEdit1";
            this.pictureEdit1.Properties.AllowZoom = DevExpress.Utils.DefaultBoolean.True;
            this.pictureEdit1.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.pictureEdit1.Properties.ShowEditMenuItem = DevExpress.Utils.DefaultBoolean.False;
            this.pictureEdit1.Properties.ShowMenu = false;
            this.pictureEdit1.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Stretch;
            this.pictureEdit1.Size = new System.Drawing.Size(158, 210);
            this.pictureEdit1.TabIndex = 0;
            // 
            // dropDownButton2
            // 
            this.dropDownButton2.DropDownArrowStyle = DevExpress.XtraEditors.DropDownArrowStyle.Hide;
            this.dropDownButton2.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("dropDownButton2.ImageOptions.Image")));
            this.dropDownButton2.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
            this.dropDownButton2.Location = new System.Drawing.Point(379, 371);
            this.dropDownButton2.Name = "dropDownButton2";
            this.dropDownButton2.ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False;
            this.dropDownButton2.Size = new System.Drawing.Size(24, 24);
            this.dropDownButton2.TabIndex = 7;
            this.dropDownButton2.ToolTip = "Reload the store page";
            this.dropDownButton2.ToolTipIconType = DevExpress.Utils.ToolTipIconType.Information;
            this.dropDownButton2.ToolTipTitle = "Reload";
            this.dropDownButton2.Click += new System.EventHandler(this.dropDownButton2_Click);
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
            this.pictureEdit2.Size = new System.Drawing.Size(1091, 198);
            this.pictureEdit2.TabIndex = 0;
            // 
            // ApplicationCard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainerControl1);
            this.DoubleBuffered = true;
            this.Name = "ApplicationCard";
            this.Size = new System.Drawing.Size(1095, 871);
            this.Load += new System.EventHandler(this.ApplicationCard_Load);
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).EndInit();
            this.panelControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.webView21)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl2)).EndInit();
            this.panelControl2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.webView22)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel1)).EndInit();
            this.splitContainerControl1.Panel1.ResumeLayout(false);
            this.splitContainerControl1.Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel2)).EndInit();
            this.splitContainerControl1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1)).EndInit();
            this.splitContainerControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit2.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraEditors.DropDownButton dropDownButton1;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private System.Windows.Forms.Timer timer1;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        public DevExpress.XtraEditors.PictureEdit pictureEdit2;
        public DevExpress.XtraEditors.PictureEdit pictureEdit1;
        public Microsoft.Web.WebView2.WinForms.WebView2 webView21;
        private DevExpress.XtraEditors.PanelControl panelControl2;
        public Microsoft.Web.WebView2.WinForms.WebView2 webView22;
        private DevExpress.XtraEditors.DropDownButton dropDownButton2;
        private DevExpress.XtraEditors.DropDownButton dropDownButton3;
        public DevExpress.XtraEditors.SplitContainerControl splitContainerControl1;
    }
}
