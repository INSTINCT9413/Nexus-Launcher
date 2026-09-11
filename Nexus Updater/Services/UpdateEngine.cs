using Nexus_Updater;
using NexusUpdater.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NexusUpdater.Services
{
    internal static class UpdateEngine
    {

        public static async Task Run(

    Form1 form,
    string installFolder,
    string packagePath,
    string currentVersion)
        {
            //----------------------------------------------------
            // Wait for Launcher
            //----------------------------------------------------
            //throw new Exception("THIS IS THE NEW UPDATE ENGINE");
            form.SetStatus("Waiting for Nexus Launcher to close...");
            await Task.Delay(250);
            WaitForLauncher();

            //----------------------------------------------------
            // Extract Package
            //----------------------------------------------------

            form.SetStatus("Extracting package...");
            await Task.Delay(4000);
            string extractedFolder =
                PackageExtractor.Extract(packagePath);

            //----------------------------------------------------
            // Read Manifest
            //----------------------------------------------------

            form.SetStatus("Reading manifest...");
            await Task.Delay(2000);
            UpdateManifest manifest =
                PackageReader.Read(extractedFolder);

            if (manifest == null)
            {
                MessageBox.Show("Invalid update package.");

                return;
            }

            form.SetVersion(
                currentVersion,
                manifest.version);

            //----------------------------------------------------
            // Install Files
            //----------------------------------------------------

            form.SetStatus("Installing update...");
            await Task.Delay(2000);
            await FileInstaller.Install(
     extractedFolder,
     installFolder,
     manifest,
     (current, total, file) =>
     {
         form.SetProgress(current, total);

         form.SetStatus(
             "Copying " + file);
     });

            //----------------------------------------------------
            // Restart Launcher
            //----------------------------------------------------

            form.SetStatus("Launching Nexus Launcher...");

            System.Diagnostics.Process.Start(
                Path.Combine(
                    installFolder,
                    "Nexus Launcher.exe"));
            await Task.Delay(2000);
            form.Close();

            await Task.CompletedTask;
        }
        private static void WaitForLauncher()
        {
            while (System.Diagnostics.Process
                .GetProcessesByName("Nexus Launcher")
                .Length > 0)
            {
                Thread.Sleep(500);
            }
        }
    }
}