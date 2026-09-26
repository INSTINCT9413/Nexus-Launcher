using DevExpress.XtraEditors;
using DevExpress.XtraTab;
using Nexus_Launcher.Controls.Profile;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls
{
    /// <summary>
    /// The Client Settings tab for launchers that have no settings
    /// control of their own.
    ///
    /// Carries the same two tabs EA and GOG show, so every client looks
    /// the same, with Add / Remove disabled: that tab manages extra
    /// library folders, and only EA and GOG support having them.
    ///
    /// Unrelated to the Add / Remove tab on the launcher card itself,
    /// which is where programs are added to Nexus.
    /// </summary>
    internal class ClientSettingsControl : XtraUserControl
    {
        private readonly XtraTabControl tabs = new XtraTabControl();

        private readonly XtraTabPage generalPage = new XtraTabPage();

        private readonly XtraTabPage addRemovePage = new XtraTabPage();

        private readonly ClientGeneralSettings general =
            new ClientGeneralSettings();

        private readonly LabelControl addRemoveNote = new LabelControl();

        private string clientName;

        /// <summary>
        /// Only used for the explanation on the disabled tab.
        /// </summary>
        public string ClientName
        {
            get
            {
                return clientName;
            }
            set
            {
                clientName = value;

                UpdateNote();
            }
        }

        public ClientSettingsControl()
        {
            SuspendLayout();

            try
            {
                generalPage.Text = "General Settings";

                general.Dock = DockStyle.Fill;

                generalPage.Controls.Add(general);

                addRemovePage.Text = "Add / Remove";

                // Visible but not usable, so the tab layout matches
                // every other client rather than the tab vanishing and
                // shifting things around.
                addRemovePage.PageEnabled = false;

                addRemoveNote.AutoSizeMode = LabelAutoSizeMode.None;

                addRemoveNote.Appearance.TextOptions.WordWrap =
                    DevExpress.Utils.WordWrap.Wrap;

                addRemoveNote.Dock = DockStyle.Fill;

                addRemovePage.Controls.Add(addRemoveNote);

                tabs.TabPages.AddRange(new[]
                {
                    generalPage,
                    addRemovePage
                });

                tabs.Dock = DockStyle.Fill;

                Controls.Add(tabs);
            }
            finally
            {
                ResumeLayout(true);
            }

            UpdateNote();

            ApplyTheme();
        }

        private void UpdateNote()
        {
            addRemoveNote.Text =
                "Extra library folders can only be added for the EA App " +
                "and GOG" +
                (string.IsNullOrWhiteSpace(clientName)
                    ? "."
                    : ", not for " + clientName + ".");
        }

        public void ApplyTheme()
        {
            addRemoveNote.Appearance.ForeColor =
                ProfileStyle.MutedTextColor;

            addRemoveNote.Appearance.Options.UseForeColor = true;

            general.ApplyTheme();
        }

        /// <summary>
        /// Picks the shared settings back up from disk, for when they
        /// were changed on another client's tab.
        /// </summary>
        public void Reload()
        {
            general.Reload();
        }
    }
}
