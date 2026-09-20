using DevExpress.Images;
using DevExpress.Utils.Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Nexus_Launcher.Services.Library
{
    /// <summary>
    /// The icons a user can pick for one of their groups.
    ///
    /// These are DevExpress SVG glyphs, which is deliberate: they are
    /// recoloured to whichever skin is active, so a group icon stays
    /// visible on both light and dark themes.
    /// </summary>
    internal static class LibraryGroupIcons
    {
        /// <summary>
        /// Offered in the picker, in the order they are shown.
        /// The %20 is part of the resource key, not a typo.
        /// </summary>
        public static readonly string[] Keys =
        {
            "svgimages/icon%20builder/actions_rating.svg",
            "svgimages/icon%20builder/shopping_favorites.svg",
            "svgimages/icon%20builder/actions_flag.svg",
            "svgimages/icon%20builder/actions_bookmark.svg",
            "svgimages/icon%20builder/actions_label.svg",
            "svgimages/icon%20builder/actions_folderclose.svg",
            "svgimages/icon%20builder/actions_folderopen.svg",
            "svgimages/icon%20builder/actions_list.svg",
            "svgimages/icon%20builder/actions_check.svg",
            "svgimages/icon%20builder/actions_clock.svg",

            "svgimages/icon%20builder/actions_calendar.svg",
            "svgimages/icon%20builder/actions_user.svg",
            "svgimages/icon%20builder/actions_home.svg",
            "svgimages/icon%20builder/actions_book.svg",
            "svgimages/icon%20builder/actions_settings.svg",
            "svgimages/icon%20builder/business_target.svg",
            "svgimages/icon%20builder/business_world.svg",
            "svgimages/icon%20builder/business_idea.svg",
            "svgimages/icon%20builder/business_briefcase.svg",
            "svgimages/icon%20builder/business_barchart.svg",

            "svgimages/icon%20builder/security_key.svg",
            "svgimages/icon%20builder/security_lock.svg",
            "svgimages/icon%20builder/security_bug.svg",
            "svgimages/icon%20builder/security_security.svg",
            "svgimages/icon%20builder/security_fingerprint.svg",
            "svgimages/icon%20builder/electronics_headphone.svg",
            "svgimages/icon%20builder/electronics_desktopwindows.svg",
            "svgimages/icon%20builder/electronics_keyboard.svg",
            "svgimages/icon%20builder/electronics_mouse.svg",
            "svgimages/icon%20builder/electronics_tv.svg",

            "svgimages/icon%20builder/electronics_video.svg",
            "svgimages/icon%20builder/electronics_photo.svg",
            "svgimages/icon%20builder/travel_car.svg",
            "svgimages/icon%20builder/travel_plane.svg",
            "svgimages/icon%20builder/travel_ship.svg",
            "svgimages/icon%20builder/travel_map.svg",
            "svgimages/icon%20builder/travel_mountains.svg",
            "svgimages/icon%20builder/travel_forest.svg",
            "svgimages/icon%20builder/travel_anchor.svg",
            "svgimages/icon%20builder/travel_walk.svg",

            "svgimages/icon%20builder/travel_camping.svg",
            "svgimages/icon%20builder/travel_beach.svg",
            "svgimages/icon%20builder/weather_moon.svg",
            "svgimages/icon%20builder/weather_sunny.svg",
            "svgimages/icon%20builder/weather_lightning.svg",
            "svgimages/icon%20builder/weather_snow.svg",
            "svgimages/icon%20builder/shopping_gift.svg",
            "svgimages/icon%20builder/shopping_box.svg",
            "svgimages/icon%20builder/shopping_store.svg",
            "svgimages/icon%20builder/shopping_shoppingcart.svg"
        };

        public const string DefaultKey =
            "svgimages/icon%20builder/actions_folderclose.svg";

        //----------------------------------------------------------
        // Context menu glyphs
        //----------------------------------------------------------

        public const string MenuFavorite =
            "svgimages/icon%20builder/actions_rating.svg";

        public const string MenuMoveToGroup =
            "svgimages/icon%20builder/actions_folderopen.svg";

        public const string MenuNewGroup =
            "svgimages/icon%20builder/actions_add.svg";

        public const string MenuEditGroup =
            "svgimages/icon%20builder/actions_edit.svg";

        public const string MenuDeleteGroup =
            "svgimages/icon%20builder/actions_trash.svg";

        public const string MenuMoveUp =
            "svgimages/icon%20builder/actions_arrow1up.svg";

        public const string MenuMoveDown =
            "svgimages/icon%20builder/actions_arrow1down.svg";

        public const string MenuUngrouped =
            "svgimages/icon%20builder/actions_clear.svg";

        private static readonly Dictionary<string, SvgImage> cache =
            new Dictionary<string, SvgImage>();

        public static SvgImage GetSvgImage(
            string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            SvgImage image;

            if (cache.TryGetValue(
                key,
                out image))
            {
                return image;
            }

            try
            {
                image =
                    ImageResourceCache.Default.GetSvgImage(key);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                image = null;
            }

            cache[key] = image;

            return image;
        }

        /// <summary>
        /// Matches the icon size the scanners use for game items, so a
        /// group header ends up the same height as the games in it.
        /// </summary>
        public static readonly Size IconSize =
            new Size(32, 32);

        /// <summary>
        /// Loads a user supplied icon without keeping the file locked.
        /// </summary>
        public static Image GetCustomImage(
            string path)
        {
            return GetCustomImage(
                path,
                IconSize);
        }

        public static Image GetCustomImage(
            string path,
            Size size)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                !File.Exists(path))
            {
                return null;
            }

            try
            {
                using (Bitmap source = new Bitmap(path))
                {
                    return new Bitmap(
                        source,
                        size);
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return null;
            }
        }
    }
}
