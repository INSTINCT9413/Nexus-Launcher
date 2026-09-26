using DevExpress.Utils.Html;
using DevExpress.XtraEditors;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Forms;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services.Account;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher.Forms
{
    /// <summary>
    /// The Nexus account dialog: the branded front door for signing in,
    /// creating an account, or seeing the account already linked.
    ///
    /// Styled with the same HTML and CSS as the original sign in popup.
    /// It deliberately has no password field. Every step that involves
    /// credentials opens the real site in NexusAccountWebForm, so this
    /// dialog only ever shows what came back.
    /// </summary>
    internal class NexusAccountDialog : XtraForm
    {
        private readonly HtmlContentControl content =
            new HtmlContentControl();

        private bool busy;

        /// <summary>
        /// A borderless window has no title bar to drag, so the card
        /// itself is the handle.
        /// </summary>
        private bool dragArmed;

        private bool dragging;

        private Point dragOrigin;

        private Point dragWindowOrigin;

        /// <summary>
        /// How far the pointer has to travel before a press counts as
        /// a drag. Without it, every click would nudge the window and
        /// the buttons underneath would feel unreliable.
        /// </summary>
        private const int DragThreshold = 4;

        /// <summary>
        /// The user chose to carry on with a local account, so the
        /// caller should show them their local profile.
        /// </summary>
        public bool LocalAccountRequested { get; private set; }

        //--------------------------------------------------------------
        // Geometry
        //
        // The window is cut to the shape of the card plus the logo
        // circle that overhangs it, so there is no square of form
        // background sitting behind the rounded corners. These have to
        // agree with the padding and sizes in the stylesheet below.
        //--------------------------------------------------------------

        private const int SidePad = 20;

        private const int TopPad = 78;

        private const int BottomPad = 20;

        private const int CornerRadius = 14;

        private const int LogoSize = 150;

        private const int LogoTop = 10;

        public NexusAccountDialog()
        {
            Text = "Nexus Account";

            FormBorderStyle = FormBorderStyle.None;

            StartPosition = FormStartPosition.CenterParent;

            ClientSize = new Size(460, 660);

            ShowInTaskbar = false;

            KeyPreview = true;

            // No shadow is drawn any more: it would fall outside
            // the region and be clipped to a hard edge, which looks
            // worse than no shadow at all.
            content.Dock = DockStyle.Fill;

            content.HtmlImages = BuildImages();

            content.ElementMouseClick += Element_Click;

            content.MouseDown += Content_MouseDown;

            content.MouseMove += Content_MouseMove;

            content.MouseUp += Content_MouseUp;

            Controls.Add(content);

            Render();

            Shape();
        }

        protected override void OnResize(
            EventArgs e)
        {
            base.OnResize(e);

            Shape();
        }

        /// <summary>
        /// Clips the window to the card and the logo circle above it.
        ///
        /// A region rather than a transparency key: the key colour is
        /// all or nothing per pixel, and picking one that never clashes
        /// with a themed card is not possible. The cost is that the
        /// rounded corners are not antialiased.
        /// </summary>
        private void Shape()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return;

            Rectangle card =
                new Rectangle(
                    SidePad,
                    TopPad,
                    Math.Max(1, ClientSize.Width - SidePad * 2),
                    Math.Max(1, ClientSize.Height - TopPad - BottomPad));

            using (GraphicsPath path = new GraphicsPath())
            {
                path.FillMode = FillMode.Winding;

                AddRounded(path, card, CornerRadius);

                path.AddEllipse(
                    (ClientSize.Width - LogoSize) / 2,
                    LogoTop,
                    LogoSize,
                    LogoSize);

                Region old = Region;

                Region = new Region(path);

                if (old != null)
                    old.Dispose();
            }
        }

        private static void AddRounded(
            GraphicsPath path,
            Rectangle bounds,
            int radius)
        {
            int d = radius * 2;

            if (d <= 0 || bounds.Width <= d || bounds.Height <= d)
            {
                path.AddRectangle(bounds);

                return;
            }

            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);

            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);

            path.AddArc(
                bounds.Right - d,
                bounds.Bottom - d,
                d,
                d,
                0,
                90);

            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);

            path.CloseFigure();
        }

        /// <summary>
        /// True once the signed in user's own picture has been added to
        /// the image collection, so the markup can use it in place of
        /// the Nexus logo.
        /// </summary>
        private bool hasAvatar;

        private DevExpress.Utils.ImageCollection BuildImages()
        {
            DevExpress.Utils.ImageCollection images =
                new DevExpress.Utils.ImageCollection();

            images.ImageSize = new Size(192, 192);

            hasAvatar = false;

            try
            {
                images.AddImage(
                    Resources.dfveffb_9b262552_e352_4348_aefc_8e699002c946,
                    "nexus-logo");

                images.AddImage(Resources.icons8_error_24, "close");

                if (NexusAccountService.IsSignedIn)
                {
                    Image avatar =
                        NexusAccountService.LoadAvatar();

                    if (avatar != null)
                    {
                        images.AddImage(avatar, "nexus-avatar");

                        hasAvatar = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            return images;
        }

        protected override void OnKeyDown(
            KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Escape)
                Close();
        }

        //--------------------------------------------------------------
        // Dragging
        //
        // Deliberately additive: the press is only turned into a move
        // once the pointer has travelled, so a plain click still
        // reaches the element underneath and the buttons keep working.
        //--------------------------------------------------------------

        private void Content_MouseDown(
            object sender,
            MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            dragArmed = true;

            dragging = false;

            dragOrigin = Control.MousePosition;

            dragWindowOrigin = Location;
        }

        private void Content_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!dragArmed)
                return;

            Point now = Control.MousePosition;

            int dx = now.X - dragOrigin.X;

            int dy = now.Y - dragOrigin.Y;

            if (!dragging &&
                Math.Abs(dx) + Math.Abs(dy) < DragThreshold)
            {
                return;
            }

            dragging = true;

            Location =
                new Point(
                    dragWindowOrigin.X + dx,
                    dragWindowOrigin.Y + dy);
        }

        private void Content_MouseUp(
            object sender,
            MouseEventArgs e)
        {
            dragArmed = false;

            dragging = false;
        }

        //--------------------------------------------------------------
        // Markup
        //--------------------------------------------------------------

        /// <summary>
        /// Carried over from the original popup so the dialog keeps the
        /// look it was designed with: rounded frame, floating logo, and
        /// a footer of full width buttons.
        /// </summary>
        private const string Styles = @"
body {
    padding: 78px 20px 20px 20px;
    margin: 0px;
}
.shadow {
    border-radius: 14px;
    height: 100%;
}
.frame {
    height: 100%;
    display: flex;
    flex-direction: column;
    align-items: stretch;
    border-radius: 14px;
    border: 1px solid rgba(0, 0, 0, 0.2);
    background-color: @Control;
}
.logo {
    position: absolute;
    top: 10px;
    left: 50%;
    display: flex;
    justify-content: center;
    align-items: center;
    width: 150px;
    height: 150px;
    border-radius: 90px;
    background-color: @Control;
    box-shadow: 0px 6px 10px 0px rgba(0, 0, 0, 0.5);
    border: 1px solid rgba(0, 0, 0, 0.2);
    margin-left: -75px;
}
.logo-mask {
    width: 140px;
    height: 140px;
    border-radius: 70px;
    background-image: url('nexus-logo');
    background-position: center;
    background-repeat: no-repeat;
    background-size: 105%;
}
/* The signed in user's own picture, in place of the Nexus logo. */
.logo-avatar {
    width: 140px;
    height: 140px;
    border-radius: 70px;
    background-image: url('nexus-avatar');
    background-position: center;
    background-repeat: no-repeat;
    background-size: 100%;
}
.titlebar {
    display: flex;
    flex-direction: row;
    justify-content: flex-end;
    height: 30px;
    padding: 8px 10px 0px 0px;
}
.close {
    width: 24px;
    height: 24px;
    font-size: 14px;
    color: @Text/0.55;
    border-radius: 5px;
    display: flex;
    justify-content: center;
    align-items: center;
}
.close:hover { background-color: @Text/0.15; color: @Text; }

.content {
    margin-top: 66px;
    flex-grow: 1;
    padding: 0px 26px 0px 26px;
}
.title {
    font-size: 20px;
    font-weight: bold;
    color: @Text;
    text-align: center;
    margin-bottom: 6px;
}
.subtitle {
    font-size: 11px;
    color: @Text/0.6;
    text-align: center;
    margin-bottom: 18px;
}
.row {
    display: flex;
    flex-direction: row;
    justify-content: space-between;
    padding: 7px 0px 7px 0px;
    border-bottom: 1px solid @Text/0.08;
}
.label { font-size: 11px; color: @Text/0.55; }
.value { font-size: 11px; color: @Text; font-weight: bold; }
.note {
    font-size: 10px;
    color: @Text/0.5;
    text-align: center;
    margin-top: 16px;
}
.footerbar {
    display: flex;
    flex-direction: column;
    background-color: @ControlText/0.05;
    border-radius: 0px 0px 13px 13px;
}
.button {
    height: 46px;
    width: 100%;
    font-size: 15px;
    font-weight: bold;
    display: flex;
    align-items: center;
    justify-content: center;
    color: @Text;
}
.button:hover { background-color: @Primary; color: @HighlightText; }
.button.last { border-radius: 0px 0px 13px 13px; }
.button.quiet { font-size: 12px; font-weight: normal; height: 38px; color: @Text/0.6; }
";

        private string BuildTemplate()
        {
            NexusAccount user =
                NexusAccountService.Current;

            bool signedIn =
                NexusAccountService.IsSignedIn;

            string body =
                signedIn
                    ? SignedInBody(user)
                    : SignedOutBody();

            string footer =
                signedIn
                    ? @"
        <div class='button' id='refresh'>Refresh from site</div>
        <div class='button' id='manage'>Manage profile</div>
        <div class='button quiet last' id='signout'>Sign out</div>"
                    : @"
        <div class='button' id='signin'>Sign in</div>
        <div class='button' id='register'>Create an account</div>
        <div class='button quiet last' id='close2'>Use a local account instead</div>";

            return @"
<div class='shadow'>
  <div class='frame'>
    <div class='logo'><div class='" + (hasAvatar ? "logo-avatar" : "logo-mask") + @"'></div></div>
    <div class='titlebar'><div class='close' id='close'>&#10005;</div></div>
    <div class='content'>" + body + @"</div>
    <div class='footerbar'>" + footer + @"</div>
  </div>
</div>";
        }

        private string SignedOutBody()
        {
            return @"
      <div class='title'>Nexus Account</div>
      <div class='subtitle'>Sign in to bring your profile into Nexus Launcher.</div>
      <div class='note'>You sign in on nexuslauncher.guardbyte.me.
        Nexus never sees your password, and you can revoke its access
        from your profile page at any time.</div>";
        }

        private string SignedInBody(
            NexusAccount user)
        {
            if (user == null)
                return SignedOutBody();

            string rows =
                Row("Username", user.Username) +
                Row("Name", user.FullName) +
                Row("Email", user.Email) +
                Row("Role", user.RoleText) +
                Row("Last synced", Synced(user));

            return @"
      <div class='title'>" + Escape(
                string.IsNullOrWhiteSpace(user.DisplayName)
                    ? user.Username
                    : user.DisplayName) + @"</div>
      <div class='subtitle'>Signed in to nexuslauncher.guardbyte.me</div>"
                + rows + @"
      <div class='note'>Nexus reads this profile from the site.
        Use Manage profile to change any of it.</div>";
        }

        private static string Synced(
            NexusAccount user)
        {
            return user.LastSyncedUtc.HasValue
                ? user.LastSyncedUtc.Value.ToLocalTime()
                    .ToString("d MMM yyyy, HH:mm")
                : "Never";
        }

        private static string Row(
            string label,
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                value = "Not set";

            return @"
      <div class='row'><div class='label'>" + Escape(label) +
                "</div><div class='value'>" + Escape(value) +
                "</div></div>";
        }

        private static string Escape(
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("'", "&#39;");
        }

        private void Render()
        {
            // Rebuilt every time: signing in or out while the dialog is
            // open changes which pictures it needs.
            content.HtmlImages = BuildImages();

            content.HtmlTemplate.Styles = Styles;

            content.HtmlTemplate.Template = BuildTemplate();
        }

        //--------------------------------------------------------------
        // Actions
        //--------------------------------------------------------------

        private async void Element_Click(
            object sender,
            DxHtmlElementMouseEventArgs e)
        {
            if (busy || e.ElementId == null)
                return;

            switch (e.ElementId)
            {
                case "close":
                    Close();
                    break;

                case "close2":
                    // Carrying on locally still means they want to see
                    // their profile, the same as the header button.
                    LocalAccountRequested = true;

                    Close();
                    break;

                case "signin":
                    await SignInAsync();
                    break;

                case "register":
                    Register();
                    break;

                case "manage":
                    await ManageAsync();
                    break;

                case "refresh":
                    await RefreshAsync();
                    break;

                case "signout":
                    SignOut();
                    break;
            }
        }

        private async Task SignInAsync()
        {
            string login = null;
            string password = null;

            using (NexusAccountWebForm web =
                new NexusAccountWebForm(NexusAccountWebMode.Authorize))
            {
                web.ShowDialog(this);

                if (!web.Authorized)
                    return;

                login = web.UserLogin;

                password = web.AppPassword;
            }

            busy = true;

            try
            {
                bool ok =
                    await NexusAccountService.CompleteSignInAsync(
                        login,
                        password);

                if (!ok)
                {
                    XtraMessageBox.Show(
                        this,
                        "Nexus was approved but could not read your " +
                            "profile back from the site. Please try again.",
                        "Nexus Account",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            finally
            {
                busy = false;
            }

            Render();
        }

        private void Register()
        {
            using (NexusAccountWebForm web =
                new NexusAccountWebForm(NexusAccountWebMode.Register))
            {
                web.ShowDialog(this);
            }

            // Registration does not sign anyone in, so the dialog stays
            // on its signed out face with the Sign in button ready.
            Render();
        }

        private async Task ManageAsync()
        {
            using (NexusAccountWebForm web =
                new NexusAccountWebForm(NexusAccountWebMode.EditProfile))
            {
                web.ShowDialog(this);
            }

            // Anything changed on the site is pulled straight back.
            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            busy = true;

            try
            {
                bool ok =
                    await NexusAccountService.RefreshAsync();

                if (!ok)
                {
                    XtraMessageBox.Show(
                        this,
                        "Nexus could not reach your profile. The " +
                            "access you granted may have been revoked " +
                            "on the site.",
                        "Nexus Account",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            finally
            {
                busy = false;
            }

            Render();
        }

        private void SignOut()
        {
            if (XtraMessageBox.Show(
                this,
                "Sign out of your Nexus account?" +
                    Environment.NewLine + Environment.NewLine +
                    "This removes the stored access from this PC. Your " +
                    "account on the site is not affected.",
                "Nexus Account",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            NexusAccountService.SignOut();

            Render();
        }
    }
}
