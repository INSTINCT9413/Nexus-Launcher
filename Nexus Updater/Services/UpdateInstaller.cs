using NexusUpdater.Models;
using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;

namespace NexusUpdater.Services
{
    internal static class UpdateInstaller
    {
        public static bool Install(
            string packagePath,
            string installFolder)
        {
            string tempFolder =
                Path.Combine(
                    Path.GetTempPath(),
                    "NexusLauncherUpdate");

            //------------------------------------------------
            // Clean temp
            //------------------------------------------------

            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(
                    tempFolder,
                    true);
            }

            Directory.CreateDirectory(
                tempFolder);

            //------------------------------------------------
            // Extract package
            //------------------------------------------------

            ZipFile.ExtractToDirectory(
                packagePath,
                tempFolder);

            string manifestPath =
                Path.Combine(
                    tempFolder,
                    "manifest.json");

            MessageBox.Show("Extracted");

            UpdateManifest manifest =
                ReadManifest(manifestPath);

            if (manifest == null)
            {
                MessageBox.Show(
                    "The update manifest could not be read.",
                    "Nexus Updater",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }

            MessageBox.Show("Manifest Read");

            //------------------------------------------------
            // Copy files
            //------------------------------------------------

            CopyDirectory(
                Path.Combine(
                    tempFolder,
                    "Files"),
                installFolder);

            MessageBox.Show("Copied");

            MessageBox.Show(
                "About to save latest.json");

            //------------------------------------------------
            // Save installed version
            //------------------------------------------------

            SaveInstalledVersion(
                installFolder,
                manifest);

            MessageBox.Show(
                "Writing Build: " +
                manifest.build);

            //------------------------------------------------
            // Cleanup
            //------------------------------------------------

            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(
                    tempFolder,
                    true);
            }

            return true;
        }

        // =========================================================
        // READ MANIFEST
        // =========================================================

        private static UpdateManifest ReadManifest(
            string manifestPath)
        {
            if (!File.Exists(manifestPath))
                return null;

            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(
                    typeof(UpdateManifest));

            using (FileStream stream =
                File.OpenRead(manifestPath))
            {
                return serializer.ReadObject(stream)
                    as UpdateManifest;
            }
        }

        // =========================================================
        // SAVE INSTALLED VERSION
        // =========================================================

        private static void SaveInstalledVersion(
            string installFolder,
            UpdateManifest manifest)
        {
            string filePath =
                Path.Combine(
                    installFolder,
                    "latest.json");

            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(
                    typeof(UpdateManifest));

            using (MemoryStream stream =
                new MemoryStream())
            {
                serializer.WriteObject(
                    stream,
                    manifest);

                string json =
                    Encoding.UTF8.GetString(
                        stream.ToArray());

                File.WriteAllText(
                    filePath,
                    json,
                    Encoding.UTF8);
            }
        }

        // =========================================================
        // COPY DIRECTORY
        // =========================================================

        private static void CopyDirectory(
            string source,
            string destination)
        {
            foreach (string directory in
                     Directory.GetDirectories(
                         source,
                         "*",
                         SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(
                    directory.Replace(
                        source,
                        destination));
            }

            foreach (string file in
                     Directory.GetFiles(
                         source,
                         "*.*",
                         SearchOption.AllDirectories))
            {
                string dest =
                    file.Replace(
                        source,
                        destination);

                File.Copy(
                    file,
                    dest,
                    true);
            }
        }
    }
}