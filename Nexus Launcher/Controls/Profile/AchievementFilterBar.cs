using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using Nexus_Launcher.Services.Achievements;
using Nexus_Launcher.Services.Library;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls.Profile
{
    /// <summary>
    /// The Achievements tab toolbar: which launcher, which states, and
    /// what to do with the goals that have not been started.
    ///
    /// Raises one <see cref="FilterChanged"/> carrying the whole filter
    /// rather than an event per control, so the panel has a single
    /// place to rebind from.
    /// </summary>
    internal class AchievementFilterBar : CardPanel
    {
        private const string AllLaunchers = "All";

        private const string GeneralItem = "General (no launcher)";

        private readonly ComboBoxEdit launcher = new ComboBoxEdit();

        private readonly CheckButton earned = new CheckButton();

        private readonly CheckButton inProgress = new CheckButton();

        private readonly CheckButton locked = new CheckButton();

        private readonly ComboBoxEdit noProgress = new ComboBoxEdit();

        private readonly LabelControl countLabel = new LabelControl();

        /// <summary>
        /// The "put away" line: how much is stashed and the way to get
        /// it back.
        /// </summary>
        private readonly LabelControl stashLabel = new LabelControl();

        private readonly SimpleButton stashButton = new SimpleButton();

        private readonly SimpleButton resetButton = new SimpleButton();

        /// <summary>
        /// Launcher names in combo order. Index 0 is "All" and index 1
        /// is General, so neither maps to a real launcher.
        /// </summary>
        private readonly List<string> launcherValues =
            new List<string>();

        private bool loading;

        private bool showEmpty;

        public event Action<AchievementViewFilter> FilterChanged;

        public AchievementFilterBar()
        {
            Height = 104;

            Build();
        }

        //--------------------------------------------------------------
        // Construction
        //--------------------------------------------------------------

        private void Build()
        {
            SuspendLayout();

            try
            {
                launcher.Properties.TextEditStyle =
                    TextEditStyles.DisableTextEditor;

                launcher.SelectedIndexChanged += Changed;

                Controls.Add(launcher);

                SetUpToggle(earned, "Earned");
                SetUpToggle(inProgress, "In progress");
                SetUpToggle(locked, "Not started");

                noProgress.Properties.TextEditStyle =
                    TextEditStyles.DisableTextEditor;

                noProgress.Properties.Items.AddRange(new object[]
                {
                    "Not started: at the end",
                    "Not started: in order",
                    "Not started: hidden"
                });

                noProgress.SelectedIndex = 0;

                noProgress.SelectedIndexChanged += Changed;

                Controls.Add(noProgress);

                countLabel.AutoSizeMode = LabelAutoSizeMode.None;

                Controls.Add(countLabel);

                stashLabel.AutoSizeMode = LabelAutoSizeMode.None;

                Controls.Add(stashLabel);

                stashButton.PaintStyle =
                    DevExpress.XtraEditors.Controls.PaintStyles.Light;

                stashButton.AllowFocus = false;

                stashButton.Cursor = Cursors.Hand;

                stashButton.Click += Stash_Click;

                Controls.Add(stashButton);

                resetButton.Text = "Reset";

                resetButton.PaintStyle =
                    DevExpress.XtraEditors.Controls.PaintStyles.Light;

                resetButton.AllowFocus = false;

                resetButton.Cursor = Cursors.Hand;

                resetButton.Click += Reset_Click;

                Controls.Add(resetButton);
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void SetUpToggle(
            CheckButton button,
            string text)
        {
            button.Text = text;

            button.Checked = true;

            button.AllowFocus = false;

            button.CheckedChanged += Changed;

            Controls.Add(button);
        }

        //--------------------------------------------------------------
        // Layout
        //--------------------------------------------------------------

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            Relayout();
        }

        private void Relayout()
        {
            const int pad = 16;
            const int gap = 8;
            const int rowHeight = 26;

            int width = ClientSize.Width;

            int top = 14;

            launcher.SetBounds(pad, top, 168, rowHeight);

            int x = pad + 168 + gap * 2;

            earned.SetBounds(x, top, 84, rowHeight);
            x += 84 + gap;

            inProgress.SetBounds(x, top, 96, rowHeight);
            x += 96 + gap;

            locked.SetBounds(x, top, 96, rowHeight);
            x += 96 + gap * 2;

            noProgress.SetBounds(x, top, 170, rowHeight);

            resetButton.SetBounds(
                Math.Max(x + 178, width - pad - 70),
                top,
                70,
                rowHeight);

            // Second row: what is on show, and what has been put away.
            int bottom = top + rowHeight + 10;

            countLabel.SetBounds(pad, bottom + 4, 260, 18);

            stashLabel.SetBounds(
                pad + 268,
                bottom + 4,
                Math.Max(60, width - pad - 268 - 140),
                18);

            stashButton.SetBounds(
                Math.Max(pad + 268, width - pad - 132),
                bottom,
                132,
                rowHeight);
        }

        //--------------------------------------------------------------
        // Population
        //--------------------------------------------------------------

        /// <summary>
        /// Fills the launcher list. Every launcher Nexus scans is
        /// listed whether or not it has games, so the filter can still
        /// be pointed at one whose set is currently put away.
        /// </summary>
        public void ReloadLaunchers()
        {
            loading = true;

            try
            {
                string previous =
                    SelectedLauncher;

                launcher.Properties.Items.Clear();

                launcherValues.Clear();

                launcher.Properties.Items.Add(AllLaunchers);

                launcherValues.Add(null);

                launcher.Properties.Items.Add(GeneralItem);

                launcherValues.Add(AchievementViewFilter.General);

                foreach (string name in
                    LibraryStatsService.GameLaunchers)
                {
                    launcher.Properties.Items.Add(name);

                    launcherValues.Add(name);
                }

                launcher.SelectedIndex = 0;

                if (previous != null)
                {
                    int index =
                        launcherValues.IndexOf(previous);

                    if (index > 0)
                        launcher.SelectedIndex = index;
                }
            }
            finally
            {
                loading = false;
            }
        }

        private string SelectedLauncher
        {
            get
            {
                int index = launcher.SelectedIndex;

                return index >= 0 && index < launcherValues.Count
                    ? launcherValues[index]
                    : null;
            }
        }

        //--------------------------------------------------------------
        // Reading
        //--------------------------------------------------------------

        public AchievementViewFilter CurrentFilter
        {
            get
            {
                AchievementViewFilter filter =
                    new AchievementViewFilter();

                filter.Launcher = SelectedLauncher;

                filter.ShowEarned = earned.Checked;

                filter.ShowInProgress = inProgress.Checked;

                filter.ShowLocked = locked.Checked;

                switch (noProgress.SelectedIndex)
                {
                    case 1:
                        filter.NoProgress = NoProgressMode.Show;
                        break;
                    case 2:
                        filter.NoProgress = NoProgressMode.Hide;
                        break;
                    default:
                        filter.NoProgress = NoProgressMode.MoveToEnd;
                        break;
                }

                filter.ShowEmptyLaunchers = showEmpty;

                return filter;
            }
        }

        /// <summary>
        /// Updates the two summary lines after the panel has rebound.
        /// </summary>
        public void SetSummary(
            int shownAchievements,
            int totalAchievements,
            int shownBadges,
            int totalBadges,
            int stashed,
            List<string> emptyLaunchers)
        {
            countLabel.Text =
                "Showing " + shownAchievements + " of " +
                totalAchievements + " achievements, " +
                shownBadges + " of " + totalBadges + " badges";

            bool anyStashed =
                stashed > 0 ||
                (showEmpty &&
                    emptyLaunchers != null &&
                    emptyLaunchers.Count > 0);

            stashButton.Visible = anyStashed;

            stashLabel.Visible = anyStashed;

            if (!anyStashed)
                return;

            stashButton.Text =
                showEmpty
                    ? "Put unused away"
                    : "Show all anyway";

            if (showEmpty)
            {
                stashLabel.Text =
                    "Showing goals for launchers with no games.";

                return;
            }

            string names =
                emptyLaunchers == null || emptyLaunchers.Count == 0
                    ? string.Empty
                    : string.Join(", ", emptyLaunchers);

            stashLabel.Text =
                stashed + " put away for launchers with no games" +
                (names.Length > 0 ? ": " + names : string.Empty);
        }

        //--------------------------------------------------------------
        // Theme
        //--------------------------------------------------------------

        public override void ApplyTheme()
        {
            base.ApplyTheme();

            countLabel.Appearance.ForeColor =
                ProfileStyle.MutedTextColor;

            countLabel.Appearance.Options.UseForeColor = true;

            countLabel.Appearance.Font = ProfileStyle.Font(8.5F);

            countLabel.Appearance.Options.UseFont = true;

            stashLabel.Appearance.ForeColor =
                ProfileStyle.MutedTextColor;

            stashLabel.Appearance.Options.UseForeColor = true;

            stashLabel.Appearance.Font = ProfileStyle.Font(8.5F);

            stashLabel.Appearance.Options.UseFont = true;
        }

        //--------------------------------------------------------------
        // Events
        //--------------------------------------------------------------

        private void Changed(
            object sender,
            EventArgs e)
        {
            Raise();
        }

        private void Stash_Click(
            object sender,
            EventArgs e)
        {
            showEmpty = !showEmpty;

            Raise();
        }

        private void Reset_Click(
            object sender,
            EventArgs e)
        {
            loading = true;

            try
            {
                launcher.SelectedIndex = 0;

                earned.Checked = true;

                inProgress.Checked = true;

                locked.Checked = true;

                noProgress.SelectedIndex = 0;

                showEmpty = false;
            }
            finally
            {
                loading = false;
            }

            Raise();
        }

        private void Raise()
        {
            if (loading)
                return;

            Action<AchievementViewFilter> handler = FilterChanged;

            if (handler != null)
                handler(CurrentFilter);
        }
    }
}
