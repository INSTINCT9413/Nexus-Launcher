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

            await webView21.EnsureCoreWebView2Async(
                env);

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
            webView21.Source =
                new Uri(
                    NexusStoreUrl);
        }
    }
}
