using DevExpress.XtraEditors;
using DevExpress.XtraRichEdit.Fields;
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

namespace Nexus_Launcher
{
    public partial class FirstTimeSetupForm : DevExpress.XtraEditors.XtraForm
    {
        Scanner scanner = new Scanner();
        string steamPath;
        string epicPath;
        string eaPath;
        string ubisoftPath;
        string gogPath;
        string battlenetPath;
        public FirstTimeSetupForm()
        {
            InitializeComponent();
        }

        private void FirstTimeSetupForm_Load(object sender, EventArgs e)
        {
            // Disables the focus rectangle globally for all DevExpress SimpleButtons
            DevExpress.XtraEditors.WindowsFormsSettings.FocusRectStyle = DevExpress.Utils.Paint.DXDashStyle.None;
            labelControl1.Text = welcomeWizardPage1.IntroductionText.ToString();
            FindLaunchers();
            AddSettingsForm();
        }
        private void FindLaunchers()
        {
            string steamPath = scanner.GetExe("C:\\Program Files (x86)\\Steam", "steam.exe");
            if (steamPath != null)
            {
                labelControl2 .Text = $"{steamPath}";
                pictureBox1.Image = Properties.Resources._9004716_tick_check_accept_mark_icon;
                Settings.Default.steamPath = steamPath;
            }
            else
            {
                labelControl2.Text = "Steam not found";
                pictureBox1.Image = Properties.Resources._9004715_cross_delete_remove_cancel_icon;
                simpleButton1.Enabled = true;
            }

            string epicPath = scanner.GetExe("C:\\Program Files (x86)\\Epic Games\\Launcher", "EpicGamesLauncher.exe");
            if (epicPath != null)
            {
                labelControl5.Text = $"{epicPath}";
                pictureBox4.Image = Properties.Resources._9004716_tick_check_accept_mark_icon;
                Settings.Default.epicPath = epicPath;
            }
            else
            {
                labelControl5 .Text = "Epic Games not found";
                pictureBox4.Image = Properties.Resources._9004715_cross_delete_remove_cancel_icon;
                simpleButton4.Enabled = true;
            }

            string eaPath = scanner.GetExe("C:\\Program Files\\Electronic Arts\\EA Desktop\\EA Desktop", "EADesktop.exe");
            if (eaPath != null)
            {
                labelControl4.Text = $"{eaPath}";
                pictureBox3.Image = Properties.Resources._9004716_tick_check_accept_mark_icon;
                Settings.Default.eaPath = eaPath;
            }
            else
            {
                labelControl4.Text = "EA Desktop not found";
                pictureBox3.Image = Properties.Resources._9004715_cross_delete_remove_cancel_icon;
                simpleButton3.Enabled = true;
            }

            string ubisoftPath = scanner.GetExe("C:\\Program Files (x86)\\Ubisoft\\Ubisoft Game Launcher", "UbisoftConnect.exe");
            if (ubisoftPath != null)
            {
                labelControl6.Text = $"{ubisoftPath}";
                pictureBox5.Image = Properties.Resources._9004716_tick_check_accept_mark_icon;
                Settings.Default.ubisoftPath = ubisoftPath;
            }
            else
            {
                labelControl6.Text = "Ubisoft Connect not found";
                pictureBox5.Image = Properties.Resources._9004715_cross_delete_remove_cancel_icon;
                simpleButton5.Enabled = true;
            }
            
            string gogPath = scanner.GetExe("C:\\Program Files (x86)\\GOG Galaxy", "GalaxyClient.exe");
            if (gogPath != null)
            {
                labelControl7.Text = $"{gogPath}";
                pictureBox6.Image = Properties.Resources._9004716_tick_check_accept_mark_icon;
                Settings.Default.gogPath = gogPath;
            }
            else
            {
                labelControl7.Text = "GOG Galaxy not found";
                pictureBox6.Image = Properties.Resources._9004715_cross_delete_remove_cancel_icon;
                simpleButton6.Enabled = true;
            }

            string battlenetPath = scanner.GetExe("C:\\Program Files (x86)\\Battle.net", "Battle.net.exe");
            if (battlenetPath != null)
            {
                labelControl3.Text = $"{battlenetPath}";
                pictureBox2.Image = Properties.Resources._9004716_tick_check_accept_mark_icon;
                Settings.Default.battlenetPath = battlenetPath;
            }
            else
            {
                labelControl3.Text = "Battle.net not found";
                pictureBox2.Image = Properties.Resources._9004715_cross_delete_remove_cancel_icon;
                simpleButton2.Enabled = true;
            }
            try
            {
                if (labelControl2.Text.Contains("not found") || labelControl3.Text.Contains("not found") || labelControl4.Text.Contains("not found") || labelControl5.Text.Contains("not found") || labelControl6.Text.Contains("not found") || labelControl7.Text.Contains("not found"))
                {
                    panelControl1.Visible = true;
                }
            }
            catch (Exception)
            {

                //throw;
            }
        }
        private void AddSettingsForm()
        {
            SettingsForm settingsForm = new SettingsForm();
            settingsForm.TopLevel = false;
            settingsForm.FormBorderStyle = FormBorderStyle.None;
            settingsForm.Dock = DockStyle.Fill;
            wizardPage2.Controls.Add(settingsForm);
            settingsForm.Show();
            settingsForm.tabNavigationPage2.PageVisible = false;
            settingsForm.tabNavigationPage3.PageVisible = false;
            settingsForm.tabNavigationPage4.PageVisible = false;
            settingsForm.tabNavigationPage5.PageVisible = false;
            settingsForm.simpleButton4.Visible = false;
            settingsForm.groupControl2.Enabled = false;
        }
        private void simpleButton2_Click(object sender, EventArgs e)
        {
            string battlenetPath = scanner.BrowseForExe("Battle.net.exe");
            if (battlenetPath != null && battlenetPath.EndsWith("Battle.net.exe"))
            {
                labelControl3.Text = $"{battlenetPath}";
                pictureBox2.Image = Properties.Resources._9004716_tick_check_accept_mark_icon;
                Settings.Default.battlenetPath = battlenetPath;
            }
            else
            {
                MessageBox.Show("Invalid Battle.net executable selected. Please select the correct Battle.net.exe file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            string steamPath = scanner.BrowseForExe("steam.exe");
            if (steamPath != null && steamPath.EndsWith("steam.exe"))
            {
                labelControl2.Text = $"{steamPath}";
                pictureBox1.Image = Properties.Resources._9004716_tick_check_accept_mark_icon;
                Settings.Default.steamPath = steamPath;
            }
            else
            {
                MessageBox.Show("Invalid Steam executable selected. Please select the correct steam.exe file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void simpleButton3_Click(object sender, EventArgs e)
        {
            string eaPath = scanner.BrowseForExe("EADesktop.exe");
            if (eaPath != null && eaPath.EndsWith("EADesktop.exe"))
            {
                labelControl4.Text = $"{eaPath}";
                pictureBox3.Image = Properties.Resources._9004716_tick_check_accept_mark_icon;
                Settings.Default.eaPath = eaPath;
            }
            else
            {
                MessageBox.Show("Invalid EA Desktop executable selected. Please select the correct EADesktop.exe file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void simpleButton4_Click(object sender, EventArgs e)
        {
            string epicPath = scanner.BrowseForExe("EpicGamesLauncher.exe");
            if (epicPath != null && epicPath.EndsWith("EpicGamesLauncher.exe"))
            {
                labelControl5.Text = $"{epicPath}";
                pictureBox4.Image = Properties.Resources._9004716_tick_check_accept_mark_icon;
                Settings.Default.epicPath = epicPath;
            }
            else
            {
                MessageBox.Show("Invalid Epic Games Launcher executable selected. Please select the correct EpicGamesLauncher.exe file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void simpleButton5_Click(object sender, EventArgs e)
        {
            string ubisoftPath = scanner.BrowseForExe("UbisoftConnect.exe");
            if (ubisoftPath != null && ubisoftPath.EndsWith("UbisoftConnect.exe"))
            {
                labelControl6.Text = $"{ubisoftPath}";
                pictureBox5.Image = Properties.Resources._9004716_tick_check_accept_mark_icon;
                Settings.Default.ubisoftPath = ubisoftPath;
            }
            else
            {
                MessageBox.Show("Invalid Ubisoft Connect executable selected. Please select the correct UbisoftConnect.exe file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void simpleButton6_Click(object sender, EventArgs e)
        {
            string gogPath = scanner.BrowseForExe("GalaxyClient.exe");
            if (gogPath != null && gogPath.EndsWith("GalaxyClient.exe"))
            {
                labelControl7.Text = $"{gogPath}";
                pictureBox6.Image = Properties.Resources._9004716_tick_check_accept_mark_icon;
                Settings.Default.gogPath = gogPath;
            }
            else
            {
                MessageBox.Show("Invalid GOG Galaxy executable selected. Please select the correct GalaxyClient.exe file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void simpleButton7_Click(object sender, EventArgs e)
        {
            Settings.Default.Setup = true;
            Settings.Default.Save();
            //Program._mutex.Dispose();

            Application.Restart();
            Environment.Exit(0);
        }

        private void wizardControl1_FinishClick(object sender, CancelEventArgs e)
        {
            simpleButton7.PerformClick();
        }
    }
}