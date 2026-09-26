using Microsoft.Win32;
using Nexus_Launcher.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Nexus_Launcher.Services.Themes
{
    /// <summary>
    /// Makes Windows open .nexustheme files with Nexus, and carries a
    /// double clicked file to the copy of Nexus that is already
    /// running.
    ///
    /// Everything is written under HKEY_CURRENT_USER, so this never
    /// needs administrator rights and never touches anything for other
    /// people on the machine.
    /// </summary>
    internal static class ThemeFileAssociation
    {
        public const string Extension = ThemePackage.Extension;

        /// <summary>
        /// The ProgID. Versioned so a later format can be introduced
        /// without inheriting this one's registry leftovers.
        /// </summary>
        private const string ProgId = "NexusLauncher.Theme.1";

        private const string FriendlyName = "Nexus Launcher theme";

        private const string ClassesKey = @"Software\Classes";

        //--------------------------------------------------------------
        // Telling the shell
        //--------------------------------------------------------------

        private const int SHCNE_ASSOCCHANGED = 0x08000000;

        private const int SHCNF_IDLIST = 0x0000;

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(
            int eventId,
            int flags,
            IntPtr item1,
            IntPtr item2);

        //--------------------------------------------------------------
        // Handing a file to the running copy
        //--------------------------------------------------------------

        /// <summary>
        /// Signalled after a file has been dropped in the inbox.
        /// </summary>
        public const string OpenEventName = "NexusLauncher_OpenTheme";

        /// <summary>
        /// A second instance cannot hand a path over on the command
        /// line, so it writes it here and signals. A folder of small
        /// files rather than a single one, because two files opened
        /// together are two processes writing at once.
        /// </summary>
        private static readonly string Inbox =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "NexusLauncher",
                "Inbox");

        //--------------------------------------------------------------
        // Registration
        //--------------------------------------------------------------

        public static bool IsRegistered
        {
            get
            {
                try
                {
                    using (RegistryKey key =
                        Registry.CurrentUser.OpenSubKey(
                            ClassesKey + "\\" + ProgId +
                            @"\shell\open\command"))
                    {
                        if (key == null)
                            return false;

                        string command =
                            key.GetValue(null) as string;

                        return !string.IsNullOrEmpty(command) &&
                            command.IndexOf(
                                Application.ExecutablePath,
                                StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);

                    return false;
                }
            }
        }

        public static bool Register(
            out string error)
        {
            error = null;

            try
            {
                string exe =
                    Application.ExecutablePath;

                using (RegistryKey classes =
                    Registry.CurrentUser.CreateSubKey(ClassesKey))
                {
                    using (RegistryKey prog =
                        classes.CreateSubKey(ProgId))
                    {
                        prog.SetValue(null, FriendlyName);

                        using (RegistryKey icon =
                            prog.CreateSubKey("DefaultIcon"))
                        {
                            icon.SetValue(null, "\"" + exe + "\",0");
                        }

                        using (RegistryKey command =
                            prog.CreateSubKey(@"shell\open\command"))
                        {
                            command.SetValue(
                                null,
                                "\"" + exe + "\" \"%1\"");
                        }
                    }

                    using (RegistryKey ext =
                        classes.CreateSubKey(Extension))
                    {
                        ext.SetValue(null, ProgId);

                        ext.SetValue("Content Type", "application/json");

                        ext.SetValue("PerceivedType", "text");

                        // Puts Nexus in "Open with" even when Windows
                        // has its own idea of the default.
                        using (RegistryKey open =
                            ext.CreateSubKey("OpenWithProgids"))
                        {
                            open.SetValue(
                                ProgId,
                                new byte[0],
                                RegistryValueKind.None);
                        }
                    }
                }

                Announce();

                return true;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                error = ex.Message;

                return false;
            }
        }

        public static bool Unregister(
            out string error)
        {
            error = null;

            try
            {
                using (RegistryKey classes =
                    Registry.CurrentUser.OpenSubKey(ClassesKey, true))
                {
                    if (classes == null)
                        return true;

                    try
                    {
                        classes.DeleteSubKeyTree(ProgId, false);
                    }
                    catch (Exception)
                    {
                    }

                    // The extension key is only given up when it is
                    // still ours. Another program may have taken it
                    // over since, and taking that away is not ours to
                    // do.
                    using (RegistryKey ext =
                        classes.OpenSubKey(Extension, true))
                    {
                        if (ext != null)
                        {
                            if (Equals(ext.GetValue(null) as string, ProgId))
                                ext.SetValue(null, string.Empty);

                            using (RegistryKey open =
                                ext.OpenSubKey("OpenWithProgids", true))
                            {
                                if (open != null)
                                    open.DeleteValue(ProgId, false);
                            }
                        }
                    }

                    // Clearing the values would leave an empty key
                    // behind for an extension nothing on the machine
                    // handles any more, so if all that is left is what
                    // Nexus put there, the key goes too.
                    if (IsOnlyOurs(classes))
                        classes.DeleteSubKeyTree(Extension, false);
                }

                Announce();

                return true;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                error = ex.Message;

                return false;
            }
        }

        /// <summary>
        /// Whether the extension key holds nothing but the entries
        /// Nexus writes, so removing it takes nothing of anyone
        /// else's with it.
        /// </summary>
        private static bool IsOnlyOurs(
            RegistryKey classes)
        {
            try
            {
                using (RegistryKey ext =
                    classes.OpenSubKey(Extension))
                {
                    if (ext == null)
                        return false;

                    string handler =
                        ext.GetValue(null) as string;

                    if (!string.IsNullOrEmpty(handler) && handler != ProgId)
                        return false;

                    string[] ours =
                    {
                        "Content Type",
                        "PerceivedType"
                    };

                    foreach (string name in ext.GetValueNames())
                    {
                        if (name.Length == 0)
                            continue;

                        if (!ours.Contains(
                                name,
                                StringComparer.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }

                    foreach (string sub in ext.GetSubKeyNames())
                    {
                        if (!string.Equals(
                                sub,
                                "OpenWithProgids",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }

                        using (RegistryKey open = ext.OpenSubKey(sub))
                        {
                            // Somebody else's ProgID in here means the
                            // key is no longer only ours.
                            if (open != null &&
                                open.GetValueNames()
                                    .Any(x => x.Length > 0))
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return false;
            }
        }

        /// <summary>
        /// Decides at startup whether Nexus should own .nexustheme
        /// files, and makes it so.
        ///
        /// The extension is Nexus's own invention, so claiming it is
        /// not taking anything from anyone: a theme file that does
        /// nothing when double clicked is simply broken. It is
        /// therefore claimed without asking the first time, and the
        /// setting is authoritative from then on, so someone who turns
        /// it off does not find it back the next morning.
        /// </summary>
        public static void ApplyStartupPreference()
        {
            try
            {
                if (!Settings.Default.ThemeAssociationInitialised)
                {
                    Settings.Default.ThemeAssociationInitialised = true;

                    // Also covers anyone whose settings were written
                    // while this defaulted to off.
                    Settings.Default.AssociateThemeFiles = true;

                    Settings.Default.Save();
                }

                if (!Settings.Default.AssociateThemeFiles)
                    return;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return;
            }

            Reassert();
        }

        /// <summary>
        /// Re-points the association at this executable.
        ///
        /// Called on startup when the association is switched on, so
        /// moving or reinstalling Nexus does not quietly leave the
        /// shell opening a path that no longer exists.
        /// </summary>
        public static void Reassert()
        {
            if (IsRegistered)
                return;

            string error;

            Register(out error);
        }

        private static void Announce()
        {
            try
            {
                SHChangeNotify(
                    SHCNE_ASSOCCHANGED,
                    SHCNF_IDLIST,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        /// <summary>
        /// Whether Windows has been told to open this extension with
        /// something else.
        ///
        /// A UserChoice is the user's own decision, made in the Open
        /// With dialog, and programs are not allowed to write it. So
        /// the association can be registered correctly and still not
        /// be what a double click uses, and the only honest thing to
        /// do is say so.
        /// </summary>
        public static bool IsOverriddenByWindows(
            out string owner)
        {
            owner = null;

            try
            {
                using (RegistryKey key =
                    Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion" +
                        @"\Explorer\FileExts\" + Extension +
                        @"\UserChoice"))
                {
                    if (key == null)
                        return false;

                    string progId =
                        key.GetValue("ProgId") as string;

                    if (string.IsNullOrEmpty(progId) || progId == ProgId)
                        return false;

                    owner = progId;

                    return true;
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return false;
            }
        }

        //--------------------------------------------------------------
        // Command line
        //--------------------------------------------------------------

        /// <summary>
        /// The theme files named on the command line.
        ///
        /// Anything that is not an existing file is ignored: the same
        /// command line also carries the restart handshake, and one day
        /// will carry something else again.
        /// </summary>
        public static List<string> FilesFromCommandLine()
        {
            List<string> paths =
                new List<string>();

            try
            {
                string[] args =
                    Environment.GetCommandLineArgs();

                // The first entry is the executable itself.
                for (int i = 1; i < args.Length; i++)
                {
                    string arg = args[i];

                    if (string.IsNullOrWhiteSpace(arg) ||
                        arg.StartsWith("-") ||
                        arg.StartsWith("/"))
                    {
                        continue;
                    }

                    if (LooksLikeTheme(arg) && File.Exists(arg))
                        paths.Add(Path.GetFullPath(arg));
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            return paths;
        }

        private static bool LooksLikeTheme(
            string path)
        {
            try
            {
                string ext =
                    Path.GetExtension(path);

                return string.Equals(
                        ext, Extension, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        ext, ".json", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        //--------------------------------------------------------------
        // The inbox
        //--------------------------------------------------------------

        /// <summary>
        /// Leaves the paths where the running instance will find them
        /// and wakes it up. Returns false when there is nothing
        /// listening, so the caller can fall back to starting normally.
        /// </summary>
        public static bool HandOff(
            IEnumerable<string> paths)
        {
            List<string> list =
                paths == null
                    ? new List<string>()
                    : paths.ToList();

            if (list.Count == 0)
                return false;

            try
            {
                Directory.CreateDirectory(Inbox);

                File.WriteAllLines(
                    Path.Combine(
                        Inbox,
                        Guid.NewGuid().ToString("N") + ".txt"),
                    list.ToArray());
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return false;
            }

            try
            {
                using (EventWaitHandleHolder holder =
                    EventWaitHandleHolder.Open(OpenEventName))
                {
                    if (holder == null)
                        return false;

                    holder.Handle.Set();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return false;
            }
        }

        /// <summary>
        /// Takes everything waiting in the inbox, emptying it as it
        /// goes so a file is never imported twice.
        /// </summary>
        public static List<string> TakeInbox()
        {
            List<string> paths =
                new List<string>();

            try
            {
                if (!Directory.Exists(Inbox))
                    return paths;

                foreach (string drop in Directory.GetFiles(Inbox, "*.txt"))
                {
                    try
                    {
                        foreach (string line in File.ReadAllLines(drop))
                        {
                            if (!string.IsNullOrWhiteSpace(line) &&
                                File.Exists(line))
                            {
                                paths.Add(line);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.LogCrash(ex);
                    }

                    try
                    {
                        File.Delete(drop);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            return paths;
        }

        /// <summary>
        /// Anything left from a crash is not worth importing at the
        /// next start, so the inbox starts empty.
        /// </summary>
        public static void ClearInbox()
        {
            try
            {
                if (!Directory.Exists(Inbox))
                    return;

                foreach (string drop in Directory.GetFiles(Inbox, "*.txt"))
                {
                    try
                    {
                        File.Delete(drop);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }
    }

    /// <summary>
    /// Opening a named event that may not exist throws rather than
    /// returning null, which is awkward at every call site.
    /// </summary>
    internal sealed class EventWaitHandleHolder : IDisposable
    {
        public System.Threading.EventWaitHandle Handle { get; private set; }

        private EventWaitHandleHolder(
            System.Threading.EventWaitHandle handle)
        {
            Handle = handle;
        }

        public static EventWaitHandleHolder Open(
            string name)
        {
            try
            {
                return new EventWaitHandleHolder(
                    System.Threading.EventWaitHandle.OpenExisting(name));
            }
            catch (System.Threading.WaitHandleCannotBeOpenedException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Dispose()
        {
            try
            {
                if (Handle != null)
                    Handle.Dispose();
            }
            catch (Exception)
            {
            }
        }
    }
}
