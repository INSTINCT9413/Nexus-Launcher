using Nexus_Launcher.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;


namespace Nexus_Launcher.Services.Update
{
    internal static class UpdateService
    {
        /// <summary>
        /// Windows' "operation was cancelled by the user", raised when the
        /// administrator prompt is declined.
        /// </summary>
        private const int ErrorCancelled = 1223;

        /// <summary>
        /// Starts the updater and returns whether it actually started.
        ///
        /// Callers exit the app only when this succeeds. It used to return
        /// nothing, so a missing updater showed its error and the app was
        /// then closed anyway, and declining the administrator prompt threw
        /// out of Process.Start and crashed.
        /// </summary>
        public static bool LaunchUpdater(string packagePath)
        {
            string updater =
                Path.Combine(
                    Application.StartupPath,
                    "NexusUpdater.exe");
            //MessageBox.Show("Updater path: " + updater);
            if (!File.Exists(updater))
            {
                MessageBox.Show("NexusUpdater.exe could not be found.");
                return false;
            }

            if (!File.Exists(packagePath))
            {
                MessageBox.Show("Update package could not be found.");
                return false;
            }

            try
            {
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
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
            {
                // The user said no to the administrator prompt. Nothing is
                // wrong; the update simply does not happen this time.
                return false;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                MessageBox.Show("The updater could not be started: " + ex.Message);
                return false;
            }

            Application.Exit();

            return true;
        }
    }
}
