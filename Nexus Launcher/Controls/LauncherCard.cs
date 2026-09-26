using DevExpress.XtraEditors;
using HorizonUI;
using Nexus_Launcher.Services;
using Nexus_Launcher.Services.Library;
using System.Threading;
using Nexus_Launcher.Forms;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls
{
    
    public partial class LauncherCard : XtraUserControl
    {
        readonly addRemoveForm addRemove = new addRemoveForm();

        /// <summary>
        /// Shown over the reset screen once a run finishes.
        /// </summary>
        private readonly ClientResetResultsView resetResults =
            new ClientResetResultsView();

        /// <summary>
        /// The reset screen's own controls, hidden while the results
        /// are up and restored on the way back.
        /// </summary>
        private readonly List<Control> resetScreenControls =
            new List<Control>();

        /// <summary>
        /// What the reset will remove, as checkboxes, beside the step
        /// indicators.
        /// </summary>
        private readonly ClientResetOptionsPanel resetOptions =
            new ClientResetOptionsPanel();

        /// <summary>
        /// Where the designer put the reset screen's centred column,
        /// kept so it can be re-centred in whatever room the options
        /// panel leaves rather than drifting a little further each
        /// time the tab is resized.
        /// </summary>
        private readonly Dictionary<Control, int> resetColumnLeft =
            new Dictionary<Control, int>();

        /// <summary>
        /// Client Settings for launchers with no control of their own.
        /// Sits behind the EA and GOG controls, which cover it when
        /// they are shown, so no extra switching is needed.
        /// </summary>
        private readonly ClientSettingsControl clientSettings =
            new ClientSettingsControl();

        /// <summary>
        /// The shared block on EA's and GOG's own General Settings
        /// tabs, so all three show the same options.
        /// </summary>
        private readonly ClientGeneralSettings gogGeneral =
            new ClientGeneralSettings();

        private readonly ClientGeneralSettings eaGeneral =
            new ClientGeneralSettings();

        private CancellationTokenSource resetCancellation;

        private bool resetRunning;
        public GOGSettings gogSettings = new GOGSettings();
        public EASettings eaSettings = new EASettings();    
        public bool isNexusLauncher { get; set; }
        public string _selectedGroup { get; set; }
        public string clientName
        {
            get => labelControl1.Text;
            set => labelControl1.Text = value;
        }
        public Image clientIcon
        {
            get => pictureEdit1.Image;
            set => pictureEdit1.Image = value;
        }
        public LauncherCard()
        {
            InitializeComponent();
        }

        private void LauncherCard_Load(object sender, EventArgs e)
        {
            FontManager.ApplyFont(
    this,
    Settings.Default.UIFont);
            addRemove.Dock = DockStyle.Fill;
            gogSettings.Dock = DockStyle.Fill;
            eaSettings.Dock = DockStyle.Fill;
            xtraTabPage3.Controls.Add(addRemove);
            xtraTabPage2.Controls.Add(gogSettings);
            xtraTabPage2.Controls.Add(eaSettings);

            BuildResetScreen();

            BuildClientSettings();
            SetSettingsOwner(null);

            addRemove.Show();
        }
        /// <summary>
        /// Which client's own settings control is on show, or null for
        /// the generic one.
        ///
        /// Tracked rather than read back from Visible: a control on an
        /// unselected tab page always reports itself invisible, so the
        /// flags cannot be used to work out what should be showing.
        /// </summary>
        private string settingsOwner;

        /// <summary>
        /// Shows exactly one settings control. All three are docked
        /// Fill in the same tab, so leaving the choice to the z-order
        /// is not reliable.
        /// </summary>
        private void SetSettingsOwner(
            string owner)
        {
            settingsOwner = owner;

            gogSettings.Visible = owner == "GOG";

            eaSettings.Visible = owner == "EA";

            clientSettings.Visible = owner == null;

            if (owner == "GOG")
                gogSettings.BringToFront();
            else if (owner == "EA")
                eaSettings.BringToFront();
            else
                clientSettings.BringToFront();
        }

        public void ShowGOGSettings()
        {
            SetSettingsOwner("GOG");
        }

        public void HideGOGSettings()
        {
            if (_selectedGroup != "GOG" && settingsOwner == "GOG")
                SetSettingsOwner(null);
        }

        public void ShowEASettings()
        {
            SetSettingsOwner("EA");
        }

        public void HideEASettings()
        {
            if (_selectedGroup != "EA" &&
                _selectedGroup != "EA App" &&
                settingsOwner == "EA")
            {
                SetSettingsOwner(null);
            }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (isNexusLauncher)
            {
                // Do something specific for Nexus Launcher
                xtraTabPage3.PageVisible = true;
            }
            else
            {
                // Do something else for other launchers
                xtraTabPage3.PageVisible = false;
            }
        }

        private void labelControl1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void xtraTabControl1_StyleChanged(object sender, EventArgs e)
        {
                
        }

        //--------------------------------------------------------------
        // Client Reset
        //--------------------------------------------------------------

        /// <summary>
        /// Puts the results view on the reset tab, hidden, and records
        /// which controls make up the reset screen so the two can be
        /// swapped without rebuilding either.
        /// </summary>
        private void BuildResetScreen()
        {
            // Docked before the list is taken, so it hides and comes
            // back with the rest of the reset screen.
            resetOptions.Dock = DockStyle.Left;

            resetOptions.Width = 296;

            resetOptions.SelectionChanged += UpdateResetButton;

            xtraTabPage1.Controls.Add(resetOptions);

            resetOptions.ShowPlan(
                ClientResetDefinitions.GetPlan(clientName));

            resetScreenControls.Clear();

            foreach (Control control in xtraTabPage1.Controls)
                resetScreenControls.Add(control);

            resetResults.Dock = DockStyle.Fill;

            resetResults.Visible = false;

            resetResults.BackRequested += ShowResetScreen;

            resetResults.LaunchRequested += LaunchClient;

            xtraTabPage1.Controls.Add(resetResults);

            // The designer sized this label for the word "Stage". Stage
            // names plus the folder being cleared are far longer, so it
            // spans the tab and trims instead of running off the edge.
            labelControl2.AutoSizeMode = LabelAutoSizeMode.None;

            labelControl2.Appearance.TextOptions.HAlignment =
                DevExpress.Utils.HorzAlignment.Center;

            labelControl2.Appearance.TextOptions.Trimming =
                DevExpress.Utils.Trimming.EllipsisCharacter;

            labelControl2.Appearance.Options.UseTextOptions = true;

            // Anchor None drifts these on every resize, which fights
            // the re-centring below.
            foreach (Control control in ResetColumn)
            {
                control.Anchor =
                    AnchorStyles.Top | AnchorStyles.Left;

                resetColumnLeft[control] = control.Left;
            }

            labelControl2.Anchor =
                AnchorStyles.Top | AnchorStyles.Left;

            xtraTabPage1.Resize += (s, e) => LayoutResetScreen();

            LayoutResetScreen();

            ResetSteps();

            UpdateResetButton();
        }

        /// <summary>
        /// A reset with nothing ticked would close the client and then
        /// delete nothing, so the button says so instead.
        /// </summary>
        private void UpdateResetButton()
        {
            if (resetRunning)
                return;

            simpleButton1.Enabled = resetOptions.HasSelection;

            simpleButton1.ToolTip =
                resetOptions.HasSelection
                    ? null
                    : "Tick at least one thing to remove.";
        }

        /// <summary>
        /// Gives every client the same Client Settings tabs.
        ///
        /// EA and GOG keep their own controls, which already have a
        /// General Settings and an Add / Remove tab and real library
        /// folder handling behind the second one. Everything else gets
        /// the generic control, which has the same two tabs with Add /
        /// Remove disabled.
        ///
        /// The auto launch toggle is the same block in all three, so
        /// it reads the same wherever it is seen.
        /// </summary>
        private void BuildClientSettings()
        {
            // The placeholder is replaced by real settings now.
            labelControl3.Visible = false;

            gogGeneral.Dock = DockStyle.Top;
            gogGeneral.Height = 78;

            gogSettings.GeneralSettingsPage.Controls.Add(gogGeneral);

            gogGeneral.BringToFront();

            eaGeneral.Dock = DockStyle.Top;
            eaGeneral.Height = 78;

            eaSettings.GeneralSettingsPage.Controls.Add(eaGeneral);

            eaGeneral.BringToFront();

            clientSettings.Dock = DockStyle.Fill;

            xtraTabPage2.Controls.Add(clientSettings);

            clientSettings.ClientName = clientName;

        }

        /// <summary>
        /// The logo, name, step indicators and Reset button, in the
        /// order the designer laid them out.
        /// </summary>
        private Control[] ResetColumn
        {
            get
            {
                return new Control[]
                {
                    pictureEdit1,
                    tableLayoutPanel1,
                    stepProgress1,
                    stepProgress2,
                    stepProgress3,
                    stepProgress4,
                    simpleButton1
                };
            }
        }

        /// <summary>
        /// Re-centres the reset screen in the room left of the options
        /// panel, and spans the stage label across it so long text
        /// trims in the middle rather than overflowing right.
        /// </summary>
        private void LayoutResetScreen()
        {
            if (resetColumnLeft.Count == 0)
                return;

            int left =
                resetOptions.Width;

            int available =
                xtraTabPage1.ClientSize.Width - left;

            if (available < 200)
                return;

            // The column is moved as one piece, so the pieces keep the
            // relationship the designer gave them.
            int designerLeft = int.MaxValue;

            int designerRight = int.MinValue;

            foreach (Control control in ResetColumn)
            {
                int start = resetColumnLeft[control];

                designerLeft = Math.Min(designerLeft, start);

                designerRight =
                    Math.Max(designerRight, start + control.Width);
            }

            int span =
                designerRight - designerLeft;

            // Centred while it fits. When it does not, it is pinned
            // beside the panel and allowed to run off to the right,
            // because the alternative is drawing it over the panel.
            int offset =
                Math.Max(0, (available - span) / 2);

            int delta =
                left + offset - designerLeft;

            foreach (Control control in ResetColumn)
            {
                control.Left =
                    resetColumnLeft[control] + delta;
            }

            labelControl2.SetBounds(
                left + 24,
                labelControl2.Top,
                Math.Max(200, available - 48),
                labelControl2.Height);
        }

        private HorizonUI.StepProgress[] Steps
        {
            get
            {
                return new[]
                {
                    stepProgress1,
                    stepProgress2,
                    stepProgress3,
                    stepProgress4
                };
            }
        }

        private void ResetSteps()
        {
            foreach (HorizonUI.StepProgress step in Steps)
            {
                step.Value = 0;

                step.HideLoading = false ;
            }
            stepProgress4.Size = new Size(32,32);
            labelControl2.Text = "Ready";
        }

        private void ShowResetScreen()
        {
            resetResults.Visible = false;

            foreach (Control control in resetScreenControls)
                control.Visible = true;

            resetOptions.Enabled = true;

            ResetSteps();

            UpdateResetButton();
        }

        private void ShowResults(
            ResetReport report)
        {
            foreach (Control control in resetScreenControls)
                control.Visible = false;

            resetResults.Bind(report);

            resetResults.Visible = true;

            resetResults.BringToFront();
        }

        private void LaunchClient()
        {
            try
            {
                LauncherStartupService.StartLauncher(clientName);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                XtraMessageBox.Show(
                    FindForm(),
                    "Nexus Launcher could not start " + clientName +
                        "." + Environment.NewLine + Environment.NewLine +
                        ex.Message,
                    "Launch",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private async void simpleButton1_Click(object sender, EventArgs e)
        {
            if (resetRunning)
                return;

            string launcher = clientName;

            ClientResetPlan plan =
                ClientResetDefinitions.GetPlan(launcher);

            if (plan == null)
            {
                XtraMessageBox.Show(
                    FindForm(),
                    "Nexus Launcher does not have a reset definition " +
                        "for " + launcher + " yet.",
                    "Client Reset",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            // The panel describes this client, so what it offers is
            // known only once a plan exists.
            resetOptions.ShowPlan(plan);

            UpdateResetButton();

            if (!resetOptions.HasSelection)
            {
                XtraMessageBox.Show(
                    FindForm(),
                    "Nothing is ticked, so a reset would not remove " +
                        "anything." + Environment.NewLine +
                        Environment.NewLine +
                        "Choose what to remove on the left first.",
                    "Client Reset",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            resetOptions.Apply(plan);

            // Deleting caches cannot be undone, and the client is
            // closed to unlock its files, so this is always confirmed.
            // The list is the user's own ticks, not a fixed sentence:
            // it would otherwise describe a reset they did not ask for.
            string message =
                "Reset " + launcher + "?" +
                Environment.NewLine + Environment.NewLine +
                "This closes " + launcher + " and removes:" +
                Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    resetOptions.SelectedNames
                        .Select(x => "    \u2022  " + x)
                        .ToArray()) +
                Environment.NewLine + Environment.NewLine +
                "Your games and your sign in are not touched.";

            if (plan.ClearWindowsUpdateCache &&
                !ClientResetRunner.IsElevated)
            {
                message +=
                    Environment.NewLine + Environment.NewLine +
                    "Nexus is not running as administrator, so the " +
                    "Windows Update cache will be skipped.";
            }

            if (XtraMessageBox.Show(
                FindForm(),
                message,
                "Client Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            resetRunning = true;

            simpleButton1.Enabled = false;

            // Changing the ticks mid-run would not affect the plan that
            // is already running, so it would only mislead.
            resetOptions.Enabled = false;

            ResetSteps();

            resetCancellation = new CancellationTokenSource();

            ResetReport report = null;

            try
            {
                Progress<ResetProgress> progress =
                    new Progress<ResetProgress>(OnResetProgress);

                report =
                    await ClientResetRunner.RunAsync(
                        plan,
                        progress,
                        resetCancellation.Token);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                XtraMessageBox.Show(
                    FindForm(),
                    "The reset did not finish." +
                        Environment.NewLine + Environment.NewLine +
                        ex.Message,
                    "Client Reset",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                resetRunning = false;

                resetCancellation.Dispose();

                resetCancellation = null;
            }

            if (report == null)
            {
                resetOptions.Enabled = true;

                UpdateResetButton();

                return;
            }

            ShowResults(report);

            if (Settings.Default.AutoLaunchAfterReset)
                LaunchClient();
        }

        /// <summary>
        /// Drives the four step indicators. Steps behind the current
        /// one read as complete, the current one fills, and the ones
        /// ahead stay empty.
        /// </summary>
        private void OnResetProgress(
            ResetProgress progress)
        {
            if (IsDisposed || progress == null)
                return;

            labelControl2.Text =
                string.IsNullOrEmpty(progress.Detail)
                    ? progress.StageName
                    : progress.StageName + " - " + progress.Detail;

            int index = (int)progress.Stage;

            HorizonUI.StepProgress[] steps = Steps;

            for (int i = 0; i < steps.Length; i++)
            {
                if (i < index)
                    steps[i].Value = steps[i].Maximum;
                else if (i > index)
                    steps[i].Value = steps[i].Minimum;
                else
                {
                    int span = steps[i].Maximum - steps[i].Minimum;

                    steps[i].Value =
                        steps[i].Minimum +
                        (int)Math.Round(span * progress.Fraction);
                }
            }
        }
    }
}
