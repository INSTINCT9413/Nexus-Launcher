using DevExpress.DXTemplateGallery.Extensions;
using DevExpress.Utils.Menu;
using DevExpress.XtraEditors;
using Microsoft.Web.WebView2.Core;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Models;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services.Artwork;
using Nexus_Launcher.Services.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AdamsLair.WinForms.OriginSelector;

namespace Nexus_Launcher.Controls
{

    public partial class ApplicationCard : XtraUserControl
    {
        public string _name { get; set; }
        public string _launchString { get; set; }
        public string _goglnk { get; set; }
        public string _EAShortuct { get; set; }
        public string _UbisoftURI { get; set; }
        public int _gameID { get; set; }
        public string _gameURI { get; set; }
        public string _productID { get; set; }
        public string _appUserModelId { get; set; }
        public Image _header { get; set; }
        public Image _icon { get; set; }
        public Image _logo { get; set; }
        public Image _library { get; set; }
        public string _selectedGroup { get; set; }
        public string _executablePath { get; set; }
        public string _gameStoreLink { get; set; }
        private bool _syncingZoom = false;
        private bool _syncingFavorite = false;
        private GameInfo _currentGame;

        /// <summary>
        /// The game the card is showing. Needed by the favourite star,
        /// which has to know which game to act on: the other fields on
        /// this card are loose values and cannot identify one.
        /// </summary>
        public GameInfo CurrentGame
        {
            get
            {
                return _currentGame;
            }
            set
            {
                _currentGame = value;

                RefreshFavoriteDisplay();

                RefreshPlayStatsDisplay();

                UpdateArtworkMenuState();
            }
        }

        /// <summary>
        /// Divider between the stats, which all sit on one line.
        /// </summary>
        private const string StatSeparator = "   |   ";

        /// <summary>
        /// Fills labelControl4 with this game's play history on a
        /// single line: total time, the length of the last session and
        /// how many times it has been launched.
        /// </summary>
        public void RefreshPlayStatsDisplay()
        {
            if (labelControl4 == null)
                return;

            if (_currentGame == null)
            {
                labelControl4.Text = string.Empty;

                return;
            }

            GamePlayStats stats =
                PlayTrackingService.GetStats(
                    _currentGame);

            if (stats == null || stats.LaunchCount == 0)
            {
                labelControl4.Text = "Never played";

                return;
            }

            StringBuilder text =
                new StringBuilder();

            // Play time is measured by watching the game's process, so
            // it is flagged when a session could not be measured rather
            // than quietly reporting a number that is too low.
            text.Append("Total play time: ");

            if (stats.TotalPlaySeconds > 0)
            {
                text.Append(
                    SidePanelPresenter.FormatDuration(
                        TimeSpan.FromSeconds(
                            stats.TotalPlaySeconds)));

                if (stats.UntrackedSessions > 0)
                    text.Append(" (approx)");
            }
            else
            {
                text.Append("Not measured");
            }

            text.Append(StatSeparator);

            text.Append("Last session: ");

            text.Append(
                stats.LastSessionSeconds > 0
                    ? SidePanelPresenter.FormatDuration(
                        TimeSpan.FromSeconds(
                            stats.LastSessionSeconds))
                    : "Not measured");

            text.Append(StatSeparator);

            text.Append("Launches: ");

            text.Append(stats.LaunchCount);

            labelControl4.Text =
                text.ToString();
        }

        private void PlayTracking_Changed()
        {
            // Raised from the session watcher on a worker thread.
            if (IsDisposed || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke(new Action(
                    RefreshPlayStatsDisplay));
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        /// <summary>
        /// Raised after the star is clicked, so the accordion can move
        /// the game to or from the top of its launcher.
        /// </summary>
        public event EventHandler FavoriteChanged;

        /// <summary>
        /// Points the star at the stored favourite state without
        /// letting it read back as a user click.
        /// </summary>
        public void RefreshFavoriteDisplay()
        {
            _syncingFavorite = true;

            try
            {
                ratingControl1.Enabled =
                    _currentGame != null;

                bool favorite =
                    _currentGame != null &&
                    LibraryOrganizationService.IsFavorite(
                        _currentGame);

                ratingControl1.Rating =
                    favorite
                        ? 1
                        : 0;

                ratingControl1.ToolTip =
                    favorite
                        ? "Remove from Favourites"
                        : "Add to Favourites";
            }
            finally
            {
                _syncingFavorite = false;
            }
        }
        public ApplicationCard()
        {
            InitializeComponent();

            // Nothing is selected yet, so the star starts disabled.
            RefreshFavoriteDisplay();

            // Fill the hero banner without stretching it out of shape
            // when the window or side panel changes its width.
            PictureCover.Attach(pictureEdit2);

            // The designer sizes this for the word "DEBUG:". Left to
            // auto size so the single stats line grows to fit rather
            // than being clipped or wrapped.
            labelControl4.AutoSizeMode =
                LabelAutoSizeMode.Default;

            labelControl4.Appearance.TextOptions.WordWrap =
                DevExpress.Utils.WordWrap.NoWrap;

            labelControl4.Appearance.Options.UseTextOptions = true;

            RefreshPlayStatsDisplay();

            PlayTrackingService.Changed +=
                PlayTracking_Changed;

            CustomArtworkService.CustomArtworkChanged +=
                CustomArtwork_Changed;

            // Everything artwork related lives on this one button.
            dropDownButton7.Text = "Artwork";

            AttachArtworkMenu(dropDownButton7);
        }

        private void dropDownButton1_Click(object sender, EventArgs e)
        {
            // Launching is done inline here rather than through
            // GameLauncherService, so report it for play tracking.
            PlayTrackingService.RecordLaunch(_currentGame);

            if (_selectedGroup == "Steam")
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
                    Process.Start( new ProcessStartInfo
                    {
                        FileName =
                    "steam://rungameid/" + _gameID,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(_executablePath)
                    });
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                    MessageBox.Show(
                        "Failed to launch the application: " + ex.Message);
                }
                
            }
            else if (_selectedGroup == "Epic Games")
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName =
           "com.epicgames.launcher://apps/" +
           _launchString +
           "?action=launch&silent=true",
                    UseShellExecute = true
                });
            }
            else if (_selectedGroup == "Battle.net")
            {
                try
                {

                    string exePath = MainView.sysDisk + @"Program Files (x86)\Battle.net\Battle.net.exe";
                    string launchParams = "-nostreamline -sso -launch -uid";
                    string diabloIVParams = "-launch";
                    // 1. Force your product ID to uppercase if required by Blizzard's system
                    string cleanProductID = _productID.ToUpper();

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
                    if(_productID == "fenris")
                    {
                       // Explicitly launch DiabloIV with Battlenet process, passing argument
                        Process.Start(new ProcessStartInfo {
                            FileName = exePath,

                            // This forces Windows to read the correct 'battlenet://' URI association
                            Arguments = "--exec=\"launch Fen\"",
                            UseShellExecute = true, Verb="runas" });
                        
                    }
                    else
                    {
                       
                        Process.Start(new ProcessStartInfo { FileName = _executablePath, Arguments = launchParams + " " + _productID, UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(_executablePath) });
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
            else if (_selectedGroup == "GOG")
            {
                try
                {
                   
                        Process.Start(
                            new ProcessStartInfo
                            {
                                FileName =
                                    _goglnk,

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
            else if (_selectedGroup == "EA App")
            {
                try
                {
                    //labelControl1.Text = _EAShortuct;
                    if (!string.IsNullOrWhiteSpace(
    _EAShortuct) &&
    File.Exists(
        _EAShortuct))
                    {
                        Process.Start(
                            new ProcessStartInfo
                            {
                                FileName =
                                    _EAShortuct,
                                UseShellExecute =
                                    true,
                                WorkingDirectory = Path.GetDirectoryName(_executablePath)
                            });

                        return;
                    }
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                }
            }
            else if (_selectedGroup == "Ubisoft Connect")
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(
    _UbisoftURI))
                    {
                        Process.Start(
                            new ProcessStartInfo
                            {
                                FileName =
                                   _UbisoftURI,

                                UseShellExecute =
                                    true,
                                WorkingDirectory = Path.GetDirectoryName(_executablePath)
                            });

                        return;
                    }
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                }
            }
            else if (_selectedGroup == "Xbox")
            {
                if (!Nexus_Launcher.Services.XboxScannerService.LaunchGame(
                    _appUserModelId))
                {
                    MessageBox.Show(
                        "Failed to launch the application: " + _name);
                }
            }
            else if (_selectedGroup == "Nexus Launcher")
            {
                try
                {
                    Process.Start( new ProcessStartInfo { FileName = _executablePath, UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(_executablePath) });
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                    MessageBox.Show(
                        "Failed to launch the application: " + ex.Message);
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            labelControl2 .Text = _executablePath;
            labelControl1.Text = _name;
            dropDownButton1.Text = "Play ";
            
            //pictureEdit1.Image = _library;
            //pictureEdit2.Image = _header;
        }

        private async void ApplicationCard_Load(object sender, EventArgs e)
        {
            FontManager.ApplyFont(
    this,
    Settings.Default.UIFont);
            try
            {
                await InitializeBrowserAsync();

                webView21.CoreWebView2.NewWindowRequested -=
                    CoreWebView2_NewWindowRequested;

                webView21.CoreWebView2.NewWindowRequested +=
                    CoreWebView2_NewWindowRequested;

                webView21.CoreWebView2.NavigationCompleted +=
                    CoreWebView2_NavigationCompleted;

                webView21.Source =
                    new Uri(_gameStoreLink);
                //sharedEnvironment = webView21.CoreWebView2.Environment;
            }
            catch (Exception ex)
            {
               // XtraMessageBox.Show(ex.ToString(),"Browser Error");
            }

        }
        public async Task InitializeBrowserAsync()
        {
            if (webView21.CoreWebView2 != null)
                return;

            string profilePath =
                Path.Combine(
                    Application.StartupPath,
                    "BrowserProfile");

            Directory.CreateDirectory(
                profilePath);

            CoreWebView2Environment env =
                await CoreWebView2Environment.CreateAsync(
                    null,
                    profilePath);

             
            await webView21.EnsureCoreWebView2Async();
            webView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled =
                true;

            webView21.CoreWebView2.Settings.AreDevToolsEnabled =
                true;

            webView21.CoreWebView2.Settings.IsStatusBarEnabled =
                false;

            webView21.CoreWebView2.Settings.IsZoomControlEnabled =
                true;

            webView21.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled =
                true;
            UpdateZoomLabel(100);
        }
        private void CoreWebView2_NewWindowRequested(
            object sender,
            CoreWebView2NewWindowRequestedEventArgs e)
        {
            try
            {
                e.Handled = true;

                webView21.CoreWebView2.Navigate(
                    e.Uri);
            }
            catch
            {
            }
        }

        private void CoreWebView2_NavigationCompleted(
            object sender,
            CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                if (!e.IsSuccess)
                {
                    // XtraMessageBox.Show("Failed to load page.","Nexus Store");
                }
            }
            catch
            {
            }
        }

        public void Navigate(
            string url)
        {
            try
            {
                if (webView21.CoreWebView2 == null)
                    return;

                webView21.CoreWebView2.Navigate(
                    url);
            }
            catch
            {
            }
        }

        public void GoBack()
        {
            if (webView21.CoreWebView2?.CanGoBack == true)
            {
                webView21.CoreWebView2.GoBack();
            }
        }

        public void GoForward()
        {
            if (webView21.CoreWebView2?.CanGoForward == true)
            {
                webView21.CoreWebView2.GoForward();
            }
        }

        public void RefreshPage()
        {
            webView21.CoreWebView2?.Reload();
        }

        public void NavigateHome()
        {
            
        }

        private void webView21_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
          webView22.Visible = true;
          webView22.BringToFront();
        }

        private void webView21_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            webView22.Visible = false;
        }

        private void dropDownButton2_Click(object sender, EventArgs e)
        {
            webView21.Reload();
        }

        private void dropDownButton3_Click(object sender, EventArgs e)
        {
            splitContainerControl1.SplitterPosition = 0;
            webView21.Dock = DockStyle.None;
            webView22.Dock = DockStyle.None;
            webView22.Size = new Size(splitContainerControl1.Panel2.Width, splitContainerControl1.Panel2.Height - 5 );
            webView21.Size = new Size(splitContainerControl1.Panel2.Width, splitContainerControl1.Panel2.Height - 5 );
            webView21.Location = new Point(0, 35);
            webView22.Location = new Point(0, 35);
            webView22.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            webView21.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panelControl3.Visible = true;
        }

        private void dropDownButton4_Click(object sender, EventArgs e)
        {
           
        }

        private void dropDownButton5_Click(object sender, EventArgs e)
        {
            webView21.Reload();
        }

        private void dropDownButton4_Click_1(object sender, EventArgs e)
        {
            splitContainerControl1.SplitterPosition = 420;
            panelControl3.Visible = false;
            webView21.Dock = DockStyle.Fill;
            webView22.Dock = DockStyle.Fill;
            webView21.Location = new Point(0, 0);
            webView22.Location = new Point(0, 0);
            UpdateZoomLabel(100);
        }

        private void zoomTrackBarControl1_EditValueChanged(object sender, EventArgs e)
        {

        }

        private void zoomTrackBarControl1_ValueChanged(object sender, EventArgs e)
        {
            if (_syncingZoom)
                return;

            int zoomPercent = zoomTrackBarControl1.Value;

            UpdateZoomLabel(zoomPercent);

            if (webView21 == null || webView21.CoreWebView2 == null)
                return;

            try
            {
                _syncingZoom = true;

                webView21.ZoomFactor = zoomPercent / 100.0;
            }
            finally
            {
                _syncingZoom = false;
            }
        }

        private void webView21_ZoomFactorChanged(object sender, EventArgs e)
        {
            if (_syncingZoom)
                return;

            if (webView21 == null || webView21.CoreWebView2 == null)
                return;

            try
            {
                _syncingZoom = true;

                int zoomPercent =
                    (int)Math.Round(webView21.ZoomFactor * 100.0);

                if (zoomPercent < zoomTrackBarControl1.Properties.Minimum)
                    zoomPercent = zoomTrackBarControl1.Properties.Minimum;

                if (zoomPercent > zoomTrackBarControl1.Properties.Maximum)
                    zoomPercent = zoomTrackBarControl1.Properties.Maximum;

                zoomTrackBarControl1.Value = zoomPercent;

                UpdateZoomLabel(zoomPercent);
            }
            finally
            {
                _syncingZoom = false;
            }
        }
        public void UpdateZoomLabel(int zoomPercent)
        {
            labelControl3.Text = zoomPercent + "%";
        }
        private void dropDownButton6_Click(object sender, EventArgs e)
        {
            zoomTrackBarControl1.Value = 100;
        }

        //--------------------------------------------------------------
        // Custom artwork
        //
        // pictureEdit1 is the grid image, pictureEdit2 the hero banner.
        // Hook these straight up to buttons. Every one of them is a
        // no-op when no game is selected, so they are safe to call from
        // anywhere.
        //--------------------------------------------------------------

        //--------------------------------------------------------------
        // Artwork drop down menu
        //--------------------------------------------------------------

        private DXPopupMenu artworkMenu;
        private DXMenuItem artworkSetGrid;
        private DXMenuItem artworkSetHero;
        private DXMenuItem artworkRemoveGrid;
        private DXMenuItem artworkRemoveHero;
        private DXMenuItem artworkReset;
        private DXMenuItem artworkOpenFolder;

        /// <summary>
        /// The artwork menu, built on first use.
        ///
        /// A DXPopupMenu rather than a bar PopupMenu because this
        /// control has no BarManager to hang the latter off.
        /// </summary>
        public DXPopupMenu ArtworkMenu
        {
            get
            {
                if (artworkMenu == null)
                    BuildArtworkMenu();

                return artworkMenu;
            }
        }

        /// <summary>
        /// Hangs the artwork menu off a drop down button. Call this
        /// once with the button that should own it.
        /// </summary>
        public void AttachArtworkMenu(
            DropDownButton button)
        {
            if (button == null)
                return;

            button.DropDownControl = ArtworkMenu;

            UpdateArtworkMenuState();
        }

        private void BuildArtworkMenu()
        {
            artworkMenu =
                new DXPopupMenu();

            artworkSetGrid =
                CreateArtworkMenuItem(
                    "Set Grid Image...",
                    "svgimages/icon%20builder/actions_image.svg",
                    (s, e) => BrowseForCustomGridArtwork());

            artworkSetHero =
                CreateArtworkMenuItem(
                    "Set Hero Image...",
                    "svgimages/icon%20builder/electronics_photo.svg",
                    (s, e) => BrowseForCustomHeroArtwork());

            artworkRemoveGrid =
                CreateArtworkMenuItem(
                    "Remove Grid Image",
                    "svgimages/icon%20builder/actions_trash.svg",
                    (s, e) => ClearCustomGridArtwork());

            artworkRemoveGrid.BeginGroup = true;

            artworkRemoveHero =
                CreateArtworkMenuItem(
                    "Remove Hero Image",
                    "svgimages/icon%20builder/actions_trash.svg",
                    (s, e) => ClearCustomHeroArtwork());

            artworkReset =
                CreateArtworkMenuItem(
                    "Reset Artwork",
                    "svgimages/icon%20builder/actions_reload.svg",
                    (s, e) => ResetArtwork());

            artworkReset.BeginGroup = true;

            artworkOpenFolder =
                CreateArtworkMenuItem(
                    "Open Artwork Folder",
                    "svgimages/icon%20builder/actions_folderopen.svg",
                    (s, e) => OpenCustomArtworkFolder());

            artworkOpenFolder.BeginGroup = true;

            artworkMenu.Items.Add(artworkSetGrid);
            artworkMenu.Items.Add(artworkSetHero);
            artworkMenu.Items.Add(artworkRemoveGrid);
            artworkMenu.Items.Add(artworkRemoveHero);
            artworkMenu.Items.Add(artworkReset);
            artworkMenu.Items.Add(artworkOpenFolder);
        }

        private DXMenuItem CreateArtworkMenuItem(
            string caption,
            string iconKey,
            EventHandler handler)
        {
            return new DXMenuItem(
                caption,
                handler,
                LibraryGroupIcons.GetSvgImage(iconKey),
                DXMenuItemPriority.Normal);
        }

        /// <summary>
        /// Greys out what cannot be done right now, so Remove is only
        /// offered when there is custom artwork to remove.
        /// </summary>
        public void UpdateArtworkMenuState()
        {
            if (artworkMenu == null)
                return;

            bool hasGame =
                _currentGame != null;

            artworkSetGrid.Enabled = hasGame;
            artworkSetHero.Enabled = hasGame;
            artworkReset.Enabled = hasGame;
            artworkOpenFolder.Enabled = hasGame;

            artworkRemoveGrid.Enabled =
                hasGame &&
                HasCustomArtwork(ArtworkKind.Grid);

            artworkRemoveHero.Enabled =
                hasGame &&
                HasCustomArtwork(ArtworkKind.Hero);
        }

        private void CustomArtwork_Changed(
            GameInfo game)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke(new Action(
                    UpdateArtworkMenuState));
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        /// <summary>
        /// Asks the user for an image and uses it as this game's grid
        /// artwork. Returns false if they cancelled or the file could
        /// not be read.
        /// </summary>
        public bool BrowseForCustomGridArtwork()
        {
            return BrowseForCustomArtwork(
                ArtworkKind.Grid);
        }

        /// <summary>
        /// Asks the user for an image and uses it as this game's hero
        /// banner.
        /// </summary>
        public bool BrowseForCustomHeroArtwork()
        {
            return BrowseForCustomArtwork(
                ArtworkKind.Hero);
        }

        public bool BrowseForCustomArtwork(
            ArtworkKind kind)
        {
            if (_currentGame == null)
                return false;

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title =
                    kind == ArtworkKind.Hero
                        ? "Choose hero artwork"
                        : "Choose grid artwork";

                dialog.Filter =
                    CustomArtworkService.FileDialogFilter;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return false;

                return SetCustomArtwork(
                    kind,
                    dialog.FileName);
            }
        }

        /// <summary>
        /// Uses an image already on disk, for drag and drop or a path
        /// from somewhere else.
        /// </summary>
        public bool SetCustomArtwork(
            ArtworkKind kind,
            string sourceFile)
        {
            if (_currentGame == null)
                return false;

            string stored =
                CustomArtworkService.SetCustomArtwork(
                    _currentGame,
                    kind,
                    sourceFile);

            if (stored == null)
            {
                XtraMessageBox.Show(
                    this,
                    "That image could not be loaded.",
                    "Custom Artwork",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return false;
            }

            // The service raises CustomArtworkChanged, which is what
            // redraws the card and the rest of the UI.
            return true;
        }

        /// <summary>
        /// Drops the custom image so the game goes back to its
        /// downloaded artwork.
        /// </summary>
        public bool ClearCustomArtwork(
            ArtworkKind kind)
        {
            if (_currentGame == null)
                return false;

            return CustomArtworkService.RemoveCustomArtwork(
                _currentGame,
                kind);
        }

        /// <summary>
        /// The "Reset artwork" button. Drops custom artwork, throws
        /// away the downloaded copy and fetches it again where the
        /// provider supports the game.
        /// </summary>
        public void ResetArtwork()
        {
            if (_currentGame == null)
                return;

            ArtworkService.ResetArtwork(_currentGame);
        }

        public bool ClearCustomGridArtwork()
        {
            return ClearCustomArtwork(
                ArtworkKind.Grid);
        }

        public bool ClearCustomHeroArtwork()
        {
            return ClearCustomArtwork(
                ArtworkKind.Hero);
        }

        /// <summary>
        /// Whether this game is currently using artwork the user
        /// supplied. Useful for enabling a "Reset artwork" button.
        /// </summary>
        public bool HasCustomArtwork(
            ArtworkKind kind)
        {
            return _currentGame != null &&
                CustomArtworkService.HasCustom(
                    _currentGame,
                    kind);
        }

        public bool HasAnyCustomArtwork()
        {
            return _currentGame != null &&
                CustomArtworkService.HasAnyCustom(
                    _currentGame);
        }

        /// <summary>
        /// Opens the folder holding this game's custom artwork, so the
        /// user can drop files in by hand.
        /// </summary>
        public void OpenCustomArtworkFolder()
        {
            if (_currentGame == null)
                return;

            string folder =
                CustomArtworkService.GetGameFolder(
                    _currentGame,
                    true);

            if (Directory.Exists(folder))
                Process.Start("explorer.exe", folder);
        }

        private void ratingControl1_EditValueChanged(object sender, EventArgs e)
        {
            // The star is driven from ItemClick. This fires for our own
            // writes too, so it deliberately does nothing.
        }

        private void ratingControl1_ItemClick(object sender, DevExpress.XtraEditors.Repository.ItemEventArgs e)
        {
            if (_syncingFavorite || _currentGame == null)
                return;

            // With a single item the control has no empty state, so
            // clicking a lit star would leave it lit. Toggle the stored
            // value and then redraw from it.
            LibraryOrganizationService.ToggleFavorite(
                _currentGame);

            RefreshFavoriteDisplay();

            FavoriteChanged?.Invoke(
                this,
                EventArgs.Empty);
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            BrowseForCustomGridArtwork();
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            BrowseForCustomHeroArtwork();
        }

        private void dropDownButton7_Click(object sender, EventArgs e)
        {
            // Split button style, so the body raises Click while only
            // the arrow opens the menu. There is no separate default
            // action here, so the body opens it too.
            dropDownButton7.ShowDropDown();
        }
    }
}
