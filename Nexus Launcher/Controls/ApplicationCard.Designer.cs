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
            this.pictureEdit2 = new DevExpress.XtraEditors.PictureEdit();
            this.dropDownButton1 = new DevExpress.XtraEditors.DropDownButton();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.webView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.panelControl2 = new DevExpress.XtraEditors.PanelControl();
            this.panelControl3 = new DevExpress.XtraEditors.PanelControl();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this.dropDownButton6 = new DevExpress.XtraEditors.DropDownButton();
            this.zoomTrackBarControl1 = new DevExpress.XtraEditors.ZoomTrackBarControl();
            this.dropDownButton4 = new DevExpress.XtraEditors.DropDownButton();
            this.dropDownButton5 = new DevExpress.XtraEditors.DropDownButton();
            this.webView22 = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.splitContainerControl1 = new DevExpress.XtraEditors.SplitContainerControl();
            this.dropDownButton7 = new DevExpress.XtraEditors.DropDownButton();
            this.labelControl4 = new DevExpress.XtraEditors.LabelControl();
            this.ratingControl1 = new DevExpress.XtraEditors.RatingControl();
            this.dropDownButton3 = new DevExpress.XtraEditors.DropDownButton();
            this.pictureEdit1 = new DevExpress.XtraEditors.PictureEdit();
            this.dropDownButton2 = new DevExpress.XtraEditors.DropDownButton();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit2.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.webView21)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl2)).BeginInit();
            this.panelControl2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl3)).BeginInit();
            this.panelControl3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.zoomTrackBarControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.zoomTrackBarControl1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.webView22)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel1)).BeginInit();
            this.splitContainerControl1.Panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel2)).BeginInit();
            this.splitContainerControl1.Panel2.SuspendLayout();
            this.splitContainerControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ratingControl1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit1.Properties)).BeginInit();
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
            // dropDownButton1
            // 
            this.dropDownButton1.DropDownArrowStyle = DevExpress.XtraEditors.DropDownArrowStyle.Hide;
            this.dropDownButton1.Location = new System.Drawing.Point(173, 342);
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
            this.labelControl2.Location = new System.Drawing.Point(173, 317);
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
            this.webView21.ZoomFactorChanged += new System.EventHandler<System.EventArgs>(this.webView21_ZoomFactorChanged);
            // 
            // panelControl2
            // 
            this.panelControl2.Controls.Add(this.panelControl3);
            this.panelControl2.Controls.Add(this.webView21);
            this.panelControl2.Controls.Add(this.webView22);
            this.panelControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelControl2.Location = new System.Drawing.Point(0, 0);
            this.panelControl2.Name = "panelControl2";
            this.panelControl2.Size = new System.Drawing.Size(1095, 441);
            this.panelControl2.TabIndex = 6;
            // 
            // panelControl3
            // 
            this.panelControl3.Controls.Add(this.labelControl3);
            this.panelControl3.Controls.Add(this.dropDownButton6);
            this.panelControl3.Controls.Add(this.zoomTrackBarControl1);
            this.panelControl3.Controls.Add(this.dropDownButton4);
            this.panelControl3.Controls.Add(this.dropDownButton5);
            this.panelControl3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelControl3.Location = new System.Drawing.Point(2, 2);
            this.panelControl3.Name = "panelControl3";
            this.panelControl3.Size = new System.Drawing.Size(1091, 35);
            this.panelControl3.TabIndex = 9;
            this.panelControl3.Visible = false;
            // 
            // labelControl3
            // 
            this.labelControl3.Location = new System.Drawing.Point(344, 11);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(26, 13);
            this.labelControl3.TabIndex = 13;
            this.labelControl3.Text = "200%";
            // 
            // dropDownButton6
            // 
            this.dropDownButton6.DropDownArrowStyle = DevExpress.XtraEditors.DropDownArrowStyle.Hide;
            this.dropDownButton6.ImageOptions.Image = global::Nexus_Launcher.Properties.Resources.zoom100_16x16;
            this.dropDownButton6.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
            this.dropDownButton6.Location = new System.Drawing.Point(377, 6);
            this.dropDownButton6.Name = "dropDownButton6";
            this.dropDownButton6.ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False;
            this.dropDownButton6.Size = new System.Drawing.Size(24, 24);
            this.dropDownButton6.TabIndex = 12;
            this.dropDownButton6.ToolTip = "Shrink the store page";
            this.dropDownButton6.ToolTipIconType = DevExpress.Utils.ToolTipIconType.Information;
            this.dropDownButton6.ToolTipTitle = "Shrink";
            this.dropDownButton6.Click += new System.EventHandler(this.dropDownButton6_Click);
            // 
            // zoomTrackBarControl1
            // 
            this.zoomTrackBarControl1.EditValue = 100;
            this.zoomTrackBarControl1.Location = new System.Drawing.Point(65, 11);
            this.zoomTrackBarControl1.Name = "zoomTrackBarControl1";
            this.zoomTrackBarControl1.Properties.Maximum = 200;
            this.zoomTrackBarControl1.Properties.Minimum = 75;
            this.zoomTrackBarControl1.Size = new System.Drawing.Size(273, 13);
            this.zoomTrackBarControl1.TabIndex = 11;
            this.zoomTrackBarControl1.ToolTip = "Change zoom levels of the store";
            this.zoomTrackBarControl1.ToolTipIconType = DevExpress.Utils.ToolTipIconType.Information;
            this.zoomTrackBarControl1.ToolTipTitle = "Zoom Factor";
            this.zoomTrackBarControl1.Value = 100;
            this.zoomTrackBarControl1.ValueChanged += new System.EventHandler(this.zoomTrackBarControl1_ValueChanged);
            this.zoomTrackBarControl1.EditValueChanged += new System.EventHandler(this.zoomTrackBarControl1_EditValueChanged);
            // 
            // dropDownButton4
            // 
            this.dropDownButton4.DropDownArrowStyle = DevExpress.XtraEditors.DropDownArrowStyle.Hide;
            this.dropDownButton4.ImageOptions.Image = global::Nexus_Launcher.Properties.Resources.fill_16x16;
            this.dropDownButton4.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
            this.dropDownButton4.Location = new System.Drawing.Point(35, 5);
            this.dropDownButton4.Name = "dropDownButton4";
            this.dropDownButton4.ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False;
            this.dropDownButton4.Size = new System.Drawing.Size(24, 24);
            this.dropDownButton4.TabIndex = 10;
            this.dropDownButton4.ToolTip = "Shrink the store page";
            this.dropDownButton4.ToolTipIconType = DevExpress.Utils.ToolTipIconType.Information;
            this.dropDownButton4.ToolTipTitle = "Shrink";
            this.dropDownButton4.Click += new System.EventHandler(this.dropDownButton4_Click_1);
            // 
            // dropDownButton5
            // 
            this.dropDownButton5.DropDownArrowStyle = DevExpress.XtraEditors.DropDownArrowStyle.Hide;
            this.dropDownButton5.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("dropDownButton5.ImageOptions.Image")));
            this.dropDownButton5.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
            this.dropDownButton5.Location = new System.Drawing.Point(5, 5);
            this.dropDownButton5.Name = "dropDownButton5";
            this.dropDownButton5.ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False;
            this.dropDownButton5.Size = new System.Drawing.Size(24, 24);
            this.dropDownButton5.TabIndex = 9;
            this.dropDownButton5.ToolTip = "Reload the store page";
            this.dropDownButton5.ToolTipIconType = DevExpress.Utils.ToolTipIconType.Information;
            this.dropDownButton5.ToolTipTitle = "Reload";
            this.dropDownButton5.Click += new System.EventHandler(this.dropDownButton5_Click);
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
            this.splitContainerControl1.Panel1.Controls.Add(this.dropDownButton7);
            this.splitContainerControl1.Panel1.Controls.Add(this.labelControl4);
            this.splitContainerControl1.Panel1.Controls.Add(this.ratingControl1);
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
            // dropDownButton7
            // 
            this.dropDownButton7.DropDownArrowStyle = DevExpress.XtraEditors.DropDownArrowStyle.SplitButton;
            this.dropDownButton7.Location = new System.Drawing.Point(271, 392);
            this.dropDownButton7.Name = "dropDownButton7";
            this.dropDownButton7.Size = new System.Drawing.Size(102, 24);
            this.dropDownButton7.TabIndex = 13;
            this.dropDownButton7.Text = "dropDownButton7";
            this.dropDownButton7.Click += new System.EventHandler(this.dropDownButton7_Click);
            // 
            // labelControl4
            // 
            this.labelControl4.Appearance.Font = new System.Drawing.Font("Tahoma", 10F);
            this.labelControl4.Appearance.Options.UseFont = true;
            this.labelControl4.Location = new System.Drawing.Point(173, 269);
            this.labelControl4.Name = "labelControl4";
            this.labelControl4.Size = new System.Drawing.Size(43, 16);
            this.labelControl4.TabIndex = 10;
            this.labelControl4.Text = "DEBUG:";
            // 
            // ratingControl1
            // 
            this.ratingControl1.Location = new System.Drawing.Point(379, 342);
            this.ratingControl1.Name = "ratingControl1";
            this.ratingControl1.Properties.Appearance.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.ratingControl1.Properties.Appearance.Options.UseFont = true;
            this.ratingControl1.Properties.AutoHeight = false;
            this.ratingControl1.Properties.AutoSize = false;
            this.ratingControl1.Properties.ItemCount = 1;
            this.ratingControl1.Size = new System.Drawing.Size(24, 44);
            this.ratingControl1.TabIndex = 9;
            this.ratingControl1.Text = "ratingControl1";
            this.ratingControl1.ItemClick += new DevExpress.XtraEditors.Repository.ItemEventHandler(this.ratingControl1_ItemClick);
            this.ratingControl1.EditValueChanged += new System.EventHandler(this.ratingControl1_EditValueChanged);
            // 
            // dropDownButton3
            // 
            this.dropDownButton3.DropDownArrowStyle = DevExpress.XtraEditors.DropDownArrowStyle.Hide;
            this.dropDownButton3.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("dropDownButton3.ImageOptions.Image")));
            this.dropDownButton3.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
            this.dropDownButton3.Location = new System.Drawing.Point(203, 392);
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
            this.dropDownButton2.Location = new System.Drawing.Point(173, 392);
            this.dropDownButton2.Name = "dropDownButton2";
            this.dropDownButton2.ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False;
            this.dropDownButton2.Size = new System.Drawing.Size(24, 24);
            this.dropDownButton2.TabIndex = 7;
            this.dropDownButton2.ToolTip = "Reload the store page";
            this.dropDownButton2.ToolTipIconType = DevExpress.Utils.ToolTipIconType.Information;
            this.dropDownButton2.ToolTipTitle = "Reload";
            this.dropDownButton2.Click += new System.EventHandler(this.dropDownButton2_Click);
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
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit2.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.webView21)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl2)).EndInit();
            this.panelControl2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.panelControl3)).EndInit();
            this.panelControl3.ResumeLayout(false);
            this.panelControl3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.zoomTrackBarControl1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.zoomTrackBarControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.webView22)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel1)).EndInit();
            this.splitContainerControl1.Panel1.ResumeLayout(false);
            this.splitContainerControl1.Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel2)).EndInit();
            this.splitContainerControl1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1)).EndInit();
            this.splitContainerControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ratingControl1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit1.Properties)).EndInit();
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
        private DevExpress.XtraEditors.PanelControl panelControl3;
        private DevExpress.XtraEditors.DropDownButton dropDownButton5;
        public DevExpress.XtraEditors.DropDownButton dropDownButton4;
        private DevExpress.XtraEditors.ZoomTrackBarControl zoomTrackBarControl1;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        public DevExpress.XtraEditors.DropDownButton dropDownButton6;
        private DevExpress.XtraEditors.RatingControl ratingControl1;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.DropDownButton dropDownButton7;
    }
}
