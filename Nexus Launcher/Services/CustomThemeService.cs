using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.Utils.Svg;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Nexus_Launcher.Services.Themes
{
    /// <summary>
    /// One colour in a user built theme. SvgColor itself is not
    /// serialisable, so it is stored as a name plus an ARGB value.
    /// </summary>
    public class CustomThemeColor
    {
        public string Name { get; set; }

        public int Argb { get; set; }
    }

    /// <summary>
    /// A user built theme: a set of colours applied on top of one of
    /// the vector skins.
    /// </summary>
    public class CustomTheme
    {
        public string Name { get; set; }

        /// <summary>
        /// The skin the palette was authored against. A palette only
        /// makes sense on the skin it was built for.
        /// </summary>
        public string SkinName { get; set; }

        public List<CustomThemeColor> Colors { get; set; }

        public CustomTheme()
        {
            Colors = new List<CustomThemeColor>();
        }
    }

    /// <summary>
    /// User built themes, stored as SVG skin palettes.
    ///
    /// DevExpress palettes only work on its vector skins. The bitmap
    /// skins draw from pre-rendered images, so there are no colours to
    /// swap and a palette applied to one silently does nothing. That is
    /// why the skin list has to be restricted while custom theming is
    /// on.
    /// </summary>
    internal static class CustomThemeService
    {
        private static readonly string StoreFile =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "NexusLauncher",
                "CustomThemes.json");

        /// <summary>
        /// The skins that can take a custom palette.
        ///
        /// This is a fixed list on purpose. DevExpress exposes no
        /// "is this a vector skin" API: SvgPaletteDescription
        /// .GetPaletteDescription falls back to the Basic description
        /// for every skin, so it cannot be used to tell them apart.
        /// The only runtime signal is that vector skins carry three
        /// built in palettes against a bitmap skin's two, which is too
        /// incidental to rely on. These are the skins DevExpress ships
        /// a palette description for.
        /// </summary>
        public static readonly string[] PaletteCapableSkins =
        {
            "Basic",
            "The Bezier",
            "WXI",
            "Office 2019 Colorful",
            "Office 2019 Black",
            "Office 2019 White",
            "Office 2019 Dark Gray",
            "High Contrast"
        };

        public static event Action Changed;

        private static List<CustomTheme> themes;

        //--------------------------------------------------------------
        // Capability
        //--------------------------------------------------------------

        public static bool IsPaletteCapable(
            string skinName)
        {
            if (string.IsNullOrWhiteSpace(skinName))
                return false;

            return PaletteCapableSkins.Contains(
                skinName,
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// A readable list for the "this theme cannot be used" message.
        /// </summary>
        public static string DescribeCapableSkins()
        {
            return string.Join(
                ", ",
                PaletteCapableSkins);
        }

        //--------------------------------------------------------------
        // Storage
        //--------------------------------------------------------------

        public static List<CustomTheme> Themes
        {
            get
            {
                if (themes == null)
                    themes = LoadFromDisk();

                return themes;
            }
        }

        private static List<CustomTheme> LoadFromDisk()
        {
            try
            {
                if (!File.Exists(StoreFile))
                    return new List<CustomTheme>();

                return JsonConvert.DeserializeObject<List<CustomTheme>>(
                    File.ReadAllText(StoreFile))
                    ?? new List<CustomTheme>();
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return new List<CustomTheme>();
            }
        }

        private static void Save()
        {
            try
            {
                string folder =
                    Path.GetDirectoryName(StoreFile);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                File.WriteAllText(
                    StoreFile,
                    JsonConvert.SerializeObject(
                        Themes,
                        Formatting.Indented));
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            Changed?.Invoke();
        }

        public static CustomTheme Find(
            string name)
        {
            return Themes.FirstOrDefault(x => string.Equals(
                x.Name,
                name,
                StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Stores a palette produced by the palette editor.
        /// </summary>
        public static CustomTheme SaveTheme(
            string name,
            string skinName,
            SvgPalette palette)
        {
            if (string.IsNullOrWhiteSpace(name) || palette == null)
                return null;

            CustomTheme theme =
                Find(name) ?? new CustomTheme();

            theme.Name = name;
            theme.SkinName = skinName;

            theme.Colors =
                palette.Colors
                    .Select(x => new CustomThemeColor
                    {
                        Name = x.Name,
                        Argb = x.Value.ToArgb()
                    })
                    .ToList();

            if (!Themes.Contains(theme))
                Themes.Add(theme);

            Save();

            Register(theme);

            return theme;
        }

        public static bool DeleteTheme(
            string name)
        {
            CustomTheme theme =
                Find(name);

            if (theme == null)
                return false;

            Themes.Remove(theme);

            // Removing it from the store is not enough. The palette is
            // also registered on the skin, and the palette list is
            // built from there, so a deleted theme kept showing up and
            // could not be edited or deleted again.
            Unregister(theme);

            Save();

            return true;
        }

        /// <summary>
        /// Takes a theme's palette back off its skin.
        /// </summary>
        public static void Unregister(
            CustomTheme theme)
        {
            if (theme == null ||
                string.IsNullOrWhiteSpace(theme.Name))
            {
                return;
            }

            try
            {
                Skin skin =
                    GetSkin(theme.SkinName);

                if (skin == null)
                    return;

                // Matched on name rather than by building a key, so it
                // still works if the index differs from the one used
                // when it was added.
                List<SvgPaletteKey> matches =
                    skin.CustomSvgPalettes.Keys
                        .Where(x => string.Equals(
                            x.Name,
                            theme.Name,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();

                foreach (SvgPaletteKey key in matches)
                {
                    skin.CustomSvgPalettes.Remove(key);
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        //--------------------------------------------------------------
        // Registration
        //--------------------------------------------------------------

        /// <summary>
        /// Puts every saved theme back into its skin so
        /// SetSkinStyle(skin, paletteName) can find it.
        ///
        /// Must run before anything applies the saved theme, which is
        /// why it is called from Program rather than a form: both
        /// WaitForm1 and MainView apply the theme on startup.
        /// </summary>
        public static void RegisterAll()
        {
            foreach (CustomTheme theme in Themes)
            {
                Register(theme);
            }
        }

        public static void Register(
            CustomTheme theme)
        {
            if (theme == null ||
                string.IsNullOrWhiteSpace(theme.Name) ||
                !IsPaletteCapable(theme.SkinName))
            {
                return;
            }

            try
            {
                Skin skin =
                    GetSkin(theme.SkinName);

                if (skin == null)
                    return;

                SvgPalette palette =
                    new SvgPalette();

                foreach (CustomThemeColor color in theme.Colors)
                {
                    palette.Colors.Add(
                        new SvgColor(
                            color.Name,
                            Color.FromArgb(color.Argb)));
                }

                SvgPaletteKey key =
                    new SvgPaletteKey(
                        0,
                        theme.Name);

                skin.CustomSvgPalettes[key] = palette;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        /// <summary>
        /// The skin object for a name, without disturbing the skin the
        /// user is currently looking at.
        /// </summary>
        private static Skin GetSkin(
            string skinName)
        {
            try
            {
                SkinContainer container =
                    SkinManager.Default.Skins[skinName];

                // CommonSkin rather than CommonSkins.GetSkin, which
                // only takes an ISkinProvider and would resolve the
                // active skin instead of the one asked for.
                return container == null
                    ? null
                    : container.CommonSkin;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return null;
            }
        }
    }
}
