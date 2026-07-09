using Nexus_Launcher.Models;
using System;
using System.IO;

namespace Nexus_Launcher.Services.Artwork
{
    internal static class ArtworkCache
    {
        private static readonly string CacheRoot =
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Cache");

        public static string GetCacheRoot()
        {
            Directory.CreateDirectory(CacheRoot);

            return CacheRoot;
        }

        public static string GetLauncherFolder(
            string launcher)
        {
            string folder =
                Path.Combine(
                    GetCacheRoot(),
                    launcher);

            Directory.CreateDirectory(folder);

            return folder;
        }

        public static string GetGameFolder(
            GameInfo game)
        {
            string id =
                GetGameId(game);

            if (string.IsNullOrWhiteSpace(id))
            {
                id =
                    SanitizeFileName(
                        game.Name);
            }

            string folder =
                Path.Combine(
                    GetLauncherFolder(
                        game.Launcher),
                    id);

            Directory.CreateDirectory(folder);

            return folder;
        }

        public static string GetGridPath(
            GameInfo game)
        {
            return Path.Combine(
                GetGameFolder(game),
                "grid.jpg");
        }

        public static string GetHeroPath(
            GameInfo game)
        {
            return Path.Combine(
                GetGameFolder(game),
                "hero.jpg");
        }

        public static string GetLogoPath(
            GameInfo game)
        {
            return Path.Combine(
                GetGameFolder(game),
                "logo.png");
        }

        public static string GetMetadataPath(
            GameInfo game)
        {
            return Path.Combine(
                GetGameFolder(game),
                "metadata.json");
        }

        public static bool ArtworkExists(GameInfo game)
        {
            string grid = GetGridPath(game);
            string hero = GetHeroPath(game);
            string logo = GetLogoPath(game);

            bool gridExists = File.Exists(grid);
            bool heroExists = File.Exists(hero);
            bool logoExists = File.Exists(logo);

            System.Diagnostics.Debug.WriteLine(
                $"===== {game.Name} =====");

            System.Diagnostics.Debug.WriteLine(
                $"Launcher : {game.Launcher}");

            System.Diagnostics.Debug.WriteLine(
                $"Grid Path : {grid}");

            System.Diagnostics.Debug.WriteLine(
                $"Grid Exists : {gridExists}");

            System.Diagnostics.Debug.WriteLine(
                $"Hero Path : {hero}");

            System.Diagnostics.Debug.WriteLine(
                $"Hero Exists : {heroExists}");

            System.Diagnostics.Debug.WriteLine(
                $"Logo Path : {logo}");

            System.Diagnostics.Debug.WriteLine(
                $"Logo Exists : {logoExists}");

            return gridExists || heroExists || logoExists;
        }

        public static void LoadCachedArtwork(
            GameInfo game)
        {
            string grid =
                GetGridPath(game);

            string hero =
                GetHeroPath(game);

            string logo =
                GetLogoPath(game);

            if (File.Exists(grid))
                game.GridImagePath = grid;

            if (File.Exists(hero))
                game.HeroImagePath = hero;

            if (File.Exists(logo))
                game.LogoPath = logo;

            game.HasArtwork =
                ArtworkExists(game);
        }

        private static string GetGameId(
            GameInfo game)
        {
            switch (game.Launcher)
            {
                case "Steam":

                    if (game.AppId > 0)
                        return game.AppId.ToString();

                    break;

                case "BattleNet":

                    if (!string.IsNullOrWhiteSpace(
                        game.ProductId))
                    {
                        return game.ProductId;
                    }

                    break;

                case "Epic":

                    if (!string.IsNullOrWhiteSpace(
                        game.epicLauncherAppId))
                    {
                        return game.epicLauncherAppId;
                    }

                    break;

                case "EA":

                    if (!string.IsNullOrWhiteSpace(
                        game.AppUserModelId))
                    {
                        return game.AppUserModelId;
                    }

                    break;

                case "Ubisoft":

                    if (!string.IsNullOrWhiteSpace(
                        game.LaunchUri))
                    {
                        return SanitizeFileName(
                            game.LaunchUri);
                    }

                    break;

                case "GOG":

                    return SanitizeFileName(
                        game.Name);

                case "Xbox":

                    if (!string.IsNullOrWhiteSpace(
                        game.AppUserModelId))
                    {
                        return game.AppUserModelId;
                    }

                    break;
            }

            return null;
        }

        private static string SanitizeFileName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                value =
                    value.Replace(
                        c,
                        '_');
            }

            return value;
        }
        public static bool ClearCache()
        {
            try
            {
                string cacheRoot = GetCacheRoot();

                if (!Directory.Exists(cacheRoot))
                    return true;

                // Give the UI a moment to release any disposed images
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                DeleteDirectory(cacheRoot);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                return false;
            }
        }

        private static void DeleteDirectory(string path)
        {
            foreach (string file in Directory.GetFiles(path))
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch
                {
                    // ignore locked files
                }
            }

            foreach (string dir in Directory.GetDirectories(path))
            {
                DeleteDirectory(dir);

                try
                {
                    Directory.Delete(dir, false);
                }
                catch
                {
                }
            }

            try
            {
                Directory.Delete(path, false);
            }
            catch
            {
            }
        }

    }
}