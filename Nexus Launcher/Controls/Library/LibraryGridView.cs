using DevExpress.XtraEditors;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Models;
using Nexus_Launcher.Services.Library;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls.Library
{
    /// <summary>
    /// The Full Library poster grid.
    ///
    /// Drawn rather than built out of child controls: a library of a
    /// few hundred games would otherwise mean a few hundred live
    /// windows, which is slow to lay out and slower to scroll. Only the
    /// tiles inside the viewport are painted, and their artwork comes
    /// from <see cref="LibraryThumbnailCache"/> already scaled to tile
    /// size.
    /// </summary>
    internal class LibraryGridView : XtraUserControl
    {
        //--------------------------------------------------------------
        // Layout model
        //--------------------------------------------------------------

        private class Cell
        {
            public LibraryEntry Entry;

            /// <summary>
            /// Position in content coordinates, before scrolling.
            /// </summary>
            public Rectangle Bounds;
        }

        private class Header
        {
            public LibrarySection Section;

            public Rectangle Bounds;
        }

        private readonly List<Cell> cells =
            new List<Cell>();

        private readonly List<Header> headers =
            new List<Header>();

        private List<LibrarySection> sections =
            new List<LibrarySection>();

        /// <summary>
        /// The DevExpress scroll bar rather than the WinForms one, so
        /// it follows the active skin like the rest of the app.
        /// </summary>
        private readonly DevExpress.XtraEditors.VScrollBar scrollBar =
            new DevExpress.XtraEditors.VScrollBar();

        private int contentHeight;

        private int hoverIndex = -1;

        private int focusIndex = -1;

        //--------------------------------------------------------------
        // Appearance
        //--------------------------------------------------------------

        /// <summary>
        /// Poster aspect. Grid artwork from every store this launcher
        /// scans is portrait 2:3.
        /// </summary>
        private const double PosterAspect = 1.5;

        private const int Gap = 18;

        private const int OuterPad = 24;

        private const int CaptionHeight = 38;

        private const int HeaderHeight = 44;

        private int tileWidth = 172;

        /// <summary>
        /// Poster width in pixels. The grid reflows and the thumbnail
        /// cache re-decodes at the new size.
        /// </summary>
        public int TileWidth
        {
            get
            {
                return tileWidth;
            }
            set
            {
                int clamped =
                    Math.Max(110, Math.Min(320, value));

                if (clamped == tileWidth)
                    return;

                tileWidth = clamped;

                // Every prepared thumbnail key was built for the old
                // size, so the host has to rebuild the entries.
                Action handler = TileSizeChanged;

                if (handler != null)
                    handler();

                Relayout();

                Invalidate();
            }
        }

        private int PosterHeight
        {
            get
            {
                return (int)Math.Round(tileWidth * PosterAspect);
            }
        }

        private int TileHeight
        {
            get
            {
                return PosterHeight + CaptionHeight;
            }
        }

        private Size ThumbSize
        {
            get
            {
                return new Size(tileWidth, PosterHeight);
            }
        }

        //--------------------------------------------------------------
        // Events
        //--------------------------------------------------------------

        /// <summary>
        /// A game was clicked or activated with the keyboard. The host
        /// swaps in the detail view.
        /// </summary>
        public event Action<GameInfo> GameActivated;

        /// <summary>
        /// The star overlay was clicked, or Ctrl+D pressed.
        /// </summary>
        public event Action<GameInfo> FavoriteToggled;

        /// <summary>
        /// A game was right clicked. The host puts up the menu so the
        /// grid does not need to know about launching or artwork.
        /// </summary>
        public event Action<GameInfo, Point> GameContextMenu;

        /// <summary>
        /// The poster size changed, so the entries need rebuilding with
        /// thumbnail keys for the new size.
        /// </summary>
        public event Action TileSizeChanged;

        public LibraryGridView()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw,
                true);

            // Needed for arrow keys and Enter to reach OnKeyDown.
            SetStyle(ControlStyles.Selectable, true);

            TabStop = true;

            scrollBar.Dock = DockStyle.Right;
            scrollBar.SmallChange = 48;
            scrollBar.ValueChanged += (s, e) => Invalidate();

            Controls.Add(scrollBar);

            LibraryThumbnailCache.ThumbnailReady += Thumbnail_Ready;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                LibraryThumbnailCache.ThumbnailReady -= Thumbnail_Ready;

            base.Dispose(disposing);
        }

        /// <summary>
        /// Raised from a decode worker, so the repaint is marshalled.
        /// </summary>
        private void Thumbnail_Ready(string cacheKey)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke(new Action(Invalidate));
            }
            catch (Exception)
            {
                // The control went away between the check and the call.
            }
        }

        //--------------------------------------------------------------
        // Data
        //--------------------------------------------------------------

        /// <summary>
        /// Replaces everything on show. Scroll position is kept so
        /// toggling a favourite does not jump the view back to the top.
        /// </summary>
        public void SetSections(
            List<LibrarySection> value)
        {
            sections =
                value ?? new List<LibrarySection>();

            hoverIndex = -1;

            if (focusIndex >= TotalCount)
                focusIndex = -1;

            Relayout();

            Invalidate();
        }

        public int TotalCount
        {
            get
            {
                return sections.Sum(x => x.Entries.Count);
            }
        }

        public GameInfo FocusedGame
        {
            get
            {
                return focusIndex >= 0 && focusIndex < cells.Count
                    ? cells[focusIndex].Entry.Game
                    : null;
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

        /// <summary>
        /// Rebuilds every cell rectangle. Cheap enough to run on each
        /// resize: it is arithmetic per game with no measuring.
        /// </summary>
        private void Relayout()
        {
            cells.Clear();
            headers.Clear();

            // Assume the scroll bar is there. Laying out without it and
            // then discovering the content overflows would need a
            // second pass at a narrower width.
            int available =
                Math.Max(1, ClientSize.Width - scrollBar.Width);

            int usable =
                Math.Max(tileWidth, available - OuterPad * 2);

            int columns =
                Math.Max(1, (usable + Gap) / (tileWidth + Gap));

            // Centre the block so a part-filled last row does not leave
            // the grid looking left-weighted in a wide window.
            int blockWidth =
                columns * tileWidth + (columns - 1) * Gap;

            int left =
                Math.Max(OuterPad, (available - blockWidth) / 2);

            int y = OuterPad;

            foreach (LibrarySection section in sections)
            {
                if (section.Entries.Count == 0)
                    continue;

                if (!string.IsNullOrEmpty(section.Title))
                {
                    Header header = new Header();

                    header.Section = section;

                    header.Bounds = new Rectangle(
                        left,
                        y,
                        blockWidth,
                        HeaderHeight);

                    headers.Add(header);

                    y += HeaderHeight;
                }

                for (int i = 0; i < section.Entries.Count; i++)
                {
                    int column = i % columns;
                    int row = i / columns;

                    Cell cell = new Cell();

                    cell.Entry = section.Entries[i];

                    cell.Bounds = new Rectangle(
                        left + column * (tileWidth + Gap),
                        y + row * (TileHeight + Gap),
                        tileWidth,
                        TileHeight);

                    cells.Add(cell);
                }

                int rows =
                    (section.Entries.Count + columns - 1) / columns;

                y += rows * TileHeight + (rows - 1) * Gap + Gap * 2;
            }

            contentHeight =
                cells.Count == 0
                    ? 0
                    : y - Gap * 2 + OuterPad;

            UpdateScrollBar();
        }

        private void UpdateScrollBar()
        {
            int viewport =
                Math.Max(1, ClientSize.Height);

            bool needed =
                contentHeight > viewport;

            scrollBar.Visible = needed;

            if (!needed)
            {
                scrollBar.Value = 0;

                return;
            }

            scrollBar.Minimum = 0;

            // A scroll bar's thumb covers LargeChange units, so the
            // maximum has to be overshot by that much for the last row
            // to be reachable.
            scrollBar.LargeChange = viewport;

            scrollBar.Maximum =
                Math.Max(0, contentHeight - 1);

            if (scrollBar.Value > contentHeight - viewport)
                scrollBar.Value = Math.Max(0, contentHeight - viewport);
        }

        private int ScrollOffset
        {
            get
            {
                return scrollBar.Visible ? scrollBar.Value : 0;
            }
        }

        private Rectangle ToScreen(
            Rectangle contentBounds)
        {
            Rectangle r = contentBounds;

            r.Y -= ScrollOffset;

            return r;
        }

        //--------------------------------------------------------------
        // Painting
        //--------------------------------------------------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            Color back =
                ProfileStyle.IsDarkTheme
                    ? ProfileStyle.Shift(ProfileStyle.CardColor, -8)
                    : ProfileStyle.Shift(ProfileStyle.CardColor, -4);

            using (SolidBrush brush = new SolidBrush(back))
                g.FillRectangle(brush, ClientRectangle);

            if (cells.Count == 0)
            {
                PaintEmpty(g);

                return;
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle viewport =
                new Rectangle(
                    0,
                    ScrollOffset,
                    ClientSize.Width,
                    ClientSize.Height);

            foreach (Header header in headers)
            {
                if (header.Bounds.IntersectsWith(viewport))
                    PaintHeader(g, header);
            }

            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].Bounds.IntersectsWith(viewport))
                    PaintCell(g, cells[i], i);
            }
        }

        private void PaintEmpty(Graphics g)
        {
            string message =
                sections.Count == 0
                    ? "No games in your library yet."
                    : "No games match these filters.";

            using (Font font = ProfileStyle.Font(12F, FontStyle.Regular))
            using (SolidBrush brush =
                new SolidBrush(ProfileStyle.MutedTextColor))
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;

                g.DrawString(
                    message,
                    font,
                    brush,
                    ClientRectangle,
                    format);
            }
        }

        private void PaintHeader(
            Graphics g,
            Header header)
        {
            Rectangle bounds =
                ToScreen(header.Bounds);

            using (Font font = ProfileStyle.Font(12F, FontStyle.Bold))
            using (SolidBrush brush =
                new SolidBrush(ProfileStyle.TextColor))
            {
                SizeF size =
                    g.MeasureString(header.Section.Title, font);

                g.DrawString(
                    header.Section.Title,
                    font,
                    brush,
                    bounds.X,
                    bounds.Y + (bounds.Height - size.Height) / 2);

                // A rule from the end of the title to the right edge
                // ties the header to the row of posters under it.
                int lineX =
                    bounds.X + (int)size.Width + 12;

                int lineY =
                    bounds.Y + bounds.Height / 2;

                string count =
                    header.Section.Entries.Count.ToString();

                using (Font small =
                    ProfileStyle.Font(9F, FontStyle.Regular))
                using (SolidBrush muted =
                    new SolidBrush(ProfileStyle.MutedTextColor))
                {
                    SizeF countSize =
                        g.MeasureString(count, small);

                    g.DrawString(
                        count,
                        small,
                        muted,
                        lineX,
                        bounds.Y + (bounds.Height - countSize.Height) / 2);

                    lineX += (int)countSize.Width + 12;
                }

                if (lineX < bounds.Right)
                {
                    using (Pen pen = new Pen(
                        ProfileStyle.Blend(
                            ProfileStyle.MutedTextColor,
                            ProfileStyle.CardColor,
                            0.65)))
                    {
                        g.DrawLine(
                            pen,
                            lineX,
                            lineY,
                            bounds.Right,
                            lineY);
                    }
                }
            }
        }

        private void PaintCell(
            Graphics g,
            Cell cell,
            int index)
        {
            Rectangle bounds =
                ToScreen(cell.Bounds);

            Rectangle poster =
                new Rectangle(
                    bounds.X,
                    bounds.Y,
                    bounds.Width,
                    PosterHeight);

            bool hovered = index == hoverIndex;
            bool focused = index == focusIndex;

            // Hover lifts the poster slightly. The caption stays put so
            // the text does not jitter under the pointer.
            if (hovered)
                poster.Y -= 4;

            LibraryEntry entry = cell.Entry;

            // Prepared when the entry was built, so painting does
            // not touch the disk.
            string cacheKey =
                entry.ThumbnailKey ??
                    LibraryThumbnailCache.GetKey(
                        entry.Key,
                        entry.GridImagePath,
                        ThumbSize);

            Image thumb =
                LibraryThumbnailCache.Get(cacheKey);

            if (thumb == null)
            {
                LibraryThumbnailCache.Request(
                    cacheKey,
                    entry.GridImagePath,
                    entry.Name,
                    ThumbSize);
            }

            using (GraphicsPath path = RoundedRect(poster, 8))
            {
                // Shadow under the hovered poster, drawn as a couple of
                // translucent outlines rather than a blur.
                if (hovered)
                {
                    for (int i = 3; i >= 1; i--)
                    {
                        Rectangle glow =
                            Rectangle.Inflate(poster, i, i);

                        using (GraphicsPath glowPath =
                            RoundedRect(glow, 8 + i))
                        using (Pen pen = new Pen(
                            Color.FromArgb(28, 0, 0, 0),
                            1))
                        {
                            g.DrawPath(pen, glowPath);
                        }
                    }
                }

                Region clip = g.Clip;

                g.SetClip(path, CombineMode.Replace);

                if (thumb != null)
                {
                    g.DrawImage(thumb, poster);
                }
                else
                {
                    // Placeholder while the decode runs, so the grid
                    // never shows holes during a fast scroll.
                    using (SolidBrush brush = new SolidBrush(
                        ProfileStyle.Shift(ProfileStyle.CardColor, 6)))
                    {
                        g.FillRectangle(brush, poster);
                    }
                }

                // Games that are not installed read as unavailable at a
                // glance instead of looking identical to installed ones.
                if (!entry.IsInstalled)
                {
                    using (SolidBrush veil =
                        new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                    {
                        g.FillRectangle(veil, poster);
                    }
                }

                g.Clip = clip;

                using (Pen pen = new Pen(
                    focused
                        ? ProfileStyle.AccentColor
                        : Color.FromArgb(
                            hovered ? 140 : 70,
                            0,
                            0,
                            0),
                    focused ? 2F : 1F))
                {
                    g.DrawPath(pen, path);
                }
            }

            PaintOverlays(g, poster, entry);

            PaintCaption(
                g,
                new Rectangle(
                    bounds.X,
                    bounds.Y + PosterHeight + 6,
                    bounds.Width,
                    CaptionHeight - 6),
                entry,
                hovered);
        }

        /// <summary>
        /// The favourite star and the launcher badge, drawn over the
        /// poster.
        /// </summary>
        private void PaintOverlays(
            Graphics g,
            Rectangle poster,
            LibraryEntry entry)
        {
            if (entry.IsFavorite)
            {
                Rectangle star =
                    FavoriteBounds(poster);

                using (SolidBrush shade =
                    new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
                {
                    g.FillEllipse(shade, star);
                }

                DrawStar(
                    g,
                    RectangleF.Inflate(star, -5, -5),
                    Color.FromArgb(255, 214, 92));
            }

            if (string.IsNullOrEmpty(entry.Launcher))
                return;

            using (Font font = ProfileStyle.Font(7.5F, FontStyle.Regular))
            {
                SizeF size =
                    g.MeasureString(entry.Launcher, font);

                // Top left, opposite the star. The bottom of a
                // poster is where cover art puts its own title, and
                // where the generated placeholder puts its caption.
                Rectangle badge =
                    new Rectangle(
                        poster.X + 8,
                        poster.Y + 8,
                        (int)size.Width + 12,
                        (int)size.Height + 6);

                using (GraphicsPath path = RoundedRect(badge, 4))
                using (SolidBrush brush =
                    new SolidBrush(Color.FromArgb(185, 12, 12, 14)))
                {
                    g.FillPath(brush, path);
                }

                using (SolidBrush brush =
                    new SolidBrush(Color.FromArgb(235, 235, 235)))
                {
                    g.DrawString(
                        entry.Launcher,
                        font,
                        brush,
                        badge.X + 6,
                        badge.Y + 3);
                }
            }
        }

        private void PaintCaption(
            Graphics g,
            Rectangle bounds,
            LibraryEntry entry,
            bool hovered)
        {
            using (Font font = ProfileStyle.Font(9F, FontStyle.Regular))
            using (SolidBrush brush = new SolidBrush(
                hovered
                    ? ProfileStyle.TextColor
                    : ProfileStyle.Blend(
                        ProfileStyle.TextColor,
                        ProfileStyle.MutedTextColor,
                        0.35)))
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Near;
                format.Trimming = StringTrimming.EllipsisCharacter;
                format.FormatFlags = StringFormatFlags.NoWrap;

                g.DrawString(
                    entry.Name,
                    font,
                    brush,
                    bounds,
                    format);
            }
        }

        private Rectangle FavoriteBounds(
            Rectangle poster)
        {
            return new Rectangle(
                poster.Right - 34,
                poster.Y + 8,
                26,
                26);
        }

        private static void DrawStar(
            Graphics g,
            RectangleF bounds,
            Color color)
        {
            PointF centre =
                new PointF(
                    bounds.X + bounds.Width / 2F,
                    bounds.Y + bounds.Height / 2F);

            float outer =
                Math.Min(bounds.Width, bounds.Height) / 2F;

            float inner = outer * 0.45F;

            PointF[] points = new PointF[10];

            for (int i = 0; i < 10; i++)
            {
                double angle =
                    -Math.PI / 2 + i * Math.PI / 5;

                float radius =
                    (i % 2 == 0) ? outer : inner;

                points[i] = new PointF(
                    centre.X + (float)(Math.Cos(angle) * radius),
                    centre.Y + (float)(Math.Sin(angle) * radius));
            }

            using (SolidBrush brush = new SolidBrush(color))
                g.FillPolygon(brush, points);
        }

        private static GraphicsPath RoundedRect(
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

        //--------------------------------------------------------------
        // Interaction
        //--------------------------------------------------------------

        private int HitTest(Point location)
        {
            Point content =
                new Point(location.X, location.Y + ScrollOffset);

            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].Bounds.Contains(content))
                    return i;
            }

            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int index = HitTest(e.Location);

            if (index == hoverIndex)
                return;

            hoverIndex = index;

            Cursor =
                index >= 0
                    ? Cursors.Hand
                    : Cursors.Default;

            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (hoverIndex < 0)
                return;

            hoverIndex = -1;

            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            Focus();

            int index = HitTest(e.Location);

            if (index < 0)
                return;

            focusIndex = index;

            Cell cell = cells[index];

            if (e.Button == MouseButtons.Right)
            {
                Action<GameInfo, Point> menu = GameContextMenu;

                if (menu != null)
                    menu(cell.Entry.Game, e.Location);

                Invalidate();

                return;
            }

            if (e.Button != MouseButtons.Left)
                return;

            // The star is a control of its own inside the tile, so a
            // click on it favourites rather than opening the game.
            Rectangle poster =
                new Rectangle(
                    cell.Bounds.X,
                    cell.Bounds.Y - ScrollOffset -
                        (index == hoverIndex ? 4 : 0),
                    cell.Bounds.Width,
                    PosterHeight);

            if (FavoriteBounds(poster).Contains(e.Location))
            {
                Action<GameInfo> favorite = FavoriteToggled;

                if (favorite != null)
                    favorite(cell.Entry.Game);

                return;
            }

            Action<GameInfo> activated = GameActivated;

            if (activated != null)
                activated(cell.Entry.Game);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (!scrollBar.Visible)
                return;

            int delta =
                -(e.Delta / 120) * scrollBar.SmallChange * 2;

            ScrollBy(delta);
        }

        private void ScrollBy(int delta)
        {
            if (!scrollBar.Visible)
                return;

            int max =
                Math.Max(0, contentHeight - ClientSize.Height);

            scrollBar.Value =
                Math.Max(0, Math.Min(max, scrollBar.Value + delta));
        }

        //--------------------------------------------------------------
        // Keyboard
        //--------------------------------------------------------------

        /// <summary>
        /// Arrow keys would otherwise be swallowed as navigation
        /// between sibling controls.
        /// </summary>
        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                case Keys.Home:
                case Keys.End:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Enter:
                    return true;
            }

            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (cells.Count == 0)
                return;

            int columns = ColumnsPerRow();

            int next = focusIndex;

            switch (e.KeyCode)
            {
                case Keys.Left:
                    next = Math.Max(0, focusIndex - 1);
                    break;

                case Keys.Right:
                    next = Math.Min(cells.Count - 1, focusIndex + 1);
                    break;

                case Keys.Up:
                    next = Math.Max(0, focusIndex - columns);
                    break;

                case Keys.Down:
                    next = Math.Min(
                        cells.Count - 1,
                        focusIndex + columns);
                    break;

                case Keys.Home:
                    next = 0;
                    break;

                case Keys.End:
                    next = cells.Count - 1;
                    break;

                case Keys.PageUp:
                    ScrollBy(-ClientSize.Height);
                    return;

                case Keys.PageDown:
                    ScrollBy(ClientSize.Height);
                    return;

                case Keys.Enter:
                    if (FocusedGame != null)
                    {
                        Action<GameInfo> activated = GameActivated;

                        if (activated != null)
                            activated(FocusedGame);
                    }
                    return;

                case Keys.D:
                    if (e.Control && FocusedGame != null)
                    {
                        Action<GameInfo> favorite = FavoriteToggled;

                        if (favorite != null)
                            favorite(FocusedGame);
                    }
                    return;

                default:
                    return;
            }

            if (next < 0)
                next = 0;

            if (next == focusIndex)
                return;

            focusIndex = next;

            EnsureVisible(focusIndex);

            Invalidate();
        }

        private int ColumnsPerRow()
        {
            if (cells.Count < 2)
                return 1;

            // Read back off the layout rather than recomputing it, so
            // the two can never disagree.
            int columns = 1;

            for (int i = 1; i < cells.Count; i++)
            {
                if (cells[i].Bounds.Y != cells[0].Bounds.Y)
                    break;

                columns++;
            }

            return columns;
        }

        private void EnsureVisible(int index)
        {
            if (index < 0 || index >= cells.Count || !scrollBar.Visible)
                return;

            Rectangle bounds = cells[index].Bounds;

            int top = ScrollOffset;

            int bottom = top + ClientSize.Height;

            if (bounds.Y - OuterPad < top)
                ScrollBy(bounds.Y - OuterPad - top);
            else if (bounds.Bottom + OuterPad > bottom)
                ScrollBy(bounds.Bottom + OuterPad - bottom);
        }

        /// <summary>
        /// Scrolls a game into view and gives it the focus ring, for
        /// returning from the detail view to where you were.
        /// </summary>
        public void FocusGame(GameInfo game)
        {
            if (game == null)
                return;

            for (int i = 0; i < cells.Count; i++)
            {
                if (ReferenceEquals(cells[i].Entry.Game, game))
                {
                    focusIndex = i;

                    EnsureVisible(i);

                    Invalidate();

                    return;
                }
            }
        }
    }
}
