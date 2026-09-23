using DevExpress.LookAndFeel;
using DevExpress.XtraEditors;
using DevExpress.XtraTab;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services.Achievements;
using Nexus_Launcher.Services.Artwork;
using Nexus_Launcher.Services.Library;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows.Forms;

namespace Nexus_Launcher.Services
{
    /// <summary>
    /// The profile page: account, library statistics, the user's rig,
    /// and Nexus achievements and badges.
    ///
    /// The designer supplies the frame (three group boxes on the first
    /// tab and the Achievements tab); the content is built in code from
    /// the Controls.Profile panels, and a My Rig tab is added between
    /// the two.
    /// </summary>
    public partial class UserAccount : XtraUserControl
    {
        private const string Icon = "svgimages/icon%20builder/";

        private ProfileHeaderPanel header;
        private LibraryStatsPanel libraryStats;
        private RigSummaryPanel rigSummary;
        private RigPanel rigPanel;
        private AchievementsPanel achievements;
        private XtraTabPage rigPage;

        private bool built;
        private bool refreshQueued;

        public UserAccount()
        {
            InitializeComponent();
        }

        public static string GetUserAccountPicturePath()
        {
            try
            {
                // LOCATION 1: The Modern Windows 10/11 Public Cache (Based on User Security ID)
                string currentSid = WindowsIdentity.GetCurrent().User?.Value;
                if (!string.IsNullOrEmpty(currentSid))
                {
                    string publicAccountPictures = MainView.sysDisk + $@"Users\Public\AccountPictures\{currentSid}";
                    if (Directory.Exists(publicAccountPictures))
                    {
                        var file = new DirectoryInfo(publicAccountPictures)
                            .GetFiles("*.jpg")
                            .OrderByDescending(f => f.LastWriteTime)
                            .FirstOrDefault();

                        if (file != null) return file.FullName;
                    }
                }

                // LOCATION 2: Fallback to the Roaming AppData Folder
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appDataPictures = Path.Combine(appData, @"Microsoft\Windows\AccountPictures");
                if (Directory.Exists(appDataPictures))
                {
                    var file = new DirectoryInfo(appDataPictures)
                        .GetFiles()
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault();

                    if (file != null) return file.FullName;
                }

                // LOCATION 3: Fallback to the System-Wide Default Placeholder
                string programDataPictures = MainView.sysDisk + @"ProgramData\Microsoft\User Account Pictures";
                if (Directory.Exists(programDataPictures))
                {
                    // Windows stores default variations here like user-192.png, user.bmp, etc.
                    string defaultPic = Path.Combine(programDataPictures, "user-192.png");
                    if (File.Exists(defaultPic)) return defaultPic;

                    string fallbackBmp = Path.Combine(programDataPictures, "user.bmp");
                    if (File.Exists(fallbackBmp)) return fallbackBmp;
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                System.Diagnostics.Debug.WriteLine($"Failed to fetch profile picture: {ex.Message}");
            }

            return null; // No images found anywhere
        }

        private void UserAccount_Load(object sender, EventArgs e)
        {
            BuildProfile();

            FontManager.ApplyFont(
                this,
                Settings.Default.UIFont);

            RefreshProfile();
        }

        //--------------------------------------------------------------
        // Building
        //--------------------------------------------------------------

        private void BuildProfile()
        {
            if (built)
                return;

            built = true;

            SuspendLayout();

            try
            {
                // The header replaces the designer's avatar and name.
                uiAvatar1.Visible = false;
                labelControl1.Visible = false;

                header = new ProfileHeaderPanel();
                header.Dock = DockStyle.Fill;
                groupControl5.Controls.Add(header);

                // Loaded without a file lock, unlike Image.FromFile.
                header.SetUser(
                    MainView.fullUserName,
                    CustomArtworkService.LoadUnlocked(
                        GetUserAccountPicturePath()));

                libraryStats = new LibraryStatsPanel();
                libraryStats.Dock = DockStyle.Fill;
                groupControl3.Controls.Add(libraryStats);

                rigSummary = new RigSummaryPanel();
                rigSummary.Dock = DockStyle.Fill;
                rigSummary.OpenRigRequested += (s, e) =>
                    xtraTabControl1.SelectedTabPage = rigPage;
                groupControl4.Controls.Add(rigSummary);

                LayoutAccountTab();

                // My Rig gets a tab of its own. The old control was
                // designed at 747 x 809 but squeezed into a fixed
                // 328 x 265 box that could not grow.
                rigPage = new XtraTabPage();
                rigPage.Text = "My Rig";

                rigPanel = new RigPanel();
                rigPanel.Dock = DockStyle.Fill;
                rigPage.Controls.Add(rigPanel);

                xtraTabControl1.TabPages.Insert(1, rigPage);

                achievements = new AchievementsPanel();
                achievements.Dock = DockStyle.Fill;
                xtraTabPage2.Controls.Add(achievements);

                xtraTabPage1.ImageOptions.SvgImage = ProfileStyle.Svg(Icon + "actions_user.svg");
                rigPage.ImageOptions.SvgImage = ProfileStyle.Svg(Icon + "electronics_desktopwindows.svg");
                xtraTabPage2.ImageOptions.SvgImage = ProfileStyle.Svg(Icon + "actions_rating.svg");

                // The designer already gave Library Statistics a Refresh
                // header button; it just was not wired to anything.
                groupControl3.CustomButtonClick += (s, e) => RefreshProfile();

                xtraTabControl1.SelectedPageChanged += (s, e) => RefreshProfile();

                AchievementService.Changed += Data_Changed;
                UserProfileService.Changed += UserProfile_Changed;
                PlayTrackingService.Changed += Data_Changed;
                LibraryOrganizationService.Changed += Data_Changed;
                UserLookAndFeel.Default.StyleChanged += LookAndFeel_StyleChanged;

                Disposed += UserAccount_Disposed;
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        /// <summary>
        /// Header across the top, rig summary down the right, library
        /// statistics filling the rest.
        ///
        /// WinForms lays docked controls out from the back of the
        /// z-order forward, so the header goes to the back to claim the
        /// full width first and the fill control comes to the front to
        /// be laid out last.
        /// </summary>
        private void LayoutAccountTab()
        {
            groupControl5.Dock = DockStyle.Top;
            groupControl5.Height = 210;

            groupControl4.Dock = DockStyle.Right;
            groupControl4.Width = 380;

            groupControl3.Dock = DockStyle.Fill;

            groupControl3.BringToFront();
            groupControl5.SendToBack();
        }

        //--------------------------------------------------------------
        // Refreshing
        //--------------------------------------------------------------

        /// <summary>
        /// Rebuilds the statistics once and hands the same snapshot to
        /// every panel, so the header, stats and achievements can never
        /// disagree with each other.
        /// </summary>
        public void RefreshProfile()
        {
            if (!built || IsDisposed)
                return;

            try
            {
                LibraryStats stats =
                    LibraryStatsService.Build();

                header.Bind(stats);
                libraryStats.Bind(stats);
                achievements.Bind(stats);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        /// <summary>
        /// Several of these fire from worker threads and can arrive in
        /// bursts, so the refresh is marshalled to the UI thread and
        /// collapsed into one.
        /// </summary>
        private void Data_Changed()
        {
            if (IsDisposed || !IsHandleCreated || !Visible)
                return;

            if (refreshQueued)
                return;

            refreshQueued = true;

            try
            {
                BeginInvoke(new Action(() =>
                {
                    refreshQueued = false;
                    RefreshProfile();
                }));
            }
            catch (Exception ex)
            {
                refreshQueued = false;
                Program.LogCrash(ex);
            }
        }

        /// <summary>
        /// Shows the current display name on the profile header.
        /// </summary>
        public void RefreshUserName()
        {
            if (!built || IsDisposed)
                return;

            // Null picture keeps the avatar that is already loaded.
            header.SetUser(
                UserProfileService.DisplayName,
                null);
        }

        private void UserProfile_Changed()
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke(new Action(RefreshUserName));
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        private void LookAndFeel_StyleChanged(
            object sender,
            EventArgs e)
        {
            if (!built || IsDisposed)
                return;

            ProfileTheme.Apply(this);

            RefreshProfile();
        }

        protected override void OnVisibleChanged(
            EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (Visible)
                RefreshProfile();
        }

        private void UserAccount_Disposed(
            object sender,
            EventArgs e)
        {
            AchievementService.Changed -= Data_Changed;
            UserProfileService.Changed -= UserProfile_Changed;
            PlayTrackingService.Changed -= Data_Changed;
            LibraryOrganizationService.Changed -= Data_Changed;
            UserLookAndFeel.Default.StyleChanged -= LookAndFeel_StyleChanged;
        }
    }
}
