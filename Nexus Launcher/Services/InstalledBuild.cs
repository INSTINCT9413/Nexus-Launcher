using Newtonsoft.Json;
using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher.Services
{
    internal static class InstalledBuild
    {
        private static readonly string VersionFile =
            Path.Combine(
                Application.StartupPath,
                "latest.json");

        private static InstalledUpdateInfo current;

        public static InstalledUpdateInfo Current
        {
            get
            {
                if (current != null)
                    return current;

                //----------------------------------------------------
                // Try reading latest.json
                //----------------------------------------------------

                if (File.Exists(VersionFile))
                {
                    try
                    {
                        current =
                            JsonConvert.DeserializeObject<InstalledUpdateInfo>(
                                File.ReadAllText(VersionFile));

                        if (current != null)
                            return current;
                    }
                    catch
                    {
                    }
                }

                //----------------------------------------------------
                // Fall back to executable build
                //----------------------------------------------------

                current =
                    new InstalledUpdateInfo
                    {
                        build = BuildInfo.Build,
                        version = BuildInfo.Version
                    };

                try
                {
                    File.WriteAllText(
                        VersionFile,
                        JsonConvert.SerializeObject(
                            current,
                            Formatting.Indented));
                }
                catch
                {
                }

                return current;
            }
        }

        public static string Display
        {
            get
            {
                return $"v{Current.version} (Build: {Current.build})";
            }
        }

        public static void Reload()
        {
            current = null;
        }
    }
}
