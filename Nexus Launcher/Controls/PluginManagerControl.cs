using DevExpress.Utils.Html;
using DevExpress.XtraEditors;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Services.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls
{
    /// <summary>
    /// The Plugins page in Settings.
    ///
    /// Lists what is installed, what each one contributes, and what
    /// went wrong with the ones that failed. A plugin that throws is
    /// deliberately still listed, with its error, because a plugin
    /// that silently disappears is the worst possible outcome for
    /// whoever is trying to write one.
    /// </summary>
    internal class PluginManagerControl : XtraUserControl
    {
        private readonly HtmlContentControl content =
            new HtmlContentControl();

        public PluginManagerControl()
        {
            content.Dock = DockStyle.Fill;

            content.ElementMouseClick += Content_ElementMouseClick;

            Controls.Add(content);

            Render();
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

        private string BuildStyles()
        {
            Color text = ProfileStyle.TextColor;
            Color muted = ProfileStyle.MutedTextColor;
            Color accent = ProfileStyle.AccentColor;

            Color onAccent =
                ProfileStyle.Luminance(accent) < 150
                    ? Color.White
                    : Color.FromArgb(20, 20, 20);

            Color ok = Color.FromArgb(46, 160, 87);

            Color bad = ProfileStyle.IsDarkTheme
                ? Color.FromArgb(235, 105, 95)
                : Color.FromArgb(178, 40, 30);

            StringBuilder css = new StringBuilder();

            css.AppendLine("body { padding: 8px; margin: 0px; }");

            css.AppendLine(".head { display: flex; flex-direction: row; align-items: center; padding: 0px 4px 10px 4px; }");
            css.AppendLine(".headtext { flex-grow: 1; }");
            css.AppendLine(".h1 { font-size: 13px; font-weight: bold; color: " + Hex(text) + "; }");
            css.AppendLine(".h2 { font-size: 10px; color: " + Hex(muted) + "; }");

            css.AppendLine(".row {");
            css.AppendLine("    display: flex;");
            css.AppendLine("    flex-direction: row;");
            css.AppendLine("    align-items: center;");
            css.AppendLine("    padding: 10px;");
            css.AppendLine("    margin-bottom: 6px;");
            css.AppendLine("    border-radius: 6px;");
            css.AppendLine("    background-color: " + Rgba(text, 0.05) + ";");
            css.AppendLine("}");

            css.AppendLine(".info { display: flex; flex-direction: column; flex-grow: 1; }");
            css.AppendLine(".name { font-size: 12px; font-weight: bold; color: " + Hex(text) + "; }");
            css.AppendLine(".by { font-size: 10px; color: " + Hex(muted) + "; }");
            css.AppendLine(".desc { font-size: 10px; color: " + Hex(muted) + "; padding-top: 2px; }");
            css.AppendLine(".gives { font-size: 10px; color: " + Hex(accent) + "; padding-top: 2px; }");
            css.AppendLine(".err { font-size: 10px; color: " + Hex(bad) + "; padding-top: 2px; }");

            css.AppendLine(".state { font-size: 10px; font-weight: bold; width: 66px; }");
            css.AppendLine(".state.on { color: " + Hex(ok) + "; }");
            css.AppendLine(".state.off { color: " + Hex(muted) + "; }");
            css.AppendLine(".state.bad { color: " + Hex(bad) + "; }");

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
            css.AppendLine(".btn.primary { background-color: " + Hex(accent) +
                "; color: " + Hex(onAccent) + "; font-weight: bold; }");

            css.AppendLine(".sources { padding: 6px 0px 0px 0px; }");
            css.AppendLine(".source { display: flex; flex-direction: row; align-items: center; padding: 3px 0px 3px 0px; }");
            css.AppendLine(".srcname { font-size: 10px; color: " + Hex(muted) + "; flex-grow: 1; }");
            css.AppendLine(".small {");
            css.AppendLine("    height: 20px;");
            css.AppendLine("    padding: 0px 8px 0px 8px;");
            css.AppendLine("    margin-left: 6px;");
            css.AppendLine("    border-radius: 3px;");
            css.AppendLine("    font-size: 10px;");
            css.AppendLine("    display: flex;");
            css.AppendLine("    align-items: center;");
            css.AppendLine("    color: " + Hex(text) + ";");
            css.AppendLine("    background-color: " + Rgba(text, 0.10) + ";");
            css.AppendLine("}");
            css.AppendLine(".small:hover { background-color: " + Rgba(text, 0.18) + "; }");

            css.AppendLine(".empty { font-size: 11px; color: " + Hex(muted) +
                "; padding: 20px 6px 6px 6px; }");

            css.AppendLine(".note { font-size: 10px; color: " + Hex(muted) +
                "; padding: 8px 4px 0px 4px; }");

            return css.ToString();
        }

        private string BuildTemplate()
        {
            List<PluginRecord> all =
                PluginService.All.OrderBy(p => p.Name).ToList();

            StringBuilder html = new StringBuilder();

            html.AppendLine("<div class='head'>");
            html.AppendLine("  <div class='headtext'>");
            html.AppendLine("    <div class='h1'>Plugins</div>");
            html.AppendLine("    <div class='h2'>" +
                (all.Count == 0
                    ? "None installed"
                    : all.Count + " installed") +
                "</div>");
            html.AppendLine("  </div>");
            html.AppendLine("  <div class='btn primary' id='add'>Add plugin</div>");
            html.AppendLine("  <div class='btn' id='folder'>Open plugins folder</div>");
            html.AppendLine("</div>");

            if (all.Count == 0)
            {
                html.AppendLine("<div class='empty'>" +
                    "Nothing here yet. Drop a plugin's .dll into the " +
                    "plugins folder and restart Nexus, and it will " +
                    "appear in this list." +
                    "</div>");

                return html.ToString();
            }

            foreach (PluginRecord plugin in all)
            {
                html.AppendLine("<div class='row'>");

                html.AppendLine("  <div class='info'>");

                html.AppendLine("    <div class='name'>" +
                    Escape(plugin.Name) +
                    (plugin.IsBuiltIn ? "  (built in)" : string.Empty) +
                    "</div>");

                html.AppendLine("    <div class='by'>" +
                    Escape(string.IsNullOrWhiteSpace(plugin.Author)
                        ? "Unknown author"
                        : plugin.Author) +
                    (string.IsNullOrWhiteSpace(plugin.Version)
                        ? string.Empty
                        : "  ·  v" + Escape(plugin.Version)) +
                    "</div>");

                if (!string.IsNullOrWhiteSpace(plugin.Description))
                {
                    html.AppendLine("    <div class='desc'>" +
                        Escape(plugin.Description) + "</div>");
                }

                // Only the error is worth saying here. What a
                // plugin contributes, and anything the user can
                // change about it, lives behind its Settings button:
                // this page is the list of what is installed and
                // whether it is on.
                if (plugin.Error != null)
                {
                    html.AppendLine("    <div class='err'>" +
                        Escape(plugin.Error) + "</div>");
                }

                html.AppendLine("  </div>");

                string state = plugin.Error != null
                    ? "bad'>Error"
                    : plugin.Enabled ? "on'>Enabled" : "off'>Disabled";

                html.AppendLine("  <div class='state " + state + "</div>");

                if (plugin.Enabled && plugin.Error == null &&
                    HasSettings(plugin))
                {
                    html.AppendLine("  <div class='btn' id='settings:" +
                        Escape(plugin.Id) + "'>Settings</div>");
                }

                html.AppendLine("  <div class='btn" +
                    (plugin.Enabled ? string.Empty : " primary") +
                    "' id='toggle:" + Escape(plugin.Id) + "'>" +
                    (plugin.Enabled ? "Disable" : "Enable") +
                    "</div>");

                html.AppendLine("</div>");
            }

            html.AppendLine("<div class='note'>" +
                "Enabling or disabling takes full effect when Nexus " +
                "next starts. Plugins run inside Nexus: only install " +
                "ones you trust." +
                "</div>");

            return html.ToString();
        }

        private void Render()
        {
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
            string id = e.ElementId;

            if (id == null)
                return;

            if (id == "folder")
            {
                OpenFolder();

                return;
            }

            if (id == "add")
            {
                AddPlugin();

                return;
            }

            if (id.StartsWith("settings:"))
            {
                ShowSettings(id.Substring("settings:".Length));

                return;
            }

            if (!id.StartsWith("toggle:"))
                return;

            PluginRecord plugin =
                Find(id.Substring("toggle:".Length));

            if (plugin == null)
                return;

            PluginService.SetEnabled(plugin, !plugin.Enabled);

            Render();
        }

        private static PluginRecord Find(
            string pluginId)
        {
            return PluginService.All.FirstOrDefault(p =>
                string.Equals(p.Id, pluginId,
                    StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Whether the plugin has a settings dialog worth opening:
        /// its own settings, its own buttons, or library sources the
        /// user can show and hide.
        /// </summary>
        private static bool HasSettings(
            PluginRecord plugin)
        {
            return plugin.Instance is Nexus.Plugin.IPluginSettings ||
                   plugin.Instance is Nexus.Plugin.IPluginCommands ||
                   PluginService.Sources.Any(s => s.Owner == plugin);
        }

        /// <summary>
        /// Copies a plugin into the plugins folder.
        ///
        /// Saves the user finding the folder, and copies rather than
        /// moves, so dropping a file in by hand still works and the
        /// original is left where it was.
        /// </summary>
        private void AddPlugin()
        {
            using (OpenFileDialog picker = new OpenFileDialog())
            {
                picker.Title = "Add a plugin";
                picker.Filter = "Nexus plugin (*.dll)|*.dll";
                picker.Multiselect = true;

                if (picker.ShowDialog(this) != DialogResult.OK)
                    return;

                int copied = 0;

                foreach (string file in picker.FileNames)
                {
                    try
                    {
                        Directory.CreateDirectory(PluginService.UserFolder);

                        string target = Path.Combine(
                            PluginService.UserFolder,
                            Path.GetFileName(file));

                        File.Copy(file, target, true);

                        copied++;
                    }
                    catch (Exception ex)
                    {
                        Program.LogCrash(ex);

                        XtraMessageBox.Show(
                            this,
                            "That plugin could not be copied in." +
                                Environment.NewLine + Environment.NewLine +
                                ex.Message,
                            "Plugins",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }

                if (copied == 0)
                    return;

                XtraMessageBox.Show(
                    this,
                    copied + (copied == 1 ? " plugin was" : " plugins were") +
                        " added." + Environment.NewLine + Environment.NewLine +
                        "Restart Nexus and it will appear in this list.",
                    "Plugins",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void ShowSettings(
            string pluginId)
        {
            PluginRecord plugin = Find(pluginId);

            if (plugin == null)
                return;

            Nexus.Plugin.IPluginSettings aware =
                plugin.Instance as Nexus.Plugin.IPluginSettings;

            IEnumerable<Nexus.Plugin.PluginSetting> settings = null;

            if (aware != null)
            {
                PluginService.Guard(plugin, "describe its settings",
                    () => settings = aware.GetSettings());
            }

            using (Forms.PluginSettingsForm dialog =
                new Forms.PluginSettingsForm(
                    plugin,
                    settings ?? Enumerable.Empty<Nexus.Plugin.PluginSetting>()))
            {
                dialog.ShowDialog(this);
            }

            Render();
        }

        private void OpenFolder()
        {
            try
            {
                Directory.CreateDirectory(PluginService.UserFolder);

                Process.Start(new ProcessStartInfo(
                    PluginService.UserFolder)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                XtraMessageBox.Show(
                    this,
                    "The plugins folder could not be opened." +
                        Environment.NewLine + Environment.NewLine +
                        PluginService.UserFolder,
                    "Plugins",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
