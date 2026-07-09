using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus_Launcher.Services
{
    public class LauncherScannerService
    {
        public List<LauncherInfo> ScanLaunchers()
        {
            List<LauncherInfo> launchers =
                new List<LauncherInfo>();

            ScanSteam(launchers);

            return launchers;
        }

        private void ScanSteam(List<LauncherInfo> launchers)
        {
            string steamPath = MainView.sysDisk +
                @"Program Files (x86)\Steam";

            if (!Directory.Exists(steamPath))
                return;

            LauncherInfo steam =
                new LauncherInfo();

            steam.Name = "Steam";
            steam.InstallPath = steamPath;
            steam.ExecutablePath =
                Path.Combine(steamPath, "steam.exe");

            steam.Games = ScanSteamGames(steamPath);

            launchers.Add(steam);
        }

        private List<GameInfo> ScanSteamGames(string steamPath)
        {
            List<GameInfo> games =
                new List<GameInfo>();

            string commonPath =
                Path.Combine(steamPath,
                "steamapps",
                "common");

            if (!Directory.Exists(commonPath))
                return games;

            string[] directories =
                Directory.GetDirectories(commonPath);

            foreach (string dir in directories)
            {
                string exe =
                    FindMainExecutable(dir);

                GameInfo game =
                    new GameInfo();

                game.Name =
                    Path.GetFileName(dir);

                game.InstallPath = dir;
                game.ExecutablePath = exe;
                game.IsInstalled = true;

                games.Add(game);
            }

            return games;
        }

        private string FindMainExecutable(string folder)
        {
            string[] exes =
                Directory.GetFiles(
                    folder,
                    "*.exe",
                    SearchOption.AllDirectories);

            List<string> filtered =
                exes.Where(x =>
                    !x.ToLower().Contains("uninstall") &&
                    !x.ToLower().Contains("setup") &&
                    !x.ToLower().Contains("crash"))
                .ToList();

            if (filtered.Count == 0)
                return null;

            return filtered
                .OrderByDescending(x =>
                    new FileInfo(x).Length)
                .FirstOrDefault();
        }
    }
}
