using DevExpress.XtraEditors;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Services.Achievements;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls.Notifications
{
    /// <summary>
    /// One unlock announcing itself in the bottom right, in the style of
    /// a console achievement pop: slides up, holds, slides away.
    ///
    /// Drawn into a layered window rather than an ordinary form. A
    /// normal form cannot have soft rounded corners or a drop shadow
    /// without a jagged Region, and fading one in and out means
    /// per-pixel alpha, which is exactly what UpdateLayeredWindow
    /// composites for us.
    ///
    /// The window never takes focus: it can appear while a game is
    /// launching, and stealing the foreground then would be hostile.
    /// </summary>
    internal class AchievementToastForm : Form
    {
        //--------------------------------------------------------------
        // Layered window plumbing
        //--------------------------------------------------------------

        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOPMOST = 0x00000008;

        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;
        private const int ULW_ALPHA = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        private struct Point32
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Size32
        {
            public int Width;
            public int Height;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BlendFunction
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UpdateLayeredWindow(
            IntPtr hwnd,
            IntPtr hdcDst,
            ref Point32 pptDst,
            ref Size32 psize,
            IntPtr hdcSrc,
            ref Point32 pptSrc,
            int crKey,
            ref BlendFunction pblend,
            int dwFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(
            IntPtr hdc,
            IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        //--------------------------------------------------------------
        // Geometry
        //--------------------------------------------------------------

        /// <summary>
        /// Room around the panel for the drop shadow. The window is
        /// this much bigger than the card on every side, and more
        /// underneath where the shadow falls.
        /// </summary>
        private const int ShadowMargin = 18;

        private const int PanelWidth = 396;

        private const int PanelHeight = 94;

        private const int Radius = 10;

        private const int MedalSize = 62;

        /// <summary>
        /// How far the card travels while sliding in and out.
        /// </summary>
        private const int TravelDistance = 54;

        /// <summary>
        /// Gap from the bottom right corner of the working area.
        /// </summary>
        private const int ScreenMargin = 16;

        //--------------------------------------------------------------
        // Timing
        //--------------------------------------------------------------

        private enum Phase
        {
            In,
            Hold,
            Out,
            Done
        }

        private const int SlideInMs = 420;

        private const int SlideOutMs = 320;

        /// <summary>
        /// A sweep of light crosses the card as it settles.
        /// </summary>
        private const int ShineMs = 900;

        private readonly int holdMs;

        private Phase phase = Phase.In;

        private readonly Stopwatch clock = new Stopwatch();

        private readonly Timer frameTimer = new Timer();

        private readonly UnlockEvent unlock;

        private Point restPosition;

        /// <summary>
        /// Raised once the card has finished sliding away, so the queue
        /// can bring the next one up.
        /// </summary>
        public event Action Finished;

        public AchievementToastForm(
            UnlockEvent value,
            int displaySeconds)
        {
            unlock = value;

            holdMs =
                Math.Max(2000, Math.Min(15000, displaySeconds * 1000));

            FormBorderStyle = FormBorderStyle.None;

            ShowInTaskbar = false;

            StartPosition = FormStartPosition.Manual;

            TopMost = true;

            Size = new Size(
                PanelWidth + ShadowMargin * 2,
                PanelHeight + ShadowMargin * 2 + 6);

            frameTimer.Interval = 16;

            frameTimer.Tick += Frame;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;

                cp.ExStyle |=
                    WS_EX_LAYERED |
                    WS_EX_TOOLWINDOW |
                    WS_EX_NOACTIVATE |
                    WS_EX_TOPMOST;

                return cp;
            }
        }

        /// <summary>
        /// Keeps the popup from pulling focus off whatever the user is
        /// doing when it appears.
        /// </summary>
        protected override bool ShowWithoutActivation
        {
            get
            {
                return true;
            }
        }

        //--------------------------------------------------------------
        // Showing
        //--------------------------------------------------------------

        /// <summary>
        /// Places the card against the bottom right of the working area
        /// of <paramref name="owner"/>'s screen and starts the slide.
        /// </summary>
        public void Play(
            Form owner)
        {
            Rectangle area =
                (owner != null && !owner.IsDisposed
                    ? Screen.FromControl(owner)
                    : Screen.PrimaryScreen).WorkingArea;

            restPosition =
                new Point(
                    area.Right - Width - ScreenMargin + ShadowMargin,
                    area.Bottom - Height - ScreenMargin + ShadowMargin);

            Location =
                new Point(restPosition.X, restPosition.Y + TravelDistance);

            phase = Phase.In;

            clock.Restart();

            Show();

            Draw();

            frameTimer.Start();
        }

        /// <summary>
        /// Cuts the hold short, for a click or a shutdown.
        /// </summary>
        public void DismissNow()
        {
            if (phase == Phase.Out || phase == Phase.Done)
                return;

            phase = Phase.Out;

            clock.Restart();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            DismissNow();
        }

        private void Frame(
            object sender,
            EventArgs e)
        {
            int elapsed =
                (int)clock.ElapsedMilliseconds;

            switch (phase)
            {
                case Phase.In:
                    if (elapsed >= SlideInMs)
                    {
                        phase = Phase.Hold;

                        clock.Restart();
                    }
                    break;

                case Phase.Hold:
                    if (elapsed >= holdMs)
                    {
                        phase = Phase.Out;

                        clock.Restart();
                    }
                    break;

                case Phase.Out:
                    if (elapsed >= SlideOutMs)
                    {
                        phase = Phase.Done;

                        frameTimer.Stop();

                        Action handler = Finished;

                        if (handler != null)
                            handler();

                        Close();

                        return;
                    }
                    break;
            }

            Draw();
        }

        //--------------------------------------------------------------
        // Animation curves
        //--------------------------------------------------------------

        private static double EaseOut(double t)
        {
            double inv = 1 - t;

            return 1 - inv * inv * inv;
        }

        private static double EaseIn(double t)
        {
            return t * t * t;
        }

        private static double Clamp01(double v)
        {
            return v < 0 ? 0 : (v > 1 ? 1 : v);
        }

        /// <summary>
        /// Where the card is and how solid it is, for the current
        /// moment in the current phase.
        /// </summary>
        private void GetState(
            out int offset,
            out byte alpha,
            out double holdFraction)
        {
            int elapsed =
                (int)clock.ElapsedMilliseconds;

            holdFraction = 0;

            switch (phase)
            {
                case Phase.In:
                    {
                        double t =
                            EaseOut(Clamp01(elapsed / (double)SlideInMs));

                        offset = (int)Math.Round(TravelDistance * (1 - t));

                        alpha = (byte)Math.Round(255 * t);

                        break;
                    }

                case Phase.Out:
                    {
                        double t =
                            EaseIn(Clamp01(elapsed / (double)SlideOutMs));

                        offset = (int)Math.Round(TravelDistance * t);

                        alpha = (byte)Math.Round(255 * (1 - t));

                        holdFraction = 1;

                        break;
                    }

                default:
                    offset = 0;

                    alpha = 255;

                    holdFraction =
                        Clamp01(elapsed / (double)holdMs);

                    break;
            }
        }

        //--------------------------------------------------------------
        // Painting
        //--------------------------------------------------------------

        private void Draw()
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            int offset;
            byte alpha;
            double holdFraction;

            GetState(out offset, out alpha, out holdFraction);

            Location =
                new Point(restPosition.X, restPosition.Y + offset);

            using (Bitmap bitmap =
                new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    g.TextRenderingHint =
                        TextRenderingHint.ClearTypeGridFit;

                    g.InterpolationMode =
                        InterpolationMode.HighQualityBicubic;

                    Render(g, holdFraction);
                }

                Push(bitmap, alpha);
            }
        }

        private void Render(
            Graphics g,
            double holdFraction)
        {
            Rectangle panel =
                new Rectangle(
                    ShadowMargin,
                    ShadowMargin,
                    PanelWidth,
                    PanelHeight);

            DrawShadow(g, panel);

            Color back =
                ProfileStyle.IsDarkTheme
                    ? ProfileStyle.Shift(ProfileStyle.CardColor, 10)
                    : ProfileStyle.CardColor;

            Color accent =
                unlock.IsBadge
                    ? ProfileStyle.TierColor(unlock.Tier)
                    : ProfileStyle.AccentColor;

            using (GraphicsPath path = Rounded(panel, Radius))
            {
                using (LinearGradientBrush brush =
                    new LinearGradientBrush(
                        panel,
                        ProfileStyle.Shift(back, 6),
                        ProfileStyle.Shift(back, -6),
                        LinearGradientMode.Vertical))
                {
                    g.FillPath(brush, path);
                }

                // A coloured edge on the left says at a glance whether
                // this was an achievement or which badge tier it was.
                Region clip = g.Clip;

                g.SetClip(path, CombineMode.Replace);

                using (SolidBrush edge = new SolidBrush(accent))
                {
                    g.FillRectangle(
                        edge,
                        new Rectangle(panel.X, panel.Y, 4, panel.Height));
                }

                DrawShine(g, panel);

                DrawTimeBar(g, panel, accent, holdFraction);

                g.Clip = clip;

                using (Pen pen = new Pen(
                    ProfileStyle.IsDarkTheme
                        ? Color.FromArgb(70, 255, 255, 255)
                        : Color.FromArgb(40, 0, 0, 0)))
                {
                    g.DrawPath(pen, path);
                }
            }

            DrawMedal(g, panel, accent);

            DrawText(g, panel, accent);
        }

        /// <summary>
        /// A soft shadow, built from a few rounded outlines at low
        /// alpha rather than a real blur: cheap enough to redraw every
        /// frame.
        /// </summary>
        private static void DrawShadow(
            Graphics g,
            Rectangle panel)
        {
            for (int i = ShadowMargin; i >= 1; i--)
            {
                Rectangle bounds =
                    new Rectangle(
                        panel.X - i,
                        panel.Y - i + 3,
                        panel.Width + i * 2,
                        panel.Height + i * 2);

                int a =
                    (int)(28.0 * (1.0 - (double)i / ShadowMargin));

                if (a <= 0)
                    continue;

                using (GraphicsPath path = Rounded(bounds, Radius + i))
                using (SolidBrush brush =
                    new SolidBrush(Color.FromArgb(a, 0, 0, 0)))
                {
                    g.FillPath(brush, path);
                }
            }
        }

        /// <summary>
        /// The band of light that crosses the card as it arrives. Only
        /// while sliding in and just after, then it is gone.
        /// </summary>
        private void DrawShine(
            Graphics g,
            Rectangle panel)
        {
            int since =
                phase == Phase.In
                    ? (int)clock.ElapsedMilliseconds
                    : phase == Phase.Hold
                        ? SlideInMs + (int)clock.ElapsedMilliseconds
                        : int.MaxValue;

            if (since > ShineMs)
                return;

            double t = Clamp01(since / (double)ShineMs);

            int bandWidth = 90;

            int x =
                panel.X - bandWidth +
                (int)((panel.Width + bandWidth * 2) * t);

            using (GraphicsPath band = new GraphicsPath())
            {
                // Slanted so it reads as a sweep rather than a wipe.
                band.AddPolygon(new[]
                {
                    new Point(x, panel.Bottom),
                    new Point(x + bandWidth, panel.Bottom),
                    new Point(x + bandWidth + 28, panel.Y),
                    new Point(x + 28, panel.Y)
                });

                using (PathGradientBrush brush =
                    new PathGradientBrush(band))
                {
                    brush.CenterColor =
                        Color.FromArgb(
                            (int)(46 * (1 - t)),
                            255,
                            255,
                            255);

                    brush.SurroundColors =
                        new[] { Color.FromArgb(0, 255, 255, 255) };

                    g.FillPath(brush, band);
                }
            }
        }

        /// <summary>
        /// A hairline along the bottom that drains while the card is
        /// held, so it is obvious it is about to leave.
        /// </summary>
        private static void DrawTimeBar(
            Graphics g,
            Rectangle panel,
            Color accent,
            double fraction)
        {
            int width =
                (int)Math.Round(panel.Width * (1 - Clamp01(fraction)));

            if (width <= 0)
                return;

            using (SolidBrush brush =
                new SolidBrush(Color.FromArgb(150, accent)))
            {
                g.FillRectangle(
                    brush,
                    new Rectangle(
                        panel.X,
                        panel.Bottom - 3,
                        width,
                        3));
            }
        }

        private void DrawMedal(
            Graphics g,
            Rectangle panel,
            Color accent)
        {
            Rectangle disc =
                new Rectangle(
                    panel.X + 18,
                    panel.Y + (panel.Height - MedalSize) / 2,
                    MedalSize,
                    MedalSize);

            using (SolidBrush brush = new SolidBrush(
                ProfileStyle.Blend(
                    accent,
                    ProfileStyle.CardColor,
                    0.55)))
            {
                g.FillEllipse(brush, disc);
            }

            using (Pen pen = new Pen(accent, 3f))
            {
                Rectangle inner = disc;

                inner.Inflate(-2, -2);

                g.DrawEllipse(pen, inner);
            }

            int glyphSize =
                (int)(MedalSize * 0.52);

            Image glyph =
                BadgeMedal.GetGlyph(unlock.IconKey, glyphSize);

            if (glyph == null)
                return;

            g.DrawImage(
                glyph,
                new Rectangle(
                    disc.X + (disc.Width - glyphSize) / 2,
                    disc.Y + (disc.Height - glyphSize) / 2,
                    glyphSize,
                    glyphSize));
        }

        private void DrawText(
            Graphics g,
            Rectangle panel,
            Color accent)
        {
            int left =
                panel.X + 18 + MedalSize + 16;

            int right =
                panel.Right - 16;

            // The XP chip is measured first: the title has to stop
            // short of it rather than run underneath.
            string points =
                "+" + unlock.Points + " XP";

            int chipWidth;

            using (Font chipFont = ProfileStyle.Font(8.5F, FontStyle.Bold))
            {
                chipWidth =
                    (int)Math.Ceiling(
                        g.MeasureString(points, chipFont).Width) + 16;

                Rectangle chip =
                    new Rectangle(
                        right - chipWidth,
                        panel.Y + 15,
                        chipWidth,
                        20);

                using (GraphicsPath path = Rounded(chip, 9))
                using (SolidBrush brush =
                    new SolidBrush(Color.FromArgb(46, accent)))
                {
                    g.FillPath(brush, path);
                }

                using (SolidBrush brush = new SolidBrush(accent))
                using (StringFormat format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;

                    format.LineAlignment = StringAlignment.Center;

                    g.DrawString(points, chipFont, brush, chip, format);
                }
            }

            string caption =
                unlock.IsBadge
                    ? (unlock.Tier == BadgeTier.None
                        ? "BADGE EARNED"
                        : "BADGE EARNED  ·  " +
                            unlock.Tier.ToString().ToUpperInvariant())
                    : "ACHIEVEMENT UNLOCKED";

            using (Font font = ProfileStyle.Font(7.5F, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(accent))
            {
                g.DrawString(caption, font, brush, left, panel.Y + 16);
            }

            int textWidth =
                Math.Max(60, right - chipWidth - 10 - left);

            using (Font font = ProfileStyle.Font(11.5F, FontStyle.Bold))
            using (SolidBrush brush =
                new SolidBrush(ProfileStyle.TextColor))
            {
                g.DrawString(
                    Fit(g, unlock.Title, font, textWidth),
                    font,
                    brush,
                    left,
                    panel.Y + 34);
            }

            using (Font font = ProfileStyle.Font(8.5F))
            using (SolidBrush brush =
                new SolidBrush(ProfileStyle.MutedTextColor))
            {
                g.DrawString(
                    Fit(g, unlock.Description, font, right - left),
                    font,
                    brush,
                    left,
                    panel.Y + 58);
            }
        }

        /// <summary>
        /// Shortens text to fit, ending in an ellipsis.
        ///
        /// Done by hand rather than with StringTrimming: GDI+ widens
        /// the gaps between words on a line it has trimmed, which reads
        /// as a spacing fault rather than as a long title.
        /// </summary>
        private static string Fit(
            Graphics g,
            string text,
            Font font,
            int maxWidth)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 0)
                return string.Empty;

            if (g.MeasureString(text, font).Width <= maxWidth)
                return text;

            const string Ellipsis = "…";

            int low = 0;

            int high = text.Length;

            // Binary search for the longest prefix that still fits.
            while (low < high)
            {
                int mid = (low + high + 1) / 2;

                string candidate =
                    text.Substring(0, mid).TrimEnd() + Ellipsis;

                if (g.MeasureString(candidate, font).Width <= maxWidth)
                    low = mid;
                else
                    high = mid - 1;
            }

            return low <= 0
                ? Ellipsis
                : text.Substring(0, low).TrimEnd() + Ellipsis;
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

        //--------------------------------------------------------------
        // Compositing
        //--------------------------------------------------------------

        /// <summary>
        /// Hands the finished frame to the window manager, which
        /// composites it against whatever is behind using the alpha in
        /// the bitmap plus the overall fade.
        /// </summary>
        private void Push(
            Bitmap bitmap,
            byte alpha)
        {
            IntPtr screenDc = GetDC(IntPtr.Zero);

            IntPtr memDc = CreateCompatibleDC(screenDc);

            IntPtr hBitmap = IntPtr.Zero;

            IntPtr previous = IntPtr.Zero;

            try
            {
                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));

                previous = SelectObject(memDc, hBitmap);

                Size32 size =
                    new Size32 { Width = bitmap.Width, Height = bitmap.Height };

                Point32 source =
                    new Point32 { X = 0, Y = 0 };

                Point32 target =
                    new Point32 { X = Left, Y = Top };

                BlendFunction blend =
                    new BlendFunction
                    {
                        BlendOp = AC_SRC_OVER,
                        BlendFlags = 0,
                        SourceConstantAlpha = alpha,
                        AlphaFormat = AC_SRC_ALPHA
                    };

                UpdateLayeredWindow(
                    Handle,
                    screenDc,
                    ref target,
                    ref size,
                    memDc,
                    ref source,
                    0,
                    ref blend,
                    ULW_ALPHA);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, screenDc);

                if (hBitmap != IntPtr.Zero)
                {
                    SelectObject(memDc, previous);

                    DeleteObject(hBitmap);
                }

                DeleteDC(memDc);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                frameTimer.Stop();

                frameTimer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
