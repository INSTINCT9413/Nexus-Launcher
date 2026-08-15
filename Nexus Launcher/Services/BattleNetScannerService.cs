using Newtonsoft.Json;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Nexus_Launcher.Services
{
        internal class BattleNetScannerService
        {
            private string AggregatePath = MainView.sysDisk +
            @"ProgramData\Battle.net\Agent\aggregate.json";

    private string ReadAggregateJson()
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        return File.ReadAllText(
                            AggregatePath);
                    }
                    catch
                    {
                        Thread.Sleep(500);
                    }
                }

                return null;
            }

            public List<GameInfo> ScanGames()
            {
                List<GameInfo> games =
                    new List<GameInfo>();

                if (!File.Exists(
                    AggregatePath))
                {
                    return games;
                }

                string json =
                    ReadAggregateJson();

                if (string.IsNullOrWhiteSpace(
                    json))
                {
                    return games;
                }

                BattleNetRoot data =
                    JsonConvert.DeserializeObject<
                        BattleNetRoot>(json);

                if (data?.installed == null)
                {
                    return games;
                }

                foreach (BattleNetGame gameData
                    in data.installed)
                {
                    try
                    {
                        GameInfo game =
                            new GameInfo();

                        game.Name =
                            gameData.name;

                        game.ProductId =
                            gameData.product_id;

                        string aggregatePath =
                            gameData.icon_path
                                ?.Replace("/", "\\");

                        game.ExecutablePath =
                            FindExecutable(
                                aggregatePath,
                                gameData.name);

                        if (!string.IsNullOrWhiteSpace(
                            game.ExecutablePath))
                        {
                            game.InstallPath =
                                Path.GetDirectoryName(
                                    game.ExecutablePath);
                        }
                    game.Launcher = "Battle.net";
                        game.LaunchUri =
                            gameData.launch_uri;

                        game.HeaderImageUrl =
                            gameData.box_art_uri;

                        game.LogoUrl =
                            gameData.logo_art_uri;

                        try
                        {
                            BattleNetArtworkHelper
                                .DownloadArtwork(
                                    game);
                        }
                        catch
                        {
                        }

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

            private string FindExecutable(
                string executablePathFromJson,
                string gameName)
            {
                if (string.IsNullOrWhiteSpace(
                    executablePathFromJson))
                {
                    return null;
                }

                string installFolder =
                    Path.GetDirectoryName(
                        executablePathFromJson);

                if (!Directory.Exists(
                    installFolder))
                {
                    return executablePathFromJson;
                }

                try
                {
                    List<FileInfo> exes =
                        Directory.GetFiles(
                            installFolder,
                            "*.exe",
                            SearchOption.TopDirectoryOnly)
                        .Select(x =>
                            new FileInfo(x))
                        .ToList();

                    exes.RemoveAll(x =>
                    {
                        string name =
                            x.Name
                            .ToLowerInvariant();

                        return
                            name.Contains("launcher") ||
                            name.Contains("launch") ||
                            name.Contains("boot") ||
                            name.Contains("bootstrap") ||
                            name.Contains("updater") ||
                            name.Contains("update") ||
                            name.Contains("repair") ||
                            name.Contains("crash") ||
                            name.Contains("report") ||
                            name.Contains("support") ||
                            name.Contains("helper") ||
                            name.Contains("agent") ||
                            name.Contains("battle.net") ||
                            name.Contains("editor") ||
                            name.Contains("vc_redist");
                    });

                    if (exes.Count == 0)
                    {
                        return executablePathFromJson;
                    }

                    string normalizedGameName =
                        gameName
                        .Replace(":", "")
                        .Replace("-", "")
                        .Replace("_", "")
                        .Replace(" ", "")
                        .ToLowerInvariant();

                    FileInfo matchingExe =
                        exes.FirstOrDefault(x =>
                        {
                            string exeName =
                                Path.GetFileNameWithoutExtension(
                                    x.Name)
                                .Replace(":", "")
                                .Replace("-", "")
                                .Replace("_", "")
                                .Replace(" ", "")
                                .ToLowerInvariant();

                            return exeName.Contains(
                                normalizedGameName);
                        });

                    if (matchingExe != null)
                    {
                        return matchingExe.FullName;
                    }

                    FileInfo largestExe =
                        exes
                        .OrderByDescending(
                            x => x.Length)
                        .FirstOrDefault();

                    return largestExe?.FullName;
                }
                catch
                {
                    return executablePathFromJson;
                }
            }
        }

        public class BattleNetRoot
        {
            public List<BattleNetGame> installed
            {
                get;
                set;
            }
        }

        public class BattleNetGame
        {
            public string name
            {
                get;
                set;
            }

            public string product_id
            {
                get;
                set;
            }

            public string icon_path
            {
                get;
                set;
            }

            public string launch_uri
            {
                get;
                set;
            }

            public string box_art_uri
            {
                get;
                set;
            }

            public string logo_art_uri
            {
                get;
                set;
            }
        }

}
