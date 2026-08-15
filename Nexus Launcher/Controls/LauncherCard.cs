using DevExpress.XtraEditors;
using HorizonUI;
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
            gogSettings.Visible = false;
            eaSettings.Visible = false;
            addRemove.Show();
        }
        public void ShowGOGSettings()
        {
            
                gogSettings.Show();
                gogSettings.Visible = true;
                gogSettings.BringToFront();
            
        }
        public void HideGOGSettings()
        {
            if (_selectedGroup != "GOG")
            {
                gogSettings.Visible = false;
                gogSettings.Hide();
            }
        }
        public void ShowEASettings()
        {

            eaSettings.Show();
            eaSettings.Visible = true;
            eaSettings.BringToFront();

        }
        public void HideEASettings()
        {
            if (_selectedGroup != "EA")
            {
                eaSettings.Visible = false;
                eaSettings.Hide();
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

        private void simpleButton1_Click(object sender, EventArgs e)
        {

        }
    }
}
