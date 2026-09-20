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
        /// Waits for the game's process to show up, times it, then
        /// records the session.
        /// </summary>
        private static async Task WatchSessionAsync(
            GameInfo game,
            string key)
        {
            try
            {
                string installPath =
                    game.InstallPath;

                if (string.IsNullOrWhiteSpace(installPath) ||
                    !Directory.Exists(installPath))
                {
                    RecordUntracked(key);

                    return;
                }

                Process target =
                    await WaitForProcessAsync(installPath);

                if (target == null)
                {
                    RecordUntracked(key);

                    return;
                }

                DateTime started =
                    DateTime.UtcNow;

                await WaitForExitAsync(target);

                TimeSpan elapsed =
                    DateTime.UtcNow - started;

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
                        entry.TotalPlaySeconds +=
                            (long)elapsed.TotalSeconds;
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

        private static async Task<Process> WaitForProcessAsync(
            string installPath)
        {
            DateTime deadline =
                DateTime.UtcNow + StartupGracePeriod;

            while (DateTime.UtcNow < deadline)
            {
                Process found =
                    FindProcessUnder(installPath);

                if (found != null)
                    return found;

                await Task.Delay(PollInterval);
            }

            return null;
        }

        private static async Task WaitForExitAsync(
            Process process)
        {
            while (true)
            {
                try
                {
                    if (process.HasExited)
                        return;
                }
                catch
                {
                    // Handle went away, treat it as exited.
                    return;
                }

                await Task.Delay(PollInterval);
            }
        }

        /// <summary>
        /// Finds a running process whose executable sits inside the
        /// game's install folder.
        /// </summary>
        private static Process FindProcessUnder(
            string installPath)
        {
            string root =
                installPath.TrimEnd(
                    Path.DirectorySeparatorChar) +
                Path.DirectorySeparatorChar;

            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    string path =
                        process.MainModule.FileName;

                    if (!string.IsNullOrEmpty(path) &&
                        path.StartsWith(
                            root,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return process;
                    }
                }
                catch
                {
                    // Protected or 32/64 bit mismatch, nothing we can
                    // do about those so just skip them.
                }
            }

            return null;
        }
    }
}
