using DevExpress.Utils.Menu;
using DevExpress.XtraEditors;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Models;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services;
using Nexus_Launcher.Services.Artwork;
using Nexus_Launcher.Services.Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls.Library
{
    /// <summary>
    /// The page the Full Library opens when a poster is clicked: hero
    /// artwork, the poster, a Play button and this game's history.
    ///
    /// Related to ApplicationCard but deliberately not the same control.
    /// ApplicationCard is the accordion's per-game page and carries the
    /// embedded store browser, zoom controls and launcher plumbing; this
    /// is a read-and-play page reached from the grid, so it stays to
    /// artwork, play stats, favourites and grouping, and offers a way
    /// back to the grid.
    /// </summary>
    internal class LibraryGameDetailView : XtraUserControl
    {
        private const int HeroHeight = 240;

        private const int PosterWidth = 176;

        /// <summary>
        /// Leaves a band of artwork showing above the poster, which
        /// lines the poster up with the back button in the header.
        /// </summary>
        private const int PosterTop = 56;

        private const int Pad = 32;

        private GameInfo game;

        /// <summary>
        /// The hero band's artwork. Held as an AnimatedImage rather
        /// than a plain one because a hero may be an animated gif, and
        /// the usual loader would flatten it to its first frame before
        /// this control ever saw it.
        /// </summary>
        private AnimatedImage hero;

        /// <summary>
        /// Set while the hero is playing, so painting knows to move it
        /// on and so it can be stopped again exactly once.
        /// </summary>
        private bool heroPlaying;

        /// <summary>
        /// The form this control is sitting on, tracked so animation
        /// stops while Nexus is in the background.
        /// </summary>
        private Form watchedForm;

        /// <summary>
        /// Whether the owning form is the one being used, from its own
        /// activation events.
        ///
        /// Not Form.ActiveForm: that is null whenever the active window
        /// is not a managed form, which includes every DevExpress popup
        /// and dropdown, so the hero would freeze while a menu was
        /// open. Starts true, because assuming the window is in use is
        /// the harmless way to be wrong.
        /// </summary>
        private bool formActive = true;

        private Image poster;

        private readonly SimpleButton playButton = new SimpleButton();

        private readonly CheckButton favoriteButton = new CheckButton();

        private readonly DropDownButton artworkButton =
            new DropDownButton();

        private readonly SimpleButton folderButton = new SimpleButton();

        private readonly LabelControl statsLabel = new LabelControl();

        private readonly LabelControl pathLabel = new LabelControl();

        private bool syncingFavorite;

        private static int PosterHeight
        {
            get
            {
                return (int)(PosterWidth * 1.5);
            }
        }

        /// <summary>
        /// The poster overhangs the hero band, and everything below is
        /// measured from here so the two cannot drift apart.
        /// </summary>
        private static int PosterBottom
        {
            get
            {
                return PosterTop + PosterHeight;
            }
        }

        /// <summary>
        /// The favourite state changed here, so the grid behind can
        /// re-sort.
        /// </summary>
        public event Action<GameInfo> FavoriteToggled;

        public GameInfo Game
        {
            get
            {
                return game;
            }
        }

        public LibraryGameDetailView()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw,
                true);

            AutoScroll = true;

            Build();

            PlayTrackingService.Changed += PlayTracking_Changed;

            CustomArtworkService.CustomArtworkChanged +=
                Artwork_Changed;

            Disposed += (s, e) =>
            {
                PlayTrackingService.Changed -= PlayTracking_Changed;

                CustomArtworkService.CustomArtworkChanged -=
                    Artwork_Changed;

                WatchForm(null);

                DisposeImages();
            };
        }

        //--------------------------------------------------------------
        // Construction
        //--------------------------------------------------------------

        private void Build()
        {
            SuspendLayout();

            try
            {
                playButton.Text = "Play";

                playButton.ImageOptions.SvgImage =
                    ProfileStyle.Svg(
                        "svgimages/icon%20builder/actions_arrow4right.svg");

                playButton.ImageOptions.SvgImageSize =
                    new Size(20, 20);

                playButton.Appearance.Font =
                    ProfileStyle.Font(12F, FontStyle.Bold);

                playButton.Appearance.Options.UseFont = true;

                playButton.Click += Play_Click;

                Controls.Add(playButton);

                favoriteButton.Text = "Favourite";

                favoriteButton.AllowFocus = false;

                favoriteButton.ImageOptions.SvgImage =
                    ProfileStyle.Svg(
                        "svgimages/icon%20builder/actions_star.svg");

                favoriteButton.ImageOptions.SvgImageSize =
                    new Size(16, 16);

                favoriteButton.CheckedChanged += Favorite_Changed;

                Controls.Add(favoriteButton);

                artworkButton.Text = "Artwork";

                artworkButton.ImageOptions.SvgImage =
                    ProfileStyle.Svg(
                        "svgimages/icon%20builder/business_image.svg");

                artworkButton.ImageOptions.SvgImageSize =
                    new Size(16, 16);

                artworkButton.DropDownControl = BuildArtworkMenu();

                Controls.Add(artworkButton);

                folderButton.Text = "Open folder";

                folderButton.ImageOptions.SvgImage =
                    ProfileStyle.Svg(
                        "svgimages/icon%20builder/business_folder.svg");

                folderButton.ImageOptions.SvgImageSize =
                    new Size(16, 16);

                folderButton.Click += Folder_Click;

                Controls.Add(folderButton);

                statsLabel.AutoSizeMode =
                    LabelAutoSizeMode.None;

                statsLabel.Appearance.TextOptions.WordWrap =
                    DevExpress.Utils.WordWrap.Wrap;

                Controls.Add(statsLabel);

                pathLabel.AutoSizeMode =
                    LabelAutoSizeMode.None;

                pathLabel.Appearance.TextOptions.Trimming =
                    DevExpress.Utils.Trimming.EllipsisPath;

                Controls.Add(pathLabel);
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        //--------------------------------------------------------------
        // Binding
        //--------------------------------------------------------------

        public void SetGame(
            GameInfo value)
        {
            game = value;

            DisposeImages();

            if (game != null)
            {
                // Loaded unlocked so the user can replace their own
                // custom artwork while this page is showing it.
                hero =
                    AnimatedImage.Load(
                        game.HeroImagePath);

                poster =
                    CustomArtworkService.LoadUnlocked(
                        game.GridImagePath);

                if (poster == null)
                {
                    poster = ArtworkPlaceholder.Create(
                        game.Name,
                        new Size(
                            PosterWidth,
                            (int)(PosterWidth * 1.5)));
                }
            }

            RefreshFavorite();

            RefreshStats();

            RefreshArtworkMenu();

            pathLabel.Text =
                game == null
                    ? string.Empty
                    : (game.InstallPath ??
                        game.ExecutablePath ??
                        string.Empty);

            playButton.Enabled = game != null;

            // Scroll back to the top: this is a new page, not the same
            // one scrolled.
            AutoScrollPosition = new Point(0, 0);

            Relayout();

            UpdateHeroAnimation();

            Invalidate();
        }

        //--------------------------------------------------------------
        // Animated heroes
        //--------------------------------------------------------------

        /// <summary>
        /// Starts or stops the hero, from everything that should have a
        /// say: whether there is anything to play, whether the setting
        /// allows it, whether this page is on screen at all, and
        /// whether Nexus is the window being used.
        ///
        /// Repainting a band of artwork many times a second is not free
        /// and it is not welcome in the corner of someone's eye while
        /// they are working in another program.
        /// </summary>
        /// <summary>
        /// Re-reads the animation setting. Called when it is changed in
        /// Settings while a game page is open.
        /// </summary>
        public void RefreshAnimation()
        {
            UpdateHeroAnimation();

            Invalidate();
        }

        private void UpdateHeroAnimation()
        {
            bool shouldPlay =
                hero != null &&
                hero.IsAnimated &&
                Settings.Default.AnimateHeroArtwork &&
                Visible &&
                !IsDisposed &&
                FormIsActive();

            if (shouldPlay == heroPlaying)
                return;

            if (shouldPlay)
            {
                hero.Start(Hero_FrameChanged);

                heroPlaying = true;
            }
            else
            {
                if (hero != null)
                    hero.Stop();

                heroPlaying = false;
            }
        }

        private bool FormIsActive()
        {
            return watchedForm == null || formActive;
        }

        /// <summary>
        /// GDI+ raises this on its own animation thread, so the repaint
        /// is posted rather than done here.
        /// </summary>
        private void Hero_FrameChanged(
            object sender,
            EventArgs e)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke(new Action(InvalidateHero));
            }
            catch (Exception)
            {
                // The handle can go between the checks above and here.
            }
        }

        /// <summary>
        /// Only the hero band is repainted. Invalidating the whole page
        /// would redraw the poster, the title and every button below it
        /// on every frame.
        /// </summary>
        private void InvalidateHero()
        {
            if (IsDisposed)
                return;

            Invalidate(
                new Rectangle(
                    0,
                    AutoScrollPosition.Y,
                    ClientSize.Width,
                    HeroHeight));
        }

        protected override void OnVisibleChanged(
            EventArgs e)
        {
            base.OnVisibleChanged(e);

            UpdateHeroAnimation();
        }

        protected override void OnParentChanged(
            EventArgs e)
        {
            base.OnParentChanged(e);

            WatchForm(FindForm());
        }

        /// <summary>
        /// Follows the owning form's activation, so a hero stops
        /// playing when Nexus is not the window in front.
        /// </summary>
        private void WatchForm(
            Form form)
        {
            if (ReferenceEquals(form, watchedForm))
                return;

            if (watchedForm != null)
            {
                watchedForm.Activated -= Form_Activated;
                watchedForm.Deactivate -= Form_Deactivated;
            }

            watchedForm = form;

            if (watchedForm != null)
            {
                watchedForm.Activated += Form_Activated;
                watchedForm.Deactivate += Form_Deactivated;
            }

            UpdateHeroAnimation();
        }

        private void Form_Activated(
            object sender,
            EventArgs e)
        {
            formActive = true;

            UpdateHeroAnimation();
        }

        private void Form_Deactivated(
            object sender,
            EventArgs e)
        {
            formActive = false;

            UpdateHeroAnimation();
        }

        private void DisposeImages()
        {
            if (hero != null)
            {
                hero.Dispose();

                hero = null;

                heroPlaying = false;
            }

            if (poster != null)
            {
                poster.Dispose();

                poster = null;
            }
        }

        private void RefreshFavorite()
        {
            syncingFavorite = true;

            try
            {
                favoriteButton.Enabled = game != null;

                bool favorite =
                    game != null &&
                    LibraryOrganizationService.IsFavorite(game);

                favoriteButton.Checked = favorite;

                favoriteButton.Text =
                    favorite ? "Favourited" : "Favourite";
            }
            finally
            {
                syncingFavorite = false;
            }
        }

        /// <summary>
        /// The play history, on its own lines rather than the single
        /// line ApplicationCard uses: this page has the room.
        /// </summary>
        private void RefreshStats()
        {
            if (game == null)
            {
                statsLabel.Text = string.Empty;

                return;
            }

            GamePlayStats stats =
                PlayTrackingService.GetStats(game);

            if (stats == null || stats.LaunchCount == 0)
            {
                statsLabel.Text = "Never played";

                return;
            }

            List<string> lines = new List<string>();

            string total =
                stats.TotalPlaySeconds > 0
                    ? SidePanelPresenter.FormatDuration(
                        TimeSpan.FromSeconds(stats.TotalPlaySeconds))
                    : "Not measured";

            if (stats.TotalPlaySeconds > 0 &&
                stats.UntrackedSessions > 0)
            {
                total += " (approx)";
            }

            lines.Add("Total play time     " + total);

            lines.Add(
                "Last session          " +
                (stats.LastSessionSeconds > 0
                    ? SidePanelPresenter.FormatDuration(
                        TimeSpan.FromSeconds(stats.LastSessionSeconds))
                    : "Not measured"));

            lines.Add(
                "Last played            " +
                (stats.LastPlayedUtc.HasValue
                    ? SidePanelPresenter.FormatRelative(
                        stats.LastPlayedUtc.Value)
                    : "Never"));

            lines.Add(
                "Launches                " + stats.LaunchCount);

            statsLabel.Text =
                string.Join(Environment.NewLine, lines);
        }

        private void PlayTracking_Changed()
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke(new Action(RefreshStats));
            }
            catch (Exception)
            {
            }
        }

        private void Artwork_Changed(GameInfo changed)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            if (game == null || !ReferenceEquals(changed, game))
                return;

            try
            {
                BeginInvoke(new Action(() => SetGame(game)));
            }
            catch (Exception)
            {
            }
        }

        //--------------------------------------------------------------
        // Artwork menu
        //--------------------------------------------------------------

        private DXPopupMenu artworkMenu;

        private DXPopupMenu BuildArtworkMenu()
        {
            artworkMenu = new DXPopupMenu();

            artworkMenu.Items.Add(Item(
                "Set poster (grid) artwork...",
                "svgimages/icon%20builder/business_image.svg",
                (s, e) => Browse(ArtworkKind.Grid)));

            artworkMenu.Items.Add(Item(
                "Set hero artwork...",
                "svgimages/icon%20builder/business_picture.svg",
                (s, e) => Browse(ArtworkKind.Hero)));

            artworkMenu.Items.Add(new DXMenuItem("-"));

            artworkMenu.Items.Add(Item(
                "Remove custom poster",
                "svgimages/icon%20builder/actions_deletecircled.svg",
                (s, e) => Remove(ArtworkKind.Grid)));

            artworkMenu.Items.Add(Item(
                "Remove custom hero",
                "svgimages/icon%20builder/actions_deletecircled.svg",
                (s, e) => Remove(ArtworkKind.Hero)));

            artworkMenu.Items.Add(new DXMenuItem("-"));

            artworkMenu.Items.Add(Item(
                "Open artwork folder",
                "svgimages/icon%20builder/business_folder.svg",
                (s, e) => OpenArtworkFolder()));

            return artworkMenu;
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

        /// <summary>
        /// Greys the removals out when there is nothing to remove, so
        /// the menu says what is actually set.
        /// </summary>
        private void RefreshArtworkMenu()
        {
            if (artworkMenu == null)
                return;

            artworkButton.Enabled = game != null;

            bool hasGrid =
                game != null &&
                CustomArtworkService.HasCustom(
                    game,
                    ArtworkKind.Grid);

            bool hasHero =
                game != null &&
                CustomArtworkService.HasCustom(
                    game,
                    ArtworkKind.Hero);

            // Indices follow the order the menu was built in.
            artworkMenu.Items[3].Enabled = hasGrid;

            artworkMenu.Items[4].Enabled = hasHero;
        }

        private void Browse(
            ArtworkKind kind)
        {
            if (game == null)
                return;

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter =
                    CustomArtworkService.FileDialogFilter;

                dialog.Title =
                    kind == ArtworkKind.Grid
                        ? "Choose poster artwork for " + game.Name
                        : "Choose hero artwork for " + game.Name;

                if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                    return;

                string stored =
                    CustomArtworkService.SetCustomArtwork(
                        game,
                        kind,
                        dialog.FileName);

                if (stored == null)
                {
                    XtraMessageBox.Show(
                        FindForm(),
                        "That file could not be read as an image.",
                        "Custom Artwork",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }

        private void Remove(
            ArtworkKind kind)
        {
            if (game == null)
                return;

            CustomArtworkService.RemoveCustomArtwork(game, kind);
        }

        private void OpenArtworkFolder()
        {
            if (game == null)
                return;

            try
            {
                string folder =
                    CustomArtworkService.GetGameFolder(game, true);

                if (!string.IsNullOrEmpty(folder) &&
                    Directory.Exists(folder))
                {
                    Process.Start("explorer.exe", "\"" + folder + "\"");
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        //--------------------------------------------------------------
        // Actions
        //--------------------------------------------------------------

        private void Play_Click(
            object sender,
            EventArgs e)
        {
            if (game == null)
                return;

            try
            {
                GameLauncherService.Launch(game);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                XtraMessageBox.Show(
                    FindForm(),
                    "Nexus Launcher could not start " + game.Name + "." +
                        Environment.NewLine + Environment.NewLine +
                        ex.Message,
                    "Play",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void Favorite_Changed(
            object sender,
            EventArgs e)
        {
            if (syncingFavorite || game == null)
                return;

            LibraryOrganizationService.SetFavorite(
                game,
                favoriteButton.Checked);

            favoriteButton.Text =
                favoriteButton.Checked ? "Favourited" : "Favourite";

            Action<GameInfo> handler = FavoriteToggled;

            if (handler != null)
                handler(game);
        }

        private void Folder_Click(
            object sender,
            EventArgs e)
        {
            if (game == null)
                return;

            try
            {
                string folder = game.InstallPath;

                if (string.IsNullOrEmpty(folder) &&
                    !string.IsNullOrEmpty(game.ExecutablePath))
                {
                    folder =
                        Path.GetDirectoryName(game.ExecutablePath);
                }

                if (string.IsNullOrEmpty(folder) ||
                    !Directory.Exists(folder))
                {
                    XtraMessageBox.Show(
                        FindForm(),
                        "Nexus Launcher does not have an install " +
                            "folder recorded for this game.",
                        "Open Folder",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    return;
                }

                Process.Start("explorer.exe", "\"" + folder + "\"");
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        //--------------------------------------------------------------
        // Layout
        //--------------------------------------------------------------

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            Relayout();
        }

        private void Relayout()
        {
            int width = ClientSize.Width;

            // The poster hangs off the bottom of the hero, with the
            // title and buttons in the column to its right.
            int posterX = Pad;

            int column = posterX + PosterWidth + 28;

            int right = Math.Max(column + 200, width - Pad);

            // Below whichever of the hero band and the poster reaches
            // further down, so the buttons never land on the artwork.
            int y =
                Math.Max(HeroHeight, PosterBottom) + 22;

            playButton.SetBounds(column, y, 150, 44);

            favoriteButton.SetBounds(column + 160, y + 7, 124, 30);

            artworkButton.SetBounds(column + 294, y + 7, 116, 30);

            folderButton.SetBounds(column + 420, y + 7, 128, 30);

            y += 62;

            statsLabel.SetBounds(
                posterX,
                y,
                Math.Max(200, right - posterX),
                90);

            pathLabel.SetBounds(
                posterX,
                y + 98,
                Math.Max(200, right - posterX),
                20);

            int contentBottom =
                Math.Max(
                    pathLabel.Bottom,
                    statsLabel.Bottom) + Pad;

            AutoScrollMinSize =
                new Size(0, contentBottom);

            ApplyTheme();
        }

        public void ApplyTheme()
        {
            BackColor = ProfileStyle.CardColor;

            statsLabel.Appearance.ForeColor =
                ProfileStyle.TextColor;

            statsLabel.Appearance.Options.UseForeColor = true;

            statsLabel.Appearance.Font =
                ProfileStyle.Font(9.5F);

            statsLabel.Appearance.Options.UseFont = true;

            pathLabel.Appearance.ForeColor =
                ProfileStyle.MutedTextColor;

            pathLabel.Appearance.Options.UseForeColor = true;

            pathLabel.Appearance.Font =
                ProfileStyle.Font(8.5F);

            pathLabel.Appearance.Options.UseFont = true;
        }

        //--------------------------------------------------------------
        // Painting
        //--------------------------------------------------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.TranslateTransform(
                AutoScrollPosition.X,
                AutoScrollPosition.Y);

            using (SolidBrush brush =
                new SolidBrush(ProfileStyle.CardColor))
            {
                g.FillRectangle(
                    brush,
                    new Rectangle(
                        0,
                        0,
                        ClientSize.Width,
                        Math.Max(
                            ClientSize.Height,
                            AutoScrollMinSize.Height)));
            }

            PaintHero(g);

            PaintPoster(g);

            PaintTitle(g);
        }

        private void PaintHero(Graphics g)
        {
            Rectangle bounds =
                new Rectangle(0, 0, ClientSize.Width, HeroHeight);

            Image art =
                hero == null ? null : hero.Image;

            if (art != null)
            {
                // Moves a playing gif on to whichever frame is due.
                // Doing it here rather than on the timer keeps it to
                // the moments the frame is actually drawn.
                hero.Advance();

                // Cover rather than stretch: crop the overflow instead
                // of distorting the art.
                double scale = Math.Max(
                    (double)bounds.Width / art.Width,
                    (double)bounds.Height / art.Height);

                int drawWidth =
                    (int)Math.Ceiling(art.Width * scale);

                int drawHeight =
                    (int)Math.Ceiling(art.Height * scale);

                Region clip = g.Clip;

                g.SetClip(bounds);

                g.InterpolationMode =
                    InterpolationMode.HighQualityBicubic;

                g.DrawImage(
                    art,
                    new Rectangle(
                        (bounds.Width - drawWidth) / 2,
                        (bounds.Height - drawHeight) / 2,
                        drawWidth,
                        drawHeight));

                g.Clip = clip;
            }
            else
            {
                using (LinearGradientBrush brush =
                    new LinearGradientBrush(
                        bounds,
                        ProfileStyle.Shift(ProfileStyle.CardColor, 14),
                        ProfileStyle.Shift(ProfileStyle.CardColor, -6),
                        LinearGradientMode.Vertical))
                {
                    g.FillRectangle(brush, bounds);
                }
            }

            if (art != null)
            {
                // A wash across the top of the artwork, so it settles
                // under the header bar instead of butting straight
                // against it.
                Rectangle top =
                    new Rectangle(0, 0, bounds.Width, 96);

                using (LinearGradientBrush brush =
                    new LinearGradientBrush(
                        new Rectangle(
                            top.X,
                            top.Y - 1,
                            top.Width,
                            top.Height + 1),
                        Color.FromArgb(140, 0, 0, 0),
                        Color.FromArgb(0, 0, 0, 0),
                        LinearGradientMode.Vertical))
                {
                    g.FillRectangle(brush, top);
                }
            }

            // Fade the hero into the page so the title below it stays
            // readable whatever the artwork is.
            Rectangle fade =
                new Rectangle(
                    0,
                    bounds.Bottom - 160,
                    bounds.Width,
                    160);

            using (LinearGradientBrush brush =
                new LinearGradientBrush(
                    fade,
                    Color.FromArgb(0, ProfileStyle.CardColor),
                    ProfileStyle.CardColor,
                    LinearGradientMode.Vertical))
            {
                g.FillRectangle(brush, fade);
            }
        }

        private void PaintPoster(Graphics g)
        {
            if (poster == null)
                return;

            Rectangle bounds =
                new Rectangle(
                    Pad,
                    PosterTop,
                    PosterWidth,
                    PosterHeight);

            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = Rounded(bounds, 10))
            {
                Region clip = g.Clip;

                g.SetClip(path, CombineMode.Replace);

                double scale = Math.Max(
                    (double)bounds.Width / poster.Width,
                    (double)bounds.Height / poster.Height);

                int drawWidth =
                    (int)Math.Ceiling(poster.Width * scale);

                int drawHeight =
                    (int)Math.Ceiling(poster.Height * scale);

                g.InterpolationMode =
                    InterpolationMode.HighQualityBicubic;

                g.DrawImage(
                    poster,
                    new Rectangle(
                        bounds.X + (bounds.Width - drawWidth) / 2,
                        bounds.Y + (bounds.Height - drawHeight) / 2,
                        drawWidth,
                        drawHeight));

                g.Clip = clip;

                using (Pen pen = new Pen(
                    Color.FromArgb(90, 0, 0, 0)))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        private void PaintTitle(Graphics g)
        {
            if (game == null)
                return;

            int column = Pad + PosterWidth + 28;

            int width =
                Math.Max(120, ClientSize.Width - column - Pad);

            g.TextRenderingHint =
                System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (Font font = ProfileStyle.Font(22F, FontStyle.Bold))
            using (SolidBrush brush =
                new SolidBrush(ProfileStyle.TextColor))
            using (StringFormat format = new StringFormat())
            {
                format.Trimming = StringTrimming.EllipsisCharacter;

                format.FormatFlags = StringFormatFlags.NoWrap;

                g.DrawString(
                    game.Name,
                    font,
                    brush,
                    new RectangleF(
                        column,
                        PosterBottom - 92,
                        width,
                        40),
                    format);
            }

            // Launcher, group and install state on one subtitle line.
            List<string> parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(game.Launcher))
                parts.Add(game.Launcher);

            LibraryGroup group =
                LibraryOrganizationService.GetGroupFor(game);

            if (group != null)
                parts.Add(group.Name);

            parts.Add(
                game.IsInstalled ? "Installed" : "Not installed");

            using (Font font = ProfileStyle.Font(10F))
            using (SolidBrush brush =
                new SolidBrush(ProfileStyle.MutedTextColor))
            {
                g.DrawString(
                    string.Join("   •   ", parts),
                    font,
                    brush,
                    column,
                    PosterBottom - 46);
            }
        }

        private static GraphicsPath Rounded(
            Rectangle bounds,
            int radius)
        {
            GraphicsPath path = new GraphicsPath();

            int d = radius * 2;

            if (d <= 0 || bounds.Width <= d || bounds.Height <= d)
            {
                path.AddRectangle(bounds);

                return path;
            }

            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);

            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);

            path.AddArc(
                bounds.Right - d,
                bounds.Bottom - d,
                d,
                d,
                0,
                90);

            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);

            path.CloseFigure();

            return path;
        }
    }
}
