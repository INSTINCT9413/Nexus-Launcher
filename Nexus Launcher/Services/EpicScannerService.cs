using Newtonsoft.Json;
using Nexus_Launcher.Models;
using System.Collections.Generic;
using System.IO;

namespace Nexus_Launcher.Services
{
    internal class EpicScannerService
    {
        private string ManifestPath = MainView.epicPath2;
            

        public List<GameInfo> ScanGames()
        {
            List<GameInfo> games =
                new List<GameInfo>();

            if (!Directory.Exists(
                ManifestPath))
            {
                return games;
            }

            string[] files =
                Directory.GetFiles(
                    ManifestPath,
                    "*.item");

            foreach (string file in files)
            {
                try
                {
                    string json =
                        File.ReadAllText(file);

                    EpicManifest manifest =
                        JsonConvert.DeserializeObject
                        <EpicManifest>(json);

                    if (manifest == null)
                        continue;

                    GameInfo game =
                        new GameInfo();
                    game.Launcher = "Epic Games";
                    game.Name =
                        manifest.DisplayName;

                    game.InstallPath =
                        manifest.InstallLocation;

                    game.ExecutablePath =
                        Path.Combine(
                            manifest.InstallLocation,
                            manifest.LaunchExecutable);
                    game.epicLauncherAppId =
                    manifest.AppName;

                    game.IsInstalled =
                        Directory.Exists(
                            manifest.InstallLocation);

                    games.Add(game);
                }
                catch
                {

                }
            }

            foreach (GameInfo game in games)
            {
                Nexus_Launcher.Services.Artwork.ArtworkService.RegisterGame(game);
            }

            return games;
        }
    }
}