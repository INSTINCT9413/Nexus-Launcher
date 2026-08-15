using DevExpress.XtraEditors;
using Newtonsoft.Json;
using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Services
{
    internal class EpicScannerService
    {
        private string ManifestPath =
            MainView.epicPath2;

        public List<GameInfo> ScanGames()
        {
            List<GameInfo> games =
                new List<GameInfo>();

            //------------------------------------------------
            // Make sure Epic manifest directory exists
            //------------------------------------------------

            if (!Directory.Exists(
                ManifestPath))
            {
                return games;
            }

            //------------------------------------------------
            // Read Epic .item files
            //------------------------------------------------

            string[] files;

            try
            {
                files =
                    Directory.GetFiles(
                        ManifestPath,
                        "*.item",
                        SearchOption.TopDirectoryOnly);
            }
            catch
            {
                return games;
            }

            //------------------------------------------------
            // Read each manifest
            //------------------------------------------------

            foreach (string file in files)
            {
                try
                {
                    string json =
                        File.ReadAllText(file);

                    EpicManifest manifest =
                        JsonConvert.DeserializeObject<EpicManifest>(
                            json);

                    if (manifest == null)
                        continue;

                    //------------------------------------------------
                    // Basic validation
                    //------------------------------------------------

                    if (string.IsNullOrWhiteSpace(
                        manifest.DisplayName))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(
                        manifest.InstallLocation))
                    {
                        continue;
                    }

                    //------------------------------------------------
                    // Make sure the game is actually installed
                    //------------------------------------------------

                    if (!Directory.Exists(
                        manifest.InstallLocation))
                    {
                        continue;
                    }

                    //------------------------------------------------
                    // Create GameInfo
                    //------------------------------------------------

                    GameInfo game =
                        new GameInfo();

                    game.Launcher =
                        "Epic Games";

                    game.Name =
                        manifest.DisplayName;

                    game.InstallPath =
                        manifest.InstallLocation;

                    //------------------------------------------------
                    // Epic App ID
                    //------------------------------------------------

                    game.epicLauncherAppId =
                        manifest.AppName;
                    
                    //------------------------------------------------
                    // Also populate ProductId
                    //
                    // This gives the uninstall system a common
                    // identifier to work with.
                    //------------------------------------------------

                    game.ProductId =
                        manifest.AppName;

                    //------------------------------------------------
                    // Executable
                    //------------------------------------------------

                    if (!string.IsNullOrWhiteSpace(
                        manifest.LaunchExecutable))
                    {
                        string executable =
                            Path.Combine(
                                manifest.InstallLocation,
                                manifest.LaunchExecutable);

                        if (File.Exists(
                            executable))
                        {
                            game.ExecutablePath =
                                executable;
                        }
                    }

                    //------------------------------------------------
                    // Installed
                    //------------------------------------------------

                    game.IsInstalled =
                        true;

                    //------------------------------------------------
                    // Prevent duplicate games
                    //------------------------------------------------

                    bool duplicate =
                        games.Any(
                            x =>
                                IsSameEpicGame(
                                    x,
                                    game));

                    if (duplicate)
                        continue;

                    games.Add(game);
                }
                catch
                {
                    // Ignore invalid Epic manifests.
                }
            }

            //------------------------------------------------
            // Register artwork
            //------------------------------------------------

            foreach (GameInfo game in games)
            {
                Nexus_Launcher.Services.Artwork.ArtworkService
                    .RegisterGame(game);
            }

            return games;
        }

        //----------------------------------------------------
        // Determine whether two Epic games are the same
        //----------------------------------------------------

        private bool IsSameEpicGame(
            GameInfo first,
            GameInfo second)
        {
            //------------------------------------------------
            // Best match: Epic App ID
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                first.epicLauncherAppId) &&
                !string.IsNullOrWhiteSpace(
                second.epicLauncherAppId))
            {
                if (string.Equals(
                    first.epicLauncherAppId,
                    second.epicLauncherAppId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            //------------------------------------------------
            // Second match: installation path
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                first.InstallPath) &&
                !string.IsNullOrWhiteSpace(
                second.InstallPath))
            {
                string firstPath =
                    NormalizePath(
                        first.InstallPath);

                string secondPath =
                    NormalizePath(
                        second.InstallPath);

                if (string.Equals(
                    firstPath,
                    secondPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            //------------------------------------------------
            // Third match: normalized game name
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                first.Name) &&
                !string.IsNullOrWhiteSpace(
                second.Name))
            {
                string firstName =
                    NormalizeGameName(
                        first.Name);

                string secondName =
                    NormalizeGameName(
                        second.Name);

                if (string.Equals(
                    firstName,
                    secondName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        //----------------------------------------------------
        // Normalize installation path
        //----------------------------------------------------

        private string NormalizePath(
            string path)
        {
            if (string.IsNullOrWhiteSpace(
                path))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetFullPath(
                    path)
                    .TrimEnd(
                        '\\',
                        '/');
            }
            catch
            {
                return path
                    .Trim()
                    .TrimEnd(
                        '\\',
                        '/');
            }
        }

        //----------------------------------------------------
        // Normalize game names
        //
        // Handles:
        //
        // Need For Speed™ Most Wanted
        // Need For Speed(TM) Most Wanted
        //
        //----------------------------------------------------

        private string NormalizeGameName(
            string name)
        {
            if (string.IsNullOrWhiteSpace(
                name))
            {
                return string.Empty;
            }

            string normalized =
                name;

            normalized =
                normalized.Replace(
                    "™",
                    "");

            normalized =
                normalized.Replace(
                    "®",
                    "");

            normalized =
                normalized.Replace(
                    "©",
                    "");

            normalized =
                normalized.Replace(
                    "(TM)",
                    "");

            normalized =
                normalized.Replace(
                    "(tm)",
                    "");

            normalized =
                normalized.Replace(
                    "(R)",
                    "");

            normalized =
                normalized.Replace(
                    "(r)",
                    "");

            normalized =
                normalized.Replace(
                    "(C)",
                    "");

            normalized =
                normalized.Replace(
                    "(c)",
                    "");

            //------------------------------------------------
            // Normalize whitespace
            //------------------------------------------------

            while (normalized.Contains(
                "  "))
            {
                normalized =
                    normalized.Replace(
                        "  ",
                        " ");
            }

            return normalized.Trim();
        }
    }
}