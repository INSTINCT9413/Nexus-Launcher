using IWshRuntimeLibrary;
using Microsoft.Win32;
using Nexus_Launcher.Models;
using Nexus_Launcher.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nexus_Launcher.Services
{
    internal class EAScannerService
    {
        private const string RegistryPath =
            @"SOFTWARE\WOW6432Node\EA Games";
        private bool IsGameExecutable(
    string exe)
        {
            string lower =
                exe.ToLowerInvariant();

            //------------------------------------------------
            // Ignore folders
            //------------------------------------------------

            string[] ignoredFolders =
            {
        "\\__installer\\",
        "\\installer\\",
        "\\install\\",
        "\\support\\",
        "\\redist\\",
        "\\redistributable\\",
        "\\_commonredist\\",
        "\\directx\\",
        "\\vc\\",
        "\\touchup\\",
        "\\language\\",
        "\\core\\",
        "\\post\\",

        "\\en_us\\",
        "\\fr_fr\\",
        "\\de_de\\",
        "\\es_es\\",
        "\\it_it\\",
        "\\ja_jp\\",
        "\\ko_kr\\",
        "\\ru_ru\\",
        "\\zh_cn\\"
    };

            foreach (string folder in ignoredFolders)
            {
                if (lower.Contains(folder))
                    return false;
            }

            //------------------------------------------------
            // Ignore filenames
            //------------------------------------------------

            string file =
                Path.GetFileName(lower);

            string[] ignored =
            {
        "installer",
        "setup",
        "touchup",
        "unins",
        "uninstall",
        "patch",
        "update",
        "updater",
        "crash",
        "report",
        "benchmark",
        "redist",
        "directx",
        "d3d11install",
        "eadesktop",
        "originthinsetup",
        "eaanticheat"
    };

            foreach (string text in ignored)
            {
                if (file.Contains(text))
                    return false;
            }

            return true;
        }
        private void AddExecutableGame(
    string exe,
    List<GameInfo> games)
        {
            string gameRoot =
                FindGameRoot(exe);

            if (string.IsNullOrWhiteSpace(gameRoot))
                return;

            string gameName =
                Path.GetFileName(gameRoot);

            //------------------------------------------------
            // Duplicate?
            //------------------------------------------------

            string normalized =
    NormalizeGameName(gameName);

            if (games.Any(x =>
                NormalizeGameName(x.Name) == normalized))
            {
                return;
            }

            GameInfo game =
                new GameInfo();

            game.Name =
                gameName;

            game.ProductId =
                gameName;

            game.InstallPath =
                gameRoot;

            game.ExecutablePath =
                exe;

            game.IsInstalled =
                true;

            game.Launcher =
                "EA";

            games.Add(game);
        }
        private string FindGameRoot(
    string exe)
        {
            DirectoryInfo dir =
                new FileInfo(exe).Directory;

            string[] ignored =
            {
        "FinalAlert2",
        "bin",
        "bin64",
        "game",
        "x64",
        "x86",
        "win64",
        "win32",

        "__installer",
        "installer",
        "install",
        "support",

        "core",
        "post",

        "redist",
        "_commonredist",
        "directx",
        "vc",

        "touchup",

        "en_us",
        "fr_fr",
        "de_de",
        "es_es",
        "it_it",
        "ja_jp",
        "ko_kr",
        "ru_ru",
        "zh_cn"
    };

            while (dir != null)
            {
                if (!ignored.Contains(
                    dir.Name,
                    StringComparer.OrdinalIgnoreCase))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            return null;
        }
        private string NormalizeGameName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            //------------------------------------------------
            // Remove common trademark text
            //------------------------------------------------

            name = Regex.Replace(
                name,
                @"\((tm|r|c)\)",
                "",
                RegexOptions.IgnoreCase);

            //------------------------------------------------
            // Remove unicode trademark symbols
            //------------------------------------------------

            name = name
                .Replace("™", "")
                .Replace("®", "")
                .Replace("©", "");

            //------------------------------------------------
            // Normalize unicode
            //------------------------------------------------

            name = name.Normalize(
                NormalizationForm.FormKD);

            //------------------------------------------------
            // Remove punctuation
            //------------------------------------------------

            name = Regex.Replace(
                name,
                @"[^\w\s]",
                "");

            //------------------------------------------------
            // Remove standalone TM/R/C words
            //------------------------------------------------

            name = Regex.Replace(
                name,
                @"\b(tm|r|c)\b",
                "",
                RegexOptions.IgnoreCase);

            //------------------------------------------------
            // Collapse whitespace
            //------------------------------------------------

            name = Regex.Replace(
                name,
                @"\s+",
                " ");

            return name
                .Trim()
                .ToLowerInvariant();
        }
        public List<GameInfo> ScanGames()
        {
            List<GameInfo> games =
                ScanRegistryGames();

            ScanLibraryFolders(games);

            foreach (GameInfo game in games)
            {
                Nexus_Launcher.Services.Artwork.ArtworkService.RegisterGame(game);
            }

            return games;
        }
        private List<GameInfo> ScanRegistryGames()
        {
            List<GameInfo> games =
                new List<GameInfo>();

            using (RegistryKey root =
                Registry.LocalMachine.OpenSubKey(
                    RegistryPath))
            {
                if (root == null)
                    return games;

                foreach (string subKeyName in root.GetSubKeyNames())
                {
                    try
                    {
                        using (RegistryKey gameKey =
    root.OpenSubKey(subKeyName))
                        {
                            if (gameKey == null)
                                continue;

                            //------------------------------------------------
                            // Ignore empty registry entries
                            //------------------------------------------------

                            if (gameKey.ValueCount == 0)
                                continue;

                            string displayName =
                                gameKey.GetValue(
                                    "DisplayName")
                                ?.ToString();

                            string installDir =
                                gameKey.GetValue(
                                    "Install Dir")
                                ?.ToString();

                            //------------------------------------------------
                            // No install directory?
                            //------------------------------------------------

                            if (string.IsNullOrWhiteSpace(installDir))
                                continue;

                            //------------------------------------------------
                            // Install folder missing?
                            //------------------------------------------------

                            if (!Directory.Exists(installDir))
                                continue;

                            if (string.IsNullOrWhiteSpace(displayName))
                            {
                                displayName = subKeyName;
                            }

                            GameInfo game =
                                new GameInfo();

                            game.Name = displayName;
                            game.ProductId = subKeyName;
                            game.InstallPath = installDir;
                            game.Launcher = "EA";
                            game.IsInstalled = true;

                            string shortcut =
                                FindShortcut(displayName);

                            if (!string.IsNullOrWhiteSpace(shortcut))
                            {
                                game.ShortcutPath = shortcut;
                                game.ExecutablePath = shortcut;
                            }
                            else
                            {
                                game.ExecutablePath =
                                    FindExecutable(installDir);
                            }

                            string normalized =
    NormalizeGameName(displayName);

                            if (games.Any(x =>
                                NormalizeGameName(x.Name) == normalized))
                            {
                                continue;
                            }

                            games.Add(game);
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return games;
        }
        private void ScanLibraryFolders(
    List<GameInfo> games)
        {
            List<string> libraries =
                new List<string>();

            //------------------------------------------------
            // Default folders
            //------------------------------------------------

            libraries.Add(MainView.originPath);
            libraries.Add(MainView.eaGamesPath);

            //------------------------------------------------
            // User folders
            //------------------------------------------------

            if (Settings.Default.EALibraryPaths != null)
            {
                foreach (string path in Settings.Default.EALibraryPaths)
                {
                    if (!libraries.Any(x =>
                        string.Equals(
                            x,
                            path,
                            StringComparison.OrdinalIgnoreCase)))
                    {
                        libraries.Add(path);
                    }
                }
            }

            //------------------------------------------------
            // Scan
            //------------------------------------------------

            foreach (string library in libraries)
            {
                if (!Directory.Exists(library))
                    continue;

                ScanLibraryFolder(
                    library,
                    games);
            }
        }
        private void ScanLibraryFolder(
    string library,
    List<GameInfo> games)
        {
            string[] executables;

            try
            {
                executables =
                    Directory.GetFiles(
                        library,
                        "*.exe",
                        SearchOption.AllDirectories);
            }
            catch
            {
                return;
            }

            foreach (string exe in executables)
            {
                if (!IsGameExecutable(exe))
                    continue;

                AddExecutableGame(
                    exe,
                    games);
            }
        }
        private void AddFolderGame(
    string gameFolder,
    List<GameInfo> games)
        {
            string exe =
                FindExecutable(gameFolder);

            if (string.IsNullOrWhiteSpace(exe))
                return;

            //----------------------------------------------------
            // Skip duplicates
            //----------------------------------------------------

            if (games.Any(x =>
                !string.IsNullOrWhiteSpace(x.InstallPath) &&
                string.Equals(
                    x.InstallPath,
                    gameFolder,
                    StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            if (games.Any(x =>
                !string.IsNullOrWhiteSpace(x.ExecutablePath) &&
                string.Equals(
                    x.ExecutablePath,
                    exe,
                    StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            //----------------------------------------------------
            // Add game
            //----------------------------------------------------

            GameInfo game =
                new GameInfo();

            game.Name =
                Path.GetFileName(gameFolder);

            game.ProductId =
                Path.GetFileName(gameFolder);

            game.InstallPath =
                gameFolder;

            game.ExecutablePath =
                exe;

            game.IsInstalled =
                true;

            game.Launcher =
                "EA";

            games.Add(game);
        }
        private string FindShortcut(
            string gameName)
        {
            List<string> searchPaths =
                new List<string>();

            searchPaths.Add(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonPrograms));

            searchPaths.Add(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.Programs));

            foreach (string path in searchPaths)
            {
                if (!Directory.Exists(path))
                    continue;

                try
                {
                    string[] shortcuts =
                        Directory.GetFiles(
                            path,
                            "*.lnk",
                            SearchOption.AllDirectories);

                    foreach (string shortcut in shortcuts)
                    {
                        string shortcutName =
                            Path.GetFileNameWithoutExtension(
                                shortcut);

                        if (shortcutName.IndexOf(
                            gameName,
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return shortcut;
                        }

                        if (gameName.IndexOf(
                            shortcutName,
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return shortcut;
                        }
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private string FindExecutable(
            string installDir)
        {
            if (string.IsNullOrWhiteSpace(
                installDir))
            {
                return null;
            }

            if (!Directory.Exists(
                installDir))
            {
                return null;
            }

            try
            {
                List<FileInfo> exes =
                    Directory.GetFiles(
                        installDir,
                        "*.exe",
                        SearchOption.TopDirectoryOnly)
                    .Select(x => new FileInfo(x))
                    .ToList();

                exes.RemoveAll(x =>
                {
                    string name =
                        x.Name.ToLower();

                    return
        name.Contains("unins") ||
        name.Contains("uninstall") ||
        name.Contains("crash") ||
        name.Contains("reporter") ||
        name.Contains("updater") ||
        name.Contains("update") ||
        name.Contains("patch") ||
        name.Contains("repair") ||
        name.Contains("support") ||
        name.Contains("eadesktop") ||
        name.Contains("anticheat") ||
        name.Contains("trial") ||
        name.Contains("benchmark");
                });

                FileInfo best =
                    exes
                    .OrderByDescending(
                        x => x.Length)
                    .FirstOrDefault();

                return best?.FullName;
            }
            catch
            {
                return null;
            }
        }
    }
}