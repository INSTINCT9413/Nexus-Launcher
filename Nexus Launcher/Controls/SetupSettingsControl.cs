using DevExpress.XtraEditors;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services;
using Nexus_Launcher.Services.Account;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls
{
    /// <summary>
    /// The settings step of the first run wizard.
    ///
    /// Replaces embedding the whole SettingsForm and hiding most of it.
    /// Only the choices that matter on a first run are here, plus the
    /// name Nexus should use. Nothing is written until Save is called,
    /// so backing out of the wizard changes nothing.
    /// </summary>
    public partial class SetupSettingsControl : XtraUserControl
    {
        /// <summary>
        /// Must match the values MainView compares DefaultLauncher
        /// against, and the list in SettingsForm.
        /// </summary>
        private static readonly string[] Launchers =
        {
            "Battle.net",
            "EA App",
            "Epic Games",
            "GOG Galaxy",
            "Nexus Launcher",
            "Steam Client",
            "Ubisoft Connect"
        };

        private const string Icon = "svgimages/icon%20builder/";

        private TextEdit nameEdit;
        private CheckEdit useWindowsName;

        private CheckEdit useOnlineName;

        private LabelControl accountStatus;

        private SimpleButton signInButton;

        private SimpleButton registerButton;
        private CheckEdit autoStart;
        private CheckEdit startMinimized;
        private CheckEdit startMaximized;
        private CheckEdit minimizeOnClose;
        private CheckEdit showFullLibrary;
        private CheckEdit rememberSidePanel;
        private ComboBoxEdit defaultLauncher;

        public SetupSettingsControl()
        {
            Build();
            LoadValues();
        }

        //--------------------------------------------------------------
        // Layout
        //--------------------------------------------------------------

        private void Build()
        {
            Padding = new Padding(2);

            TableLayoutPanel columns =
                new TableLayoutPanel();

            columns.Dock = DockStyle.Fill;
            columns.ColumnCount = 2;
            columns.RowCount = 1;
            columns.BackColor = Color.Transparent;
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Panel left = Column();
            Panel right = Column();

            columns.Controls.Add(left, 0, 0);
            columns.Controls.Add(right, 1, 0);

            // Left: who you are, then how Nexus starts.
            nameEdit = new TextEdit();
            nameEdit.Dock = DockStyle.Top;
            nameEdit.Height = 24;
            nameEdit.Margin = new Padding(0, 4, 0, 0);

            useWindowsName = Check("Use my Windows account name");
            useWindowsName.CheckedChanged += (s, e) => UpdateNameState();

            CardPanel nameCard = new CardPanel();

            // Only meaningful once an account is linked, so it is
            // hidden until then rather than sitting there greyed out.
            useOnlineName = Check("Use my Nexus account name");
            useOnlineName.CheckedChanged += (s, e) => UpdateNameState();

            CardStack.Fill(nameCard, new List<Control>
            {
                new SectionTitle("Your name", Icon + "actions_user.svg"),
                Hint("Nexus greets you by this name."),
                useOnlineName,
                useWindowsName,
                nameEdit
            });

            // An account is optional. Signing in fills the name in from
            // the site; skipping it leaves the local name above, which
            // is all Nexus needs to run.
            CardPanel accountCard = new CardPanel();

            accountStatus = Hint(string.Empty);

            signInButton = new SimpleButton();
            signInButton.Text = "Sign in";
            signInButton.Dock = DockStyle.Top;
            signInButton.Height = 28;
            signInButton.Click += SignIn_Click;

            registerButton = new SimpleButton();
            registerButton.Text = "Create an account";
            registerButton.Dock = DockStyle.Top;
            registerButton.Height = 28;
            registerButton.Click += Register_Click;

            CardStack.Fill(accountCard, new List<Control>
            {
                new SectionTitle(
                    "Nexus Account",
                    Icon + "actions_user.svg"),
                Hint("Optional. Sign in to bring your online profile " +
                     "into Nexus, or just use a local name below."),
                accountStatus,
                signInButton,
                registerButton
            });

            autoStart = Check("Start Nexus when Windows starts");
            startMinimized = Check("Start minimised to the tray");
            startMaximized = Check("Start maximised");
            minimizeOnClose = Check("Close to the tray instead of exiting");

            CardPanel startupCard = new CardPanel();

            CardStack.Fill(startupCard, new List<Control>
            {
                new SectionTitle("Startup", Icon + "actions_clock.svg"),
                autoStart,
                startMinimized,
                startMaximized,
                minimizeOnClose
            });

            Stack(left, accountCard, nameCard, startupCard);

            // Right: what Nexus opens on.
            defaultLauncher = new ComboBoxEdit();
            defaultLauncher.Dock = DockStyle.Top;
            defaultLauncher.Height = 24;
            defaultLauncher.Margin = new Padding(0, 4, 0, 0);
            defaultLauncher.Properties.TextEditStyle =
                DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            defaultLauncher.Properties.Items.AddRange(Launchers);

            showFullLibrary = Check("Show the Full Library page");
            rememberSidePanel = Check("Remember the side panel state");

            CardPanel libraryCard = new CardPanel();

            CardStack.Fill(libraryCard, new List<Control>
            {
                new SectionTitle("Library", Icon + "shopping_box.svg"),
                Hint("Which launcher Nexus opens on."),
                defaultLauncher,
                showFullLibrary,
                rememberSidePanel
            });

            Stack(right, libraryCard);

            Controls.Add(columns);
        }

        private static Panel Column()
        {
            Panel panel =
                new Panel();

            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.Transparent;
            panel.Margin = new Padding(6);

            return panel;
        }

        /// <summary>
        /// Stacks cards down a column. Added back to front because
        /// WinForms docks the last added control to the top first.
        /// </summary>
        private void SignIn_Click(
            object sender,
            EventArgs e)
        {
            using (Nexus_Launcher.Forms.NexusAccountDialog dialog =
                new Nexus_Launcher.Forms.NexusAccountDialog())
            {
                dialog.ShowDialog(FindForm());
            }

            AdoptAccountName();

            UpdateAccountState();
        }

        private void Register_Click(
            object sender,
            EventArgs e)
        {
            using (Nexus_Launcher.Forms.NexusAccountWebForm web =
                new Nexus_Launcher.Forms.NexusAccountWebForm(
                    Nexus_Launcher.Forms.NexusAccountWebMode.Register))
            {
                web.ShowDialog(FindForm());
            }

            // Registering does not sign anyone in, so the buttons stay
            // as they were and Sign in is the next step.
            UpdateAccountState();
        }

        /// <summary>
        /// Uses the account's name for the local greeting too, so a
        /// signed in user does not have to type it twice. Only fills a
        /// blank box: a name already typed is the user's choice.
        /// </summary>
        private void AdoptAccountName()
        {
            if (!NexusAccountService.IsSignedIn)
                return;

            NexusAccount user =
                NexusAccountService.Current;

            string name =
                user == null ? null : user.FullName;

            if (string.IsNullOrWhiteSpace(name))
                return;

            if (!string.IsNullOrWhiteSpace(nameEdit.Text))
                return;

            useWindowsName.Checked = false;

            nameEdit.Text = name;

            UpdateNameState();
        }

        private void UpdateAccountState()
        {
            bool signedIn =
                NexusAccountService.IsSignedIn;

            NexusAccount user =
                NexusAccountService.Current;

            accountStatus.Text =
                signedIn && user != null
                    ? "Signed in as " +
                        (string.IsNullOrWhiteSpace(user.DisplayName)
                            ? user.Username
                            : user.DisplayName)
                    : "Not signed in. Nexus works fine without an account.";

            signInButton.Text =
                signedIn ? "Account settings" : "Sign in";

            registerButton.Visible = !signedIn;

            if (useOnlineName != null)
            {
                bool appeared =
                    signedIn && !useOnlineName.Visible;

                useOnlineName.Visible = signedIn;

                // Default to the account's name the moment one is
                // linked, which is what someone signing in expects.
                if (appeared)
                    useOnlineName.Checked = UserProfileService.UseOnlineName;

                UpdateNameState();
            }
        }

        /// <summary>
        /// A card's height is worked out when it is filled, from the
        /// rows as they measured then. A wrapping hint does not know
        /// its height until it has been given a width, so the cards are
        /// measured again once the layout has settled.
        /// </summary>
        protected override void OnLayout(
            LayoutEventArgs e)
        {
            base.OnLayout(e);

            ResizeCards(this);
        }

        private static void ResizeCards(
            Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                CardPanel card =
                    child as CardPanel;

                if (card != null)
                {
                    int wanted =
                        card.Padding.Vertical;

                    foreach (Control row in card.Controls)
                    {
                        if (row.Visible)
                            wanted += row.Height;
                    }

                    if (card.Height != wanted)
                        card.Height = wanted;
                }

                ResizeCards(child);
            }
        }

        private static void Stack(
            Panel column,
            params Control[] cards)
        {
            // Back to front, and with a spacer between each card: a
            // docked control ignores its Margin, so the cards would
            // otherwise sit flush against one another.
            for (int i = cards.Length - 1; i >= 0; i--)
            {
                if (i < cards.Length - 1)
                {
                    Panel gap = new Panel();
                    gap.Dock = DockStyle.Top;
                    gap.Height = 10;
                    gap.BackColor = Color.Transparent;
                    column.Controls.Add(gap);
                }

                cards[i].Dock = DockStyle.Top;
                column.Controls.Add(cards[i]);
            }
        }

        private static CheckEdit Check(
            string caption)
        {
            CheckEdit check =
                new CheckEdit();

            check.Text = caption;
            check.Dock = DockStyle.Top;
            check.Height = 24;

            return check;
        }

        private static LabelControl Hint(
            string text)
        {
            MutedLabel label =
                new MutedLabel();

            label.Text = text;
            label.Dock = DockStyle.Top;

            // Wraps and grows instead of running past the card edge.
            // The cards are narrow and these hints are sentences.
            label.AutoSizeMode = LabelAutoSizeMode.Vertical;

            label.Appearance.TextOptions.WordWrap =
                DevExpress.Utils.WordWrap.Wrap;

            label.Appearance.Options.UseTextOptions = true;

            label.Height = 20;
            label.Appearance.Font = ProfileStyle.Font(8.5F);
            label.Appearance.Options.UseFont = true;

            return label;
        }

        //--------------------------------------------------------------
        // Values
        //--------------------------------------------------------------

        private void LoadValues()
        {
            useWindowsName.Checked = UserProfileService.UseWindowsName;

            useOnlineName.Visible =
                UserProfileService.HasOnlineAccount;

            useOnlineName.Checked =
                UserProfileService.HasOnlineAccount &&
                UserProfileService.UseOnlineName;

            UpdateAccountState();

            nameEdit.Text =
                string.IsNullOrWhiteSpace(UserProfileService.CustomName)
                    ? UserProfileService.WindowsName
                    : UserProfileService.CustomName;

            autoStart.Checked = Settings.Default.AutoStart;
            startMinimized.Checked = Settings.Default.StartMinimized;
            startMaximized.Checked = Settings.Default.StartMaximized;
            minimizeOnClose.Checked = Settings.Default.MinimizeOnClose;

            showFullLibrary.Checked = Settings.Default.enableFullLibrary;
            rememberSidePanel.Checked = Settings.Default.SidePanelRemember;

            defaultLauncher.EditValue =
                string.IsNullOrWhiteSpace(Settings.Default.DefaultLauncher)
                    ? "Nexus Launcher"
                    : Settings.Default.DefaultLauncher;

            UpdateNameState();
        }

        /// <summary>
        /// The box is only editable when a custom name is wanted; it
        /// shows the Windows name otherwise so the choice is visible.
        /// </summary>
        private void UpdateNameState()
        {
            bool online =
                useOnlineName.Visible && useOnlineName.Checked;

            // The account name wins while it is selected, so the local
            // options below it are switched off rather than looking
            // like they still apply.
            useWindowsName.Enabled = !online;

            bool windows =
                useWindowsName.Checked;

            nameEdit.Enabled = !online && !windows;

            if (online)
                nameEdit.Text = UserProfileService.OnlineName;
            else if (windows)
                nameEdit.Text = UserProfileService.WindowsName;
        }

        /// <summary>
        /// Writes every choice. Called when the wizard finishes.
        /// </summary>
        public void Save()
        {
            Settings.Default.AutoStart = autoStart.Checked;
            Settings.Default.StartMinimized = startMinimized.Checked;
            Settings.Default.StartMaximized = startMaximized.Checked;
            Settings.Default.MinimizeOnClose = minimizeOnClose.Checked;

            Settings.Default.enableFullLibrary = showFullLibrary.Checked;
            Settings.Default.SidePanelRemember = rememberSidePanel.Checked;

            if (defaultLauncher.EditValue != null)
            {
                Settings.Default.DefaultLauncher =
                    defaultLauncher.EditValue.ToString();
            }

            UserProfileService.SaveUseOnlineName(
                useOnlineName.Visible && useOnlineName.Checked);

            Settings.Default.Save();

            // Saves the name and tells the rest of the app to redraw.
            UserProfileService.Save(
                useWindowsName.Checked,
                nameEdit.Text);
        }
    }
}
