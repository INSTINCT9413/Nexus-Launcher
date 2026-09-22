using Nexus_Launcher.Models;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Nexus_Launcher.Services.Artwork
{
    /// <summary>
    /// Which of the three artwork slots is being replaced.
    ///
    /// Public because ApplicationCard is public and exposes this on
    /// the methods buttons are wired to.
    /// </summary>
    public enum ArtworkKind
    {
        Grid,

        Hero,

        Logo
    }

    /// <summary>
    /// User supplied artwork that overrides whatever SteamGridDB
    /// provided, and gives unsupported programs artwork they would
    /// otherwise never get.
    ///
    /// Deliberately stored under AppData rather than in the Cache
    /// folder: ArtworkCache.ClearCache deletes that entire tree, and a
    /// refresh must never throw away files the user supplied.
    /// </summary>
    internal static class CustomArtworkService
    {
        private static readonly string StoreRoot =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "NexusLauncher",
                "CustomArtwork");

        /// <summary>
        /// Raised after artwork is added or removed so the UI can
        /// redraw the game.
        /// </summary>
        public static event Action<GameInfo> CustomArtworkChanged;

        public static readonly string FileDialogFilter =
            "Images (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|" +
            "*.png;*.jpg;*.jpeg;*.bmp;*.gif";

        //--------------------------------------------------------------
        // Locations
        //--------------------------------------------------------------

        /// <summary>
        /// Folder holding one game's custom artwork. Keyed on launcher
        /// plus name to match how favourites and play stats are keyed,
        /// so it survives a rescan.
        /// </summary>
        public static string GetGameFolder(
            GameInfo game,
            bool create)
        {
            if (game == null)
                return null;

            string folder =
                Path.Combine(
                    StoreRoot,
                    Sanitize(game.Launcher),
                    Sanitize(game.Name));

            if (create)
                Directory.CreateDirectory(folder);

            return folder;
        }

        private static string GetBaseName(
            ArtworkKind kind)
        {
            switch (kind)
            {
                case ArtworkKind.Hero:
                    return "hero";

                case ArtworkKind.Logo:
                    return "logo";

                default:
                    return "grid";
            }
        }

        /// <summary>
        /// The custom file for a slot, or null when there is none.
        /// The extension is whatever the user supplied, so this globs
        /// rather than assuming .png.
        /// </summary>
        public static string GetCustomPath(
            GameInfo game,
            ArtworkKind kind)
        {
            try
            {
                string folder =
                    GetGameFolder(game, false);

                if (string.IsNullOrEmpty(folder) ||
                    !Directory.Exists(folder))
                {
                    return null;
                }

                return Directory
                    .GetFiles(
                        folder,
                        GetBaseName(kind) + ".*")
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return null;
            }
        }

        public static bool HasCustom(
            GameInfo game,
            ArtworkKind kind)
        {
            return GetCustomPath(game, kind) != null;
        }

        public static bool HasAnyCustom(
            GameInfo game)
        {
            return HasCustom(game, ArtworkKind.Grid) ||
                HasCustom(game, ArtworkKind.Hero) ||
                HasCustom(game, ArtworkKind.Logo);
        }

        /// <summary>
        /// How many games have at least one custom image, counted from
        /// the store on disk so it includes games not loaded yet.
        /// </summary>
        public static int CountGamesWithCustomArtwork()
        {
            try
            {
                if (!Directory.Exists(StoreRoot))
                    return 0;

                return Directory
                    .GetDirectories(StoreRoot)
                    .SelectMany(Directory.GetDirectories)
                    .Count(x => Directory.GetFiles(x).Length > 0);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return 0;
            }
        }

        //--------------------------------------------------------------
        // Changing artwork
        //--------------------------------------------------------------

        /// <summary>
        /// Copies a user chosen image into the store and points the
        /// game at it. Returns the stored path, or null on failure.
        /// </summary>
        public static string SetCustomArtwork(
            GameInfo game,
            ArtworkKind kind,
            string sourceFile)
        {
            if (game == null ||
                string.IsNullOrWhiteSpace(sourceFile) ||
                !File.Exists(sourceFile))
            {
                return null;
            }

            try
            {
                // Fail before touching the store if it is not an image.
                if (!IsReadableImage(sourceFile))
                    return null;

                string folder =
                    GetGameFolder(game, true);

                // Drop any previous file for this slot, whatever its
                // extension was.
                RemoveCustomArtwork(
                    game,
                    kind,
                    false);

                string target =
                    Path.Combine(
                        folder,
                        GetBaseName(kind) +
                            Path.GetExtension(sourceFile).ToLowerInvariant());

                File.Copy(
                    sourceFile,
                    target,
                    true);

                ApplyTo(game);

                CustomArtworkChanged?.Invoke(game);

                return target;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return null;
            }
        }

        /// <summary>
        /// Drops the custom image for a slot. The game falls back to
        /// its cached SteamGridDB artwork on the next load.
        /// </summary>
        public static bool RemoveCustomArtwork(
            GameInfo game,
            ArtworkKind kind)
        {
            return RemoveCustomArtwork(
                game,
                kind,
                true);
        }

        private static bool RemoveCustomArtwork(
            GameInfo game,
            ArtworkKind kind,
            bool notify)
        {
            bool removed = false;

            try
            {
                string folder =
                    GetGameFolder(game, false);

                if (string.IsNullOrEmpty(folder) ||
                    !Directory.Exists(folder))
                {
                    return false;
                }

                foreach (string file in Directory.GetFiles(
                    folder,
                    GetBaseName(kind) + ".*"))
                {
                    File.SetAttributes(
                        file,
                        FileAttributes.Normal);

                    File.Delete(file);

                    removed = true;
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return false;
            }

            if (removed && notify)
            {
                // Rebuild the paths from the cache, then tell the UI.
                ArtworkCache.LoadCachedArtwork(game);

                CustomArtworkChanged?.Invoke(game);
            }

            return removed;
        }

        /// <summary>
        /// Drops every custom image for a game in one go. Raises a
        /// single change notification rather than one per slot.
        /// </summary>
        public static bool RemoveAllCustomArtwork(
            GameInfo game)
        {
            bool removed = false;

            removed |= RemoveCustomArtwork(
                game,
                ArtworkKind.Grid,
                false);

            removed |= RemoveCustomArtwork(
                game,
                ArtworkKind.Hero,
                false);

            removed |= RemoveCustomArtwork(
                game,
                ArtworkKind.Logo,
                false);

            return removed;
        }

        /// <summary>
        /// Lets another service announce a change it made on this
        /// game's behalf, such as a full artwork reset.
        /// </summary>
        public static void NotifyChanged(
            GameInfo game)
        {
            if (game != null)
                CustomArtworkChanged?.Invoke(game);
        }

        //--------------------------------------------------------------
        // Applying
        //--------------------------------------------------------------

        /// <summary>
        /// Points a game's artwork paths at its custom files where any
        /// exist. Called from ArtworkCache.LoadCachedArtwork so every
        /// screen picks the override up without its own special case.
        /// </summary>
        public static void ApplyTo(
            GameInfo game)
        {
            if (game == null)
                return;

            string grid =
                GetCustomPath(game, ArtworkKind.Grid);

            if (grid != null)
                game.GridImagePath = grid;

            string hero =
                GetCustomPath(game, ArtworkKind.Hero);

            if (hero != null)
                game.HeroImagePath = hero;

            string logo =
                GetCustomPath(game, ArtworkKind.Logo);

            if (logo != null)
                game.LogoPath = logo;

            if (grid != null || hero != null || logo != null)
                game.HasArtwork = true;
        }

        //--------------------------------------------------------------
        // Loading
        //--------------------------------------------------------------

        /// <summary>
        /// Loads an image without keeping a lock on the file.
        ///
        /// Image.FromFile holds the file open for the lifetime of the
        /// Image, which would stop the user replacing their own artwork
        /// while it is on screen.
        /// </summary>
        public static Image LoadUnlocked(
            string path)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                !File.Exists(path))
            {
                return null;
            }

            try
            {
                using (Bitmap source = new Bitmap(path))
                {
                    return new Bitmap(source);
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return null;
            }
        }

        private static bool IsReadableImage(
            string path)
        {
            try
            {
                using (Bitmap probe = new Bitmap(path))
                {
                    return probe.Width > 0 && probe.Height > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string Sanitize(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                value =
                    value.Replace(c, '_');
            }

            return value.Trim();
        }
    }
}
