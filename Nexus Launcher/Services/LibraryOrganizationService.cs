using Newtonsoft.Json;
using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nexus_Launcher.Services.Library
{
    /// <summary>
    /// Stores the user's favourites and their per launcher groups.
    ///
    /// Games are keyed by "launcher|name" so the arrangement is kept
    /// when a launcher is rescanned and the GameInfo objects are
    /// rebuilt from scratch.
    /// </summary>
    internal static class LibraryOrganizationService
    {
        private static readonly string SaveFile =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "NexusLauncher",
                "LibraryOrganization.json");

        private static readonly object sync =
            new object();

        private static LibraryOrganization data;

        /// <summary>
        /// Raised after a favourite or group change has been saved.
        /// </summary>
        public static event Action Changed;

        //----------------------------------------------------------
        // Keys
        //----------------------------------------------------------

        public static string GetKey(
            string launcher,
            string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return (launcher ?? string.Empty).Trim().ToLowerInvariant() +
                "|" +
                name.Trim().ToLowerInvariant();
        }

        public static string GetKey(
            GameInfo game)
        {
            if (game == null)
                return null;

            return GetKey(
                game.Launcher,
                game.Name);
        }

        //----------------------------------------------------------
        // Storage
        //----------------------------------------------------------

        private static LibraryOrganization Data
        {
            get
            {
                lock (sync)
                {
                    if (data == null)
                        data = LoadFromDisk();

                    return data;
                }
            }
        }

        private static LibraryOrganization LoadFromDisk()
        {
            try
            {
                if (!File.Exists(SaveFile))
                    return new LibraryOrganization();

                LibraryOrganization loaded =
                    JsonConvert.DeserializeObject<LibraryOrganization>(
                        File.ReadAllText(SaveFile));

                if (loaded == null)
                    return new LibraryOrganization();

                if (loaded.Favorites == null)
                    loaded.Favorites = new List<string>();

                if (loaded.Groups == null)
                    loaded.Groups = new List<LibraryGroup>();

                foreach (LibraryGroup group in loaded.Groups)
                {
                    if (group.Games == null)
                        group.Games = new List<string>();
                }

                return loaded;
            }
            catch (Exception ex)
            {
                // A corrupt file must never stop the launcher from
                // starting, so fall back to an empty arrangement.
                Program.LogCrash(ex);

                return new LibraryOrganization();
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

                File.WriteAllText(
                    SaveFile,
                    JsonConvert.SerializeObject(
                        Data,
                        Formatting.Indented));
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            Changed?.Invoke();
        }

        //----------------------------------------------------------
        // Favourites
        //----------------------------------------------------------

        public static bool IsFavorite(
            GameInfo game)
        {
            string key =
                GetKey(game);

            if (key == null)
                return false;

            lock (sync)
            {
                return Data.Favorites.Contains(key);
            }
        }

        public static void SetFavorite(
            GameInfo game,
            bool favorite)
        {
            string key =
                GetKey(game);

            if (key == null)
                return;

            lock (sync)
            {
                if (favorite)
                {
                    if (Data.Favorites.Contains(key))
                        return;

                    Data.Favorites.Add(key);
                }
                else
                {
                    if (!Data.Favorites.Remove(key))
                        return;
                }
            }

            Save();
        }

        public static bool ToggleFavorite(
            GameInfo game)
        {
            bool favorite =
                !IsFavorite(game);

            SetFavorite(
                game,
                favorite);

            return favorite;
        }

        //----------------------------------------------------------
        // Groups
        //----------------------------------------------------------

        /// <summary>
        /// The groups belonging to a launcher, in display order.
        /// </summary>
        public static List<LibraryGroup> GetGroups(
            string launcher)
        {
            lock (sync)
            {
                return Data.Groups
                    .Where(x => string.Equals(
                        x.Launcher,
                        launcher,
                        StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name)
                    .ToList();
            }
        }

        public static LibraryGroup GetGroupById(
            string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            lock (sync)
            {
                return Data.Groups
                    .FirstOrDefault(x => x.Id == id);
            }
        }

        /// <summary>
        /// The group a game has been placed in, or null when it has
        /// not been grouped.
        /// </summary>
        public static LibraryGroup GetGroupFor(
            GameInfo game)
        {
            string key =
                GetKey(game);

            if (key == null)
                return null;

            lock (sync)
            {
                return Data.Groups
                    .FirstOrDefault(x => x.Games.Contains(key));
            }
        }

        public static LibraryGroup CreateGroup(
            string launcher,
            string name,
            string iconKey,
            string iconPath)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            LibraryGroup group;

            lock (sync)
            {
                int nextOrder =
                    Data.Groups.Count == 0
                        ? 0
                        : Data.Groups.Max(x => x.SortOrder) + 1;

                group = new LibraryGroup
                {
                    Launcher = launcher,
                    Name = name.Trim(),
                    IconKey = iconKey,
                    IconPath = iconPath,
                    SortOrder = nextOrder
                   
                };
                
                Data.Groups.Add(group);
            }

            Save();

            return group;
        }

        /// <summary>
        /// Applies a rename and an icon change in one go.
        /// </summary>
        public static void UpdateGroup(
            string id,
            string name,
            string iconKey,
            string iconPath)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            lock (sync)
            {
                LibraryGroup group =
                    Data.Groups.FirstOrDefault(x => x.Id == id);

                if (group == null)
                    return;

                group.Name = name.Trim();
                group.IconKey = iconKey;
                group.IconPath = iconPath;
            }

            Save();
        }

        /// <summary>
        /// Removes a group. The games it held simply become
        /// ungrouped again, they are never deleted.
        /// </summary>
        public static void DeleteGroup(
            string id)
        {
            lock (sync)
            {
                LibraryGroup group =
                    Data.Groups.FirstOrDefault(x => x.Id == id);

                if (group == null)
                    return;

                Data.Groups.Remove(group);
            }

            Save();
        }

        /// <summary>
        /// Places a game in a group, or removes it from every group
        /// when groupId is null or empty.
        /// </summary>
        public static void AssignToGroup(
            GameInfo game,
            string groupId)
        {
            string key =
                GetKey(game);

            if (key == null)
                return;

            lock (sync)
            {
                foreach (LibraryGroup group in Data.Groups)
                {
                    group.Games.Remove(key);
                }

                if (!string.IsNullOrEmpty(groupId))
                {
                    LibraryGroup target =
                        Data.Groups.FirstOrDefault(x => x.Id == groupId);

                    if (target != null)
                        target.Games.Add(key);
                }
            }

            Save();
        }

        /// <summary>
        /// Moves a group up (-1) or down (+1) within its launcher.
        /// </summary>
        public static void MoveGroup(
            string id,
            int offset)
        {
            lock (sync)
            {
                LibraryGroup group =
                    Data.Groups.FirstOrDefault(x => x.Id == id);

                if (group == null)
                    return;

                List<LibraryGroup> ordered =
                    Data.Groups
                        .Where(x => string.Equals(
                            x.Launcher,
                            group.Launcher,
                            StringComparison.OrdinalIgnoreCase))
                        .OrderBy(x => x.SortOrder)
                        .ThenBy(x => x.Name)
                        .ToList();

                int index =
                    ordered.IndexOf(group);

                int target =
                    index + offset;

                if (index < 0 || target < 0 || target >= ordered.Count)
                    return;

                ordered.RemoveAt(index);

                ordered.Insert(
                    target,
                    group);

                // Renumber so the new order is what gets saved.
                for (int i = 0; i < ordered.Count; i++)
                {
                    ordered[i].SortOrder = i;
                }
            }

            Save();
        }
    }
}
