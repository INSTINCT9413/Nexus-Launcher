using DevExpress.LookAndFeel;
using DevExpress.XtraWaitForm;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using static SplashHelper;

namespace Nexus_Launcher
{
    public partial class WaitForm1 : WaitForm
    {
       
        public WaitForm1()
        {
            InitializeComponent();
            this.progressPanel1.AutoHeight = true;
            this.Parent = Program.MainFormInstance;
        }

        #region Overrides

        public override void SetCaption(string caption)
        {
            base.SetCaption(caption);
            this.progressPanel1.Caption = caption;
        }
        public override void SetDescription(string description)
        {
            base.SetDescription(description);
            this.progressPanel1.Description = description;
        }
        public override void ProcessCommand(
    Enum cmd,
    object arg)
        {
            base.ProcessCommand(cmd, arg);

            switch ((WaitFormCommand)cmd)
            {
                case WaitFormCommand.UpdateArtworkProgress:

                    ArtworkProgressInfo progress =
                        arg as ArtworkProgressInfo;

                    if (progress == null)
                        return;

                    progressBarControl1.Visible = true;

                    progressBarControl1.Properties.Minimum = 0;
                    progressBarControl1.Properties.Maximum = progress.Total;

                    progressBarControl1.Position = progress.Completed;

                    

                    progressPanel1.Description =
                        "      Download Artwork: " +progress.Completed + " / " + progress.Total;

                    break;
            }
        }

        #endregion

        public enum WaitFormCommand
        {
            UpdateArtworkProgress
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {
           
        }

        private void WaitForm1_Load(object sender, EventArgs e)
        {
            FontManager.ApplyFont(
    this,
    Settings.Default.UIFont);
            this.Parent = Program.MainFormInstance;
            this.ShowOnTopMode = ShowFormOnTopMode.AboveAll;
            ThemesSettings settings =
            ThemeSettingsManager.Load();
            
            if (!string.IsNullOrWhiteSpace(
                settings.SkinName))
            {
                UserLookAndFeel.Default.SetSkinStyle(
                    settings.SkinName);
            }

            if (!string.IsNullOrWhiteSpace(
                settings.PaletteName))
            {
                UserLookAndFeel.Default.SetSkinStyle(
                    settings.SkinName,
                    settings.PaletteName);
            }
        }

        private void WaitForm1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Program.MainFormInstance.Focus();
        }

        private void progressPanel1_Click(object sender, EventArgs e)
        {

        }
    }
}