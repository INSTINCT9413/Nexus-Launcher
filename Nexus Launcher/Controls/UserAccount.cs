using AdamsLair.WinForms.Properties;
using DevExpress.XtraEditors;
using Nexus_Launcher.Controls;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher.Services
{
    public partial class UserAccount : XtraUserControl
    {
        MyRigControl myRig = new MyRigControl();
        public UserAccount()
        {
            InitializeComponent();
        }
        public static string GetUserAccountPicturePath()
        {
            try
            {
                // LOCATION 1: The Modern Windows 10/11 Public Cache (Based on User Security ID)
                string currentSid = WindowsIdentity.GetCurrent().User?.Value;
                if (!string.IsNullOrEmpty(currentSid))
                {
                    string publicAccountPictures = MainView.sysDisk + $@"Users\Public\AccountPictures\{currentSid}";
                    if (Directory.Exists(publicAccountPictures))
                    {
                        var file = new DirectoryInfo(publicAccountPictures)
                            .GetFiles("*.jpg")
                            .OrderByDescending(f => f.LastWriteTime)
                            .FirstOrDefault();

                        if (file != null) return file.FullName;
                    }
                }

                // LOCATION 2: Fallback to the Roaming AppData Folder
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appDataPictures = Path.Combine(appData, @"Microsoft\Windows\AccountPictures");
                if (Directory.Exists(appDataPictures))
                {
                    var file = new DirectoryInfo(appDataPictures)
                        .GetFiles()
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault();

                    if (file != null) return file.FullName;
                }

                // LOCATION 3: Fallback to the System-Wide Default Placeholder
                string programDataPictures = MainView.sysDisk + @"ProgramData\Microsoft\User Account Pictures";
                if (Directory.Exists(programDataPictures))
                {
                    // Windows stores default variations here like user-192.png, user.bmp, etc.
                    string defaultPic = Path.Combine(programDataPictures, "user-192.png");
                    if (File.Exists(defaultPic)) return defaultPic;

                    string fallbackBmp = Path.Combine(programDataPictures, "user.bmp");
                    if (File.Exists(fallbackBmp)) return fallbackBmp;
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                System.Diagnostics.Debug.WriteLine($"Failed to fetch profile picture: {ex.Message}");
            }

            return null; // No images found anywhere
        }

        private void UserAccount_Load(object sender, EventArgs e)
        {
            FontManager.ApplyFont(
    this,
    Settings.Default.UIFont);
            string avatarPath = GetUserAccountPicturePath();
            if (!string.IsNullOrEmpty(avatarPath) && File.Exists(avatarPath))
            {
                uiAvatar1.Image = Image.FromFile(avatarPath);
                
            }
            else
            {
                //uiAvatar1.Image = Resources.dfveffb_9b262552_e352_4348_aefc_8e699002c946; // Fallback to default avatar
            }

            labelControl1.Text = MainView.fullUserName;
            myRig.Dock = DockStyle.Fill;
            groupControl4.Controls.Add(myRig);
            myRig.Show();
        }
    }
}
