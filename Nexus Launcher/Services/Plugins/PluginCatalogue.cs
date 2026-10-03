using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Nexus_Launcher.Services.Plugins
{
    /// <summary>
    /// One plugin as the Nexus site publishes it.
    /// </summary>
    internal class CatalogueEntry
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>What the downloaded file is called.</summary>
        [JsonProperty("file")]
        public string File { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// SHA-256 of the published file. This is what makes the
        /// official badge mean anything, so an entry without one is
        /// never treated as official.
        /// </summary>
        [JsonProperty("sha256")]
        public string Sha256 { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }
    }

    internal class CatalogueFile
    {
        [JsonProperty("formatVersion")]
        public int FormatVersion { get; set; }

        [JsonProperty("plugins")]
        public List<CatalogueEntry> Plugins { get; set; }
    }

    /// <summary>
    /// The list of plugins Nexus publishes, and what it is used for.
    ///
    /// Two jobs, both resting on the same idea. It says which plugins
    /// can be installed from inside Nexus, and it decides which
    /// installed plugins are official.
    ///
    /// Official means "byte for byte the file we published", checked
    /// by hash. Not "the author field says Nexus Launcher", which any
    /// plugin could claim, and which would make the badge worth
    /// nothing precisely where it matters: telling a user whether the
    /// code about to run inside their launcher came from us.
    /// </summary>
    internal static class PluginCatalogue
    {
        private const string Url =
            "https://nexuspowered.com/Nexus/plugins.json";

        private static List<CatalogueEntry> entries =
            new List<CatalogueEntry>();

        private static readonly Dictionary<string, string> hashes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Raised when the list changes, so the page can redraw.</summary>
        public static event Action Changed;

        public static IEnumerable<CatalogueEntry> Entries
        {
            get { return entries; }
        }

        /// <summary>
        /// True once the site has been reached at least once this
        /// session or a cached copy was read. Until then the page
        /// says it is still looking rather than "nothing available".
        /// </summary>
        public static bool Loaded { get; private set; }

        private static string CacheFile
        {
            get
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "NexusLauncher");

                Directory.CreateDirectory(folder);

                return Path.Combine(folder, "plugins-catalogue.json");
            }
        }

        //--------------------------------------------------------------
        // Loading
        //--------------------------------------------------------------

        /// <summary>
        /// Reads the cached copy, then refreshes from the site in the
        /// background. Nothing here blocks the user interface, and
        /// being offline simply means working from the cache.
        /// </summary>
        public static void Start()
        {
            ReadCache();

            Task.Run(() => Refresh());
        }

        private static void ReadCache()
        {
            try
            {
                if (!File.Exists(CacheFile))
                    return;

                Parse(File.ReadAllText(CacheFile));
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        public static void Refresh()
        {
            try
            {
                ServicePointManager.SecurityProtocol |=
                    SecurityProtocolType.Tls12;

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(20);

                    client.DefaultRequestHeaders.UserAgent
                        .ParseAdd("NexusLauncher");

                    string body = client
                        .GetStringAsync(Url)
                        .GetAwaiter()
                        .GetResult();

                    if (!Parse(body))
                        return;

                    try
                    {
                        File.WriteAllText(CacheFile, body);
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                // Offline, or the file is not published yet. Neither
                // is worth bothering the user about: the cache or an
                // empty list is a perfectly good answer.
                System.Diagnostics.Debug.WriteLine(
                    "plugin catalogue: " + ex.Message);
            }
        }

        private static bool Parse(
            string json)
        {
            try
            {
                CatalogueFile file =
                    JsonConvert.DeserializeObject<CatalogueFile>(json);

                if (file == null || file.Plugins == null)
                    return false;

                entries = file.Plugins
                    .Where(p => p != null &&
                                !string.IsNullOrWhiteSpace(p.Id))
                    .ToList();

                Loaded = true;

                Action changed = Changed;

                if (changed != null)
                    changed();

                return true;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return false;
            }
        }

        public static CatalogueEntry Find(
            string id)
        {
            return entries.FirstOrDefault(e =>
                string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        //--------------------------------------------------------------
        // Is this one of ours?
        //--------------------------------------------------------------

        /// <summary>
        /// SHA-256 of a file, as lower case hex. Cached by path and
        /// write time, because the plugin list redraws often and
        /// hashing on every paint would be wasteful.
        /// </summary>
        private static string HashOf(
            string file)
        {
            try
            {
                string key = file + "|" +
                    File.GetLastWriteTimeUtc(file).Ticks;

                string cached;

                if (hashes.TryGetValue(key, out cached))
                    return cached;

                using (SHA256 sha = SHA256.Create())
                using (FileStream stream = File.OpenRead(file))
                {
                    string hex = BitConverter
                        .ToString(sha.ComputeHash(stream))
                        .Replace("-", string.Empty)
                        .ToLowerInvariant();

                    hashes[key] = hex;

                    return hex;
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return null;
            }
        }

        /// <summary>
        /// Whether this installed plugin is the file Nexus published.
        ///
        /// Both halves matter. A matching id alone would let anyone
        /// publish a plugin called nexus.savebackup and inherit the
        /// badge; a matching hash is the part that cannot be faked
        /// without the file actually being ours.
        /// </summary>
        public static bool IsOfficial(
            PluginRecord plugin)
        {
            if (plugin == null || string.IsNullOrEmpty(plugin.File))
                return false;

            CatalogueEntry entry = Find(plugin.Id);

            if (entry == null || string.IsNullOrWhiteSpace(entry.Sha256))
                return false;

            string actual = HashOf(plugin.File);

            return actual != null &&
                   string.Equals(actual, entry.Sha256.Trim(),
                       StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// An official plugin whose file does not match what we
        /// published. Worth saying out loud rather than quietly
        /// showing it as somebody's unofficial plugin.
        /// </summary>
        public static bool IsModified(
            PluginRecord plugin)
        {
            if (plugin == null || string.IsNullOrEmpty(plugin.File))
                return false;

            CatalogueEntry entry = Find(plugin.Id);

            return entry != null &&
                   !string.IsNullOrWhiteSpace(entry.Sha256) &&
                   !IsOfficial(plugin);
        }

        /// <summary>
        /// A published version newer than the one installed.
        /// </summary>
        public static bool HasUpdate(
            PluginRecord plugin)
        {
            CatalogueEntry entry = Find(plugin.Id);

            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.Version) ||
                string.IsNullOrWhiteSpace(plugin.Version))
            {
                return false;
            }

            Version published;
            Version installed;

            return Version.TryParse(entry.Version, out published) &&
                   Version.TryParse(plugin.Version, out installed) &&
                   published > installed;
        }

        //--------------------------------------------------------------
        // Installing
        //--------------------------------------------------------------

        /// <summary>
        /// Downloads a published plugin into the user's plugins
        /// folder.
        ///
        /// The hash is checked before the file is put in place, and a
        /// mismatch throws rather than installing: this writes code
        /// that will run inside Nexus, so "probably the right file"
        /// is not good enough. Downloaded to a .part and renamed, so
        /// a failed download cannot leave half a plugin behind.
        /// </summary>
        public static void Install(
            CatalogueEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Url))
                throw new InvalidOperationException(
                    "That plugin has no download address.");

            ServicePointManager.SecurityProtocol |=
                SecurityProtocolType.Tls12;

            Directory.CreateDirectory(PluginService.UserFolder);

            string name = string.IsNullOrWhiteSpace(entry.File)
                ? entry.Id + ".dll"
                : Path.GetFileName(entry.File);

            string target = Path.Combine(PluginService.UserFolder, name);

            string partial = target + ".part";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(2);

                    client.DefaultRequestHeaders.UserAgent
                        .ParseAdd("NexusLauncher");

                    byte[] data = client
                        .GetByteArrayAsync(entry.Url)
                        .GetAwaiter()
                        .GetResult();

                    if (!string.IsNullOrWhiteSpace(entry.Sha256))
                    {
                        string actual;

                        using (SHA256 sha = SHA256.Create())
                        {
                            actual = BitConverter
                                .ToString(sha.ComputeHash(data))
                                .Replace("-", string.Empty)
                                .ToLowerInvariant();
                        }

                        if (!string.Equals(actual, entry.Sha256.Trim(),
                                StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                "The downloaded file is not the one " +
                                "Nexus published, so it has not been " +
                                "installed.");
                        }
                    }

                    File.WriteAllBytes(partial, data);
                }

                if (File.Exists(target))
                    File.Delete(target);

                File.Move(partial, target);
            }
            finally
            {
                try
                {
                    if (File.Exists(partial))
                        File.Delete(partial);
                }
                catch
                {
                }
            }
        }
    }
}
