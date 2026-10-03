using Nexus.Plugin;
using System;
using System.Collections.Generic;
using System.IO;

namespace MyNexusPlugin
{
    //==================================================================
    // A starter plugin.
    //
    // Everything below is optional except Initialize and Shutdown.
    // Delete the parts you do not want, change the id and the name,
    // and you have a plugin.
    //
    // Build, then copy the .dll from bin\Debug into
    //   %AppData%\NexusLauncher\Plugins
    // and restart Nexus. It appears in Settings → Plugins.
    //==================================================================

    [NexusPlugin(
        // Permanent and unique. Used for your settings folder and to
        // remember whether the user disabled you, so never change it.
        "yourname.myplugin",
        "My Plugin",
        Author = "Your Name",
        Version = "1.0.0",
        Description = "Says what this plugin does, in one line.")]
    public class MyPlugin : INexusPlugin, IPluginSettings, IPluginCommands
    {
        private INexusHost host;

        //--------------------------------------------------------------
        // Required
        //--------------------------------------------------------------

        public void Initialize(
            INexusHost host)
        {
            this.host = host;

            host.Log.Info("my plugin is starting");

            // Pick whichever of these you need, and delete the rest.
            host.AddGameAction(new OpenFolderAction());
            host.AddSidePanelWidget(new ClockWidget());
            // host.AddLibrarySource(new MySource());

            host.GameLaunched += (s, e) =>
                host.Log.Info("started " + e.Game.Name);
        }

        public void Shutdown()
        {
            // Stop timers, close files, release anything held.
        }

        //--------------------------------------------------------------
        // Optional: settings, shown in your own dialog
        //--------------------------------------------------------------

        public IEnumerable<PluginSetting> GetSettings()
        {
            return new[]
            {
                new PluginSetting
                {
                    Key = "greeting",
                    Label = "Greeting",
                    Kind = PluginSettingKind.Text,
                    Default = "Hello",
                    Description = "Shown in the log when a game starts."
                },
                new PluginSetting
                {
                    Key = "enabled",
                    Label = "Do the thing",
                    Kind = PluginSettingKind.Toggle,
                    Default = "true"
                }
            };
        }

        public void SettingsChanged(
            IDictionary<string, string> values)
        {
            // Called after the user saves. Nexus has already stored
            // them, so this is only for reacting.
            host.Log.Info("settings saved");
        }

        //--------------------------------------------------------------
        // Optional: buttons in your settings dialog
        //--------------------------------------------------------------

        public IEnumerable<PluginCommand> GetCommands()
        {
            return new[]
            {
                new PluginCommand
                {
                    Id = "count",
                    Label = "Count my games",
                    Description = "Writes the number to the log."
                }
            };
        }

        public void RunCommand(
            string commandId)
        {
            if (commandId != "count")
                return;

            int total = 0;

            foreach (GameRef game in host.GetGames())
                total++;

            host.Log.Info(
                host.GetSetting("greeting", "Hello") +
                ", you have " + total + " games");
        }
    }

    //------------------------------------------------------------------
    // An example game action
    //------------------------------------------------------------------

    internal class OpenFolderAction : IGameAction
    {
        public string Id
        {
            get { return "yourname.myplugin.openfolder"; }
        }

        public string Caption
        {
            get { return "Open install folder"; }
        }

        /// <summary>
        /// Return false and the entry is left out for that game,
        /// rather than shown greyed.
        /// </summary>
        public bool AppliesTo(
            GameRef game)
        {
            return game != null &&
                   !string.IsNullOrEmpty(game.InstallPath) &&
                   Directory.Exists(game.InstallPath);
        }

        /// <summary>
        /// Runs on the user interface thread, so anything slow needs
        /// a thread of your own or the launcher will appear to hang.
        /// </summary>
        public void Invoke(
            GameRef game)
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(game.InstallPath)
                {
                    UseShellExecute = true
                });
        }
    }

    //------------------------------------------------------------------
    // An example side panel tile
    //------------------------------------------------------------------

    internal class ClockWidget : ISidePanelWidget
    {
        public string Id
        {
            get { return "yourname.myplugin.clock"; }
        }

        public string Title
        {
            get { return "My Plugin"; }
        }

        public TimeSpan RefreshInterval
        {
            get { return TimeSpan.FromSeconds(30); }
        }

        /// <summary>
        /// Called on a background thread, so it may take a moment,
        /// but it must not touch the user interface.
        /// </summary>
        public IEnumerable<string> GetLines()
        {
            return new[] { "The time is " + DateTime.Now.ToString("HH:mm") };
        }
    }

    //------------------------------------------------------------------
    // An example library source. Uncomment the registration above to
    // use it.
    //------------------------------------------------------------------

    internal class MySource : ILibrarySource
    {
        public string Id
        {
            get { return "mysource"; }
        }

        public string DisplayName
        {
            get { return "My Source"; }
        }

        /// <summary>
        /// Return false and Nexus leaves the heading out entirely,
        /// rather than showing an empty one.
        /// </summary>
        public bool IsInstalled()
        {
            return false;
        }

        /// <summary>
        /// Called on a background thread. Throwing is safe: it is
        /// logged and the rest of the library still loads.
        /// </summary>
        public IEnumerable<PluginGame> Scan()
        {
            return new[]
            {
                new PluginGame
                {
                    Name = "An Example Game",
                    InstallPath = @"C:\Games\Example",
                    ExecutablePath = @"C:\Games\Example\game.exe"
                }
            };
        }
    }
}
