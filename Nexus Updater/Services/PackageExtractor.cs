using System.IO;
using System.IO.Compression;

namespace NexusUpdater.Services
{
    internal static class PackageExtractor
    {
        public static string Extract(string packagePath)
        {
            string extractFolder =
                Path.Combine(
                    Path.GetTempPath(),
                    "NexusLauncherUpdate");

            if (Directory.Exists(extractFolder))
            {
                Directory.Delete(
                    extractFolder,
                    true);
            }

            Directory.CreateDirectory(
                extractFolder);

            ZipFile.ExtractToDirectory(
                packagePath,
                extractFolder);

            return extractFolder;
        }
    }
}