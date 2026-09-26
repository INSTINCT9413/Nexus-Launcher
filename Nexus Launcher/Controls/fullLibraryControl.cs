using DevExpress.LookAndFeel;
using DevExpress.Utils.Menu;
using DevExpress.XtraEditors;
using Nexus_Launcher.Controls.Library;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Models;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services;
using Nexus_Launcher.Services.Library;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls
{
    /// <summary>
    /// The Full Library: a filterable poster grid that opens a detail
    /// page when a game is clicked.
    ///
    /// The control is two pages stacked in the same space. The grid
    /// page holds the filter bar and the poster grid; the detail page
    /// holds one game. Only one is visible at a time, and the grid keeps
    /// its scroll position while the detail page is up.
    /// </summary>
    public partial class fullLibraryControl : DevExpress.XtraEditors.XtraUserControl
    {
        private LibraryFilterBar filterBar;

        private LibraryGridView grid;

        private LibraryGameDetailView detail;

        private Panel gridPage;

        /// <summary>
        /// Back to the grid, in the header rather than on the artwork.
        ///
        /// It takes the place of the header's title while a game is
        /// open: the game's name is already the heading of the page
        /// below, so showing it twice only cost the button a home.
        /// </summary>
        private readonly SimpleButton backButton = new SimpleButton();

        private LibraryFilter filter = new LibraryFilter();

        /// <summary>
        /// Total games in the library, so the count can read
        /// "Showing 12 of 148".
        /// </summary>
        private int totalGames;

        private bool built;

        public fullLibraryControl()
        {
            InitializeComponent();

            Build();

            LibraryOrganizationService.Changed += Organization_Changed;

            Services.Artwork.CustomArtworkService.CustomArtworkChanged +=
                Artwork_Changed;

            UserLookAndFeel.Default.StyleChanged += Style_Changed;

            Disposed += (s, e) =>
            {
                LibraryOrganizationService.Changed -=
                    Organization_Changed;

                Services.Artwork.CustomArtworkService
                    .CustomArtworkChanged -= Artwork_Changed;

                UserLookAndFeel.Default.StyleChanged -= Style_Changed;
            };
        }

        //--------------------------------------------------------------
        // Construction
        //--------------------------------------------------------------

        private void Build()
        {
            if (built)
                return;

            built = true;

            SuspendLayout();

            try
            {
                // The grid page. Filter bar on top, grid filling the
                // rest; the grid is added first and brought to the
                // front so docking lays the bar out before it.
                gridPage = new Panel();

                gridPage.Dock = DockStyle.Fill;

                grid = new LibraryGridView();

                grid.Dock = DockStyle.Fill;

                grid.GameActivated += Grid_GameActivated;

                grid.FavoriteToggled += ToggleFavorite;

                grid.GameContextMenu += Grid_ContextMenu;

                filterBar = new LibraryFilterBar();

                filterBar.Dock = DockStyle.Top;

                filterBar.FilterChanged += FilterBar_Changed;

                filterBar.TileSizeChanged += size =>
                    grid.TileWidth = size;

                // Thumbnail keys are size specific, so a resize needs a
                // rebuild rather than just a repaint.
                grid.TileSizeChanged += Rebuild;

                filterBar.RefreshRequested += Rebuild;

                gridPage.Controls.Add(grid);

                gridPage.Controls.Add(filterBar);

                grid.BringToFront();

                // The detail page sits in the same space, hidden.
                detail = new LibraryGameDetailView();

                detail.Dock = DockStyle.Fill;

                detail.Visible = false;


                detail.FavoriteToggled += game =>
                {
                    Rebuild();

                    grid.FocusGame(game);
                };

                Controls.Add(detail);

                Controls.Add(gridPage);

                BuildBackButton();

                // panelControl1 is the docked header from the designer
                // and has to stay behind both pages.
                panelControl1.SendToBack();

                gridPage.BringToFront();
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        /// <summary>
        /// The header's back button, lined up with the poster on the
        /// detail page so the page reads as one column.
        /// </summary>
        private void BuildBackButton()
        {
            backButton.Text = "  Back to library";

            backButton.ImageOptions.SvgImage =
                ProfileStyle.Svg(
                    "svgimages/icon%20builder/actions_arrow2left.svg");

            backButton.ImageOptions.SvgImageSize =
                new Size(16, 16);

            backButton.AllowFocus = false;

            backButton.Visible = false;

            backButton.Click += (s, e) => ShowGrid();

            panelControl1.Controls.Add(backButton);

            panelControl1.Resize += (s, e) => LayoutBackButton();

            LayoutBackButton();
        }

        /// <summary>
        /// Positioned by hand rather than docked: it has to line up
        /// with the poster below it, and the header's own padding is
        /// not the same as the detail page's margin.
        /// </summary>
        private void LayoutBackButton()
        {
            const int posterLeft = 32;

            const int width = 158;

            const int height = 30;

            // Padding only moves docked children, so this is already
            // the header's own left edge and matches the poster.
            int left = posterLeft;

            int top =
                Math.Max(
                    0,
                    (panelControl1.ClientSize.Height - height) / 2);

            backButton.SetBounds(left, top, width, height);
        }

        /// <summary>
        /// The animation setting changed, so the detail page should
        /// decide again whether to play.
        /// </summary>
        public void RefreshArtworkAnimation()
        {
            if (detail != null && !detail.IsDisposed)
                detail.RefreshAnimation();
        }

        //--------------------------------------------------------------
        // First time here
        //--------------------------------------------------------------

        /// <summary>
        /// Points out the parts of this page that are not obvious, the
        /// first time it is opened.
        ///
        /// Run from OnVisibleChanged rather than Load: the page is
        /// built with the window and would otherwise introduce itself
        /// to someone who has not looked at it yet.
        /// </summary>
        private void ShowFirstRunTips()
        {
            if (tipsQueued || !Visible)
                return;

            if (TutorialService.HasSeen(TutorialService.FullLibrary))
                return;

            tipsQueued = true;

            // Queued rather than shown: the service waits until this
            // page is actually in front before putting markers on it,
            // and takes them away again if something covers it. This
            // page shares its space with the game card, the store and
            // the profile, so being Visible is not the same as being
            // the page on screen.
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed)
                    return;

                TutorialService.ShowOnce(
                    FindForm(),
                    TutorialService.FullLibrary,
                    new TutorialStep
                    {
                        Title = "Everything, in one place",
                        Body =
                            "Every game from every launcher is here. " +
                            "Click a poster to open it, then play it " +
                            "without going back to its own launcher.",
                        Target = () => grid
                    },
                    new TutorialStep
                    {
                        Title = "Find things quickly",
                        Body =
                            "Search, filter by launcher, show only " +
                            "favourites or installed games, group them, " +
                            "and change how big the posters are.",
                        Target = () => filterBar,
                        Location =
                            DevExpress.Utils.VisualEffects
                                .GuideFlyoutLocation.Bottom
                    },
                    new TutorialStep
                    {
                        Title = "Make it yours",
                        Body =
                            "A game's page lets you set your own " +
                            "artwork, including animated gifs for the " +
                            "banner, and mark it as a favourite.",
                        Target = () => simpleButton1,
                        Location =
                            DevExpress.Utils.VisualEffects
                                .GuideFlyoutLocation.Bottom
                    });
            }));
        }

        private bool tipsQueued;

        /// <summary>
        /// Guides were reset in Settings, so this page offers its own
        /// again without the user having to navigate away and back.
        /// </summary>
        private void Tutorials_Reset()
        {
            tipsQueued = false;

            if (IsDisposed)
                return;

            ShowFirstRunTips();
        }

        protected override void OnVisibleChanged(
            EventArgs e)
        {
            base.OnVisibleChanged(e);

            ShowFirstRunTips();
        }

        private void fullLibraryControl_Load(
            object sender,
            EventArgs e)
        {
            TutorialService.Reset += Tutorials_Reset;

            Disposed += (s, a) => TutorialService.Reset -= Tutorials_Reset;

            FontManager.ApplyFont(
                this,
                Settings.Default.UIFont);

            ApplyTheme();
        }

        //--------------------------------------------------------------
        // Populating
        //--------------------------------------------------------------

        /// <summary>
        /// Reloads the library from scratch. Called by MainView after a
        /// scan and whenever artwork finishes downloading.
        /// </summary>
        public void PopulateLibrary()
        {
            if (!built)
                return;

            filterBar.ReloadSources();

            Rebuild();
        }

        /// <summary>
        /// Re-runs the current filter against the library. Cheap enough
        /// to call whenever favourites or groups change.
        /// </summary>
        private void Rebuild()
        {
            if (!built)
                return;

            try
            {
                totalGames = LibraryService.Games.Count;

                List<LibrarySection> sections =
                    LibraryQueryService.Build(
                        filter,
                        new Size(
                            grid.TileWidth,
                            (int)Math.Round(grid.TileWidth * 1.5)));

                grid.SetSections(sections);

                int shown =
                    sections.Sum(x => x.Entries.Count);

                filterBar.SetCount(shown, totalGames);

                labelControl1.Text = "Full Library";
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        private void FilterBar_Changed(
            LibraryFilter value)
        {
            filter = value ?? new LibraryFilter();

            Rebuild();
        }

        /// <summary>
        /// Favourites or groups changed somewhere else in the app, for
        /// example from the accordion's context menu.
        /// </summary>
        private void Organization_Changed()
        {
            if (IsDisposed || !IsHandleCreated || !Visible)
                return;

            try
            {
                BeginInvoke(new Action(() =>
                {
                    filterBar.ReloadSources();

                    Rebuild();
                }));
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// The user replaced or cleared a game's artwork, so the
        /// prepared thumbnail keys for it are stale.
        /// </summary>
        private void Artwork_Changed(
            GameInfo changed)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke(new Action(Rebuild));
            }
            catch (Exception)
            {
            }
        }

        //--------------------------------------------------------------
        // Navigation
        //--------------------------------------------------------------

        private void Grid_GameActivated(
            GameInfo game)
        {
            if (game == null)
                return;

            LibrarySelectionService.Select(game);

            detail.SetGame(game);

            detail.Visible = true;

            detail.BringToFront();

            gridPage.Visible = false;

            // The game's name is the heading of the page below, so the
            // header gives its place to the way back instead.
            labelControl1.Visible = false;

            backButton.Visible = true;

            LayoutBackButton();

            backButton.BringToFront();
        }

        /// <summary>
        /// Back to the grid, with the game that was open still focused
        /// so the user knows where they were.
        /// </summary>
        private void ShowGrid()
        {
            GameInfo previous = detail.Game;

            detail.Visible = false;

            gridPage.Visible = true;

            gridPage.BringToFront();

            backButton.Visible = false;

            labelControl1.Visible = true;

            labelControl1.Text = "Full Library";

            grid.FocusGame(previous);

            grid.Focus();
        }

        //--------------------------------------------------------------
        // Context menu
        //--------------------------------------------------------------

        private void Grid_ContextMenu(
            GameInfo game,
            Point location)
        {
            if (game == null)
                return;

            bool favorite =
                LibraryOrganizationService.IsFavorite(game);

            DXPopupMenu menu = new DXPopupMenu();

            menu.Items.Add(Item(
                "Play",
                "svgimages/icon%20builder/actions_arrow4right.svg",
                (s, e) => Play(game)));

            menu.Items.Add(Item(
                favorite
                    ? "Remove from Favourites"
                    : "Add to Favourites",
                "svgimages/icon%20builder/actions_star.svg",
                (s, e) => ToggleFavorite(game)));

            menu.Items.Add(new DXMenuItem("-"));

            menu.Items.Add(Item(
                "Open details",
                "svgimages/icon%20builder/actions_info.svg",
                (s, e) => Grid_GameActivated(game)));

            menu.Items.Add(Item(
                "Show only " + (game.Launcher ?? "this launcher"),
                "svgimages/icon%20builder/actions_filter.svg",
                (s, e) => filterBar.SelectLauncher(game.Launcher)));

            menu.ShowPopup(grid, location);
        }

        private static DXMenuItem Item(
            string caption,
            string svgKey,
            EventHandler handler)
        {
            DXMenuItem item =
                new DXMenuItem(caption, handler);

            item.SvgImage = ProfileStyle.Svg(svgKey);

            return item;
        }

        private void Play(
            GameInfo game)
        {
            try
            {
                GameLauncherService.Launch(game);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        private void ToggleFavorite(
            GameInfo game)
        {
            if (game == null)
                return;

            LibraryOrganizationService.ToggleFavorite(game);

            // The organization event rebuilds the grid; focus the game
            // again afterwards so it does not get lost in the re-sort.
            grid.FocusGame(game);
        }

        //--------------------------------------------------------------
        // Theme
        //--------------------------------------------------------------

        private void Style_Changed(
            object sender,
            EventArgs e)
        {
            if (IsDisposed)
                return;

            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (!built)
                return;

            panelControl1.Appearance.BackColor =
                ProfileStyle.CardColor;

            panelControl1.Appearance.Options.UseBackColor = true;

            labelControl1.Appearance.ForeColor =
                ProfileStyle.TextColor;

            labelControl1.Appearance.Options.UseForeColor = true;

            filterBar.ApplyTheme();

            detail.ApplyTheme();

            Invalidate(true);
        }

        //--------------------------------------------------------------
        // Designer wiring
        //--------------------------------------------------------------

        private void simpleButton1_Click(
            object sender,
            EventArgs e)
        {
            // A manual refresh is also the way out of stale artwork, so
            // the thumbnails are dropped rather than reused.
            LibraryThumbnailCache.Clear();

            PopulateLibrary();
        }
    }
}
