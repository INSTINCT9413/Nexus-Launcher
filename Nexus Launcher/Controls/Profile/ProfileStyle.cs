using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.Utils.Svg;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services.Achievements;
using Nexus_Launcher.Services.Library;
using System;
using System.Drawing;

namespace Nexus_Launcher.Controls.Profile
{
    /// <summary>
    /// Colours and fonts for the profile pages, taken from the active
    /// skin so everything stays readable on light and dark themes and
    /// under custom themes.
    ///
    /// The one deliberate exception is the badge tier colours: bronze,
    /// silver, gold and platinum mean something, so they stay fixed.
    /// </summary>
    internal static class ProfileStyle
    {
        //--------------------------------------------------------------
        // Skin colours
        //--------------------------------------------------------------

        private static Skin CurrentSkin()
        {
            return CommonSkins.GetSkin(UserLookAndFeel.Default);
        }

        private static Color SkinColor(
            string name,
            Color fallback)
        {
            try
            {
                Color color =
                    CurrentSkin().Colors[name];

                return color == Color.Empty
                    ? fallback
                    : color;
            }
            catch
            {
                return fallback;
            }
        }

        public static Color TextColor
        {
            get
            {
                return SkinColor(
                    "WindowText",
                    SystemColors.ControlText);
            }
        }

        public static Color MutedTextColor
        {
            get
            {
                return Blend(
                    TextColor,
                    SkinColor("Control", SystemColors.Control),
                    0.45);
            }
        }

        public static bool IsDarkTheme
        {
            get
            {
                return Luminance(
                    SkinColor("Control", SystemColors.Control)) < 128;
            }
        }

        /// <summary>
        /// A card surface a shade off the page, lighter on dark themes
        /// and darker on light ones, so cards read as raised.
        /// </summary>
        public static Color CardColor
        {
            get
            {
                Color control =
                    SkinColor("Control", SystemColors.Control);

                return IsDarkTheme
                    ? Shift(control, 14)
                    : Shift(control, -10);
            }
        }

        private static readonly Color FallbackAccent =
            Color.FromArgb(0, 120, 215);

        /// <summary>
        /// The theme's accent, used for progress and highlights.
        ///
        /// Taken from the active SVG palette's "Accent Paint" colour, so
        /// custom themes carry their accent through: Fireball gives
        /// orange, Art House blue. DXSkinColors.FillColors.Primary was
        /// tried first but reads back as black, which would have made
        /// every bar and the level text vanish on a dark theme.
        /// </summary>
        public static Color AccentColor
        {
            get
            {
                try
                {
                    Skin skin =
                        CurrentSkin();

                    SvgPalette palette =
                        skin.SvgPalettes[Skin.DefaultSkinPaletteName];

                    SvgColor accent =
                        palette == null
                            ? null
                            : palette["Accent Paint"];

                    if (accent != null && HasContrast(accent.Value))
                        return accent.Value;

                    // Bitmap skins and WXI have no Accent Paint. Their
                    // Highlight is a selection colour that is sometimes
                    // too pale to read as a bar, so only use it when it
                    // stands out from the card.
                    Color highlight =
                        skin.Colors["Highlight"];

                    if (highlight != Color.Empty && HasContrast(highlight))
                        return highlight;
                }
                catch
                {
                }

                return FallbackAccent;
            }
        }

        /// <summary>
        /// Whether a colour will stand out against the card surface.
        /// </summary>
        private static bool HasContrast(
            Color color)
        {
            return color.A > 0 &&
                Math.Abs(Luminance(color) - Luminance(CardColor)) > 55;
        }

        //--------------------------------------------------------------
        // Badge tiers
        //--------------------------------------------------------------

        public static Color TierColor(
            BadgeTier tier)
        {
            switch (tier)
            {
                case BadgeTier.Bronze:
                    return Color.FromArgb(205, 127, 50);

                case BadgeTier.Silver:
                    return Color.FromArgb(168, 176, 188);

                case BadgeTier.Gold:
                    return Color.FromArgb(230, 180, 20);

                case BadgeTier.Platinum:
                    return Color.FromArgb(90, 200, 220);

                default:
                    return MutedTextColor;
            }
        }

        //--------------------------------------------------------------
        // Fonts
        //--------------------------------------------------------------

        /// <summary>
        /// A font in the user's chosen UI family. FontManager keeps the
        /// size and style when it re-applies the family, so sizes set
        /// here survive.
        /// </summary>
        public static Font Font(
            float size,
            FontStyle style = FontStyle.Regular)
        {
            string family =
                string.IsNullOrWhiteSpace(Settings.Default.UIFont)
                    ? "Segoe UI"
                    : Settings.Default.UIFont;

            try
            {
                return new Font(family, size, style);
            }
            catch
            {
                return new Font("Segoe UI", size, style);
            }
        }

        //--------------------------------------------------------------
        // Icons
        //--------------------------------------------------------------

        public static SvgImage Svg(
            string key)
        {
            return LibraryGroupIcons.GetSvgImage(key);
        }

        //--------------------------------------------------------------
        // Formatting
        //--------------------------------------------------------------

        public static string FormatHours(
            long seconds)
        {
            if (seconds <= 0)
                return "0h";

            return SidePanelPresenter.FormatDuration(
                TimeSpan.FromSeconds(seconds));
        }

        public static string FormatBytes(
            long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };

            int unit = 0;
            double value = bytes;

            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }

            return value.ToString(unit < 3 ? "F0" : "F1") + " " + units[unit];
        }

        //--------------------------------------------------------------
        // Colour maths
        //--------------------------------------------------------------

        public static double Luminance(
            Color color)
        {
            return (0.299 * color.R) +
                (0.587 * color.G) +
                (0.114 * color.B);
        }

        public static Color Shift(
            Color color,
            int amount)
        {
            return Color.FromArgb(
                color.A,
                Clamp(color.R + amount),
                Clamp(color.G + amount),
                Clamp(color.B + amount));
        }

        /// <summary>
        /// Mixes two colours, amount 0 being all of a and 1 all of b.
        /// </summary>
        public static Color Blend(
            Color a,
            Color b,
            double amount)
        {
            return Color.FromArgb(
                Clamp((int)(a.R + (b.R - a.R) * amount)),
                Clamp((int)(a.G + (b.G - a.G) * amount)),
                Clamp((int)(a.B + (b.B - a.B) * amount)));
        }

        private static int Clamp(
            int value)
        {
            return value < 0
                ? 0
                : value > 255
                    ? 255
                    : value;
        }
    }
}
