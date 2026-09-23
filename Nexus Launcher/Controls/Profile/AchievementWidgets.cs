using DevExpress.Utils;
using DevExpress.Utils.Svg;
using DevExpress.XtraEditors;
using Nexus_Launcher.Services.Achievements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls.Profile
{
    /// <summary>
    /// A round medal: a disc in the badge's tier colour with its glyph
    /// drawn in the middle. Greyed out while locked.
    /// </summary>
    internal class BadgeMedal : Control, IProfileThemed
    {
        /// <summary>
        /// Rendered glyphs, keyed on icon and pixel size. Rendering an
        /// SVG is not free and the same few icons repeat everywhere.
        /// </summary>
        private static readonly Dictionary<string, Image> glyphCache =
            new Dictionary<string, Image>();

        private string iconKey;
        private Color discColor = Color.Gray;
        private bool locked;

        public BadgeMedal()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.Transparent;
            Size = new Size(56, 56);
        }

        public string IconKey
        {
            get
            {
                return iconKey;
            }
            set
            {
                iconKey = value;
                Invalidate();
            }
        }

        public Color DiscColor
        {
            get
            {
                return discColor;
            }
            set
            {
                discColor = value;
                Invalidate();
            }
        }

        public bool Locked
        {
            get
            {
                return locked;
            }
            set
            {
                locked = value;
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
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            int size =
                Math.Min(Width, Height) - 2;

            if (size <= 4)
                return;

            Rectangle disc =
                new Rectangle(
                    (Width - size) / 2,
                    (Height - size) / 2,
                    size,
                    size);

            Color fill =
                locked
                    ? ProfileStyle.Blend(
                        ProfileStyle.CardColor,
                        ProfileStyle.TextColor,
                        0.14)
                    : ProfileStyle.Blend(
                        discColor,
                        ProfileStyle.CardColor,
                        0.55);

            Color ring =
                locked
                    ? ProfileStyle.Blend(
                        ProfileStyle.CardColor,
                        ProfileStyle.TextColor,
                        0.28)
                    : discColor;

            using (SolidBrush brush = new SolidBrush(fill))
            {
                e.Graphics.FillEllipse(brush, disc);
            }

            using (Pen pen = new Pen(ring, Math.Max(2f, size / 18f)))
            {
                Rectangle inner = disc;
                inner.Inflate(-1, -1);
                e.Graphics.DrawEllipse(pen, inner);
            }

            int glyphSize =
                (int)(size * 0.52);

            Image glyph =
                GetGlyph(iconKey, glyphSize);

            if (glyph == null)
                return;

            Rectangle target =
                new Rectangle(
                    disc.X + (disc.Width - glyphSize) / 2,
                    disc.Y + (disc.Height - glyphSize) / 2,
                    glyphSize,
                    glyphSize);

            if (!locked)
            {
                e.Graphics.DrawImage(glyph, target);

                return;
            }

            // Locked glyphs are drawn faded so the medal reads as not
            // yet earned without hiding what it is for.
            using (ImageAttributes faded = new ImageAttributes())
            {
                ColorMatrix matrix =
                    new ColorMatrix();

                matrix.Matrix33 = 0.35f;

                faded.SetColorMatrix(matrix);

                e.Graphics.DrawImage(
                    glyph,
                    target,
                    0,
                    0,
                    glyph.Width,
                    glyph.Height,
                    GraphicsUnit.Pixel,
                    faded);
            }
        }

        /// <summary>
        /// Shared with the unlock popup, which wants the same glyphs at
        /// its own size and benefits from the same cache.
        /// </summary>
        internal static Image GetGlyph(
            string key,
            int pixelSize)
        {
            if (string.IsNullOrEmpty(key) || pixelSize <= 0)
                return null;

            string cacheKey =
                key + "@" + pixelSize.ToString(CultureInfo.InvariantCulture);

            Image cached;

            if (glyphCache.TryGetValue(cacheKey, out cached))
                return cached;

            Image rendered = null;

            try
            {
                SvgImage svg =
                    ProfileStyle.Svg(key);

                if (svg != null)
                {
                    rendered =
                        SvgBitmap.Create(svg).Render(
                            new Size(pixelSize, pixelSize),
                            null,
                            DefaultBoolean.Default,
                            DefaultBoolean.Default);
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            glyphCache[cacheKey] = rendered;

            return rendered;
        }
    }

    /// <summary>
    /// A tiered badge: medal, current tier and progress to the next.
    /// </summary>
    internal class BadgeCard : CardPanel
    {
        private readonly BadgeMedal medal;
        private readonly LabelControl titleLabel;
        private readonly LabelControl tierLabel;
        private readonly BarMeter meter;
        private readonly LabelControl progressLabel;
        private readonly LabelControl nextLabel;

        private BadgeProgress progress;

        public BadgeCard()
        {
            Size = new Size(268, 118);
            Margin = new Padding(0, 0, 12, 12);

            medal = new BadgeMedal();
            medal.Location = new Point(12, 14);
            medal.Size = new Size(60, 60);

            titleLabel = CreateLabel(84, 12, 172, 22, 11F, FontStyle.Bold);
            tierLabel = CreateLabel(84, 34, 172, 18, 9F, FontStyle.Bold);

            meter = new BarMeter();
            meter.Location = new Point(84, 60);
            meter.Size = new Size(170, 7);

            progressLabel = CreateLabel(84, 70, 172, 18, 8.5F, FontStyle.Regular);
            nextLabel = CreateLabel(12, 92, 244, 18, 8.25F, FontStyle.Italic);

            Controls.Add(medal);
            Controls.Add(titleLabel);
            Controls.Add(tierLabel);
            Controls.Add(meter);
            Controls.Add(progressLabel);
            Controls.Add(nextLabel);
        }

        public void Bind(
            BadgeProgress value)
        {
            progress = value;

            BadgeDefinition definition =
                value.Definition;

            titleLabel.Text = definition.Title;

            tierLabel.Text =
                AchievementService.TierName(value.Tier);

            medal.IconKey = definition.IconKey;
            medal.Locked = value.Tier == BadgeTier.None;
            medal.DiscColor = ProfileStyle.TierColor(value.Tier);

            meter.Value = value.FractionToNext;

            if (value.IsMaxed)
            {
                progressLabel.Text =
                    value.Current + " " + definition.Unit;

                nextLabel.Text = "Maxed out. Platinum reached.";
            }
            else
            {
                progressLabel.Text =
                    value.Current + " / " + value.NextThreshold + " " +
                    definition.Unit;

                BadgeTier next =
                    (BadgeTier)((int)value.Tier + 1);

                nextLabel.Text =
                    "Next, " + next + ": " +
                    string.Format(
                        definition.DescriptionFormat,
                        value.NextThreshold);
            }

            // The bar shows the colour of the tier being worked towards.
            meter.FillColor =
                ProfileStyle.TierColor(
                    value.IsMaxed
                        ? BadgeTier.Platinum
                        : (BadgeTier)((int)value.Tier + 1));

            ApplyTheme();
        }

        public override void ApplyTheme()
        {
            base.ApplyTheme();

            if (tierLabel == null || progress == null)
                return;

            tierLabel.Appearance.ForeColor =
                ProfileStyle.TierColor(progress.Tier);

            tierLabel.Appearance.Options.UseForeColor = true;

            progressLabel.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            progressLabel.Appearance.Options.UseForeColor = true;

            nextLabel.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            nextLabel.Appearance.Options.UseForeColor = true;
        }

        private static LabelControl CreateLabel(
            int x,
            int y,
            int width,
            int height,
            float size,
            FontStyle style)
        {
            LabelControl label =
                new LabelControl();

            label.AutoSizeMode = LabelAutoSizeMode.None;
            label.Location = new Point(x, y);
            label.Size = new Size(width, height);
            label.Appearance.Font = ProfileStyle.Font(size, style);
            label.Appearance.Options.UseFont = true;
            label.Appearance.TextOptions.Trimming = Trimming.EllipsisCharacter;
            label.Appearance.Options.UseTextOptions = true;

            return label;
        }
    }

    /// <summary>
    /// A one off achievement: earned or not, with progress where the
    /// goal is more than a single step.
    /// </summary>
    internal class AchievementCard : CardPanel
    {
        private readonly BadgeMedal medal;
        private readonly LabelControl titleLabel;
        private readonly LabelControl descriptionLabel;
        private readonly BarMeter meter;
        private readonly LabelControl statusLabel;
        private readonly LabelControl pointsLabel;

        private AchievementProgress progress;

        public AchievementCard()
        {
            Size = new Size(268, 104);
            Margin = new Padding(0, 0, 12, 12);

            medal = new BadgeMedal();
            medal.Location = new Point(12, 14);
            medal.Size = new Size(48, 48);

            titleLabel = new LabelControl();
            titleLabel.AutoSizeMode = LabelAutoSizeMode.None;
            titleLabel.Location = new Point(72, 12);
            titleLabel.Size = new Size(184, 20);
            titleLabel.Appearance.Font = ProfileStyle.Font(10.5F, FontStyle.Bold);
            titleLabel.Appearance.Options.UseFont = true;

            descriptionLabel = new LabelControl();
            descriptionLabel.AutoSizeMode = LabelAutoSizeMode.None;
            descriptionLabel.Location = new Point(72, 33);
            descriptionLabel.Size = new Size(184, 34);
            descriptionLabel.Appearance.Font = ProfileStyle.Font(8.5F);
            descriptionLabel.Appearance.Options.UseFont = true;
            descriptionLabel.Appearance.TextOptions.WordWrap = WordWrap.Wrap;
            descriptionLabel.Appearance.Options.UseTextOptions = true;

            meter = new BarMeter();
            meter.Location = new Point(72, 70);
            meter.Size = new Size(184, 6);

            statusLabel = new LabelControl();
            statusLabel.AutoSizeMode = LabelAutoSizeMode.None;
            statusLabel.Location = new Point(72, 80);
            statusLabel.Size = new Size(130, 18);
            statusLabel.Appearance.Font = ProfileStyle.Font(8.25F);
            statusLabel.Appearance.Options.UseFont = true;

            pointsLabel = new LabelControl();
            pointsLabel.AutoSizeMode = LabelAutoSizeMode.None;
            pointsLabel.Location = new Point(200, 80);
            pointsLabel.Size = new Size(56, 18);
            pointsLabel.Appearance.Font = ProfileStyle.Font(8.25F, FontStyle.Bold);
            pointsLabel.Appearance.Options.UseFont = true;
            pointsLabel.Appearance.TextOptions.HAlignment = HorzAlignment.Far;
            pointsLabel.Appearance.Options.UseTextOptions = true;

            Controls.Add(medal);
            Controls.Add(titleLabel);
            Controls.Add(descriptionLabel);
            Controls.Add(meter);
            Controls.Add(statusLabel);
            Controls.Add(pointsLabel);
        }

        public void Bind(
            AchievementProgress value)
        {
            progress = value;

            AchievementDefinition definition =
                value.Definition;

            titleLabel.Text = definition.Title;
            descriptionLabel.Text = definition.Description;

            medal.IconKey = definition.IconKey;
            medal.Locked = !value.Unlocked;
            medal.DiscColor = ProfileStyle.AccentColor;

            pointsLabel.Text = "+" + definition.Points + " XP";

            // A bar only means something when the goal has steps.
            bool showMeter =
                !value.Unlocked && definition.Target > 1;

            meter.Visible = showMeter;
            meter.Value = value.Fraction;

            if (value.Unlocked)
            {
                statusLabel.Text =
                    value.UnlockedUtc.HasValue
                        ? "Unlocked " +
                            value.UnlockedUtc.Value.ToLocalTime()
                                .ToString("d MMM yyyy", CultureInfo.CurrentCulture)
                        : "Unlocked";
            }
            else if (showMeter)
            {
                statusLabel.Text =
                    Math.Min(value.Current, definition.Target) + " / " +
                    definition.Target + " " + definition.Unit;
            }
            else
            {
                statusLabel.Text = "Locked";
            }

            ApplyTheme();
        }

        public override void ApplyTheme()
        {
            base.ApplyTheme();

            if (descriptionLabel == null)
                return;

            descriptionLabel.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            descriptionLabel.Appearance.Options.UseForeColor = true;

            statusLabel.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            statusLabel.Appearance.Options.UseForeColor = true;

            bool unlocked =
                progress != null && progress.Unlocked;

            pointsLabel.Appearance.ForeColor =
                unlocked
                    ? ProfileStyle.AccentColor
                    : ProfileStyle.MutedTextColor;

            pointsLabel.Appearance.Options.UseForeColor = true;

            // Keep the medal on the current accent after a theme switch.
            if (medal != null)
                medal.DiscColor = ProfileStyle.AccentColor;
        }
    }
}
