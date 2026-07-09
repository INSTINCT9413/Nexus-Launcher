using DevExpress.XtraEditors;
using Nexus_Launcher.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls
{
    public partial class GOGSettings : XtraUserControl
    {
        public GOGSettings()
        {
            InitializeComponent();
        }

        private void GOGSettings_Load(object sender, EventArgs e)
        {
            labelControl1.Text =
                "Default Path:\n" +
                MainView.gogPath +
                @"\Games";

            LoadGogLibraryPaths();
            UpdateGogPathsLabel();
        }

        private void LoadGogLibraryPaths()
        {
            checkedComboBoxEdit1.Properties.Items.BeginUpdate();

            try
            {
                checkedComboBoxEdit1.Properties.Items.Clear();

                if (Settings.Default.GOGLibraryPaths == null)
                    return;

                foreach (string path in Settings.Default.GOGLibraryPaths)
                {
                    checkedComboBoxEdit1.Properties.Items.Add(
                        path,
                        path,
                        CheckState.Unchecked,
                        true);
                }
            }
            finally
            {
                checkedComboBoxEdit1.Properties.Items.EndUpdate();
            }

            UpdateDropDownRows();
            checkedComboBoxEdit1.Refresh();
        }

        private void UpdateDropDownRows()
        {
            checkedComboBoxEdit1.Properties.DropDownRows =
                Math.Min(
                    checkedComboBoxEdit1.Properties.Items.Count,
                    12);
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            using (XtraFolderBrowserDialog dlg =
                new XtraFolderBrowserDialog())
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                if (Settings.Default.GOGLibraryPaths == null)
                {
                    Settings.Default.GOGLibraryPaths =
                        new System.Collections.Specialized.StringCollection();
                }

                if (Settings.Default.GOGLibraryPaths.Contains(dlg.SelectedPath))
                    return;

                Settings.Default.GOGLibraryPaths.Add(dlg.SelectedPath);

                checkedComboBoxEdit1.Properties.Items.BeginUpdate();

                try
                {
                    checkedComboBoxEdit1.Properties.Items.Add(
                        dlg.SelectedPath,
                        dlg.SelectedPath,
                        CheckState.Unchecked,
                        true);
                }
                finally
                {
                    checkedComboBoxEdit1.Properties.Items.EndUpdate();
                }

                UpdateDropDownRows();

                Settings.Default.Save();

                UpdateGogPathsLabel();

                checkedComboBoxEdit1.Refresh();

                _ = ((MainView)FindForm())?.RefreshGogLibraryAsync();
            }
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            List<string> pathsToRemove =
                new List<string>();

            foreach (DevExpress.XtraEditors.Controls.CheckedListBoxItem item
                in checkedComboBoxEdit1.Properties.Items)
            {
                if (item.CheckState == CheckState.Checked)
                {
                    pathsToRemove.Add(item.Value.ToString());
                }
            }

            if (pathsToRemove.Count == 0)
            {
                XtraMessageBox.Show(
                    "Please check at least one library path to remove.",
                    "No Paths Selected");

                return;
            }

            if (XtraMessageBox.Show(
                "Remove the selected library paths?\n\n" +
                string.Join(Environment.NewLine, pathsToRemove),
                "Confirm Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            checkedComboBoxEdit1.Properties.Items.BeginUpdate();

            try
            {
                foreach (string path in pathsToRemove)
                {
                    Settings.Default.GOGLibraryPaths.Remove(path);

                    for (int i =
                        checkedComboBoxEdit1.Properties.Items.Count - 1;
                        i >= 0;
                        i--)
                    {
                        if (checkedComboBoxEdit1.Properties.Items[i]
                            .Value.ToString() == path)
                        {
                            checkedComboBoxEdit1.Properties.Items.RemoveAt(i);
                        }
                    }
                }
            }
            finally
            {
                checkedComboBoxEdit1.Properties.Items.EndUpdate();
            }

            UpdateDropDownRows();

            Settings.Default.Save();

            UpdateGogPathsLabel();

            checkedComboBoxEdit1.Refresh();

            _ = ((MainView)FindForm())?.RefreshGogLibraryAsync();

            XtraMessageBox.Show(
                "Library paths removed successfully.",
                "Library Updated");
        }

        private void UpdateGogPathsLabel()
        {
            if (Settings.Default.GOGLibraryPaths == null ||
                Settings.Default.GOGLibraryPaths.Count == 0)
            {
                labelControl2.Text =
                    "Added Paths:\nNone";

                return;
            }

            labelControl2.Text =
                "Added Paths:\n" +
                string.Join(
                    Environment.NewLine,
                    Settings.Default.GOGLibraryPaths
                        .Cast<string>()
                        .Select(ShortenPath));
        }

        private string ShortenPath(
    string path,
    int maxLength = 50)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            if (path.Length <= maxLength)
                return path;

            int keepStart = Math.Min(24, path.Length);
            int keepEnd = Math.Min(20, path.Length - keepStart);

            if (keepStart + keepEnd + 3 >= path.Length)
                return path;

            return path.Substring(0, keepStart) +
                   "..." +
                   path.Substring(path.Length - keepEnd);
        }

        private void checkedComboBoxEdit1_EditValueChanged(object sender, EventArgs e)
        {
        }

        private void checkedComboBoxEdit1_QueryPopUp(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UpdateDropDownRows();
            int rows =
       Math.Max(
           checkedComboBoxEdit1.Properties.Items.Count,
           4);

            int height =
                Math.Min(
                    rows * 22 + 10,
                    300);

            checkedComboBoxEdit1.Properties.PopupFormSize =
                new Size(
                    600,
                    height);

            checkedComboBoxEdit1.Properties.PopupFormMinSize =
                new Size(
                    600,
                    height);
        }
    }
}