using DevExpress.Printing.ExportHelpers;
using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.Utils.Menu;
using DevExpress.Utils.Svg;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraPrinting;
using DevExpress.XtraSplashScreen;
using Microsoft.Win32;
using Nexus_Launcher.Controls;
using Nexus_Launcher.Forms;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Models;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services;
using Nexus_Launcher.Services.Artwork;
using Nexus_Launcher.Services.Themes;
using Nexus_Launcher.Services.Update;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher
{
    public partial class SettingsForm : DevExpress.XtraEditors.XtraForm
    {
        public int TabSelected { get; set; } = 0;
        private bool MinimizeOnClose { get; set; }
        private bool AutoStart { get; set; }
        private bool StartMinimized { get; set; }

        private bool ShowSteam { get; set; }
        private bool ShowEpic { get; set; }
        private bool ShowGOG { get; set; }
        private bool ShowEA { get; set; }
        private bool ShowUbisoft { get; set; }
        private bool ShowParadox { get; set; }
        private bool ShowXbox { get; set; }
        private bool ShowWindowsStore { get; set; }
        private bool ShowWargaming { get; set; }
        private bool ShowBattleNet { get; set; }
        private bool ShowAmazon { get; set; }
        // Image lists to store the swatches/icons
        private ImageList skinImages = new ImageList();
        private ImageList paletteImages = new ImageList();
        public MainView _mainview;
        public SettingsForm(MainView mainView)
        {
            InitializeComponent();
            this.Shown += SettingsForm_Shown;
            // Setup ImageLists for the dropdowns
            // FIX: Set ImageSize on the ImageLists, not the ComboBox controls
            skinImages.ImageSize = new Size(16, 16);
            paletteImages.ImageSize = new Size(16, 16);

            comboBoxEdit1.Properties.SmallImages = skinImages;
            comboBoxEdit2.Properties.SmallImages = paletteImages;

            // Initial populate skins
            PopulateSkins();
            // Initial palette load
            UpdatePaletteList();

            BindCustomThemingToggle(checkEdit7);
            AttachCustomThemeMenu(dropDownButton1);

            _mainview = mainView;
        }

        public SettingsForm()
        {
            InitializeComponent();
            this.Shown += SettingsForm_Shown;
            // Setup ImageLists for the dropdowns
            // FIX: Set ImageSize on the ImageLists, not the ComboBox controls
            skinImages.ImageSize = new Size(16, 16);
            paletteImages.ImageSize = new Size(16, 16);

            comboBoxEdit1.Properties.SmallImages = skinImages;
            comboBoxEdit2.Properties.SmallImages = paletteImages;

            // Initial populate skins
            PopulateSkins();
            // Initial palette load
            UpdatePaletteList();

            BindCustomThemingToggle(checkEdit7);
            AttachCustomThemeMenu(dropDownButton1);

            //_mainview = mainView;
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            FontManager.ApplyFont(
    this,
    Settings.Default.UIFont);
            try
            {
                labelControl17.Text = $"Version: {BuildInfo.Version}";
                labelControl28.Text = $"Build: {InstalledBuild.Current.build}";
                AutoStart = Settings.Default.AutoStart;
                MinimizeOnClose = Settings.Default.MinimizeOnClose;
                StartMinimized = Settings.Default.StartMinimized;
                toggleSwitch1.IsOn = AutoStart;
                toggleSwitch2.IsOn = StartMinimized;
                toggleSwitch3.IsOn = MinimizeOnClose;

                ShowAmazon = Settings.Default.ShowAmazon;
                ShowBattleNet = Settings.Default.ShowBattleNet;
                ShowSteam = Settings.Default.ShowSteam;
                ShowEA = Settings.Default.ShowEA;
                ShowEpic = Settings.Default.ShowEpic;
                ShowGOG = Settings.Default.ShowGOG;
                ShowParadox = Settings.Default.ShowParadox;
                ShowUbisoft = Settings.Default.ShowUbisoft;
                ShowWindowsStore = Settings.Default.ShowWindowsStore;
                ShowXbox = Settings.Default.ShowXbox;
                ShowWargaming = Settings.Default.ShowWargaming;
                toggleSwitch4.IsOn = ShowSteam;

                toggleSwitch5.IsOn = ShowEpic;
                toggleSwitch6.IsOn = ShowBattleNet;
                toggleSwitch7.IsOn = ShowGOG;
                toggleSwitch8.IsOn = ShowUbisoft;
                toggleSwitch9.IsOn = ShowParadox;
                toggleSwitch10.IsOn = ShowAmazon;
                toggleSwitch11.IsOn = ShowEA;
                toggleSwitch12.IsOn = ShowXbox;
                toggleSwitch13.IsOn = ShowWindowsStore;
                toggleSwitch14.IsOn = ShowWargaming;
                toggleSwitch19.IsOn = Settings.Default.StartLaunchers;
                checkEdit1.Checked = Settings.Default.StartBattleNet;
                checkEdit2.Checked = Settings.Default.StartEA;
                checkEdit3.Checked = Settings.Default.StartEpic;
                checkEdit4.Checked = Settings.Default.StartGOG;
                checkEdit5.Checked = Settings.Default.StartUbisoft;
                checkEdit6.Checked = Settings.Default.StartSteam;


                toggleSwitch15.IsOn = Settings.Default.SidePanelRemember;
                toggleSwitch16.IsOn = Settings.Default.HideEANotice;

                comboBoxEdit4.Text = Settings.Default.DefaultLauncher.ToString();
                comboBoxEdit4.SelectedText = Settings.Default.DefaultLauncher.ToString();

                toggleSwitch17.IsOn = Settings.Default.ViewType;
                toggleSwitch18.IsOn = Settings.Default.RootDisplayMode;
                toggleSwitch20.IsOn = Settings.Default.enableFullLibrary;
                toggleSwitch21.IsOn = Settings.Default.StartMaximized;
                labelControl29.Text = "UI Font (" + Settings.Default.UIFont + ")";
                //GetdllInfo();
            }
            catch (Exception)
            {


            }

        }
        private void PopulateSkins()
        {
            comboBoxEdit1.Properties.Items.Clear();
            skinImages.Images.Clear();

            int imageIndex = 0;
            foreach (SkinContainer skin in SkinManager.Default.Skins)
            {
                // Get the internal preview icon for the skin
                Image skinIcon = SkinCollectionHelper.GetSkinIcon(skin.SkinName, SkinIconsSize.Small);
                if (skinIcon != null)
                {
                    skinImages.Images.Add(skin.SkinName, skinIcon);
                }

                // Add item to ImageComboBox (Value, Description, ImageIndex)
                comboBoxEdit1.Properties.Items.Add(new ImageComboBoxItem(skin.SkinName, skin.SkinName, imageIndex));
                imageIndex++;
            }

            comboBoxEdit1.EditValue = DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName;

            // Grey out skins that cannot take a custom palette while
            // custom theming is on. Drawn rather than removed so the
            // user can see they exist and why they are unavailable.
            comboBoxEdit1.Properties.DropDownCustomDrawItem -=
                SkinList_DropDownCustomDrawItem;

            comboBoxEdit1.Properties.DropDownCustomDrawItem +=
                SkinList_DropDownCustomDrawItem;
        }

        private void SkinList_DropDownCustomDrawItem(
            object sender,
            ListBoxDrawItemEventArgs e)
        {
            ImageComboBoxItem item =
                comboBoxEdit1.Properties.Items[e.Index] as ImageComboBoxItem;

            if (item == null)
                return;

            bool unavailable =
                Settings.Default.CustomThemingEnabled &&
                !CustomThemeService.IsPaletteCapable(
                    item.Value as string);

            // The appearance object is reused between draws, so the
            // supported case has to clear the grey rather than just
            // returning. Returning early is what left items stuck grey
            // after custom theming was switched back off.
            if (unavailable)
            {
                e.Appearance.ForeColor =
                    System.Drawing.SystemColors.GrayText;

                e.Appearance.Options.UseForeColor = true;
            }
            else
            {
                e.Appearance.Options.UseForeColor = false;
            }
        }

        /// <summary>
        /// Explains why a bitmap skin cannot be used while custom
        /// theming is on, and offers the two ways out.
        /// Returns true when the skin change should be allowed.
        /// </summary>
        private bool ConfirmUnsupportedSkin(
            string skinName)
        {
            if (!Settings.Default.CustomThemingEnabled ||
                CustomThemeService.IsPaletteCapable(skinName))
            {
                return true;
            }

            string message =
                "\"" + skinName + "\" cannot be used with custom themes." +
                Environment.NewLine + Environment.NewLine +
                "Custom themes work by recolouring a theme's palette. " +
                "This one is drawn from fixed images instead of colours, " +
                "so there is nothing for your colours to change." +
                Environment.NewLine + Environment.NewLine +
                "Themes that support custom colours:" +
                Environment.NewLine +
                CustomThemeService.DescribeCapableSkins() +
                Environment.NewLine + Environment.NewLine +
                "Turn off custom theming to use this theme anyway?";

            DialogResult result =
                XtraMessageBox.Show(
                    this,
                    message,
                    "Theme Not Supported",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

            if (result != DialogResult.Yes)
            {
                // Keep the old skin, they will pick another.
                return false;
            }

            // Untick the box rather than just writing the setting.
            // Its CheckedChanged handler is what saves the setting,
            // clears the greying, updates the theme menu and puts the
            // header Themes menu back, so going straight to Settings
            // left the checkbox still ticked and everything else stale.
            if (customThemingToggle != null)
            {
                customThemingToggle.Checked = false;
            }
            else
            {
                Settings.Default.CustomThemingEnabled = false;
                Settings.Default.Save();
            }

            return true;
        }

        //--------------------------------------------------------------
        // Custom themes
        //--------------------------------------------------------------

        private DXPopupMenu customThemeMenu;
        private DXMenuItem customThemeNew;
        private DXMenuItem customThemeEdit;
        private DXMenuItem customThemeDelete;
        private CheckEdit customThemingToggle;

        /// <summary>
        /// Hangs the new/edit/delete menu off a drop down button.
        /// </summary>
        public void AttachCustomThemeMenu(
            DropDownButton button)
        {
            if (button == null)
                return;

            if (customThemeMenu == null)
                BuildCustomThemeMenu();

            button.DropDownControl = customThemeMenu;

            UpdateCustomThemeMenuState();
        }

        /// <summary>
        /// Binds the on/off switch for custom theming.
        /// </summary>
        public void BindCustomThemingToggle(
            CheckEdit toggle)
        {
            if (toggle == null)
                return;

            customThemingToggle = toggle;

            customThemingToggle.Checked =
                Settings.Default.CustomThemingEnabled;

            customThemingToggle.CheckedChanged -=
                CustomThemingToggle_CheckedChanged;

            customThemingToggle.CheckedChanged +=
                CustomThemingToggle_CheckedChanged;

            UpdateCustomThemeMenuState();
        }

        private void BuildCustomThemeMenu()
        {
            customThemeMenu =
                new DXPopupMenu();

            customThemeNew =
                CreateThemeMenuItem(
                    "New Theme...",
                    "svgimages/icon%20builder/actions_add.svg",
                    (s, e) => NewCustomTheme());

            customThemeEdit =
                CreateThemeMenuItem(
                    "Edit Current Theme...",
                    "svgimages/icon%20builder/actions_edit.svg",
                    (s, e) => EditCustomTheme());

            customThemeDelete =
                CreateThemeMenuItem(
                    "Delete Current Theme",
                    "svgimages/icon%20builder/actions_trash.svg",
                    (s, e) => DeleteCustomTheme());

            customThemeDelete.BeginGroup = true;

            customThemeMenu.Items.Add(customThemeNew);
            customThemeMenu.Items.Add(customThemeEdit);
            customThemeMenu.Items.Add(customThemeDelete);
        }

        private DXMenuItem CreateThemeMenuItem(
            string caption,
            string iconKey,
            EventHandler handler)
        {
            return new DXMenuItem(
                caption,
                handler,
                Nexus_Launcher.Services.Library.LibraryGroupIcons
                    .GetSvgImage(iconKey),
                DXMenuItemPriority.Normal);
        }

        /// <summary>
        /// Everything needs custom theming switched on, and edit and
        /// delete additionally need one of the user's own themes to be
        /// the active palette.
        /// </summary>
        private void UpdateCustomThemeMenuState()
        {
            if (customThemeMenu == null)
                return;

            bool enabled =
                Settings.Default.CustomThemingEnabled;

            bool capableSkin =
                CustomThemeService.IsPaletteCapable(
                    UserLookAndFeel.Default.SkinName);

            customThemeNew.Enabled =
                enabled && capableSkin;

            bool onCustomTheme =
                enabled &&
                CustomThemeService.Find(
                    UserLookAndFeel.Default.ActiveSvgPaletteName) != null;

            customThemeEdit.Enabled = onCustomTheme;
            customThemeDelete.Enabled = onCustomTheme;
        }

        private void CustomThemingToggle_CheckedChanged(
            object sender,
            EventArgs e)
        {
            bool enabled =
                customThemingToggle.Checked;

            Settings.Default.CustomThemingEnabled = enabled;
            Settings.Default.Save();

            // Turning it on while sitting on a skin that cannot take a
            // palette would leave the user stuck, so say so straight
            // away rather than waiting for them to try to create one.
            if (enabled &&
                !CustomThemeService.IsPaletteCapable(
                    UserLookAndFeel.Default.SkinName))
            {
                XtraMessageBox.Show(
                    this,
                    "Custom theming is on, but the current theme \"" +
                    UserLookAndFeel.Default.SkinName +
                    "\" cannot be recoloured." +
                    Environment.NewLine + Environment.NewLine +
                    "Pick one of these to start building a theme:" +
                    Environment.NewLine +
                    CustomThemeService.DescribeCapableSkins(),
                    "Choose a Supported Theme",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            // Repaint the skin list so the greying updates.
            comboBoxEdit1.Refresh();

            UpdateCustomThemeMenuState();

            // The header Themes menu would override a custom theme, so
            // it is hidden while custom theming is on.
            if (_mainview != null)
                _mainview.ApplyCustomThemingVisibility();
        }

        private void NewCustomTheme()
        {
            string skinName =
                UserLookAndFeel.Default.SkinName;

            if (!CustomThemeService.IsPaletteCapable(skinName))
                return;

            using (PaletteEditorDialog editor = new PaletteEditorDialog())
            {
                if (editor.ShowDialog(
                    PaletteEditorMode.Create,
                    UserLookAndFeel.Default) != DialogResult.OK)
                {
                    return;
                }

                ApplyEditedTheme(
                    editor.PaletteName,
                    skinName,
                    editor.Palette);
            }
        }

        private void EditCustomTheme()
        {
            string skinName =
                UserLookAndFeel.Default.SkinName;

            CustomTheme current =
                CustomThemeService.Find(
                    UserLookAndFeel.Default.ActiveSvgPaletteName);

            if (current == null)
                return;

            using (PaletteEditorDialog editor = new PaletteEditorDialog())
            {
                if (editor.ShowDialog(
                    PaletteEditorMode.Update,
                    UserLookAndFeel.Default) != DialogResult.OK)
                {
                    return;
                }

                ApplyEditedTheme(
                    editor.PaletteName,
                    skinName,
                    editor.Palette);
            }
        }

        private void ApplyEditedTheme(
            string paletteName,
            string skinName,
            SvgPalette palette)
        {
            if (string.IsNullOrWhiteSpace(paletteName) || palette == null)
                return;

            CustomThemeService.SaveTheme(
                paletteName,
                skinName,
                palette);

            UserLookAndFeel.Default.SetSkinStyle(
                skinName,
                paletteName);

            ThemeSettingsManager.Save(
                skinName,
                paletteName);

            UpdatePaletteList();

            UpdateCustomThemeMenuState();
        }

        private void DeleteCustomTheme()
        {
            CustomTheme current =
                CustomThemeService.Find(
                    UserLookAndFeel.Default.ActiveSvgPaletteName);

            if (current == null)
                return;

            if (XtraMessageBox.Show(
                this,
                "Delete the theme \"" + current.Name + "\"?",
                "Delete Theme",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            string skinName =
                current.SkinName;

            CustomThemeService.DeleteTheme(
                current.Name);

            // Drop back to the skin's own default palette, otherwise
            // the UI keeps rendering a palette that no longer exists.
            UserLookAndFeel.Default.SetSkinStyle(skinName);

            ThemeSettingsManager.Save(
                skinName,
                UserLookAndFeel.Default.ActiveSvgPaletteName);

            UpdatePaletteList();

            UpdateCustomThemeMenuState();
        }
        private void UpdatePaletteList()
        {
            comboBoxEdit2.Properties.Items.Clear();
            paletteImages.Images.Clear();

            var currentSkin = CommonSkins.GetSkin(DevExpress.LookAndFeel.UserLookAndFeel.Default);

            if (currentSkin.CustomSvgPalettes.Count > 0)
            {
                comboBoxEdit2.Enabled = true;
                int imageIndex = 0;

                foreach (var paletteKeyVal in currentSkin.CustomSvgPalettes)
                {
                    string paletteName = paletteKeyVal.Key.Name;
                    var palette = paletteKeyVal.Value;

                    // FIX: Use SkinCollectionHelper to safely generate the color swatch image for the palette
                    // FIX: Pass the palette object itself, not the strings!
                    Image swatch = DevExpress.XtraBars.Helpers.SkinHelper.GetPalettePreviewImage(paletteKeyVal.Value);

                    if (swatch != null)
                    {
                        paletteImages.Images.Add(paletteName, swatch);
                        comboBoxEdit2.Properties.Items.Add(new ImageComboBoxItem(paletteName, paletteName, imageIndex));
                        imageIndex++;
                    }
                    else
                    {
                        // Fallback if no image can be generated
                        comboBoxEdit2.Properties.Items.Add(new ImageComboBoxItem(paletteName, paletteName, -1));
                    }
                }

                comboBoxEdit2.EditValue = DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveSvgPaletteName;
            }
            else
            {
                comboBoxEdit2.Properties.Items.Add(new ImageComboBoxItem("Default", "Default", -1));
                comboBoxEdit2.EditValue = "Default";
                comboBoxEdit2.Enabled = false;
            }
        }
        private void SettingsForm_Shown(object sender, EventArgs e)
        {

            tabPane1.SelectedPageIndex = TabSelected;
        }
        public async void GetdllInfo()
        {
            memoEdit1.Text = await GetThirdPartydll.GetThirdPartyReferences();
        }
        private void toggleSwitch1_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch1.IsOn)
            {
                AutoStart = true;
                Settings.Default.AutoStart = true;
                Settings.Default.Save();
                EnableStartup();
            }
            else
            {
                AutoStart = false;
                Settings.Default.AutoStart = false;
                Settings.Default.Save();
                DisableStartup();
            }
        }

        private void toggleSwitch2_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch2.IsOn && toggleSwitch21.IsOn)
            {
                StartMinimized = true;
                toggleSwitch21.IsOn = false;
                Settings.Default.StartMinimized = true;
                Settings.Default.StartMaximized = false;
                Settings.Default.Save();
            }
            if (toggleSwitch2.IsOn)
            {
                StartMinimized = true;
                
                Settings.Default.StartMinimized = true;
                
                Settings.Default.Save();
            }
            else
            {
                StartMinimized = false;
                Settings.Default.StartMinimized = false;
                Settings.Default.Save();
            }
        }

        private void toggleSwitch3_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch3.IsOn)
            {
                MinimizeOnClose = true;
                Settings.Default.MinimizeOnClose = true;
                Settings.Default.Save();
            }
            else
            {
                MinimizeOnClose = false;
                Settings.Default.MinimizeOnClose = false;
                Settings.Default.Save();
            }
        }
        public static void EnableStartup()
        {
            RegistryKey key =
                Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run",
                    true);

            key.SetValue(
                "Nexus Launcher",
                Application.ExecutablePath);
        }
        public static void DisableStartup()
        {
            RegistryKey key =
                Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run",
                    true);

            key.DeleteValue(
                "Nexus Launcher",
                false);
        }

        private void toggleSwitch4_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch4.IsOn)
            {
                ShowSteam = true;
                Settings.Default.ShowSteam = true;
                uiLight1.State = Sunny.UI.UILightState.On;
                Settings.Default.Save();
            }
            else
            {
                ShowSteam = false;
                Settings.Default.ShowSteam = false;
                uiLight1.State = Sunny.UI.UILightState.Off;
                Settings.Default.Save();
            }
        }

        private void toggleSwitch6_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch6.IsOn)
            {
                ShowBattleNet = true;
                Settings.Default.ShowBattleNet = true;
                uiLight3.State = Sunny.UI.UILightState.On;
                Settings.Default.Save();
            }
            else
            {
                ShowBattleNet = false;
                Settings.Default.ShowBattleNet = false;
                uiLight3.State = Sunny.UI.UILightState.Off;
                Settings.Default.Save();
            }
        }

        private void toggleSwitch5_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch5.IsOn)
            {
                ShowEpic = true;
                Settings.Default.ShowEpic = true;
                uiLight2.State = Sunny.UI.UILightState.On;
                Settings.Default.Save();
            }
            else
            {
                ShowEpic = false;
                Settings.Default.ShowEpic = false;
                uiLight2.State = Sunny.UI.UILightState.Off;
                Settings.Default.Save();
            }
        }

        private void toggleSwitch7_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch7.IsOn)
            {
                ShowGOG = true;
                Settings.Default.ShowGOG = true;
                uiLight11.State = Sunny.UI.UILightState.On;
                Settings.Default.Save();
            }
            else
            {
                ShowGOG = false;
                Settings.Default.ShowGOG = false;
                uiLight11.State = Sunny.UI.UILightState.Off;
                Settings.Default.Save();
            }
        }

        private void toggleSwitch10_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch10.IsOn)
            {
                ShowAmazon = true;
                Settings.Default.ShowAmazon = true;
                uiLight8.State = Sunny.UI.UILightState.On;
                Settings.Default.Save();
            }
            else
            {
                ShowAmazon = false;
                Settings.Default.ShowAmazon = false;
                uiLight8.State = Sunny.UI.UILightState.Off;
                Settings.Default.Save();
            }
        }

        private void toggleSwitch9_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch9.IsOn)
            {
                ShowParadox = true;
                Settings.Default.ShowParadox = true;
                uiLight9.State = Sunny.UI.UILightState.On;
                Settings.Default.Save();
            }
            else
            {
                ShowParadox = false;
                Settings.Default.ShowParadox = false;
                uiLight9.State = Sunny.UI.UILightState.Off;
                Settings.Default.Save();
            }
        }

        private void toggleSwitch11_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch11.IsOn)
            {
                ShowEA = true;
                Settings.Default.ShowEA = true;
                uiLight7.State = Sunny.UI.UILightState.On;
                Settings.Default.Save();
            }
            else
            {
                ShowEA = false;
                Settings.Default.ShowEA = false;
                uiLight7.State = Sunny.UI.UILightState.Off;
                Settings.Default.Save();
            }
        }

        private void toggleSwitch12_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch12.IsOn)
            {
                ShowXbox = true;
                Settings.Default.ShowXbox = true;
                uiLight4.State = Sunny.UI.UILightState.On;
                Settings.Default.Save();
            }
            else
            {
                ShowXbox = false;
                Settings.Default.ShowXbox = false;
                uiLight4.State = Sunny.UI.UILightState.Off;
                Settings.Default.Save();
            }
        }

        private void toggleSwitch13_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch13.IsOn)
            {
                ShowWindowsStore = true;
                Settings.Default.ShowWindowsStore = true;
                uiLight5.State = Sunny.UI.UILightState.On;
                Settings.Default.Save();
            }
            else
            {
                ShowWindowsStore = false;
                Settings.Default.ShowWindowsStore = false;
                uiLight5.State = Sunny.UI.UILightState.Off;
                Settings.Default.Save();
            }
        }

        private void toggleSwitch14_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch14.IsOn)
            {
                ShowWargaming = true;
                Settings.Default.ShowWargaming = true;
                uiLight6.State = Sunny.UI.UILightState.On;
                Settings.Default.Save();
            }
            else
            {
                ShowWargaming = false;
                Settings.Default.ShowWargaming = false;
                uiLight6.State = Sunny.UI.UILightState.Off;
                Settings.Default.Save();
            }
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            // Save settings display message about restarting the application to apply changes
            var result = XtraMessageBox.Show("Settings saved. Please restart the application to apply changes.\n\nDo you want to restart now?", "Settings Saved", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.No)
            {
                this.Close();

            }
            else if (result == DialogResult.Yes)
            {
                Program._mutex.Dispose();

                Application.Restart();
                Environment.Exit(0);
            }
        }

        private void toggleSwitch8_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch8.IsOn)
            {
                ShowUbisoft = true;
                Settings.Default.ShowUbisoft = true;
                uiLight10.State = Sunny.UI.UILightState.On;
                Settings.Default.Save();
            }
            else
            {
                ShowUbisoft = false;
                Settings.Default.ShowUbisoft = false;
                uiLight10.State = Sunny.UI.UILightState.Off;
                Settings.Default.Save();
            }
        }

        private void comboBoxEdit2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxEdit2.Enabled && comboBoxEdit2.EditValue != null)
            {
                string selectedPalette = comboBoxEdit2.Text;
                string currentSkin = comboBoxEdit1.Text;

                // FIX CS0200: Use SetSkinStyle to change the active palette safely
                DevExpress.LookAndFeel.UserLookAndFeel.Default.SetSkinStyle(currentSkin, selectedPalette);

                // Edit and Delete only apply to the user's own themes.
                UpdateCustomThemeMenuState();
            }
        }

        private bool revertingSkin;

        private void comboBoxEdit1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Set while putting the old value back, so the revert does
            // not re-enter this handler and prompt a second time.
            if (revertingSkin)
                return;

            string selectedSkin = comboBoxEdit1.Text;

            if (!ConfirmUnsupportedSkin(selectedSkin))
            {
                revertingSkin = true;

                try
                {
                    comboBoxEdit1.EditValue =
                        DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName;
                }
                finally
                {
                    revertingSkin = false;
                }

                return;
            }

            DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = selectedSkin;

            // Update palettes whenever the skin shifts
            UpdatePaletteList();

            // New Theme depends on the new skin being recolourable.
            UpdateCustomThemeMenuState();
        }

        private void simpleButton4_Click(object sender, EventArgs e)
        {
            Settings.Default.DefaultLauncher = comboBoxEdit4.Text;
            Settings.Default.Save();
            this.Close();
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            string updater = Application.StartupPath + @"\updater.exe";
            string updaterLNK = Application.StartupPath + @"\Check for updates.lnk";
            string updaterini = Application.StartupPath + @"\updater.ini";
            if (File.Exists(updaterLNK) && (File.Exists(updaterini)))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = updaterLNK,
                        UseShellExecute = true,
                        Verb = "runas"
                    });
                    //System.Diagnostics.Process.Start(updaterLNK);
                }
                catch
                {
                    XtraMessageBox.Show("Updater failed");
                }
            }
            else if (File.Exists(updater) && (File.Exists(updaterini)))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = updater,
                        UseShellExecute = true,
                        Verb = "runas"
                    });
                }
                catch
                {
                    XtraMessageBox.Show("Updater failed");
                }
            }
            else
            {
                XtraMessageBox.Show("Updater not found");
            }
        }

        private void toggleSwitch15_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch15.IsOn)
            {
                Settings.Default.SidePanelRemember = true;
                Settings.Default.Save();
            }
            else
            {
                Settings.Default.SidePanelRemember = false;
                Settings.Default.Save();
            }
        }

        private void toggleSwitch16_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch16.IsOn)
            {
                Settings.Default.HideEANotice = true;
                Settings.Default.Save();
            }
            else
            {
                Settings.Default.HideEANotice = false;
                Settings.Default.Save();
            }
        }

        private void comboBoxEdit4_SelectedIndexChanged(object sender, EventArgs e)
        {
            //comboBoxEdit4.Text = comboBoxEdit4.SelectedText;
            //comboBoxEdit4.SelectedText = comboBoxEdit4.Text;
            Settings.Default.DefaultLauncher = comboBoxEdit4.SelectedText;
            Settings.Default.Save();
        }

        private void toggleSwitch18_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch18.IsOn)
            {
                Settings.Default.RootDisplayMode = true;
                Settings.Default.Save();
            }
            else
            {
                Settings.Default.RootDisplayMode = false;
                Settings.Default.Save();
            }
        }

        private void toggleSwitch17_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch17.IsOn)
            {
                Settings.Default.ViewType = true;
                Settings.Default.Save();
            }
            else
            {
                Settings.Default.ViewType = false;
                Settings.Default.Save();
            }
        }

        private void simpleButton5_Click(object sender, EventArgs e)
        {
            try
            {
                if (XtraMessageBox.Show(
                    "Are you sure you want to uninstall Nexus Launcher?\n\n" +
                    "This will launch the Nexus Launcher uninstaller.",
                    "Confirm Uninstall",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Process.Start(
                        Application.StartupPath +
                        @"\Nexus Uninstall.lnk");
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                XtraMessageBox.Show(
                    "Could not locate the Nexus Launcher uninstaller.\n\n" +
                    "Please uninstall Nexus Launcher through Windows Settings or the Control Panel.",
                    "Uninstall Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void simpleButton6_Click(object sender, EventArgs e)
        {
            _mainview.FocusGOGSettings();
            //_mainview.ApplyAcrylicAccent();

            this.Close();

        }

        private void toggleSwitch19_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch19.IsOn)
            {
                checkEdit1.Enabled = true;
                checkEdit2.Enabled = true;
                checkEdit3.Enabled = true;
                checkEdit4.Enabled = true;
                checkEdit5.Enabled = true;
                checkEdit6.Enabled = true;
                Properties.Settings.Default.StartLaunchers = true;
                Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.StartLaunchers = false;
                checkEdit1.Checked = false;
                checkEdit2.Checked = false;
                checkEdit3.Checked = false;
                checkEdit4.Checked = false;
                checkEdit5.Checked = false;
                checkEdit6.Checked = false;
                checkEdit1.Enabled = false;
                checkEdit2.Enabled = false;
                checkEdit3.Enabled = false;
                checkEdit4.Enabled = false;
                checkEdit5.Enabled = false;
                checkEdit6.Enabled = false;
                Properties.Settings.Default.StartBattleNet = false;
                Properties.Settings.Default.StartEA = false;
                Properties.Settings.Default.StartEpic = false;
                Properties.Settings.Default.StartGOG = false;
                Properties.Settings.Default.StartSteam = false;
                Properties.Settings.Default.StartUbisoft = false;
                Settings.Default.Save();
            }
        }

        private void tabNavigationPage1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void checkEdit1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkEdit1.Checked)
            {
                Properties.Settings.Default.StartBattleNet = true;
                Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.StartBattleNet = false;
                Settings.Default.Save();
            }
        }

        private void checkEdit2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkEdit2.Checked)
            {
                Properties.Settings.Default.StartEA = true;
                Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.StartEA = false;
                Settings.Default.Save();
            }
        }

        private void checkEdit3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkEdit3.Checked)
            {
                Properties.Settings.Default.StartEpic = true;
                Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.StartEpic = false;
                Settings.Default.Save();
            }
        }

        private void checkEdit4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkEdit4.Checked)
            {
                Properties.Settings.Default.StartGOG = true;
                Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.StartGOG = false;
                Settings.Default.Save();
            }
        }

        private void checkEdit5_CheckedChanged(object sender, EventArgs e)
        {
            if (checkEdit5.Checked)
            {
                Properties.Settings.Default.StartUbisoft = true;
                Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.StartUbisoft = false;
                Settings.Default.Save();
            }
        }

        private void checkEdit6_CheckedChanged(object sender, EventArgs e)
        {
            if (checkEdit6.Checked)
            {
                Properties.Settings.Default.StartSteam = true;
                Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.StartSteam = false;
                Settings.Default.Save();
            }
        }

        private void simpleButton7_Click(object sender, EventArgs e)
        {
            XtraMessageBox.Show(
    "Some launchers do not support silent startup and may briefly\n" +
    "display a window before minimizing.\n\n" +
    "For the best experience, enable 'Start Minimized' or\n" +
    "'Minimize to System Tray' in each launcher's own settings.\n\n" +
    "Nexus Launcher will only start launchers that are not\n" +
    "already running.\n\n"+
    "Most games will have a boost to launching and/or may require the launcher to be running before starting.",
    "Launcher Startup",
    MessageBoxButtons.OK,
    MessageBoxIcon.Information);
        }

        private void simpleButton8_Click(object sender, EventArgs e)
        {
            try
            {
                if (XtraMessageBox.Show(
                    "The configuration file contains Nexus Launcher's saved settings and preferences.\n\n" +
                    "Editing this file incorrectly may cause settings to reset or prevent certain features from working correctly.\n\n" +
                    "Do you want to open the configuration file?",
                    "Open Configuration File",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    OpenUserSettingsFile();
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                XtraMessageBox.Show(
                    "The configuration file could not be opened.",
                    "Open Configuration File",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            
        }
        public static void OpenUserSettingsFolder()
        {
            try
            {
                // 1. Force .NET to look up the active user.config path
                Configuration userConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                string configFilePath = userConfig.FilePath;

                // 2. Open Windows Explorer and highlight the file
                if (File.Exists(configFilePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{configFilePath}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    // Note: The file doesn't actually get created on disk until you call Settings.Default.Save() at least once!
                    Console.WriteLine("The user.config file hasn't been created yet. Call Settings.Default.Save() first.");
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                Console.WriteLine($"Error finding user settings: {ex.Message}");
            }
        }
        public static void OpenUserSettingsFile()
        {
            try
            {
                // 1. Force .NET to look up the active user.config path
                Configuration userConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                string configFilePath = userConfig.FilePath;

                // 2. Open the config file directly
                if (File.Exists(configFilePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = configFilePath,
                        UseShellExecute = true // Opens it with the default text editor/XML viewer
                    });
                }
                else
                {
                    // Note: The file doesn't actually get created on disk until you call Settings.Default.Save() at least once!
                    Console.WriteLine("The user.config file hasn't been created yet. Call Settings.Default.Save() first.");

                    // Optional Fallback: Open the directory if the file isn't there yet
                    string configDirectory = Path.GetDirectoryName(configFilePath);
                    if (Directory.Exists(configDirectory))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"\"{configDirectory}\"",
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                Console.WriteLine($"Error opening user settings: {ex.Message}");
            }
        }
        public static void OpenCrashLogFile()
        {
            try
            {
                // 1. Get the directory containing user.config and locate CrashLog.txt
                Configuration userConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                string configDirectory = Path.GetDirectoryName(userConfig.FilePath);
                string crashLogPath = Path.Combine(configDirectory, "CrashLog.txt");

                // 2. Open the file directly
                if (File.Exists(crashLogPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = crashLogPath,
                        UseShellExecute = true // Tells Windows to use the default app associated with .txt
                    });
                }
                else
                {
                    Console.WriteLine("CrashLog.txt does not exist yet.");

                    // Optional Fallback: If the file doesn't exist, open the directory instead
                    if (Directory.Exists(configDirectory))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"\"{configDirectory}\"",
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                Console.WriteLine($"Error opening crash log: {ex.Message}");
            }
        }

        private async void simpleButton3_Click(object sender, EventArgs e)
        {
            UpdateInfo update =
        await UpdateChecker.CheckAsync();

            if (update == null)
            {
                XtraMessageBox.Show(
                    "Unable to contact the update server.");

                return;
            }

            if (update.build <= UpdateState.InstalledBuild)
            {
                XtraMessageBox.Show(
                    "Nexus Launcher is up to date.");

                return;
            }

            // A download the user postponed earlier is offered straight to
            // install rather than downloaded again.
            string pending =
                UpdateDownloader.GetPendingPackage(update);

            DialogResult result =
                XtraMessageBox.Show(
                    pending != null
                        ? "Build " + update.build +
                            " is downloaded and ready to install.\n\nContinue?"
                        : "Build " + update.build +
                            " is available.\n\nDownload now?",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

            if (result != DialogResult.Yes)
                return;

            // Shows download progress, then asks whether to install now
            // or later. Downloading used to happen with no feedback and
            // then launch the installer immediately.
            string package;

            using (UpdateDownloadForm download = new UpdateDownloadForm(update, pending))
            {
                download.ShowDialog(this);

                if (!download.InstallNow)
                    return;

                package = download.PackagePath;
            }

            if (UpdateService.LaunchUpdater(package))
            {
                UpdateDownloader.ClearPending();
                Environment.Exit(0);
            }

            // The updater did not start, for example the administrator
            // prompt was declined. Keep the package so the next check can
            // offer it again without downloading it a second time.
            UpdateDownloader.SavePending(update, package);
        }

        public void simpleButton9_Click(object sender, EventArgs e)
        {

            if (XtraMessageBox.Show(
    "Rebuilding the artwork cache will clear all cached artwork and metadata before creating a fresh cache.\n\n" +
    "Nexus Launcher will close and automatically restart to begin the rebuild process.\n\n" +
    "Depending on the size of your game libraries, the rebuild may take several minutes.\n\n" +
    "Do you want to continue?",
    "Rebuild Artwork Cache",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            Interlocked.Exchange(ref ArtworkService.totalArtworkJobs, 0);
            Interlocked.Exchange(ref ArtworkService.completedArtworkJobs, 0);

            XtraMessageBox.Show(
                "Nexus Launcher will now restart and begin rebuilding the artwork cache.",
                "Restarting...",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            _mainview.appExit = true;
            Program._mutex.Dispose();

            Process process = new Process();

            process.StartInfo.FileName =
                Path.Combine(
                    Application.StartupPath,
                    "ClearArtworkCache.bat");

            process.StartInfo.UseShellExecute = false;

            process.Start();

            Environment.Exit(0);
        }

        private void toggleSwitch20_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch20.IsOn)
            {
                Properties.Settings.Default.enableFullLibrary = true;
                _mainview.barButtonItem13.Visibility = DevExpress.XtraBars.BarItemVisibility.Always;
                Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.enableFullLibrary = false;
                _mainview.barButtonItem13.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
                Settings.Default.Save();
            }
        }

        private void toggleSwitch21_Toggled(object sender, EventArgs e)
        {
            if (toggleSwitch21.IsOn && toggleSwitch2.IsOn)
            {
                toggleSwitch2.IsOn = false;
                Properties.Settings.Default.StartMaximized = true;
                Properties.Settings.Default.StartMinimized = false;
                Settings.Default.Save();
            }
            if (toggleSwitch21.IsOn)
            {
                
                Properties.Settings.Default.StartMaximized = true;
                
                Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.StartMaximized = false;
                
                Settings.Default.Save();
            }
        }

        private void simpleButton10_Click(object sender, EventArgs e)
        {
            OpenCrashLogFile();
        }

        private void simpleButton11_Click(object sender, EventArgs e)
        {
            OpenUserSettingsFolder();
        }

        private void simpleButton12_Click(object sender, EventArgs e)
        {
            FontDialog dlg =
    new FontDialog();

            dlg.Font =
                this.Font;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Settings.Default.UIFont =
                    dlg.Font.FontFamily.Name;

                Settings.Default.Save();

                FontManager.ApplyFontToAllOpenForms(
                    Settings.Default.UIFont);
                labelControl29.Text = "UI Font (" + Settings.Default.UIFont + ")";
            }
        }

        private void simpleButton13_Click(object sender, EventArgs e)
        {
            XtraMessageBox.Show(
    "The Nexus Launcher uninstaller only removes Nexus Launcher itself.\n\n" +

    "• Your installed games will NOT be removed.\n" +
    "• Third-party launchers (Steam, EA App, Epic Games, Ubisoft Connect, etc.) will NOT be removed.\n" +
    "• Most settings and configuration files may be kept so they can be restored if you reinstall Nexus Launcher.\n\n" +

    "If you want to completely remove all traces of Nexus Launcher, you can manually delete any remaining configuration files after uninstalling.",
    "About Uninstalling",
    MessageBoxButtons.OK,
    MessageBoxIcon.Information);
        }

        private void simpleButton15_Click(object sender, EventArgs e)
        {
            XtraMessageBox.Show(
    "The configuration file contains Nexus Launcher's saved settings, preferences, and other application data.\n\n" +

    "Advanced users can edit this file directly to customize settings, troubleshoot issues, or restore a previous configuration.\n\n" +

    "Any changes made while Nexus Launcher is running may be overwritten when the application closes. For best results, close Nexus Launcher before editing the file and consider creating a backup beforehand.",
    "Configuration File Information",
    MessageBoxButtons.OK,
    MessageBoxIcon.Information);
        }

        private void simpleButton16_Click(object sender, EventArgs e)
        {
            XtraMessageBox.Show(
    "Rebuild Cache clears Nexus Launcher's existing cache and creates a fresh one.\n\n" +

    "This process rescans your installed games and launchers, then rebuilds artwork, icons, logos, metadata, and other cached information.\n\n" +

    "Use this option if artwork is missing, game information appears incorrect, duplicate entries are shown, or after making significant changes to your game libraries.\n\n" +

    "Rebuilding the cache does not affect your installed games or personal settings, but it may take several minutes to complete depending on the size of your libraries.",
    "About Rebuild Cache",
    MessageBoxButtons.OK,
    MessageBoxIcon.Information);
        }

        /// <summary>
        /// Runs the first run wizard again from Advanced Settings.
        ///
        /// The wizard restarts Nexus itself when it finishes, the same as
        /// on a first run. Closing it without finishing changes nothing.
        /// </summary>
        private void simpleButtonRunSetup_Click(object sender, EventArgs e)
        {
            DialogResult result =
                XtraMessageBox.Show(
                    this,
                    "Run the setup wizard again?" + Environment.NewLine + Environment.NewLine +
                    "Nexus Launcher will restart once you finish the wizard. " +
                    "Closing it before the end leaves your settings as they are.",
                    "Setup Wizard",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            using (FirstTimeSetupForm wizard = new FirstTimeSetupForm())
            {
                wizard.ShowDialog(this);
            }
        }

        private void simpleButtonSetupInfo_Click(object sender, EventArgs e)
        {
            XtraMessageBox.Show(
                "The setup wizard is the short series of steps shown the first time Nexus Launcher runs." + Environment.NewLine + Environment.NewLine +

                "It detects your installed game launchers, lets you point Nexus at any it could not find, and sets up how Nexus starts, which launcher it opens on, and the name it greets you by." + Environment.NewLine + Environment.NewLine +

                "Running it again is useful after installing a new launcher, moving one to a different drive, or if you want to change those choices in one place." + Environment.NewLine + Environment.NewLine +

                "Your library, artwork, achievements and themes are not affected. Nexus Launcher restarts when the wizard finishes.",
                "About the Setup Wizard",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void simpleButton14_Click(object sender, EventArgs e)
        {
            XtraMessageBox.Show(
    "The Nexus Launcher log file contains diagnostic information recorded while the application is running.\n\n" +

    "Logs can help identify startup issues, game detection problems, update failures, and other unexpected behavior. They are also useful when reporting bugs or requesting support.\n\n" +

    "The log file is updated automatically while Nexus Launcher is running and may contain timestamps, system information, and error details.\n\n" +

    "You can safely view the log file at any time, but avoid editing or deleting it while Nexus Launcher is running.",
    "About the Log File",
    MessageBoxButtons.OK,
    MessageBoxIcon.Information);
        }

        private void hyperlinkLabelControl1_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://github.com/INSTINCT9413/Nexus-Launcher");
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void hyperlinkLabelControl2_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://guardbyte.me/downloads/Nexus%20Launcher/index.html#download");
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
