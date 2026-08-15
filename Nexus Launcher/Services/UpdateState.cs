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
    internal static class UpdateState
    {
        private static readonly string LocalInfo =
            Path.Combine(
                Application.StartupPath,
                "latest.json");

        public static int InstalledBuild
        {
            get
            {
                if (!File.Exists(LocalInfo))
                {
                    SaveCurrentBuild();

                    return BuildInfo.Build;
                }

                InstalledUpdateInfo info =
                    JsonConvert.DeserializeObject<InstalledUpdateInfo>(
                        File.ReadAllText(LocalInfo));

                return info.build;
            }
        }

        public static void SaveCurrentBuild()
        {
            InstalledUpdateInfo info =
                new InstalledUpdateInfo
                {
                    build = BuildInfo.Build,
                    version = BuildInfo.Version
                };

            File.WriteAllText(
                LocalInfo,
                JsonConvert.SerializeObject(
                    info,
                    Formatting.Indented));
        }

        public static void SaveInstalled(UpdateInfo update)
        {
            InstalledUpdateInfo info =
                new InstalledUpdateInfo
                {
                    build = update.build,
                    version = update.version
                };

            File.WriteAllText(
                LocalInfo,
                JsonConvert.SerializeObject(
                    info,
                    Formatting.Indented));
        }
    }
}
