using IWshRuntimeLibrary;
using Newtonsoft.Json.Linq;
using Nexus_Launcher.Models;
using Nexus_Launcher.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using File = System.IO.File;

namespace Nexus_Launcher.Services
{
    internal class GOGScannerService
    {
        private readonly List<string> _libraryPaths;

        public GOGScannerService()
        {
            _libraryPaths =
                new List<string>();

            _libraryPaths.Add(MainView.sysDisk +
                @"Program Files (x86)\GOG Galaxy\Games");

            if (Settings.Default.GOGLibraryPaths != null)
            {
                foreach (string path in
                    Settings.Default.GOGLibraryPaths)
                {
                    if (!_libraryPaths.Contains(
                        path,
                        StringComparer.OrdinalIgnoreCase))
                    {
                        _libraryPaths.Add(path);
                    }
                }
            }
        }

        public List<GameInfo> ScanGames()
        {
            List<GameInfo> games =
                new List<GameInfo>();

            foreach (string libraryPath in _libraryPaths)
            {
                if (!Directory.Exists(libraryPath))
                    continue;

                string[] gameFolders =
                    Directory.GetDirectories(
                        libraryPath);

                foreach (string gameFolder in gameFolders)
                {
                    try
                    {
                        GameInfo game =
                            ParseGameFolder(
                                gameFolder);

                        if (game != null)
                        {
                            games.Add(game);
                        }
                    }
                    catch
                    {
                        // Ignore broken installs
                    }
                }
            }

            foreach (GameInfo game in games)
            {
                Nexus_Launcher.Services.Artwork.ArtworkService.RegisterGame(game);
            }

            return games;
        }

        private GameInfo ParseGameFolder(
    string gameFolder)
        {
            string gameName = null;

            //----------------------------------------------------
            // Try shortcut name first
            //----------------------------------------------------

            string shortcutPath =
                FindLaunchShortcut(
                    gameFolder);

            string shortcutName =
                GetShortcutName(
                    shortcutPath);

            if (!string.IsNullOrWhiteSpace(
                shortcutName))
            {
                gameName =
                    shortcutName;
            }

            //----------------------------------------------------
            // Resolve executable
            //----------------------------------------------------

            string executablePath =
                null;

            if (!string.IsNullOrWhiteSpace(
                shortcutPath))
            {
                executablePath =
                    ResolveShortcutTarget(
                        shortcutPath);
            }

            if (string.IsNullOrWhiteSpace(
                executablePath))
            {
                executablePath =
                    FindExecutable(
                        gameFolder);
            }

            if (string.IsNullOrWhiteSpace(
                executablePath) &&
                string.IsNullOrWhiteSpace(
                shortcutPath))
            {
                return null;
            }

            //----------------------------------------------------
            // Read GOG metadata
            //----------------------------------------------------

            string infoFile =
                Directory.GetFiles(
                    gameFolder,
                    "goggame-*.info",
                    SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(
                infoFile))
            {
                try
                {
                    JObject info =
                        JObject.Parse(
                            File.ReadAllText(
                                infoFile));

                    // Only use the .info name if the shortcut
                    // didn't already provide one.
                    if (string.IsNullOrWhiteSpace(
                        gameName))
                    {
                        gameName =
                            info["name"]?.ToString();
                    }
                }
                catch
                {
                }
            }

            //----------------------------------------------------
            // Final fallback
            //----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                gameName))
            {
                gameName =
                    Path.GetFileName(
                        gameFolder);
            }

            //----------------------------------------------------
            // Create GameInfo
            //----------------------------------------------------

            GameInfo game =
                new GameInfo();

            game.Name =
                gameName;

            game.ShortcutPath =
                shortcutPath;

            game.ExecutablePath =
                executablePath;

            game.InstallPath =
                gameFolder;

            game.IsInstalled =
                true;

            game.Launcher =
                "GOG";

            return game;
        }

        private string FindLaunchShortcut(
            string folder)
        {
            string shortcut =
                Directory.GetFiles(
                    folder,
                    "Launch *.lnk",
                    SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            return shortcut;
        }

        private string ResolveShortcutTarget(
            string shortcut)
        {
            try
            {
                WshShell shell =
                    new WshShell();

                IWshShortcut link =
                    (IWshShortcut)
                    shell.CreateShortcut(
                        shortcut);

                return link.TargetPath;
            }
            catch
            {
                return null;
            }
        }

        private string FindExecutable(
            string folder)
        {
            string[] exes =
                Directory.GetFiles(
                    folder,
                    "*.exe",
                    SearchOption.TopDirectoryOnly);

            foreach (string exe in exes)
            {
                string name =
                    Path.GetFileName(exe)
                    .ToLower();

                if (name.Contains("unins"))
                    continue;

                if (name.Contains("setup"))
                    continue;

                if (name.Contains("updater"))
                    continue;

                if (name.Contains("crash"))
                    continue;

                return exe;
            }

            return null;
        }
        private string GetShortcutName(
    string shortcutPath)
        {
            if (string.IsNullOrWhiteSpace(shortcutPath))
                return null;

            string name =
                Path.GetFileNameWithoutExtension(
                    shortcutPath);

            if (name.StartsWith(
                "Launch ",
                StringComparison.OrdinalIgnoreCase))
            {
                name =
                    name.Substring(7);
            }

            return name.Trim();
        }
    }
}