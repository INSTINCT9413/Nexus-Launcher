using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Nexus_Launcher.Models;

namespace Nexus_Launcher.Services
{
    public class NexusLinksService
    {
        // Change this to the actual location of your JSON file.
        private const string ConfigUrl =
            "https://guardbyte.me/downloads/Nexus%20Launcher/nexus-links.json";

        private readonly string _cachePath;

        private NexusLinksConfig _config;

        public NexusLinksService()
        {
            string cacheDirectory = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "Nexus Launcher",
                "Cache");

            Directory.CreateDirectory(cacheDirectory);

            _cachePath = Path.Combine(
                cacheDirectory,
                "nexus-links.json");
        }

        public NexusLinksConfig Config
        {
            get
            {
                return _config;
            }
        }

        public async Task<bool> LoadAsync()
        {
            // =====================================================
            // TRY TO DOWNLOAD FROM SERVER
            // =====================================================

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout =
                        TimeSpan.FromSeconds(10);

                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "Nexus Launcher");

                    string json =
                        await client.GetStringAsync(ConfigUrl);

                    NexusLinksConfig config =
                        Deserialize(json);

                    if (config != null)
                    {
                        _config = config;

                        // Save the downloaded configuration locally.
                        File.WriteAllText(
                            _cachePath,
                            json,
                            Encoding.UTF8);

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Server unavailable.
                // We will try the cached copy below.

                Program.LogCrash(ex);
            }


            // =====================================================
            // SERVER FAILED - TRY LOCAL CACHE
            // =====================================================

            try
            {
                if (File.Exists(_cachePath))
                {
                    string json =
                        File.ReadAllText(
                            _cachePath,
                            Encoding.UTF8);

                    NexusLinksConfig config =
                        Deserialize(json);

                    if (config != null)
                    {
                        _config = config;

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }


            // =====================================================
            // NOTHING AVAILABLE
            // =====================================================

            return false;
        }


        // =========================================================
        // JSON DESERIALIZATION
        // =========================================================

        private NexusLinksConfig Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(
                    typeof(NexusLinksConfig));

            using (MemoryStream stream =
                new MemoryStream(
                    Encoding.UTF8.GetBytes(json)))
            {
                return serializer.ReadObject(stream)
                    as NexusLinksConfig;
            }
        }


        // =========================================================
        // GET LAUNCHER LINKS
        // =========================================================

        public LauncherLinks GetLauncher(string launcherId)
        {
            if (_config == null)
                return null;

            if (string.IsNullOrWhiteSpace(launcherId))
                return null;


            switch (launcherId.ToLowerInvariant())
            {
                case "steam":
                    return _config.Steam;

                case "ubisoft":
                case "ubisoftconnect":
                    return _config.Ubisoft;

                case "ea":
                    return _config.EA;

                case "battlenet":
                case "battle.net":
                    return _config.BattleNet;

                case "epic":
                case "epicgames":
                    return _config.Epic;

                case "gog":
                    return _config.GOG;

                case "nexus":
                case "nexuslauncher":
                    return _config.Nexus;

                default:
                    return null;
            }
        }
    }
}