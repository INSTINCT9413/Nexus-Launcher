using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher.Services
{
    internal static class BrowserProfileManager
    {
        public static async Task<CoreWebView2Environment>
            CreateEnvironmentAsync(
                string profileName)
        {
            string profilePath =
                Path.Combine(
                    Application.StartupPath,
                    "Profiles",
                    profileName);

            Directory.CreateDirectory(
                profilePath);

            return await CoreWebView2Environment
                .CreateAsync(
                    null,
                    profilePath);
        }
    }
}