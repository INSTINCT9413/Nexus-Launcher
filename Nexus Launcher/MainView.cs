using CCWin.SkinControl;
using CCWin.Win32;
using DevExpress.LookAndFeel;
using DevExpress.Utils;
using DevExpress.Utils.Extensions;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors;
using DevExpress.XtraRichEdit.Import.Html;
using DevExpress.XtraSplashScreen;
using DevExpress.XtraWaitForm;
using Microsoft.Win32;
using Nexus_Launcher.Controls;
using Nexus_Launcher.Forms;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Models;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services;
using Nexus_Launcher.Services.Artwork;
using Ookii.Dialogs.WinForms;
using QlmControls.v10;
using Sunny.UI;
using DevExpress.Utils.Html;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DevExpress.XtraEditors.ViewInfo.BaseListBoxViewInfo;
using static Nexus_Launcher.MainView;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using DevExpress.Utils.VisualEffects;
namespace Nexus_Launcher
{
    public partial class MainView : DevExpress.XtraBars.FluentDesignSystem.FluentDesignForm
    {

        // Variables (sort later)
        public MainView mainView;
        public WaitForm1 waitForm1;
        public static string fullUserName = UserPrincipal.Current.DisplayName;
        public virtual string Title { get; set; } = "Nexus Launcher";
        public virtual string VersionTitle { get; set; } = "Version: " + Version;
        public static string Version = Application.ProductVersion;
        readonly static string NexusPath = Application.StartupPath;
        readonly ApplicationCard applicationCard = new ApplicationCard();
        readonly LauncherCard launcherCard = new LauncherCard();
        readonly NexusStore nexusStore = new NexusStore();
        readonly UserAccount userAccount = new UserAccount();
        fullLibraryControl fullLibrary = new fullLibraryControl();
        static bool notiShown = false;
        public bool appExit = false;
        readonly List<LauncherDefinition> launchers = new List<LauncherDefinition>();
        static string currentUser = Environment.UserName;
        public static string sysDisk = System.IO.Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
        public string selectedGroup;
        private FileSystemWatcher steamWatcher;
        private CancellationTokenSource steamReloadToken;
        SteamScannerService steamscanner =
                        new SteamScannerService();
        EpicScannerService epicscanner =
                    new EpicScannerService();
        GOGScannerService gogscanner =
                    new GOGScannerService();
        BattleNetScannerService battleNetscanner =
                    new BattleNetScannerService();
        EAScannerService eascanner =
                    new EAScannerService();
        UbisoftScannerService ubisoftscanner =
                    new UbisoftScannerService();
        LauncherInfo steamInfo = new LauncherInfo
        {
            Name = "Steam",
            InstallPath = steamPath,
            ExecutablePath = steamPath + @"\steam.exe",
            Games = new List<GameInfo>()
        };

        private FileSystemWatcher battleNetWatcher;
        private CancellationTokenSource battleNetReloadToken;
        LauncherInfo battleNetInfo = new LauncherInfo
        {
            Name = "Battle.net",
            InstallPath = battleNetPath,
            ExecutablePath = battleNetPath + @"\Battle.net Launcher.exe",
            Games = new List<GameInfo>()
        };

        private FileSystemWatcher epicWatcher;
        private CancellationTokenSource epicReloadToken;
        LauncherInfo epicInfo = new LauncherInfo
        {
            Name = "Epic Games",
            InstallPath = epicPath,
            ExecutablePath = epicPath + @"\EpicGamesLauncher.exe",
            Games = new List<GameInfo>()
        };

        private FileSystemWatcher gogWatcher;
        private CancellationTokenSource gogReloadToken;
        LauncherInfo gogInfo = new LauncherInfo
        {
            Name = "GOG Galaxy",
            InstallPath = gogPath,
            ExecutablePath = gogPath + @"\GalaxyClient.exe",
            Games = new List<GameInfo>()
        };

        private List<FileSystemWatcher> eaWatchers = new List<FileSystemWatcher>();
        private CancellationTokenSource eaReloadToken;
        LauncherInfo eaAppInfo = new LauncherInfo
        {
            Name = "EA App",
            InstallPath = eaAppPath,
            ExecutablePath = eaAppPath + @"\EADesktop.exe",
            Games = new List<GameInfo>()
        };

        private FileSystemWatcher ubisoftWatcher;
        private CancellationTokenSource ubisoftReloadToken;
        LauncherInfo ubisoftInfo = new LauncherInfo
        {
            Name = "Ubisoft Connect",
            InstallPath = ubisoftPath,
            ExecutablePath = ubisoftPath + @"\UbisoftConnect.exe",
            Games = new List<GameInfo>()
        };
        LauncherInfo nexusInfo = new LauncherInfo
        {
            Name = "Nexus Launcher",
            InstallPath = NexusPath,
            ExecutablePath = NexusPath + @"\Nexus Launcher.exe",
            Games = new List<GameInfo>()
        };
        public static string steamPath = !string.IsNullOrWhiteSpace(Settings.Default.steamPath) ? Path.GetDirectoryName(Settings.Default.steamPath) : string.Empty;
        public static string epicPath = !string.IsNullOrWhiteSpace(Settings.Default.epicPath) ? Path.GetDirectoryName(Settings.Default.epicPath) : string.Empty;
        public static string epicPath2 = sysDisk + @"ProgramData\Epic\EpicGamesLauncher\Data\Manifests";
        public static string gogPath = !string.IsNullOrWhiteSpace(Settings.Default.gogPath) ? Path.GetDirectoryName(Settings.Default.gogPath) : string.Empty;
        public static string ubisoftPath = !string.IsNullOrWhiteSpace(Settings.Default.ubisoftPath) ? Path.GetDirectoryName(Settings.Default.ubisoftPath) : string.Empty;
        public static string battleNetPath = !string.IsNullOrWhiteSpace(Settings.Default.battlenetPath) ? Path.GetDirectoryName(Settings.Default.battlenetPath) : string.Empty;
        public static string wargamingPath = sysDisk + @"ProgramData\Wargaming.net\GameCenter";
        public static string amazonGamesPath = sysDisk + @"Users\" + currentUser + @"\AppData\Local\Amazon Games\App";
        public static string xboxAppPath = sysDisk + @"Program Files\WindowsApps\Microsoft.XboxApp_8wekyb3d8bbwe";
        public static string windowsStorePath = sysDisk + @"Program Files\WindowsApps\Microsoft.WindowsStore_8wekyb3d8bbwe";
        public static string paradoxLauncherPath = sysDisk + @"Users\" + currentUser + @"\AppData\Local\Programs\Paradox Interactive\launcher";
        public static string eaAppPath = !string.IsNullOrWhiteSpace(Settings.Default.eaPath) ? Path.GetDirectoryName(Settings.Default.eaPath) : string.Empty;
        public static string originPath = sysDisk + @"Program Files (x86)\Origin";
        public static string eaGamesPath = sysDisk + @"Program Files\EA Games";
        public string aboutNexusLauncher = "Nexus Launcher is a unified game and application launcher designed to bring all of your gaming platforms together in one place. It automatically detects supported launchers such as Steam, Epic Games, GOG Galaxy, Ubisoft Connect, EA App, Battle.net, and others, allowing you to browse and launch your installed games from a single interface. Nexus Launcher also supports custom applications, making it easy to organize games, tools, and programs in one centralized library. Built with performance and customization in mind, Nexus Launcher aims to simplify game management while providing a clean, modern experience for PC gamers.";
        private readonly ContextMenuStrip gameContextMenu = new ContextMenuStrip();
        private readonly PopupMenu gameContextMenu0 = new PopupMenu();
        private GameInfo selectedGame;
        private bool showUpdateBadge = false;
        private Badge nexusUpdateBadge;
        private bool updateAvailable;

        public MainView()
        {
            
            System.Diagnostics.Debug.WriteLine("Hello Debug");
            Console.WriteLine("Hello Console");
            InitializeComponent();
            ArtworkService.ArtworkDownloaded += ArtworkService_ArtworkDownloaded;
            if (barButtonItem3.Manager != null)
            {
                barButtonItem3.Manager.CustomDrawItem -= barManager1_CustomDrawItem;
                barButtonItem3.Manager.CustomDrawItem += barManager1_CustomDrawItem;
            }
            if (updateAvailable == true)
            {
                badge1.Visible = true;
            }
            else { badge1.Visible = false; }
            InitializeUpdateBadge();
            SplashScreenManager.Default.SetWaitFormDescription("Initializing...");
            ThemesSettings settings =
            ThemeSettingsManager.Load();
            EnableAcrylicAccent = true;
            SurfaceMaterial = SurfaceMaterial.Acrylic;
            if (!string.IsNullOrWhiteSpace(
                settings.SkinName))
            {
                UserLookAndFeel.Default.SetSkinStyle(
                    settings.SkinName);
            }

            if (!string.IsNullOrWhiteSpace(
                settings.PaletteName))
            {
                UserLookAndFeel.Default.SetSkinStyle(
                    settings.SkinName,
                    settings.PaletteName);
            }
            if (string.IsNullOrEmpty(Settings.Default.DefaultLauncher))
            {
                // Add any logic needed when DefaultLauncher is null or empty
                Settings.Default.DefaultLauncher = "Nexus Launcher";
                Settings.Default.Save();
            }
        }
        private void InitializeUpdateBadge()
        {
            nexusUpdateBadge = new Badge();

            nexusUpdateBadge.Properties.Text = "!";

            nexusUpdateBadge.Appearance.BackColor = Color.DarkOrange;
            nexusUpdateBadge.Appearance.BorderColor = Color.Black;
            nexusUpdateBadge.Appearance.ForeColor = Color.White;
            nexusUpdateBadge.Appearance.Font =
                new Font("Segoe UI", 8f, FontStyle.Bold);

            nexusUpdateBadge.Properties.Location =
                ContentAlignment.TopRight;

            nexusUpdateBadge.Properties.Offset =
                new Point(-4, 2);

            nexusUpdateBadge.Visible = false;

            adornerUIManager1.Elements.Add(nexusUpdateBadge);
        }
        public void SetUpdateBadge(bool visible)
        {
            showUpdateBadge = visible;

            if (barButtonItem3 != null)
                barButtonItem3.Refresh();
        }
        private void barManager1_CustomDrawItem(
    object sender,
    DevExpress.XtraBars.BarItemCustomDrawEventArgs e)
        {
            if (e.LinkInfo == null || e.LinkInfo.Link == null)
                return;

            if (e.LinkInfo.Link.Item != barButtonItem3)
                return;

            if (!updateAvailable)
                return;

            // Normal item
            e.DrawBackground();
            e.DrawBorder();
            e.DrawGlyph();
            e.DrawText();

            int badgeSize = 16;

            Rectangle badgeBounds = new Rectangle(
                e.Bounds.Right - badgeSize - 6,
                e.Bounds.Top + (e.Bounds.Height - badgeSize) / 2,
                badgeSize,
                badgeSize);

            using (Brush brush =
                new SolidBrush(nexusUpdateBadge.Appearance.BackColor))
            {
                e.Cache.FillEllipse(
                    brush,
                    badgeBounds);
            }

            using (Font font =
                new Font(
                    nexusUpdateBadge.Appearance.Font,
                    FontStyle.Bold))
            {
                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                e.Cache.DrawString(
                    nexusUpdateBadge.Properties.Text,
                    font,
                    new SolidBrush(
                        nexusUpdateBadge.Appearance.ForeColor),
                    badgeBounds,
                    format);
            }

            e.Handled = true;
        }
        public void RestoreLauncher()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(
                    RestoreLauncher));

                return;
            }

            Show();

            ShowInTaskbar = true;

            WindowState =
                FormWindowState.Normal;

            Activate();

            BringToFront();

            Focus();
        }
        #region SteamRefresh
        private void InitializeSteamWatcher()
        {
            try
            {
               

                if (string.IsNullOrWhiteSpace(
                    steamPath))
                {
                    return;
                }

                string steamApps =
                    Path.Combine(
                        steamPath,
                        "steamapps");

                if (!Directory.Exists(
                    steamApps))
                {
                    return;
                }

                steamWatcher =
                    new FileSystemWatcher();

                steamWatcher.Path =
                    steamApps;

                steamWatcher.Filter =
                    "appmanifest_*.acf";

                steamWatcher.NotifyFilter =
                    NotifyFilters.FileName |
                    NotifyFilters.LastWrite;

                steamWatcher.Created +=
                    SteamLibraryChanged;

                steamWatcher.Deleted +=
                    SteamLibraryChanged;

                steamWatcher.Renamed +=
                    SteamLibraryChanged;

                steamWatcher.Changed +=
                    SteamLibraryChanged;

                steamWatcher.EnableRaisingEvents =
                    true;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                XtraMessageBox.Show(
                    ex.ToString());
            }
        }
        private async void SteamLibraryChanged(
    object sender,
    FileSystemEventArgs e)
        {
            try
            {
                steamReloadToken?.Cancel();

                steamReloadToken =
                    new CancellationTokenSource();

                CancellationToken token =
                    steamReloadToken.Token;

                await Task.Delay(
                    3000,
                    token);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (IsDisposed)
                {
                    return;
                }

                BeginInvoke(
                    new Action(async () =>
                    {
                        await RefreshSteamLibraryAsync();
                    }));
            }
            catch
            {
            }
        }
        private async Task RefreshSteamLibraryAsync()
        {
            try
            {
                groupSteam.Elements.Clear();

                await LoadSteamGamesAsync(steamInfo);

                // Optional:
                // statusLabel.Text =
                // "Steam library refreshed";
                // Replace this line in RefreshSteamLibraryAsync():
                // this.Text.Append("Steam Refreshed");

                // With this line:
                //this.Text += " - Steam Refreshed";
               
            }
            catch
            {
            }
        }
        #endregion

        #region Battle.net Refresh
        private void InitializeBattleNetWatcher()
        {
            try
            {
                string path = sysDisk+
                    @"ProgramData\Battle.net\Agent";

                if (!Directory.Exists(path))
                    return;

                battleNetWatcher =
                    new FileSystemWatcher();

                battleNetWatcher.Path =
                    path;

                battleNetWatcher.Filter =
                    "aggregate.json";

                battleNetWatcher.NotifyFilter =
                    NotifyFilters.LastWrite |
                    NotifyFilters.Size |
                    NotifyFilters.CreationTime;

                battleNetWatcher.Changed +=
                    BattleNetLibraryChanged;

                battleNetWatcher.EnableRaisingEvents =
                    true;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                XtraMessageBox.Show(ex.ToString());
            }
        }
        private async void BattleNetLibraryChanged(
    object sender,
    FileSystemEventArgs e)
        {
            try
            {
                battleNetReloadToken?.Cancel();

                battleNetReloadToken =
                    new CancellationTokenSource();

                CancellationToken token =
                    battleNetReloadToken.Token;

                await Task.Delay(
                    3000,
                    token);

                if (token.IsCancellationRequested)
                    return;

                if (IsDisposed)
                    return;

                BeginInvoke(
                    new Action(async () =>
                    {
                        await RefreshBattleNetLibraryAsync();
                    }));
            }
            catch
            {
            }
        }
        private async Task RefreshBattleNetLibraryAsync()
        {
            try
            {
                groupBattleNet.Elements.Clear();

                await LoadBattleNetGamesAsync(battleNetInfo);

                // Optional:
                // statusLabel.Text =
                // "Battle.net library updated";
            }
            catch
            {
            }
        }
        #endregion
        #region Epic Refresh
        private void InitializeEpicWatcher()
        {
            try
            {
                string path = sysDisk +
                    @"ProgramData\Epic\EpicGamesLauncher\Data\Manifests";

                if (!Directory.Exists(path))
                    return;

                epicWatcher =
                    new FileSystemWatcher();

                epicWatcher.Path =
                    path;

                epicWatcher.Filter =
                    "*.item";

                epicWatcher.NotifyFilter =
                    NotifyFilters.FileName |
                    NotifyFilters.LastWrite;

                epicWatcher.Created +=
                    EpicLibraryChanged;

                epicWatcher.Deleted +=
                    EpicLibraryChanged;

                epicWatcher.Changed +=
                    EpicLibraryChanged;

                epicWatcher.Renamed +=
                    EpicLibraryChanged;

                epicWatcher.EnableRaisingEvents =
                    true;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                XtraMessageBox.Show(ex.ToString());
            }
        }
        private async void EpicLibraryChanged(
    object sender,
    FileSystemEventArgs e)
        {
            try
            {
                epicReloadToken?.Cancel();

                epicReloadToken =
                    new CancellationTokenSource();

                CancellationToken token =
                    epicReloadToken.Token;

                await Task.Delay(
                    3000,
                    token);

                if (token.IsCancellationRequested)
                    return;

                if (IsDisposed)
                    return;

                BeginInvoke(
                    new Action(async () =>
                    {
                        await RefreshEpicLibraryAsync();
                    }));
            }
            catch
            {
            }
        }
        private async Task RefreshEpicLibraryAsync()
        {
            try
            {
                groupEpic.Elements.Clear();

                await LoadEpicGamesAsync(
                    epicInfo);

                //this.Text += " - Epic Refreshed";
            }
            catch
            {
            }
        }
        #endregion
        #region GOG Refresh
        private void InitializeGogWatcher()
        {
            try
            {
                string path = sysDisk +
                    @"Program Files (x86)\GOG Galaxy\Games";

                if (!Directory.Exists(path))
                    return;

                gogWatcher =
                    new FileSystemWatcher();

                gogWatcher.Path =
                    path;

                gogWatcher.Filter =
                    "*";

                gogWatcher.IncludeSubdirectories =
                    false;

                gogWatcher.NotifyFilter =
                    NotifyFilters.DirectoryName;

                gogWatcher.Created +=
                    GogFolderChanged;

                gogWatcher.Deleted +=
                    GogFolderChanged;

                gogWatcher.Renamed +=
                    GogFolderRenamed;

                gogWatcher.EnableRaisingEvents =
                    true;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                XtraMessageBox.Show(ex.ToString());
            }
        }
        public async void GogFolderChanged(
    object sender,
    FileSystemEventArgs e)
        {
            if (!Directory.Exists(e.FullPath))
                return;

            await HandleGogReloadAsync();
        }
        private async void GogFolderRenamed(
    object sender,
    RenamedEventArgs e)
        {
            await HandleGogReloadAsync();
        }
        private async Task HandleGogReloadAsync()
        {
            try
            {
                gogReloadToken?.Cancel();

                gogReloadToken =
                    new CancellationTokenSource();

                CancellationToken token =
                    gogReloadToken.Token;

                await Task.Delay(
                    3000,
                    token);

                if (token.IsCancellationRequested)
                    return;

                if (IsDisposed)
                    return;

                BeginInvoke(
                    new Action(async () =>
                    {
                        await RefreshGogLibraryAsync();
                    }));
            }
            catch
            {
            }
        }
        public async Task RefreshGogLibraryAsync()
        {
            try
            {
                groupGOG.Elements.Clear();

                await LoadGogGamesAsync();

                //this.Text += " - GOG Refreshed";
            }
            catch
            {
            }
        }
        #endregion
        #region EA Refresh
        private void InitializeEAWatcher()
        {
            try
            {
                DisposeEAWatchers();
                EAScannerService scanner =
                    new EAScannerService();

                List<GameInfo> games =
                    scanner.ScanGames();

                foreach (FileSystemWatcher watcher
                    in eaWatchers)
                {
                    watcher.EnableRaisingEvents =
                        false;

                    watcher.Dispose();
                }

                eaWatchers.Clear();

                foreach (GameInfo game in games)
                {
                    if (string.IsNullOrWhiteSpace(
                        game.InstallPath))
                    {
                        continue;
                    }

                    if (!Directory.Exists(
                        game.InstallPath))
                    {
                        continue;
                    }

                    FileSystemWatcher watcher =
                        new FileSystemWatcher();

                    watcher.Path =
                        game.InstallPath;

                    watcher.Filter =
                        "*";

                    watcher.IncludeSubdirectories =
                        false;

                    watcher.NotifyFilter =
                        NotifyFilters.DirectoryName;

                    watcher.Created +=
                        EAFolderChanged;

                    watcher.Deleted +=
                        EAFolderChanged;

                    watcher.Renamed +=
                        EAFolderRenamed;

                    watcher.EnableRaisingEvents =
                        true;

                    eaWatchers.Add(
                        watcher);
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                XtraMessageBox.Show(
                    ex.ToString());
            }
        }
        public async void EAFolderChanged(
    object sender,
    FileSystemEventArgs e)
        {
            await HandleEAReloadAsync();
        }
        private async void EAFolderRenamed(
    object sender,
    RenamedEventArgs e)
        {
            await HandleEAReloadAsync();
        }
        private async Task HandleEAReloadAsync()
        {
            try
            {
                eaReloadToken?.Cancel();

                eaReloadToken =
                    new CancellationTokenSource();

                CancellationToken token =
                    eaReloadToken.Token;

                await Task.Delay(
                    3000,
                    token);

                if (token.IsCancellationRequested)
                    return;

                if (IsDisposed)
                    return;

                BeginInvoke(
                    new Action(async () =>
                    {
                        await RefreshEALibraryAsync();
                    }));
            }
            catch
            {
            }
        }
        private async Task RefreshEALibraryAsync()
        {
            try
            {
                groupEA.Elements.Clear();

                await LoadEAGamesAsync();

                InitializeEAWatcher();
            }
            catch
            {
            }
        }
        private void DisposeEAWatchers()
        {
            foreach (FileSystemWatcher watcher
                in eaWatchers)
            {
                try
                {
                    watcher.EnableRaisingEvents =
                        false;

                    watcher.Dispose();
                }
                catch
                {
                }
            }

            eaWatchers.Clear();
        }
        #endregion
        #region Ubisoft Refresh
        private void InitializeUbisoftWatcher()
        {
            try
            {
                UbisoftScannerService scanner =
                    new UbisoftScannerService();

                List<GameInfo> games =
                    scanner.ScanGames();

                if (games.Count == 0)
                    return;

                string firstInstallPath =
                    games[0].InstallPath;

                if (string.IsNullOrWhiteSpace(
                    firstInstallPath))
                {
                    return;
                }

                string parentFolder =
                    Directory.GetParent(
                        firstInstallPath)?.FullName;

                if (string.IsNullOrWhiteSpace(
                    parentFolder))
                {
                    return;
                }

                if (!Directory.Exists(
                    parentFolder))
                {
                    return;
                }

                ubisoftWatcher?.Dispose();

                ubisoftWatcher =
                    new FileSystemWatcher();

                ubisoftWatcher.Path =
                    parentFolder;

                ubisoftWatcher.Filter =
                    "*";

                ubisoftWatcher.IncludeSubdirectories =
                    false;

                ubisoftWatcher.NotifyFilter =
                    NotifyFilters.DirectoryName;

                ubisoftWatcher.Created +=
                    UbisoftFolderChanged;

                ubisoftWatcher.Deleted +=
                    UbisoftFolderChanged;

                ubisoftWatcher.Renamed +=
                    UbisoftFolderRenamed;

                ubisoftWatcher.EnableRaisingEvents =
                    true;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                XtraMessageBox.Show(
                    ex.ToString());
            }
        }
        public async void UbisoftFolderChanged(
    object sender,
    FileSystemEventArgs e)
        {
            await HandleUbisoftReloadAsync();
        }
        private async void UbisoftFolderRenamed(
    object sender,
    RenamedEventArgs e)
        {
            await HandleUbisoftReloadAsync();
        }
        private async Task HandleUbisoftReloadAsync()
{
    try
    {
        ubisoftReloadToken?.Cancel();

        ubisoftReloadToken =
            new CancellationTokenSource();

        CancellationToken token =
            ubisoftReloadToken.Token;

        await Task.Delay(
            3000,
            token);

        if (token.IsCancellationRequested)
            return;

        if (IsDisposed)
            return;

        BeginInvoke(
            new Action(async () =>
            {
                await RefreshUbisoftLibraryAsync();
            }));
    }
    catch
    {
    }
}
        private async Task RefreshUbisoftLibraryAsync()
        {
            try
            {
                groupUbisoft.Elements.Clear();

                await LoadUbisoftGamesAsync();

                InitializeUbisoftWatcher();

                //this.Text += " - Ubisoft Refreshed";
            }
            catch
            {
            }
        }
        #endregion
        private async void MainView_Load_1(object sender, EventArgs e)
        {
            FontManager.ApplyFont(
    this,
    Settings.Default.UIFont);
            
            Hide();
            accordionControl2.Enabled = false;
            barButtonItem13.Enabled = false;
            // Disables the focus rectangle globally for all DevExpress SimpleButtons
            DevExpress.XtraEditors.WindowsFormsSettings.FocusRectStyle = DevExpress.Utils.Paint.DXDashStyle.None;
            if (Settings.Default.StartMinimized)
            {
                Hide();

                ShowInTaskbar = false;
                notifyIcon1.Visible = true;

                if (!notiShown)
                {
                    notifyIcon1.ShowBalloonTip(
                        3000,
                        "Nexus Launcher",
                        "Nexus Launcher has started and is running in the system tray.",
                        ToolTipIcon.Info);

                    notiShown = true;
                }
            }
            else
            {
                BeginInvoke(new Action(() =>
                {
                    WindowState = Settings.Default.StartMaximized
                        ? FormWindowState.Maximized
                        : FormWindowState.Normal;

                    Show();
                    Activate();
                }));

                if (!Settings.Default.HideEANotice)
                {
                    taskDialog1.ShowDialog(this);
                }
            }

            this.Text = Title;
            this.barStaticItem1.Caption = InstalledBuild.Display;
            //aloneTextBox1.Text = NexusPath;
            
            barButtonItem4.Caption = fullUserName;
            this.taskDialogButton1.Enabled = false;
            applicationCard.Dock = DockStyle.Fill;
            launcherCard.Dock = DockStyle.Fill;
            nexusStore.Dock = DockStyle.Fill;
            userAccount.Dock = DockStyle.Fill;
            fullLibrary.Dock = DockStyle.Fill;
            //applicationCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            splitContainerControl1.Panel2.Controls.Add(applicationCard);
            splitContainerControl1.Panel2.Controls.Add(launcherCard);
            launcherCard.xtraTabPage4.Controls.Add(nexusStore);
            splitContainerControl1.Panel2.Controls.Add(userAccount);
            splitContainerControl1.Panel2.Controls.Add(fullLibrary);
            applicationCard.Visible = false;
            launcherCard.Visible = false;
            nexusStore.Visible = false;
            userAccount.Visible = false;
            

            groupNexus.Tag = nexusInfo;
            SplashHelper.UpdateStatus("Finding Launchers...");
            await FindLaunchers();
            //SuspendLayout();
            //launcherCard.BringToFront();
            //applicationCard._selectedGroup = "Nexus Launcher";
            //launcherCard.Visible = true;
            //applicationCard.Visible = false;
            //launcherCard.clientName = "Nexus Launcher";
            //launcherCard.clientIcon = Resources.dfveffb_9b262552_e352_4348_aefc_8e699002c946; // Replace with actual path to Nexus Launcher icon
            //launcherCard.isNexusLauncher = true;
            //ResumeLayout();
            launcherCard.xtraTabControl1.SelectedTabPage = launcherCard.xtraTabPage4;
            nexusStore.Visible = true;

            ApplyUIChanges();

            
        }
        private async void ApplyUIChanges()
        {
            
            if (Properties.Settings.Default.SidePanelRemember == true)
            {
                simpleButton2.PerformClick();
            }
            
                if (Properties.Settings.Default.DefaultLauncher == "EA App")
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "EA App";
                    launcherCard._selectedGroup = "EA App";
                    nexusStore.NexusStoreUrl = "https://www.ea.com/games/library/pc-download";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "EA App";
                    launcherCard.clientIcon = Resources._256x256; // Replace with actual path to EA App icon
                    launcherCard.isNexusLauncher = false;
                    accordionControl2.BeginUpdate();
                    groupEA.Visible = true;
                    accordionControl2.ActiveGroup = groupEA;
                    groupEA.Expanded = true;
                    accordionControl2.EndUpdate();
                    accordionControl2.Refresh();
                    ResumeLayout();
                }
                if (Properties.Settings.Default.DefaultLauncher == "Steam Client")
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "Steam";
                    launcherCard._selectedGroup = "Steam";
                    nexusStore.NexusStoreUrl = "https://store.steampowered.com/";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "Steam";
                    launcherCard.clientIcon = Resources.Steam_icon_logo_svg; // Replace with actual path to Steam icon
                    launcherCard.isNexusLauncher = false;
                    accordionControl2.BeginUpdate();
                    groupSteam.Visible = true;
                    accordionControl2.ActiveGroup = groupSteam;
                    groupSteam.Expanded = true;
                    accordionControl2.EndUpdate();
                    accordionControl2.Refresh();
                    ResumeLayout();
                }
                if (Properties.Settings.Default.DefaultLauncher == "Epic Games")
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "Epic Games";
                    launcherCard._selectedGroup = "Epic Games";
                    nexusStore.NexusStoreUrl = "https://www.epicgames.com/store/";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "Epic Games";
                    launcherCard.clientIcon = Resources.epic_games_black_logo_icon_147139; // Replace with actual path to Epic Games icon
                    launcherCard.isNexusLauncher = false;
                    accordionControl2.BeginUpdate();
                    groupEpic.Visible = true;
                    accordionControl2.ActiveGroup = groupEpic;
                    groupEpic.Expanded = true;
                    accordionControl2.EndUpdate();
                    accordionControl2.Refresh();
                    ResumeLayout();
                }
                if (Properties.Settings.Default.DefaultLauncher == "Nexus Launcher")
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "Nexus Launcher";
                    launcherCard._selectedGroup = "Nexus Launcher";
                    nexusStore.NexusStoreUrl = "https://horizonsocial.media/apps/nexus.html";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "Nexus Launcher";
                    launcherCard.clientIcon = Resources.dfveffb_9b262552_e352_4348_aefc_8e699002c946; // Replace with actual path to Nexus Launcher icon
                    launcherCard.isNexusLauncher = true;
                    accordionControl2.BeginUpdate();
                    groupNexus.Visible = true;
                    accordionControl2.ActiveGroup = groupNexus;
                    groupNexus.Expanded = true;
                    accordionControl2.EndUpdate();
                    accordionControl2.Refresh();
                    ResumeLayout();
                }
                if (Properties.Settings.Default.DefaultLauncher == "Battle.net")
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "Battle.net";
                    launcherCard._selectedGroup = "Battle.net";
                    nexusStore.NexusStoreUrl = "https://us.shop.battle.net/en-us#optLogin=true";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    //change pictureboxedit2 sizemode
                    //applicationCard.pictureEdit2.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom;
                    //applicationCard.pictureEdit2.Properties.ZoomPercent = 45;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "Battle.net";
                    launcherCard.clientIcon = Resources.unnamed; // Replace with actual path to Battle.net icon
                    launcherCard.isNexusLauncher = false;
                    accordionControl2.BeginUpdate();
                    groupBattleNet.Visible = true;
                    accordionControl2.ActiveGroup = groupBattleNet;
                    groupBattleNet.Expanded = true;
                    accordionControl2.EndUpdate();
                    accordionControl2.Refresh();
                    ResumeLayout();
                }
                if (Properties.Settings.Default.DefaultLauncher == "GOG Galaxy")
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "GOG";
                    launcherCard._selectedGroup = "GOG";
                    nexusStore.NexusStoreUrl = "https://www.gog.com/en/games";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "GOG";
                    launcherCard.clientIcon = Resources.d8ac3b01ba19729174a8f1e63c9e937c; // Replace with actual path to GOG Galaxy icon
                    launcherCard.isNexusLauncher = false;
                    accordionControl2.BeginUpdate();
                    groupGOG.Visible = true;
                    accordionControl2.ActiveGroup = groupGOG;
                    groupGOG.Expanded = true;
                    accordionControl2.EndUpdate();
                    accordionControl2.Refresh();
                    ResumeLayout();
                }
                if (Properties.Settings.Default.DefaultLauncher == "Ubisoft Connect")
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "Ubisoft Connect";
                    launcherCard._selectedGroup = "Ubisoft Connect";
                    nexusStore.NexusStoreUrl = "https://store.ubisoft.com/us/home?lang=en_US";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "Ubisoft Connect";
                    launcherCard.clientIcon = Resources._1024x1024; // Replace with actual path to Ubisoft Connect icon
                    launcherCard.isNexusLauncher = false;
                    accordionControl2.BeginUpdate();
                    groupUbisoft.Visible = true;
                    accordionControl2.ActiveGroup = groupUbisoft;
                    groupUbisoft.Expanded = true;
                    accordionControl2.EndUpdate();
                    accordionControl2.Refresh();
                    ResumeLayout();
                }
                if(Settings.Default.RootDisplayMode == true)
                    {
                        accordionControl2.RootDisplayMode = AccordionControlRootDisplayMode.Footer;
                    }
                    else
                    {
                        accordionControl2.RootDisplayMode = AccordionControlRootDisplayMode.Default;
                    }
                if(Settings.Default.ViewType == true)
                    {
                        accordionControl2.ViewType = AccordionControlViewType.HamburgerMenu;
                    }
                    else
                    {
                        accordionControl2.ViewType = AccordionControlViewType.Standard;
                    }
            if (Settings.Default.enableFullLibrary == true)
            {
                barButtonItem13.Visibility = DevExpress.XtraBars.BarItemVisibility.Always;
            }
            else
            {
                barButtonItem13.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
            }
                await LauncherStartupService.StartConfiguredLaunchersAsync();
            BuildGameContextMenu();
        }
        public void FocusGOGSettings()
        {
            SuspendLayout();
            launcherCard.BringToFront();
            applicationCard._selectedGroup = "GOG";
            nexusStore.NexusStoreUrl = "https://www.gog.com/en/games";
            nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
            launcherCard.Visible = true;
            applicationCard.Visible = false;
            launcherCard.clientName = "GOG";
            launcherCard.clientIcon = Resources.d8ac3b01ba19729174a8f1e63c9e937c; // Replace with actual path to GOG Galaxy icon
            launcherCard.isNexusLauncher = false;
            accordionControl2.BeginUpdate();
            groupGOG.Visible = true;
            accordionControl2.ActiveGroup = groupGOG;
            groupGOG.Expanded = true;
            accordionControl2.EndUpdate();
            accordionControl2.Refresh();
            launcherCard.xtraTabControl1.SelectedTabPage = launcherCard.xtraTabPage2;
            launcherCard.ShowGOGSettings();
            ResumeLayout();
        }
        private async Task FindLaunchers()
        {
            
            SplashHelper.UpdateStatus("Loading Nexus List Items...");
            await LoadNexusGamesAsync();
            // Steam
            //string steamPath =
            //    @"C:\Program Files (x86)\Steam";

            bool installedSteam =
                Directory.Exists(steamPath) && File.Exists(steamPath + @"\steam.exe");
            launchers.Add(new LauncherDefinition
            {
                Name = "Steam",
                DetectInstalled = () =>
                    Directory.Exists(steamPath) &&
                    File.Exists(Path.Combine(
                        steamPath,
                        "steam.exe"))
            });
            

            groupSteam.Visible =
                installedSteam;
            if (installedSteam)
            {
                groupSteam.Tag = steamInfo;
                SplashHelper.UpdateStatus("Loading Steam Games...");
                await LoadSteamGamesAsync(steamInfo);
                InitializeSteamWatcher();
            }
            
            // Epic
            //string epicPath =
            //    @"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win32";

            bool installedEpic =
                Directory.Exists(epicPath) && File.Exists(epicPath + @"\EpicGamesLauncher.exe");
            launchers.Add(new LauncherDefinition
            {
                Name = "Epic Games",
                DetectInstalled = () =>
                    Directory.Exists(epicPath) &&
                    File.Exists(Path.Combine(
                        epicPath,
                        "EpicGamesLauncher.exe"))
            });
            

            groupEpic.Visible =
                installedEpic;
            if (installedEpic)
            {
                groupEpic.Tag = epicInfo;
                SplashHelper.UpdateStatus("Loading Epic Games...");
                await LoadEpicGamesAsync(epicInfo);
                InitializeEpicWatcher();
            }
            // Battle.net
            bool installedBattleNet =
                Directory.Exists(battleNetPath) && File.Exists(battleNetPath + @"\Battle.net Launcher.exe");
            launchers.Add(new LauncherDefinition
            {
                Name = "Battle.net",
                DetectInstalled = () =>
                    Directory.Exists(battleNetPath) &&
                    File.Exists(Path.Combine(
                        battleNetPath,
                        "Battle.net Launcher.exe"))
            });
            

            groupBattleNet.Visible =
                installedBattleNet;
            if (installedBattleNet)
            {
                groupBattleNet.Tag = battleNetInfo;
                SplashHelper.UpdateStatus("Loading Battlenet Games...");
                await LoadBattleNetGamesAsync(battleNetInfo);
                InitializeBattleNetWatcher();
            }
            // GOG Galaxy
            bool installedGOG =
                Directory.Exists(gogPath) && File.Exists(gogPath + @"\GalaxyClient.exe");
            launchers.Add(new LauncherDefinition
            {
                Name = "GOG Galaxy",
                DetectInstalled = () =>
                    Directory.Exists(gogPath) &&
                    File.Exists(Path.Combine(
                        gogPath,
                        "GalaxyClient.exe"))
            });
            

            groupGOG.Visible =
                installedGOG;
            if (installedGOG)
            {
                groupGOG.Tag = gogInfo;
                SplashHelper.UpdateStatus("Loading GOG Games...");
                await LoadGogGamesAsync();
                InitializeGogWatcher();
            }
            // Ubisoft Connect
            bool installedUbisoft =
                Directory.Exists(ubisoftPath) && File.Exists(ubisoftPath + @"\UbisoftConnect.exe");
            launchers.Add(new LauncherDefinition
            {
                Name = "Ubisoft Connect",
                DetectInstalled = () =>
                    Directory.Exists(ubisoftPath) &&
                    File.Exists(Path.Combine(
                        ubisoftPath,
                        "UbisoftConnect.exe"))
            });
            
            groupUbisoft.Visible =
                installedUbisoft;
            if (installedUbisoft)
            {
                groupUbisoft.Tag = ubisoftInfo;
                SplashHelper.UpdateStatus("Loading Ubisoft Games...");
                await LoadUbisoftGamesAsync();
                InitializeUbisoftWatcher();
            }
            // Paradox Launcher
            bool installedParadox =
                Directory.Exists(paradoxLauncherPath) && File.Exists(paradoxLauncherPath + @"\bootstrapper-v2.exe");
            launchers.Add(new LauncherDefinition
            {
                Name = "Paradox Launcher",
                DetectInstalled = () =>
                    Directory.Exists(paradoxLauncherPath) &&
                    File.Exists(Path.Combine(
                        paradoxLauncherPath,
                        "bootstrapper-v2.exe"))
            });
            LauncherInfo paradoxInfo = new LauncherInfo
            {
                Name = "Paradox Launcher",
                InstallPath = paradoxLauncherPath,
                ExecutablePath = paradoxLauncherPath + @"\bootstrapper-v2.exe",
                Games = new List<GameInfo>()
            };
            groupParadox.Visible =
                installedParadox;
            if (installedParadox)
            {
                groupParadox.Tag = paradoxInfo;
            }
            // Amazon Games
            bool installedAmazonGames =
                Directory.Exists(amazonGamesPath) && File.Exists(amazonGamesPath + @"\Amazon Games.exe");
            launchers.Add(new LauncherDefinition
            {
                Name = "Amazon Games",
                DetectInstalled = () =>
                    Directory.Exists(amazonGamesPath) &&
                    File.Exists(Path.Combine(
                        amazonGamesPath,
                        "Amazon Games.exe"))
            }
            );
            LauncherInfo amazonGamesInfo = new LauncherInfo
            {
                Name = "Amazon Games",
                InstallPath = amazonGamesPath,
                ExecutablePath = amazonGamesPath + @"\Amazon Games.exe",
                Games = new List<GameInfo>()
            };
            groupAmazon.Visible =
                installedAmazonGames;
            if (installedAmazonGames)
            {
                groupAmazon.Tag = amazonGamesInfo;
            }
            // Xbox App
            bool installedXboxApp = IsXboxInstalled();

            groupXbox.Visible =
                installedXboxApp;
            if (installedXboxApp)
            {
                //groupXbox.Tag = xboxAppInfo;
                //await LoadXboxGamesAsync();
            }
            // Windows Store
            bool installedWindowsStore = IsMicrosoftStoreInstalled();
            
            groupWindowsStore.Visible =
                installedWindowsStore;
            if (installedWindowsStore)
            {
                //groupWindowsStore.Tag = windowsStoreInfo;
            }
            // EA App   
            bool installedEAApp =
                Directory.Exists(eaAppPath) && File.Exists(eaAppPath + @"\EADesktop.exe");
            launchers.Add(new LauncherDefinition
                {
                Name = "EA App",
                DetectInstalled = () =>
                    Directory.Exists(eaAppPath) &&
                    File.Exists(Path.Combine(
                        eaAppPath,
                        "EADesktop.exe"))
            }
            );
            
            groupEA.Visible =
                installedEAApp;
            if (installedEAApp)
                {
                groupEA.Tag = eaAppInfo;
                SplashHelper.UpdateStatus("Loading EA Games...");
                await LoadEAGamesAsync();
                InitializeEAWatcher();
            }
            // Wargaming Game Center
            bool installedWargaming =
                Directory.Exists(wargamingPath) && File.Exists(wargamingPath + @"\wgc.exe");
            launchers.Add(new LauncherDefinition
                {
                Name = "Wargaming",
                DetectInstalled = () =>
                    Directory.Exists(wargamingPath) &&
                    File.Exists(Path.Combine(
                        wargamingPath,
                        "wgc.exe"))
            }
            );
            LauncherInfo wargamingInfo = new LauncherInfo
            {
                Name = "Wargaming",
                InstallPath = wargamingPath,
                ExecutablePath = wargamingPath + @"\wgc.exe",
                Games = new List<GameInfo>()
            };
            groupWargaming.Visible =
                installedWargaming;
            if (installedWargaming)
                {
                groupWargaming.Tag = wargamingInfo;
            }
            ShowHideLaunchers();
            LibraryService.Clear();

            LibraryService.AddGames(
                steamscanner.ScanGames(steamPath));

            LibraryService.AddGames(
                epicscanner.ScanGames());

            LibraryService.AddGames(
                gogscanner.ScanGames());

            LibraryService.AddGames(
                battleNetscanner.ScanGames());

            LibraryService.AddGames(
                eascanner.ScanGames());

            LibraryService.AddGames(
                ubisoftscanner.ScanGames());
        }
        private async void ShowHideLaunchers()
        {
            if (Settings.Default.ShowAmazon)
            {
                groupAmazon.Visible = true;
            }
            else
            {
                groupAmazon.Visible = false;
            }
            if (Settings.Default.ShowBattleNet)
            {
                groupBattleNet.Visible = true;
            }
            else
            {
                groupBattleNet.Visible = false;
            }
            if (Settings.Default.ShowEA)
            {
                groupEA.Visible = true;
            }
            else
            {
                groupEA.Visible = false;
            }
            if (Settings.Default.ShowEpic)
            {
                groupEpic.Visible = true;
            }
            else
            {
                groupEpic.Visible = false;
            }
            if (Settings.Default.ShowGOG)
            {
                groupGOG.Visible = true;
            }
            else
            {
                groupGOG.Visible = false;
            }
            if (Settings.Default.ShowParadox)
            {
                groupParadox.Visible = true;
            }
            else
            {
                groupParadox.Visible = false;
            }
            if (Settings.Default.ShowSteam)
            {
                groupSteam.Visible = true;
            }
            else
            {
                groupSteam.Visible = false;
            }
            if (Settings.Default.ShowUbisoft)
            {
                groupUbisoft.Visible = true;
            }
            else
            {
                groupUbisoft.Visible = false;
            }
            if (Settings.Default.ShowWindowsStore)
            {
                groupWindowsStore.Visible = true;
            }
            else
            {
                groupWindowsStore.Visible = false;
            }
            if (Settings.Default.ShowXbox)
            {
                groupXbox.Visible = true;
            }
            else
            {
                groupXbox.Visible = false;
            }
            if (Settings.Default.ShowWargaming)
            {
                groupWargaming.Visible = true;
            }
            else
            {
                groupWargaming.Visible = false;
            }
            this.Show();
            ArtworkService.ArtworkProgressChanged +=
            ArtworkService_ArtworkProgressChanged;
            SplashHelper.UpdateStatus("Finalizing...");
            await Task.Delay(1500);
            ArtworkService.FinishRegistration();
            SplashHelper.UpdateStatus("Downloading Artwork, Please Wait...");
            
            await ArtworkService.QueueFinished;
            accordionControl2.Enabled = true;
            barButtonItem13.Enabled = true;
            SplashScreenManager.CloseForm();
            taskbarAssistant2.ProgressMode = DevExpress.Utils.Taskbar.Core.TaskbarButtonProgressMode.NoProgress;
            groupNexus.Text = $"Nexus ({groupNexus.Elements.Count})";
            groupEA.Text = $"EA ({groupEA.Elements.Count})";
            groupEpic.Text = $"Epic Games ({groupEpic.Elements.Count})";
            groupGOG.Text = $"GOG ({groupGOG.Elements.Count})";
            groupSteam.Text = $"Steam ({groupSteam.Elements.Count})";
            groupUbisoft.Text = $"Ubisoft ({groupUbisoft.Elements.Count})";
            groupBattleNet.Text = $"Battle.net ({groupBattleNet.Elements.Count})";
        }
        private void ArtworkService_ArtworkProgressChanged(int completed, int total)
        {
            if (InvokeRequired)
            {
                BeginInvoke(
                    new Action<int, int>(
                        ArtworkService_ArtworkProgressChanged),
                    completed,
                    total);

                return;
            }

            SplashHelper.UpdateStatus(
                $"Downloading Artwork ({completed}/{total})...");
        }
        public static bool IsXboxInstalled()
        {
            return Registry.ClassesRoot.OpenSubKey("xbox") != null;
        }
        public static bool IsMicrosoftStoreInstalled()
        {
            return Registry.ClassesRoot.OpenSubKey("ms-windows-store") != null;
        }


        public async Task LoadNexusGamesAsync()
        {
            try
            {
                List<GameInfo> games =
                    await Task.Run(() =>
                    {
                        return NexusAddRemoveManager.Load();
                    });

                groupNexus.Elements.Clear();

                foreach (GameInfo game in games)
                {
                    AccordionControlElement item =
                        new AccordionControlElement();

                    if (File.Exists(
                        game.ExecutablePath))
                    {
                        item.ImageOptions.Image =
                            IconHelper.ExtractExeIcon(
                                game.ExecutablePath);
                    }

                    item.Text =
                        game.Name;
                    game.Launcher = "Nexus Launcher";
                    item.Style =
                        ElementStyle.Item;

                    item.Tag =
                        game;

                    groupNexus.Elements.Add(item);
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                XtraMessageBox.Show(ex.ToString());
            }

        }
        private async Task LoadSteamGamesAsync(
    LauncherInfo steamInfo)
        {
            try
            {
                await Task.Run(() =>
                {
                    

                    LauncherInfo updated =
                        steamscanner.ScanSteam(
                            steamInfo.InstallPath);

                    steamInfo.Games =
                        updated.Games;

                    steamInfo.Games =
                        steamInfo.Games
                        .OrderBy(x => x.Name)
                        .ToList();
                });

                PopulateSteamGames(steamInfo);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                XtraMessageBox.Show(ex.ToString());
            }
        }
        private void PopulateSteamGames(
    LauncherInfo steamInfo)
        {
            groupSteam.Elements.Clear();

            foreach (GameInfo game in steamInfo.Games)
            {
                AccordionControlElement item =
                    new AccordionControlElement();

                item.Text = game.Name;

                item.Style =
                    ElementStyle.Item;

                item.Tag = game;

                if (!string.IsNullOrWhiteSpace(game.IconPath) && File.Exists(game.IconPath))
                {
                    try
                    {
                        using (FileStream fs =
                            new FileStream(
                                game.IconPath,
                                FileMode.Open,
                                FileAccess.Read))

                        {
                            using (Image img =
                                Image.FromStream(fs))
                            {
                                item.ImageOptions.Image = ResizeImage(img, 32, 32);
                            }
                        }
                    }
                    catch
                    {

                    }
                }
                else
                {
                    item.ImageOptions.Image = Resources.NAicon; // Replace with actual path to default icon
                }
                    // debugging this.Text += " - " + game.IconPath;


                    groupSteam
                        .Elements
                        .Add(item);
            }
        }
        private async Task LoadEpicGamesAsync( LauncherInfo epicInfo)
        {
            try 
            {
                await Task.Run(() =>
                {
                    

                    epicInfo.Games =
                        epicscanner.ScanGames();
                    foreach (GameInfo game in epicInfo.Games)
                    {
                        AccordionControlElement item =
                            new AccordionControlElement();

                        item.Text =
                            game.Name;

                        item.Style =
                            ElementStyle.Item;

                        item.Tag =
                            game;

                        
                    }
                });
                // Update UI with loaded games
                PopulateEpicGames(epicInfo);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                XtraMessageBox.Show(ex.ToString());
            }
        }
        private void PopulateEpicGames(
    LauncherInfo epicInfo)
        {
            groupEpic.Elements.Clear();

            foreach (GameInfo game in epicInfo.Games)
            {
                AccordionControlElement item =
                    new AccordionControlElement();

                item.Text =
                    game.Name;

                item.Style =
                    ElementStyle.Item;

                item.Tag =
                    game;
                item.Image = IconHelper.ExtractExeIcon(game.ExecutablePath);

                groupEpic.Elements.Add(item);
            }
        }
        private async Task LoadBattleNetGamesAsync(
    LauncherInfo battleNetInfo)
        {
            try
            {
                

                battleNetInfo.Games =
                    await Task.Run(() =>
                    {
                        return battleNetscanner.ScanGames();
                    });

                PopulateBattleNetGames(
                    battleNetInfo);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                XtraMessageBox.Show(
                    ex.ToString());
            }
        }
        private void PopulateBattleNetGames(
    LauncherInfo battleNetInfo)
        {
            groupBattleNet.Elements.Clear();

            foreach (GameInfo game in battleNetInfo.Games)
            {
                AccordionControlElement item =
                    new AccordionControlElement();

                item.Text =
                    game.Name;

                item.Style =
                    ElementStyle.Item;

                item.Tag =
                    game;
                
                // Use executable icon
                if (!string.IsNullOrWhiteSpace(
                    game.ExecutablePath) &&
                    File.Exists(
                        game.ExecutablePath))
                {
                    item.ImageOptions.Image =
                        IconHelper.ExtractExeIcon(
                            game.ExecutablePath);
                }

                groupBattleNet
                    .Elements
                    .Add(item);
            }
        }
        private async Task LoadGogGamesAsync()
        {
            try
            {
                

                List<GameInfo> games =
                    await Task.Run(() =>
                    {
                        return gogscanner.ScanGames();
                    });
                
                groupGOG.Elements.Clear();

                foreach (GameInfo game in games)
                {
                    AccordionControlElement item =
                        new AccordionControlElement();

                    item.Text =
                        game.Name;

                    item.Style =
                        ElementStyle.Item;

                    item.Tag =
                        game;

                    if (File.Exists(game.ShortcutPath))
                    {
                        try
                        {
                            item.ImageOptions.Image =
                                IconHelper.ExtractExeIcon(
                                    game.ShortcutPath);
                        }
                        catch(Exception ex)
                        {
                            Program.LogCrash(ex);
                            XtraMessageBox.Show("ERROR: " + ex.Message);
                        }
                    }

                    groupGOG.Elements.Add(item);
                    
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                XtraMessageBox.Show(ex.ToString());
            }

        }
        private async Task LoadEAGamesAsync()
        {
            try
            {
                

                List<GameInfo> games =
                    await Task.Run(() =>
                    {
                        return eascanner.ScanGames();
                    });

                groupEA.Elements.Clear();

                foreach (GameInfo game in games)
                {
                    AccordionControlElement item =
                        new AccordionControlElement();

                    item.Text =
                        game.Name;

                    item.Style =
                        ElementStyle.Item;

                    item.Tag =
                        game;

                    try
                    {
                        string iconSource =
                            !string.IsNullOrWhiteSpace(
                                game.ShortcutPath)
                            ? game.ShortcutPath
                            : game.ExecutablePath;

                        if (System.IO.File.Exists(
                            iconSource))
                        {
                            item.ImageOptions.Image =
                                IconHelper.ExtractExeIcon(
                                    iconSource);
                        }
                    }
                    catch
                    {
                    }

                    groupEA.Elements.Add(item);
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                XtraMessageBox.Show(
                    ex.ToString());
            }
        }
        private async Task LoadUbisoftGamesAsync()
        {
            try
            {
               

                List<GameInfo> games =
                    await Task.Run(() =>
                    {
                        return ubisoftscanner.ScanGames();
                    });

                groupUbisoft.Elements.Clear();

                foreach (GameInfo game in games)
                {
                    AccordionControlElement item =
                        new AccordionControlElement();

                    item.Text =
                        game.Name;

                    item.Style =
                        ElementStyle.Item;

                    item.Tag =
                        game;

                    try
                    {
                        string iconSource =
                            !string.IsNullOrWhiteSpace(
                                game.ExecutablePath)
                            ? game.ExecutablePath
                            : game.ShortcutPath;

                        if (File.Exists(
                            iconSource))
                        {
                            item.ImageOptions.Image =
                                IconHelper.ExtractExeIcon(
                                    iconSource);
                        }
                    }
                    catch
                    {
                    }

                    groupUbisoft.Elements.Add(item);
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                XtraMessageBox.Show(
                    ex.ToString());
            }
        }
        private async Task LoadXboxGamesAsync()
        {
            try
            {
                XboxScannerService scanner =
                    new XboxScannerService();

                List<GameInfo> games =
                    await Task.Run(() =>
                    {
                        return scanner.ScanGames();
                    });

                groupXbox.Elements.Clear();

                foreach (GameInfo game in games)
                {
                    AccordionControlElement item =
                        new AccordionControlElement();

                    item.Text =
                        game.Name;

                    item.Style =
                        ElementStyle.Item;

                    item.Tag =
                        game;

                    groupXbox.Elements.Add(
                        item);
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                XtraMessageBox.Show(
                    ex.ToString());
            }
        }
        private Image ResizeImage(
    Image image,
    int width,
    int height)
        {
            Bitmap bmp =
                new Bitmap(width, height);

            using (Graphics g =
                Graphics.FromImage(bmp))
            {
                g.InterpolationMode =
                    System.Drawing.Drawing2D
                    .InterpolationMode.HighQualityBicubic;

                g.DrawImage(
                    image,
                    0,
                    0,
                    width,
                    height);
            }

            return bmp;
        }
        private void accordionControl2_StateChanged(object sender, EventArgs e)
        {
            // BeginUpdate stops DevExpress controls from repainting during batch property changes
            accordionControl2.BeginUpdate();
            try
            {
                bool isMinimized = accordionControl2.OptionsMinimizing.State == AccordionControlState.Minimized;
                bool isNormal = accordionControl2.OptionsMinimizing.State == AccordionControlState.Normal;

                if (isMinimized || isNormal)
                {
                    splitContainerControl1.SplitterPosition = isMinimized ? 72 : 420;
                    bool setVisible = !isMinimized; // false if Minimized, true if Normal

                    foreach (AccordionControlElement group in accordionControl2.Elements)
                    {
                        foreach (AccordionControlElement item in group.Elements)
                        {
                            if (item.Style == ElementStyle.Item)
                            {
                                item.Visible = setVisible;
                            }
                        }
                    }
                }
            }
            finally
            {
                // Unlocks control rendering and redraws everything once
                accordionControl2.EndUpdate();
            }
        }

        private async void accordionControl2_ContextButtonClick(object sender, DevExpress.Utils.ContextItemClickEventArgs e)
        {
            
            // Get accordion element
            AccordionControlElement element =
                e.DataItem as AccordionControlElement;

            // Get clicked button
            ContextButton button =
                e.Item as ContextButton;

            if (element == null)
                return;

            // Get launcher info
            LauncherInfo launcher =
                element.Tag as LauncherInfo;
            if ( button.Name == "refreshSteam")
            {
                await RefreshSteamLibraryAsync();

            }
            if ( button.Name == "refreshBattlenet")
            {
                await RefreshBattleNetLibraryAsync();
            }
            if (button.Name == "refreshEpic")
            {
                await RefreshEpicLibraryAsync();
            }
            if (button.Name == "refreshGOG")
            {
                await RefreshGogLibraryAsync();
            }
            if (button.Name == "refreshEA")
            {
                await RefreshEALibraryAsync();
            }
            if (button.Name == "refreshUbisoft")
            {
                await RefreshUbisoftLibraryAsync();
            }
            if (button.Name == "nexusStore")
                {
                // Handle Nexus Store launch
                try
                {
                    nexusStore.Visible = true;
                    nexusStore.BringToFront();
                    launcherCard.BringToFront();
                    launcherCard.xtraTabControl1.SelectedTabPage = launcherCard.xtraTabPage4;
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                    XtraMessageBox.Show("Failed to open Nexus Store: " + ex.Message);
                }
            }

            if (button.Name == "launchWindows")
            {

                // Handle Windows Store launch
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = "ms-windows-store:",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                    XtraMessageBox.Show("Failed to launch Windows Store: " + ex.Message);
                }
            }
            else if (button.Name == "launchXbox")
            {
                // Handle Xbox App launch
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = "xbox:",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                    XtraMessageBox.Show(ex.ToString());
                }
            }

            if (launcher == null)
                return;

            

            if (button == null)
                return;

            // OPEN FOLDER
            if (button.Name == "openFolder")
            {
                if (!Directory.Exists(
                    launcher.InstallPath))
                {
                    XtraMessageBox.Show(
                        "Folder not found.");

                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName =
                        launcher.InstallPath,

                    UseShellExecute = true
                });
            }
          
            // LAUNCH CLIENT
            else if (button.Name == "launchClient")
            {
                { 
                    if (!File.Exists(
                    launcher.ExecutablePath))
                    {
                        XtraMessageBox.Show(
                            "Launcher executable not found.");

                        return;
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName =
                            launcher.ExecutablePath,

                        WorkingDirectory =
                            launcher.InstallPath,

                        UseShellExecute = true
                    });
                    
                }
                
            }
            else
            {
                
            }
        }
        private void SaveCurrentTheme()
        {
            ThemeSettingsManager.Save(
                UserLookAndFeel.Default.SkinName,
                UserLookAndFeel.Default.ActiveSvgPaletteName);
        }
        private void accordionControlElement6_Click(object sender, EventArgs e)
        {

        }

        private void accordionControl2_ElementClick(object sender, ElementClickEventArgs e)
        {
            // Get game from Tag
            GameInfo game =
                e.Element.Tag as GameInfo;
            applicationCard.BringToFront();
            applicationCard._header = null;
            applicationCard._icon = null;
            applicationCard._library = null;
            nexusStore.Visible = true;
            
            //applicationCard._selectedGroup = null;
            // Ignore groups
            if (e.Element.Style != ElementStyle.Item)
            { 
                if (e.Element == groupSteam)
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "Steam";
                    launcherCard._selectedGroup = "Steam";
                    nexusStore.NexusStoreUrl = "https://store.steampowered.com/";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    applicationCard._gameStoreLink = nexusStore.NexusStoreUrl;
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "Steam";
                    launcherCard.clientIcon = Resources.Steam_icon_logo_svg; // Replace with actual path to Steam icon
                    launcherCard.isNexusLauncher = false;
                    launcherCard.HideGOGSettings();
                    launcherCard.HideEASettings();
                    ResumeLayout();
                }
                if (e.Element == groupEpic)
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "Epic Games";
                    launcherCard._selectedGroup = "Epic Games";
                    nexusStore.NexusStoreUrl = "https://www.epicgames.com/store/";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "Epic Games";
                    launcherCard.clientIcon = Resources.epic_games_black_logo_icon_147139; // Replace with actual path to Epic Games icon
                    launcherCard.isNexusLauncher = false;
                    launcherCard.HideGOGSettings();
                    launcherCard.HideEASettings();
                    ResumeLayout();
                }
                if (e.Element == groupNexus)
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "Nexus Launcher";
                    launcherCard._selectedGroup = "Nexus Launcher";
                    nexusStore.NexusStoreUrl = "https://horizonsocial.media/apps/nexus.html";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "Nexus Launcher";
                    launcherCard.clientIcon = Resources.dfveffb_9b262552_e352_4348_aefc_8e699002c946; // Replace with actual path to Nexus Launcher icon
                    launcherCard.isNexusLauncher = true;
                    launcherCard.HideGOGSettings();
                    launcherCard.HideEASettings();
                    ResumeLayout();
                }
                if (e.Element == groupBattleNet)
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "Battle.net";
                    launcherCard._selectedGroup = "Battle.net";
                    nexusStore.NexusStoreUrl = "https://us.shop.battle.net/en-us#optLogin=true";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    //change pictureboxedit2 sizemode
                    //applicationCard.pictureEdit2.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom;
                    //applicationCard.pictureEdit2.Properties.ZoomPercent = 45;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "Battle.net";
                    launcherCard.clientIcon = Resources.unnamed; // Replace with actual path to Battle.net icon
                    launcherCard.isNexusLauncher = false;
                    launcherCard.HideGOGSettings();
                    launcherCard.HideEASettings();
                    ResumeLayout();
                }
                if (e.Element == groupGOG)
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "GOG";
                    launcherCard._selectedGroup = "GOG";
                    nexusStore.NexusStoreUrl = "https://www.gog.com/en/games";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "GOG";
                    launcherCard.clientIcon = Resources.d8ac3b01ba19729174a8f1e63c9e937c; // Replace with actual path to GOG Galaxy icon
                    launcherCard.isNexusLauncher = false;
                    launcherCard.ShowGOGSettings();
                    launcherCard.HideEASettings();
                    ResumeLayout();
                }
                if (e.Element == groupUbisoft)
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "Ubisoft Connect";
                    launcherCard._selectedGroup = "Ubisoft Connect";
                    nexusStore.NexusStoreUrl = "https://store.ubisoft.com/us/home?lang=en_US";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "Ubisoft Connect";
                    launcherCard.clientIcon = Resources._1024x1024; // Replace with actual path to Ubisoft Connect icon
                    launcherCard.isNexusLauncher = false;
                    launcherCard.HideGOGSettings();
                    launcherCard.HideEASettings();
                    ResumeLayout();
                }
                if (e.Element == groupParadox)
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "Paradox Launcher";
                    launcherCard._selectedGroup = "Paradox Launcher";
                    nexusStore.NexusStoreUrl = "https://www.paradoxinteractive.com/our-games/all-games?amount=20";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "Paradox Launcher";
                    launcherCard.clientIcon = Resources.Paradox_Interactive_logo; // Replace with actual path to Paradox Launcher icon
                    launcherCard.isNexusLauncher = false;
                    launcherCard.HideGOGSettings();
                    launcherCard.HideEASettings();
                    ResumeLayout();
                }
                if (e.Element == groupAmazon)
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "Amazon Games";
                    nexusStore.NexusStoreUrl = "https://games.amazon.com/en-us";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "Amazon Games";
                    launcherCard.clientIcon = Resources.amazon_games_icon; // Replace with actual path to Amazon Games Game Center icon
                    launcherCard.isNexusLauncher = false;
                    launcherCard.HideGOGSettings();
                    launcherCard.HideEASettings();
                    ResumeLayout();
                }
                if (e.Element == groupEA)
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "EA App";
                    launcherCard._selectedGroup = "EA App";
                    nexusStore.NexusStoreUrl = "https://www.ea.com/games/library/pc-download";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "EA App";
                    launcherCard.clientIcon = Resources._256x256; // Replace with actual path to EA App icon
                    launcherCard.isNexusLauncher = false;
                    launcherCard.HideGOGSettings();
                    launcherCard.ShowEASettings();
                    ResumeLayout();
                }
                if (e.Element == groupWargaming)
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "Wargaming";
                    nexusStore.NexusStoreUrl = "https://wargaming.net/en";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "Wargaming";
                    launcherCard.clientIcon = Resources.wargaming; // Replace with actual path to Wargaming icon
                    launcherCard.isNexusLauncher = false;
                    launcherCard.HideGOGSettings();
                    launcherCard.HideEASettings();
                    ResumeLayout();
                }
                if (e.Element == groupXbox)
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "Xbox";
                    nexusStore.NexusStoreUrl = "https://www.xbox.com/en-US/games/browse/Popular?PlayWith=PC";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "Xbox";
                    launcherCard.clientIcon = Resources.apps_60199_13798539581762600_abe1643f_1704_4a4d_a61b_47ccc25da012; // Replace with actual path to Windows Store icon
                    launcherCard.isNexusLauncher = false;
                    launcherCard.HideGOGSettings();
                    launcherCard.HideEASettings();
                    ResumeLayout();
                }
                if (e.Element == groupWindowsStore)
                {
                    SuspendLayout();
                    launcherCard.BringToFront();
                    applicationCard._selectedGroup = "Windows Store";
                    nexusStore.NexusStoreUrl = "https://apps.microsoft.com/games?hl=en-US&gl=US";
                    nexusStore.webView21.Source = new Uri(nexusStore.NexusStoreUrl);
                    launcherCard.Visible = true;
                    applicationCard.Visible = false;
                    launcherCard.clientName = "Windows Store";
                    launcherCard.clientIcon = Resources.apps_22118_9007199266252480_94f4e265_68d4_4ddc_a67b_b29c8d3021c8; // Replace with actual path to GOG Galaxy icon
                    launcherCard.isNexusLauncher = false;
                    launcherCard.HideGOGSettings();
                    launcherCard.HideEASettings();
                    ResumeLayout();
                }
                
            }

            
            if (game == null)
                return;
            try
            {
                applicationCard.Visible = true;
                applicationCard._gameID = game.AppId;
                applicationCard._goglnk = game.ShortcutPath;
                applicationCard._EAShortuct = game.ExecutablePath;
                applicationCard._gameURI = game.ExecutablePath;
                applicationCard._UbisoftURI = game.LaunchUri;
                applicationCard._launchString = game.epicLauncherAppId;
                applicationCard._name = game.Name;
                applicationCard._executablePath = game.ExecutablePath;
                applicationCard._productID = game.ProductId;
                applicationCard.splitContainerControl1.SplitterPosition = 420;


                if (File.Exists(game.LogoPath))
                {
                    applicationCard._icon = Image.FromFile(game.LogoPath);
                }
                else
                {
                    //applicationCard._icon = Resources.NAicon;
                }
                if (File.Exists(game.HeaderImagePath))
                {
                    applicationCard._header = Image.FromFile(game.HeaderImagePath);
                }
                else
                {
                    applicationCard._header = Resources.NAHE;
                }
                if (File.Exists(game.LibraryImagePath))
                {
                    applicationCard._library = Image.FromFile(game.LibraryImagePath);
                }
                else if(File.Exists(game.HeaderImagePath))
                {
                    applicationCard._library = Image.FromFile(game.HeaderImagePath);
                }
                else
                {
                   //applicationCard._library = Resources.NA;
                }

                RefreshGameArtwork(game);

                // Launch through Steam
                //Process.Start(
                //    "steam://rungameid/" +
                //    game.AppId);
                if (game.Launcher == "Steam")
                {

                    applicationCard.webView21.Source = new Uri("https://store.steampowered.com/app/" + game.AppId);
                    applicationCard._gameStoreLink = "https://store.steampowered.com/app/" + game.AppId;
                    //MessageBox.Show("Nexus Launcher - Steam - " + applicationCard.webView21.Source);
                }
                if (game.Launcher == "Epic Games")
                {
                    string formattedName = game.Name.Replace(" ", "-").Replace("'", "").ToLower();
                    applicationCard.webView21.Source = new Uri("https://store.epicgames.com/p/" + formattedName);
                    applicationCard._gameStoreLink = "https://store.epicgames.com/p/" + formattedName;
                    //MessageBox.Show("Nexus Launcher - Epic Games - " + applicationCard.webView21.Source);
                }
                if (game.Launcher == "Battle.net" && game.Name == "StarCraft")
                {
                    string formattedName = game.Name.Replace(" ", "-").Replace("'", "").ToLower() + "-remastered";
                    applicationCard.webView21.Source = new Uri("https://us.shop.battle.net/en-us/product/" + formattedName);
                    applicationCard._gameStoreLink = "https://us.shop.battle.net/en-us/product/" + formattedName;
                    //MessageBox.Show("Nexus Launcher - Battle.net - " + applicationCard.webView21.Source);
                }
                else if (game.Launcher == "Battle.net")
                {
                    string formattedName = game.Name.Replace(" ", "-").Replace("'", "").ToLower();
                    applicationCard.webView21.Source = new Uri("https://us.shop.battle.net/en-us/product/" + formattedName);
                    applicationCard._gameStoreLink = "https://us.shop.battle.net/en-us/product/" + formattedName;
                    //MessageBox.Show("Nexus Launcher - Battle.net - " + applicationCard.webView21.Source);
                }
                if (game.Launcher == "GOG")
                {
                    string formattedName = game.Name.Replace(" - ", "_").Replace(" ", "_").Replace("-", "_").ToLower();
                    applicationCard.webView21.Source = new Uri("https://www.gog.com/en/game/" + formattedName);
                    applicationCard._gameStoreLink = "https://www.gog.com/en/game/" + formattedName;
                    //MessageBox.Show("Nexus Launcher - GOG - " + applicationCard.webView21.Source);
                }
                if (game.Launcher == "EA")
                {
                    string gameSerise;
                    if (game.Name.Contains("FIFA"))
                    {
                        gameSerise = "fifa";
                    }
                    else if (game.Name.Contains("Battlefield"))
                    {
                        gameSerise = "battlefield";
                    }
                    else if (game.Name.Contains("Need for Speed"))
                    {
                        gameSerise = "need-for-speed";
                    }
                    else if (game.Name.Contains("The Sims"))
                    {
                        gameSerise = "the-sims";
                    }
                    else if (game.Name.Contains("Apex Legends"))
                    {
                        gameSerise = "apex-legends";
                    }
                    else if (game.Name.Contains("Star Wars"))
                    {
                        gameSerise = "star-wars";
                    }
                    else if (game.Name.Contains("Command and Conquer") || game.Name.Contains("Command & Conquer"))
                    {
                        gameSerise = "command-and-conquer";
                    }
                    else
                    {
                        gameSerise = game.Name;
                    }
                    string formattedName1 = gameSerise + "/";
                    string formattedName2 = game.Name.Replace("&", "and").Replace("™", "").Replace(" - ", "-").Replace(" ", "-").Replace("'", "").ToLower();
                    applicationCard.webView21.Source = new Uri("https://www.ea.com/games/"+ formattedName1 + formattedName2);
                    //MessageBox.Show("Nexus Launcher - EA App - " + applicationCard.webView21.Source);
                }
                if (game.Launcher == "Ubisoft" || game.Launcher == "Nexus Launcher" || game.Launcher == string.Empty)
                {
                    
                    applicationCard.webView21.Source = new Uri("https://guardbyte.me/downloads/Nexus%20Launcher/store-not-supported.html");
                    //MessageBox.Show("Nexus Launcher - Ubisoft Connect - " + applicationCard.webView21.Source);
                  
                }
                
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                // XtraMessageBox.Show( ex.Message + " "+ ex.Source+ ex.InnerException, "test");
            }
        }

        private void accordionControl2_FilterContent(object sender, FilterContentEventArgs e)
        {
            if (e.FilterValue != null && e.FilterValue.ToString() != "")
                accordionControl2.RootDisplayMode = AccordionControlRootDisplayMode.Default;
            else
            {
                accordionControl2.RootDisplayMode = AccordionControlRootDisplayMode.Footer;
                if (Settings.Default.RootDisplayMode == true)
                {
                    accordionControl2.RootDisplayMode = AccordionControlRootDisplayMode.Footer;
                }
                else
                {
                    accordionControl2.RootDisplayMode = AccordionControlRootDisplayMode.Default;
                }
                if (Settings.Default.ViewType == true)
                {
                    accordionControl2.ViewType = AccordionControlViewType.HamburgerMenu;
                }
                else
                {
                    accordionControl2.ViewType = AccordionControlViewType.Standard;
                }
                accordionControl2.Refresh();
            }
            
        }

        private void accordionControl2_SelectedElementChanged(object sender, SelectedElementChangedEventArgs e)
        {
           
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Debug size
            //this.Text = "Nexus Launcher - " + this.Size.Width + "x" + this.Size.Height;
        }

        private void skinBarSubItem2_ItemPress(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            
        }

        private void MainView_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Save theme on every close event
            SaveCurrentTheme();
            // if appExit is false then continue with flow
            if (appExit == false) {
                // if setting to minimize on close is true
                if (Settings.Default.MinimizeOnClose)
                {
                    // Cancel closing form
                    e.Cancel = true;
                    // fix for max window from reshowing
                    if (this.WindowState == FormWindowState.Maximized)
                    {
                        // set to normal
                        this.WindowState = FormWindowState.Normal;
                    }
                    // hide mainview and get notifyicon ready
                    Hide();
                    ShowInTaskbar = false;
                    notifyIcon1.Visible = true;
                    // check to show notify icon once per run
                    if (notiShown == false)
                    {
                        notifyIcon1.ShowBalloonTip(
                        3000,
                        "Nexus Launcher",
                        "Nexus Launcher is still running in the system tray.",
                        ToolTipIcon.Info);
                        notiShown = true;
                    }

                    return;
                }
                // else ask if you really want to exit
                else
                {
                    var result = XtraMessageBox.Show(
                "Are you sure you want to exit Nexus Launcher?",
                "Exit Nexus Launcher",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
                    // cancel if result no
                    if (result == DialogResult.No)
                    {
                        e.Cancel = true;
                    }
                    if (result == DialogResult.Yes)
                    {
                        // Dispose watchers and cancel tokens
                        disposeClose();
                        Environment.Exit(0);
                    }

                }
            }

            disposeClose();
            

        }
        public void disposeClose()
        {
            //Dispose anything needed to end
            steamWatcher?.Dispose();
            steamReloadToken?.Cancel();
            steamReloadToken?.Dispose();
            battleNetWatcher?.Dispose();
            battleNetReloadToken?.Cancel();
            battleNetReloadToken?.Dispose();
            gogWatcher?.Dispose();
            gogReloadToken?.Cancel();
            gogReloadToken?.Dispose();
            epicWatcher?.Dispose();
            epicReloadToken?.Cancel();
            epicReloadToken?.Dispose();
            ubisoftWatcher?.Dispose();
            ubisoftReloadToken?.Cancel();
            ubisoftReloadToken?.Dispose();
            DisposeEAWatchers();
            eaReloadToken?.Cancel();
            eaReloadToken?.Dispose();
            notifyIcon1.Visible = false;
            notifyIcon1.Dispose();
            
            Thread.Sleep(2000);
            
        }
        private void barButtonItem5_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            SettingsForm settingsForm = new SettingsForm(this);
            settingsForm.ShowDialog();
        }

        private void barButtonItem3_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            
            //AboutForm aboutForm = new AboutForm();  
            //aboutForm.ShowDialog();

            SettingsForm settingsForm = new SettingsForm(this);
            settingsForm.TabSelected = 8;
            settingsForm.ShowDialog();
            Activate();

            
            
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();

            WindowState =
                FormWindowState.Normal;

            ShowInTaskbar = true;

            notifyIcon1.Visible = false;

            Activate();
        }

        private void barButtonItem7_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // debugging code for Wargaming Game Center, can be removed later
            //groupWargaming.Visible = true;
        }

        private void barButtonItem8_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                popupMenu1.ShowPopup(Cursor.Position);
            }
        }

        private void barButtonItem9_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            Show();

            WindowState =
                FormWindowState.Normal;

            ShowInTaskbar = true;

            notifyIcon1.Visible = false;

            Activate();
        }

        private void barButtonItem10_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            
            Environment.Exit(0);
            //Application.Exit();
            
        }

        private void barButtonItem4_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            userAccount.BringToFront();
            userAccount.Visible = true;
        }

        private void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            Show();

            WindowState =
                FormWindowState.Normal;

            ShowInTaskbar = true;

            notifyIcon1.Visible = false;

            Activate();
        }

        private void barButtonItem11_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            //restart Neuxs
            var mess = XtraMessageBox.Show("Do you want to restart Nexus Launcher?", "Restarting...", MessageBoxButtons.YesNo);
            if (mess == DialogResult.Yes)
            {
                appExit = true;
                Program._mutex.Dispose();

                Application.Restart();
                Environment.Exit(0);
            }
        }

        private void barButtonItem12_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var mess = XtraMessageBox.Show("Do you really want to shutdown Nexus Launcher?", "Shutting Down", MessageBoxButtons.YesNo);
            if (mess == DialogResult.Yes)
            {
                appExit = true;
                Program._mutex.Dispose();
                this.Close();
                Environment.Exit(0);
            }
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            if (simpleButton2.Text == ">")
            {
                SuspendLayout();
                groupControl1.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
                groupControl2.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
                groupControl3.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
                sidePanel1.BorderThickness = 0;
                sidePanel1.Size = new Size(50, 0);
                foreach (Control control in groupControl1.Controls)
                {
                    control.Visible = false;
                }
                foreach (Control control in groupControl2.Controls)
                {
                    control.Visible = false;
                }
                foreach (Control control in groupControl3.Controls)
                {
                    control.Visible = false;
                }
                simpleButton2.Text = "<";
                ResumeLayout();

                return;
            }
            else if (simpleButton2.Text == "<")
            {
                SuspendLayout();
                groupControl1.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Default;
                groupControl2.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Default;
                groupControl3.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Default;
                sidePanel1.Size = new Size(300, 0);
                foreach (Control control in groupControl1.Controls)
                {
                    control.Visible = true;
                }
                foreach (Control control in groupControl2.Controls)
                {
                    control.Visible = true;
                }
                foreach (Control control in groupControl3.Controls)
                {
                    control.Visible = true;
                }
                simpleButton2.Text = ">";
                ResumeLayout();
                return;
            }
        }

        private async void MainView_Shown(object sender, EventArgs e)
        {
           
        }

        private void barButtonItem13_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            accordionControl2.SuspendLayout();
            try
            {
                // Set the state first so the event fires cleanly
                accordionControl2.OptionsMinimizing.State = AccordionControlState.Minimized;

                // If you want everything collapsed when they expand it later:
                //accordionControl2.CollapseAll();

                fullLibrary.Show();
                fullLibrary.BringToFront();
                fullLibrary.Visible = true;
                fullLibrary.PopulateLibrary();
            }
            finally
            {
                accordionControl2.ResumeLayout();
            }
        }
        private void ArtworkService_ArtworkDownloaded(GameInfo game)
        {
            System.Diagnostics.Debug.WriteLine(
        "UI EVENT: " + game.Name);
            if (InvokeRequired)
            {
                BeginInvoke(new Action<GameInfo>(
                    ArtworkService_ArtworkDownloaded), game);

                return;
            }

            RefreshGameArtwork(game);
        }
        private void RefreshGameArtwork(GameInfo game)
        {
            if (File.Exists(game.GridImagePath))
            {
                applicationCard.pictureEdit1.Image = null;
                applicationCard.pictureEdit1.Image =
                    Image.FromFile(game.GridImagePath);
            }
            else
            {
                applicationCard.pictureEdit1.Image = Properties.Resources.NA;
            }

            if (File.Exists(game.HeroImagePath))
            {
                applicationCard.pictureEdit2.Image = null;
                applicationCard.pictureEdit2.Image =
                    Image.FromFile(game.HeroImagePath);
            }
            else
            {
                applicationCard.pictureEdit2.Image = Properties.Resources.NAHE;
            }
            if (accordionControl2.ActiveGroup == groupNexus)
            {
                applicationCard.pictureEdit2.Image = GetRandomNexusHeader();
                applicationCard.pictureEdit1.Image = IconHelper.ExtractExeIcon(game.ExecutablePath);
                applicationCard.pictureEdit1.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Squeeze;
                applicationCard.pictureEdit1.Properties.ZoomPercent = -65;
                applicationCard.Refresh();
            }
            else
            {
                applicationCard.pictureEdit1.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Stretch;
                applicationCard.pictureEdit2.Properties.ZoomPercent = 100;
                applicationCard.Refresh();
            }
            //if (File.Exists(game.LogoPath))
            //{
            //    pictureLogo.Image =
            //        Image.FromFile(game.LogoPath);
            //}
        }
        private readonly Random _random = new Random();

        private Image GetRandomNexusHeader()
        {
            Image[] headers =
            {
        Properties.Resources.nexusHeader1,
        Properties.Resources.nexusHeader2,
        Properties.Resources.nexusHeader3
    };

            return headers[_random.Next(headers.Length)];
        }
        public void ReleaseArtworkImages()
        {
            if (applicationCard.pictureEdit1.Image != null)
            {
                applicationCard.pictureEdit1.Image.Dispose();
                applicationCard.pictureEdit1.Image = null;
            }

            if (applicationCard.pictureEdit2.Image != null)
            {
                applicationCard.pictureEdit2.Image.Dispose();
                applicationCard.pictureEdit2.Image = null;
            }

            ArtworkCache.ClearCache();
            ArtworkService.RedownloadAllArtwork();
        }
        private void BuildGameContextMenu()
        {
            gameContextMenu.Items.Clear();

            gameContextMenu.Items.Add(
                "Play",
                null,
                PlayGame_Click);

            gameContextMenu.Items.Add(
                "Browse Local Files",
                null,
                BrowseFiles_Click);

            gameContextMenu.Items.Add(
                "Open Install Folder",
                null,
                OpenFolder_Click);

            gameContextMenu.Items.Add(
                new ToolStripSeparator());

            gameContextMenu.Items.Add(
                "Uninstall",
                null,
                Uninstall_Click);
        }
        private void PlayGame_Click(
    object sender,
    EventArgs e)
        {
            if (selectedGame == null)
                return;

            //LaunchGame(selectedGame);
        }

        private void BrowseFiles_Click(
            object sender,
            EventArgs e)
        {
            if (selectedGame == null)
                return;

            if (!string.IsNullOrWhiteSpace(
                selectedGame.ExecutablePath))
            {
                Process.Start(
                    "explorer.exe",
                    "/select,\"" +
                    selectedGame.ExecutablePath +
                    "\"");
            }
        }

        private void OpenFolder_Click(
            object sender,
            EventArgs e)
        {
            if (selectedGame == null)
                return;

            if (Directory.Exists(
                selectedGame.InstallPath))
            {
                Process.Start(
                    "explorer.exe",
                    selectedGame.InstallPath);
            }
        }

        private void Uninstall_Click(
            object sender,
            EventArgs e)
        {
            if (selectedGame == null)
                return;

            // Steam for now
            Process.Start(
                "steam://uninstall/" +
                selectedGame.ProductId);
        }
        private void accordionControl2_Click(object sender, EventArgs e)
        {

        }

        private void fluentDesignFormControl2_Click(object sender, EventArgs e)
        {

        }

        private void accordionControl2_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            AccordionControl accordion =
                sender as AccordionControl;

            var hit =
                accordion.CalcHitInfo(e.Location);

            if (hit.ItemInfo == null)
                return;

            AccordionControlElement element =
                hit.ItemInfo.Element;

            if (element == null)
                return;

            selectedGame =
                element.Tag as GameInfo;

            if (selectedGame == null)
                return;

            //gameContextMenu.Show(accordion, e.Location);
            popupMenu2.ShowPopup(Cursor.Position);
        }

        private void contextPlay_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (applicationCard._selectedGroup == "Steam")
            {
                try
                {
                    string exePathSteam = MainView.sysDisk + @"Program Files (x86)\Steam\Steam.exe";
                    bool isSteamRunning = Process.GetProcessesByName("Steam").Any();
                    if (!isSteamRunning)
                    {
                        // Start Steam normally first so it can initialize its background hooks
                        Process.Start(new ProcessStartInfo { FileName = exePathSteam, UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(exePathSteam) });

                        // Give it 3 to 5 seconds to load up before sending the game instruction
                        Thread.Sleep(4000);
                    }
                    Process.Start(new ProcessStartInfo
                    {
                        FileName =
                    "steam://rungameid/" + applicationCard._gameID,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(applicationCard._executablePath)
                    });
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                    MessageBox.Show(
                        "Failed to launch the application: " + ex.Message);
                }

            }
            else if (applicationCard._selectedGroup == "Epic Games")
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName =
           "com.epicgames.launcher://apps/" +
           applicationCard._launchString +
           "?action=launch&silent=true",
                    UseShellExecute = true
                });
            }
            else if (applicationCard._selectedGroup == "Battle.net")
            {
                try
                {

                    string exePath = MainView.sysDisk + @"Program Files (x86)\Battle.net\Battle.net.exe";
                    string launchParams = "-nostreamline -sso -launch -uid";
                    string diabloIVParams = "-launch";
                    // 1. Force your product ID to uppercase if required by Blizzard's system
                    string cleanProductID = applicationCard._productID.ToUpper();

                    // 2. Check if Battle.net is already running in the background
                    bool isBnetRunning = Process.GetProcessesByName("Battle.net").Any();

                    if (!isBnetRunning)
                    {
                        // Start Battle.net normally first so it can initialize its background hooks
                        Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true });

                        // Give it 3 to 5 seconds to load up before sending the game instruction
                        Thread.Sleep(4000);
                    }

                    // 3. Send the execution command (Now it will successfully trigger the game!)
                    //Process.Start(new ProcessStartInfo
                    //{
                    //    FileName = exePath,
                    //    Arguments = $@"--exec=""launch {cleanProductID}""",
                    //    UseShellExecute = true
                    //});
                    if (applicationCard._productID == "fenris")
                    {
                        // Explicitly launch DiabloIV with Battlenet process, passing argument
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = exePath,

                            // This forces Windows to read the correct 'battlenet://' URI association
                            Arguments = "--exec=\"launch Fen\"",
                            UseShellExecute = true,
                            Verb = "runas"
                        });

                    }
                    else
                    {

                        Process.Start(new ProcessStartInfo { FileName = applicationCard._executablePath, Arguments = launchParams + " " + applicationCard._productID, UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(applicationCard._executablePath) });
                    }

                    //MessageBox.Show(_executablePath + _productID);

                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                    MessageBox.Show(
                        "Failed to launch the application: " + ex.Message);
                }
            }
            else if (applicationCard._selectedGroup == "GOG")
            {
                try
                {

                    Process.Start(
                        new ProcessStartInfo
                        {
                            FileName =
                                applicationCard._goglnk,

                            UseShellExecute =
                                true
                        });



                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                    MessageBox.Show(
                        "Failed to launch the application: " + ex.Message);
                }
            }
            else if (applicationCard._selectedGroup == "EA App")
            {
                try
                {
                    //labelControl1.Text = _EAShortuct;
                    if (!string.IsNullOrWhiteSpace(
    applicationCard._EAShortuct) &&
    File.Exists(
        applicationCard._EAShortuct))
                    {
                        Process.Start(
                            new ProcessStartInfo
                            {
                                FileName =
                                    applicationCard._EAShortuct,
                                UseShellExecute =
                                    true,
                                WorkingDirectory = Path.GetDirectoryName(applicationCard._executablePath)
                            });

                        return;
                    }
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                }
            }
            else if (applicationCard._selectedGroup == "Ubisoft Connect")
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(
    applicationCard._UbisoftURI))
                    {
                        Process.Start(
                            new ProcessStartInfo
                            {
                                FileName =
                                   applicationCard._UbisoftURI,

                                UseShellExecute =
                                    true,
                                WorkingDirectory = Path.GetDirectoryName(applicationCard._executablePath)
                            });

                        return;
                    }
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                }
            }
            else if (applicationCard._selectedGroup == "Nexus Launcher")
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = applicationCard._executablePath, UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(applicationCard._executablePath) });
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                    MessageBox.Show(
                        "Failed to launch the application: " + ex.Message);
                }
            }
        }

        private void contextVerify_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (applicationCard._selectedGroup == "Steam")
            {
                if (XtraMessageBox.Show(
    "You are about to verify / repair " + applicationCard._name +"\nThis will open Steam and start validating and may take some time to finish!\n\nDo you want to continue?",
    "Verify & Repair",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }
                Process.Start("steam://validate/" + applicationCard._gameID);
            }
            else if (applicationCard._selectedGroup == "Epic Games")
            {
                
            }
            else if (applicationCard._selectedGroup == "Battle.net")
            {
                
            }
            else if (applicationCard._selectedGroup == "GOG")
            {
                
            }
            else if (applicationCard._selectedGroup == "EA App")
            {
                
            }
            else if (applicationCard._selectedGroup == "Ubisoft Connect")
            {
                
                
            }
            else if (applicationCard._selectedGroup == "Nexus Launcher")
            {
                
            }
        }

        private void contextUninstall_ItemClick(object sender, ItemClickEventArgs e)
        {

            //if (applicationCard._selectedGroup == "Steam")
            //{
            //    Process.Start("steam://uninstall/" + applicationCard._gameID);
            //}
            //else if (applicationCard._selectedGroup == "Epic Games")
            //{

            //}
            //else if (applicationCard._selectedGroup == "Battle.net")
            //{

            //}
            //else if (applicationCard._selectedGroup == "GOG")
            //{

            //}
            //else if (applicationCard._selectedGroup == "EA App")
            //{

            //}
            //else if (applicationCard._selectedGroup == "Ubisoft Connect")
            //{


            //}
            //else if (applicationCard._selectedGroup == "Nexus Launcher")
            //{

            //}
            if (selectedGame == null)
                return;

            UninstallerService.UninstallResult result =
                UninstallerService.Uninstall(
                    selectedGame);
            if (!result.Success)
            {
                XtraMessageBox.Show(
                "Launcher: " + selectedGame.Launcher +
                "\n\nGame: " + selectedGame.Name +
                "\n\nInstall Path: " + selectedGame.InstallPath +
                "\n\nProduct ID: " + selectedGame.ProductId +
                "\n\nSuccess: " + result.Success +
                "\n\nMethod: " + result.Method +
                "\n\nDetails:\n" + result.Message,
                "Uninstall Debug",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            }
            
        }

        private void contextBrowse_ItemClick(object sender, ItemClickEventArgs e)
        {
            Process.Start(
            "explorer.exe",
            "/select,\"" +
            applicationCard._executablePath +
            "\"");
        }

        private void contextCopyName_ItemClick(object sender, ItemClickEventArgs e)
        {
            Clipboard.SetText(applicationCard._name);
        }

        private void contextCopyID_ItemClick(object sender, ItemClickEventArgs e)
        {
            Clipboard.SetText(applicationCard._gameID.ToString());
        }

        private void contextCopyGPath_ItemClick(object sender, ItemClickEventArgs e)
        {
            Clipboard.SetText(applicationCard._executablePath);
        }

        private void contextCopyFPath_ItemClick(object sender, ItemClickEventArgs e)
        {
            string inputFolder = applicationCard._executablePath;
            int lastIndex = inputFolder.LastIndexOf('\\');
            if (lastIndex != -1)
            {
                // Remove starting from the last backslash to the end of the string
                string result = inputFolder.Substring(0, lastIndex);
                Clipboard.SetText(result);
            }
           
        }

        private void barButtonItem14_ItemClick(object sender, ItemClickEventArgs e)
        {
            updateAvailable = true;
            TEST test = new TEST();
            test.ShowDialog();
        }

        private void badge1_Click(object sender, EventArgs e)
        {
            barButtonItem3.PerformClick();
        }
    }    
}
