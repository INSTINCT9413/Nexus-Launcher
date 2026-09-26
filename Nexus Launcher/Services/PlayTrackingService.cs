using Newtonsoft.Json;
using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Nexus_Launcher.Services.Library
{
    /// <summary>
    /// Records when games are launched and, where it can, how long
    /// they ran for.
    ///
    /// Measuring duration is awkward because most launches go out as a
    /// URI to the launcher client, so the process we start is Steam or
    /// the Epic launcher rather than the game. Instead of timing that,
    /// we wait for a process to appear underneath the game's install
    /// folder and time that one. It works for the common case and
    /// quietly gives up when it cannot find anything, which is why a
    /// session can be counted as untracked.
    /// </summary>
    internal static class PlayTrackingService
    {
        private static readonly string SaveFile =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "NexusLauncher",
                "PlayStats.json");

        /// <summary>
        /// How long to keep looking for the game's process after the
        /// launch before giving up on timing the session.
        /// </summary>
        private static readonly TimeSpan StartupGracePeriod =
            TimeSpan.FromMinutes(3);

        private static readonly TimeSpan PollInterval =
            TimeSpan.FromSeconds(5);

        /// <summary>
        /// Anything shorter than this is treated as a false match
        /// rather than a real session.
        /// </summary>
        private static readonly TimeSpan MinimumSession =
            TimeSpan.FromSeconds(30);

        /// <summary>
        /// How many consecutive polls have to come back empty before
        /// the session is called over.
        ///
        /// One empty poll is not enough. Several launchers start a
        /// small bootstrapper first, which exits as soon as it has
        /// handed off to the real game, and Xbox titles in particular
        /// go through a gamelaunchhelper.exe that is gone within
        /// seconds. Ending on the first gap timed the bootstrapper
        /// instead of the game and then threw the session away for
        /// being too short.
        /// </summary>
        private const int EmptyPollsBeforeEnd = 3;

        private static readonly object sync =
            new object();

        private static Dictionary<string, GamePlayStats> stats;

        /// <summary>
        /// Games currently being timed, so a second Launch press does
        /// not start a duplicate watcher.
        /// </summary>
        private static readonly HashSet<string> watching =
            new HashSet<string>();

        public static event Action Changed;

        /// <summary>
        /// Raised the moment a launch is recorded, with the local time
        /// it happened. Changed does not say what changed, and some
        /// achievements care about when a game was started.
        /// </summary>
        public static event Action<GameInfo, DateTime> GameLaunched;

        //--------------------------------------------------------------
        // Storage
        //--------------------------------------------------------------

        private static Dictionary<string, GamePlayStats> Stats
        {
            get
            {
                lock (sync)
                {
                    if (stats == null)
                        stats = LoadFromDisk();

                    return stats;
                }
            }
        }

        private static Dictionary<string, GamePlayStats> LoadFromDisk()
        {
            try
            {
                if (!File.Exists(SaveFile))
                    return new Dictionary<string, GamePlayStats>();

                List<GamePlayStats> loaded =
                    JsonConvert.DeserializeObject<List<GamePlayStats>>(
                        File.ReadAllText(SaveFile));

                Dictionary<string, GamePlayStats> result =
                    new Dictionary<string, GamePlayStats>();

                if (loaded == null)
                    return result;

                foreach (GamePlayStats entry in loaded)
                {
                    if (entry == null ||
                        string.IsNullOrWhiteSpace(entry.Key))
                    {
                        continue;
                    }

                    result[entry.Key] = entry;
                }

                return result;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return new Dictionary<string, GamePlayStats>();
            }
        }

        private static void Save()
        {
            try
            {
                string folder =
                    Path.GetDirectoryName(SaveFile);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                List<GamePlayStats> snapshot;

                lock (sync)
                {
                    snapshot =
                        Stats.Values.ToList();
                }

                File.WriteAllText(
                    SaveFile,
                    JsonConvert.SerializeObject(
                        snapshot,
                        Formatting.Indented));
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            Changed?.Invoke();
        }

        //--------------------------------------------------------------
        // Reading
        //--------------------------------------------------------------

        public static GamePlayStats GetStats(
            GameInfo game)
        {
            string key =
                LibraryOrganizationService.GetKey(game);

            if (key == null)
                return null;

            lock (sync)
            {
                GamePlayStats found;

                return Stats.TryGetValue(
                    key,
                    out found)
                    ? found
                    : null;
            }
        }

        /// <summary>
        /// A copy of every tracked game, for library wide totals.
        /// </summary>
        public static List<GamePlayStats> GetAllStats()
        {
            lock (sync)
            {
                return Stats.Values.ToList();
            }
        }

        /// <summary>
        /// Most played first, by measured play time.
        /// </summary>
        public static List<GamePlayStats> GetMostPlayed(
            int count)
        {
            lock (sync)
            {
                return Stats.Values
                    .Where(x => x.TotalPlaySeconds > 0)
                    .OrderByDescending(x => x.TotalPlaySeconds)
                    .Take(count)
                    .ToList();
            }
        }

        /// <summary>
        /// Most recently played first.
        /// </summary>
        public static List<GamePlayStats> GetRecentlyPlayed(
            int count)
        {
            lock (sync)
            {
                return Stats.Values
                    .Where(x => x.LastPlayedUtc.HasValue)
                    .OrderByDescending(x => x.LastPlayedUtc.Value)
                    .Take(count)
                    .ToList();
            }
        }

        //--------------------------------------------------------------
        // Recording
        //--------------------------------------------------------------

        /// <summary>
        /// Call this the moment a launch is triggered. The timestamp
        /// and count are exact, the duration is best effort.
        /// </summary>
        public static void RecordLaunch(
            GameInfo game)
        {
            string key =
                LibraryOrganizationService.GetKey(game);

            if (key == null)
                return;

            lock (sync)
            {
                GamePlayStats entry =
                    GetOrCreate(key, game);

                entry.LastPlayedUtc = DateTime.UtcNow;
                entry.LaunchCount++;
            }

            Save();

            GameLaunched?.Invoke(
                game,
                DateTime.Now);

            StartWatching(
                game,
                key);
        }

        private static GamePlayStats GetOrCreate(
            string key,
            GameInfo game)
        {
            GamePlayStats entry;

            if (!Stats.TryGetValue(
                key,
                out entry))
            {
                entry = new GamePlayStats();
                entry.Key = key;

                Stats[key] = entry;
            }

            // Refreshed each time so a renamed game keeps a sane label.
            entry.Name = game.Name;
            entry.Launcher = game.Launcher;

            return entry;
        }

        private static void StartWatching(
            GameInfo game,
            string key)
        {
            lock (sync)
            {
                if (watching.Contains(key))
                    return;

                watching.Add(key);
            }

            Task.Run(() => WatchSessionAsync(
                game,
                key));
        }

        /// <summary>
        /// Times a session by watching for any process belonging to the
        /// game, from the first one that appears to the last one that
        /// exits.
        ///
        /// Deliberately not "find a process and wait for it to exit".
        /// Games routinely start one process and continue in another:
        /// a bootstrapper hands off to the real executable, launchers
        /// re-exec through a helper, anti-cheat wrappers relaunch the
        /// game. Following a single handle timed whichever process
        /// happened to be found first, which for Xbox titles was a
        /// helper that exits within seconds.
        /// </summary>
        private static async Task WatchSessionAsync(
            GameInfo game,
            string key)
        {
            try
            {
                List<string> roots =
                    BuildRoots(game);

                string exeName =
                    SafeFileName(game.ExecutablePath);

                if (roots.Count == 0 && exeName == null)
                {
                    RecordUntracked(key);

                    return;
                }

                DateTime deadline =
                    DateTime.UtcNow + StartupGracePeriod;

                DateTime? firstSeen = null;

                DateTime lastSeen = DateTime.UtcNow;

                int emptyPolls = 0;

                while (true)
                {
                    bool running =
                        IsGameRunning(roots, exeName);

                    if (running)
                    {
                        if (firstSeen == null)
                            firstSeen = DateTime.UtcNow;

                        lastSeen = DateTime.UtcNow;

                        emptyPolls = 0;
                    }
                    else if (firstSeen != null)
                    {
                        emptyPolls++;

                        if (emptyPolls >= EmptyPollsBeforeEnd)
                            break;
                    }
                    else if (DateTime.UtcNow > deadline)
                    {
                        // Never showed up at all.
                        RecordUntracked(key);

                        return;
                    }

                    await Task.Delay(PollInterval);
                }

                TimeSpan elapsed =
                    lastSeen - firstSeen.Value;

                if (elapsed < MinimumSession)
                {
                    RecordUntracked(key);

                    return;
                }

                lock (sync)
                {
                    GamePlayStats entry;

                    if (Stats.TryGetValue(
                        key,
                        out entry))
                    {
                        entry.LastSessionSeconds =
                            (long)elapsed.TotalSeconds;

                        entry.TotalPlaySeconds +=
                            entry.LastSessionSeconds;

                        if (entry.LastSessionSeconds >
                            entry.LongestSessionSeconds)
                        {
                            entry.LongestSessionSeconds =
                                entry.LastSessionSeconds;
                        }
                    }
                }

                Save();
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
            finally
            {
                lock (sync)
                {
                    watching.Remove(key);
                }
            }
        }

        private static void RecordUntracked(
            string key)
        {
            lock (sync)
            {
                GamePlayStats entry;

                if (Stats.TryGetValue(
                    key,
                    out entry))
                {
                    entry.UntrackedSessions++;
                }
            }

            Save();
        }

        /// <summary>
        /// The folders a process has to be running from to count as
        /// this game. The install folder normally, plus the folder the
        /// executable sits in when that is somewhere else entirely,
        /// which happens when a launcher records one and installs to
        /// the other.
        /// </summary>
        private static List<string> BuildRoots(
            GameInfo game)
        {
            List<string> roots =
                new List<string>();

            AddRoot(roots, game.InstallPath);

            try
            {
                if (!string.IsNullOrWhiteSpace(game.ExecutablePath))
                {
                    AddRoot(
                        roots,
                        Path.GetDirectoryName(game.ExecutablePath));
                }
            }
            catch (Exception)
            {
                // A malformed path is simply not a usable root.
            }

            return roots;
        }

        private static void AddRoot(
            List<string> roots,
            string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                if (!Directory.Exists(path))
                    return;

                string root =
                    Path.GetFullPath(path)
                        .TrimEnd(Path.DirectorySeparatorChar) +
                    Path.DirectorySeparatorChar;

                // An empty root would match every process on the
                // machine, since everything starts with "".
                if (root.Length <= 1)
                    return;

                if (!roots.Contains(root, StringComparer.OrdinalIgnoreCase))
                    roots.Add(root);
            }
            catch (Exception)
            {
            }
        }

        private static string SafeFileName(
            string path)
        {
            try
            {
                return string.IsNullOrWhiteSpace(path)
                    ? null
                    : Path.GetFileName(path);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// True while any process belonging to the game is running.
        ///
        /// Matching on the install folder catches the game and all of
        /// its helpers at once. The executable name is a fallback for
        /// games whose files are not where the launcher said they
        /// would be.
        /// </summary>
        private static bool IsGameRunning(
            List<string> roots,
            string exeName)
        {
            Process[] processes;

            try
            {
                processes = Process.GetProcesses();
            }
            catch (Exception)
            {
                return false;
            }

            try
            {
                foreach (Process process in processes)
                {
                    string path = null;

                    try
                    {
                        path = process.MainModule.FileName;
                    }
                    catch
                    {
                        // Protected process, or a 32/64 bit mismatch.
                        // Nothing to be done, so fall back to the name.
                    }

                    if (!string.IsNullOrEmpty(path))
                    {
                        foreach (string root in roots)
                        {
                            if (path.StartsWith(
                                root,
                                StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }

                        continue;
                    }

                    if (exeName == null)
                        continue;

                    // Process names have no extension.
                    string name;

                    try
                    {
                        name = process.ProcessName;
                    }
                    catch
                    {
                        continue;
                    }

                    if (string.Equals(
                        name + ".exe",
                        exeName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            finally
            {
                // GetProcesses hands back live handles; the watcher
                // polls for hours, so they have to go back.
                foreach (Process process in processes)
                {
                    try
                    {
                        process.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return false;
        }
    }
}
