using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nexus_Launcher.Services.Plugins
{
    /// <summary>
    /// Where a plugin's settings are kept.
    ///
    /// One plain "key=value" file per plugin, inside the plugin's own
    /// data folder. Plain text rather than a serialised blob so a
    /// user can open it, see what a plugin is storing, and fix it by
    /// hand when a plugin writes something daft.
    /// </summary>
    internal static class PluginSettingsStore
    {
        private static readonly Dictionary<string, Dictionary<string, string>> cache =
            new Dictionary<string, Dictionary<string, string>>(
                StringComparer.OrdinalIgnoreCase);

        private static string FileFor(
            string pluginId)
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "NexusLauncher",
                "PluginData",
                Safe(pluginId));

            Directory.CreateDirectory(folder);

            return Path.Combine(folder, "settings.txt");
        }

        private static string Safe(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return "unknown";

            foreach (char bad in Path.GetInvalidFileNameChars())
                id = id.Replace(bad, '_');

            return id;
        }

        public static Dictionary<string, string> Load(
            string pluginId)
        {
            Dictionary<string, string> values;

            if (cache.TryGetValue(pluginId, out values))
                return values;

            values = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            try
            {
                string file = FileFor(pluginId);

                if (File.Exists(file))
                {
                    foreach (string line in File.ReadAllLines(file))
                    {
                        int split = line.IndexOf('=');

                        if (split <= 0)
                            continue;

                        values[line.Substring(0, split).Trim()] =
                            line.Substring(split + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            cache[pluginId] = values;

            return values;
        }

        public static void Save(
            string pluginId,
            Dictionary<string, string> values)
        {
            cache[pluginId] = values;

            try
            {
                File.WriteAllLines(
                    FileFor(pluginId),
                    values.Select(p => p.Key + "=" + p.Value).ToArray(),
                    Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        public static string Get(
            string pluginId,
            string key,
            string fallback)
        {
            string value;

            return Load(pluginId).TryGetValue(key ?? string.Empty, out value)
                ? value
                : fallback;
        }
    }
}
