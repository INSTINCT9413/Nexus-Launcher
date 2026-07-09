namespace Nexus_Launcher.Forms
{
    partial class addRemoveForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            DevExpress.XtraBars.Ribbon.GalleryItemGroup galleryItemGroup1 = new DevExpress.XtraBars.Ribbon.GalleryItemGroup();
            this.splitContainerControl1 = new DevExpress.XtraEditors.SplitContainerControl();
            this.galleryControl1 = new DevExpress.XtraBars.Ribbon.GalleryControl();
            this.galleryControlClient1 = new DevExpress.XtraBars.Ribbon.GalleryControlClient();
            this.labelControl5 = new DevExpress.XtraEditors.LabelControl();
            this.toggleSwitch2 = new DevExpress.XtraEditors.ToggleSwitch();
            this.labelControl4 = new DevExpress.XtraEditors.LabelControl();
            this.toggleSwitch1 = new DevExpress.XtraEditors.ToggleSwitch();
            this.pictureEdit2 = new DevExpress.XtraEditors.PictureEdit();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this.pictureEdit1 = new DevExpress.XtraEditors.PictureEdit();
            this.simpleButton3 = new DevExpress.XtraEditors.SimpleButton();
            this.textEdit2 = new DevExpress.XtraEditors.TextEdit();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.simpleButton2 = new DevExpress.XtraEditors.SimpleButton();
            this.simpleButton1 = new DevExpress.XtraEditors.SimpleButton();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.textEdit1 = new DevExpress.XtraEditors.TextEdit();
            this.separatorControl1 = new DevExpress.XtraEditors.SeparatorControl();
            this.galleryControlClient2 = new DevExpress.XtraBars.Ribbon.GalleryControlClient();
            this.xtraOpenFileDialog1 = new DevExpress.XtraEditors.XtraOpenFileDialog(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel1)).BeginInit();
            this.splitContainerControl1.Panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel2)).BeginInit();
            this.splitContainerControl1.Panel2.SuspendLayout();
            this.splitContainerControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.galleryControl1)).BeginInit();
            this.galleryControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.toggleSwitch2.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.toggleSwitch1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit2.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEdit2.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEdit1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.separatorControl1)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainerControl1
            // 
            this.splitContainerControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerControl1.Location = new System.Drawing.Point(0, 0);
            this.splitContainerControl1.Name = "splitContainerControl1";
            // 
            // splitContainerControl1.Panel1
            // 
            this.splitContainerControl1.Panel1.Controls.Add(this.galleryControl1);
            this.splitContainerControl1.Panel1.Text = "Panel1";
            // 
            // splitContainerControl1.Panel2
            // 
            this.splitContainerControl1.Panel2.Controls.Add(this.labelControl5);
            this.splitContainerControl1.Panel2.Controls.Add(this.toggleSwitch2);
            this.splitContainerControl1.Panel2.Controls.Add(this.labelControl4);
            this.splitContainerControl1.Panel2.Controls.Add(this.toggleSwitch1);
            this.splitContainerControl1.Panel2.Controls.Add(this.pictureEdit2);
            this.splitContainerControl1.Panel2.Controls.Add(this.labelControl3);
            this.splitContainerControl1.Panel2.Controls.Add(this.pictureEdit1);
            this.splitContainerControl1.Panel2.Controls.Add(this.simpleButton3);
            this.splitContainerControl1.Panel2.Controls.Add(this.textEdit2);
            this.splitContainerControl1.Panel2.Controls.Add(this.labelControl2);
            this.splitContainerControl1.Panel2.Controls.Add(this.simpleButton2);
            this.splitContainerControl1.Panel2.Controls.Add(this.simpleButton1);
            this.splitContainerControl1.Panel2.Controls.Add(this.labelControl1);
            this.splitContainerControl1.Panel2.Controls.Add(this.textEdit1);
            this.splitContainerControl1.Panel2.Controls.Add(this.separatorControl1);
            this.splitContainerControl1.Panel2.Text = "Panel2";
            this.splitContainerControl1.Size = new System.Drawing.Size(819, 500);
            this.splitContainerControl1.SplitterPosition = 370;
            this.splitContainerControl1.TabIndex = 0;
            // 
            // galleryControl1
            // 
            this.galleryControl1.Controls.Add(this.galleryControlClient1);
            this.galleryControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            // 
            // 
            // 
            galleryItemGroup1.Caption = "Nexus Launcher Executables";
            this.galleryControl1.Gallery.Groups.AddRange(new DevExpress.XtraBars.Ribbon.GalleryItemGroup[] {
            galleryItemGroup1});
            this.galleryControl1.Gallery.ImageSize = new System.Drawing.Size(32, 32);
            this.galleryControl1.Gallery.ShowItemText = true;
            this.galleryControl1.Gallery.ItemClick += new DevExpress.XtraBars.Ribbon.GalleryItemClickEventHandler(this.galleryControl1_Gallery_ItemClick);
            this.galleryControl1.Location = new System.Drawing.Point(0, 0);
            this.galleryControl1.Name = "galleryControl1";
            this.galleryControl1.Size = new System.Drawing.Size(370, 500);
            this.galleryControl1.TabIndex = 0;
            this.galleryControl1.Text = "galleryControl1";
            this.galleryControl1.Click += new System.EventHandler(this.galleryControl1_Click);
            // 
            // galleryControlClient1
            // 
            this.galleryControlClient1.GalleryControl = this.galleryControl1;
            this.galleryControlClient1.Location = new System.Drawing.Point(2, 2);
            this.galleryControlClient1.Size = new System.Drawing.Size(349, 496);
            // 
            // labelControl5
            // 
            this.labelControl5.Location = new System.Drawing.Point(66, 348);
            this.labelControl5.Name = "labelControl5";
            this.labelControl5.Size = new System.Drawing.Size(71, 13);
            this.labelControl5.TabIndex = 14;
            this.labelControl5.Text = "Batch Remove";
            // 
            // toggleSwitch2
            // 
            this.toggleSwitch2.Location = new System.Drawing.Point(141, 343);
            this.toggleSwitch2.Name = "toggleSwitch2";
            this.toggleSwitch2.Properties.AllowFocused = false;
            this.toggleSwitch2.Properties.OffText = "Off";
            this.toggleSwitch2.Properties.OnText = "On";
            this.toggleSwitch2.Size = new System.Drawing.Size(95, 19);
            this.toggleSwitch2.TabIndex = 13;
            this.toggleSwitch2.Toggled += new System.EventHandler(this.toggleSwitch2_Toggled);
            // 
            // labelControl4
            // 
            this.labelControl4.Location = new System.Drawing.Point(66, 170);
            this.labelControl4.Name = "labelControl4";
            this.labelControl4.Size = new System.Drawing.Size(50, 13);
            this.labelControl4.TabIndex = 12;
            this.labelControl4.Text = "Batch Add";
            // 
            // toggleSwitch1
            // 
            this.toggleSwitch1.Location = new System.Drawing.Point(121, 165);
            this.toggleSwitch1.Name = "toggleSwitch1";
            this.toggleSwitch1.Properties.AllowFocused = false;
            this.toggleSwitch1.Properties.OffText = "Off";
            this.toggleSwitch1.Properties.OnText = "On";
            this.toggleSwitch1.Size = new System.Drawing.Size(95, 19);
            this.toggleSwitch1.TabIndex = 11;
            this.toggleSwitch1.Toggled += new System.EventHandler(this.toggleSwitch1_Toggled);
            // 
            // pictureEdit2
            // 
            this.pictureEdit2.Location = new System.Drawing.Point(29, 245);
            this.pictureEdit2.Name = "pictureEdit2";
            this.pictureEdit2.Properties.ReadOnly = true;
            this.pictureEdit2.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.pictureEdit2.Size = new System.Drawing.Size(60, 60);
            this.pictureEdit2.TabIndex = 10;
            // 
            // labelControl3
            // 
            this.labelControl3.Location = new System.Drawing.Point(29, 319);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(31, 13);
            this.labelControl3.TabIndex = 9;
            this.labelControl3.Text = "Name:";
            // 
            // pictureEdit1
            // 
            this.pictureEdit1.Location = new System.Drawing.Point(29, 17);
            this.pictureEdit1.Name = "pictureEdit1";
            this.pictureEdit1.Properties.ReadOnly = true;
            this.pictureEdit1.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.pictureEdit1.Size = new System.Drawing.Size(60, 60);
            this.pictureEdit1.TabIndex = 8;
            // 
            // simpleButton3
            // 
            this.simpleButton3.Location = new System.Drawing.Point(296, 128);
            this.simpleButton3.Name = "simpleButton3";
            this.simpleButton3.ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False;
            this.simpleButton3.Size = new System.Drawing.Size(30, 23);
            this.simpleButton3.TabIndex = 7;
            this.simpleButton3.Text = "...";
            this.simpleButton3.Click += new System.EventHandler(this.simpleButton3_Click);
            // 
            // textEdit2
            // 
            this.textEdit2.Location = new System.Drawing.Point(66, 126);
            this.textEdit2.Name = "textEdit2";
            this.textEdit2.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            this.textEdit2.Size = new System.Drawing.Size(224, 20);
            this.textEdit2.TabIndex = 6;
            this.textEdit2.EditValueChanged += new System.EventHandler(this.textEdit2_EditValueChanged);
            this.textEdit2.Click += new System.EventHandler(this.textEdit2_Click);
            // 
            // labelControl2
            // 
            this.labelControl2.Location = new System.Drawing.Point(29, 133);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(25, 13);
            this.labelControl2.TabIndex = 5;
            this.labelControl2.Text = "Path:";
            // 
            // simpleButton2
            // 
            this.simpleButton2.Location = new System.Drawing.Point(251, 343);
            this.simpleButton2.Name = "simpleButton2";
            this.simpleButton2.ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False;
            this.simpleButton2.Size = new System.Drawing.Size(75, 23);
            this.simpleButton2.TabIndex = 4;
            this.simpleButton2.Text = "REMOVE";
            this.simpleButton2.Click += new System.EventHandler(this.simpleButton2_Click);
            // 
            // simpleButton1
            // 
            this.simpleButton1.Location = new System.Drawing.Point(251, 165);
            this.simpleButton1.Name = "simpleButton1";
            this.simpleButton1.ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False;
            this.simpleButton1.Size = new System.Drawing.Size(75, 23);
            this.simpleButton1.TabIndex = 3;
            this.simpleButton1.Text = "ADD";
            this.simpleButton1.Click += new System.EventHandler(this.simpleButton1_Click);
            // 
            // labelControl1
            // 
            this.labelControl1.Location = new System.Drawing.Point(29, 99);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(31, 13);
            this.labelControl1.TabIndex = 2;
            this.labelControl1.Text = "Name:";
            // 
            // textEdit1
            // 
            this.textEdit1.Location = new System.Drawing.Point(66, 92);
            this.textEdit1.Name = "textEdit1";
            this.textEdit1.Size = new System.Drawing.Size(224, 20);
            this.textEdit1.TabIndex = 1;
            // 
            // separatorControl1
            // 
            this.separatorControl1.Location = new System.Drawing.Point(29, 201);
            this.separatorControl1.Name = "separatorControl1";
            this.separatorControl1.Size = new System.Drawing.Size(472, 23);
            this.separatorControl1.TabIndex = 0;
            // 
            // galleryControlClient2
            // 
            this.galleryControlClient2.GalleryControl = null;
            this.galleryControlClient2.Location = new System.Drawing.Point(0, 0);
            this.galleryControlClient2.Size = new System.Drawing.Size(0, 0);
            // 
            // xtraOpenFileDialog1
            // 
            this.xtraOpenFileDialog1.FileName = "xtraOpenFileDialog1";
            // 
            // addRemoveForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainerControl1);
            this.DoubleBuffered = true;
            this.Name = "addRemoveForm";
            this.Size = new System.Drawing.Size(819, 500);
            this.Load += new System.EventHandler(this.addRemoveForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel1)).EndInit();
            this.splitContainerControl1.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel2)).EndInit();
            this.splitContainerControl1.Panel2.ResumeLayout(false);
            this.splitContainerControl1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1)).EndInit();
            this.splitContainerControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.galleryControl1)).EndInit();
            this.galleryControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.toggleSwitch2.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.toggleSwitch1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit2.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEdit2.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEdit1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.separatorControl1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.SplitContainerControl splitContainerControl1;
        private DevExpress.XtraEditors.SeparatorControl separatorControl1;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.SimpleButton simpleButton2;
        private DevExpress.XtraEditors.SimpleButton simpleButton1;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.TextEdit textEdit1;
        private DevExpress.XtraEditors.TextEdit textEdit2;
        private DevExpress.XtraBars.Ribbon.GalleryControl galleryControl1;
        private DevExpress.XtraBars.Ribbon.GalleryControlClient galleryControlClient1;
        private DevExpress.XtraEditors.SimpleButton simpleButton3;
        private DevExpress.XtraEditors.PictureEdit pictureEdit1;
        private DevExpress.XtraEditors.PictureEdit pictureEdit2;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraBars.Ribbon.GalleryControlClient galleryControlClient2;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.ToggleSwitch toggleSwitch1;
        private DevExpress.XtraEditors.XtraOpenFileDialog xtraOpenFileDialog1;
        private DevExpress.XtraEditors.LabelControl labelControl5;
        private DevExpress.XtraEditors.ToggleSwitch toggleSwitch2;
    }
}