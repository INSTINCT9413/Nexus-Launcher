using Nexus_Launcher.Services.Library;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nexus_Launcher.Services
{
    internal static class ClientResetDefinitions
    {
        /// <summary>
        /// The reset plan for a launcher, named as the accordion and
        /// the statistics name it. Returns null for launchers that
        /// have no definition yet, which the UI reports rather than
        /// pretending to clear something.
        /// </summary>
        public static ClientResetPlan GetPlan(
            string launcher)
        {
            switch (LibraryStatsService.Canonical(launcher))
            {
                case "Steam":
                    return ResetSteam();

                case "EA App":
                    return ResetEA();

                case "Epic Games":
                    return ResetEpic();

                case "Ubisoft Connect":
                    return ResetUbisoft();

                case "GOG":
                    return ResetGOG();

                case "Battle.net":
                    return ResetBattleNet();

                default:
                    return null;
            }
        }

        /// <summary>
        /// Whether a launcher can be reset at all.
        /// </summary>
        public static bool CanReset(
            string launcher)
        {
            return GetPlan(launcher) != null;
        }

        // ============================================================
        // STEAM
        // ============================================================

        public static ClientResetPlan ResetSteam()
        {
            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            string appData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);

            return ClientResetService.BuildPlan(
                "Steam",

                // TEMP
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "Steam",
                        "htmlcache")
                },

                // CACHE
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "Steam",
                        "htmlcache"),

                    Path.Combine(
                        localAppData,
                        "Steam",
                        "appcache",
                        "httpcache"),

                    Path.Combine(
                        localAppData,
                        "Steam",
                        "appcache",
                        "librarycache")
                },

                // LOGS
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "Steam",
                        "logs"),

                    Path.Combine(
                        appData,
                        "Steam",
                        "logs")
                },

                // CRASH FILES
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "CrashDumps")
                },

                // ENTIRE FOLDERS
                new string[]
                {
                },

                // REPAIR TOOLS
                new string[]
                {
                },

                // PROCESSES
                new[]
                {
                    "steam",
                    "steamwebhelper"
                },

                clearWindowsTemp: true,
                clearDns: false);
        }


        // ============================================================
        // EA APP
        // ============================================================

        public static ClientResetPlan ResetEA()
        {
            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            string appData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);

            return ClientResetService.BuildPlan(
                "EA",

                // TEMP
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "Electronic Arts",
                        "EA Desktop",
                        "cache")
                },

                // CACHE
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "Electronic Arts",
                        "EA Desktop",
                        "cache"),

                    Path.Combine(
                        localAppData,
                        "Electronic Arts",
                        "EA Desktop",
                        "Cache"),

                    Path.Combine(
                        localAppData,
                        "Electronic Arts",
                        "EA Desktop",
                        "WebCache"),

                    Path.Combine(
                        localAppData,
                        "Electronic Arts",
                        "EA Desktop",
                        "webcache")
                },

                // LOGS
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "Electronic Arts"),

                    Path.Combine(
                        appData,
                        "Electronic Arts")
                },

                // CRASH FILES
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "CrashDumps")
                },

                // ENTIRE FOLDERS
                new string[]
                {
                },

                // REPAIR TOOLS
                new string[]
                {
                },

                // PROCESSES
                new[]
                {
                    "EADesktop",
                    "EABackgroundService",
                    "EALauncher"
                },

                clearWindowsTemp: true,
                clearDns: false);
        }


        // ============================================================
        // EPIC GAMES
        // ============================================================

        public static ClientResetPlan ResetEpic()
        {
            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            string appData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);

            return ClientResetService.BuildPlan(
                "Epic Games",

                // TEMP
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "EpicGamesLauncher",
                        "Saved",
                        "webcache")
                },

                // CACHE
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "EpicGamesLauncher",
                        "Saved",
                        "webcache"),

                    Path.Combine(
                        localAppData,
                        "EpicGamesLauncher",
                        "Saved",
                        "webcache_4147"),

                    Path.Combine(
                        localAppData,
                        "EpicGamesLauncher",
                        "Saved",
                        "webcache_4430")
                },

                // LOGS
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "EpicGamesLauncher",
                        "Saved",
                        "Logs")
                },

                // CRASH FILES
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "CrashDumps")
                },

                // ENTIRE FOLDERS
                new string[]
                {
                },

                // REPAIR TOOLS
                new string[]
                {
                },

                // PROCESSES
                new[]
                {
                    "EpicGamesLauncher",
                    "EpicWebHelper"
                },

                clearWindowsTemp: true,
                clearDns: false);
        }


        // ============================================================
        // UBISOFT CONNECT
        // ============================================================

        public static ClientResetPlan ResetUbisoft()
        {
            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            string appData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);

            return ClientResetService.BuildPlan(
                "Ubisoft Connect",

                // TEMP
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "Ubisoft Game Launcher",
                        "cache")
                },

                // CACHE
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "Ubisoft Game Launcher",
                        "cache"),

                    Path.Combine(
                        localAppData,
                        "Ubisoft Game Launcher",
                        "webcache")
                },

                // LOGS
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "Ubisoft Game Launcher",
                        "logs"),

                    Path.Combine(
                        appData,
                        "Ubisoft Game Launcher",
                        "logs")
                },

                // CRASH FILES
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "CrashDumps")
                },

                // ENTIRE FOLDERS
                new string[]
                {
                },

                // REPAIR TOOLS
                new string[]
                {
                },

                // PROCESSES
                new[]
                {
                    "upc",
                    "UbisoftConnect",
                    "UbisoftConnectCore"
                },

                clearWindowsTemp: true,
                clearDns: false);
        }


        // ============================================================
        // GOG GALAXY
        // ============================================================

        public static ClientResetPlan ResetGOG()
        {
            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            string appData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);

            return ClientResetService.BuildPlan(
                "GOG Galaxy",

                // TEMP
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "GOG.com",
                        "Galaxy",
                        "webcache")
                },

                // CACHE
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "GOG.com",
                        "Galaxy",
                        "webcache"),

                    Path.Combine(
                        localAppData,
                        "GOG.com",
                        "Galaxy",
                        "Cache")
                },

                // LOGS
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "GOG.com",
                        "Galaxy",
                        "logs"),

                    Path.Combine(
                        appData,
                        "GOG.com",
                        "Galaxy",
                        "logs")
                },

                // CRASH FILES
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "CrashDumps")
                },

                // ENTIRE FOLDERS
                new string[]
                {
                },

                // REPAIR TOOLS
                new string[]
                {
                },

                // PROCESSES
                new[]
                {
                    "GalaxyClient",
                    "GalaxyClientService"
                },

                clearWindowsTemp: true,
                clearDns: false);
        }


        // ============================================================
        // BATTLE.NET
        // ============================================================

        public static ClientResetPlan ResetBattleNet()
        {
            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            string appData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);

            return ClientResetService.BuildPlan(
                "Battle.net",

                // TEMP
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "Battle.net")
                },

                // CACHE
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "Battle.net"),

                    Path.Combine(
                        localAppData,
                        "Blizzard Entertainment",
                        "Battle.net")
                },

                // LOGS
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "Battle.net",
                        "Logs"),

                    Path.Combine(
                        appData,
                        "Battle.net",
                        "Logs")
                },

                // CRASH FILES
                new[]
                {
                    Path.Combine(
                        localAppData,
                        "CrashDumps")
                },

                // ENTIRE FOLDERS
                new string[]
                {
                },

                // REPAIR TOOLS
                new string[]
                {
                },

                // PROCESSES
                new[]
                {
                    "Battle.net",
                    "Agent",
                    "AgentSwitcher"
                },

                clearWindowsTemp: true,
                clearDns: false);
        }
    }
}