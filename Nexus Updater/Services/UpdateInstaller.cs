using Newtonsoft.Json;
using NexusUpdater.Models;
using System.IO;
using System.IO.Compression;
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
                JsonConvert.DeserializeObject<UpdateManifest>(
                    File.ReadAllText(manifestPath));
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
            MessageBox.Show("About to save latest.json");

            SaveInstalledVersion(
                installFolder,
                manifest);
            MessageBox.Show(
    $"Writing Build: {manifest.build}");
            //------------------------------------------------
            // Cleanup
            //------------------------------------------------

            Directory.Delete(
                tempFolder,
                true);

            return true;
        }
        private static void SaveInstalledVersion(
    string installFolder,
    UpdateManifest manifest)
        {
            string json =
                JsonConvert.SerializeObject(
                    new
                    {
                        build = manifest.build,
                        version = manifest.version
                    },
                    Formatting.Indented);

            File.WriteAllText(
                Path.Combine(
                    installFolder,
                    "latest.json"),
                json);
        }
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