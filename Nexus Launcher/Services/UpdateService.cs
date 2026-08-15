using Nexus_Launcher.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;


namespace Nexus_Launcher.Services.Update
{
    internal static class UpdateService
    {
        public static void LaunchUpdater(string packagePath)
        {
            string updater =
                Path.Combine(
                    Application.StartupPath,
                    "NexusUpdater.exe");
            //MessageBox.Show("Updater path: " + updater);
            if (!File.Exists(updater))
            {
                MessageBox.Show("NexusUpdater.exe could not be found.");
                return;
            }

            if (!File.Exists(packagePath))
            {
                MessageBox.Show("Update package could not be found.");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = updater,
                Arguments =
    "\"" + Application.StartupPath + "\" " +
    "\"" + packagePath + "\" " +
    "\"" + BuildInfo.Build + "\"",
                UseShellExecute = true,
                Verb = "runas"
            });

            Application.Exit();
        }
    }
}