namespace Nexus_Launcher
{
    partial class WaitForm1
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
            this.tableLayoutMain = new System.Windows.Forms.TableLayoutPanel();
            this.pictureEditLoader = new DevExpress.XtraEditors.PictureEdit();
            this.tableLayoutText = new System.Windows.Forms.TableLayoutPanel();
            this.labelCaption = new DevExpress.XtraEditors.LabelControl();
            this.labelDescription = new DevExpress.XtraEditors.LabelControl();
            this.progressBarControl1 = new DevExpress.XtraEditors.ProgressBarControl();
            this.labelProgress = new DevExpress.XtraEditors.LabelControl();
            this.tableLayoutMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEditLoader.Properties)).BeginInit();
            this.tableLayoutText.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.progressBarControl1.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutMain
            // 
            this.tableLayoutMain.BackColor = System.Drawing.Color.Transparent;
            this.tableLayoutMain.ColumnCount = 2;
            this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 124F));
            this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutMain.Controls.Add(this.pictureEditLoader, 0, 0);
            this.tableLayoutMain.Controls.Add(this.tableLayoutText, 1, 0);
            this.tableLayoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutMain.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutMain.Name = "tableLayoutMain";
            this.tableLayoutMain.Padding = new System.Windows.Forms.Padding(20, 16, 22, 16);
            this.tableLayoutMain.RowCount = 1;
            this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutMain.Size = new System.Drawing.Size(460, 150);
            this.tableLayoutMain.TabIndex = 0;
            // 
            // pictureEditLoader
            // 
            this.pictureEditLoader.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.pictureEditLoader.EditValue = global::Nexus_Launcher.Properties.Resources.nexus_loader_128;
            this.pictureEditLoader.Location = new System.Drawing.Point(34, 27);
            this.pictureEditLoader.Margin = new System.Windows.Forms.Padding(0);
            this.pictureEditLoader.Name = "pictureEditLoader";
            this.pictureEditLoader.Properties.AllowFocused = false;
            this.pictureEditLoader.Properties.AnimatedImageLoopMode = DevExpress.Utils.AnimatedImageLoopMode.Infinite;
            this.pictureEditLoader.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.pictureEditLoader.Properties.Appearance.Options.UseBackColor = true;
            this.pictureEditLoader.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.pictureEditLoader.Properties.ReadOnly = true;
            this.pictureEditLoader.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.pictureEditLoader.Properties.ShowMenu = false;
            this.pictureEditLoader.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom;
            this.pictureEditLoader.Size = new System.Drawing.Size(96, 96);
            this.pictureEditLoader.TabIndex = 0;
            // 
            // tableLayoutText
            // 
            this.tableLayoutText.BackColor = System.Drawing.Color.Transparent;
            this.tableLayoutText.ColumnCount = 1;
            this.tableLayoutText.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutText.Controls.Add(this.labelCaption, 0, 1);
            this.tableLayoutText.Controls.Add(this.labelDescription, 0, 2);
            this.tableLayoutText.Controls.Add(this.progressBarControl1, 0, 3);
            this.tableLayoutText.Controls.Add(this.labelProgress, 0, 4);
            this.tableLayoutText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutText.Location = new System.Drawing.Point(144, 16);
            this.tableLayoutText.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutText.Name = "tableLayoutText";
            this.tableLayoutText.RowCount = 6;
            this.tableLayoutText.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutText.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutText.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutText.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutText.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutText.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutText.Size = new System.Drawing.Size(294, 118);
            this.tableLayoutText.TabIndex = 1;
            // 
            // labelCaption
            // 
            this.labelCaption.Appearance.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.labelCaption.Appearance.Options.UseFont = true;
            this.labelCaption.Appearance.Options.UseTextOptions = true;
            this.labelCaption.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.labelCaption.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
            this.labelCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelCaption.Location = new System.Drawing.Point(0, 18);
            this.labelCaption.Margin = new System.Windows.Forms.Padding(0);
            this.labelCaption.Name = "labelCaption";
            this.labelCaption.Size = new System.Drawing.Size(294, 23);
            this.labelCaption.TabIndex = 0;
            this.labelCaption.Text = "Starting Nexus Launcher";
            // 
            // labelDescription
            // 
            this.labelDescription.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.labelDescription.Appearance.Options.UseFont = true;
            this.labelDescription.Appearance.Options.UseTextOptions = true;
            this.labelDescription.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.labelDescription.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
            this.labelDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelDescription.Location = new System.Drawing.Point(0, 45);
            this.labelDescription.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.labelDescription.Name = "labelDescription";
            this.labelDescription.Size = new System.Drawing.Size(294, 17);
            this.labelDescription.TabIndex = 1;
            this.labelDescription.Text = "Initializing...";
            // 
            // progressBarControl1
            // 
            this.progressBarControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBarControl1.Location = new System.Drawing.Point(0, 74);
            this.progressBarControl1.Margin = new System.Windows.Forms.Padding(0, 12, 0, 0);
            this.progressBarControl1.Name = "progressBarControl1";
            this.progressBarControl1.Size = new System.Drawing.Size(294, 8);
            this.progressBarControl1.TabIndex = 2;
            this.progressBarControl1.Visible = false;
            // 
            // labelProgress
            // 
            this.labelProgress.Appearance.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelProgress.Appearance.Options.UseFont = true;
            this.labelProgress.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
            this.labelProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelProgress.Location = new System.Drawing.Point(0, 86);
            this.labelProgress.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.labelProgress.Name = "labelProgress";
            this.labelProgress.Size = new System.Drawing.Size(294, 13);
            this.labelProgress.TabIndex = 3;
            this.labelProgress.Text = "Downloading artwork";
            this.labelProgress.Visible = false;
            // 
            // WaitForm1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(460, 150);
            this.Controls.Add(this.tableLayoutMain);
            this.DoubleBuffered = true;
            this.MinimumSize = new System.Drawing.Size(460, 150);
            this.Name = "WaitForm1";
            this.ShowOnTopMode = DevExpress.XtraWaitForm.ShowFormOnTopMode.AboveParent;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Nexus Launcher";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.WaitForm1_FormClosing);
            this.Load += new System.EventHandler(this.WaitForm1_Load);
            this.tableLayoutMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureEditLoader.Properties)).EndInit();
            this.tableLayoutText.ResumeLayout(false);
            this.tableLayoutText.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.progressBarControl1.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
        private DevExpress.XtraEditors.PictureEdit pictureEditLoader;
        private System.Windows.Forms.TableLayoutPanel tableLayoutText;
        private DevExpress.XtraEditors.LabelControl labelCaption;
        private DevExpress.XtraEditors.LabelControl labelDescription;
        public DevExpress.XtraEditors.ProgressBarControl progressBarControl1;
        private DevExpress.XtraEditors.LabelControl labelProgress;
    }
}
