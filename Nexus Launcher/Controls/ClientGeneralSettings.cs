using DevExpress.XtraEditors;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Properties;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls
{
    /// <summary>
    /// The settings every client shares, shown on the General Settings
    /// tab of whichever settings control that client uses.
    ///
    /// One class with several instances rather than the same checkbox
    /// copied onto three tabs: they all read and write the same
    /// setting, so they have to agree, and duplicating the wiring is
    /// how they stop agreeing.
    /// </summary>
    internal class ClientGeneralSettings : XtraUserControl
    {
        private readonly CheckEdit autoLaunch = new CheckEdit();

        private readonly LabelControl hint = new LabelControl();

        /// <summary>
        /// Raised when any instance changes the setting, so the others
        /// can follow without the user having to reopen the tab.
        /// </summary>
        private static event Action Changed;

        private bool syncing;

        public ClientGeneralSettings()
        {
            autoLaunch.Text =
                "Launch the client automatically after a reset";

            autoLaunch.CheckedChanged += AutoLaunch_CheckedChanged;

            Controls.Add(autoLaunch);

            hint.AutoSizeMode = LabelAutoSizeMode.None;

            hint.Appearance.TextOptions.WordWrap =
                DevExpress.Utils.WordWrap.Wrap;

            hint.Text =
                "When a reset finishes on the Client Reset tab, the " +
                "client is started again straight away instead of " +
                "waiting on the results screen.";

            Controls.Add(hint);

            Changed += Other_Changed;

            Disposed += (s, e) => Changed -= Other_Changed;

            Reload();

            ApplyTheme();
        }

        private void Other_Changed()
        {
            if (IsDisposed)
                return;

            Reload();
        }

        /// <summary>
        /// Takes the value from settings without reporting it back as
        /// a user edit.
        /// </summary>
        public void Reload()
        {
            syncing = true;

            try
            {
                autoLaunch.Checked =
                    Settings.Default.AutoLaunchAfterReset;
            }
            finally
            {
                syncing = false;
            }
        }

        private void AutoLaunch_CheckedChanged(
            object sender,
            EventArgs e)
        {
            if (syncing)
                return;

            Settings.Default.AutoLaunchAfterReset =
                autoLaunch.Checked;

            Settings.Default.Save();

            Action handler = Changed;

            if (handler != null)
                handler();
        }

        protected override void OnVisibleChanged(
            EventArgs e)
        {
            base.OnVisibleChanged(e);

            // The setting can be changed from another client's tab, so
            // it is re-read whenever this one comes back into view.
            if (Visible)
                Reload();
        }

        protected override void OnResize(
            EventArgs e)
        {
            base.OnResize(e);

            Relayout();
        }

        private void Relayout()
        {
            const int pad = 18;

            int width =
                Math.Max(200, ClientSize.Width - pad * 2);

            autoLaunch.SetBounds(pad, pad, width, 22);

            hint.SetBounds(pad + 20, pad + 26, width - 20, 40);
        }

        public void ApplyTheme()
        {
            hint.Appearance.ForeColor = ProfileStyle.MutedTextColor;

            hint.Appearance.Options.UseForeColor = true;

            hint.Appearance.Font = ProfileStyle.Font(8.5F);

            hint.Appearance.Options.UseFont = true;
        }
    }
}
