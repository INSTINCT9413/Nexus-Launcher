using DevExpress.Office.Drawing;
using DevExpress.XtraSplashScreen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexus_Launcher.Models;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services;
using SQLitePCL;
using System;
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
        public static MainView MainFormInstance;
        private static EventWaitHandle _showEvent;
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

        private static void MainInternal()
        {
            if (!Settings.Default.Setup)
            {
                FirstTimeSetupForm setupForm =
                    new FirstTimeSetupForm();

                setupForm.ShowDialog();
                return;
            }

            bool createdNew;

            _mutex = new Mutex(
                true,
                "NexusLauncher_SingleInstance",
                out createdNew);

            if (!createdNew)
            {
                var mess = MessageBox.Show(
                    "Nexus Launcher is already running.\n\r\n\rDo you want to show the existing instance?(Yes)\n\r\n\rDo you want to force close the existing instance\n\rand restart?(No)\n\r\n\rDo you want to cancel and do nothing?(Cancel)",
                    "Nexus Launcher",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Information);

                if (mess == DialogResult.Yes)
                {
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
                }
                else
                {
                    if (mess == DialogResult.No)
                    {
                        try
                        {
                            Process currentProcess =
                                Process.GetCurrentProcess();
                            foreach (var process in Process.GetProcessesByName(currentProcess.ProcessName))
                            {
                                if (process.Id != currentProcess.Id)
                                {
                                    process.Kill();
                                    Application.Restart();
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                    return;
            }

            _showEvent =
                new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    "NexusLauncher_Show");

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

            Application.Run(
                MainFormInstance);

            _mutex.ReleaseMutex();
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
