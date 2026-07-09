using IWshRuntimeLibrary;
using Microsoft.Win32;
using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nexus_Launcher.Services
{
    internal class EAScannerService
    {
        private const string RegistryPath =
            @"SOFTWARE\WOW6432Node\EA Games";

        public List<GameInfo> ScanGames()
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

                            string displayName =
                                gameKey.GetValue(
                                    "DisplayName")
                                ?.ToString();

                            string installDir =
                                gameKey.GetValue(
                                    "Install Dir")
                                ?.ToString();

                            if (string.IsNullOrWhiteSpace(
                                displayName))
                            {
                                displayName =
                                    subKeyName;
                            }

                            GameInfo game =
                                new GameInfo();

                            game.Name =
                                displayName;

                            game.ProductId =
                                subKeyName;

                            game.InstallPath =
                                installDir;

                            game.Launcher =
                                "EA";

                            game.IsInstalled =
                                true;

                            string shortcut =
                                FindShortcut(
                                    displayName);

                            if (!string.IsNullOrWhiteSpace(
                                shortcut))
                            {
                                game.ShortcutPath =
                                    shortcut;

                                game.ExecutablePath =
                                    shortcut;
                            }
                            else
                            {
                                game.ExecutablePath =
                                    FindExecutable(
                                        installDir);
                            }

                            games.Add(game);
                        }
                    }
                    catch
                    {
                    }
                }
            }

            foreach (GameInfo game in games)
            {
                Nexus_Launcher.Services.Artwork.ArtworkService.RegisterGame(game);
            }

            return games;
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