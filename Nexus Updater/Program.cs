using Nexus_Updater;
using System;
using System.Windows.Forms;

namespace NexusUpdater
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length < 3)
            {
                MessageBox.Show(
                    "Invalid update arguments.",
                    "Nexus Updater",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            string installFolder = args[0];
            string packagePath = args[1];
            string currentBuild = args[2];

            Application.Run(
                new Form1(
                    installFolder,
                    packagePath,
                    currentBuild));
        }
    }
}