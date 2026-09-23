using Nexus_Launcher.Models;
using Nexus_Launcher.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Nexus_Launcher.Services.Library
{
    /// <summary>
    /// How the Full Library grid is sorted.
    /// </summary>
    public enum LibrarySort
    {
        NameAscending,
        NameDescending,
        RecentlyPlayed,
        MostPlayed,
        Launcher
    }

    /// <summary>
    /// How the Full Library grid is divided into headed sections.
    /// </summary>
    public enum LibraryGrouping
    {
        None,
        Launcher,
        UserGroup,
        Alphabetical
    }

    /// <summary>
    /// Everything the user has narrowed the library down to.
    ///
    /// This is a plain value object with no UI attached so the whole
    /// filter pipeline can be exercised without building a form.
    /// </summary>
    public class LibraryFilter
    {
        /// <summary>
        /// Free text matched against the game name. Null or blank
        /// matches everything.
        /// </summary>
        public string Search { get; set; }

        /// <summary>
        /// A single launcher name, or null for all launchers.
        /// </summary>
        public string Launcher { get; set; }

        /// <summary>
        /// A <see cref="LibraryGroup.Id"/>, or null for all groups.
        /// </summary>
        public string GroupId { get; set; }

        public bool FavoritesOnly { get; set; }

        public bool InstalledOnly { get; set; }

        public LibrarySort Sort { get; set; }

        public LibraryGrouping Grouping { get; set; }

        public LibraryFilter()
        {
            Sort = LibrarySort.NameAscending;
            Grouping = LibraryGrouping.None;
        }

        /// <summary>
        /// True when nothing has been narrowed down, so the caller can
        /// show "Full Library" rather than a filter summary.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(Search) &&
                    Launcher == null &&
                    GroupId == null &&
                    !FavoritesOnly &&
                    !InstalledOnly;
            }
        }

        public LibraryFilter Clone()
        {
            return (LibraryFilter)MemberwiseClone();
        }
    }

    /// <summary>
    /// One game as the grid draws it: the game itself plus the few
    /// facts the tile shows, resolved once per rebuild instead of on
    /// every paint.
    /// </summary>
    public class LibraryEntry
    {
        public GameInfo Game { get; set; }

        public string Key { get; set; }

        public string Name { get; set; }

        public string Launcher { get; set; }

        public bool IsFavorite { get; set; }

        public bool IsInstalled { get; set; }

        public LibraryGroup Group { get; set; }

        public DateTime? LastPlayedUtc { get; set; }

        public long TotalPlaySeconds { get; set; }

        public int LaunchCount { get; set; }

        /// <summary>
        /// Grid artwork, or null when the game has none and the tile
        /// should fall back to a generated placeholder.
        /// </summary>
        public string GridImagePath { get; set; }

        /// <summary>
        /// Thumbnail cache key for this poster, resolved here rather
        /// than while painting: building it stats the artwork file, and
        /// doing that per tile per paint would hit the disk on every
        /// scroll. Artwork changes rebuild the entries, so the key
        /// cannot go stale.
        /// </summary>
        public string ThumbnailKey { get; set; }
    }

    /// <summary>
    /// A headed run of tiles, for example "Steam" or "F".
    /// </summary>
    public class LibrarySection
    {
        public string Title { get; set; }

        /// <summary>
        /// A DevExpress SVG key for the header glyph, when the section
        /// stands for something with an icon (a launcher or a user
        /// group). Null means no glyph.
        /// </summary>
        public string IconKey { get; set; }

        /// <summary>
        /// An image file the user picked for a group icon.
        /// </summary>
        public string IconPath { get; set; }

        public List<LibraryEntry> Entries { get; set; }

        public LibrarySection()
        {
            Entries = new List<LibraryEntry>();
        }
    }

    /// <summary>
    /// Turns the scanned library plus a <see cref="LibraryFilter"/>
    /// into the sections the Full Library grid draws.
    ///
    /// Favourites always sort ahead of everything else inside their
    /// section, which is how the accordion behaves, so the two views
    /// agree with each other.
    /// </summary>
    internal static class LibraryQueryService
    {
        /// <summary>
        /// Snapshots the library, favourites, groups and play stats
        /// once, then filters and sorts in memory. Every lookup the
        /// grid would otherwise repeat per tile is resolved here.
        /// </summary>
        public static List<LibrarySection> Build(
            LibraryFilter filter)
        {
            return Build(filter, Size.Empty);
        }

        public static List<LibrarySection> Build(
            LibraryFilter filter,
            Size thumbnailSize)
        {
            if (filter == null)
                filter = new LibraryFilter();

            List<LibraryEntry> entries =
                BuildEntries(thumbnailSize);

            entries =
                Apply(entries, filter);

            entries =
                Sort(entries, filter.Sort);

            return Section(entries, filter.Grouping);
        }

        /// <summary>
        /// Every game in the library with its favourite, group and
        /// play facts attached.
        /// </summary>
        public static List<LibraryEntry> BuildEntries()
        {
            return BuildEntries(Size.Empty);
        }

        /// <summary>
        /// <paramref name="thumbnailSize"/> is the tile size the grid
        /// is currently drawing at, so each entry can carry a ready
        /// made thumbnail cache key. Pass Size.Empty when the caller
        /// does not draw posters.
        /// </summary>
        public static List<LibraryEntry> BuildEntries(
            Size thumbnailSize)
        {
            // Groups are read once and indexed by key: asking
            // GetGroupFor per game would rescan every group's game
            // list for every game in the library.
            Dictionary<string, LibraryGroup> groupsByKey =
                new Dictionary<string, LibraryGroup>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (LibraryGroup group in
                LibraryOrganizationService.GetAllGroups())
            {
                if (group.Games == null)
                    continue;

                foreach (string key in group.Games)
                {
                    if (key != null && !groupsByKey.ContainsKey(key))
                        groupsByKey[key] = group;
                }
            }

            Dictionary<string, GamePlayStats> statsByKey =
                new Dictionary<string, GamePlayStats>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (GamePlayStats stats in
                PlayTrackingService.GetAllStats())
            {
                if (stats.Key != null)
                    statsByKey[stats.Key] = stats;
            }

            List<LibraryEntry> entries =
                new List<LibraryEntry>();

            foreach (GameInfo game in LibraryService.Games)
            {
                if (game == null || string.IsNullOrWhiteSpace(game.Name))
                    continue;

                string key =
                    LibraryOrganizationService.GetKey(game);

                LibraryEntry entry =
                    new LibraryEntry();

                entry.Game = game;
                entry.Key = key;
                entry.Name = game.Name;
                entry.Launcher = game.Launcher ?? string.Empty;
                entry.IsInstalled = game.IsInstalled;
                entry.GridImagePath = game.GridImagePath;

                if (!thumbnailSize.IsEmpty)
                {
                    entry.ThumbnailKey =
                        LibraryThumbnailCache.GetKey(
                            key,
                            game.GridImagePath,
                            thumbnailSize);
                }

                entry.IsFavorite =
                    LibraryOrganizationService.IsFavorite(game);

                LibraryGroup group;

                if (key != null &&
                    groupsByKey.TryGetValue(key, out group))
                {
                    entry.Group = group;
                }

                GamePlayStats stats;

                if (key != null &&
                    statsByKey.TryGetValue(key, out stats))
                {
                    entry.LastPlayedUtc = stats.LastPlayedUtc;
                    entry.TotalPlaySeconds = stats.TotalPlaySeconds;
                    entry.LaunchCount = stats.LaunchCount;
                }

                entries.Add(entry);
            }

            return entries;
        }

        private static List<LibraryEntry> Apply(
            List<LibraryEntry> entries,
            LibraryFilter filter)
        {
            IEnumerable<LibraryEntry> query = entries;

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string search =
                    filter.Search.Trim();

                query = query.Where(x =>
                    Matches(x.Name, search));
            }

            if (!string.IsNullOrEmpty(filter.Launcher))
            {
                query = query.Where(x => string.Equals(
                    x.Launcher,
                    filter.Launcher,
                    StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(filter.GroupId))
            {
                query = query.Where(x =>
                    x.Group != null &&
                    x.Group.Id == filter.GroupId);
            }

            if (filter.FavoritesOnly)
                query = query.Where(x => x.IsFavorite);

            if (filter.InstalledOnly)
                query = query.Where(x => x.IsInstalled);

            return query.ToList();
        }

        /// <summary>
        /// Case insensitive "contains", plus a match when every search
        /// word appears somewhere in the name, so "dead space" finds
        /// "Dead Space Remastered" and "space dead" finds it too.
        /// </summary>
        private static bool Matches(
            string name,
            string search)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (name.IndexOf(
                    search,
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string[] words =
                search.Split(
                    new[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

            if (words.Length < 2)
                return false;

            return words.All(word =>
                name.IndexOf(
                    word,
                    StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static List<LibraryEntry> Sort(
            List<LibraryEntry> entries,
            LibrarySort sort)
        {
            // Favourites first in every order, so the star means the
            // same thing here as it does in the accordion.
            IOrderedEnumerable<LibraryEntry> query =
                entries.OrderByDescending(x => x.IsFavorite);

            switch (sort)
            {
                case LibrarySort.NameDescending:
                    query = query.ThenByDescending(
                        x => x.Name,
                        StringComparer.CurrentCultureIgnoreCase);
                    break;

                case LibrarySort.RecentlyPlayed:
                    // Never played sinks to the bottom rather than
                    // sorting as the oldest possible date.
                    query = query
                        .ThenByDescending(x => x.LastPlayedUtc.HasValue)
                        .ThenByDescending(x =>
                            x.LastPlayedUtc ?? DateTime.MinValue)
                        .ThenBy(
                            x => x.Name,
                            StringComparer.CurrentCultureIgnoreCase);
                    break;

                case LibrarySort.MostPlayed:
                    query = query
                        .ThenByDescending(x => x.TotalPlaySeconds)
                        .ThenByDescending(x => x.LaunchCount)
                        .ThenBy(
                            x => x.Name,
                            StringComparer.CurrentCultureIgnoreCase);
                    break;

                case LibrarySort.Launcher:
                    query = query
                        .ThenBy(
                            x => x.Launcher,
                            StringComparer.CurrentCultureIgnoreCase)
                        .ThenBy(
                            x => x.Name,
                            StringComparer.CurrentCultureIgnoreCase);
                    break;

                default:
                    query = query.ThenBy(
                        x => x.Name,
                        StringComparer.CurrentCultureIgnoreCase);
                    break;
            }

            return query.ToList();
        }

        /// <summary>
        /// Splits an already sorted list into headed sections without
        /// reordering it, so the chosen sort still holds inside each
        /// section.
        /// </summary>
        private static List<LibrarySection> Section(
            List<LibraryEntry> entries,
            LibraryGrouping grouping)
        {
            List<LibrarySection> sections =
                new List<LibrarySection>();

            if (grouping == LibraryGrouping.None)
            {
                LibrarySection all =
                    new LibrarySection();

                all.Entries = entries;

                sections.Add(all);

                return sections;
            }

            Dictionary<string, LibrarySection> byTitle =
                new Dictionary<string, LibrarySection>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (LibraryEntry entry in entries)
            {
                string title;
                string iconKey = null;
                string iconPath = null;

                switch (grouping)
                {
                    case LibraryGrouping.Launcher:
                        title = string.IsNullOrWhiteSpace(entry.Launcher)
                            ? "Other"
                            : entry.Launcher;
                        break;

                    case LibraryGrouping.UserGroup:
                        if (entry.Group != null)
                        {
                            title = entry.Group.Name;
                            iconKey = entry.Group.IconKey;
                            iconPath = entry.Group.IconPath;
                        }
                        else
                        {
                            title = "Ungrouped";
                        }
                        break;

                    default:
                        title = FirstLetter(entry.Name);
                        break;
                }

                LibrarySection section;

                if (!byTitle.TryGetValue(title, out section))
                {
                    section = new LibrarySection();
                    section.Title = title;
                    section.IconKey = iconKey;
                    section.IconPath = iconPath;

                    byTitle[title] = section;
                    sections.Add(section);
                }

                section.Entries.Add(entry);
            }

            // The sections themselves are ordered by their own title
            // rather than by the entry sort, except alphabetically
            // where "#" collects the non-letters and belongs last.
            if (grouping == LibraryGrouping.Alphabetical)
            {
                return sections
                    .OrderBy(x => x.Title == "#")
                    .ThenBy(
                        x => x.Title,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }

            string trailing =
                grouping == LibraryGrouping.UserGroup
                    ? "Ungrouped"
                    : "Other";

            return sections
                .OrderBy(x => string.Equals(
                    x.Title,
                    trailing,
                    StringComparison.OrdinalIgnoreCase))
                .ThenBy(
                    x => x.Title,
                    StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        private static string FirstLetter(
            string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "#";

            char c =
                char.ToUpperInvariant(name.TrimStart()[0]);

            return char.IsLetter(c)
                ? c.ToString()
                : "#";
        }

        /// <summary>
        /// Launcher names present in the library, for the launcher
        /// filter.
        /// </summary>
        public static List<string> GetLaunchers()
        {
            return LibraryService.Games
                .Where(x => x != null &&
                    !string.IsNullOrWhiteSpace(x.Launcher))
                .Select(x => x.Launcher)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
    }
}
