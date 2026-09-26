using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;

namespace Nexus_Launcher.Helpers
{
    /// <summary>
    /// The one browser environment every WebView2 in Nexus uses.
    ///
    /// Cookies live in the user data folder, so two WebView2 controls
    /// only share a signed in session if they share that folder. Nexus
    /// used to have three: the store browser and the game card pointed
    /// at a BrowserProfile folder beside the executable, the account
    /// window pointed at one in LocalAppData, and the game card
    /// actually ended up on neither because it built an environment and
    /// then did not pass it. Signing in through the account window
    /// therefore did nothing for the store, and the store did nothing
    /// for the profile page.
    ///
    /// One folder, one environment, created once and handed to
    /// everything.
    /// </summary>
    internal static class WebViewEnvironment
    {
        /// <summary>
        /// In LocalAppData, not beside the executable: a normal install
        /// lives under Program Files, where the browser could not write
        /// its profile at all.
        /// </summary>
        public static readonly string UserDataFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "NexusLauncher",
                "WebView2");

        private static readonly SemaphoreSlim gate =
            new SemaphoreSlim(1, 1);

        private static CoreWebView2Environment shared;

        /// <summary>
        /// The shared environment, creating it on the first call.
        ///
        /// Guarded because several browsers can start at once: the
        /// store page and a game card are both capable of initialising
        /// while the other is still waiting.
        /// </summary>
        public static async Task<CoreWebView2Environment> GetAsync()
        {
            if (shared != null)
                return shared;

            await gate.WaitAsync();

            try
            {
                if (shared != null)
                    return shared;

                Directory.CreateDirectory(UserDataFolder);

                shared =
                    await CoreWebView2Environment.CreateAsync(
                        null,
                        UserDataFolder);

                return shared;
            }
            finally
            {
                gate.Release();
            }
        }

        /// <summary>
        /// Points a WebView2 at the shared folder before it starts.
        ///
        /// This is the important one. A WebView2 initialises itself the
        /// moment anything sets Source, and Nexus does that from
        /// dozens of places without going near AttachAsync. An implicit
        /// start like that would use a default folder of its own, which
        /// is why the store stayed signed out while the account window
        /// was signed in.
        ///
        /// CreationProperties is honoured by that implicit start, so
        /// setting it once when the control is built fixes every one of
        /// those call sites without any of them having to know.
        ///
        /// Safe to call more than once, and harmless after the browser
        /// has already started.
        /// </summary>
        public static void Prepare(
            WebView2 view)
        {
            if (view == null || view.CoreWebView2 != null)
                return;

            try
            {
                if (view.CreationProperties == null)
                {
                    view.CreationProperties =
                        new CoreWebView2CreationProperties();
                }

                Directory.CreateDirectory(UserDataFolder);

                view.CreationProperties.UserDataFolder =
                    UserDataFolder;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        /// <summary>
        /// Prepares every WebView2 on a control, however deeply nested.
        ///
        /// Used from a form's constructor: the designer builds the
        /// browsers, and some of them carry a Source it set, so they
        /// have to be pointed at the shared folder before anything
        /// gets a chance to run.
        /// </summary>
        public static void PrepareAll(
            Control parent)
        {
            if (parent == null)
                return;

            WebView2 view =
                parent as WebView2;

            if (view != null)
            {
                Prepare(view);

                return;
            }

            foreach (Control child in parent.Controls)
            {
                PrepareAll(child);
            }
        }

        /// <summary>
        /// Starts a WebView2 on the shared environment.
        ///
        /// Every caller should use this rather than
        /// EnsureCoreWebView2Async on its own: the parameterless
        /// overload quietly uses a default environment of its own, with
        /// its own cookies, which is how the game card ended up signed
        /// out from everything else.
        /// </summary>
        public static async Task AttachAsync(
            WebView2 view)
        {
            if (view == null || view.CoreWebView2 != null)
                return;

            // In case the control starts itself between here and the
            // await below.
            Prepare(view);

            CoreWebView2Environment environment =
                await GetAsync();

            await view.EnsureCoreWebView2Async(environment);
        }
    }
}
