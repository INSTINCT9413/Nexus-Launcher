using DevExpress.Utils.Svg;
using DevExpress.XtraEditors;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls.Profile
{
    /// <summary>
    /// Anything on the profile pages that has to recolour itself when
    /// the skin changes.
    /// </summary>
    internal interface IProfileThemed
    {
        void ApplyTheme();
    }

    internal static class ProfileTheme
    {
        /// <summary>
        /// Walks a control tree re-applying theme colours.
        /// </summary>
        public static void Apply(
            Control root)
        {
            if (root == null)
                return;

            IProfileThemed themed =
                root as IProfileThemed;

            if (themed != null)
                themed.ApplyTheme();

            foreach (Control child in root.Controls)
            {
                Apply(child);
            }

            root.Invalidate();
        }
    }

    /// <summary>
    /// A slim rounded progress bar.
    ///
    /// Drawn by hand rather than using ProgressBarControl: DevExpress
    /// skins paint over a progress bar's custom colours unless skinning
    /// is switched off on that control, and the badge bars need their
    /// fixed bronze, silver, gold and platinum colours.
    /// </summary>
    internal class BarMeter : Control, IProfileThemed
    {
        private double value;
        private Color fillColor;
        private bool customFill;

        public BarMeter()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.Transparent;
            Height = 6;
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

        /// <summary>
        /// Fixed fill colour. Leave unset to follow the skin accent.
        /// </summary>
        public Color FillColor
        {
            get
            {
                return customFill
                    ? fillColor
                    : ProfileStyle.AccentColor;
            }
            set
            {
                fillColor = value;
                customFill = value != Color.Empty;
                Invalidate();
            }
        }

        public void ApplyTheme()
        {
            Invalidate();
        }

        protected override void OnPaint(
            PaintEventArgs e)
        {
            e.Graphics.SmoothingMode =
                SmoothingMode.AntiAlias;

            Rectangle track =
                new Rectangle(0, 0, Width - 1, Height - 1);

            if (track.Width <= 0 || track.Height <= 0)
                return;

            Color trackColor =
                ProfileStyle.Blend(
                    ProfileStyle.CardColor,
                    ProfileStyle.TextColor,
                    0.12);

            using (GraphicsPath path = Rounded(track))
            using (SolidBrush brush = new SolidBrush(trackColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            int fillWidth =
                (int)Math.Round(track.Width * value);

            // Too narrow to round cleanly; skip rather than draw a
            // smear at the start of the track.
            if (fillWidth < track.Height)
                return;

            Rectangle fill =
                new Rectangle(track.X, track.Y, fillWidth, track.Height);

            using (GraphicsPath path = Rounded(fill))
            using (SolidBrush brush = new SolidBrush(FillColor))
            {
                e.Graphics.FillPath(brush, path);
            }
        }

        private static GraphicsPath Rounded(
            Rectangle bounds)
        {
            int diameter =
                Math.Max(1, bounds.Height);

            GraphicsPath path =
                new GraphicsPath();

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 90, 180);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 180);
            path.CloseFigure();

            return path;
        }
    }

    /// <summary>
    /// A raised surface to group related content.
    /// </summary>
    internal class CardPanel : PanelControl, IProfileThemed
    {
        public CardPanel()
        {
            BorderStyle =
                DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;

            Padding = new Padding(14, 10, 14, 12);

            ApplyTheme();
        }

        // Virtual so subclasses like StatTile are reached when the
        // theme walker calls through IProfileThemed.
        public virtual void ApplyTheme()
        {
            Appearance.BackColor = ProfileStyle.CardColor;
            Appearance.Options.UseBackColor = true;
        }
    }

    /// <summary>
    /// Secondary text in the theme's muted colour, kept in step when
    /// the theme changes. A plain label given a colour once keeps the
    /// old theme's colour after a switch.
    /// </summary>
    internal class MutedLabel : LabelControl, IProfileThemed
    {
        public MutedLabel()
        {
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            Appearance.ForeColor = ProfileStyle.MutedTextColor;
            Appearance.Options.UseForeColor = true;
        }
    }

    /// <summary>
    /// A heading inside a card.
    /// </summary>
    internal class SectionTitle : LabelControl, IProfileThemed
    {
        public SectionTitle(
            string text,
            string iconKey = null)
        {
            Text = text;

            Appearance.Font =
                ProfileStyle.Font(11F, FontStyle.Bold);

            Appearance.Options.UseFont = true;

            AutoSizeMode = LabelAutoSizeMode.None;
            Dock = DockStyle.Top;
            Height = 28;

            if (!string.IsNullOrEmpty(iconKey))
            {
                ImageOptions.SvgImage = ProfileStyle.Svg(iconKey);
                ImageOptions.SvgImageSize = new Size(18, 18);
                ImageOptions.Alignment = ContentAlignment.MiddleLeft;
                ImageAlignToText = ImageAlignToText.LeftCenter;
            }

            ApplyTheme();
        }

        public void ApplyTheme()
        {
            ImageOptions.SvgImageColorizationMode =
                DevExpress.Utils.SvgImageColorizationMode.CommonPalette;
        }
    }

    /// <summary>
    /// A single headline number: an icon, a big value and a caption.
    /// </summary>
    internal class StatTile : CardPanel
    {
        private readonly LabelControl icon;
        private readonly LabelControl valueLabel;
        private readonly LabelControl captionLabel;

        public StatTile(
            string caption,
            string iconKey)
        {
            Size = new Size(176, 72);
            Margin = new Padding(0, 0, 10, 10);
            Padding = new Padding(12, 8, 10, 8);

            icon = new LabelControl();
            icon.AutoSizeMode = LabelAutoSizeMode.None;
            icon.Size = new Size(34, 54);
            icon.Location = new Point(10, 9);
            icon.ImageOptions.SvgImage = ProfileStyle.Svg(iconKey);
            icon.ImageOptions.SvgImageSize = new Size(28, 28);
            icon.ImageOptions.Alignment = ContentAlignment.MiddleCenter;

            valueLabel = new LabelControl();
            valueLabel.AutoSizeMode = LabelAutoSizeMode.None;
            valueLabel.Location = new Point(50, 8);
            valueLabel.Size = new Size(120, 32);
            valueLabel.Appearance.Font = ProfileStyle.Font(17F, FontStyle.Bold);
            valueLabel.Appearance.Options.UseFont = true;
            valueLabel.Text = "-";

            captionLabel = new LabelControl();
            captionLabel.AutoSizeMode = LabelAutoSizeMode.None;
            captionLabel.Location = new Point(50, 42);
            captionLabel.Size = new Size(120, 20);
            captionLabel.Appearance.Font = ProfileStyle.Font(8.5F);
            captionLabel.Appearance.Options.UseFont = true;
            captionLabel.Text = caption;

            Controls.Add(icon);
            Controls.Add(valueLabel);
            Controls.Add(captionLabel);

            ApplyTheme();
        }

        public void SetValue(
            string value)
        {
            valueLabel.Text = value;
        }

        public override void ApplyTheme()
        {
            base.ApplyTheme();

            if (captionLabel == null)
                return;

            captionLabel.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            captionLabel.Appearance.Options.UseForeColor = true;

            icon.ImageOptions.SvgImageColorizationMode =
                DevExpress.Utils.SvgImageColorizationMode.CommonPalette;
        }
    }

    /// <summary>
    /// A labelled bar: caption on the left, value on the right, meter
    /// underneath. Used for CPU and RAM load and for play time shares.
    /// </summary>
    internal class MeterRow : Panel, IProfileThemed
    {
        private readonly LabelControl captionLabel;
        private readonly LabelControl valueLabel;
        private readonly BarMeter meter;

        public MeterRow(
            string caption)
        {
            Height = 40;
            Dock = DockStyle.Top;
            Padding = new Padding(0, 2, 0, 6);
            BackColor = Color.Transparent;

            meter = new BarMeter();
            meter.Dock = DockStyle.Bottom;
            meter.Height = 7;

            captionLabel = new LabelControl();
            captionLabel.Dock = DockStyle.Left;
            captionLabel.AutoSizeMode = LabelAutoSizeMode.Horizontal;
            captionLabel.Appearance.Font = ProfileStyle.Font(9F);
            captionLabel.Appearance.Options.UseFont = true;
            captionLabel.Text = caption;

            valueLabel = new LabelControl();
            valueLabel.Dock = DockStyle.Right;
            valueLabel.AutoSizeMode = LabelAutoSizeMode.Horizontal;
            valueLabel.Appearance.Font = ProfileStyle.Font(9F, FontStyle.Bold);
            valueLabel.Appearance.Options.UseFont = true;
            valueLabel.Text = "-";

            Controls.Add(captionLabel);
            Controls.Add(valueLabel);
            Controls.Add(meter);
        }

        public string Caption
        {
            get
            {
                return captionLabel.Text;
            }
            set
            {
                captionLabel.Text = value;
            }
        }

        public void SetValue(
            double fraction,
            string text)
        {
            meter.Value = fraction;
            valueLabel.Text = text;
        }

        public Color FillColor
        {
            get
            {
                return meter.FillColor;
            }
            set
            {
                meter.FillColor = value;
            }
        }

        public void ApplyTheme()
        {
            meter.ApplyTheme();
        }
    }

    /// <summary>
    /// A label: value pair on one line, for spec sheets.
    /// </summary>
    internal class SpecRow : Panel, IProfileThemed
    {
        private readonly LabelControl keyLabel;
        private readonly LabelControl valueLabel;

        public SpecRow(
            string key,
            string value)
        {
            Height = 24;
            Dock = DockStyle.Top;
            BackColor = Color.Transparent;

            keyLabel = new LabelControl();
            keyLabel.Dock = DockStyle.Left;
            keyLabel.AutoSizeMode = LabelAutoSizeMode.None;
            keyLabel.Width = 130;
            keyLabel.Appearance.Font = ProfileStyle.Font(9F);
            keyLabel.Appearance.Options.UseFont = true;
            keyLabel.Text = key;

            valueLabel = new LabelControl();
            valueLabel.Dock = DockStyle.Fill;
            valueLabel.AutoSizeMode = LabelAutoSizeMode.None;
            valueLabel.Appearance.Font = ProfileStyle.Font(9F, FontStyle.Bold);
            valueLabel.Appearance.Options.UseFont = true;
            valueLabel.Appearance.TextOptions.Trimming =
                DevExpress.Utils.Trimming.EllipsisCharacter;
            valueLabel.Appearance.Options.UseTextOptions = true;
            valueLabel.Text = string.IsNullOrWhiteSpace(value) ? "Unknown" : value;

            // Hover shows the full value when it is too long to fit.
            valueLabel.ToolTip = valueLabel.Text;

            Controls.Add(valueLabel);
            Controls.Add(keyLabel);

            ApplyTheme();
        }

        /// <summary>
        /// Updates the value in place, for readings that change live.
        /// </summary>
        public void SetValue(
            string value)
        {
            valueLabel.Text =
                string.IsNullOrWhiteSpace(value)
                    ? "Unknown"
                    : value;

            valueLabel.ToolTip = valueLabel.Text;
        }

        public void ApplyTheme()
        {
            keyLabel.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            keyLabel.Appearance.Options.UseForeColor = true;
        }
    }
}
