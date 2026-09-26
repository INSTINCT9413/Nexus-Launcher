using DevExpress.Skins;
using DevExpress.Utils.Svg;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Nexus_Launcher.Services.Themes
{
    /// <summary>
    /// One theme as it appears in a theme file.
    ///
    /// Colours are a name to hex map rather than the internal list of
    /// name and ARGB integer, because these files are meant to be
    /// shared, read and hand edited. "#1E1F24" is something a person
    /// can work with; 1979711524 is not.
    /// </summary>
    public class ThemeFileEntry
    {
        public string Name { get; set; }

        /// <summary>
        /// The skin this palette was built for. A palette only means
        /// anything on its own skin, so this travels with it.
        /// </summary>
        public string Skin { get; set; }

        public string Author { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Colour name to hex, "#RRGGBB" or "#AARRGGBB".
        ///
        /// Need not be complete. Anything left out keeps the skin's own
        /// value, so a theme file can change three colours and say
        /// nothing about the other thirty.
        /// </summary>
        public Dictionary<string, string> Colors { get; set; }

        public ThemeFileEntry()
        {
            Colors =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// A theme file: one theme or a pack of them.
    /// </summary>
    public class ThemeFile
    {
        public int FormatVersion { get; set; }

        public string Generator { get; set; }

        public List<ThemeFileEntry> Themes { get; set; }

        public ThemeFile()
        {
            FormatVersion = ThemePackage.FormatVersion;

            Generator = "Nexus Launcher";

            Themes = new List<ThemeFileEntry>();
        }
    }

    /// <summary>
    /// What to do about a theme in the file whose name is already
    /// taken by one of the user's own.
    /// </summary>
    public enum ThemeImportMode
    {
        /// <summary>
        /// Bring it in under a free name, keeping both.
        /// </summary>
        KeepBoth = 0,

        /// <summary>
        /// Overwrite the existing theme of that name.
        /// </summary>
        Replace = 1
    }

    public class ThemeImportResult
    {
        public List<string> Imported { get; private set; }

        public List<string> Replaced { get; private set; }

        public List<string> Renamed { get; private set; }

        /// <summary>
        /// Themes that could not be brought in, each with the reason.
        /// </summary>
        public List<string> Skipped { get; private set; }

        public string Error { get; set; }

        public bool AnythingImported
        {
            get
            {
                return Imported.Count > 0;
            }
        }

        public ThemeImportResult()
        {
            Imported = new List<string>();
            Replaced = new List<string>();
            Renamed = new List<string>();
            Skipped = new List<string>();
        }
    }

    /// <summary>
    /// Reading and writing theme files, so themes can be shared
    /// between machines and people.
    ///
    /// Kept apart from CustomThemeService, which owns the user's own
    /// store: this is the exchange format, and the two should be free
    /// to change independently.
    /// </summary>
    internal static class ThemePackage
    {
        public const int FormatVersion = 1;

        public const string Extension = ".nexustheme";

        public const string Filter =
            "Nexus themes (*.nexustheme)|*.nexustheme|" +
            "JSON files (*.json)|*.json|" +
            "All files (*.*)|*.*";

        //--------------------------------------------------------------
        // Reading a file without applying it
        //--------------------------------------------------------------

        /// <summary>
        /// Parses a theme file. Returns null and sets error when the
        /// file is not one.
        /// </summary>
        public static ThemeFile Read(
            string path,
            out string error)
        {
            error = null;

            try
            {
                ThemeFile file =
                    JsonConvert.DeserializeObject<ThemeFile>(
                        File.ReadAllText(path));

                if (file == null || file.Themes == null)
                {
                    error = "That file does not contain any themes.";

                    return null;
                }

                // A newer file may use colours or fields this build
                // knows nothing about. Those are ignored rather than
                // refused, so an older Nexus can still use most of a
                // newer theme.
                file.Themes.RemoveAll(x => x == null);

                if (file.Themes.Count == 0)
                {
                    error = "That file does not contain any themes.";

                    return null;
                }

                return file;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                error =
                    "That file could not be read as a theme." +
                    Environment.NewLine + Environment.NewLine +
                    ex.Message;

                return null;
            }
        }

        //--------------------------------------------------------------
        // Export
        //--------------------------------------------------------------

        public static bool Export(
            CustomTheme theme,
            string path,
            out string error)
        {
            return Export(new[] { theme }, path, out error);
        }

        public static bool Export(
            IEnumerable<CustomTheme> themes,
            string path,
            out string error)
        {
            error = null;

            try
            {
                ThemeFile file =
                    new ThemeFile();

                foreach (CustomTheme theme in themes)
                {
                    if (theme == null)
                        continue;

                    ThemeFileEntry entry =
                        new ThemeFileEntry();

                    entry.Name = theme.Name;
                    entry.Skin = theme.SkinName;

                    foreach (CustomThemeColor color in theme.Colors)
                    {
                        if (string.IsNullOrEmpty(color.Name))
                            continue;

                        entry.Colors[color.Name] =
                            ToHex(Color.FromArgb(color.Argb));
                    }

                    file.Themes.Add(entry);
                }

                if (file.Themes.Count == 0)
                {
                    error = "There was nothing to export.";

                    return false;
                }

                File.WriteAllText(
                    path,
                    JsonConvert.SerializeObject(
                        file,
                        Formatting.Indented));

                return true;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                error =
                    "The theme could not be saved." +
                    Environment.NewLine + Environment.NewLine +
                    ex.Message;

                return false;
            }
        }

        //--------------------------------------------------------------
        // Import
        //--------------------------------------------------------------

        /// <summary>
        /// Whether any theme in the file would land on a name the user
        /// already has, so the caller can ask before overwriting.
        /// </summary>
        public static List<string> FindCollisions(
            ThemeFile file)
        {
            List<string> collisions =
                new List<string>();

            if (file == null || file.Themes == null)
                return collisions;

            foreach (ThemeFileEntry entry in file.Themes)
            {
                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.Name))
                {
                    continue;
                }

                if (CustomThemeService.Find(entry.Name) != null)
                    collisions.Add(entry.Name);
            }

            return collisions;
        }

        public static ThemeImportResult Import(
            ThemeFile file,
            ThemeImportMode mode)
        {
            ThemeImportResult result =
                new ThemeImportResult();

            if (file == null)
            {
                result.Error = "There was nothing to import.";

                return result;
            }

            foreach (ThemeFileEntry entry in file.Themes)
            {
                ImportOne(entry, mode, result);
            }

            return result;
        }

        private static void ImportOne(
            ThemeFileEntry entry,
            ThemeImportMode mode,
            ThemeImportResult result)
        {
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.Name))
            {
                result.Skipped.Add("A theme with no name was ignored.");

                return;
            }

            if (!CustomThemeService.IsPaletteCapable(entry.Skin))
            {
                result.Skipped.Add(
                    entry.Name + " - built for \"" +
                    (entry.Skin ?? "an unknown theme") +
                    "\", which cannot be recoloured.");

                return;
            }

            SvgPalette palette =
                BuildPalette(entry);

            if (palette == null)
            {
                result.Skipped.Add(
                    entry.Name + " - none of its colours are used by " +
                    entry.Skin + ".");

                return;
            }

            string name = entry.Name;

            bool existed =
                CustomThemeService.Find(name) != null;

            if (existed && mode == ThemeImportMode.KeepBoth)
            {
                name = FreeName(name);

                result.Renamed.Add(entry.Name + " -> " + name);
            }
            else if (existed)
            {
                result.Replaced.Add(name);
            }

            CustomThemeService.SaveTheme(
                name,
                entry.Skin,
                palette);

            result.Imported.Add(name);
        }

        /// <summary>
        /// Turns a file's colours into a palette for its skin.
        ///
        /// Built in the skin's own order, starting from the skin's own
        /// values, so a theme file only has to name the colours it
        /// wants to change and anything it does not mention keeps
        /// working. Names the skin does not have are dropped: they
        /// would render as nothing at all.
        /// </summary>
        private static SvgPalette BuildPalette(
            ThemeFileEntry entry)
        {
            SvgPalette source =
                DefaultPalette(entry.Skin);

            if (source == null)
                return null;

            SvgPalette palette =
                new SvgPalette();

            int matched = 0;

            foreach (SvgColor color in source.Colors)
            {
                Color value = color.Value;

                string hex;

                if (color.Name != null &&
                    entry.Colors.TryGetValue(color.Name, out hex))
                {
                    Color parsed;

                    if (TryParseHex(hex, out parsed))
                    {
                        value = parsed;

                        matched++;
                    }
                }

                palette.Colors.Add(
                    new SvgColor(color.Name, value));
            }

            return matched > 0 ? palette : null;
        }

        /// <summary>
        /// A skin's own palette, used as the starting point and for the
        /// colour order.
        /// </summary>
        private static SvgPalette DefaultPalette(
            string skinName)
        {
            try
            {
                SkinContainer container =
                    SkinManager.Default.Skins[skinName];

                if (container == null)
                    return null;

                Skin skin =
                    container.CommonSkin;

                if (skin == null || skin.SvgPalettes == null)
                    return null;

                SvgPalette palette;

                if (skin.SvgPalettes.TryGetValue(
                        new SvgPaletteKey(
                            0,
                            Skin.DefaultSkinPaletteName),
                        out palette))
                {
                    return palette;
                }

                return skin.SvgPalettes.Values.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return null;
            }
        }

        private static string FreeName(
            string name)
        {
            for (int i = 2; i < 1000; i++)
            {
                string candidate =
                    name + " (" + i + ")";

                if (CustomThemeService.Find(candidate) == null)
                    return candidate;
            }

            return name + " " + Guid.NewGuid().ToString("N").Substring(0, 6);
        }

        //--------------------------------------------------------------
        // Colours
        //--------------------------------------------------------------

        public static string ToHex(
            Color color)
        {
            return color.A == 255
                ? string.Format("#{0:X2}{1:X2}{2:X2}",
                    color.R, color.G, color.B)
                : string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}",
                    color.A, color.R, color.G, color.B);
        }

        /// <summary>
        /// Accepts #RGB, #RRGGBB and #AARRGGBB, with or without the
        /// hash, because these files get edited by hand.
        /// </summary>
        public static bool TryParseHex(
            string text,
            out Color color)
        {
            color = Color.Empty;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            string value =
                text.Trim().TrimStart('#');

            if (value.Length == 3)
            {
                value = string.Concat(
                    value[0], value[0],
                    value[1], value[1],
                    value[2], value[2]);
            }

            if (value.Length != 6 && value.Length != 8)
                return false;

            uint raw;

            if (!uint.TryParse(
                    value,
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out raw))
            {
                return false;
            }

            if (value.Length == 6)
                raw |= 0xFF000000;

            color = Color.FromArgb(unchecked((int)raw));

            return true;
        }
    }
}
