using DevExpress.XtraEditors;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls
{

    public partial class NexusStore : XtraUserControl
    {
        public string NexusStoreUrl =
            "https://horizonsocial.media/apps/nexus.html";
        public static CoreWebView2Environment sharedEnvironment;
        public NexusStore()
        {
            InitializeComponent();

            // Every browser on this control uses the one shared
            // profile, so a sign in anywhere in Nexus counts
            // everywhere. Done here because the designer can set
            // Source, which starts a browser on its own.
            WebViewEnvironment.PrepareAll(this);

            // Set here rather than in the designer: assigning
            // Source starts the browser immediately, which would
            // happen inside InitializeComponent and beat the line
            // above to it.
            webView22.Source =
                new System.Uri("https://guardbyte.me/downloads/Nexus%20Launcher/loading.html");
        }

        public async void NexusStore_Load(
            object sender,
            EventArgs e)
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
                    new Uri(NexusStoreUrl);
                sharedEnvironment = webView21.CoreWebView2.Environment;
            }
            catch (Exception ex)
            {
                //XtraMessageBox.Show(ex.ToString(),"Browser Error");
            }
        }

        public async Task InitializeBrowserAsync()
        {
            if (webView21.CoreWebView2 != null)
                return;

            // The shared environment, so the store sees the session
            // the account window signed in with.
            await WebViewEnvironment.AttachAsync(webView21);

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
            webView22.Visible = false;
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
            webView21.Source =
                new Uri(
                    NexusStoreUrl);
        }

        private void webView22_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
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
    }
}
