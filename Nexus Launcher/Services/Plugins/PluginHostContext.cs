using Nexus.Plugin;
using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nexus_Launcher.Services.Plugins
{
    /// <summary>
    /// The INexusHost a plugin is handed.
    ///
    /// One per plugin, so its log lines are tagged with it, its data
    /// folder is its own, and anything it registers can be traced back
    /// to it when it misbehaves.
    /// </summary>
    internal class PluginHostContext : INexusHost
    {
        private static readonly Dictionary<PluginRecord, PluginHostContext> hosts =
            new Dictionary<PluginRecord, PluginHostContext>();

        private readonly PluginRecord plugin;

        public PluginHostContext(
            PluginRecord plugin)
        {
            this.plugin = plugin;

            hosts[plugin] = this;
        }

        public static PluginHostContext For(
            PluginRecord plugin)
        {
            PluginHostContext host;

            return plugin != null && hosts.TryGetValue(plugin, out host)
                ? host
                : null;
        }

        public string LauncherVersion
        {
            get { return BuildInfo.Version; }
        }

        public int LauncherBuild
        {
            get { return BuildInfo.Build; }
        }

        public string DataFolder
        {
            get
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "NexusLauncher",
                    "PluginData",
                    SafeName(plugin.Id));

                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                }

                return path;
            }
        }

        /// <summary>
        /// Plugin ids are the author's to choose, so they cannot be
        /// trusted as a folder name.
        /// </summary>
        private static string SafeName(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return "unknown";

            foreach (char bad in Path.GetInvalidFileNameChars())
                id = id.Replace(bad, '_');

            return id;
        }

        public ILogger Log
        {
            get { return new PluginLogger(plugin); }
        }

        public string GetSetting(
            string key,
            string fallback = null)
        {
            return PluginSettingsStore.Get(plugin.Id, key, fallback);
        }

        public bool GetSetting(
            string key,
            bool fallback)
        {
            string value =
                PluginSettingsStore.Get(plugin.Id, key, null);

            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            return string.Equals(value, "true",
                StringComparison.OrdinalIgnoreCase);
        }

        public IEnumerable<GameRef> GetGames()
        {
            List<GameRef> games = new List<GameRef>();

            try
            {
                foreach (GameInfo game in LibraryService.GetGames())
                {
                    GameRef reference = PluginBridge.ToRef(game);

                    if (reference != null)
                        games.Add(reference);
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            return games;
        }

        public void AddLibrarySource(
            ILibrarySource source)
        {
            PluginService.Register(plugin, source);
        }

        public void AddGameAction(
            IGameAction action)
        {
            PluginService.Register(plugin, action);
        }

        public void AddSidePanelWidget(
            ISidePanelWidget widget)
        {
            PluginService.Register(plugin, widget);
        }

        public event EventHandler<GameEventArgs> GameLaunched;

        public event EventHandler<GameEventArgs> GameExited;

        internal void RaiseLaunched(
            GameRef game)
        {
            EventHandler<GameEventArgs> handler = GameLaunched;

            if (handler != null)
                handler(this, new GameEventArgs(game));
        }

        internal void RaiseExited(
            GameRef game)
        {
            EventHandler<GameEventArgs> handler = GameExited;

            if (handler != null)
                handler(this, new GameEventArgs(game));
        }

        /// <summary>
        /// Everything a plugin logs goes to the Nexus log with the
        /// plugin's name in front, so a noisy or broken plugin is
        /// obvious in a crash report.
        /// </summary>
        private class PluginLogger : ILogger
        {
            private readonly PluginRecord plugin;

            public PluginLogger(
                PluginRecord plugin)
            {
                this.plugin = plugin;
            }

            private string Tag(
                string level,
                string message)
            {
                return "[plugin " + plugin.Id + "] " + level + ": " + message;
            }

            public void Info(
                string message)
            {
                System.Diagnostics.Debug.WriteLine(Tag("info", message));
            }

            public void Warn(
                string message)
            {
                System.Diagnostics.Debug.WriteLine(Tag("warn", message));
            }

            public void Error(
                string message,
                Exception error = null)
            {
                System.Diagnostics.Debug.WriteLine(Tag("error", message));

                if (error != null)
                    Program.LogCrash(error);
            }
        }
    }
}
