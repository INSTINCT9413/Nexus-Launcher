using Nexus_Launcher.Helpers;
using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher.Services
{
    internal class SteamScannerService
    {
        private string steamRootPath = MainView.sysDisk + @"Program Files (x86)\Steam";
        private Dictionary<int, SteamArtworkInfo>
    artworkCache =
        new Dictionary<int, SteamArtworkInfo>();
        public SteamScannerService()
        {
            BuildArtworkCache();
        }
        private void BuildArtworkCache()
        {
            artworkCache.Clear();

            string cacheRoot =
                Path.Combine(
                    steamRootPath,
                    "appcache",
                    "librarycache");

            if (!Directory.Exists(cacheRoot))
                return;

            string[] files =
                Directory.GetFiles(
                    cacheRoot,
                    "*.*",
                    SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string fullPath =
                    file.ToLower();

                Match match =
                    Regex.Match(
                        fullPath,
                        @"\\(\d+)\\");

                if (!match.Success)
                    continue;

                int appId;

                if (!int.TryParse(
                    match.Groups[1].Value,
                    out appId))
                {
                    continue;
                }

                if (!artworkCache.ContainsKey(appId))
                {
                    artworkCache[appId] =
                        new SteamArtworkInfo();
                }

                SteamArtworkInfo artwork =
                    artworkCache[appId];

                string fileName =
                    Path.GetFileName(file)
                    .ToLower();

                if (fileName == "library_hero.jpg")
                {
                    artwork.Header = file;
                }
                
                else if (
                    fileName.Contains("library_600"))
                {
                    artwork.Capsule = file;
                }
                else if (
                    fileName.Length == 40)
                {
                    artwork.Logo = file;
                }
            }
        }

        public LauncherInfo ScanSteam(string steamPath)
        {
            if (string.IsNullOrWhiteSpace(steamPath) ||
                !Directory.Exists(steamPath))
            {
                return null;
            }

            // Store the main Steam install path
            steamRootPath = steamPath;

            LauncherInfo launcher =
                new LauncherInfo();

            launcher.Name = "Steam";

            launcher.InstallPath =
                steamPath;

            launcher.ExecutablePath =
                Path.Combine(
                    steamPath,
                    "steam.exe");

            launcher.Games =
                ScanGames(steamPath);

            return launcher;
        }
        private void ApplyArtwork(
    GameInfo game)
        {
            if (!artworkCache.ContainsKey(
                game.AppId))
            {
                return;
            }

            SteamArtworkInfo artwork =
                artworkCache[game.AppId];
            game.Launcher = "Steam";
            game.HeaderImagePath =
                artwork.Header;

            game.LogoPath =
                artwork.Logo;
            game.LibraryImagePath =
                artwork.Capsule;
            // Prefer capsule over icon
       if (!string.IsNullOrWhiteSpace(
                artwork.Logo))
            {
                game.IconPath =
                    artwork.Logo;
            }
            else
            {
                game.IconPath =
                    artwork.Capsule;
            }
            

        }
        public List<GameInfo> ScanGames(string steamPath)
        {
            List<GameInfo> games =
                new List<GameInfo>();

            List<string> libraries =
                GetSteamLibraries(steamPath);

            foreach (string library in libraries)
            {
                string steamAppsPath =
                    Path.Combine(
                        library,
                        "steamapps");

                if (!Directory.Exists(steamAppsPath))
                    continue;

                string[] manifests =
                    Directory.GetFiles(
                        steamAppsPath,
                        "appmanifest_*.acf");

                foreach (string manifest in manifests)
                {
                    try
                    {
                        GameInfo game =
                            ParseAppManifest(
                                manifest,
                                library);

                        if (game == null)
                            continue;

                        if (ShouldIgnoreSteamGame(game))
                            continue;

                        games.Add(game);
                    }
                    catch (Exception ex)
                    {
                        Program.LogCrash(ex);
                        System.Diagnostics.Debug.WriteLine(
                            ex.ToString());
                    }
                }
            }

            // Sort alphabetically
            games.Sort(delegate (GameInfo a, GameInfo b)
            {
                return string.Compare(
                    a.Name,
                    b.Name,
                    StringComparison.OrdinalIgnoreCase);
            });

            foreach (GameInfo game in games)
            {
                Nexus_Launcher.Services.Artwork.ArtworkService.RegisterGame(game);
            }
            return games;
        }

        private List<string> GetSteamLibraries(
            string steamPath)
        {
            List<string> libraries =
                new List<string>
                {
                    // Main Steam library
                    steamPath
                };

            string vdfPath =
                Path.Combine(
                    steamPath,
                    "steamapps",
                    "libraryfolders.vdf");

            if (!File.Exists(vdfPath))
                return libraries;

            string content =
                File.ReadAllText(vdfPath);

            MatchCollection matches =
                Regex.Matches(
                    content,
                    "\"path\"\\s+\"([^\"]+)\"");

            foreach (Match match in matches)
            {
                string path =
                    match.Groups[1].Value;

                path =
                    path.Replace("\\\\", "\\");

                if (!libraries.Contains(path))
                {
                    libraries.Add(path);
                }
            }

            return libraries;
        }

        private GameInfo ParseAppManifest(
            string manifestPath,
            string libraryPath)
        {
            string content =
                File.ReadAllText(manifestPath);

            int appId =
                ParseIntValue(
                    content,
                    "appid");

            string name =
                ParseStringValue(
                    content,
                    "name");

            string installDir =
                ParseStringValue(
                    content,
                    "installdir");

            if (string.IsNullOrWhiteSpace(
                installDir))
            {
                return null;
            }

            string gamePath =
                Path.Combine(
                    libraryPath,
                    "steamapps",
                    "common",
                    installDir);

            if (!Directory.Exists(gamePath))
                return null;

            string exe =
                FindMainExecutable(
                    gamePath);

            GameInfo game =
                new GameInfo();
            game.Launcher = "Steam";
            game.AppId = appId;

            game.Name = name;

            game.InstallPath =
                gamePath;

            game.ExecutablePath =
                exe;
            ApplyArtwork(game);
            

            return game;
        }

        private int ParseIntValue(
            string content,
            string key)
        {
            Match match =
                Regex.Match(
                    content,
                    "\"" + key + "\"\\s+\"(\\d+)\"");

            if (!match.Success)
                return 0;

            return int.Parse(
                match.Groups[1].Value);
        }

        private string ParseStringValue(
            string content,
            string key)
        {
            Match match =
                Regex.Match(
                    content,
                    "\"" + key + "\"\\s+\"([^\"]+)\"");

            if (!match.Success)
                return null;

            return match.Groups[1].Value;
        }

        private string FindMainExecutable(
            string folder)
        {
            List<string> searchFolders =
                new List<string>
                {
                    folder
                };

            string[] commonFolders =
            {
                "bin",
                "binaries",
                "win64",
                "win32",
                "launcher"
            };

            foreach (string name in commonFolders)
            {
                string path =
                    Path.Combine(
                        folder,
                        name);

                if (Directory.Exists(path))
                {
                    searchFolders.Add(path);
                }
            }

            foreach (string dir in searchFolders)
            {
                string[] exes =
                    Directory.GetFiles(
                        dir,
                        "*.exe",
                        SearchOption.TopDirectoryOnly);

                List<string> filtered =
    exes
    .Where(x =>
        !x.ToLower().Contains("crash") &&
        !x.ToLower().Contains("setup") &&
        !x.ToLower().Contains("unins") &&
        !x.ToLower().Contains("easyanticheat") &&
        !x.ToLower().Contains("eac"))
    .ToList();

                if (filtered.Count > 0)
                {
                    return filtered
                        .OrderByDescending(x =>
                            new FileInfo(x).Length)
                        .FirstOrDefault();
                }
            }

            return null;
        }
        private bool ShouldIgnoreSteamGame(GameInfo game)
        {
            if (game == null)
                return true;

            string name =
                (game.Name ?? string.Empty)
                .ToLowerInvariant();

            //------------------------------------------------
            // Steam system apps
            //------------------------------------------------

            if (name.Contains("steamworks common redistributables"))
                return true;

            if (name.Contains("steam linux runtime"))
                return true;

            if (name.Contains("proton"))
                return true;

            if (name.Contains("steam runtime"))
                return true;

            if (name.Contains("source sdk"))
                return true;

            if (name.Contains("dedicated server"))
                return true;

            if (name.Contains("server"))
                return true;

            if (name.Contains("redistributable"))
                return true;

            if (name.Contains("benchmark"))
                return true;

            if (name.Contains("soundtrack"))
                return true;

            if (name.Contains("demo"))
                return true;

            return false;
        }
    }

}
