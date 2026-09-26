using DevExpress.XtraEditors;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using DevExpress.XtraEditors.Controls;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services.Account;
using System;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Web;
using System.Windows.Forms;

namespace Nexus_Launcher.Forms
{
    /// <summary>
    /// What the window is being opened for.
    /// </summary>
    internal enum NexusAccountWebMode
    {
        /// <summary>
        /// Sign in and approve Nexus, ending with an application
        /// password on the callback URL.
        /// </summary>
        Authorize,

        /// <summary>
        /// The site's own registration form.
        /// </summary>
        Register,

        /// <summary>
        /// The WordPress profile page, for changing details. Nexus only
        /// reads the profile, so every edit happens on the site.
        /// </summary>
        EditProfile
    }

    /// <summary>
    /// Hosts the real Nexus website for anything involving credentials.
    ///
    /// Nexus deliberately has no password field of its own. Sign in,
    /// registration and profile edits all happen on the site, in a
    /// browser view, so the password is typed where it belongs and
    /// whatever the site puts in front of it, two factor or a captcha,
    /// still works.
    /// </summary>
    internal class NexusAccountWebForm : XtraForm
    {
        private readonly WebView2 web = new WebView2();

        private readonly LabelControl status = new LabelControl();

        private readonly SimpleButton closeButton = new SimpleButton();

        //--------------------------------------------------------------
        // Loading overlay
        //
        // The Nexus loader over the browser while a page is on its way,
        // so the window is never a blank white rectangle.
        //
        // Drawn locally rather than in a second WebView2 like the store
        // pages use. A second browser control has to start up and fetch
        // its own page before it can show anything, which is the very
        // thing being waited on here, and it would show nothing at all
        // when the connection is the problem.
        //--------------------------------------------------------------

        private readonly PanelControl overlay = new PanelControl();

        private readonly PictureEdit spinner = new PictureEdit();

        private readonly LabelControl loadingLabel = new LabelControl();

        private readonly NexusAccountWebMode mode;

        //--------------------------------------------------------------
        // Bounce handling
        //
        // The authorize page lives under wp-admin, and plenty of
        // WordPress sites keep ordinary members out of wp-admin.
        // WooCommerce does it by default to anyone without edit_posts,
        // which is every customer and subscriber, and login redirect
        // plugins do it by throwing away the redirect_to the login form
        // was given.
        //
        // Either way the browser lands somewhere that is not the
        // authorize page and the link never completes. An administrator
        // never sees it, because an administrator is allowed in.
        //--------------------------------------------------------------

        private readonly PanelControl blockedPanel = new PanelControl();

        private readonly LabelControl blockedTitle = new LabelControl();

        private readonly LabelControl blockedText = new LabelControl();

        private readonly LabelControl blockedUrl = new LabelControl();

        private readonly SimpleButton retryButton = new SimpleButton();

        private readonly SimpleButton copyButton = new SimpleButton();

        private readonly SimpleButton dismissButton = new SimpleButton();

        /// <summary>
        /// Set once the browser has been on the login form, so a
        /// landing somewhere unexpected can be told apart from the
        /// journey there.
        /// </summary>
        private bool sawLoginForm;

        /// <summary>
        /// How many times the authorize page has been asked for
        /// directly after being bounced. One is enough: if asking for
        /// the page itself still does not reach it, the block is on the
        /// page and not on the redirect.
        /// </summary>
        private int directAttempts;

        /// <summary>
        /// Set while the callback is being handled, so cancelling that
        /// navigation is not mistaken for a bounce.
        /// </summary>
        private bool completing;

        /// <summary>
        /// Whether the login form has been asked for yet.
        ///
        /// The destination is tried on its own first. Someone who is
        /// already signed in gets straight there and is never shown a
        /// login form at all, which is the whole point of keeping the
        /// session. Only if the site turns that away is the login form
        /// brought in.
        /// </summary>
        private bool triedLogin;

        /// <summary>
        /// Where the site put the browser instead of the authorize
        /// page, for the copyable details.
        /// </summary>
        private string bounceUrl;

        /// <summary>
        /// Set once WordPress hands back credentials.
        /// </summary>
        public string UserLogin { get; private set; }

        public string AppPassword { get; private set; }

        public bool Authorized
        {
            get
            {
                return !string.IsNullOrEmpty(UserLogin) &&
                    !string.IsNullOrEmpty(AppPassword);
            }
        }

        public NexusAccountWebForm(
            NexusAccountWebMode value)
        {
            mode = value;

            Text = Caption(mode);

            StartPosition = FormStartPosition.CenterParent;

            ClientSize = new Size(980, 760);

            MinimumSize = new Size(560, 480);

            ShowIcon = false;

            MaximizeBox = true;

            MinimizeBox = false;

            Build();
        }

        private static string Caption(
            NexusAccountWebMode mode)
        {
            switch (mode)
            {
                case NexusAccountWebMode.Register:
                    return "Create a Nexus account";

                case NexusAccountWebMode.EditProfile:
                    return "Your Nexus profile";

                default:
                    return "Sign in to Nexus";
            }
        }

        private void Build()
        {
            // The shared profile, set before anything can start the
            // browser by touching Source.
            WebViewEnvironment.Prepare(web);

            SuspendLayout();

            try
            {
                status.Dock = DockStyle.Top;

                status.Height = 34;

                status.Padding = new Padding(12, 9, 12, 0);

                status.Text =
                    mode == NexusAccountWebMode.Register
                        ? "Create your account on nexuslauncher.guardbyte.me, then sign in."
                        : mode == NexusAccountWebMode.EditProfile
                            ? "Changes you save here are pulled back into Nexus when you close this window."
                            : "Sign in on nexuslauncher.guardbyte.me and approve Nexus Launcher.";

                Controls.Add(status);

                BuildOverlay();

                Controls.Add(overlay);

                BuildBlockedPanel();

                web.Dock = DockStyle.Fill;

                Controls.Add(web);

                closeButton.Text = "Close";

                closeButton.Dock = DockStyle.Bottom;

                closeButton.Height = 36;

                closeButton.Click += (s, e) => Close();

                Controls.Add(closeButton);

                // Docking lays out from the back of the z-order, so the
                // filling browser has to come to the front to be sized
                // after the two bars. The overlay then goes in front of
                // the browser.
                web.BringToFront();

                overlay.BringToFront();

                blockedPanel.BringToFront();
            }
            finally
            {
                ResumeLayout(true);
            }

            ApplyTheme();
        }

        private void BuildOverlay()
        {
            overlay.Dock = DockStyle.Fill;

            overlay.BorderStyle =
                DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;

            spinner.EditValue = Resources.nexus_loader_128;

            spinner.Properties.AnimatedImageLoopMode =
                DevExpress.Utils.AnimatedImageLoopMode.Infinite;

            spinner.Properties.SizeMode = PictureSizeMode.Zoom;

            spinner.Properties.BorderStyle =
                DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;

            spinner.Properties.ShowMenu = false;

            spinner.Properties.ReadOnly = true;

            spinner.Properties.AllowFocused = false;

            spinner.Properties.Appearance.BackColor = Color.Transparent;

            spinner.Properties.Appearance.Options.UseBackColor = true;

            spinner.Size = new Size(128, 128);

            overlay.Controls.Add(spinner);

            loadingLabel.AutoSizeMode = LabelAutoSizeMode.None;

            loadingLabel.Appearance.TextOptions.HAlignment =
                DevExpress.Utils.HorzAlignment.Center;

            loadingLabel.Appearance.Options.UseTextOptions = true;

            loadingLabel.Text = "Contacting nexuslauncher.guardbyte.me...";

            overlay.Controls.Add(loadingLabel);

            overlay.Resize += (s, e) => LayoutOverlay();

            LayoutOverlay();
        }

        private void LayoutOverlay()
        {
            int x = (overlay.ClientSize.Width - spinner.Width) / 2;

            int y = (overlay.ClientSize.Height - spinner.Height) / 2 - 30;

            spinner.Location = new Point(Math.Max(0, x), Math.Max(0, y));

            loadingLabel.SetBounds(
                20,
                spinner.Bottom + 14,
                Math.Max(60, overlay.ClientSize.Width - 40),
                20);
        }

        /// <summary>
        /// Shows or hides the loader. The animation is only running
        /// while it is visible, so a hidden overlay costs nothing.
        /// </summary>
        private void SetLoading(
            bool loading)
        {
            if (IsDisposed)
                return;

            if (overlay.Visible == loading)
                return;

            overlay.Visible = loading;

            try
            {
                if (loading)
                {
                    overlay.BringToFront();

                    spinner.StartAnimation();
                }
                else
                {
                    spinner.StopAnimation();
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        private void ApplyTheme()
        {
            status.Appearance.ForeColor = ProfileStyle.MutedTextColor;

            status.Appearance.Options.UseForeColor = true;

            status.Appearance.Font = ProfileStyle.Font(9F);

            status.Appearance.Options.UseFont = true;

            // Matches the page behind it, so hiding the overlay is not
            // a jarring change of colour.
            overlay.Appearance.BackColor = ProfileStyle.CardColor;

            overlay.Appearance.Options.UseBackColor = true;

            loadingLabel.Appearance.ForeColor = ProfileStyle.TextColor;

            loadingLabel.Appearance.Options.UseForeColor = true;

            loadingLabel.Appearance.Font = ProfileStyle.Font(10F);

            loadingLabel.Appearance.Options.UseFont = true;

            blockedPanel.Appearance.BackColor =
                ProfileStyle.CardColor;

            blockedPanel.Appearance.Options.UseBackColor = true;

            blockedTitle.Appearance.ForeColor = ProfileStyle.TextColor;
            blockedTitle.Appearance.Options.UseForeColor = true;
            blockedTitle.Appearance.Font =
                ProfileStyle.Font(12F, FontStyle.Bold);
            blockedTitle.Appearance.Options.UseFont = true;

            blockedText.Appearance.ForeColor = ProfileStyle.TextColor;
            blockedText.Appearance.Options.UseForeColor = true;
            blockedText.Appearance.Font = ProfileStyle.Font(9F);
            blockedText.Appearance.Options.UseFont = true;

            blockedUrl.Appearance.ForeColor =
                ProfileStyle.MutedTextColor;
            blockedUrl.Appearance.Options.UseForeColor = true;
            blockedUrl.Appearance.Font = ProfileStyle.Font(8F);
            blockedUrl.Appearance.Options.UseFont = true;
        }

        protected override void OnResize(
            EventArgs e)
        {
            base.OnResize(e);

            if (blockedPanel.Visible)
                LayoutBlocked();
        }

        protected override async void OnLoad(
            EventArgs e)
        {
            base.OnLoad(e);

            SetLoading(true);

            try
            {
                // The shared environment: signing in here has to
                // count everywhere else in Nexus.
                await WebViewEnvironment.AttachAsync(web);

                web.CoreWebView2.Settings.AreDevToolsEnabled = false;

                web.CoreWebView2.Settings.IsStatusBarEnabled = false;

                // Keep everything inside this window rather than
                // spawning popups the user cannot see.
                web.CoreWebView2.NewWindowRequested += (s, args) =>
                {
                    args.Handled = true;

                    web.CoreWebView2.Navigate(args.Uri);
                };

                web.CoreWebView2.NavigationStarting += Navigation_Starting;

                web.CoreWebView2.NavigationCompleted += Navigation_Completed;

                // A page that pulls in a lot after its first paint would
                // otherwise sit behind the loader; this is the point the
                // user can actually see something.
                web.CoreWebView2.DOMContentLoaded += (s, args) =>
                {
                    SetLoading(false);

                    KeepMeSignedIn();
                };

                web.CoreWebView2.Navigate(StartUrl());
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                XtraMessageBox.Show(
                    this,
                    "The browser component could not start." +
                        Environment.NewLine + Environment.NewLine +
                        "The WebView2 runtime may be missing." +
                        Environment.NewLine + Environment.NewLine +
                        ex.Message,
                    "Nexus Account",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                Close();
            }
        }

        /// <summary>
        /// The page this window is actually for, asked for directly.
        ///
        /// Going through wp-login.php every time meant a signed in
        /// user was still shown a login form, because the form is what
        /// was requested. Asking for the destination instead means the
        /// session is used when there is one.
        /// </summary>
        private string StartUrl()
        {
            switch (mode)
            {
                case NexusAccountWebMode.Register:
                    return NexusAccountService.RegisterUrl;

                case NexusAccountWebMode.EditProfile:
                    return NexusAccountService.ProfileUrl;

                default:
                    return NexusAccountService.AuthorizePageUrl();
            }
        }

        /// <summary>
        /// The same destination, but reached through the login form,
        /// for when the site turns the direct request away.
        ///
        /// wp-login.php rather than whatever wp_login_url() returns: a
        /// plugin on the site filters that to a themed page built from
        /// a shortcode that no longer exists.
        /// </summary>
        private string LoginUrl()
        {
            return mode == NexusAccountWebMode.EditProfile
                ? NexusAccountService.BuildProfileUrl()
                : NexusAccountService.BuildAuthorizeUrl();
        }

        /// <summary>
        /// The file name of the page that means this window has done
        /// its job.
        /// </summary>
        private string TargetFile()
        {
            return mode == NexusAccountWebMode.EditProfile
                ? "profile.php"
                : "authorize-application.php";
        }

        //--------------------------------------------------------------
        // Where did we end up?
        //--------------------------------------------------------------

        private void Navigation_Completed(
            object sender,
            CoreWebView2NavigationCompletedEventArgs e)
        {
            SetLoading(false);

            if (mode == NexusAccountWebMode.Register || completing)
                return;

            string url = CurrentUrl();

            if (string.IsNullOrEmpty(url))
                return;

            if (PathIs(url, TargetFile()))
            {
                // Arrived. Anything said earlier no longer applies.
                HideBlocked();

                return;
            }

            if (PathIs(url, "wp-login.php"))
            {
                sawLoginForm = true;

                HideBlocked();

                return;
            }

            // Somewhere else: either signed out and bounced to the
            // site's own login page, or signed in and turned away from
            // the admin area.
            bounceUrl = url;

            if (!triedLogin)
            {
                triedLogin = true;

                // Signed out, most likely. WordPress sent us to a login
                // page of its own choosing; ask for the real one.
                SetLoading(true);

                web.CoreWebView2.Navigate(LoginUrl());

                return;
            }

            // Past this point the login form has been offered. If it
            // was never actually shown, the site is turning a signed in
            // user away rather than asking them to sign in.
            if (sawLoginForm && directAttempts == 0)
            {
                directAttempts++;

                // The session exists now, so ask for the page on its
                // own. That is enough whenever the redirect_to was
                // discarded rather than the page being off limits.
                SetLoading(true);

                web.CoreWebView2.Navigate(StartUrl());

                return;
            }

            ShowBlocked();
        }

        /// <summary>
        /// Ticks WordPress's "Remember Me" on the sign in form.
        ///
        /// Without it WordPress issues a session cookie, which lasts
        /// only as long as the browser process: close Nexus and the
        /// sign in is gone, which is exactly the thing this window
        /// exists to avoid. With it the cookie is a persistent one and
        /// the store page and profile page stay signed in.
        ///
        /// Only ever touched on the site's own login page, and it only
        /// turns the box on; anything else on the form is the user's.
        /// </summary>
        private async void KeepMeSignedIn()
        {
            if (!PathIs(CurrentUrl(), "wp-login.php"))
                return;

            try
            {
                await web.CoreWebView2.ExecuteScriptAsync(
                    "(function(){" +
                    "var b=document.getElementById('rememberme');" +
                    "if(b&&!b.checked){b.checked=true;" +
                    "b.dispatchEvent(new Event('change',{bubbles:true}));}" +
                    "})();");
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        private string CurrentUrl()
        {
            try
            {
                return web.CoreWebView2 == null
                    ? null
                    : web.CoreWebView2.Source;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Whether a url is that page, by path alone.
        ///
        /// Matching anywhere in the url would be wrong: asking for the
        /// authorize page while signed out lands on
        /// wp-login.php?redirect_to=...authorize-application.php, which
        /// names both pages and is neither of them.
        /// </summary>
        private static bool PathIs(
            string url,
            string fileName)
        {
            try
            {
                Uri uri;

                if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                    return false;

                string last =
                    uri.AbsolutePath.TrimEnd('/');

                int slash =
                    last.LastIndexOf('/');

                if (slash >= 0)
                    last = last.Substring(slash + 1);

                return string.Equals(
                    last,
                    fileName,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        //--------------------------------------------------------------
        // The explanation
        //--------------------------------------------------------------

        private void BuildBlockedPanel()
        {
            blockedPanel.Visible = false;

            blockedPanel.BorderStyle =
                DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;

            blockedTitle.Text =
                mode == NexusAccountWebMode.EditProfile
                    ? "Your profile cannot be opened yet"
                    : "This account cannot be linked yet";

            blockedTitle.AutoSizeMode = LabelAutoSizeMode.None;

            blockedPanel.Controls.Add(blockedTitle);

            blockedText.AutoSizeMode = LabelAutoSizeMode.None;

            blockedText.Appearance.TextOptions.WordWrap =
                DevExpress.Utils.WordWrap.Wrap;

            blockedText.Appearance.Options.UseTextOptions = true;

            blockedText.Text =
                (mode == NexusAccountWebMode.EditProfile
                    ? "The site sent the browser away from your " +
                      "profile page, so there was nothing to show."
                    : "You signed in, but the site sent the browser " +
                      "away from the page that grants Nexus access, " +
                      "so there was nothing to approve.") +
                Environment.NewLine + Environment.NewLine +
                "This is a setting on the website, not a problem with " +
                "your account or with Nexus. Sites running a store or " +
                "a membership plugin usually keep ordinary members out " +
                "of the WordPress admin area, and the page that links " +
                "an app lives there. Administrators are let through, " +
                "which is why it works for them." +
                Environment.NewLine + Environment.NewLine +
                "The site owner can fix this by allowing " +
                "authorize-application.php for every signed in role. " +
                "Until then you can use Nexus with a local account.";

            blockedPanel.Controls.Add(blockedText);

            blockedUrl.AutoSizeMode = LabelAutoSizeMode.None;

            blockedUrl.Appearance.TextOptions.Trimming =
                DevExpress.Utils.Trimming.EllipsisPath;

            blockedUrl.Appearance.Options.UseTextOptions = true;

            blockedPanel.Controls.Add(blockedUrl);

            retryButton.Text = "Try again";

            retryButton.Click += (s, e) =>
            {
                directAttempts = 0;

                triedLogin = false;

                HideBlocked();

                SetLoading(true);

                web.CoreWebView2.Navigate(StartUrl());
            };

            blockedPanel.Controls.Add(retryButton);

            copyButton.Text = "Copy details";

            copyButton.Click += (s, e) => CopyDetails();

            blockedPanel.Controls.Add(copyButton);

            dismissButton.Text = "Show the page anyway";

            dismissButton.Click += (s, e) => HideBlocked();

            blockedPanel.Controls.Add(dismissButton);

            Controls.Add(blockedPanel);

            blockedPanel.BringToFront();
        }

        private void ShowBlocked()
        {
            blockedUrl.Text =
                "The site sent the browser to:  " + bounceUrl;

            LayoutBlocked();

            blockedPanel.Visible = true;

            blockedPanel.BringToFront();
        }

        private void HideBlocked()
        {
            if (blockedPanel.Visible)
                blockedPanel.Visible = false;
        }

        /// <summary>
        /// Everything the site owner needs to act on, in one paste.
        /// </summary>
        private void CopyDetails()
        {
            try
            {
                Clipboard.SetText(
                    "Nexus Launcher could not link a WordPress account." +
                    Environment.NewLine +
                    "Asked for:  " +
                        NexusAccountService.AuthorizePageUrl() +
                    Environment.NewLine +
                    "Ended up at:  " + (bounceUrl ?? "unknown") +
                    Environment.NewLine +
                    Environment.NewLine +
                    "The account signed in, but was redirected away " +
                    "from wp-admin/authorize-application.php, so no " +
                    "application password could be created. This " +
                    "usually means a plugin blocks the admin area for " +
                    "roles without edit_posts, such as customer or " +
                    "subscriber." +
                    Environment.NewLine +
                    Environment.NewLine +
                    "WooCommerce does this by default. Allowing the " +
                    "one page through:" +
                    Environment.NewLine +
                    Environment.NewLine +
                    "add_filter( 'woocommerce_prevent_admin_access', " +
                    "function ( $prevent ) {" + Environment.NewLine +
                    "    global $pagenow;" + Environment.NewLine +
                    "    return 'authorize-application.php' === " +
                    "$pagenow ? false : $prevent;" + Environment.NewLine +
                    "} );");
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        private void LayoutBlocked()
        {
            const int pad = 28;

            int width =
                Math.Min(620, Math.Max(320, ClientSize.Width - 80));

            int height = 340;

            blockedPanel.SetBounds(
                (ClientSize.Width - width) / 2,
                Math.Max(20, (ClientSize.Height - height) / 2),
                width,
                height);

            int inner = width - pad * 2;

            blockedTitle.SetBounds(pad, pad, inner, 26);

            blockedText.SetBounds(pad, pad + 36, inner, 196);

            blockedUrl.SetBounds(pad, pad + 240, inner, 18);

            int buttonTop = height - pad - 30;

            retryButton.SetBounds(pad, buttonTop, 96, 30);

            copyButton.SetBounds(pad + 104, buttonTop, 110, 30);

            dismissButton.SetBounds(pad + 222, buttonTop, 158, 30);
        }

        /// <summary>
        /// Watches for the callback WordPress redirects to once Nexus
        /// has been approved, and takes the credentials off it.
        /// </summary>
        private void Navigation_Starting(
            object sender,
            CoreWebView2NavigationStartingEventArgs e)
        {
            SetLoading(true);

            if (mode != NexusAccountWebMode.Authorize)
                return;

            if (string.IsNullOrEmpty(e.Uri))
                return;

            if (e.Uri.IndexOf(
                    NexusAccountService.CallbackUrl,
                    StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            // Nothing is served at the callback address, so the
            // navigation is stopped before it can 404. Cancelling ends
            // the navigation as a failure, which must not read as a
            // bounce.
            completing = true;

            e.Cancel = true;

            SetLoading(false);

            try
            {
                Uri uri = new Uri(e.Uri);

                NameValueCollection query =
                    HttpUtility.ParseQueryString(uri.Query);

                UserLogin = query["user_login"];

                AppPassword = query["password"];
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            if (IsHandleCreated)
            {
                BeginInvoke(new Action(() =>
                {
                    DialogResult =
                        Authorized
                            ? DialogResult.OK
                            : DialogResult.Cancel;

                    Close();
                }));
            }
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                try
                {
                    web.Dispose();
                }
                catch (Exception)
                {
                }
            }

            base.Dispose(disposing);
        }
    }
}
