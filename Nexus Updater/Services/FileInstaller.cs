
using NexusUpdater.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace NexusUpdater.Services
{
    internal static class FileInstaller
    {
        public static async Task Install(
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
    }
}