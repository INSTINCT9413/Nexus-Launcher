using DevExpress.LookAndFeel;
using DevExpress.XtraEditors;
using Nexus_Launcher.Services.Themes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Forms
{
    /// <summary>
    /// Importing a theme file, with the questions that have to be put
    /// to the user along the way.
    ///
    /// Shared rather than living in the settings window, because a
    /// theme now arrives two ways: from the Import Theme menu, and
    /// from double clicking a .nexustheme file in Explorer. Both should
    /// ask the same things and say the same things.
    /// </summary>
    internal static class ThemeImportUI
    {
        /// <summary>
        /// Raised after anything is imported, so an open settings
        /// window can refresh its palette list.
        /// </summary>
        public static event Action Imported;

        /// <summary>
        /// Imports one or more files. Returns true if anything landed.
        /// </summary>
        public static bool ImportFiles(
            IWin32Window owner,
            IEnumerable<string> paths)
        {
            if (paths == null)
                return false;

            bool any = false;

            foreach (string path in paths.ToList())
            {
                if (ImportFile(owner, path))
                    any = true;
            }

            if (any)
            {
                Action handler = Imported;

                if (handler != null)
                    handler();
            }

            return any;
        }

        private static bool ImportFile(
            IWin32Window owner,
            string path)
        {
            string error;

            ThemeFile file =
                ThemePackage.Read(path, out error);

            if (file == null)
            {
                XtraMessageBox.Show(
                    owner,
                    SafeName(path) +
                        Environment.NewLine + Environment.NewLine +
                        error,
                    "Import Theme",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return false;
            }

            ThemeImportMode mode =
                ThemeImportMode.KeepBoth;

            List<string> collisions =
                ThemePackage.FindCollisions(file);

            if (collisions.Count > 0)
            {
                // Overwriting someone's own theme because a file
                // happened to share its name is not a decision to make
                // for them.
                DialogResult answer =
                    XtraMessageBox.Show(
                        owner,
                        "You already have a theme called " +
                            string.Join(", ", collisions.ToArray()) +
                            "." + Environment.NewLine +
                            Environment.NewLine +
                            "Replace what you have, or keep both?" +
                            Environment.NewLine +
                            Environment.NewLine +
                            "Yes  -  replace" + Environment.NewLine +
                            "No   -  keep both" + Environment.NewLine +
                            "Cancel  -  import nothing",
                        "Import Theme",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                if (answer == DialogResult.Cancel)
                    return false;

                if (answer == DialogResult.Yes)
                    mode = ThemeImportMode.Replace;
            }

            ThemeImportResult result =
                ThemePackage.Import(file, mode);

            Report(owner, path, result);

            if (result.AnythingImported)
                OfferToApply(owner, file, result);

            return result.AnythingImported;
        }

        /// <summary>
        /// A theme that has just been imported is almost certainly
        /// meant to be used, so it is offered rather than left sitting
        /// in a list the user then has to go and find.
        ///
        /// Only for a single theme: a pack of ten has no obvious one to
        /// switch to.
        /// </summary>
        private static void OfferToApply(
            IWin32Window owner,
            ThemeFile file,
            ThemeImportResult result)
        {
            if (result.Imported.Count != 1)
                return;

            string name =
                result.Imported[0];

            CustomTheme theme =
                CustomThemeService.Find(name);

            if (theme == null)
                return;

            bool alreadyOn =
                string.Equals(
                    UserLookAndFeel.Default.SkinName,
                    theme.SkinName,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    UserLookAndFeel.Default.ActiveSvgPaletteName,
                    name,
                    StringComparison.OrdinalIgnoreCase);

            if (alreadyOn)
                return;

            string question =
                "Use \"" + name + "\" now?";

            if (!string.Equals(
                    UserLookAndFeel.Default.SkinName,
                    theme.SkinName,
                    StringComparison.OrdinalIgnoreCase))
            {
                // Switching skin as well is a bigger change than
                // switching palette, so it is spelled out.
                question +=
                    Environment.NewLine + Environment.NewLine +
                    "This theme is built for \"" + theme.SkinName +
                    "\", so Nexus will switch to that theme as well.";
            }

            if (XtraMessageBox.Show(
                    owner,
                    question,
                    "Import Theme",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            Apply(theme);
        }

        public static void Apply(
            CustomTheme theme)
        {
            try
            {
                UserLookAndFeel.Default.SetSkinStyle(
                    theme.SkinName,
                    theme.Name);

                ThemeSettingsManager.Save(
                    theme.SkinName,
                    theme.Name);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        private static void Report(
            IWin32Window owner,
            string path,
            ThemeImportResult result)
        {
            List<string> lines =
                new List<string>();

            if (result.Imported.Count > 0)
            {
                lines.Add(
                    "Imported: " +
                    string.Join(", ", result.Imported.ToArray()));
            }

            if (result.Renamed.Count > 0)
            {
                lines.Add(
                    "Renamed to keep both: " +
                    string.Join(", ", result.Renamed.ToArray()));
            }

            foreach (string skipped in result.Skipped)
            {
                lines.Add("Not imported: " + skipped);
            }

            if (!string.IsNullOrEmpty(result.Error))
                lines.Add(result.Error);

            if (lines.Count == 0)
                return;

            // A single clean import goes straight to the "use it now?"
            // question instead of stopping to announce itself first.
            if (result.AnythingImported &&
                result.Imported.Count == 1 &&
                result.Renamed.Count == 0 &&
                result.Skipped.Count == 0)
            {
                return;
            }

            if (result.AnythingImported)
            {
                lines.Add(string.Empty);

                lines.Add(
                    "Pick a theme from the palette list to use it.");
            }

            XtraMessageBox.Show(
                owner,
                SafeName(path) +
                    Environment.NewLine + Environment.NewLine +
                    string.Join(Environment.NewLine, lines.ToArray()),
                "Import Theme",
                MessageBoxButtons.OK,
                result.AnythingImported
                    ? MessageBoxIcon.Information
                    : MessageBoxIcon.Warning);
        }

        private static string SafeName(
            string path)
        {
            try
            {
                return Path.GetFileName(path);
            }
            catch (Exception)
            {
                return path;
            }
        }
    }
}
