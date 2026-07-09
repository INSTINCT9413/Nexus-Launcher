namespace Nexus_Launcher
{
    partial class AboutForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            this.inputDialog1 = new Ookii.Dialogs.WinForms.InputDialog(this.components);
            this.barRenderer1 = new BrightIdeasSoftware.BarRenderer();
            this.qlmService1 = new QlmLicenseLib.QlmAspService.QlmService();
            this.SuspendLayout();
            // 
            // inputDialog1
            // 
            this.inputDialog1.MainInstruction = "inputDialog1";
            // 
            // qlmService1
            // 
            this.qlmService1.Credentials = null;
            this.qlmService1.QlmSoapHeaderValue = null;
            this.qlmService1.Url = "http://localhost/QlmLicenseServer/qlmservice.asmx";
            this.qlmService1.UseDefaultCredentials = false;
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(503, 424);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.IconOptions.Image = ((System.Drawing.Image)(resources.GetObject("AboutForm.IconOptions.Image")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AboutForm";
            this.Load += new System.EventHandler(this.AboutForm_Load);
            this.ResumeLayout(false);

        }

        #endregion
        private Ookii.Dialogs.WinForms.InputDialog inputDialog1;
        private BrightIdeasSoftware.BarRenderer barRenderer1;
        private QlmLicenseLib.QlmAspService.QlmService qlmService1;
    }
}