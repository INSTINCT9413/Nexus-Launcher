using DevExpress.Printing.ExportHelpers;
using DevExpress.Skins;
using DevExpress.Utils.Svg;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraPrinting;
using DevExpress.XtraSplashScreen;
using Microsoft.Win32;
using Nexus_Launcher.Controls;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Models;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services;
using Nexus_Launcher.Services.Artwork;
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
                GetdllInfo();
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
            }
        }

        private void comboBoxEdit1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedSkin = comboBoxEdit1.Text;
            DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = selectedSkin;

            // Update palettes whenever the skin shifts
            UpdatePaletteList();
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

            DialogResult result =
                XtraMessageBox.Show(
                    "Build " + update.build +
                    " is available.\n\nDownload now?",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

            if (result != DialogResult.Yes)
                return;

            string package =
                await UpdateDownloader.DownloadAsync(update);

            UpdateService.LaunchUpdater(package);
            Environment.Exit(0);
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
