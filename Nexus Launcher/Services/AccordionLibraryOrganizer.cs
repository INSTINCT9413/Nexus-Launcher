using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.XtraBars.Navigation;
using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Nexus_Launcher.Services.Library
{
    /// <summary>
    /// Lays the games of one launcher out inside its Accordion group.
    ///
    /// Favourites sort to the top of whatever they belong to: a
    /// favourite inside a user group rises to the top of that group
    /// and stays in it, an ungrouped favourite rises to the top of the
    /// launcher.
    ///
    /// The scanners keep building plain item elements exactly as they
    /// did before. They just hand the finished list to Arrange instead
    /// of adding it to the launcher group themselves.
    /// </summary>
    internal static class AccordionLibraryOrganizer
    {
        public const string FavoriteGlyph = "★ ";

        /// <summary>
        /// Remembers which launcher an Accordion group belongs to so
        /// Rearrange can redraw it without a rescan.
        /// </summary>
        private static readonly Dictionary<AccordionControlElement, string> launcherNames =
            new Dictionary<AccordionControlElement, string>();

        /// <summary>
        /// When each launcher was last populated from its scanner,
        /// shown as "Last scan" in the side panel.
        /// </summary>
        private static readonly Dictionary<AccordionControlElement, DateTime> lastScans =
            new Dictionary<AccordionControlElement, DateTime>();

        /// <summary>
        /// isScan separates a real rescan from a redraw after a
        /// favourite or group change, which must not move the
        /// "last scan" time.
        /// </summary>
        public static void Arrange(
            AccordionControlElement launcherGroup,
            string launcher,
            IEnumerable<AccordionControlElement> items,
            bool isScan = true)
        {
            if (launcherGroup == null)
                return;

            launcherNames[launcherGroup] = launcher;

            if (isScan)
                lastScans[launcherGroup] = DateTime.UtcNow;

            List<AccordionControlElement> gameItems =
                (items ?? Enumerable.Empty<AccordionControlElement>())
                .Where(x => x != null && x.Tag is GameInfo)
                .ToList();

            // Scanners are inconsistent about filling this in, and the
            // key used to store favourites and groups depends on it.
            foreach (AccordionControlElement item in gameItems)
            {
                GameInfo game =
                    (GameInfo)item.Tag;

                if (string.IsNullOrWhiteSpace(game.Launcher))
                    game.Launcher = launcher;

                // Custom artwork is normally applied by
                // ArtworkCache.LoadCachedArtwork, but Nexus Launcher
                // entries never register for artwork at all, so that
                // never runs for them and their custom art would be
                // lost on restart. Every game from every launcher
                // passes through here, after Launcher is stamped, so
                // this is the one place that covers all of them.
                Artwork.CustomArtworkService.ApplyTo(game);
            }

            // Rearrange reuses the elements that are already on screen,
            // so take them off their current parent before the group is
            // cleared out.
            foreach (AccordionControlElement item in gameItems)
            {
                if (item.OwnerElement != null)
                    item.OwnerElement.Elements.Remove(item);
            }

            launcherGroup.Elements.Clear();

            List<AccordionControlElement> ungrouped =
                new List<AccordionControlElement>(gameItems);

            // Build the group branches first so what is left over is
            // exactly the ungrouped games.
            List<AccordionControlElement> groupElements =
                new List<AccordionControlElement>();

            foreach (LibraryGroup group in
                LibraryOrganizationService.GetGroups(launcher))
            {
                List<AccordionControlElement> members =
                    ungrouped
                        .Where(x => group.Games.Contains(
                            LibraryOrganizationService.GetKey(
                                (GameInfo)x.Tag)))
                        .ToList();

                AccordionControlElement groupElement =
                    new AccordionControlElement();

                groupElement.Style =
                    ElementStyle.Group;

                groupElement.Text =
                    group.Name;

                groupElement.Tag =
                    group;

                groupElement.Expanded =
                    true;

                ApplyGroupAppearance(
                    groupElement,
                    group,
                    launcherGroup);

                // A favourite rises to the top of its own group rather
                // than being lifted out of it.
                foreach (AccordionControlElement item in Sort(members))
                {
                    groupElement.Elements.Add(item);

                    ungrouped.Remove(item);
                }

                groupElements.Add(groupElement);
            }

            List<AccordionControlElement> sortedUngrouped =
                Sort(ungrouped);

            int favoriteCount =
                sortedUngrouped.Count(x => IsFavorite(x));

            // Ungrouped favourites sit above the groups, the rest of
            // the ungrouped games below them.
            foreach (AccordionControlElement item in
                sortedUngrouped.Take(favoriteCount))
            {
                launcherGroup.Elements.Add(item);
            }

            foreach (AccordionControlElement groupElement in groupElements)
            {
                launcherGroup.Elements.Add(groupElement);
            }

            foreach (AccordionControlElement item in
                sortedUngrouped.Skip(favoriteCount))
            {
                launcherGroup.Elements.Add(item);
            }
        }

        /// <summary>
        /// Favourites first, then everything else, each run A to Z.
        /// </summary>
        private static List<AccordionControlElement> Sort(
            IEnumerable<AccordionControlElement> items)
        {
            List<AccordionControlElement> sorted =
                items
                    .OrderByDescending(x => IsFavorite(x))
                    .ThenBy(x => ((GameInfo)x.Tag).Name)
                    .ToList();

            foreach (AccordionControlElement item in sorted)
            {
                ApplyText(
                    item,
                    IsFavorite(item));
            }

            return sorted;
        }

        private static bool IsFavorite(
            AccordionControlElement item)
        {
            return LibraryOrganizationService.IsFavorite(
                (GameInfo)item.Tag);
        }

        /// <summary>
        /// Gives a group header its icon and a text colour taken from
        /// the active skin, so it stays readable whichever theme the
        /// user has picked.
        /// </summary>
        private static void ApplyGroupAppearance(
            AccordionControlElement groupElement,
            LibraryGroup group,
            AccordionControlElement launcherGroup)
        {
            Image custom =
                LibraryGroupIcons.GetCustomImage(group.IconPath);

            if (custom != null)
            {
                groupElement.ImageOptions.Image = custom;
            }
            else
            {
                groupElement.ImageOptions.SvgImage =
                    LibraryGroupIcons.GetSvgImage(
                        string.IsNullOrWhiteSpace(group.IconKey)
                            ? LibraryGroupIcons.DefaultKey
                            : group.IconKey);

                // Recolours the glyph to match the skin.
                groupElement.ImageOptions.SvgImageColorizationMode =
                    DevExpress.Utils.SvgImageColorizationMode.CommonPalette;

                groupElement.ImageOptions.SvgImageSize =
                    LibraryGroupIcons.IconSize;
            }

            AccordionControl accordion =
                launcherGroup.AccordionControl;

            // A group header is auto sized the same way an item is, so
            // matching the icon size is what keeps the two the same
            // height. If the accordion has been given a fixed item
            // height, follow that instead.
            if (accordion != null && accordion.ItemHeight > 0)
                groupElement.Height = accordion.ItemHeight;

            UserLookAndFeel lookAndFeel =
                accordion != null
                    ? accordion.LookAndFeel
                    : UserLookAndFeel.Default;

            Color foreColor =
                GetThemeTextColor(lookAndFeel);

            Color backColor =
                GetGroupBackColor(lookAndFeel);

            Font font =
                GetGroupFont(accordion);

            ApplyAppearance(
                groupElement.Appearance.Normal,
                foreColor,
                backColor,
                font);

            // No back colour on these two so the skin's own hover and
            // press highlight still comes through.
            ApplyAppearance(
                groupElement.Appearance.Hovered,
                foreColor,
                Color.Empty,
                font);

            ApplyAppearance(
                groupElement.Appearance.Pressed,
                foreColor,
                Color.Empty,
                font);
        }

        private static void ApplyAppearance(
            DevExpress.Utils.AppearanceObject appearance,
            Color foreColor,
            Color backColor,
            Font font)
        {
            appearance.ForeColor = foreColor;
            appearance.Options.UseForeColor = true;

            if (backColor != Color.Empty)
            {
                appearance.BackColor = backColor;
                appearance.Options.UseBackColor = true;
            }

            if (font == null)
                return;

            appearance.Font = font;
            appearance.Options.UseFont = true;
        }

        /// <summary>
        /// A shade off the skin's control colour, so a group reads as
        /// distinct from the games around it. Light themes get a
        /// slightly darker band, dark themes a slightly lighter one.
        /// </summary>
        private static Color GetGroupBackColor(
            UserLookAndFeel lookAndFeel)
        {
            try
            {
                Skin skin =
                    CommonSkins.GetSkin(lookAndFeel);

                Color control =
                    skin.Colors["Control"];

                if (control == Color.Empty)
                    return Color.Empty;

                double luminance =
                    (0.299 * control.R) +
                    (0.587 * control.G) +
                    (0.114 * control.B);

                return luminance < 128
                    ? Shift(control, 26)
                    : Shift(control, -24);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return Color.Empty;
            }
        }

        private static Color Shift(
            Color color,
            int amount)
        {
            return Color.FromArgb(
                color.A,
                Clamp(color.R + amount),
                Clamp(color.G + amount),
                Clamp(color.B + amount));
        }

        private static int Clamp(
            int value)
        {
            if (value < 0)
                return 0;

            return value > 255
                ? 255
                : value;
        }

        private static Color GetThemeTextColor(
            UserLookAndFeel lookAndFeel)
        {
            try
            {
                Skin skin =
                    CommonSkins.GetSkin(lookAndFeel);

                Color color =
                    skin.Colors["WindowText"];

                if (color != Color.Empty)
                    return color;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            return SystemColors.ControlText;
        }

        /// <summary>
        /// Bold version of whatever font the accordion is using, so the
        /// user's font setting is still respected.
        /// </summary>
        private static Font GetGroupFont(
            AccordionControl accordion)
        {
            try
            {
                Font baseFont =
                    accordion != null
                        ? accordion.Font
                        : null;

                if (baseFont == null)
                    return null;

                return new Font(
                    baseFont,
                    FontStyle.Bold);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return null;
            }
        }

        /// <summary>
        /// Redraws a launcher after a favourite or group change,
        /// reusing the item elements that are already built.
        /// </summary>
        public static void Rearrange(
            AccordionControlElement launcherGroup)
        {
            if (launcherGroup == null)
                return;

            string launcher;

            if (!launcherNames.TryGetValue(
                launcherGroup,
                out launcher))
            {
                return;
            }

            List<AccordionControlElement> items =
                new List<AccordionControlElement>();

            Collect(
                launcherGroup,
                items);

            Arrange(
                launcherGroup,
                launcher,
                items,
                false);
        }

        /// <summary>
        /// When this launcher was last populated from its scanner.
        /// </summary>
        public static DateTime? GetLastScan(
            AccordionControlElement launcherGroup)
        {
            DateTime scanned;

            if (launcherGroup != null &&
                lastScans.TryGetValue(
                    launcherGroup,
                    out scanned))
            {
                return scanned;
            }

            return null;
        }

        /// <summary>
        /// How many games sit under a launcher, groups included.
        /// </summary>
        public static int GetGameCount(
            AccordionControlElement launcherGroup)
        {
            if (launcherGroup == null)
                return 0;

            List<AccordionControlElement> items =
                new List<AccordionControlElement>();

            Collect(
                launcherGroup,
                items);

            return items.Count;
        }

        /// <summary>
        /// Every game under a launcher.
        /// </summary>
        public static List<GameInfo> GetGames(
            AccordionControlElement launcherGroup)
        {
            List<AccordionControlElement> items =
                new List<AccordionControlElement>();

            if (launcherGroup != null)
            {
                Collect(
                    launcherGroup,
                    items);
            }

            return items
                .Select(x => (GameInfo)x.Tag)
                .ToList();
        }

        /// <summary>
        /// Every launcher that has been populated, with its games,
        /// keyed on the display name the user sees in the accordion.
        /// This is the source of truth for library statistics: it
        /// includes Nexus entries, which LibraryService does not.
        /// </summary>
        public static Dictionary<string, List<GameInfo>> GetAllLaunchers()
        {
            Dictionary<string, List<GameInfo>> result =
                new Dictionary<string, List<GameInfo>>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<AccordionControlElement, string> pair in
                launcherNames.ToList())
            {
                result[pair.Value] =
                    GetGames(pair.Key);
            }

            return result;
        }

        /// <summary>
        /// Redraws every launcher that has been arranged, used when the
        /// skin changes so group headers pick up the new colours.
        /// </summary>
        public static void RearrangeAll()
        {
            foreach (AccordionControlElement launcherGroup in
                launcherNames.Keys.ToList())
            {
                Rearrange(launcherGroup);
            }
        }

        /// <summary>
        /// Finds the launcher branch holding a game, matching on the
        /// object itself rather than on its Launcher string: scanners
        /// disagree with the display names, EA being the obvious case.
        /// </summary>
        public static AccordionControlElement FindLauncherGroupFor(
            GameInfo game)
        {
            if (game == null)
                return null;

            foreach (AccordionControlElement launcherGroup in
                launcherNames.Keys.ToList())
            {
                List<AccordionControlElement> items =
                    new List<AccordionControlElement>();

                Collect(
                    launcherGroup,
                    items);

                if (items.Any(x => ReferenceEquals(x.Tag, game)))
                    return launcherGroup;
            }

            return null;
        }

        /// <summary>
        /// Redraws whichever launcher holds this game.
        /// </summary>
        public static void RearrangeFor(
            GameInfo game)
        {
            Rearrange(
                FindLauncherGroupFor(game));
        }

        /// <summary>
        /// Walks up to the launcher group that owns an element.
        /// </summary>
        public static AccordionControlElement GetLauncherGroup(
            AccordionControlElement element)
        {
            AccordionControlElement current =
                element;

            while (current != null &&
                current.OwnerElement != null)
            {
                current = current.OwnerElement;
            }

            return current;
        }

        public static string GetLauncherName(
            AccordionControlElement launcherGroup)
        {
            string launcher;

            if (launcherGroup != null &&
                launcherNames.TryGetValue(
                    launcherGroup,
                    out launcher))
            {
                return launcher;
            }

            return null;
        }

        private static void Collect(
            AccordionControlElement parent,
            List<AccordionControlElement> items)
        {
            foreach (AccordionControlElement child in parent.Elements)
            {
                if (child.Tag is GameInfo)
                {
                    items.Add(child);
                }
                else
                {
                    Collect(
                        child,
                        items);
                }
            }
        }

        private static void ApplyText(
            AccordionControlElement item,
            bool favorite)
        {
            GameInfo game =
                (GameInfo)item.Tag;

            // Always rebuilt from the game name so the star never
            // gets doubled up when a launcher is rearranged.
            item.Text =
                favorite
                    ? FavoriteGlyph + game.Name
                    : game.Name;
        }
    }
}
