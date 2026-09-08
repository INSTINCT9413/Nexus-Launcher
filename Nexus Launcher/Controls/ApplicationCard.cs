using DevExpress.DXTemplateGallery.Extensions;
using DevExpress.XtraEditors;
using Microsoft.Web.WebView2.Core;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AdamsLair.WinForms.OriginSelector;

namespace Nexus_Launcher.Controls
{

    public partial class ApplicationCard : XtraUserControl
    {
        public string _name { get; set; }
        public string _launchString { get; set; }
        public string _goglnk { get; set; }
        public string _EAShortuct { get; set; }
        public string _UbisoftURI { get; set; }
        public int _gameID { get; set; }
        public string _gameURI { get; set; }
        public string _productID { get; set; }
        public Image _header { get; set; }
        public Image _icon { get; set; }
        public Image _logo { get; set; }
        public Image _library { get; set; }
        public string _selectedGroup { get; set; }
        public string _executablePath { get; set; }
        public string _gameStoreLink { get; set; }
        
        public ApplicationCard()
        {
            InitializeComponent();

            
            
        }

        private void dropDownButton1_Click(object sender, EventArgs e)
        {
            
            if (_selectedGroup == "Steam")
            {
                try
                    {
                    string exePathSteam = MainView.sysDisk + @"Program Files (x86)\Steam\Steam.exe";
                    bool isSteamRunning = Process.GetProcessesByName("Steam").Any();
                    if (!isSteamRunning)
                    {
                        // Start Steam normally first so it can initialize its background hooks
                        Process.Start(new ProcessStartInfo { FileName = exePathSteam, UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(exePathSteam) });

                        // Give it 3 to 5 seconds to load up before sending the game instruction
                        Thread.Sleep(4000);
                    }
                    Process.Start( new ProcessStartInfo
                    {
                        FileName =
                    "steam://rungameid/" + _gameID,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(_executablePath)
                    });
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                    MessageBox.Show(
                        "Failed to launch the application: " + ex.Message);
                }
                
            }
            else if (_selectedGroup == "Epic Games")
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName =
           "com.epicgames.launcher://apps/" +
           _launchString +
           "?action=launch&silent=true",
                    UseShellExecute = true
                });
            }
            else if (_selectedGroup == "Battle.net")
            {
                try
                {

                    string exePath = MainView.sysDisk + @"Program Files (x86)\Battle.net\Battle.net.exe";
                    string launchParams = "-nostreamline -sso -launch -uid";
                    string diabloIVParams = "-launch";
                    // 1. Force your product ID to uppercase if required by Blizzard's system
                    string cleanProductID = _productID.ToUpper();

                    // 2. Check if Battle.net is already running in the background
                    bool isBnetRunning = Process.GetProcessesByName("Battle.net").Any();

                    if (!isBnetRunning)
                    {
                        // Start Battle.net normally first so it can initialize its background hooks
                        Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true });

                        // Give it 3 to 5 seconds to load up before sending the game instruction
                        Thread.Sleep(4000);
                    }

                    // 3. Send the execution command (Now it will successfully trigger the game!)
                    //Process.Start(new ProcessStartInfo
                    //{
                    //    FileName = exePath,
                    //    Arguments = $@"--exec=""launch {cleanProductID}""",
                    //    UseShellExecute = true
                    //});
                    if(_productID == "fenris")
                    {
                       // Explicitly launch DiabloIV with Battlenet process, passing argument
                        Process.Start(new ProcessStartInfo {
                            FileName = exePath,

                            // This forces Windows to read the correct 'battlenet://' URI association
                            Arguments = "--exec=\"launch Fen\"",
                            UseShellExecute = true, Verb="runas" });
                        
                    }
                    else
                    {
                       
                        Process.Start(new ProcessStartInfo { FileName = _executablePath, Arguments = launchParams + " " + _productID, UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(_executablePath) });
                    }
                   
                    //MessageBox.Show(_executablePath + _productID);

                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                    MessageBox.Show(
                        "Failed to launch the application: " + ex.Message);
                }
            }
            else if (_selectedGroup == "GOG")
            {
                try
                {
                   
                        Process.Start(
                            new ProcessStartInfo
                            {
                                FileName =
                                    _goglnk,

                                UseShellExecute =
                                    true
                            });
                    
                  
                    
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                    MessageBox.Show(
                        "Failed to launch the application: " + ex.Message);
                }
            }
            else if (_selectedGroup == "EA App")
            {
                try
                {
                    //labelControl1.Text = _EAShortuct;
                    if (!string.IsNullOrWhiteSpace(
    _EAShortuct) &&
    File.Exists(
        _EAShortuct))
                    {
                        Process.Start(
                            new ProcessStartInfo
                            {
                                FileName =
                                    _EAShortuct,
                                UseShellExecute =
                                    true,
                                WorkingDirectory = Path.GetDirectoryName(_executablePath)
                            });

                        return;
                    }
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                }
            }
            else if (_selectedGroup == "Ubisoft Connect")
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(
    _UbisoftURI))
                    {
                        Process.Start(
                            new ProcessStartInfo
                            {
                                FileName =
                                   _UbisoftURI,

                                UseShellExecute =
                                    true,
                                WorkingDirectory = Path.GetDirectoryName(_executablePath)
                            });

                        return;
                    }
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                }
            }
            else if (_selectedGroup == "Nexus Launcher")
            {
                try
                {
                    Process.Start( new ProcessStartInfo { FileName = _executablePath, UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(_executablePath) });
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                    MessageBox.Show(
                        "Failed to launch the application: " + ex.Message);
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            labelControl2 .Text = _executablePath;
            labelControl1.Text = _name;
            dropDownButton1.Text = "Play ";
            
            //pictureEdit1.Image = _library;
            //pictureEdit2.Image = _header;
        }

        private async void ApplicationCard_Load(object sender, EventArgs e)
        {
            FontManager.ApplyFont(
    this,
    Settings.Default.UIFont);
            try
            {
                await InitializeBrowserAsync();

                webView21.CoreWebView2.NewWindowRequested -=
                    CoreWebView2_NewWindowRequested;

                webView21.CoreWebView2.NewWindowRequested +=
                    CoreWebView2_NewWindowRequested;

                webView21.CoreWebView2.NavigationCompleted +=
                    CoreWebView2_NavigationCompleted;

                webView21.Source =
                    new Uri(_gameStoreLink);
                //sharedEnvironment = webView21.CoreWebView2.Environment;
            }
            catch (Exception ex)
            {
               // XtraMessageBox.Show(ex.ToString(),"Browser Error");
            }

        }
        public async Task InitializeBrowserAsync()
        {
            if (webView21.CoreWebView2 != null)
                return;

            string profilePath =
                Path.Combine(
                    Application.StartupPath,
                    "BrowserProfile");

            Directory.CreateDirectory(
                profilePath);

            CoreWebView2Environment env =
                await CoreWebView2Environment.CreateAsync(
                    null,
                    profilePath);

             
            await webView21.EnsureCoreWebView2Async();
            webView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled =
                true;

            webView21.CoreWebView2.Settings.AreDevToolsEnabled =
                true;

            webView21.CoreWebView2.Settings.IsStatusBarEnabled =
                false;

            webView21.CoreWebView2.Settings.IsZoomControlEnabled =
                true;

            webView21.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled =
                true;
        }
        private void CoreWebView2_NewWindowRequested(
            object sender,
            CoreWebView2NewWindowRequestedEventArgs e)
        {
            try
            {
                e.Handled = true;

                webView21.CoreWebView2.Navigate(
                    e.Uri);
            }
            catch
            {
            }
        }

        private void CoreWebView2_NavigationCompleted(
            object sender,
            CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                if (!e.IsSuccess)
                {
                    // XtraMessageBox.Show("Failed to load page.","Nexus Store");
                }
            }
            catch
            {
            }
        }

        public void Navigate(
            string url)
        {
            try
            {
                if (webView21.CoreWebView2 == null)
                    return;

                webView21.CoreWebView2.Navigate(
                    url);
            }
            catch
            {
            }
        }

        public void GoBack()
        {
            if (webView21.CoreWebView2?.CanGoBack == true)
            {
                webView21.CoreWebView2.GoBack();
            }
        }

        public void GoForward()
        {
            if (webView21.CoreWebView2?.CanGoForward == true)
            {
                webView21.CoreWebView2.GoForward();
            }
        }

        public void RefreshPage()
        {
            webView21.CoreWebView2?.Reload();
        }

        public void NavigateHome()
        {
            
        }

        private void webView21_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
          webView22.Visible = true;
          webView22.BringToFront();
        }

        private void webView21_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            webView22.Visible = false;
        }

        private void dropDownButton2_Click(object sender, EventArgs e)
        {
            webView21.Reload();
        }

        private void dropDownButton3_Click(object sender, EventArgs e)
        {
            splitContainerControl1.SplitterPosition = 0;
        }

        private void dropDownButton4_Click(object sender, EventArgs e)
        {
           
        }
    }
}
