using Microsoft.Win32;
using Nexus.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nexus.Plugin.ExtraLaunchers
{
    //==================================================================
    // The three launchers Nexus shows a heading for but has never had
    // a scanner for: Amazon Games, Paradox and Wargaming. Until now
    // those headings sat empty for anyone who had the clients
    // installed.
    //
    // One plugin, three sources, to show that a plugin is not limited
    // to contributing one thing.
    //==================================================================

    [NexusPlugin(
        "nexus.extralaunchers",
        "Extra Launchers",
        Author = "Nexus Launcher",
        Version = "1.0.0",
        Description = "Finds games from Amazon Games, the Paradox " +
                      "launcher and Wargaming Game Center.")]
    public class ExtraLaunchersPlugin : INexusPlugin
    {
        public void Initialize(
            INexusHost host)
        {
            host.AddLibrarySource(new WargamingSource(host));
            host.AddLibrarySource(new AmazonSource(host));
            host.AddLibrarySource(new ParadoxSource(host));
        }

        public void Shutdown()
        {
        }
    }

    //------------------------------------------------------------------
    // Shared helpers
    //------------------------------------------------------------------

    internal static class Uninstall
    {
        private static readonly string[] Roots =
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        internal class Entry
        {
            public string Name;
            public string Publisher;
            public string InstallLocation;
            public string Icon;
        }

        /// <summary>
        /// Everything Windows knows is installed, from both the
        /// machine and the current user, in both registry views.
        /// </summary>
        public static IEnumerable<Entry> All()
        {
            List<Entry> found = new List<Entry>();

            foreach (RegistryKey hive in
                     new[] { Registry.LocalMachine, Registry.CurrentUser })
            {
                foreach (string root in Roots)
                {
                    Read(hive, root, found);
                }
            }

            return found;
        }

        private static void Read(
            RegistryKey hive,
            string root,
            List<Entry> into)
        {
            try
            {
                using (RegistryKey key = hive.OpenSubKey(root))
                {
                    if (key == null)
                        return;

                    foreach (string name in key.GetSubKeyNames())
                    {
                        using (RegistryKey sub = key.OpenSubKey(name))
                        {
                            if (sub == null)
                                continue;

                            into.Add(new Entry
                            {
                                Name = sub.GetValue("DisplayName") as string,
                                Publisher = sub.GetValue("Publisher") as string,
                                InstallLocation =
                                    sub.GetValue("InstallLocation") as string,
                                Icon = sub.GetValue("DisplayIcon") as string
                            });
                        }
                    }
                }
            }
            catch
            {
                // A key we are not allowed to read is not worth
                // failing the whole scan over.
            }
        }

        /// <summary>
        /// Strips the ",0" index off a DisplayIcon and checks the file
        /// is really there.
        /// </summary>
        public static string IconFile(
            string displayIcon)
        {
            if (string.IsNullOrWhiteSpace(displayIcon))
                return null;

            string path = displayIcon.Trim();

            int comma = path.LastIndexOf(',');

            if (comma > 2)
                path = path.Substring(0, comma);

            path = path.Trim('"');

            return File.Exists(path) ? path : null;
        }

        /// <summary>
        /// A game sitting in a Steam library belongs to Steam, whoever
        /// published it. Without this, a Paradox game bought on Steam
        /// would be listed twice: once by Nexus's Steam scanner and
        /// again by this plugin.
        /// </summary>
        public static bool IsAnotherStoresCopy(
            string installLocation)
        {
            if (string.IsNullOrWhiteSpace(installLocation))
                return false;

            string lower = installLocation.ToLowerInvariant();

            return lower.Contains(@"\steamapps\") ||
                   lower.Contains(@"\gog galaxy\") ||
                   lower.Contains(@"\epic games\") ||
                   lower.Contains(@"\origin games\") ||
                   lower.Contains(@"\ea games\");
        }

        /// <summary>
        /// Things that are never the game, whatever their size.
        ///
        /// This list exists because "the biggest exe in the folder"
        /// is a poor guess: a Unity game's crash handler is often
        /// larger than the game, which is how Amazon games ended up
        /// pointing at UnityCrashHandler64.exe.
        /// </summary>
        private static readonly string[] NeverTheGame =
        {
            "unins", "uninstall", "crashhandler", "crashpad",
            "crashreport", "unitycrash", "vcredist", "vc_redist",
            "dxsetup", "directx", "dotnetfx", "dotnet", "xnafx",
            "setup", "installer", "launcher", "wgc_api", "redist",
            "helper", "update", "oalinst", "physx"
        };

        /// <summary>
        /// Whether a path is obviously not a game.
        /// </summary>
        public static bool LooksLikePlumbing(
            string path)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            string name = Path.GetFileName(path);

            return NeverTheGame.Any(i =>
                name.IndexOf(i, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// A guess at the game executable in a folder.
        ///
        /// Only a guess. Prefer whatever the launcher itself records,
        /// as the Amazon source does with fuel.json; fall back to
        /// this when there is nothing better.
        /// </summary>
        public static string BestExe(
            string folder,
            params string[] ignore)
        {
            try
            {
                if (!Directory.Exists(folder))
                    return null;

                List<FileInfo> files = new DirectoryInfo(folder)
                    .GetFiles("*.exe")
                    .Where(f => !NeverTheGame.Any(i =>
                        f.Name.IndexOf(i, StringComparison.OrdinalIgnoreCase) >= 0))
                    .Where(f => !ignore.Any(i =>
                        f.Name.IndexOf(i, StringComparison.OrdinalIgnoreCase) >= 0))
                    .OrderByDescending(f => f.Length)
                    .ToList();

                return files.Count == 0 ? null : files[0].FullName;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Pulls one string value out of a small JSON object.
        ///
        /// A whole parser would be a dependency for one field, but
        /// this has to be told where to start: Amazon's fuel.json
        /// lists PostInstall before Main, both have a "Command", and
        /// taking the first match picked the redistributable
        /// installer instead of the game.
        /// </summary>
        public static string JsonValue(
            string json,
            string key,
            string afterSection = null)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            int from = 0;

            if (afterSection != null)
            {
                from = json.IndexOf("\"" + afterSection + "\"",
                    StringComparison.OrdinalIgnoreCase);

                if (from < 0)
                    return null;
            }

            int at = json.IndexOf("\"" + key + "\"", from,
                StringComparison.OrdinalIgnoreCase);

            if (at < 0)
                return null;

            int colon = json.IndexOf(':', at);

            if (colon < 0)
                return null;

            int open = json.IndexOf('"', colon);

            if (open < 0)
                return null;

            int close = json.IndexOf('"', open + 1);

            return close < 0
                ? null
                : json.Substring(open + 1, close - open - 1).Trim();
        }
    }

    //------------------------------------------------------------------
    // Wargaming Game Center
    //------------------------------------------------------------------

    /// <summary>
    /// Wargaming registers each game under its own uninstall entry
    /// with the install folder on it, which is far more reliable than
    /// reading the Game Center's own files.
    /// </summary>
    internal class WargamingSource : ILibrarySource
    {
        private readonly INexusHost host;

        public WargamingSource(
            INexusHost host)
        {
            this.host = host;
        }

        public string Id
        {
            get { return "wargaming"; }
        }

        public string DisplayName
        {
            get { return "Wargaming"; }
        }

        private static string GameCenterFolder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.CommonApplicationData),
                    "Wargaming.net",
                    "GameCenter");
            }
        }

        public bool IsInstalled()
        {
            return Directory.Exists(GameCenterFolder);
        }

        public IEnumerable<PluginGame> Scan()
        {
            List<PluginGame> games = new List<PluginGame>();

            foreach (Uninstall.Entry entry in Uninstall.All())
            {
                if (!string.Equals(entry.Publisher, "Wargaming.net",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.InstallLocation) ||
                    !Directory.Exists(entry.InstallLocation))
                {
                    // The Game Center itself has no install location,
                    // which is a tidy way of leaving it out.
                    continue;
                }

                // Wargaming games are not started from their own
                // exe. Game Center's own Start menu shortcuts run
                // "wgc_api.exe --open" from the game folder, which
                // brings Game Center up if it is not running and
                // then starts the game. Running WorldOfWarships.exe
                // directly simply does nothing.
                string helper = Path.Combine(
                    entry.InstallLocation, "wgc_api.exe");

                bool viaGameCentre = File.Exists(helper);

                games.Add(new PluginGame
                {
                    Name = Tidy(entry.Name),
                    SourceGameId = ReadGameId(entry.InstallLocation)
                                   ?? entry.Name,
                    InstallPath = entry.InstallLocation,
                    ExecutablePath = viaGameCentre
                        ? helper
                        : Uninstall.BestExe(entry.InstallLocation),
                    Arguments = viaGameCentre ? "--open" : null,
                    IconPath = Uninstall.IconFile(entry.Icon)
                });
            }

            host.Log.Info("Wargaming: " + games.Count + " game(s)");

            return games;
        }

        /// <summary>
        /// Game Center's own id for the game, such as
        /// "WOWS.WW.PRODUCTION", out of the game_info.xml it keeps in
        /// the install folder. Better than the display name for
        /// telling two installs apart.
        /// </summary>
        private static string ReadGameId(
            string folder)
        {
            try
            {
                string file = Path.Combine(folder, "game_info.xml");

                if (!File.Exists(file))
                    return null;

                string text = File.ReadAllText(file);

                int open = text.IndexOf("<id>", StringComparison.OrdinalIgnoreCase);

                if (open < 0)
                    return null;

                int close = text.IndexOf("</id>", open,
                    StringComparison.OrdinalIgnoreCase);

                return close < 0
                    ? null
                    : text.Substring(open + 4, close - open - 4).Trim();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Wargaming writes names like "World_of_Warships". Shown as
        /// is they look like folder names rather than games.
        /// </summary>
        private static string Tidy(
            string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Unknown";

            return name.Replace('_', ' ').Trim();
        }
    }

    //------------------------------------------------------------------
    // Amazon Games
    //------------------------------------------------------------------

    /// <summary>
    /// Amazon keeps its library list in a SQLite database. Rather
    /// than take a native database dependency for one list, this
    /// looks for the games on disk: every Amazon game folder contains
    /// a fuel.json, which makes it unambiguous that the folder is one
    /// of theirs.
    /// </summary>
    internal class AmazonSource : ILibrarySource
    {
        private readonly INexusHost host;

        public AmazonSource(
            INexusHost host)
        {
            this.host = host;
        }

        public string Id
        {
            get { return "amazon"; }
        }

        public string DisplayName
        {
            get { return "Amazon Games"; }
        }

        private static string ClientFolder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "Amazon Games");
            }
        }

        public bool IsInstalled()
        {
            return Directory.Exists(ClientFolder);
        }

        /// <summary>
        /// Where Amazon puts games. The default is a Library folder at
        /// the root of a drive, and the user can move it, so every
        /// fixed drive is checked rather than assuming C.
        /// </summary>
        private static IEnumerable<string> LibraryRoots()
        {
            List<string> roots = new List<string>();

            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
                        continue;

                    roots.Add(Path.Combine(
                        drive.RootDirectory.FullName,
                        "Amazon Games", "Library"));
                }
            }
            catch
            {
            }

            return roots;
        }

        public IEnumerable<PluginGame> Scan()
        {
            List<PluginGame> games = new List<PluginGame>();

            foreach (string root in LibraryRoots())
            {
                if (!Directory.Exists(root))
                    continue;

                string[] folders;

                try
                {
                    folders = Directory.GetDirectories(root);
                }
                catch
                {
                    continue;
                }

                foreach (string folder in folders)
                {
                    string fuel = Path.Combine(folder, "fuel.json");

                    // The marker that says this really is an Amazon
                    // game rather than something else in the folder.
                    if (!File.Exists(fuel))
                        continue;

                    games.Add(new PluginGame
                    {
                        Name = Path.GetFileName(folder),
                        SourceGameId = Path.GetFileName(folder),
                        InstallPath = folder,
                        ExecutablePath = ExeFromFuel(folder, fuel)
                    });
                }
            }

            host.Log.Info("Amazon: " + games.Count + " game(s)");

            return games;
        }

        /// <summary>
        /// The executable Amazon itself records, from the game's
        /// fuel.json: "Main": { "Command": "The Game.exe" }.
        ///
        /// Guessing was wrong here. Unity games ship a crash handler
        /// that is larger than the game, so picking the biggest exe
        /// chose UnityCrashHandler64.exe every time.
        /// </summary>
        private string ExeFromFuel(
            string folder,
            string fuelFile)
        {
            try
            {
                // Scoped to Main: PostInstall comes first in the
                // file and its Command is a redistributable.
                string command = Uninstall.JsonValue(
                    File.ReadAllText(fuelFile), "Command", "Main");

                if (!string.IsNullOrWhiteSpace(command))
                {
                    // Amazon writes these with backslashes.
                    string full = Path.Combine(
                        folder, command.Replace("/", "\\"));

                    if (File.Exists(full) && !Uninstall.LooksLikePlumbing(full))
                        return full;

                    host.Log.Warn("fuel.json names " + command +
                        " which is missing or is not a game");
                }
            }
            catch (Exception ex)
            {
                host.Log.Error("could not read " + fuelFile, ex);
            }

            return Uninstall.BestExe(folder);
        }
    }

    //------------------------------------------------------------------
    // Paradox
    //------------------------------------------------------------------

    /// <summary>
    /// Paradox games installed through the Paradox launcher.
    ///
    /// The publisher alone is not enough to go on: most Paradox games
    /// are bought on Steam, and those are Steam's to list. Anything
    /// sitting in another store's folders is left alone.
    /// </summary>
    internal class ParadoxSource : ILibrarySource
    {
        private readonly INexusHost host;

        public ParadoxSource(
            INexusHost host)
        {
            this.host = host;
        }

        public string Id
        {
            get { return "paradox"; }
        }

        public string DisplayName
        {
            get { return "Paradox"; }
        }

        private static string LauncherFolder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "Paradox Interactive");
            }
        }

        public bool IsInstalled()
        {
            return Directory.Exists(LauncherFolder);
        }

        public IEnumerable<PluginGame> Scan()
        {
            List<PluginGame> games = new List<PluginGame>();

            foreach (Uninstall.Entry entry in Uninstall.All())
            {
                if (string.IsNullOrWhiteSpace(entry.Publisher) ||
                    entry.Publisher.IndexOf("Paradox",
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.InstallLocation) ||
                    !Directory.Exists(entry.InstallLocation))
                {
                    // Catches the Paradox launcher itself, which
                    // records no install location.
                    continue;
                }

                if (Uninstall.IsAnotherStoresCopy(entry.InstallLocation))
                {
                    host.Log.Info(
                        "Paradox: skipping " + entry.Name +
                        ", it belongs to another store");

                    continue;
                }

                if (!string.IsNullOrWhiteSpace(entry.Name) &&
                    entry.Name.IndexOf("Launcher",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                games.Add(new PluginGame
                {
                    Name = entry.Name,
                    SourceGameId = entry.Name,
                    InstallPath = entry.InstallLocation,
                    ExecutablePath = Uninstall.BestExe(
                        entry.InstallLocation, "unins", "launcher", "crash"),
                    IconPath = Uninstall.IconFile(entry.Icon)
                });
            }

            host.Log.Info("Paradox: " + games.Count + " game(s)");

            return games;
        }
    }
}
