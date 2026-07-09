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
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services.Artwork;
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
            try
            {
                labelControl17.Text = $"Version: {Assembly.GetExecutingAssembly().GetName().Version}";
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
            string updaterini = Application.StartupPath + @"\updater.ini";
            if (File.Exists(updater) && (File.Exists(updaterini)))
            {
                try
                {
                    System.Diagnostics.Process.Start(updater);
                }
                catch
                {
                    XtraMessageBox.Show("Updater failed");
                }
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
                Process.Start(Application.StartupPath + @"\Nexus Uninstall.lnk");
            }
            catch (Exception ex) { XtraMessageBox.Show("Could not find uninstall path. Please uninstall via control panel!"); }
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
            OpenUserSettingsFolder();
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
                Console.WriteLine($"Error finding user settings: {ex.Message}");
            }
        }

        private void simpleButton3_Click(object sender, EventArgs e)
        {
            throw new Exception("Testing global crash handler.");
        }

        public void simpleButton9_Click(object sender, EventArgs e)
        {
           
            ArtworkService.TotalArtworkJobs = 0;
            ArtworkService.CompletedArtworkJobs = 0;
            XtraMessageBox.Show("Nexus Launcher must restart to resync the artwork cache.", "Restarting...");
            
            
            _mainview.appExit = true;
            Program._mutex.Dispose();

            Application.Restart();
            try
            {
                _mainview.ReleaseArtworkImages();
            }
            catch (Exception)
            {

             
            }
            Environment.Exit(0);
        }
        
    }
}
