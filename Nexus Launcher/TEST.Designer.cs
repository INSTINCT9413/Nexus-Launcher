namespace Nexus_Launcher
{
    partial class TEST
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TEST));
            this.formAssistant1 = new DevExpress.XtraBars.FormAssistant();
            this.htmlTemplateCollection1 = new DevExpress.Utils.Html.HtmlTemplateCollection();
            this.htmlTemplate1 = new DevExpress.Utils.Html.HtmlTemplate();
            this.directXFormContainerControl1 = new DevExpress.XtraEditors.DirectXFormContainerControl();
            this.SuspendLayout();
            // 
            // htmlTemplateCollection1
            // 
            this.htmlTemplateCollection1.AddRange(new DevExpress.Utils.Html.HtmlTemplate[] {
            this.htmlTemplate1});
            // 
            // htmlTemplate1
            // 
            this.htmlTemplate1.Name = "htmlTemplate1";
            this.htmlTemplate1.Styles = resources.GetString("htmlTemplate1.Styles");
            this.htmlTemplate1.Template = resources.GetString("htmlTemplate1.Template");
            // 
            // directXFormContainerControl1
            // 
            this.directXFormContainerControl1.Location = new System.Drawing.Point(31, 93);
            this.directXFormContainerControl1.Name = "directXFormContainerControl1";
            this.directXFormContainerControl1.Size = new System.Drawing.Size(631, 348);
            this.directXFormContainerControl1.TabIndex = 0;
            // 
            // TEST
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ChildControls.Add(this.directXFormContainerControl1);
            this.ClientSize = new System.Drawing.Size(693, 524);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.DoubleBuffered = true;
            this.HtmlTemplate.Styles = resources.GetString("TEST.HtmlTemplate.Styles");
            this.HtmlTemplate.Template = resources.GetString("TEST.HtmlTemplate.Template");
            this.HtmlText = "TEST";
            this.Name = "TEST";
            this.Load += new System.EventHandler(this.TEST_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraBars.FormAssistant formAssistant1;
        private DevExpress.Utils.Html.HtmlTemplateCollection htmlTemplateCollection1;
        private DevExpress.Utils.Html.HtmlTemplate htmlTemplate1;
        private DevExpress.XtraEditors.DirectXFormContainerControl directXFormContainerControl1;
    }
}