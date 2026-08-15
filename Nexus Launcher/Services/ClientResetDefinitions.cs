using System;
using System.Collections.Generic;
using System.IO;

namespace Nexus_Launcher.Services
{
    internal static class ClientResetDefinitions
    {
        // ============================================================
        // STEAM
        // ============================================================

        public static ClientResetService.ResetResult ResetSteam()
        {
            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            string appData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);

            return ClientResetService.ResetClient(
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

        public static ClientResetService.ResetResult ResetEA()
        {
            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            string appData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);

            return ClientResetService.ResetClient(
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

        public static ClientResetService.ResetResult ResetEpic()
        {
            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            string appData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);

            return ClientResetService.ResetClient(
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

        public static ClientResetService.ResetResult ResetUbisoft()
        {
            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            string appData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);

            return ClientResetService.ResetClient(
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

        public static ClientResetService.ResetResult ResetGOG()
        {
            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            string appData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);

            return ClientResetService.ResetClient(
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

        public static ClientResetService.ResetResult ResetBattleNet()
        {
            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            string appData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);

            return ClientResetService.ResetClient(
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