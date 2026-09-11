using Nexus_Updater;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;

namespace NexusUpdater
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // =========================================================
            // NORMAL UPDATE MODE
            // =========================================================

            // Nexus Launcher normally starts the updater with:
            //
            // NexusUpdater.exe
            //     [0] Install Folder
            //     [1] Package Path
            //     [2] Current Build
            //

            if (args.Length >= 3)
            {
                string installFolder = args[0];
                string packagePath = args[1];
                string currentBuild = args[2];

                Application.Run(
                    new Form1(
                        installFolder,
                        packagePath,
                        currentBuild));

                return;
            }

            // =========================================================
            // RECOVERY MODE
            // =========================================================

            RunRecoveryMode();
        }

        // =============================================================
        // RECOVERY MODE
        // =============================================================

        private static void RunRecoveryMode()
        {
            string packagePath = FindUpdatePackage();

            if (string.IsNullOrEmpty(packagePath))
            {
                MessageBox.Show(
                    "No Nexus Launcher update package could be found.\r\n\r\n" +
                    "Make sure update.pkg is located in the Nexus Launcher " +
                    "installation or update directory.",
                    "Nexus Updater",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            // ---------------------------------------------------------
            // Read manifest.json from inside update.pkg
            // ---------------------------------------------------------

            UpdateManifest manifest;

            try
            {
                manifest = ReadUpdateManifest(packagePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "The update package was found, but its manifest could " +
                    "not be read.\r\n\r\n" +
                    "Package:\r\n" +
                    packagePath +
                    "\r\n\r\n" +
                    "Error:\r\n" +
                    ex.Message +
                    "\r\n\r\n" +
                    "Recommendation:\r\n" +
                    "Please try to download the update package again or reinstall the application.",
                    "Nexus Updater",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            // ---------------------------------------------------------
            // Determine installation folder
            // ---------------------------------------------------------

            string installFolder = Application.StartupPath;

            if (string.IsNullOrEmpty(installFolder) ||
                !Directory.Exists(installFolder))
            {
                MessageBox.Show(
                    "The Nexus Launcher installation folder could not " +
                    "be determined.",
                    "Nexus Updater",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            // ---------------------------------------------------------
            // Display update information
            // ---------------------------------------------------------

            string packageName = Path.GetFileName(packagePath);

            string versionText =
                string.IsNullOrEmpty(manifest.Version)
                    ? "Unknown"
                    : manifest.Version;

            string buildText =
                manifest.Build.HasValue
                    ? manifest.Build.Value.ToString()
                    : "Unknown";

            DialogResult result = MessageBox.Show(
                "A Nexus Launcher update package was found.\r\n\r\n" +

                "Package:\r\n" +
                packageName +
                "\r\n\r\n" +

                "Version:\r\n" +
                versionText +
                "\r\n\r\n" +

                "Build:\r\n" +
                buildText +
                "\r\n\r\n" +

                "Installation folder:\r\n" +
                installFolder +
                "\r\n\r\n" +

                "Would you like to install this update?",

                "Nexus Updater",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            // User selected No
            if (result != DialogResult.Yes)
                return;

            // ---------------------------------------------------------
            // Make sure Nexus Launcher is closed
            // ---------------------------------------------------------

            if (!EnsureNexusLauncherClosed())
            {
                MessageBox.Show(
                    "Nexus Launcher is still running.\r\n\r\n" +
                    "The update cannot continue until the launcher is closed.",
                    "Nexus Updater",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            // ---------------------------------------------------------
            // Start existing updater
            // ---------------------------------------------------------

            // We don't know the currently installed Nexus build here.
            // The normal launcher passes this as an argument.
            //
            // If Form1 only uses this for display, "Recovery" is fine.
            // If it actually compares builds, we can add proper
            // installed-build detection later.

            string currentBuild = "Recovery";

            Application.Run(
                new Form1(
                    installFolder,
                    packagePath,
                    currentBuild));
        }

        // =============================================================
        // CHECK / CLOSE NEXUS LAUNCHER
        // =============================================================

        private static bool IsNexusLauncherRunning()
        {
            Process[] processes =
                Process.GetProcessesByName("Nexus Launcher");

            return processes != null && processes.Length > 0;
        }

        private static bool EnsureNexusLauncherClosed()
        {
            Process[] processes =
                Process.GetProcessesByName("Nexus Launcher");

            // Nexus Launcher isn't running.
            if (processes == null || processes.Length == 0)
                return true;

            DialogResult result = MessageBox.Show(
                "Nexus Launcher is currently running.\r\n\r\n" +
                "The launcher must be closed before the update can be installed.\r\n\r\n" +
                "Would you like Nexus Updater to close it now?",

                "Nexus Updater",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            // User does not want us to close the launcher.
            if (result != DialogResult.Yes)
                return false;

            foreach (Process process in processes)
            {
                try
                {
                    if (process.HasExited)
                        continue;

                    // -------------------------------------------------
                    // First attempt a graceful shutdown.
                    // -------------------------------------------------

                    try
                    {
                        process.CloseMainWindow();
                    }
                    catch
                    {
                        // Ignore and attempt force termination below.
                    }

                    // -------------------------------------------------
                    // Give Nexus Launcher 5 seconds to close normally.
                    // -------------------------------------------------

                    if (!process.WaitForExit(5000))
                    {
                        // -------------------------------------------------
                        // Launcher didn't close.
                        // Force terminate it.
                        // -------------------------------------------------

                        try
                        {
                            process.Kill();
                        }
                        catch
                        {
                            // Ignore. Verification below will determine
                            // whether the launcher is actually gone.
                        }

                        try
                        {
                            process.WaitForExit(5000);
                        }
                        catch
                        {
                            // Ignore.
                        }
                    }
                }
                catch
                {
                    // The process may have exited between the time it
                    // was detected and when we attempted to close it.
                }
                finally
                {
                    process.Dispose();
                }
            }

            // ---------------------------------------------------------
            // Final verification
            // ---------------------------------------------------------

            return !IsNexusLauncherRunning();
        }

        // =============================================================
        // FIND UPDATE PACKAGE
        // =============================================================

        private static string FindUpdatePackage()
        {
            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            string[] searchLocations =
            {
                // Same directory as NexusUpdater.exe
                Application.StartupPath,

                // Nexus Launcher local application data
                Path.Combine(
                    localAppData,
                    "Nexus Launcher"),

                // Dedicated update directory
                Path.Combine(
                    localAppData,
                    "Nexus Launcher",
                    "Updates")
            };

            foreach (string location in searchLocations)
            {
                if (string.IsNullOrEmpty(location))
                    continue;

                if (!Directory.Exists(location))
                    continue;

                // Prefer an exact update.pkg
                string exactPackage =
                    Path.Combine(
                        location,
                        "update.pkg");

                if (File.Exists(exactPackage))
                    return exactPackage;

                // Otherwise look for any .pkg file.
                string package = Directory
                    .GetFiles(
                        location,
                        "*.pkg",
                        SearchOption.AllDirectories)
                    .OrderByDescending(
                        File.GetLastWriteTime)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(package))
                    return package;
            }

            return null;
        }

        // =============================================================
        // READ MANIFEST.JSON FROM PACKAGE
        // =============================================================

        private static UpdateManifest ReadUpdateManifest(
            string packagePath)
        {
            if (!File.Exists(packagePath))
            {
                throw new FileNotFoundException(
                    "The update package could not be found.",
                    packagePath);
            }

            using (FileStream stream =
                File.OpenRead(packagePath))

            using (ZipArchive archive =
                new ZipArchive(
                    stream,
                    ZipArchiveMode.Read))
            {
                // Your package structure is:
                //
                // update.pkg
                // ├── Files/
                // └── manifest.json
                //

                ZipArchiveEntry manifestEntry =
                    archive.GetEntry("manifest.json");

                if (manifestEntry == null)
                {
                    // Some ZIP tools can store paths with different
                    // casing or directory formatting, so perform a
                    // fallback search.

                    manifestEntry = archive.Entries
                        .FirstOrDefault(
                            x => string.Equals(
                                Path.GetFileName(x.FullName),
                                "manifest.json",
                                StringComparison.OrdinalIgnoreCase));
                }

                if (manifestEntry == null)
                {
                    throw new InvalidDataException(
                        "manifest.json was not found inside the " +
                        "update package.");
                }

                using (Stream manifestStream =
                    manifestEntry.Open())

                using (StreamReader reader =
                    new StreamReader(manifestStream))
                {
                    string json = reader.ReadToEnd();

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        throw new InvalidDataException(
                            "manifest.json is empty.");
                    }

                    DataContractJsonSerializer serializer =
                        new DataContractJsonSerializer(
                            typeof(UpdateManifest));

                    using (MemoryStream jsonStream =
                        new MemoryStream(
                            Encoding.UTF8.GetBytes(json)))
                    {
                        UpdateManifest result =
                            serializer.ReadObject(
                                jsonStream) as UpdateManifest;

                        if (result == null)
                        {
                            throw new InvalidDataException(
                                "The manifest could not be parsed.");
                        }

                        return result;
                    }
                }
            }
        }

        // =============================================================
        // UPDATE MANIFEST
        // =============================================================

        [DataContract]
        private class UpdateManifest
        {
            [DataMember(Name = "version")]
            public string Version { get; set; }

            [DataMember(Name = "build")]
            public int? Build { get; set; }
        }
    }
}