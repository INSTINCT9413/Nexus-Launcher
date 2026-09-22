using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Properties;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Nexus_Launcher.Helpers
{
    /// <summary>
    /// Stand in artwork for games that have none: a tinted card with a
    /// gamepad and the game's name.
    ///
    /// Replaces the old NA.png, whose "Could Not Load Image Data / Not
    /// Supported" text read as an error when it was only ever meant to
    /// say there was no artwork. Drawn at runtime so it can carry the
    /// game's name and suit the current theme, neither of which a fixed
    /// image can do.
    /// </summary>
    internal static class ArtworkPlaceholder
    {
        /// <summary>
        /// Rendered at twice the display size so it stays crisp when
        /// the picture edit scales it down.
        /// </summary>
        private const int Supersample = 2;

        /// <summary>
        /// A placeholder sized for a picture box. Each game gets its own
        /// colour from its name, so the same game always looks the same
        /// and neighbouring games are easy to tell apart.
        /// </summary>
        public static Image Create(
            string title,
            Size displaySize)
        {
            int width =
                Math.Max(40, displaySize.Width) * Supersample;

            int height =
                Math.Max(40, displaySize.Height) * Supersample;

            Bitmap bitmap =
                new Bitmap(width, height);

            try
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    Draw(g, new Rectangle(0, 0, width, height), title);
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            return bitmap;
        }

        private static void Draw(
            Graphics g,
            Rectangle area,
            string title)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            bool dark =
                ProfileStyle.IsDarkTheme;

            // Muted on purpose: it should read as "nothing here yet",
            // not compete with real artwork beside it.
            double hue =
                StableHue(title);

            Color top =
                FromHsl(hue, 0.38, dark ? 0.30 : 0.80);

            Color bottom =
                FromHsl(hue, 0.42, dark ? 0.18 : 0.68);

            using (LinearGradientBrush background =
                new LinearGradientBrush(
                    area,
                    top,
                    bottom,
                    LinearGradientMode.Vertical))
            {
                g.FillRectangle(background, area);
            }

            // Text colour comes from the card itself, not the skin, so
            // it always contrasts with whatever tint the name produced.
            Color ink =
                ProfileStyle.Luminance(bottom) < 140
                    ? Color.FromArgb(240, 245, 250)
                    : Color.FromArgb(30, 34, 42);

            float unit =
                area.Width / 10f;

            RectangleF pad =
                new RectangleF(
                    area.X + unit * 2f,
                    area.Y + area.Height * 0.15f,
                    unit * 6f,
                    unit * 6f * 0.62f);

            // The cut-outs take the gradient's colour at the pad's own
            // height. Using the bottom colour made them read as tinted
            // shapes on light themes rather than holes.
            float padCentre =
                (pad.Top + pad.Height * 0.34f - area.Top) / area.Height;

            DrawGamepad(
                g,
                pad,
                Color.FromArgb(dark ? 70 : 60, ink),
                ProfileStyle.Blend(top, bottom, padCentre));

            // The title gets everything between the pad and a band kept
            // clear at the bottom for the caption, so long names can no
            // longer run into it.
            float captionBand =
                unit * 1.45f;

            RectangleF titleArea =
                new RectangleF(
                    area.X + unit * 0.6f,
                    pad.Bottom + area.Height * 0.05f,
                    area.Width - unit * 1.2f,
                    0);

            titleArea.Height =
                area.Bottom - captionBand - titleArea.Top;

            string family =
                string.IsNullOrWhiteSpace(Settings.Default.UIFont)
                    ? "Segoe UI"
                    : Settings.Default.UIFont;

            string text =
                string.IsNullOrWhiteSpace(title) ? "Unknown game" : title.Trim();

            using (Font titleFont = FitFont(g, text, family, titleArea, unit))
            using (Font captionFont = SafeFont(family, unit * 0.62f, FontStyle.Regular))
            using (SolidBrush titleBrush = new SolidBrush(ink))
            using (SolidBrush captionBrush = new SolidBrush(Color.FromArgb(150, ink)))
            using (StringFormat centred = new StringFormat())
            {
                centred.Alignment = StringAlignment.Center;
                centred.LineAlignment = StringAlignment.Near;
                centred.Trimming = StringTrimming.EllipsisWord;
                centred.FormatFlags = StringFormatFlags.LineLimit;

                g.DrawString(
                    text,
                    titleFont,
                    titleBrush,
                    titleArea,
                    centred);

                centred.LineAlignment = StringAlignment.Far;

                g.DrawString(
                    "No artwork",
                    captionFont,
                    captionBrush,
                    new RectangleF(
                        area.X,
                        area.Y,
                        area.Width,
                        area.Height - unit * 0.55f),
                    centred);
            }
        }

        /// <summary>
        /// A simple controller silhouette: a rounded body, two grips, a
        /// d-pad and four face buttons, the last two cut out in the
        /// card colour.
        /// </summary>
        private static void DrawGamepad(
            Graphics g,
            RectangleF r,
            Color fill,
            Color hole)
        {
            float w = r.Width;
            float h = r.Height;

            using (GraphicsPath body = new GraphicsPath())
            {
                // Winding, or the overlap between the bar and the grips
                // would be treated as a hole and left unfilled.
                body.FillMode = FillMode.Winding;

                AddRounded(
                    body,
                    new RectangleF(r.X, r.Y, w, h * 0.66f),
                    h * 0.33f);

                float gripWidth = w * 0.34f;
                float gripHeight = h * 0.80f;

                body.AddEllipse(r.X, r.Bottom - gripHeight, gripWidth, gripHeight);
                body.AddEllipse(r.Right - gripWidth, r.Bottom - gripHeight, gripWidth, gripHeight);

                using (SolidBrush brush = new SolidBrush(fill))
                {
                    g.FillPath(brush, body);
                }
            }

            using (SolidBrush brush = new SolidBrush(hole))
            {
                float padX = r.X + w * 0.25f;
                float padY = r.Y + h * 0.34f;
                float arm = h * 0.11f;
                float span = h * 0.36f;

                g.FillRectangle(brush, padX - span / 2, padY - arm / 2, span, arm);
                g.FillRectangle(brush, padX - arm / 2, padY - span / 2, arm, span);

                float buttonsX = r.X + w * 0.75f;
                float size = h * 0.12f;
                float offset = h * 0.13f;

                FillDot(g, brush, buttonsX, padY - offset, size);
                FillDot(g, brush, buttonsX, padY + offset, size);
                FillDot(g, brush, buttonsX - offset, padY, size);
                FillDot(g, brush, buttonsX + offset, padY, size);
            }
        }

        private static void FillDot(
            Graphics g,
            Brush brush,
            float centreX,
            float centreY,
            float size)
        {
            g.FillEllipse(brush, centreX - size / 2, centreY - size / 2, size, size);
        }

        private static void AddRounded(
            GraphicsPath path,
            RectangleF r,
            float radius)
        {
            float d =
                Math.Min(radius * 2, Math.Min(r.Width, r.Height));

            path.StartFigure();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
        }

        /// <summary>
        /// The largest title font, within reason, that fits the whole
        /// name in its box. Short names stay big; long ones step down
        /// rather than being cut off, and only the very longest fall
        /// back to an ellipsis at the smallest size.
        /// </summary>
        private static Font FitFont(
            Graphics g,
            string text,
            string family,
            RectangleF box,
            float unit)
        {
            float largest = unit * 0.95f;
            float smallest = unit * 0.62f;

            for (float size = largest; size > smallest; size -= unit * 0.05f)
            {
                Font candidate =
                    SafeFont(family, size, FontStyle.Bold);

                SizeF needed =
                    g.MeasureString(text, candidate, (int)box.Width);

                if (needed.Height <= box.Height)
                    return candidate;

                candidate.Dispose();
            }

            return SafeFont(family, smallest, FontStyle.Bold);
        }

        private static Font SafeFont(
            string family,
            float pixels,
            FontStyle style)
        {
            try
            {
                return new Font(family, pixels, style, GraphicsUnit.Pixel);
            }
            catch
            {
                return new Font("Segoe UI", pixels, style, GraphicsUnit.Pixel);
            }
        }

        /// <summary>
        /// A hue from 0 to 360 that is always the same for a given name.
        ///
        /// string.GetHashCode is not used because it is allowed to
        /// differ between runs and between 32 and 64 bit, which would
        /// make a game change colour. This is FNV-1a.
        /// </summary>
        private static double StableHue(
            string text)
        {
            unchecked
            {
                uint hash = 2166136261;

                foreach (char c in (text ?? string.Empty).ToLowerInvariant())
                {
                    hash ^= c;
                    hash *= 16777619;
                }

                return hash % 360;
            }
        }

        private static Color FromHsl(
            double hue,
            double saturation,
            double lightness)
        {
            double c = (1 - Math.Abs(2 * lightness - 1)) * saturation;
            double x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
            double m = lightness - c / 2;

            double r, g, b;

            if (hue < 60) { r = c; g = x; b = 0; }
            else if (hue < 120) { r = x; g = c; b = 0; }
            else if (hue < 180) { r = 0; g = c; b = x; }
            else if (hue < 240) { r = 0; g = x; b = c; }
            else if (hue < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return Color.FromArgb(
                (int)Math.Round((r + m) * 255),
                (int)Math.Round((g + m) * 255),
                (int)Math.Round((b + m) * 255));
        }
    }
}
