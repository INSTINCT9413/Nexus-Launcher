using Nexus.Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Nexus.Plugin.Sample
{
    //==================================================================
    // The example plugin.
    //
    // It exercises all three things a plugin can do, and it is meant
    // to be read: if you are writing a plugin, copy this folder,
    // change the id and delete the parts you do not want.
    //
    // To build one yourself:
    //   1. New Class Library (.NET Framework 4.8).
    //   2. Reference Nexus.Plugin.SDK.dll, which sits beside
    //      "Nexus Launcher.exe".
    //   3. Write one class with [NexusPlugin] that implements
    //      INexusPlugin.
    //   4. Drop the built dll in
    //      %AppData%\NexusLauncher\Plugins and restart Nexus.
    //
    // Nothing else is required. No installer, no registry, no
    // reference to Nexus itself.
    //==================================================================

    [NexusPlugin(
        "nexus.sample",
        "Sample Plugin",
        Author = "Nexus Launcher",
        Version = "1.0.0",
        Description = "A worked example of the three things a plugin " +
                      "can do. Safe to disable.")]
    public class SamplePlugin : INexusPlugin
    {
        private INexusHost host;

        public void Initialize(
            INexusHost host)
        {
            this.host = host;

            host.Log.Info("Sample plugin starting on Nexus " +
                host.LauncherVersion + " build " + host.LauncherBuild);

            // 1. A library source. This one invents nothing: it lists
            //    whatever shortcuts are sitting in a folder inside the
            //    plugin's own data directory, so you can see games
            //    appear without installing another launcher.
            host.AddLibrarySource(new SampleLibrarySource(host));

            // 2. A game action, on the menu for every game.
            host.AddGameAction(new OpenInstallFolderAction());

            // 3. A side panel tile.
            host.AddSidePanelWidget(new SampleWidget(host));

            // 4. Events, for anything that wants to react to play.
            host.GameLaunched += (s, e) =>
                host.Log.Info("launched: " + e.Game.Name);

            host.GameExited += (s, e) =>
                host.Log.Info("closed: " + e.Game.Name +
                    " after " + e.Game.Played);
        }

        public void Shutdown()
        {
            if (host != null)
                host.Log.Info("Sample plugin stopping");
        }
    }

    /// <summary>
    /// Lists .lnk and .exe files dropped into the plugin's own
    /// "Games" folder, so the source has something real to find
    /// without needing another launcher installed.
    /// </summary>
    internal class SampleLibrarySource : ILibrarySource
    {
        private readonly INexusHost host;

        public SampleLibrarySource(
            INexusHost host)
        {
            this.host = host;
        }

        public string Id
        {
            get { return "sample"; }
        }

        public string DisplayName
        {
            get { return "Sample"; }
        }

        private string GamesFolder
        {
            get { return Path.Combine(host.DataFolder, "Games"); }
        }

        /// <summary>
        /// Only claims to be present once the folder exists, so the
        /// heading does not appear empty for everyone who happens to
        /// have the example enabled.
        /// </summary>
        public bool IsInstalled()
        {
            try
            {
                return Directory.Exists(GamesFolder) &&
                       Directory.GetFiles(GamesFolder).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerable<PluginGame> Scan()
        {
            List<PluginGame> games = new List<PluginGame>();

            if (!Directory.Exists(GamesFolder))
                return games;

            foreach (string file in Directory.GetFiles(GamesFolder))
            {
                string extension =
                    Path.GetExtension(file).ToLowerInvariant();

                if (extension != ".lnk" && extension != ".exe")
                    continue;

                games.Add(new PluginGame
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    SourceGameId = Path.GetFileName(file),
                    ExecutablePath = file,
                    InstallPath = GamesFolder
                });
            }

            host.Log.Info("sample source found " + games.Count + " game(s)");

            return games;
        }
    }

    /// <summary>
    /// Opens a game's folder in Explorer. About as small as a useful
    /// action gets.
    /// </summary>
    internal class OpenInstallFolderAction : IGameAction
    {
        public string Id
        {
            get { return "sample.openfolder"; }
        }

        public string Caption
        {
            get { return "Open install folder"; }
        }

        public bool AppliesTo(
            GameRef game)
        {
            return game != null &&
                   !string.IsNullOrEmpty(game.InstallPath) &&
                   Directory.Exists(game.InstallPath);
        }

        public void Invoke(
            GameRef game)
        {
            Process.Start(new ProcessStartInfo(game.InstallPath)
            {
                UseShellExecute = true
            });
        }
    }

    /// <summary>
    /// A tile showing how long Nexus has been open. Trivial on
    /// purpose: it shows the shape without distracting from it.
    /// </summary>
    internal class SampleWidget : ISidePanelWidget
    {
        private readonly DateTime started = DateTime.UtcNow;

        private readonly INexusHost host;

        public SampleWidget(
            INexusHost host)
        {
            this.host = host;
        }

        public string Id
        {
            get { return "sample.uptime"; }
        }

        public string Title
        {
            get { return "Sample Plugin"; }
        }

        public TimeSpan RefreshInterval
        {
            get { return TimeSpan.FromSeconds(10); }
        }

        public IEnumerable<string> GetLines()
        {
            TimeSpan open = DateTime.UtcNow - started;

            return new[]
            {
                "Nexus open for " + (int)open.TotalMinutes + " min",
                "Data: " + Path.GetFileName(host.DataFolder)
            };
        }
    }
}
