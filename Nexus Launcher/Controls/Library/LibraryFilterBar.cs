using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Models;
using Nexus_Launcher.Services;
using Nexus_Launcher.Services.Library;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls.Library
{
    /// <summary>
    /// The Full Library toolbar: search, launcher, group, favourites,
    /// sort, section grouping and poster size.
    ///
    /// Raises one <see cref="FilterChanged"/> event carrying the whole
    /// filter rather than an event per control, so the host has a
    /// single place to rebuild from.
    /// </summary>
    internal class LibraryFilterBar : XtraUserControl
    {
        private const string AllLaunchers = "All launchers";

        private const string AllGroups = "All groups";

        private readonly ButtonEdit search = new ButtonEdit();

        private readonly ComboBoxEdit launcher = new ComboBoxEdit();

        private readonly ComboBoxEdit group = new ComboBoxEdit();

        private readonly ComboBoxEdit sort = new ComboBoxEdit();

        private readonly ComboBoxEdit grouping = new ComboBoxEdit();

        private readonly CheckButton favorites = new CheckButton();

        private readonly CheckButton installed = new CheckButton();

        private readonly TrackBarControl size = new TrackBarControl();

        private readonly LabelControl count = new LabelControl();

        /// <summary>
        /// Group ids in the same order as the group combo's items, so
        /// the selected index maps back to an id. Index 0 is "all".
        /// </summary>
        private readonly List<string> groupIds = new List<string>();

        private bool loading;

        public event Action<LibraryFilter> FilterChanged;

        /// <summary>
        /// Raised when the poster size slider moves.
        /// </summary>
        public event Action<int> TileSizeChanged;

        /// <summary>
        /// Raised by the refresh button.
        /// </summary>
        public event Action RefreshRequested;

        public LibraryFilterBar()
        {
            Height = 96;

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
                // Two rows: the search and the drop-downs on top, the
                // toggles, size slider and count underneath. Absolute
                // positions, re-flowed in OnResize.
                search.Properties.NullValuePrompt =
                    "Search your library";

                search.Properties.NullValuePromptShowForEmptyValue =
                    true;

                search.Properties.Buttons.Clear();

                search.Properties.Buttons.Add(
                    new EditorButton(ButtonPredefines.Search));

                search.Properties.Buttons.Add(
                    new EditorButton(ButtonPredefines.Delete));

                search.Properties.ButtonClick += Search_ButtonClick;

                search.EditValueChanged += Changed;

                Controls.Add(search);

                launcher.Properties.TextEditStyle =
                    TextEditStyles.DisableTextEditor;

                launcher.SelectedIndexChanged += Changed;

                Controls.Add(launcher);

                group.Properties.TextEditStyle =
                    TextEditStyles.DisableTextEditor;

                group.SelectedIndexChanged += Changed;

                Controls.Add(group);

                sort.Properties.TextEditStyle =
                    TextEditStyles.DisableTextEditor;

                sort.Properties.Items.AddRange(new object[]
                {
                    "Name A-Z",
                    "Name Z-A",
                    "Recently played",
                    "Most played",
                    "Launcher"
                });

                sort.SelectedIndex = 0;

                sort.SelectedIndexChanged += Changed;

                Controls.Add(sort);

                grouping.Properties.TextEditStyle =
                    TextEditStyles.DisableTextEditor;

                grouping.Properties.Items.AddRange(new object[]
                {
                    "No sections",
                    "By launcher",
                    "By group",
                    "A-Z sections"
                });

                grouping.SelectedIndex = 0;

                grouping.SelectedIndexChanged += Changed;

                Controls.Add(grouping);

                favorites.Text = "Favourites";

                favorites.AllowFocus = false;

                favorites.CheckedChanged += Changed;

                Controls.Add(favorites);

                installed.Text = "Installed";

                installed.AllowFocus = false;

                installed.CheckedChanged += Changed;

                Controls.Add(installed);

                size.Properties.Minimum = 110;

                size.Properties.Maximum = 300;

                size.Properties.SmallChange = 10;

                size.Properties.LargeChange = 30;

                size.Properties.ShowLabels = false;

                size.Properties.BorderStyle =
                    DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;

                size.Value = 172;

                size.EditValueChanged += Size_Changed;

                Controls.Add(size);

                count.Text = string.Empty;

                Controls.Add(count);
            }
            finally
            {
                ResumeLayout(true);
            }

            ApplyTheme();
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
            const int pad = 24;
            const int gap = 10;
            const int rowHeight = 26;

            int width = ClientSize.Width;

            // Top row: search grows, the three drop-downs are fixed.
            int top = 14;

            int comboWidth = 140;

            int right = width - pad;

            grouping.SetBounds(
                right - comboWidth,
                top,
                comboWidth,
                rowHeight);

            right -= comboWidth + gap;

            sort.SetBounds(
                right - comboWidth,
                top,
                comboWidth,
                rowHeight);

            right -= comboWidth + gap;

            group.SetBounds(
                right - comboWidth,
                top,
                comboWidth,
                rowHeight);

            right -= comboWidth + gap;

            launcher.SetBounds(
                right - comboWidth,
                top,
                comboWidth,
                rowHeight);

            right -= comboWidth + gap;

            int searchWidth =
                Math.Max(120, right - pad);

            search.SetBounds(pad, top, searchWidth, rowHeight);

            // Bottom row.
            int bottom = top + rowHeight + 12;

            favorites.SetBounds(pad, bottom, 104, rowHeight);

            installed.SetBounds(
                pad + 104 + gap,
                bottom,
                94,
                rowHeight);

            int sizeWidth = 150;

            size.SetBounds(
                width - pad - sizeWidth,
                bottom - 2,
                sizeWidth,
                rowHeight + 4);

            count.SetBounds(
                pad + 104 + gap + 94 + gap * 2,
                bottom + 6,
                Math.Max(
                    10,
                    width - pad - sizeWidth - gap -
                        (pad + 104 + gap + 94 + gap * 2)),
                rowHeight);
        }

        //--------------------------------------------------------------
        // Theme
        //--------------------------------------------------------------

        public void ApplyTheme()
        {
            BackColor =
                ProfileStyle.CardColor;

            count.Appearance.ForeColor =
                ProfileStyle.MutedTextColor;

            count.Appearance.Options.UseForeColor = true;

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // A hairline under the bar separates it from the grid
            // without the weight of a full border.
            using (Pen pen = new Pen(
                ProfileStyle.Blend(
                    ProfileStyle.MutedTextColor,
                    ProfileStyle.CardColor,
                    0.7)))
            {
                e.Graphics.DrawLine(
                    pen,
                    0,
                    Height - 1,
                    Width,
                    Height - 1);
            }
        }

        //--------------------------------------------------------------
        // Population
        //--------------------------------------------------------------

        /// <summary>
        /// Refills the launcher and group drop-downs from the current
        /// library, keeping the user's selection where it still exists.
        /// </summary>
        public void ReloadSources()
        {
            loading = true;

            try
            {
                string previousLauncher =
                    launcher.SelectedIndex > 0
                        ? launcher.Text
                        : null;

                string previousGroupId =
                    SelectedGroupId;

                launcher.Properties.Items.Clear();

                launcher.Properties.Items.Add(AllLaunchers);

                foreach (string name in
                    LibraryQueryService.GetLaunchers())
                {
                    launcher.Properties.Items.Add(name);
                }

                launcher.SelectedIndex = 0;

                if (previousLauncher != null)
                {
                    int index =
                        launcher.Properties.Items.IndexOf(
                            previousLauncher);

                    if (index > 0)
                        launcher.SelectedIndex = index;
                }

                groupIds.Clear();

                group.Properties.Items.Clear();

                group.Properties.Items.Add(AllGroups);

                groupIds.Add(null);

                List<LibraryGroup> groups =
                    LibraryOrganizationService.GetAllGroups()
                        .OrderBy(x => x.Launcher,
                            StringComparer.CurrentCultureIgnoreCase)
                        .ThenBy(x => x.Name,
                            StringComparer.CurrentCultureIgnoreCase)
                        .ToList();

                foreach (LibraryGroup item in groups)
                {
                    // Group names are only unique inside a launcher, so
                    // the launcher is shown to tell two "Co-op" groups
                    // apart.
                    group.Properties.Items.Add(
                        string.IsNullOrWhiteSpace(item.Launcher)
                            ? item.Name
                            : item.Name + "  (" + item.Launcher + ")");

                    groupIds.Add(item.Id);
                }

                group.SelectedIndex = 0;

                if (previousGroupId != null)
                {
                    int index =
                        groupIds.IndexOf(previousGroupId);

                    if (index > 0)
                        group.SelectedIndex = index;
                }
            }
            finally
            {
                loading = false;
            }
        }

        public void SetCount(
            int shown,
            int total)
        {
            count.Text =
                shown == total
                    ? total + (total == 1 ? " game" : " games")
                    : "Showing " + shown + " of " + total + " games";
        }

        //--------------------------------------------------------------
        // Reading the filter
        //--------------------------------------------------------------

        private string SelectedGroupId
        {
            get
            {
                int index = group.SelectedIndex;

                return index > 0 && index < groupIds.Count
                    ? groupIds[index]
                    : null;
            }
        }

        public LibraryFilter CurrentFilter
        {
            get
            {
                LibraryFilter filter = new LibraryFilter();

                filter.Search = search.Text;

                filter.Launcher =
                    launcher.SelectedIndex > 0
                        ? launcher.Text
                        : null;

                filter.GroupId = SelectedGroupId;

                filter.FavoritesOnly = favorites.Checked;

                filter.InstalledOnly = installed.Checked;

                switch (sort.SelectedIndex)
                {
                    case 1:
                        filter.Sort = LibrarySort.NameDescending;
                        break;
                    case 2:
                        filter.Sort = LibrarySort.RecentlyPlayed;
                        break;
                    case 3:
                        filter.Sort = LibrarySort.MostPlayed;
                        break;
                    case 4:
                        filter.Sort = LibrarySort.Launcher;
                        break;
                    default:
                        filter.Sort = LibrarySort.NameAscending;
                        break;
                }

                switch (grouping.SelectedIndex)
                {
                    case 1:
                        filter.Grouping = LibraryGrouping.Launcher;
                        break;
                    case 2:
                        filter.Grouping = LibraryGrouping.UserGroup;
                        break;
                    case 3:
                        filter.Grouping = LibraryGrouping.Alphabetical;
                        break;
                    default:
                        filter.Grouping = LibraryGrouping.None;
                        break;
                }

                return filter;
            }
        }

        /// <summary>
        /// Points the bar at a launcher and switches to launcher
        /// sections, for "show me everything in Steam" from elsewhere
        /// in the app.
        /// </summary>
        public void SelectLauncher(
            string name)
        {
            int index =
                launcher.Properties.Items.IndexOf(name);

            launcher.SelectedIndex =
                index > 0 ? index : 0;
        }

        public void ClearFilters()
        {
            loading = true;

            try
            {
                search.EditValue = null;

                launcher.SelectedIndex = 0;

                group.SelectedIndex = 0;

                favorites.Checked = false;

                installed.Checked = false;
            }
            finally
            {
                loading = false;
            }

            Raise();
        }

        //--------------------------------------------------------------
        // Events
        //--------------------------------------------------------------

        private void Search_ButtonClick(
            object sender,
            ButtonPressedEventArgs e)
        {
            if (e.Button.Kind == ButtonPredefines.Delete)
                search.EditValue = null;
        }

        private void Changed(
            object sender,
            EventArgs e)
        {
            Raise();
        }

        private void Size_Changed(
            object sender,
            EventArgs e)
        {
            if (loading)
                return;

            Action<int> handler = TileSizeChanged;

            if (handler != null)
                handler(size.Value);
        }

        private void Raise()
        {
            if (loading)
                return;

            Action<LibraryFilter> handler = FilterChanged;

            if (handler != null)
                handler(CurrentFilter);
        }

        public void RequestRefresh()
        {
            Action handler = RefreshRequested;

            if (handler != null)
                handler();
        }
    }
}
