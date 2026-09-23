using Nexus_Launcher.Models;
using Nexus_Launcher.Services.Artwork;
using Nexus_Launcher.Services.Themes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus_Launcher.Services.Library
{
    /// <summary>
    /// A point in time summary of the user's library and how they use
    /// Nexus. Feeds both the Library Statistics panel and the
    /// achievement checks, so the two can never disagree.
    /// </summary>
    internal class LibraryStats
    {
        public int TotalGames { get; set; }

        /// <summary>
        /// Games per launcher, keyed on the accordion display name.
        /// </summary>
        public Dictionary<string, int> GamesPerLauncher { get; set; }

        /// <summary>
        /// Measured play time per launcher, keyed the same way.
        /// </summary>
        public Dictionary<string, long> PlaySecondsPerLauncher { get; set; }

        /// <summary>
        /// Launch count per launcher, keyed the same way.
        /// </summary>
        public Dictionary<string, int> LaunchesPerLauncher { get; set; }

        /// <summary>
        /// Distinct games played per launcher, keyed the same way.
        /// </summary>
        public Dictionary<string, int> GamesPlayedPerLauncher { get; set; }

        public int LaunchersWithGames { get; set; }

        public int SteamGames { get; set; }

        public int NexusEntries { get; set; }

        public long TotalPlaySeconds { get; set; }

        public int TotalLaunches { get; set; }

        public long LongestSessionSeconds { get; set; }

        /// <summary>
        /// Total play time of the single most played game.
        /// </summary>
        public long TopGameSeconds { get; set; }

        /// <summary>
        /// Distinct games launched at least once.
        /// </summary>
        public int GamesPlayed { get; set; }

        /// <summary>
        /// Distinct launchers the user has actually played games from.
        /// </summary>
        public int LaunchersPlayed { get; set; }

        public int Favorites { get; set; }

        public int Groups { get; set; }

        public int CustomArtworkGames { get; set; }

        public int CustomThemes { get; set; }

        public List<GamePlayStats> MostPlayed { get; set; }

        public List<GamePlayStats> RecentlyPlayed { get; set; }

        public LibraryStats()
        {
            GamesPerLauncher =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);

            PlaySecondsPerLauncher =
                new Dictionary<string, long>(
                    StringComparer.OrdinalIgnoreCase);

            LaunchesPerLauncher =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);

            GamesPlayedPerLauncher =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);

            MostPlayed = new List<GamePlayStats>();
            RecentlyPlayed = new List<GamePlayStats>();
        }

        //--------------------------------------------------------------
        // Per launcher lookups
        //
        // A launcher with nothing in it reads as zero rather than
        // throwing, so an achievement for a launcher the user has not
        // installed simply sits at no progress.
        //--------------------------------------------------------------

        public int GamesIn(
            string launcher)
        {
            int value;

            return launcher != null &&
                GamesPerLauncher.TryGetValue(launcher, out value)
                ? value
                : 0;
        }

        public long PlaySecondsIn(
            string launcher)
        {
            long value;

            return launcher != null &&
                PlaySecondsPerLauncher.TryGetValue(launcher, out value)
                ? value
                : 0;
        }

        public int LaunchesIn(
            string launcher)
        {
            int value;

            return launcher != null &&
                LaunchesPerLauncher.TryGetValue(launcher, out value)
                ? value
                : 0;
        }

        public int GamesPlayedIn(
            string launcher)
        {
            int value;

            return launcher != null &&
                GamesPlayedPerLauncher.TryGetValue(launcher, out value)
                ? value
                : 0;
        }
    }

    internal static class LibraryStatsService
    {
        public const string SteamLauncher = "Steam";

        public const string NexusLauncher = "Nexus Launcher";

        public const string EpicLauncher = "Epic Games";

        public const string BattleNetLauncher = "Battle.net";

        public const string GogLauncher = "GOG";

        public const string EaLauncher = "EA App";

        public const string UbisoftLauncher = "Ubisoft Connect";

        public const string XboxLauncher = "Xbox";

        /// <summary>
        /// The game launchers Nexus scans, spelled exactly as MainView
        /// names them when it arranges the accordion. Everything that
        /// counts games per launcher keys off this list, so a launcher
        /// renamed in one place cannot quietly stop matching in
        /// another.
        ///
        /// Nexus Launcher itself is deliberately not here: its entries
        /// are programs the user added, counted as NexusEntries.
        /// </summary>
        public static readonly IReadOnlyList<string> GameLaunchers =
            new List<string>
            {
                SteamLauncher,
                EpicLauncher,
                BattleNetLauncher,
                GogLauncher,
                EaLauncher,
                UbisoftLauncher,
                XboxLauncher
            };

        /// <summary>
        /// Builds a fresh snapshot. Reads the accordion, so call it on
        /// the UI thread.
        /// </summary>
        public static LibraryStats Build()
        {
            LibraryStats stats =
                new LibraryStats();

            try
            {
                foreach (KeyValuePair<string, List<GameInfo>> launcher in
                    AccordionLibraryOrganizer.GetAllLaunchers())
                {
                    int count =
                        launcher.Value.Count;

                    stats.GamesPerLauncher[launcher.Key] = count;
                    stats.TotalGames += count;
                }

                stats.LaunchersWithGames =
                    stats.GamesPerLauncher.Count(x => x.Value > 0);

                stats.SteamGames =
                    CountFor(stats, SteamLauncher);

                stats.NexusEntries =
                    CountFor(stats, NexusLauncher);

                List<GamePlayStats> played =
                    PlayTrackingService.GetAllStats()
                        .Where(x => x.LaunchCount > 0)
                        .ToList();

                stats.GamesPlayed = played.Count;

                stats.TotalLaunches =
                    played.Sum(x => x.LaunchCount);

                stats.TotalPlaySeconds =
                    played.Sum(x => x.TotalPlaySeconds);

                stats.LongestSessionSeconds =
                    played.Count == 0
                        ? 0
                        : played.Max(x => x.LongestSessionSeconds);

                stats.TopGameSeconds =
                    played.Count == 0
                        ? 0
                        : played.Max(x => x.TotalPlaySeconds);

                stats.LaunchersPlayed =
                    played
                        .Select(x => x.Launcher)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count();

                foreach (IGrouping<string, GamePlayStats> group in
                    played
                        .Where(x => !string.IsNullOrWhiteSpace(x.Launcher))
                        .GroupBy(
                            x => x.Launcher,
                            StringComparer.OrdinalIgnoreCase))
                {
                    stats.PlaySecondsPerLauncher[group.Key] =
                        group.Sum(x => x.TotalPlaySeconds);

                    stats.LaunchesPerLauncher[group.Key] =
                        group.Sum(x => x.LaunchCount);

                    stats.GamesPlayedPerLauncher[group.Key] =
                        group.Count();
                }

                stats.Favorites =
                    LibraryOrganizationService.GetFavoriteCount();

                stats.Groups =
                    LibraryOrganizationService.GetAllGroups().Count;

                stats.CustomArtworkGames =
                    CustomArtworkService.CountGamesWithCustomArtwork();

                stats.CustomThemes =
                    CustomThemeService.Themes.Count;

                stats.MostPlayed =
                    PlayTrackingService.GetMostPlayed(5);

                stats.RecentlyPlayed =
                    PlayTrackingService.GetRecentlyPlayed(5);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            return stats;
        }

        private static int CountFor(
            LibraryStats stats,
            string launcher)
        {
            int count;

            return stats.GamesPerLauncher.TryGetValue(
                launcher,
                out count)
                ? count
                : 0;
        }
    }
}
