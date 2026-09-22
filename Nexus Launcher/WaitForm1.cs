using DevExpress.LookAndFeel;
using DevExpress.XtraWaitForm;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Properties;
using System;
using System.Windows.Forms;
using static SplashHelper;

namespace Nexus_Launcher
{
    /// <summary>
    /// The startup splash: the animated Nexus loader on the left, the
    /// current step on the right, and an artwork download bar that only
    /// appears once downloads start.
    ///
    /// Everything reaches it through SplashHelper and the
    /// SplashScreenManager, which runs this form on its own UI thread:
    /// SetCaption is the headline, SetDescription the current step, and
    /// the UpdateArtworkProgress command drives the bar.
    /// </summary>
    public partial class WaitForm1 : WaitForm
    {
        public WaitForm1()
        {
            InitializeComponent();
            this.Parent = Program.MainFormInstance;
        }

        #region Overrides

        public override void SetCaption(string caption)
        {
            base.SetCaption(caption);
            labelCaption.Text = caption ?? string.Empty;
        }

        public override void SetDescription(string description)
        {
            base.SetDescription(description);
            labelDescription.Text = description ?? string.Empty;
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

                    ShowArtworkProgress(
                        progress.Completed,
                        progress.Total);

                    break;
            }
        }

        #endregion

        public enum WaitFormCommand
        {
            UpdateArtworkProgress
        }

        /// <summary>
        /// Shows the download bar on its own line under the current step.
        /// It used to overwrite the step text, so the user lost sight of
        /// what the launcher was doing while artwork downloaded.
        /// </summary>
        private void ShowArtworkProgress(
            int completed,
            int total)
        {
            if (total <= 0)
                total = 1;

            completed =
                Math.Max(0, Math.Min(completed, total));

            progressBarControl1.Properties.Minimum = 0;
            progressBarControl1.Properties.Maximum = total;
            progressBarControl1.Position = completed;

            labelProgress.Text =
                completed >= total
                    ? "Artwork ready"
                    : "Downloading artwork   " + completed + " of " + total;

            // Revealing both at once keeps the text block centred; the
            // table collapses them while they are hidden.
            progressBarControl1.Visible = true;
            labelProgress.Visible = true;
        }

        /// <summary>
        /// Sets every text colour explicitly from the active theme.
        ///
        /// The labels are created before Load applies the saved theme,
        /// and left to themselves they kept the text colour of the skin
        /// that was active then: on a dark theme the headline came out
        /// black on a dark background. The step and progress lines use
        /// the muted colour so they read as secondary to the headline.
        /// </summary>
        private void ApplyThemeColors()
        {
            labelCaption.Appearance.ForeColor = ProfileStyle.TextColor;
            labelCaption.Appearance.Options.UseForeColor = true;

            labelDescription.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            labelDescription.Appearance.Options.UseForeColor = true;

            labelProgress.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            labelProgress.Appearance.Options.UseForeColor = true;
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

            // Colours are read after the saved theme is applied, or the
            // muted text would be worked out from the wrong skin.
            ApplyThemeColors();

            pictureEditLoader.StartAnimation();
        }

        private void WaitForm1_FormClosing(object sender, FormClosingEventArgs e)
        {
            pictureEditLoader.StopAnimation();
        }
    }
}
