using Nexus_Launcher.Properties;
using Nexus_Launcher.Services.Account;
using System;
using System.DirectoryServices.AccountManagement;

namespace Nexus_Launcher.Services
{
    /// <summary>
    /// The name Nexus shows for the user: either the Windows account
    /// name or one they chose themselves.
    ///
    /// Everything that displays a name reads DisplayName rather than
    /// asking Windows directly, so changing it in setup or settings
    /// updates the whole app at once.
    /// </summary>
    internal static class UserProfileService
    {
        private static string windowsName;

        /// <summary>
        /// Raised after the name or the Windows-name choice changes.
        /// </summary>
        public static event Action Changed;

        static UserProfileService()
        {
            // Linking or unlinking an online account changes the name
            // Nexus shows, so it counts as a profile change too.
            NexusAccountService.Changed += () =>
            {
                Action handler = Changed;

                if (handler != null)
                    handler();
            };
        }

        /// <summary>
        /// Whether an online Nexus account is linked and its name
        /// should be preferred over the local one.
        /// </summary>
        public static bool UseOnlineName
        {
            get
            {
                return Settings.Default.UseOnlineAccountName;
            }
        }

        public static bool HasOnlineAccount
        {
            get
            {
                return NexusAccountService.IsSignedIn;
            }
        }

        /// <summary>
        /// The name from the linked account, preferring what the site
        /// calls the display name and falling back through the real
        /// name to the login.
        /// </summary>
        public static string OnlineName
        {
            get
            {
                NexusAccount user =
                    NexusAccountService.Current;

                if (user == null)
                    return string.Empty;

                if (!string.IsNullOrWhiteSpace(user.DisplayName))
                    return user.DisplayName.Trim();

                if (!string.IsNullOrWhiteSpace(user.FullName))
                    return user.FullName.Trim();

                return (user.Username ?? string.Empty).Trim();
            }
        }

        /// <summary>
        /// The signed in Windows user's display name, falling back to
        /// the account name.
        ///
        /// Cached because UserPrincipal.Current queries the directory,
        /// which is slow on a domain machine and can throw when one is
        /// unreachable.
        /// </summary>
        public static string WindowsName
        {
            get
            {
                if (windowsName != null)
                    return windowsName;

                try
                {
                    string name =
                        UserPrincipal.Current.DisplayName;

                    windowsName =
                        string.IsNullOrWhiteSpace(name)
                            ? Environment.UserName
                            : name;
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);

                    windowsName = Environment.UserName;
                }

                return windowsName;
            }
        }

        public static bool UseWindowsName
        {
            get
            {
                return Settings.Default.UseWindowsUserName;
            }
        }

        /// <summary>
        /// The custom name, empty when none has been set.
        /// </summary>
        public static string CustomName
        {
            get
            {
                return Settings.Default.UserDisplayName ?? string.Empty;
            }
        }

        /// <summary>
        /// The name to show. Falls back to the Windows name when a
        /// custom one is selected but blank, so the app never shows an
        /// empty name.
        /// </summary>
        public static string DisplayName
        {
            get
            {
                // A linked account wins by default: having signed in,
                // seeing the Windows login name instead would be odd.
                // The preference exists so it can be turned back.
                if (UseOnlineName && HasOnlineAccount)
                {
                    string online = OnlineName;

                    if (!string.IsNullOrWhiteSpace(online))
                        return online;
                }

                if (UseWindowsName)
                    return WindowsName;

                return string.IsNullOrWhiteSpace(CustomName)
                    ? WindowsName
                    : CustomName.Trim();
            }
        }

        /// <summary>
        /// Stores the choice and tells the rest of the app to redraw.
        /// </summary>
        /// <summary>
        /// Stores whether the linked account's name is preferred.
        /// </summary>
        public static void SaveUseOnlineName(
            bool value)
        {
            Settings.Default.UseOnlineAccountName = value;

            Settings.Default.Save();

            Changed?.Invoke();
        }

        public static void Save(
            bool useWindowsName,
            string customName)
        {
            Settings.Default.UseWindowsUserName = useWindowsName;

            Settings.Default.UserDisplayName =
                (customName ?? string.Empty).Trim();

            Settings.Default.Save();

            Changed?.Invoke();
        }
    }
}
