using DevExpress.XtraEditors;
using Nexus.Plugin;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Services.Plugins;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Forms
{
    /// <summary>
    /// The settings dialog for a plugin, built from what the plugin
    /// declared.
    ///
    /// The plugin supplies a list of settings, not controls, so every
    /// plugin's settings window is laid out and themed by Nexus and
    /// looks like part of the launcher. It also means a plugin author
    /// writes no user interface code at all.
    /// </summary>
    internal class PluginSettingsForm : XtraForm
    {
        private readonly PluginRecord plugin;

        private readonly List<PluginSetting> definitions;

        private readonly Dictionary<string, Control> editors =
            new Dictionary<string, Control>();

        private const int LabelLeft = 18;
        private const int EditorLeft = 18;
        private const int RowWidth = 464;

        private readonly List<PluginService.RegisteredSource> sources;

        public PluginSettingsForm(
            PluginRecord plugin,
            IEnumerable<PluginSetting> settings)
        {
            this.plugin = plugin;

            definitions = (settings ?? Enumerable.Empty<PluginSetting>())
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Key))
                .ToList();

            // Whether a plugin's library sources appear in the sidebar
            // is the launcher's business, not the plugin's, so these
            // switches are added here rather than every plugin that
            // contributes a source having to declare them.
            sources = PluginService.Sources
                .Where(s => s.Owner == plugin)
                .ToList();

            Text = plugin.Name + " settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;

            Build();
        }

        private void Build()
        {
            Dictionary<string, string> stored =
                PluginSettingsStore.Load(plugin.Id);

            int y = 16;

            foreach (PluginSetting setting in definitions)
            {
                string current;

                if (!stored.TryGetValue(setting.Key, out current))
                    current = setting.Default;

                if (setting.Kind == PluginSettingKind.Toggle)
                {
                    // The tick box carries its own caption, so it
                    // needs no separate label above it.
                    CheckEdit toggle = new CheckEdit();
                    toggle.Text = setting.Label ?? setting.Key;
                    toggle.Checked =
                        string.Equals(current, "true",
                            StringComparison.OrdinalIgnoreCase);
                    toggle.SetBounds(EditorLeft, y, RowWidth, 22);

                    Controls.Add(toggle);
                    editors[setting.Key] = toggle;

                    y += 26;
                }
                else
                {
                    LabelControl label = new LabelControl();
                    label.Text = setting.Label ?? setting.Key;
                    label.SetBounds(LabelLeft, y, RowWidth, 16);
                    Controls.Add(label);

                    y += 20;

                    Control editor = BuildEditor(setting, current, y);
                    Controls.Add(editor);
                    editors[setting.Key] = editor;

                    y += editor.Height + 6;
                }

                if (!string.IsNullOrWhiteSpace(setting.Description))
                {
                    LabelControl hint = new LabelControl();
                    hint.Text = setting.Description;
                    hint.Appearance.ForeColor = ProfileStyle.MutedTextColor;
                    hint.Appearance.Options.UseForeColor = true;
                    hint.Appearance.Font = ProfileStyle.Font(8F);
                    hint.Appearance.Options.UseFont = true;
                    hint.AutoSizeMode = LabelAutoSizeMode.None;
                    hint.Appearance.TextOptions.WordWrap =
                        DevExpress.Utils.WordWrap.Wrap;
                    hint.Appearance.Options.UseTextOptions = true;
                    // Measured, not guessed: a fixed height silently
                    // clipped the longer descriptions, which are
                    // exactly the ones worth reading.
                    int needed = TextRenderer.MeasureText(
                        setting.Description,
                        hint.Appearance.Font,
                        new Size(RowWidth, int.MaxValue),
                        TextFormatFlags.WordBreak).Height;

                    hint.SetBounds(LabelLeft, y, RowWidth,
                        Math.Max(16, needed + 2));

                    Controls.Add(hint);

                    y += hint.Height + 4;
                }

                y += 8;
            }

            y = BuildSourceToggles(y);

            y = BuildCommands(y);

            if (definitions.Count == 0 && sources.Count == 0 &&
                commandButtons.Count == 0)
            {
                LabelControl none = new LabelControl();
                none.Text = "This plugin has nothing to configure.";
                none.SetBounds(LabelLeft, y, RowWidth, 20);
                Controls.Add(none);

                y += 30;
            }

            // Nothing to save means nothing to cancel either: a
            // dialog of buttons should not pretend otherwise.
            bool anythingToSave =
                definitions.Count > 0 || sourceToggles.Count > 0;

            SimpleButton save = new SimpleButton();
            save.Text = anythingToSave ? "Save" : "Close";
            save.SetBounds(RowWidth - 90, y + 6, 100, 30);
            save.Click += Save_Click;
            Controls.Add(save);

            SimpleButton cancel = new SimpleButton();
            cancel.Text = "Cancel";
            cancel.SetBounds(RowWidth - 196, y + 6, 100, 30);
            cancel.DialogResult = DialogResult.Cancel;
            cancel.Visible = anythingToSave;
            Controls.Add(cancel);

            AcceptButton = save;
            CancelButton = cancel;

            ClientSize = new Size(RowWidth + 36, y + 52);
        }

        private readonly Dictionary<string, CheckEdit> sourceToggles =
            new Dictionary<string, CheckEdit>(StringComparer.OrdinalIgnoreCase);

        private readonly List<SimpleButton> commandButtons =
            new List<SimpleButton>();

        /// <summary>
        /// A switch per library source, deciding whether it gets a
        /// heading in the sidebar.
        /// </summary>
        private int BuildSourceToggles(
            int y)
        {
            if (sources.Count == 0)
                return y;

            y = Heading("Show in the sidebar", y);

            foreach (PluginService.RegisteredSource entry in sources)
            {
                string id = null;
                string name = null;

                PluginService.RegisteredSource captured = entry;

                PluginService.Guard(plugin, "read its library source",
                    () =>
                    {
                        id = captured.Source.Id;
                        name = captured.Source.DisplayName;
                    });

                if (string.IsNullOrWhiteSpace(id))
                    continue;

                CheckEdit toggle = new CheckEdit();
                toggle.Text = name ?? id;
                toggle.Checked = PluginService.IsSourceVisible(id);
                toggle.SetBounds(EditorLeft, y, RowWidth, 22);

                Controls.Add(toggle);
                sourceToggles[id] = toggle;

                y += 26;
            }

            return y + 8;
        }

        /// <summary>
        /// The plugin's own buttons: things it does on request, which
        /// is where a user looks for them rather than on the menu for
        /// every game in the library.
        /// </summary>
        private int BuildCommands(
            int y)
        {
            IPluginCommands source =
                plugin.Instance as IPluginCommands;

            if (source == null)
                return y;

            IEnumerable<PluginCommand> commands = null;

            PluginService.Guard(plugin, "list its commands",
                () => commands = source.GetCommands());

            if (commands == null)
                return y;

            List<PluginCommand> usable = commands
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.Label))
                .ToList();

            if (usable.Count == 0)
                return y;

            y = Heading("Actions", y);

            foreach (PluginCommand command in usable)
            {
                SimpleButton button = new SimpleButton();
                button.Text = command.Label;
                button.SetBounds(EditorLeft, y, 180, 28);

                string id = command.Id;

                button.Click += (s, e) =>
                    PluginService.Guard(plugin, "run " + id,
                        () => source.RunCommand(id));

                Controls.Add(button);
                commandButtons.Add(button);

                if (!string.IsNullOrWhiteSpace(command.Description))
                {
                    LabelControl hint = new LabelControl();
                    hint.Text = command.Description;
                    hint.Appearance.ForeColor = ProfileStyle.MutedTextColor;
                    hint.Appearance.Options.UseForeColor = true;
                    hint.Appearance.Font = ProfileStyle.Font(8F);
                    hint.Appearance.Options.UseFont = true;
                    hint.AutoSizeMode = LabelAutoSizeMode.None;
                    hint.Appearance.TextOptions.WordWrap =
                        DevExpress.Utils.WordWrap.Wrap;
                    hint.Appearance.Options.UseTextOptions = true;
                    int width = RowWidth - 192;

                    int needed = TextRenderer.MeasureText(
                        command.Description,
                        hint.Appearance.Font,
                        new Size(width, int.MaxValue),
                        TextFormatFlags.WordBreak).Height;

                    hint.SetBounds(EditorLeft + 192, y + 2,
                        width, Math.Max(16, needed + 2));

                    Controls.Add(hint);

                    y += Math.Max(34, hint.Height + 10);
                }
                else
                {
                    y += 34;
                }
            }

            return y + 6;
        }

        private int Heading(
            string text,
            int y)
        {
            LabelControl label = new LabelControl();
            label.Text = text;
            label.Appearance.Font = ProfileStyle.Font(9.5F, FontStyle.Bold);
            label.Appearance.Options.UseFont = true;
            label.SetBounds(LabelLeft, y, RowWidth, 18);
            Controls.Add(label);

            return y + 24;
        }

        private Control BuildEditor(
            PluginSetting setting,
            string current,
            int y)
        {
            switch (setting.Kind)
            {
                case PluginSettingKind.Choice:
                    ComboBoxEdit choice = new ComboBoxEdit();
                    choice.Properties.TextEditStyle =
                        DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;

                    if (setting.Choices != null)
                    {
                        foreach (string option in setting.Choices)
                            choice.Properties.Items.Add(option);
                    }

                    choice.EditValue = current;
                    choice.SetBounds(EditorLeft, y, RowWidth, 22);
                    return choice;

                case PluginSettingKind.Number:
                    SpinEdit number = new SpinEdit();
                    number.Properties.IsFloatValue = false;
                    decimal parsed;
                    number.Value =
                        decimal.TryParse(current, out parsed) ? parsed : 0;
                    number.SetBounds(EditorLeft, y, 140, 22);
                    return number;

                case PluginSettingKind.Folder:
                    ButtonEdit folder = new ButtonEdit();
                    folder.Text = current ?? string.Empty;
                    folder.Properties.Buttons[0].Kind =
                        DevExpress.XtraEditors.Controls.ButtonPredefines.Ellipsis;
                    folder.SetBounds(EditorLeft, y, RowWidth, 22);

                    ButtonEdit captured = folder;

                    folder.ButtonClick += (s, e) =>
                    {
                        using (FolderBrowserDialog picker =
                            new FolderBrowserDialog())
                        {
                            if (!string.IsNullOrWhiteSpace(captured.Text))
                                picker.SelectedPath = captured.Text;

                            if (picker.ShowDialog(this) == DialogResult.OK)
                                captured.Text = picker.SelectedPath;
                        }
                    };

                    return folder;

                default:
                    TextEdit text = new TextEdit();
                    text.Text = current ?? string.Empty;
                    text.SetBounds(EditorLeft, y, RowWidth, 22);
                    return text;
            }
        }

        private void Save_Click(
            object sender,
            EventArgs e)
        {
            Dictionary<string, string> values =
                PluginSettingsStore.Load(plugin.Id);

            foreach (PluginSetting setting in definitions)
            {
                Control editor;

                if (!editors.TryGetValue(setting.Key, out editor))
                    continue;

                values[setting.Key] = Read(setting, editor);
            }

            PluginSettingsStore.Save(plugin.Id, values);

            foreach (KeyValuePair<string, CheckEdit> pair in sourceToggles)
            {
                PluginService.SetSourceVisible(
                    pair.Key, pair.Value.Checked);
            }

            // Telling the plugin is the last step, and behind a guard:
            // a plugin that throws here must not lose the settings the
            // user just saved.
            IPluginSettings aware =
                plugin.Instance as IPluginSettings;

            if (aware != null)
            {
                PluginService.Guard(plugin, "apply its settings",
                    () => aware.SettingsChanged(values));
            }

            DialogResult = DialogResult.OK;

            Close();
        }

        private static string Read(
            PluginSetting setting,
            Control editor)
        {
            CheckEdit toggle = editor as CheckEdit;

            if (toggle != null)
                return toggle.Checked ? "true" : "false";

            SpinEdit number = editor as SpinEdit;

            if (number != null)
                return ((long)number.Value).ToString();

            BaseEdit edit = editor as BaseEdit;

            return edit != null
                ? (edit.EditValue ?? string.Empty).ToString()
                : editor.Text;
        }
    }
}
