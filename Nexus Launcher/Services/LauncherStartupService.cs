using Nexus_Launcher.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Nexus_Launcher.Services
{
    internal static class LauncherStartupService
    {
        

[DllImport("user32.dll")]
    private static extern bool PostMessage(
    IntPtr hWnd,
    uint Msg,
    IntPtr wParam,
    IntPtr lParam);

    private const uint WM_CLOSE = 0x0010;


[DllImport("user32.dll")]
    private static extern bool ShowWindow(
    IntPtr hWnd,
    int nCmdShow);

    private const int SW_MINIMIZE = 6;
    private class LauncherInfo
        {
            public string Name { get; set; }

            public string ProcessName { get; set; }

            public string ExecutablePath { get; set; }

            public string Arguments { get; set; }

            public Func<bool> Enabled { get; set; }
            public bool CloseToTrayAfterStartup
            {
                get;
                set;
            }

            public int MinimumVisibleTime
            {
                get;
                set;
            }
        }

        public static async Task StartConfiguredLaunchersAsync()
        {
            List<LauncherInfo> launchers =
                BuildLauncherList();

            foreach (LauncherInfo launcher in launchers)
            {
                try
                {
                    if (!launcher.Enabled())
                        continue;

                    await StartLauncherIfNeededAsync(
                        launcher);

                    // Give the launcher time to initialize.
                    await Task.Delay(1000);
                }
                catch
                {
                    // Ignore failures so one launcher
                    // doesn't prevent the others.
                }
            }
        }

        private static async Task StartLauncherIfNeededAsync(
    LauncherInfo launcher)
        {
            if (string.IsNullOrWhiteSpace(
                launcher.ExecutablePath))
                return;

            if (!File.Exists(
                launcher.ExecutablePath))
                return;

            if (Process.GetProcessesByName(
                launcher.ProcessName).Any())
                return;

            ProcessStartInfo psi =
                new ProcessStartInfo
                {
                    FileName =
                        launcher.ExecutablePath,

                    Arguments =
                        launcher.Arguments,

                    UseShellExecute =
                        true,

                    WindowStyle =
                        ProcessWindowStyle.Minimized
                };

            Process.Start(
                psi);

            if (!launcher.CloseToTrayAfterStartup)
                return;

            await WaitForLauncherAndCloseAsync(
                launcher.ProcessName,
                launcher.MinimumVisibleTime);
        }
        private static async Task WaitForLauncherAndCloseAsync(
    string processName,
    int minimumVisibleTime)
        {
            DateTime? firstSeen = null;

            DateTime timeout =
                DateTime.Now.AddSeconds(30);

            while (DateTime.Now < timeout)
            {
                Process process =
                    Process
                    .GetProcessesByName(
                        processName)
                    .FirstOrDefault(
                        x =>
                        {
                            try
                            {
                                return
                                    !x.HasExited &&
                                    x.MainWindowHandle != IntPtr.Zero;
                            }
                            catch
                            {
                                return false;
                            }
                        });

                if (process == null)
                {
                    firstSeen = null;

                    await Task.Delay(500);

                    continue;
                }

                if (firstSeen == null)
                {
                    firstSeen =
                        DateTime.Now;
                }

                if ((DateTime.Now - firstSeen.Value)
                    .TotalMilliseconds
                    >= minimumVisibleTime)
                {
                    try
                    {
                        process.CloseMainWindow();
                    }
                    catch
                    {
                    }

                    return;
                }

                await Task.Delay(500);
            }
        }
        private static async Task MinimizeLauncherAsync(
    string processName)
        {
            DateTime timeout =
                DateTime.Now.AddSeconds(30);

            while (DateTime.Now < timeout)
            {
                foreach (Process process in
                    Process.GetProcessesByName(processName))
                {
                    try
                    {
                        process.Refresh();

                        if (process.HasExited)
                            continue;

                        if (process.MainWindowHandle == IntPtr.Zero)
                            continue;

                        ShowWindow(
                            process.MainWindowHandle,
                            SW_MINIMIZE);

                        return;
                    }
                    catch
                    {
                    }
                }

                await Task.Delay(500);
            }
        }
        private static async Task CloseLauncherWindowAsync(
    string processName)
        {
            const int timeoutSeconds = 20;

            DateTime end =
                DateTime.Now.AddSeconds(
                    timeoutSeconds);

            while (DateTime.Now < end)
            {
                Process[] processes =
                    Process.GetProcessesByName(
                        processName);

                foreach (Process process in processes)
                {
                    try
                    {
                        process.Refresh();

                        if (process.HasExited)
                            continue;

                        if (process.MainWindowHandle ==
                            IntPtr.Zero)
                            continue;

                        // Ask the application to close.
                        process.CloseMainWindow();

                        // If CloseMainWindow() failed,
                        // fall back to WM_CLOSE.
                        PostMessage(
                            process.MainWindowHandle,
                            WM_CLOSE,
                            IntPtr.Zero,
                            IntPtr.Zero);

                        return;
                    }
                    catch
                    {
                    }
                }

                await Task.Delay(500);
            }
        }
        private static List<LauncherInfo> BuildLauncherList()
        {
            return new List<LauncherInfo>()
            {
                new LauncherInfo
                {
                    Name = "Steam",
                    ProcessName = "steam",
                    ExecutablePath = FindSteam(),
                    Arguments = "-silent",
                    CloseToTrayAfterStartup = true,
                    MinimumVisibleTime = 5000,
                    Enabled = () =>
                        Settings.Default.StartSteam
                },

                new LauncherInfo
                {
                    Name = "Epic Games",
                    ProcessName = "EpicGamesLauncher",
                    ExecutablePath = FindEpic(),
                    Arguments = "",
                    CloseToTrayAfterStartup = true,
                    MinimumVisibleTime = 5000,
                    Enabled = () =>
                        Settings.Default.StartEpic
                },

                new LauncherInfo
                {
                    Name = "Battle.net",
                    ProcessName = "Battle.net",
                    ExecutablePath = FindBattleNet(),
                    Arguments = "",
                    CloseToTrayAfterStartup = true,
                    MinimumVisibleTime = 5000,
                    Enabled = () =>
                        Settings.Default.StartBattleNet
                },

                new LauncherInfo
                {
                    Name = "EA App",
                    ProcessName = "EADesktop",
                    ExecutablePath = FindEA(),
                    Arguments = "",
                    CloseToTrayAfterStartup = true,
                    MinimumVisibleTime = 7000,
                    Enabled = () =>
                        Settings.Default.StartEA
                },

                new LauncherInfo
                {
                    Name = "Ubisoft Connect",
                    ProcessName = "upc",
                    ExecutablePath = FindUbisoft(),
                    Arguments = "",
                    CloseToTrayAfterStartup = true,
                    MinimumVisibleTime = 5000,
                    Enabled = () =>
                        Settings.Default.StartUbisoft
                },

                new LauncherInfo
                {
                    Name = "GOG Galaxy",
                    ProcessName = "GalaxyClient",
                    ExecutablePath = FindGOG(),
                    Arguments = "",
                    CloseToTrayAfterStartup = true,
                    MinimumVisibleTime = 5000,
                    Enabled = () =>
                        Settings.Default.StartGOG
                },

                new LauncherInfo
                {
                    Name = "Xbox",
                    ProcessName = "Xbox",
                    ExecutablePath = FindXbox(),
                    Arguments = "",
                    CloseToTrayAfterStartup = true,
                    MinimumVisibleTime = 5000,
                    Enabled = () =>
                        Settings.Default.StartXbox
                }
            };
        }

        #region Executable Finders

        private static string FindSteam()
        {
            string path =
                @"C:\Program Files (x86)\Steam\steam.exe";

            return File.Exists(path)
                ? path
                : null;
        }

        private static string FindEpic()
        {
            string path =
                @"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe";

            return File.Exists(path)
                ? path
                : null;
        }

        private static string FindBattleNet()
        {
            string path =
                @"C:\Program Files (x86)\Battle.net\Battle.net Launcher.exe";

            return File.Exists(path)
                ? path
                : null;
        }

        private static string FindEA()
        {
            string path =
                @"C:\Program Files\Electronic Arts\EA Desktop\EA Desktop\EADesktop.exe";

            return File.Exists(path)
                ? path
                : null;
        }

        private static string FindUbisoft()
        {
            string path =
                @"C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\upc.exe";

            return File.Exists(path)
                ? path
                : null;
        }

        private static string FindGOG()
        {
            string path =
                @"C:\Program Files (x86)\GOG Galaxy\GalaxyClient.exe";

            return File.Exists(path)
                ? path
                : null;
        }

        private static string FindXbox()
        {
            // Microsoft Store app
            return null;
        }

        #endregion
    }
}