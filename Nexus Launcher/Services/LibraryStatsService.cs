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

            MostPlayed = new List<GamePlayStats>();
            RecentlyPlayed = new List<GamePlayStats>();
        }
    }

    internal static class LibraryStatsService
    {
        public const string SteamLauncher = "Steam";

        public const string NexusLauncher = "Nexus Launcher";

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
