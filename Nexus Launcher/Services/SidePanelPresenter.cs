using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus_Launcher.Services.Library
{
    /// <summary>
    /// Turns the tracked data into the text shown in the three side
    /// panel boxes. Kept out of MainView so the formatting is in one
    /// place and the form just assigns the result to a label.
    /// </summary>
    internal static class SidePanelPresenter
    {
        public const int RecentlyPlayedCount = 6;

        //--------------------------------------------------------------
        // Box 1, top: selected game
        //--------------------------------------------------------------

        public static string BuildGameInfo(
            GameInfo game)
        {
            if (game == null)
                return null;

            GamePlayStats stats =
                PlayTrackingService.GetStats(game);

            StringBuilder text =
                new StringBuilder();

            text.AppendLine(game.Name);
            text.AppendLine();

            text.AppendLine(
                "Launcher: " +
                (string.IsNullOrWhiteSpace(game.Launcher)
                    ? "Unknown"
                    : game.Launcher));

            text.AppendLine(
                "Last played: " +
                (stats != null && stats.LastPlayedUtc.HasValue
                    ? FormatRelative(stats.LastPlayedUtc.Value)
                    : "Never"));

            text.AppendLine(
                "Play time: " +
                FormatPlayTime(stats));

            text.AppendLine(
                "Launches: " +
                (stats != null
                    ? stats.LaunchCount.ToString()
                    : "0"));

            if (LibraryOrganizationService.IsFavorite(game))
            {
                text.AppendLine("Favourite: Yes");
            }

            LibraryGroup group =
                LibraryOrganizationService.GetGroupFor(game);

            if (group != null)
                text.AppendLine("Group: " + group.Name);

            return text.ToString().TrimEnd();
        }

        /// <summary>
        /// Play time is measured by watching the game's process, so it
        /// is flagged as approximate once a session has been missed.
        /// </summary>
        private static string FormatPlayTime(
            GamePlayStats stats)
        {
            if (stats == null || stats.TotalPlaySeconds <= 0)
            {
                return stats != null && stats.UntrackedSessions > 0
                    ? "Not measured"
                    : "None";
            }

            string formatted =
                FormatDuration(
                    TimeSpan.FromSeconds(
                        stats.TotalPlaySeconds));

            return stats.UntrackedSessions > 0
                ? formatted + " (approx)"
                : formatted;
        }

        //--------------------------------------------------------------
        // Box 1, fallback: selected launcher
        //--------------------------------------------------------------

        public static string BuildLauncherSummary(
            string launcher,
            IList<GameInfo> games,
            DateTime? lastScan)
        {
            if (string.IsNullOrWhiteSpace(launcher))
                return "Select a launcher or a game.";

            StringBuilder text =
                new StringBuilder();

            text.AppendLine(launcher);
            text.AppendLine();

            int total =
                games != null
                    ? games.Count
                    : 0;

            text.AppendLine("Games: " + total);

            if (games != null && total > 0)
            {
                int favorites = 0;
                int played = 0;
                long seconds = 0;

                foreach (GameInfo game in games)
                {
                    if (LibraryOrganizationService.IsFavorite(game))
                        favorites++;

                    GamePlayStats stats =
                        PlayTrackingService.GetStats(game);

                    if (stats == null)
                        continue;

                    if (stats.LastPlayedUtc.HasValue)
                        played++;

                    seconds += stats.TotalPlaySeconds;
                }

                text.AppendLine("Favourites: " + favorites);
                text.AppendLine("Played: " + played);

                if (seconds > 0)
                {
                    text.AppendLine(
                        "Total play time: " +
                        FormatDuration(
                            TimeSpan.FromSeconds(seconds)));
                }
            }

            text.AppendLine();

            text.AppendLine(
                "Last scan: " +
                (lastScan.HasValue
                    ? FormatRelative(lastScan.Value)
                    : "Not scanned"));

            return text.ToString().TrimEnd();
        }

        //--------------------------------------------------------------
        // Box 2
        //--------------------------------------------------------------

        public static string BuildRecentlyPlayed()
        {
            List<GamePlayStats> recent =
                PlayTrackingService.GetRecentlyPlayed(
                    RecentlyPlayedCount);

            if (recent.Count == 0)
                return "Nothing played yet.";

            StringBuilder text =
                new StringBuilder();

            foreach (GamePlayStats entry in recent)
            {
                text.AppendLine(entry.Name);

                string line =
                    FormatRelative(
                        entry.LastPlayedUtc.Value);

                if (entry.TotalPlaySeconds > 0)
                {
                    line +=
                        "  -  " +
                        FormatDuration(
                            TimeSpan.FromSeconds(
                                entry.TotalPlaySeconds));
                }

                text.AppendLine(line);
                text.AppendLine();
            }

            return text.ToString().TrimEnd();
        }

        //--------------------------------------------------------------
        // Box 3
        //--------------------------------------------------------------

        public static string BuildSystemStats(
            SystemStatsService.Snapshot snapshot)
        {
            if (snapshot == null)
                return "Unavailable.";

            StringBuilder text =
                new StringBuilder();

            text.AppendLine(
                "CPU: " +
                snapshot.CpuPercent.ToString("F0") +
                "%");

            text.AppendLine(
                "RAM: " +
                snapshot.RamUsedGb.ToString("F1") +
                " GB / " +
                snapshot.RamTotalGb.ToString("F1") +
                " GB (" +
                snapshot.RamPercent.ToString("F0") +
                "%)");

            if (!string.IsNullOrWhiteSpace(snapshot.DriveLabel))
            {
                text.AppendLine(
                    "Disk " +
                    snapshot.DriveLabel +
                    ": " +
                    snapshot.DriveUsedGb.ToString("F0") +
                    " GB / " +
                    snapshot.DriveTotalGb.ToString("F0") +
                    " GB (" +
                    snapshot.DrivePercent.ToString("F0") +
                    "%)");
            }

            text.AppendLine();

            if (snapshot.BatteryPercent.HasValue)
            {
                text.AppendLine(
                    "Battery: " +
                    snapshot.BatteryPercent.Value +
                    "%" +
                    (snapshot.OnMains
                        ? " (charging)"
                        : string.Empty));
            }
            else
            {
                text.AppendLine("Power: Mains");
            }

            text.AppendLine(
                "Uptime: " +
                FormatDuration(snapshot.Uptime));

            return text.ToString().TrimEnd();
        }

        //--------------------------------------------------------------
        // Formatting
        //--------------------------------------------------------------

        public static string FormatDuration(
            TimeSpan span)
        {
            if (span.TotalMinutes < 1)
                return "Under a minute";

            if (span.TotalHours < 1)
                return (int)span.TotalMinutes + "m";

            if (span.TotalDays < 1)
            {
                return (int)span.TotalHours + "h " +
                    span.Minutes + "m";
            }

            return (int)span.TotalDays + "d " +
                span.Hours + "h";
        }

        /// <summary>
        /// Takes a UTC timestamp and describes it in local terms.
        /// </summary>
        public static string FormatRelative(
            DateTime utc)
        {
            TimeSpan since =
                DateTime.UtcNow - utc;

            if (since < TimeSpan.Zero)
                since = TimeSpan.Zero;

            if (since.TotalMinutes < 2)
                return "Just now";

            if (since.TotalHours < 1)
                return (int)since.TotalMinutes + " minutes ago";

            if (since.TotalHours < 24)
            {
                int hours = (int)since.TotalHours;

                return hours == 1
                    ? "1 hour ago"
                    : hours + " hours ago";
            }

            if (since.TotalDays < 2)
                return "Yesterday";

            if (since.TotalDays < 30)
                return (int)since.TotalDays + " days ago";

            return utc.ToLocalTime().ToShortDateString();
        }
    }
}
