using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus_Launcher.Services
{
    internal static class UpdaterEndpoints
    {
        public const string BaseUrl =
            "https://guardbyte.me/downloads/Nexus%20Launcher/";

        public const string Latest =
            BaseUrl + "latest.json";

        public const string Packages =
            BaseUrl + "packages/";
    }
}
