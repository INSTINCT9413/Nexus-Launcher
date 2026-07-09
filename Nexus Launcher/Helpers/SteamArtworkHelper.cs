using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Nexus_Launcher.Helpers
{
    internal class SteamArtworkHelper
    {
        public static string GetLibraryCachePath(
            string steamPath)
        {
            return Path.Combine(
                steamPath,
                "appcache",
                "librarycache");
        }

        public static string GetHeaderImage(
            string steamPath,
            int appId)
        {
            string path = Path.Combine(
                GetLibraryCachePath(steamPath),
                appId + "_header.jpg");

            return File.Exists(path)
                ? path
                : null;
        }

        public static string GetIconImage(
            string steamPath,
            int appId)
        {
            string path = Path.Combine(
                GetLibraryCachePath(steamPath),
                appId + "_icon.jpg");

            return File.Exists(path)
                ? path
                : null;
        }

        public static string GetLogoImage(
            string steamPath,
            int appId)
        {
            string path = Path.Combine(
                GetLibraryCachePath(steamPath),
                appId + "_logo.png");

            return File.Exists(path)
                ? path
                : null;
        }

        public static string GetLibraryImage(
            string steamPath,
            int appId)
        {
            string path = Path.Combine(
                GetLibraryCachePath(steamPath),
                appId + "_library_600x900.jpg");

            return File.Exists(path)
                ? path
                : null;
        }
    }
}
