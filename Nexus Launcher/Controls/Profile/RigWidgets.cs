using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls.Profile
{
    /// <summary>
    /// A 270 degree load gauge with the value in the middle.
    ///
    /// Painted rather than built with XtraGauges so it matches the
    /// other profile widgets exactly and recolours with the skin.
    /// The arc turns amber then red as load climbs.
    /// </summary>
    internal class RadialGauge : Control, IProfileThemed
    {
        private const float StartAngle = 135f;
        private const float SweepAngle = 270f;

        private double value;
        private string valueText = "-";
        private string caption = string.Empty;
        private string subText = string.Empty;

        public RadialGauge()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.Transparent;
            Size = new Size(150, 150);
        }

        /// <summary>
        /// 0 to 1.
        /// </summary>
        public double Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = Math.Max(0, Math.Min(1, value));
                Invalidate();
            }
        }

        public string ValueText
        {
            get
            {
                return valueText;
            }
            set
            {
                valueText = value;
                Invalidate();
            }
        }

        public string Caption
        {
            get
            {
                return caption;
            }
            set
            {
                caption = value;
                Invalidate();
            }
        }

        /// <summary>
        /// A small line under the caption, such as "12.1 / 32 GB".
        /// </summary>
        public string SubText
        {
            get
            {
                return subText;
            }
            set
            {
                subText = value;
                Invalidate();
            }
        }

        public void ApplyTheme()
        {
            Invalidate();
        }

        /// <summary>
        /// Accent under normal load, amber when busy, red when pegged.
        /// </summary>
        private Color LoadColor()
        {
            if (value >= 0.9)
                return Color.FromArgb(232, 72, 72);

            if (value >= 0.75)
                return Color.FromArgb(240, 160, 40);

            return ProfileStyle.AccentColor;
        }

        protected override void OnPaint(
            PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint =
                System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int size =
                Math.Min(Width, Height);

            float thickness =
                Math.Max(6f, size / 11f);

            RectangleF arc =
                new RectangleF(
                    (Width - size) / 2f + thickness,
                    (Height - size) / 2f + thickness,
                    size - thickness * 2,
                    size - thickness * 2);

            if (arc.Width <= 0 || arc.Height <= 0)
                return;

            Color track =
                ProfileStyle.Blend(
                    ProfileStyle.CardColor,
                    ProfileStyle.TextColor,
                    0.12);

            using (Pen pen = new Pen(track, thickness))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                g.DrawArc(pen, arc, StartAngle, SweepAngle);
            }

            float sweep =
                (float)(SweepAngle * value);

            if (sweep > 0.5f)
            {
                using (Pen pen = new Pen(LoadColor(), thickness))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;

                    g.DrawArc(pen, arc, StartAngle, sweep);
                }
            }

            DrawCentred(
                g,
                valueText,
                ProfileStyle.Font(Math.Max(10f, size / 7.5f), FontStyle.Bold),
                ProfileStyle.TextColor,
                arc,
                -size / 14f);

            DrawCentred(
                g,
                caption,
                ProfileStyle.Font(Math.Max(7.5f, size / 16f), FontStyle.Bold),
                ProfileStyle.MutedTextColor,
                arc,
                size / 7f);

            if (!string.IsNullOrEmpty(subText))
            {
                DrawCentred(
                    g,
                    subText,
                    ProfileStyle.Font(Math.Max(7f, size / 19f)),
                    ProfileStyle.MutedTextColor,
                    arc,
                    size / 3.9f);
            }
        }

        private static void DrawCentred(
            Graphics g,
            string text,
            Font font,
            Color color,
            RectangleF area,
            float offsetY)
        {
            if (string.IsNullOrEmpty(text))
                return;

            using (font)
            using (SolidBrush brush = new SolidBrush(color))
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;
                format.FormatFlags = StringFormatFlags.NoWrap;

                RectangleF line =
                    new RectangleF(
                        area.X,
                        area.Y + offsetY,
                        area.Width,
                        area.Height);

                g.DrawString(text, font, brush, line, format);
            }
        }
    }

    /// <summary>
    /// A rolling history chart: a filled area for the main series and
    /// an optional line for a second one, such as download and upload.
    /// </summary>
    internal class Sparkline : Control, IProfileThemed
    {
        private readonly List<double> primary =
            new List<double>();

        private readonly List<double> secondary =
            new List<double>();

        public Sparkline()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.Transparent;
            Capacity = 60;
            Height = 90;
        }

        /// <summary>
        /// Samples kept. At one a second, 60 is the last minute.
        /// </summary>
        public int Capacity { get; set; }

        /// <summary>
        /// Fixed top of the scale, such as 100 for percentages. Zero or
        /// less scales to the largest visible sample instead, which is
        /// what network throughput needs.
        /// </summary>
        public double FixedMaximum { get; set; }

        public bool ShowSecondary { get; set; }

        public Color SecondaryColor { get; set; }

        public void Add(
            double primaryValue,
            double secondaryValue = 0)
        {
            primary.Add(Math.Max(0, primaryValue));
            secondary.Add(Math.Max(0, secondaryValue));

            while (primary.Count > Capacity)
                primary.RemoveAt(0);

            while (secondary.Count > Capacity)
                secondary.RemoveAt(0);

            Invalidate();
        }

        public void ApplyTheme()
        {
            Invalidate();
        }

        protected override void OnPaint(
            PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle area =
                new Rectangle(0, 0, Width - 1, Height - 1);

            if (area.Width <= 2 || area.Height <= 2)
                return;

            Color grid =
                ProfileStyle.Blend(
                    ProfileStyle.CardColor,
                    ProfileStyle.TextColor,
                    0.08);

            using (Pen pen = new Pen(grid, 1f))
            {
                for (int i = 1; i < 4; i++)
                {
                    int y = area.Top + area.Height * i / 4;
                    g.DrawLine(pen, area.Left, y, area.Right, y);
                }
            }

            if (primary.Count < 2)
                return;

            double maximum =
                FixedMaximum > 0
                    ? FixedMaximum
                    : Math.Max(
                        1,
                        Math.Max(
                            primary.Max(),
                            ShowSecondary && secondary.Count > 0
                                ? secondary.Max()
                                : 0) * 1.15);

            Color accent =
                ProfileStyle.AccentColor;

            PointF[] line =
                ToPoints(primary, area, maximum);

            // Close the line down to the baseline to fill underneath.
            List<PointF> fill =
                new List<PointF>(line);

            fill.Add(new PointF(line[line.Length - 1].X, area.Bottom));
            fill.Add(new PointF(line[0].X, area.Bottom));

            using (LinearGradientBrush brush =
                new LinearGradientBrush(
                    area,
                    Color.FromArgb(110, accent),
                    Color.FromArgb(10, accent),
                    LinearGradientMode.Vertical))
            {
                g.FillPolygon(brush, fill.ToArray());
            }

            using (Pen pen = new Pen(accent, 2f))
            {
                pen.LineJoin = LineJoin.Round;
                g.DrawLines(pen, line);
            }

            if (ShowSecondary && secondary.Count >= 2)
            {
                Color second =
                    SecondaryColor == Color.Empty
                        ? ProfileStyle.MutedTextColor
                        : SecondaryColor;

                using (Pen pen = new Pen(second, 1.6f))
                {
                    pen.LineJoin = LineJoin.Round;
                    pen.DashStyle = DashStyle.Dash;

                    g.DrawLines(pen, ToPoints(secondary, area, maximum));
                }
            }
        }

        /// <summary>
        /// Right aligned, so the newest sample always sits at the right
        /// edge and history scrolls left as it fills.
        /// </summary>
        private PointF[] ToPoints(
            List<double> values,
            Rectangle area,
            double maximum)
        {
            float step =
                area.Width / (float)Math.Max(1, Capacity - 1);

            float startX =
                area.Right - step * (values.Count - 1);

            PointF[] points =
                new PointF[values.Count];

            for (int i = 0; i < values.Count; i++)
            {
                double ratio =
                    Math.Min(1, values[i] / maximum);

                points[i] =
                    new PointF(
                        startX + step * i,
                        (float)(area.Bottom - ratio * (area.Height - 2)));
            }

            return points;
        }
    }

    /// <summary>
    /// Lays its children out top to bottom at full width, scrolling
    /// when they run past the bottom. Wrapping flow panels are sized
    /// to the height their contents need at the current width.
    /// </summary>
    internal class VerticalStack : Panel
    {
        public int Spacing { get; set; }

        public VerticalStack()
        {
            AutoScroll = true;
            Padding = new Padding(16, 14, 16, 16);
            Spacing = 14;
            BackColor = Color.Transparent;
        }

        /// <summary>
        /// Several widgets default to Dock = Top for use inside cards.
        /// Left docked here, the base layout would re-dock them over
        /// the positions set in OnLayout, so the stack owns placement.
        /// </summary>
        protected override void OnControlAdded(
            ControlEventArgs e)
        {
            e.Control.Dock = DockStyle.None;
            e.Control.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            base.OnControlAdded(e);
        }

        protected override void OnLayout(
            LayoutEventArgs levent)
        {
            int width =
                Math.Max(
                    100,
                    ClientSize.Width - Padding.Horizontal);

            int y =
                Padding.Top + AutoScrollPosition.Y;

            foreach (Control child in Controls)
            {
                if (!child.Visible)
                    continue;

                int height =
                    child is FlowLayoutPanel
                        ? child.GetPreferredSize(new Size(width, 0)).Height
                        : child.Height;

                child.SetBounds(
                    Padding.Left,
                    y,
                    width,
                    height);

                y += height + Spacing;
            }

            AutoScrollMinSize =
                new Size(
                    0,
                    y - AutoScrollPosition.Y - Spacing + Padding.Bottom);

            base.OnLayout(levent);
        }
    }

    internal static class CardStack
    {
        /// <summary>
        /// Replaces a card's contents with rows shown top to bottom in
        /// the order given, and sizes the card to fit them.
        ///
        /// WinForms docks Top controls in reverse order of addition,
        /// so the rows are added back to front.
        /// </summary>
        public static void Fill(
            Control card,
            IList<Control> rows)
        {
            card.SuspendLayout();

            try
            {
                foreach (Control old in card.Controls.Cast<Control>().ToList())
                {
                    card.Controls.Remove(old);
                    old.Dispose();
                }

                for (int i = rows.Count - 1; i >= 0; i--)
                {
                    rows[i].Dock = DockStyle.Top;
                    card.Controls.Add(rows[i]);
                }

                card.Height =
                    card.Padding.Vertical +
                    rows.Sum(x => x.Height);
            }
            finally
            {
                card.ResumeLayout();
            }
        }

        /// <summary>
        /// A muted one line message for empty sections.
        /// </summary>
        public static DevExpress.XtraEditors.LabelControl Message(
            string text)
        {
            MutedLabel label =
                new MutedLabel();

            label.AutoSizeMode =
                DevExpress.XtraEditors.LabelAutoSizeMode.None;

            label.Height = 26;
            label.Text = text;
            label.Appearance.Font = ProfileStyle.Font(9F, FontStyle.Italic);
            label.Appearance.Options.UseFont = true;

            return label;
        }
    }

    /// <summary>
    /// A wrapping row of cards that reports the height it needs, for
    /// use inside a VerticalStack.
    /// </summary>
    internal class CardGrid : FlowLayoutPanel
    {
        public CardGrid()
        {
            WrapContents = true;
            FlowDirection = FlowDirection.LeftToRight;
            AutoScroll = false;
            BackColor = Color.Transparent;
            Margin = Padding.Empty;
            Padding = Padding.Empty;
        }
    }
}
