using DevExpress.Office.Drawing;
using DevExpress.XtraSplashScreen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexus_Launcher.Models;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services;
using Nexus_Launcher.Services.Themes;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Nexus_Launcher
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        public static Mutex _mutex;

        /// <summary>
        /// True once this process owns the single instance lock, so it
        /// is only released by the instance that took it.
        /// </summary>
        private static bool _ownsInstanceLock;

        private static bool _shuttingDown;

        /// <summary>
        /// Passed to the new process on a restart, followed by the id
        /// of the process it is replacing.
        ///
        /// Without it the new instance reaches the single instance
        /// check while the old one is still closing, decides Nexus is
        /// already running, and asks the user what to do about a second
        /// copy they never started.
        /// </summary>
        private const string RestartArgument = "--restart";
        public static MainView MainFormInstance;
        private static EventWaitHandle _showEvent;

        /// <summary>
        /// Signalled by a second instance that was started to open a
        /// theme file.
        /// </summary>
        private static EventWaitHandle _openThemeEvent;
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int SW_RESTORE = 9;
        [STAThread]
        static void Main()
        {
            

            
            try
            {
                //if (File.Exists(Application.StartupPath + @"\latest.json"))
                //{
                //    File.Delete(Application.StartupPath + @"\latest.json");
                //}
                if (!Settings.Default.SettingsUpgraded)
                {
                    Settings.Default.Upgrade();
                    

                    Settings.Default.SettingsUpgraded = true;
                    
                    Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                LogCrash(ex);
                System.Diagnostics.Debug.WriteLine(
                    "Settings upgrade failed: " + ex);

                // Optional:
                // Settings.Default.Reset();
                // Settings.Default.SettingsUpgraded = true;
                // Settings.Default.Save();
            }

            // Custom palettes have to be back in their skins before
            // anything applies the saved theme. Both WaitForm1 and
            // MainView do that on startup, so it happens here rather
            // than in either of them.
            Nexus_Launcher.Services.Themes.CustomThemeService
                .RegisterAll();

            Application.SetUnhandledExceptionMode(
                UnhandledExceptionMode.CatchException);

            Application.ThreadException +=
                Application_ThreadException;

            AppDomain.CurrentDomain.UnhandledException +=
                CurrentDomain_UnhandledException;

            try
            {
                MainInternal();
                
            }
            catch (Exception ex)
            {
                LogCrash(ex);

                MessageBox.Show(
                    ex.ToString(),
                    "Fatal Startup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Waits for the instance being replaced to actually exit.
        ///
        /// Returns true when this process was started by a restart, so
        /// the caller knows to be patient with the instance lock even
        /// if the wait timed out.
        /// </summary>
        private static bool WaitForRestartHandoff()
        {
            int previousId = 0;

            try
            {
                string[] args =
                    Environment.GetCommandLineArgs();

                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (string.Equals(
                        args[i],
                        RestartArgument,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(args[i + 1], out previousId);

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogCrash(ex);
            }

            if (previousId <= 0)
                return false;

            try
            {
                using (Process previous =
                    Process.GetProcessById(previousId))
                {
                    previous.WaitForExit(20000);
                }
            }
            catch (ArgumentException)
            {
                // Already gone, which is the outcome we wanted.
            }
            catch (Exception ex)
            {
                LogCrash(ex);
            }

            return true;
        }

        /// <summary>
        /// Starts a fresh copy of Nexus and closes this one.
        ///
        /// Replaces Application.Restart, which starts the new process
        /// immediately and leaves the two overlapping.
        /// </summary>
        public static void RestartCleanly()
        {
            try
            {
                ProcessStartInfo info =
                    new ProcessStartInfo(Application.ExecutablePath);

                info.Arguments =
                    RestartArgument + " " +
                    Process.GetCurrentProcess().Id;

                info.UseShellExecute = true;

                info.WorkingDirectory =
                    Path.GetDirectoryName(Application.ExecutablePath);

                Process.Start(info);
            }
            catch (Exception ex)
            {
                LogCrash(ex);

                MessageBox.Show(
                    "Nexus Launcher could not restart itself." +
                        Environment.NewLine + Environment.NewLine +
                        ex.Message,
                    "Nexus Launcher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            ShutdownCleanly();
        }

        /// <summary>
        /// Ends the process in an orderly way: forms get their closing
        /// handlers, which is where the theme is saved and the unlock
        /// popups are dismissed, and the instance lock is handed back
        /// rather than left for Windows to reclaim.
        /// </summary>
        public static void ShutdownCleanly()
        {
            if (_shuttingDown)
                return;

            _shuttingDown = true;

            try
            {
                // Otherwise "close to tray" would cancel the close and
                // the process would stay up.
                if (MainFormInstance != null &&
                    !MainFormInstance.IsDisposed)
                {
                    MainFormInstance.appExit = true;
                }
            }
            catch (Exception ex)
            {
                LogCrash(ex);
            }

            try
            {
                foreach (Form form in
                    Application.OpenForms.Cast<Form>().ToList())
                {
                    try
                    {
                        form.Close();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                LogCrash(ex);
            }

            ReleaseInstanceLock();

            // A form that refuses to close, or a modal dialog still up,
            // would otherwise leave the process running invisibly.
            Task.Delay(4000).ContinueWith(x => Environment.Exit(0));

            Application.Exit();
        }

        public static void ReleaseInstanceLock()
        {
            try
            {
                if (_mutex == null)
                    return;

                if (_ownsInstanceLock)
                {
                    _mutex.ReleaseMutex();

                    _ownsInstanceLock = false;
                }

                _mutex.Dispose();

                _mutex = null;
            }
            catch (Exception ex)
            {
                LogCrash(ex);
            }
        }

        private static void MainInternal()
        {
            if (!Settings.Default.Setup)
            {
                FirstTimeSetupForm setupForm =
                    new FirstTimeSetupForm();

                setupForm.ShowDialog();
                return;
            }

            // Files named on the command line. A double click on a
            // theme file starts a whole second Nexus, and what the
            // user wanted was for the one already running to open it.
            List<string> themeFiles =
                ThemeFileAssociation.FilesFromCommandLine();

            bool restarting =
                WaitForRestartHandoff();

            _mutex = new Mutex(
                false,
                "NexusLauncher_SingleInstance");

            // Waiting rather than failing straight away. A restart has
            // just asked the old instance to go, and even a plain
            // double click can land two launches close enough together
            // to race.
            _ownsInstanceLock =
                TryTakeInstanceLock(
                    restarting
                        ? TimeSpan.FromSeconds(25)
                        : TimeSpan.FromSeconds(3));

            if (!_ownsInstanceLock && themeFiles.Count > 0)
            {
                // Hand the file to the copy that is already running and
                // say nothing. Asking "Nexus is already running, what
                // would you like to do?" because someone opened a theme
                // would be a strange thing to do to them.
                if (ThemeFileAssociation.HandOff(themeFiles))
                    return;
            }

            if (!_ownsInstanceLock)
            {
                var mess = MessageBox.Show(
                    "Nexus Launcher is already running.\n\r\n\rDo you want to show the existing instance?(Yes)\n\r\n\rDo you want to force close the existing instance\n\rand restart?(No)\n\r\n\rDo you want to cancel and do nothing?(Cancel)",
                    "Nexus Launcher",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Information);

                if (mess == DialogResult.Yes)
                {
                    // Bring the copy that is already running forward
                    // and leave it at that.
                    try
                    {
                        EventWaitHandle
                            .OpenExisting(
                                "NexusLauncher_Show")
                            .Set();
                    }
                    catch
                    {
                    }

                    return;
                }

                if (mess != DialogResult.No)
                {
                    // Cancel.
                    return;
                }

                if (!ForceCloseOtherInstances())
                {
                    MessageBox.Show(
                        "The running copy of Nexus Launcher could not " +
                            "be closed.",
                        "Nexus Launcher",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                // The lock is free now, so this process carries on
                // starting rather than spawning yet another one.
                _ownsInstanceLock = true;
            }

            _showEvent =
                new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    "NexusLauncher_Show");

            _openThemeEvent =
                new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    ThemeFileAssociation.OpenEventName);

            // Anything left over from a crash is stale by now, and
            // importing it at the next start would be a surprise.
            ThemeFileAssociation.ClearInbox();

            // Claims .nexustheme on first run, and re-points it at
            // this executable if Nexus has moved since. Does nothing
            // once the user has turned the association off.
            ThemeFileAssociation.ApplyStartupPreference();

            SplashScreenManager.ShowForm(MainFormInstance, typeof(WaitForm1), true, true, false);

            //Batteries.Init();
            //Batteries_V2.Init();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //UpgradeSettings();

            if (Settings.Default.GOGLibraryPaths == null)
            {
                Settings.Default.GOGLibraryPaths =
                    new System.Collections.Specialized.StringCollection();

                Settings.Default.Save();
            }

            var builder =
                Host.CreateDefaultBuilder()
                .ConfigureServices(
                    (context, services) =>
                    {
                        services.AddSingleton<LauncherInfo>();
                        services.AddSingleton<LauncherDefinition>();
                        services.AddSingleton<GameInfo>();
                        services.AddSingleton<EpicManifest>();
                        services.AddSingleton<EpicScannerService>();
                        services.AddSingleton<LauncherScannerService>();
                        services.AddSingleton<SteamScannerService>();
                        services.AddSingleton<MainView>();
                        services.AddSingleton<MainViewModel>();
                        services.AddSingleton<GOGScannerService>();
                    });

            var host =
                builder.Build();

            MainFormInstance =
                host.Services.GetRequiredService<MainView>();

            Task.Run(() =>
            {
                while (true)
                {
                    _showEvent.WaitOne();

                    MainFormInstance?.RestoreLauncher();
                }
            });

            Task.Run(() =>
            {
                while (true)
                {
                    _openThemeEvent.WaitOne();

                    MainFormInstance?.OpenThemeFiles(
                        ThemeFileAssociation.TakeInbox());
                }
            });

            // A file this instance was started with, rather than handed.
            if (themeFiles.Count > 0)
            {
                List<string> startupFiles = themeFiles;

                MainFormInstance.Shown += (s, e) =>
                    MainFormInstance.OpenThemeFiles(startupFiles);
            }

            Application.Run(
                MainFormInstance);

            ReleaseInstanceLock();
        }
        private static bool TryTakeInstanceLock(
            TimeSpan timeout)
        {
            try
            {
                return _mutex.WaitOne(timeout, false);
            }
            catch (AbandonedMutexException)
            {
                // The previous owner died without releasing it, which
                // still leaves this process holding the lock.
                return true;
            }
            catch (Exception ex)
            {
                LogCrash(ex);

                return false;
            }
        }

        /// <summary>
        /// Ends every other copy of Nexus and waits for the lock they
        /// were holding.
        /// </summary>
        private static bool ForceCloseOtherInstances()
        {
            try
            {
                Process current =
                    Process.GetCurrentProcess();

                foreach (Process other in
                    Process.GetProcessesByName(current.ProcessName))
                {
                    using (other)
                    {
                        if (other.Id == current.Id)
                            continue;

                        try
                        {
                            other.Kill();

                            other.WaitForExit(10000);
                        }
                        catch (Exception ex)
                        {
                            LogCrash(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogCrash(ex);

                return false;
            }

            return TryTakeInstanceLock(TimeSpan.FromSeconds(10));
        }

        private static void UpgradeSettings()
        {
            string currentVersion =
        Application.ProductVersion;

            if (Properties.Settings.Default.LastVersion != currentVersion)
            {
                try
                {
                    Properties.Settings.Default.Upgrade();
                }
                catch
                {
                }

                Properties.Settings.Default.LastVersion =
                    currentVersion;

                Properties.Settings.Default.Save();
            }
        }
        private static void Application_ThreadException(
    object sender,
    ThreadExceptionEventArgs e)
        {
            LogCrash(e.Exception);

            MessageBox.Show(
                e.Exception.ToString(),
                "Unhandled UI Exception",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(
            object sender,
            UnhandledExceptionEventArgs e)
        {
            Exception ex =
                e.ExceptionObject as Exception;

            if (ex != null)
            {
                LogCrash(ex);

                MessageBox.Show(
                    ex.ToString(),
                    "Unhandled Exception",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public static void LogCrash(
            Exception ex)
        {
            try
            {
                string settingsFolder =
    Path.GetDirectoryName(
        ConfigurationManager
            .OpenExeConfiguration(
                ConfigurationUserLevel.PerUserRoamingAndLocal)
            .FilePath);
                string logFolder = settingsFolder;
    

                Directory.CreateDirectory(logFolder);

                string logFile =
                    Path.Combine(
                        logFolder,
                        "CrashLog.txt");
                
                StringBuilder sb =
                    new StringBuilder();

                sb.AppendLine("===== Nexus Launcher Crash & Unhandled Events Log =====");
                sb.AppendLine("Log Time: " + DateTime.Now.ToString());
                sb.AppendLine();

                sb.AppendLine("Application Version:");
                sb.AppendLine(Application.ProductVersion);
                sb.AppendLine();
                sb.AppendLine("Build:");
                sb.AppendLine(BuildInfo.Build.ToString());
                sb.AppendLine();

                sb.AppendLine("OS Version:");
                sb.AppendLine(Environment.OSVersion.ToString());
                sb.AppendLine();

                sb.AppendLine(".NET Version:");
                sb.AppendLine(Environment.Version.ToString());
                sb.AppendLine();

                sb.AppendLine("64-Bit OS: " + Environment.Is64BitOperatingSystem);
                sb.AppendLine("64-Bit Process: " + Environment.Is64BitProcess);
                sb.AppendLine();

                sb.AppendLine("Exception:");
                sb.AppendLine(ex.ToString());

                Exception inner =
                    ex.InnerException;

                while (inner != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("===== Inner Exception =====");
                    sb.AppendLine(inner.ToString());

                    inner =
                        inner.InnerException;
                }

                sb.AppendLine();
                sb.AppendLine("========================================");
                sb.AppendLine();



                string newEntry = sb.ToString();

                // Read existing content if the file already exists; otherwise, default to empty string
                string existingContent = File.Exists(logFile)
                    ? File.ReadAllText(logFile)
                    : string.Empty;

                // Write the new entry first, followed by the historical logs
                File.WriteAllText(logFile, newEntry + existingContent);
            }
            catch
            {
                // Never let logging itself crash the application.
            }
        }
    }

}
