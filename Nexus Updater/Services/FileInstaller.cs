using Newtonsoft.Json;
using NexusUpdater.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NexusUpdater.Services
{
    internal static class FileInstaller
    {
        public static void Install(
    string extractedFolder,
    string installFolder,
    UpdateManifest manifest,
    Action<int, int, string> progress)
        {
            string filesFolder =
                Path.Combine(
                    extractedFolder,
                    "Files");

            if (!Directory.Exists(filesFolder))
                throw new DirectoryNotFoundException(
                    "Files folder not found.");

            List<string> files =
                Directory.GetFiles(
                    filesFolder,
                    "*.*",
                    SearchOption.AllDirectories)
                .ToList();

            int total =
                files.Count;

            int current =
                0;

            foreach (string sourceFile in files)
            {
                current++;

                string relative =
                    sourceFile.Substring(
                        filesFolder.Length + 1);

                string destination =
                    Path.Combine(
                        installFolder,
                        relative);

                string directory =
                    Path.GetDirectoryName(
                        destination);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                progress?.Invoke(
                    current,
                    total,
                    relative);

                File.Copy(
                    sourceFile,
                    destination,
                    true);
            }
            SaveInstalledVersion(
    installFolder,
    manifest);
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
    }
}