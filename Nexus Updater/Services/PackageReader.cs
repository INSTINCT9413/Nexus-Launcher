using DevExpress.XtraPrinting.Native.WebClientUIControl;
using Newtonsoft.Json;
using NexusUpdater.Models;
using System.IO;

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

            return JsonConvert.DeserializeObject<UpdateManifest>(
                File.ReadAllText(manifestPath));
        }
    }
}