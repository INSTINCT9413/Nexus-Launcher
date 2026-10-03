using DevExpress.Utils.Html;
using DevExpress.XtraEditors;
using Microsoft.Win32;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls
{
    /// <summary>
    /// The supported launchers step of the first time wizard.
    ///
    /// This replaces a page of hand placed picture boxes, labels and
    /// buttons that had to be kept in step by hand and left no room for
    /// an eighth launcher. The rows are generated from one list now, so
    /// adding a launcher is a single entry.
    ///
    /// Each row says whether the launcher was found and where. A
    /// launcher that is missing offers Install, which opens the
    /// publisher's own download page, and Locate, for an install that
    /// is somewhere other than the default folder.
    /// </summary>
    internal class SetupLauncherList : XtraUserControl
    {
        private readonly HtmlContentControl content =
            new HtmlContentControl();

        private readonly Scanner scanner =
            new Scanner();

        private readonly List<Target> targets =
            new List<Target>();

        /// <summary>
        /// One launcher: how to find it, where to get it, and where its
        /// path is kept once found.
        /// </summary>
        private sealed class Target
        {
            public string Key;

            public string Name;

            public Image Icon;

            /// <summary>Where it normally installs.</summary>
            public string Folder;

            /// <summary>
            /// The executable to look for, and the one the file picker
            /// insists on. Null for launchers that are not a plain exe,
            /// which currently means Xbox.
            /// </summary>
            public string Exe;

            /// <summary>The publisher's own download page.</summary>
            public string DownloadUrl;

            /// <summary>
            /// A direct link to the installer file, so Nexus can fetch
            /// and run it rather than sending the user to a web page.
            /// Null where there is no such file, which means Xbox: it
            /// is a Store package and only the Store can install it.
            /// </summary>
            public string InstallerUrl;

            /// <summary>What to call the downloaded installer.</summary>
            public string InstallerFile;

            /// <summary>
            /// Store the path where the rest of Nexus reads it. Null
            /// where there is no path to keep.
            /// </summary>
            public Action<string> Save;

            /// <summary>
            /// Overrides the folder search, for anything not found by
            /// looking for an exe.
            /// </summary>
            public Func<string> Detect;

            public string Path;

            public bool Found
            {
                get { return !string.IsNullOrEmpty(Path); }
            }

            public bool CanLocate
            {
                get { return Exe != null; }
            }
        }

        public SetupLauncherList()
        {
            content.Dock = DockStyle.Fill;

            content.ElementMouseClick += Content_ElementMouseClick;

            Controls.Add(content);

            BuildTargets();

            Detect();

            Render();
        }

        /// <summary>
        /// Whether every launcher was accounted for. The wizard can use
        /// this to decide whether the step needs the user's attention.
        /// </summary>
        public bool AllFound
        {
            get { return targets.All(t => t.Found); }
        }

        //--------------------------------------------------------------
        // What we look for
        //--------------------------------------------------------------

        private static string ProgramFilesX86
        {
            get
            {
                return Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFilesX86);
            }
        }

        private static string ProgramFiles
        {
            get
            {
                return Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFiles);
            }
        }

        /// <summary>
        /// The Xbox app is a Store package, so there is no exe to look
        /// for in Program Files. The shell registers this key when it
        /// is installed, which is what the launcher side of Nexus
        /// already checks.
        /// </summary>
        private static string DetectXbox()
        {
            try
            {
                using (RegistryKey key =
                    Registry.ClassesRoot.OpenSubKey("xbox"))
                {
                    return key == null
                        ? null
                        : "Installed from the Microsoft Store";
                }
            }
            catch
            {
                return null;
            }
        }

        private void BuildTargets()
        {
            // Paths come from the real Program Files locations rather
            // than a hard coded C:, which was wrong on any machine that
            // installs Windows elsewhere.
            targets.Add(new Target
            {
                Key = "steam",
                Name = "Steam",
                Icon = Resources.Steam25px,
                Folder = System.IO.Path.Combine(ProgramFilesX86, "Steam"),
                Exe = "steam.exe",
                DownloadUrl = "https://store.steampowered.com/about/",
                InstallerUrl = "https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe",
                InstallerFile = "SteamSetup.exe",
                Save = p => Settings.Default.steamPath = p
            });

            targets.Add(new Target
            {
                Key = "battlenet",
                Name = "Battle.net",
                Icon = Resources.Battle_net25px,
                Folder = System.IO.Path.Combine(ProgramFilesX86, "Battle.net"),
                Exe = "Battle.net.exe",
                DownloadUrl = "https://download.battle.net/",
                InstallerUrl = "https://downloader.battle.net/download/getInstaller?os=win&installer=Battle.net-Setup.exe",
                InstallerFile = "Battle.net-Setup.exe",
                Save = p => Settings.Default.battlenetPath = p
            });

            targets.Add(new Target
            {
                Key = "ea",
                Name = "EA app",
                Icon = Resources.EA25px,
                Folder = System.IO.Path.Combine(
                    ProgramFiles, @"Electronic Arts\EA Desktop\EA Desktop"),
                Exe = "EADesktop.exe",
                DownloadUrl = "https://www.ea.com/ea-app",
                InstallerUrl = "https://origin-a.akamaihd.net/EA-Desktop-Client-Download/installer-releases/EAappInstaller.exe",
                InstallerFile = "EAappInstaller.exe",
                Save = p => Settings.Default.eaPath = p
            });

            targets.Add(new Target
            {
                Key = "epic",
                Name = "Epic Games",
                Icon = Resources.EpicGames25px,
                Folder = System.IO.Path.Combine(
                    ProgramFilesX86, @"Epic Games\Launcher"),
                Exe = "EpicGamesLauncher.exe",
                // Not store.epicgames.com/download: that one bounces
                // through a sign in page before it will show the file.
                DownloadUrl = "https://www.epicgames.com/store/download",
                InstallerUrl = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi",
                InstallerFile = "EpicGamesLauncherInstaller.msi",
                Save = p => Settings.Default.epicPath = p
            });

            targets.Add(new Target
            {
                Key = "ubisoft",
                Name = "Ubisoft Connect",
                Icon = Resources.Ubisoft25px,
                Folder = System.IO.Path.Combine(
                    ProgramFilesX86, @"Ubisoft\Ubisoft Game Launcher"),
                Exe = "UbisoftConnect.exe",
                DownloadUrl = "https://ubisoftconnect.com/",
                InstallerUrl = "https://static3.cdn.ubi.com/orbit/launcher_installer/UbisoftConnectInstaller.exe",
                InstallerFile = "UbisoftConnectInstaller.exe",
                Save = p => Settings.Default.ubisoftPath = p
            });

            targets.Add(new Target
            {
                Key = "gog",
                Name = "GOG Galaxy",
                Icon = Resources.GOG25px,
                Folder = System.IO.Path.Combine(ProgramFilesX86, "GOG Galaxy"),
                Exe = "GalaxyClient.exe",
                DownloadUrl = "https://www.gog.com/galaxy",
                InstallerUrl = "https://webinstallers.gog-statics.com/download/GOG_Galaxy_2.0.exe",
                InstallerFile = "GOG_Galaxy_2.0.exe",
                Save = p => Settings.Default.gogPath = p
            });

            targets.Add(new Target
            {
                Key = "xbox",
                Name = "Xbox",
                Icon = Resources.Xbox_Emblem_300x300,
                DownloadUrl = "https://apps.microsoft.com/detail/9MV0B5HZVK9Z",
                Detect = DetectXbox
            });
        }

        private void Detect()
        {
            foreach (Target target in targets)
            {
                try
                {
                    target.Path = target.Detect != null
                        ? target.Detect()
                        : scanner.GetExe(target.Folder, target.Exe);
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);

                    target.Path = null;
                }

                if (target.Found && target.Save != null)
                    target.Save(target.Path);
            }
        }

        //--------------------------------------------------------------
        // Drawing
        //--------------------------------------------------------------

        private static string Rgba(
            Color color,
            double alpha)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "rgba({0}, {1}, {2}, {3:0.###})",
                color.R,
                color.G,
                color.B,
                alpha);
        }

        private static string Hex(
            Color color)
        {
            return string.Format(
                "#{0:X2}{1:X2}{2:X2}",
                color.R,
                color.G,
                color.B);
        }

        private static string Escape(
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("'", "&#39;");
        }

        /// <summary>
        /// Colours come from ProfileStyle rather than the DevExpress
        /// @Name tokens: those read the skin's colour table, where
        /// @Text is black under every custom theme, which would leave
        /// this unreadable on the dark ones.
        /// </summary>
        private string BuildStyles()
        {
            Color text = ProfileStyle.TextColor;
            Color muted = ProfileStyle.MutedTextColor;
            Color card = ProfileStyle.CardColor;
            Color accent = ProfileStyle.AccentColor;

            Color onAccent =
                ProfileStyle.Luminance(accent) < 150
                    ? Color.White
                    : Color.FromArgb(20, 20, 20);

            Color ok = Color.FromArgb(46, 160, 87);
            Color missing = ProfileStyle.IsDarkTheme
                ? Color.FromArgb(230, 130, 60)
                : Color.FromArgb(176, 92, 20);

            StringBuilder css = new StringBuilder();

            css.AppendLine("body { padding: 0px; margin: 0px; }");

            css.AppendLine(".list { display: flex; flex-direction: column; }");

            css.AppendLine(".row {");
            css.AppendLine("    display: flex;");
            css.AppendLine("    flex-direction: row;");
            css.AppendLine("    align-items: center;");
            css.AppendLine("    height: 54px;");
            css.AppendLine("    padding: 0px 10px 0px 10px;");
            css.AppendLine("    border-radius: 6px;");
            css.AppendLine("}");

            css.AppendLine(".row.alt { background-color: " + Rgba(text, 0.04) + "; }");

            css.AppendLine(".icon { width: 40px; height: 40px; }");

            css.AppendLine(".names {");
            css.AppendLine("    display: flex;");
            css.AppendLine("    flex-direction: column;");
            css.AppendLine("    flex-grow: 1;");
            css.AppendLine("    padding-left: 12px;");
            css.AppendLine("}");

            css.AppendLine(".name { font-size: 12px; font-weight: bold; color: " + Hex(text) + "; }");

            css.AppendLine(".path { font-size: 10px; color: " + Hex(muted) + "; }");

            css.AppendLine(".status {");
            css.AppendLine("    font-size: 10px;");
            css.AppendLine("    font-weight: bold;");
            css.AppendLine("    width: 76px;");
            css.AppendLine("}");

            css.AppendLine(".status.ok { color: " + Hex(ok) + "; }");

            css.AppendLine(".status.missing { color: " + Hex(missing) + "; }");

            css.AppendLine(".btn {");
            css.AppendLine("    height: 26px;");
            css.AppendLine("    padding: 0px 12px 0px 12px;");
            css.AppendLine("    margin-left: 6px;");
            css.AppendLine("    border-radius: 4px;");
            css.AppendLine("    font-size: 11px;");
            css.AppendLine("    display: flex;");
            css.AppendLine("    align-items: center;");
            css.AppendLine("    justify-content: center;");
            css.AppendLine("    color: " + Hex(text) + ";");
            css.AppendLine("    background-color: " + Rgba(text, 0.10) + ";");
            css.AppendLine("}");

            css.AppendLine(".btn:hover { background-color: " + Rgba(text, 0.18) + "; }");

            css.AppendLine(".btn.primary {");
            css.AppendLine("    background-color: " + Hex(accent) + ";");
            css.AppendLine("    color: " + Hex(onAccent) + ";");
            css.AppendLine("    font-weight: bold;");
            css.AppendLine("}");

            css.AppendLine(".btn.primary:hover { background-color: " +
                Hex(ProfileStyle.Shift(accent, 18)) + "; }");

            css.AppendLine(".footer {");
            css.AppendLine("    display: flex;");
            css.AppendLine("    flex-direction: row;");
            css.AppendLine("    align-items: center;");
            css.AppendLine("    padding: 4px 10px 0px 10px;");
            css.AppendLine("}");

            css.AppendLine(".summary {");
            css.AppendLine("    flex-grow: 1;");
            css.AppendLine("    font-size: 11px;");
            css.AppendLine("    color: " + Hex(muted) + ";");
            css.AppendLine("}");

            return css.ToString();
        }

        private string BuildTemplate()
        {
            StringBuilder html = new StringBuilder();

            html.AppendLine("<div class='list'>");

            for (int i = 0; i < targets.Count; i++)
            {
                Target target = targets[i];

                html.AppendLine("<div class='row" + (i % 2 == 1 ? " alt" : "") + "'>");

                html.AppendLine("    <img class='icon' src='" + target.Key + "-icon' />");

                html.AppendLine("    <div class='names'>");
                html.AppendLine("        <div class='name'>" + Escape(target.Name) + "</div>");
                html.AppendLine("        <div class='path'>" +
                    Escape(target.Found ? target.Path : "Not installed on this PC") +
                    "</div>");
                html.AppendLine("    </div>");

                html.AppendLine("    <div class='status " +
                    (target.Found ? "ok'>Found" : "missing'>Not found") +
                    "</div>");

                if (!target.Found)
                {
                    html.AppendLine("    <div class='btn primary' id='install:" +
                        target.Key + "'>Install</div>");
                }

                if (target.CanLocate)
                {
                    html.AppendLine("    <div class='btn' id='locate:" + target.Key + "'>" +
                        (target.Found ? "Change" : "Locate") + "</div>");
                }

                html.AppendLine("</div>");
            }

            html.AppendLine("</div>");

            int missing = targets.Count(t => !t.Found);

            html.AppendLine("<div class='footer'>");

            html.AppendLine("    <div class='summary'>" +
                (missing == 0
                    ? "All supported launchers were found."
                    : missing + " of " + targets.Count +
                      " not found. Install adds it from the publisher, " +
                      "Locate points Nexus at an existing install. " +
                      "You can skip this and set them up later.") +
                "</div>");

            // Install opens a browser, so the user comes back to this
            // page having changed the answer. Without this they would
            // have to restart the wizard for Nexus to notice.
            if (missing > 0)
            {
                html.AppendLine("    <div class='btn' id='rescan:all'>" +
                    "Check again</div>");
            }

            html.AppendLine("</div>");

            return html.ToString();
        }

        private DevExpress.Utils.ImageCollection BuildImages()
        {
            DevExpress.Utils.ImageCollection images =
                new DevExpress.Utils.ImageCollection();

            images.ImageSize = new Size(40, 40);

            foreach (Target target in targets)
            {
                if (target.Icon != null)
                    images.AddImage(target.Icon, target.Key + "-icon");
            }

            return images;
        }

        private void Render()
        {
            content.HtmlImages = BuildImages();

            content.HtmlTemplate.Styles = BuildStyles();

            content.HtmlTemplate.Template = BuildTemplate();
        }

        //--------------------------------------------------------------
        // Actions
        //--------------------------------------------------------------

        private void Content_ElementMouseClick(
            object sender,
            DxHtmlElementMouseEventArgs e)
        {
            HandleAction(e.ElementId);
        }

        /// <summary>
        /// Routes a clicked element id such as "install:steam".
        ///
        /// Split out from the click handler so the routing can be
        /// exercised without a rendered page: Check again was broken
        /// here and nothing but clicking it would have shown that.
        /// </summary>
        internal void HandleAction(
            string elementId)
        {
            if (elementId == null)
                return;

            int split = elementId.IndexOf(':');

            if (split <= 0)
                return;

            string action = elementId.Substring(0, split);

            string key = elementId.Substring(split + 1);

            // Check again is about the whole list, so it is handled
            // before looking for a launcher: its id carries no key,
            // and the lookup below would find nothing and give up.
            if (action == "rescan")
            {
                Rescan();

                return;
            }

            Target target =
                targets.FirstOrDefault(t => t.Key == key);

            if (target == null)
                return;

            if (action == "install")
                Install(target);
            else if (action == "locate")
                Locate(target);
        }

        /// <summary>
        /// Fetches the installer and starts it.
        ///
        /// Xbox has no installer to fetch, so that one still opens its
        /// Store page, which is the only way to install it.
        /// </summary>
        private void Install(
            Target target)
        {
            if (target.InstallerUrl == null)
            {
                OpenPage(target);

                return;
            }

            try
            {
                using (Forms.LauncherInstallForm download =
                    new Forms.LauncherInstallForm(
                        target.Name,
                        target.InstallerUrl,
                        target.InstallerFile))
                {
                    download.ShowDialog(this);

                    if (!download.Started)
                        return;
                }

                // The installer runs on its own, so there is nothing to
                // wait for. Check again picks the launcher up whenever
                // the user is done with it.
                XtraMessageBox.Show(
                    this,
                    "The " + target.Name + " installer is running." +
                        Environment.NewLine + Environment.NewLine +
                        "When it has finished, choose Check again and " +
                        "Nexus will look for it.",
                    target.Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                OpenPage(target);
            }
        }

        /// <summary>
        /// The fallback, and the only route for Xbox: the publisher's
        /// own page in the browser.
        /// </summary>
        private void OpenPage(
            Target target)
        {
            try
            {
                System.Diagnostics.Process.Start(target.DownloadUrl);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                XtraMessageBox.Show(
                    this,
                    "The download page could not be opened." +
                        Environment.NewLine + Environment.NewLine +
                        target.DownloadUrl,
                    target.Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void Locate(
            Target target)
        {
            string chosen =
                scanner.BrowseForExe(target.Exe);

            if (string.IsNullOrEmpty(chosen))
                return;

            // The picker is filtered, but a determined user can still
            // type a name, so the choice is checked rather than
            // trusted.
            if (!chosen.EndsWith(
                    target.Exe,
                    StringComparison.OrdinalIgnoreCase))
            {
                XtraMessageBox.Show(
                    this,
                    "That is not " + target.Exe + "." +
                        Environment.NewLine + Environment.NewLine +
                        "Pick the " + target.Exe + " inside your " +
                        target.Name + " folder.",
                    target.Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            target.Path = chosen;

            if (target.Save != null)
                target.Save(chosen);

            Render();
        }

        /// <summary>
        /// Looks again for anything still missing, so a launcher just
        /// installed through the Install button is picked up without
        /// restarting the wizard.
        ///
        /// Not called Refresh: that is already a Control method taking
        /// no arguments, and an overload beside it is too easy to call
        /// by mistake.
        /// </summary>
        public void Rescan()
        {
            foreach (Target target in targets)
            {
                if (target.Found)
                    continue;

                try
                {
                    target.Path = target.Detect != null
                        ? target.Detect()
                        : scanner.GetExe(target.Folder, target.Exe);
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                }

                if (target.Found && target.Save != null)
                    target.Save(target.Path);
            }

            Render();
        }
    }
}
