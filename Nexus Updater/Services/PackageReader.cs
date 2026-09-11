

using NexusUpdater.Models;
using System.IO;
using System.Runtime.Serialization.Json;

namespace NexusUpdater.Services
{
    internal static class PackageReader
    {
        public static UpdateManifest Read(string folder)
        {
            string manifestPath =
                Path.Combine(
                    folder,
                    "manifest.json");

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
    }
}