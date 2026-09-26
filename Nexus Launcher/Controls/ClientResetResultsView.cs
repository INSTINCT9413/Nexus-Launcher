using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls
{
    /// <summary>
    /// What a finished client reset achieved: how much was freed, and
    /// a line for every location that was cleared.
    ///
    /// Shown in place of the reset screen once a run finishes, with a
    /// way back to it and a way straight into the client.
    /// </summary>
    internal class ClientResetResultsView : XtraUserControl
    {
        private readonly LabelControl headline = new LabelControl();

        private readonly LabelControl summary = new LabelControl();

        private readonly LabelControl notice = new LabelControl();

        private readonly GridControl grid = new GridControl();

        private readonly GridView view = new GridView();

        private readonly SimpleButton backButton = new SimpleButton();

        private readonly SimpleButton launchButton = new SimpleButton();

        private readonly BindingList<ResetLogEntry> rows =
            new BindingList<ResetLogEntry>();

        /// <summary>
        /// Back to the reset screen.
        /// </summary>
        public event Action BackRequested;

        /// <summary>
        /// Start the client that was just reset.
        /// </summary>
        public event Action LaunchRequested;

        public ClientResetResultsView()
        {
            Build();
        }

        private void Build()
        {
            SuspendLayout();

            try
            {
                headline.AutoSizeMode = LabelAutoSizeMode.None;
                headline.Appearance.Font = ProfileStyle.Font(26F, FontStyle.Bold);
                headline.Appearance.Options.UseFont = true;
                headline.Appearance.TextOptions.HAlignment =
                    DevExpress.Utils.HorzAlignment.Center;

                Controls.Add(headline);

                summary.AutoSizeMode = LabelAutoSizeMode.None;
                summary.Appearance.Font = ProfileStyle.Font(10F);
                summary.Appearance.Options.UseFont = true;
                summary.Appearance.TextOptions.HAlignment =
                    DevExpress.Utils.HorzAlignment.Center;

                Controls.Add(summary);

                notice.AutoSizeMode = LabelAutoSizeMode.None;
                notice.Appearance.Font = ProfileStyle.Font(9F);
                notice.Appearance.Options.UseFont = true;
                notice.Appearance.TextOptions.HAlignment =
                    DevExpress.Utils.HorzAlignment.Center;
                notice.Visible = false;

                Controls.Add(notice);

                BuildGrid();

                Controls.Add(grid);

                backButton.Text = "Back to Reset";

                backButton.Click += (s, e) =>
                {
                    Action handler = BackRequested;

                    if (handler != null)
                        handler();
                };

                Controls.Add(backButton);

                launchButton.Text = "Launch";

                launchButton.Appearance.Font =
                    ProfileStyle.Font(10F, FontStyle.Bold);

                launchButton.Appearance.Options.UseFont = true;

                launchButton.Click += (s, e) =>
                {
                    Action handler = LaunchRequested;

                    if (handler != null)
                        handler();
                };

                Controls.Add(launchButton);
            }
            finally
            {
                ResumeLayout(true);
            }

            ApplyTheme();
        }

        private void BuildGrid()
        {
            grid.MainView = view;

            grid.DataSource = rows;

            view.GridControl = grid;

            view.OptionsBehavior.Editable = false;

            view.OptionsView.ShowGroupPanel = false;

            view.OptionsView.ShowIndicator = false;

            view.OptionsSelection.EnableAppearanceFocusedRow = false;

            // Columns are declared rather than populated from the
            // type. PopulateColumns against an empty list creates
            // nothing, and the grid then invents its own visible
            // columns the moment rows arrive, which showed raw byte
            // counts and an internal Skipped flag.
            view.OptionsBehavior.AutoPopulateColumns = false;

            Add("Category", "Type", 120, false);
            Add("Path", "Location", 400, false);
            Add("FilesDeleted", "Files", 70, true);
            Add("BytesFreed", "Freed", 90, true);
            Add("Note", "Note", 210, false);

            // Raw byte counts are unreadable at a glance, so this
            // column is formatted the same way as the headline.
            view.CustomColumnDisplayText += (s, e) =>
            {
                if (e.Column != null &&
                    e.Column.FieldName == "BytesFreed" &&
                    e.Value is long)
                {
                    long value = (long)e.Value;

                    e.DisplayText =
                        value <= 0
                            ? string.Empty
                            : ClientResetRunner.FormatBytes(value);
                }
            };

            // A skipped location has nothing to celebrate, so it is
            // drawn muted rather than sitting level with real results.
            view.RowStyle += (s, e) =>
            {
                if (e.RowHandle < 0)
                    return;

                ResetLogEntry entry =
                    view.GetRow(e.RowHandle) as ResetLogEntry;

                if (entry != null && entry.Skipped)
                    e.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            };
        }

        private void Add(
            string field,
            string caption,
            int width,
            bool rightAlign)
        {
            GridColumn column = view.Columns.AddField(field);

            column.Caption = caption;
            column.Visible = true;
            column.Width = width;
            column.VisibleIndex = view.Columns.Count - 1;

            column.OptionsColumn.AllowEdit = false;

            column.SortMode =
                DevExpress.XtraGrid.ColumnSortMode.Value;

            if (!rightAlign)
                return;

            column.AppearanceCell.TextOptions.HAlignment =
                DevExpress.Utils.HorzAlignment.Far;

            column.AppearanceCell.Options.UseTextOptions = true;
        }

        //--------------------------------------------------------------
        // Binding
        //--------------------------------------------------------------

        public void Bind(
            ResetReport report)
        {
            if (report == null)
                return;

            headline.Text =
                "Freed " + ClientResetRunner.FormatBytes(report.BytesFreed);

            List<ResetLogEntry> cleared =
                report.Log.Where(x => !x.Skipped).ToList();

            summary.Text = string.Format(
                "{0:N0} files removed from {1} location{2} in {3:0.0}s",
                report.FilesDeleted,
                cleared.Count,
                cleared.Count == 1 ? string.Empty : "s",
                report.Duration.TotalSeconds);

            if (report.NeededAdmin)
            {
                notice.Text =
                    "The Windows Update cache was skipped. Run Nexus " +
                    "Launcher as administrator to include it.";

                notice.Visible = true;
            }
            else if (report.FilesFailed > 0)
            {
                notice.Text =
                    report.FilesFailed.ToString("N0") +
                    " files were in use and could not be removed. " +
                    "That is normal and they will go on the next reset.";

                notice.Visible = true;
            }
            else
            {
                notice.Visible = false;
            }

            launchButton.Text =
                string.IsNullOrWhiteSpace(report.ClientName)
                    ? "Launch"
                    : "Launch " + report.ClientName;

            rows.RaiseListChangedEvents = false;

            rows.Clear();

            // Biggest win first; skipped entries sink to the bottom.
            foreach (ResetLogEntry entry in report.Log
                .OrderBy(x => x.Skipped)
                .ThenByDescending(x => x.BytesFreed))
            {
                rows.Add(entry);
            }

            rows.RaiseListChangedEvents = true;

            rows.ResetBindings();

            ApplyTheme();

            Relayout();
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
            const int pad = 28;

            int width = ClientSize.Width;

            int inner = Math.Max(200, width - pad * 2);

            headline.SetBounds(pad, 26, inner, 40);

            summary.SetBounds(pad, 72, inner, 20);

            int top = 100;

            if (notice.Visible)
            {
                notice.SetBounds(pad, top, inner, 18);

                top += 26;
            }

            int buttonRow =
                Math.Max(top + 120, ClientSize.Height - 60);

            grid.SetBounds(
                pad,
                top,
                inner,
                Math.Max(80, buttonRow - top - 16));

            launchButton.SetBounds(
                width - pad - 170,
                buttonRow,
                170,
                34);

            backButton.SetBounds(
                width - pad - 170 - 140 - 10,
                buttonRow,
                140,
                34);
        }

        public void ApplyTheme()
        {
            // Without this the control keeps the default light grey,
            // and light theme text on it is invisible.
            BackColor = ProfileStyle.CardColor;

            headline.Appearance.ForeColor = ProfileStyle.AccentColor;
            headline.Appearance.Options.UseForeColor = true;

            summary.Appearance.ForeColor = ProfileStyle.TextColor;
            summary.Appearance.Options.UseForeColor = true;

            notice.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            notice.Appearance.Options.UseForeColor = true;
        }
    }
}
