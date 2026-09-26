using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Drawing;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Nexus_Launcher.Services.Account
{
    /// <summary>
    /// The Nexus account as the site describes it.
    ///
    /// Only what /wp/v2/users/me returns. The site runs WooCommerce and
    /// therefore has addresses too, but those are not on this endpoint
    /// and are left for later rather than shown as permanently blank.
    /// </summary>
    public class NexusAccount
    {
        public string Username { get; set; }

        public string DisplayName { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string AvatarUrl { get; set; }

        public string ProfileUrl { get; set; }

        public string Description { get; set; }

        public List<string> Roles { get; set; }

        public DateTime LinkedUtc { get; set; }

        public DateTime? LastSyncedUtc { get; set; }

        /// <summary>
        /// The avatar URL the cached picture was downloaded from. Kept
        /// so a refresh only re-downloads when the picture changed.
        /// </summary>
        public string CachedAvatarUrl { get; set; }

        /// <summary>
        /// The WordPress application password, encrypted for this
        /// Windows user. Never the account's real password: the real
        /// one is only ever typed on the site itself.
        /// </summary>
        public string ProtectedAppPassword { get; set; }

        public NexusAccount()
        {
            Roles = new List<string>();
        }

        public string FullName
        {
            get
            {
                string name =
                    ((FirstName ?? string.Empty) + " " +
                     (LastName ?? string.Empty)).Trim();

                return name.Length > 0
                    ? name
                    : (DisplayName ?? Username ?? string.Empty);
            }
        }

        public string RoleText
        {
            get
            {
                if (Roles == null || Roles.Count == 0)
                    return string.Empty;

                // WordPress role slugs are lower case with underscores.
                return string.Join(
                    ", ",
                    Roles.Select(Pretty));
            }
        }

        private static string Pretty(
            string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return string.Empty;

            string spaced =
                role.Replace('_', ' ').Replace('-', ' ');

            return char.ToUpperInvariant(spaced[0]) +
                (spaced.Length > 1 ? spaced.Substring(1) : string.Empty);
        }
    }

    /// <summary>
    /// Links Nexus to a WordPress account on the Nexus site and keeps
    /// the profile it reads back.
    ///
    /// Authentication uses WordPress application passwords, which the
    /// site advertises at its REST root. The user signs in on the real
    /// site in a browser view and approves Nexus; WordPress then hands
    /// back a password that is only valid for this application and can
    /// be revoked from the user's profile page. Nexus never sees, and
    /// never stores, the account's actual password, which also means
    /// two factor prompts and login security plugins keep working.
    /// </summary>
    internal static class NexusAccountService
    {
        public const string SiteUrl =
            "https://nexuslauncher.guardbyte.me";

        public const string LoginUrl =
            SiteUrl + "/wp-login.php";

        public const string RegisterUrl =
            SiteUrl + "/wp-login.php?action=register";

        public const string ProfileUrl =
            SiteUrl + "/wp-admin/profile.php";

        /// <summary>
        /// Where WordPress sends the browser once the user approves.
        /// Nothing needs to exist at this address: the navigation is
        /// intercepted before it loads, and the credentials arrive as
        /// query parameters on it.
        /// </summary>
        public const string CallbackUrl =
            SiteUrl + "/nexus-launcher-authorized";

        private const string AppName = "Nexus Launcher";

        private static readonly string SaveFile =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "NexusLauncher",
                "Account.json");

        /// <summary>
        /// The account picture, cached so the profile page can show it
        /// without a round trip and while offline.
        /// </summary>
        private static readonly string AvatarFile =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "NexusLauncher",
                "Avatar.png");

        /// <summary>
        /// Gravatar and WordPress both size avatars from the query
        /// string, and the REST response asks for one far too small for
        /// a 128 pixel frame.
        /// </summary>
        private const int AvatarSize = 256;

        private static readonly object sync = new object();

        private static NexusAccount account;

        private static bool loaded;

        /// <summary>
        /// Raised after signing in, signing out or a refresh.
        /// </summary>
        public static event Action Changed;

        //--------------------------------------------------------------
        // State
        //--------------------------------------------------------------

        public static NexusAccount Current
        {
            get
            {
                lock (sync)
                {
                    if (!loaded)
                    {
                        account = LoadFromDisk();

                        loaded = true;
                    }

                    return account;
                }
            }
        }

        public static bool IsSignedIn
        {
            get
            {
                NexusAccount current = Current;

                return current != null &&
                    !string.IsNullOrEmpty(current.Username);
            }
        }

        /// <summary>
        /// The page that asks WordPress to mint an application password
        /// for Nexus, without going through the login form first.
        ///
        /// Public because the browser view asks for it directly when a
        /// plugin has thrown away the login form's redirect_to: once a
        /// session exists, requesting the page on its own works where
        /// the redirect did not.
        /// </summary>
        public static string AuthorizePageUrl()
        {
            return AuthorizePage();
        }

        /// <summary>
        /// The page that asks WordPress to mint an application password
        /// for Nexus.
        /// </summary>
        private static string AuthorizePage()
        {
            return SiteUrl +
                "/wp-admin/authorize-application.php" +
                "?app_name=" + Uri.EscapeDataString(AppName) +
                "&success_url=" + Uri.EscapeDataString(CallbackUrl);
        }

        /// <summary>
        /// Where to send the browser view to sign in.
        ///
        /// Goes to wp-login.php and carries the real destination in
        /// redirect_to, rather than opening the destination and letting
        /// WordPress bounce to a login page. A plugin on the site
        /// filters wp_login_url() to a themed /login/ page, so that
        /// bounce lands on a page built from a shortcode that is no
        /// longer in use. Requesting wp-login.php directly is not
        /// filtered and serves the real form, which then forwards to
        /// redirect_to once the user is in.
        /// </summary>
        public static string BuildAuthorizeUrl()
        {
            return BuildLoginUrl(AuthorizePage());
        }

        /// <summary>
        /// The profile page, reached the same way so an expired session
        /// cannot drop the user on the broken login page either.
        /// </summary>
        public static string BuildProfileUrl()
        {
            return BuildLoginUrl(ProfileUrl);
        }

        private static string BuildLoginUrl(
            string destination)
        {
            return LoginUrl +
                "?redirect_to=" + Uri.EscapeDataString(destination);
        }

        //--------------------------------------------------------------
        // Storage
        //--------------------------------------------------------------

        private static NexusAccount LoadFromDisk()
        {
            try
            {
                if (!File.Exists(SaveFile))
                    return null;

                return JsonConvert.DeserializeObject<NexusAccount>(
                    File.ReadAllText(SaveFile));
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return null;
            }
        }

        private static void Save()
        {
            Save(false);
        }

        private static void Save(
            bool forget)
        {
            try
            {
                string folder =
                    Path.GetDirectoryName(SaveFile);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                NexusAccount snapshot;

                lock (sync)
                {
                    snapshot = account;
                }

                if (snapshot == null)
                {
                    // Only an explicit sign out removes the stored
                    // account. Reaching here with nothing in memory and
                    // no intent to forget means something saved at the
                    // wrong moment, and deleting would throw away a
                    // working link for no reason.
                    if (!forget)
                        return;

                    if (File.Exists(SaveFile))
                        File.Delete(SaveFile);
                }
                else
                {
                    File.WriteAllText(
                        SaveFile,
                        JsonConvert.SerializeObject(
                            snapshot,
                            Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            Action handler = Changed;

            if (handler != null)
                handler();
        }

        //--------------------------------------------------------------
        // Secret handling
        //--------------------------------------------------------------

        /// <summary>
        /// Application passwords are encrypted with DPAPI for the
        /// current Windows user, so the file is useless if copied to
        /// another machine or another account.
        /// </summary>
        private static string Protect(
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            try
            {
                byte[] encrypted =
                    ProtectedData.Protect(
                        Encoding.UTF8.GetBytes(value),
                        null,
                        DataProtectionScope.CurrentUser);

                return Convert.ToBase64String(encrypted);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return null;
            }
        }

        private static string Unprotect(
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            try
            {
                byte[] plain =
                    ProtectedData.Unprotect(
                        Convert.FromBase64String(value),
                        null,
                        DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(plain);
            }
            catch (Exception)
            {
                // Copied from another machine, or the profile changed.
                // Treated as not signed in rather than as an error.
                return null;
            }
        }

        //--------------------------------------------------------------
        // Account picture
        //--------------------------------------------------------------

        /// <summary>
        /// The cached account picture, or null when there is none.
        ///
        /// Loaded without holding the file open, so a later download
        /// can replace it.
        /// </summary>
        public static Image LoadAvatar()
        {
            try
            {
                if (!File.Exists(AvatarFile))
                    return null;

                byte[] bytes =
                    File.ReadAllBytes(AvatarFile);

                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    using (Image loaded = Image.FromStream(stream))
                    {
                        return new Bitmap(loaded);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return null;
            }
        }

        /// <summary>
        /// Asks for the picture at a usable size rather than the
        /// thumbnail the REST response points at.
        /// </summary>
        private static string UpsizeAvatarUrl(
            string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;

            return System.Text.RegularExpressions.Regex.Replace(
                url,
                @"([?&])s=\d+",
                "$1s=" + AvatarSize);
        }

        /// <summary>
        /// Fetches the account picture when it is missing or has
        /// changed. Failure is not an error: the profile simply keeps
        /// whatever picture it had.
        /// </summary>
        private static async Task DownloadAvatarAsync(
            NexusAccount user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.AvatarUrl))
                return;

            string wanted =
                UpsizeAvatarUrl(user.AvatarUrl);

            bool haveIt =
                File.Exists(AvatarFile) &&
                string.Equals(
                    user.CachedAvatarUrl,
                    wanted,
                    StringComparison.OrdinalIgnoreCase);

            if (haveIt)
                return;

            try
            {
                ServicePointManager.SecurityProtocol |=
                    SecurityProtocolType.Tls12;

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(20);

                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "NexusLauncher");

                    byte[] bytes =
                        await client.GetByteArrayAsync(wanted);

                    if (bytes == null || bytes.Length == 0)
                        return;

                    string folder =
                        Path.GetDirectoryName(AvatarFile);

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    File.WriteAllBytes(AvatarFile, bytes);

                    user.CachedAvatarUrl = wanted;
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        private static void DeleteAvatar()
        {
            try
            {
                if (File.Exists(AvatarFile))
                    File.Delete(AvatarFile);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        //--------------------------------------------------------------
        // Signing in
        //--------------------------------------------------------------

        /// <summary>
        /// Completes a sign in from the values WordPress put on the
        /// callback URL, then reads the profile back.
        /// </summary>
        public static async Task<bool> CompleteSignInAsync(
            string userLogin,
            string appPassword)
        {
            if (string.IsNullOrWhiteSpace(userLogin) ||
                string.IsNullOrWhiteSpace(appPassword))
            {
                return false;
            }

            NexusAccount created =
                new NexusAccount();

            created.Username = userLogin.Trim();

            created.LinkedUtc = DateTime.UtcNow;

            created.ProtectedAppPassword =
                Protect(appPassword.Replace(" ", string.Empty));

            lock (sync)
            {
                account = created;

                loaded = true;
            }

            bool ok =
                await RefreshAsync();

            if (!ok)
            {
                // Credentials that cannot read a profile are no use.
                SignOut();

                return false;
            }

            return true;
        }

        /// <summary>
        /// What a silent startup check found.
        /// </summary>
        public enum SignInState
        {
            /// <summary>
            /// No account has ever been linked on this PC, so Nexus is
            /// being used locally and should not nag.
            /// </summary>
            NoAccount,

            /// <summary>
            /// Stored credentials still work; the profile is fresh.
            /// </summary>
            SignedIn,

            /// <summary>
            /// An account is linked but the site would not accept it.
            /// Usually the application password was revoked, so the
            /// user has to approve Nexus again.
            /// </summary>
            NeedsSignIn
        }

        /// <summary>
        /// Signs back in from what was saved last time.
        ///
        /// The application password is kept between runs, so a linked
        /// account comes back without the user doing anything. This
        /// call just confirms the site still accepts it and refreshes
        /// the profile while it is at it.
        /// </summary>
        public static async Task<SignInState> RestoreAsync()
        {
            if (!IsSignedIn)
                return SignInState.NoAccount;

            bool ok =
                await RefreshAsync();

            if (ok)
                return SignInState.SignedIn;

            // The account is kept rather than discarded: the username
            // is still worth showing, and signing in again only needs
            // a fresh approval.
            return SignInState.NeedsSignIn;
        }

        public static void SignOut()
        {
            lock (sync)
            {
                account = null;

                loaded = true;
            }

            DeleteAvatar();

            Save(true);
        }

        //--------------------------------------------------------------
        // Reading the profile
        //--------------------------------------------------------------

        /// <summary>
        /// Pulls the profile from the site into the stored account.
        /// Returns false when the credentials no longer work.
        /// </summary>
        public static async Task<bool> RefreshAsync()
        {
            NexusAccount current = Current;

            if (current == null ||
                string.IsNullOrEmpty(current.Username))
            {
                return false;
            }

            string password =
                Unprotect(current.ProtectedAppPassword);

            if (string.IsNullOrEmpty(password))
                return false;

            try
            {
                // TLS 1.2 is not the default on .NET Framework 4.8 for
                // every host configuration, and the site refuses less.
                ServicePointManager.SecurityProtocol |=
                    SecurityProtocolType.Tls12;

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue(
                            "Basic",
                            Convert.ToBase64String(
                                Encoding.UTF8.GetBytes(
                                    current.Username + ":" + password)));

                    // Some hosts reject requests with no user agent.
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "NexusLauncher");

                    HttpResponseMessage response =
                        await client.GetAsync(
                            SiteUrl +
                            "/wp-json/wp/v2/users/me?context=edit");

                    if (!response.IsSuccessStatusCode)
                        return false;

                    string body =
                        await response.Content.ReadAsStringAsync();

                    Apply(current, JObject.Parse(body));
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return false;
            }

            current.LastSyncedUtc = DateTime.UtcNow;

            // The picture is part of the profile, so it comes down with
            // the rest of it. Done before the save so the URL it came
            // from is stored alongside.
            await DownloadAvatarAsync(current);

            Save();

            return true;
        }

        private static void Apply(
            NexusAccount target,
            JObject json)
        {
            target.Username =
                Text(json, "slug") ?? target.Username;

            target.DisplayName = Text(json, "name");

            target.FirstName = Text(json, "first_name");

            target.LastName = Text(json, "last_name");

            target.Email = Text(json, "email");

            target.Description = Text(json, "description");

            target.ProfileUrl = Text(json, "link");

            JToken roles = json["roles"];

            target.Roles =
                roles is JArray
                    ? roles.Select(x => (string)x)
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList()
                    : new List<string>();

            // avatar_urls is keyed by pixel size; take the largest.
            JObject avatars =
                json["avatar_urls"] as JObject;

            if (avatars != null)
            {
                target.AvatarUrl =
                    avatars.Properties()
                        .OrderByDescending(p =>
                        {
                            int size;

                            return int.TryParse(p.Name, out size)
                                ? size
                                : 0;
                        })
                        .Select(p => (string)p.Value)
                        .FirstOrDefault();
            }
        }

        private static string Text(
            JObject json,
            string field)
        {
            JToken token = json[field];

            if (token == null || token.Type == JTokenType.Null)
                return null;

            // "name" and friends are plain strings; some fields come
            // back as { "raw": "...", "rendered": "..." } in edit
            // context.
            if (token.Type == JTokenType.Object)
            {
                JToken raw = token["raw"] ?? token["rendered"];

                return raw == null ? null : (string)raw;
            }

            return (string)token;
        }
    }
}
