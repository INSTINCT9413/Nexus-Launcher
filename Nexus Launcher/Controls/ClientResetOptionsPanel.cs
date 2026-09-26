using DevExpress.XtraEditors;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls
{
    /// <summary>
    /// The list of things a client reset will remove, as checkboxes, on
    /// the Client Reset tab beside the step indicators.
    ///
    /// The plan a client's definition produces still describes every
    /// folder Nexus knows about for that client. This panel decides
    /// which of those categories a particular run touches, so the
    /// definitions stay a description of the client rather than of one
    /// person's preferences.
    ///
    /// The choice is remembered across runs and across clients. Someone
    /// who never wants their shader caches cleared should have to say
    /// so once, not once per launcher.
    /// </summary>
    internal class ClientResetOptionsPanel : XtraUserControl
    {
        /// <summary>
        /// One tickable category.
        /// </summary>
        private class Option
        {
            public string Key;

            public string Text;

            public string Hint;

            public CheckEdit Check;

            public LabelControl HintLabel;

            /// <summary>
            /// Copies this category's state onto a plan.
            /// </summary>
            public Action<ClientResetPlan, bool> Apply;

            /// <summary>
            /// Whether the client being shown has anything in this
            /// category at all. Null means it does not depend on the
            /// client.
            /// </summary>
            public Func<ClientResetPlan, bool> HasWork;
        }

        private const int Pad = 14;

        private const int RowHeight = 21;

        private const int HintHeight = 15;

        private const int RowGap = 8;

        private const int HeaderHeight = 26;

        /// <summary>
        /// Stored when every category is on, so a later release that
        /// adds a category has it on by default rather than silently
        /// off for everyone who ever saved.
        /// </summary>
        private const string AllSelected = "*";

        private readonly LabelControl title = new LabelControl();

        private readonly LabelControl clientHeader = new LabelControl();

        private readonly LabelControl systemHeader = new LabelControl();

        private readonly SimpleButton allButton = new SimpleButton();

        private readonly SimpleButton noneButton = new SimpleButton();

        private readonly List<Option> clientOptions = new List<Option>();

        private readonly List<Option> systemOptions = new List<Option>();

        private bool loading;

        /// <summary>
        /// Raised when the selection changes, so the reset screen can
        /// react (the Reset button is pointless with nothing ticked).
        /// </summary>
        public event Action SelectionChanged;

        public ClientResetOptionsPanel()
        {
            BuildOptions();

            title.Text = "What this removes";

            Controls.Add(title);

            clientHeader.Text = "THIS CLIENT";

            Controls.Add(clientHeader);

            systemHeader.Text = "SYSTEM";

            Controls.Add(systemHeader);

            foreach (Option option in AllOptions)
                AddOption(option);

            allButton.Text = "All";

            allButton.Click += (s, e) => SetAll(true);

            Controls.Add(allButton);

            noneButton.Text = "None";

            noneButton.Click += (s, e) => SetAll(false);

            Controls.Add(noneButton);

            Load();

            ApplyTheme();
        }

        private IEnumerable<Option> AllOptions
        {
            get
            {
                return clientOptions.Concat(systemOptions);
            }
        }

        //--------------------------------------------------------------
        // The categories
        //--------------------------------------------------------------

        private void BuildOptions()
        {
            clientOptions.Add(new Option
            {
                Key = "clientCache",
                Text = "Caches",
                Hint = "Downloaded pages, thumbnails and manifests.",
                Apply = (p, on) => p.ClearClientCaches = on,
                HasWork = p => p.CacheFolders.Count > 0
            });

            clientOptions.Add(new Option
            {
                Key = "clientTemp",
                Text = "Temp files",
                Hint = "Half finished downloads and scratch files.",
                Apply = (p, on) => p.ClearClientTemp = on,
                HasWork = p => p.TempFolders.Count > 0
            });

            clientOptions.Add(new Option
            {
                Key = "clientLogs",
                Text = "Logs",
                Hint = "Client log files and their backups.",
                Apply = (p, on) => p.ClearClientLogs = on,
                HasWork = p => p.LogFolders.Count > 0
            });

            clientOptions.Add(new Option
            {
                Key = "clientCrash",
                Text = "Crash dumps",
                Hint = "Memory dumps left behind by crashes.",
                Apply = (p, on) => p.ClearClientCrashDumps = on,
                HasWork = p => p.CrashFolders.Count > 0
            });

            clientOptions.Add(new Option
            {
                Key = "clientData",
                Text = "Other stale data",
                Hint = "Folders the client rebuilds on its own.",
                Apply = (p, on) => p.ClearClientData = on,
                HasWork = p => p.FoldersToClear.Count > 0
            });

            systemOptions.Add(new Option
            {
                Key = "windowsTemp",
                Text = "Windows temp and error reports",
                Hint = "%TEMP%, Windows\\Temp and queued crash reports.",
                Apply = (p, on) => p.ClearWindowsTemp = on
            });

            systemOptions.Add(new Option
            {
                Key = "shaderCache",
                Text = "Shader caches",
                Hint =
                    "NVIDIA, AMD and Intel compiled shaders. Usually " +
                    "the biggest saving, but games stutter once while " +
                    "they rebuild.",
                Apply = (p, on) => p.ClearShaderCaches = on
            });

            systemOptions.Add(new Option
            {
                Key = "windowsUpdate",
                Text = "Windows Update cache",
                Hint =
                    "Downloaded update packages and Delivery " +
                    "Optimization files. Needs administrator rights.",
                Apply = (p, on) => p.ClearWindowsUpdateCache = on
            });
        }

        private void AddOption(
            Option option)
        {
            option.Check = new CheckEdit();

            option.Check.Text = option.Text;

            option.Check.CheckedChanged += Option_CheckedChanged;

            Controls.Add(option.Check);

            option.HintLabel = new LabelControl();

            option.HintLabel.Text = option.Hint;

            option.HintLabel.AutoSizeMode = LabelAutoSizeMode.None;

            option.HintLabel.Appearance.TextOptions.WordWrap =
                DevExpress.Utils.WordWrap.Wrap;

            option.HintLabel.Appearance.Options.UseTextOptions = true;

            Controls.Add(option.HintLabel);
        }

        //--------------------------------------------------------------
        // Selection
        //--------------------------------------------------------------

        private void Option_CheckedChanged(
            object sender,
            EventArgs e)
        {
            if (loading)
                return;

            Save();

            Action handler = SelectionChanged;

            if (handler != null)
                handler();
        }

        private void SetAll(
            bool on)
        {
            loading = true;

            try
            {
                // Including the categories this client has nothing
                // for. They are greyed but stay ticked, so the choice
                // still applies to a client that does have them: the
                // selection is the user's, not this client's.
                foreach (Option option in AllOptions)
                    option.Check.Checked = on;
            }
            finally
            {
                loading = false;
            }

            Option_CheckedChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Whether anything at all is ticked and available.
        /// </summary>
        public bool HasSelection
        {
            get
            {
                return AllOptions.Any(
                    x => x.Check.Checked && x.Check.Enabled);
            }
        }

        /// <summary>
        /// The ticked categories, for the confirmation prompt.
        /// </summary>
        public List<string> SelectedNames
        {
            get
            {
                return AllOptions
                    .Where(x => x.Check.Checked && x.Check.Enabled)
                    .Select(x => x.Text.ToLowerInvariant())
                    .ToList();
            }
        }

        /// <summary>
        /// Copies the selection onto a freshly built plan.
        /// </summary>
        public void Apply(
            ClientResetPlan plan)
        {
            if (plan == null)
                return;

            foreach (Option option in AllOptions)
            {
                option.Apply(
                    plan,
                    option.Check.Checked && option.Check.Enabled);
            }
        }

        /// <summary>
        /// Greys out the categories this client has no folders for, so
        /// the list describes the client in front of the user rather
        /// than clients in general.
        /// </summary>
        public void ShowPlan(
            ClientResetPlan plan)
        {
            foreach (Option option in AllOptions)
            {
                bool available =
                    option.HasWork == null ||
                    (plan != null && option.HasWork(plan));

                option.Check.Enabled = available;

                option.HintLabel.Enabled = available;
            }

            // Administrator rights are not something the user can fix
            // from here, so the box says why rather than going quiet.
            Option update =
                systemOptions.First(x => x.Key == "windowsUpdate");

            if (!ClientResetRunner.IsElevated)
            {
                update.HintLabel.Text =
                    "Skipped: Nexus is not running as administrator.";
            }

            Relayout();
        }

        //--------------------------------------------------------------
        // Persistence
        //--------------------------------------------------------------

        private void Load()
        {
            loading = true;

            try
            {
                string stored =
                    Settings.Default.ClientResetOptions;

                if (string.IsNullOrEmpty(stored) ||
                    stored == AllSelected)
                {
                    foreach (Option option in AllOptions)
                        option.Check.Checked = true;

                    return;
                }

                HashSet<string> keys =
                    new HashSet<string>(
                        stored.Split(
                            new[] { ',' },
                            StringSplitOptions.RemoveEmptyEntries),
                        StringComparer.OrdinalIgnoreCase);

                foreach (Option option in AllOptions)
                    option.Check.Checked = keys.Contains(option.Key);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
            finally
            {
                loading = false;
            }
        }

        private void Save()
        {
            try
            {
                // Every category on is stored as the sentinel, so a
                // category added later starts on rather than off.
                bool all =
                    AllOptions.All(x => x.Check.Checked);

                Settings.Default.ClientResetOptions =
                    all
                        ? AllSelected
                        : string.Join(
                            ",",
                            AllOptions
                                .Where(x => x.Check.Checked)
                                .Select(x => x.Key)
                                .ToArray());

                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        //--------------------------------------------------------------
        // Layout
        //--------------------------------------------------------------

        protected override void OnResize(
            EventArgs e)
        {
            base.OnResize(e);

            Relayout();
        }

        private void Relayout()
        {
            int width =
                Math.Max(160, ClientSize.Width - Pad * 2);

            int y = Pad;

            title.SetBounds(Pad, y, width, 24);

            y += 30;

            y = LayoutGroup(clientHeader, clientOptions, width, y);

            y += 6;

            y = LayoutGroup(systemHeader, systemOptions, width, y);

            y += 8;

            allButton.SetBounds(Pad, y, 58, 26);

            noneButton.SetBounds(Pad + 64, y, 58, 26);
        }

        private int LayoutGroup(
            LabelControl header,
            List<Option> options,
            int width,
            int y)
        {
            header.SetBounds(Pad, y, width, HeaderHeight - 8);

            y += HeaderHeight;

            foreach (Option option in options)
            {
                option.Check.SetBounds(Pad, y, width, RowHeight);

                y += RowHeight;

                // Hints wrap, so how far the next row starts depends on
                // how tall this one turned out.
                int hintWidth =
                    Math.Max(80, width - 20);

                int hintHeight =
                    MeasureHint(option.HintLabel, hintWidth);

                option.HintLabel.SetBounds(
                    Pad + 20,
                    y,
                    hintWidth,
                    hintHeight);

                y += hintHeight + RowGap;
            }

            return y;
        }

        private int MeasureHint(
            LabelControl label,
            int width)
        {
            try
            {
                using (Graphics g = CreateGraphics())
                {
                    SizeF size =
                        g.MeasureString(
                            label.Text,
                            label.Appearance.GetFont(),
                            width);

                    return Math.Max(
                        HintHeight,
                        (int)Math.Ceiling(size.Height));
                }
            }
            catch (Exception)
            {
                return HintHeight * 2;
            }
        }

        public void ApplyTheme()
        {
            BackColor = ProfileStyle.CardColor;

            title.Appearance.ForeColor = ProfileStyle.TextColor;
            title.Appearance.Options.UseForeColor = true;
            title.Appearance.Font = ProfileStyle.Font(11F, FontStyle.Bold);
            title.Appearance.Options.UseFont = true;

            foreach (LabelControl header in
                new[] { clientHeader, systemHeader })
            {
                header.Appearance.ForeColor = ProfileStyle.AccentColor;
                header.Appearance.Options.UseForeColor = true;
                header.Appearance.Font = ProfileStyle.Font(8F, FontStyle.Bold);
                header.Appearance.Options.UseFont = true;
            }

            foreach (Option option in AllOptions)
            {
                option.HintLabel.Appearance.ForeColor =
                    ProfileStyle.MutedTextColor;

                option.HintLabel.Appearance.Options.UseForeColor = true;

                option.HintLabel.Appearance.Font =
                    ProfileStyle.Font(8F);

                option.HintLabel.Appearance.Options.UseFont = true;
            }

            Relayout();
        }
    }
}
