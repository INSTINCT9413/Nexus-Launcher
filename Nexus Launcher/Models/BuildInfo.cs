using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher.Models
{
    internal static class BuildInfo
    {
        public static string Version => Application.ProductVersion;

        public const int Build = 1010;

        public static string Display => $@"v{Version} (Build: {Build})";
    }
}
