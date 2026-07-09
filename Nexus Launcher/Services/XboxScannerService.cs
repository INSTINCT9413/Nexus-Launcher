using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Nexus_Launcher.Services
{
    internal class XboxScannerService
    {
        public List<GameInfo> ScanGames()
        {
            List<GameInfo> games =
                new List<GameInfo>();

            ProcessStartInfo psi =
                new ProcessStartInfo();

            psi.FileName =
                "powershell.exe";

            psi.Arguments =
                "-NoProfile -ExecutionPolicy Bypass Get-StartApps";

            psi.RedirectStandardOutput =
                true;

            psi.UseShellExecute =
                false;

            psi.CreateNoWindow =
                true;

            using (Process process =
                Process.Start(psi))
            {
                string output =
                    process.StandardOutput.ReadToEnd();

                process.WaitForExit();

                string[] lines =
                    output.Split(
                        new[]
                        {
                            Environment.NewLine
                        },
                        StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    try
                    {
                        if (line.StartsWith("Name"))
                            continue;

                        if (line.StartsWith("-"))
                            continue;

                        int split =
                            line.LastIndexOf(
                                ' ');

                        if (split <= 0)
                            continue;

                        string appId =
                            line.Substring(
                                split)
                            .Trim();

                        string name =
                            line.Substring(
                                0,
                                split)
                            .Trim();

                        if (!IsXboxGame(
                            name))
                        {
                            continue;
                        }

                        GameInfo game =
                            new GameInfo();

                        game.Name =
                            name;

                        game.AppUserModelId =
                            appId;

                        game.Launcher =
                            "Xbox";

                        game.IsInstalled =
                            true;

                        games.Add(
                            game);
                    }
                    catch
                    {
                    }
                }
            }

            return games;
        }

        private bool IsXboxGame(
            string name)
        {
            string lower =
                name.ToLower();

            string[] excluded =
            {
                "calculator",
                "photos",
                "camera",
                "notepad",
                "paint",
                "mail",
                "store",
                "terminal",
                "xbox",
                "gaming services",
                "game bar",
                "feedback hub",
                "clipchamp",
                "clock",
                "weather",
                "maps"
            };

            return !excluded.Any(
                x => lower.Contains(x));
        }
    }
}