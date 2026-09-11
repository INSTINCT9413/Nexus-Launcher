using NexusUpdater.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Updater
{
    public partial class Form1 : Form
    {
        private readonly string installFolder;
        private readonly string packagePath;
        private readonly string currentVersion;

        public Form1(
            string installFolder,
            string packagePath,
            string currentVersion)
        {
            InitializeComponent();

            this.installFolder = installFolder;
            this.packagePath = packagePath;
            this.currentVersion = currentVersion;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await Task.Delay(2500);

            await UpdateEngine.Run(
    this,
    installFolder,
    packagePath,
    currentVersion);
        }
        public void SetStatus(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(SetStatus), text);
                return;
            }

            lblStatus.Text = text;
        }

        public void SetProgress(int value, int maximum)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<int, int>(SetProgress), value, maximum);
                return;
            }

            progressBar1.Maximum = maximum;
            progressBar1.Value = Math.Min(value, maximum);
        }

        public void SetVersion(string oldVersion, string newVersion)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string>(SetVersion), oldVersion, newVersion);
                return;
            }

            lblVersion.Text =
                $"Updating {oldVersion} → {newVersion}";
        }
    }
}
