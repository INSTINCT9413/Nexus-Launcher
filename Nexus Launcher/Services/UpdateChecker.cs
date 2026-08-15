using Newtonsoft.Json;
using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Nexus_Launcher.Services
{
    internal static class UpdateChecker
    {
        private static readonly HttpClient Client =
            new HttpClient();

        private const string LatestUrl =
            "https://guardbyte.me/downloads/Nexus%20Launcher/latest.json";

        public static async Task<UpdateInfo> CheckAsync()
        {
            try
            {
                string json =
                    await Client.GetStringAsync(LatestUrl);

                return JsonConvert.DeserializeObject<UpdateInfo>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
