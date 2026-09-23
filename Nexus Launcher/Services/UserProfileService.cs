using Nexus_Launcher.Properties;
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
