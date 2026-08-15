using Microsoft.Win32;
using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Nexus_Launcher.Services
{
    internal static class UninstallerService
    {
        //====================================================
        // Public Entry Point
        //====================================================

        public static UninstallResult Uninstall(
            GameInfo game)
        {
            if (game == null)
            {
                return Fail(
                    "No game was selected.");
            }

            //------------------------------------------------
            // 1. Launcher URI
            //------------------------------------------------

            string uri =
                GetUninstallUri(game);

            if (!string.IsNullOrWhiteSpace(uri))
            {
                if (TryLaunchUri(uri))
                {
                    return Success(
                        "Launcher URI",
                        uri);
                }
            }

            //------------------------------------------------
            // 2. Windows Uninstall Registry
            //------------------------------------------------

            UninstallEntry registryEntry =
                FindRegistryUninstaller(game);

            if (registryEntry != null)
            {
                if (TryLaunchUninstallEntry(
                    registryEntry))
                {
                    return Success(
                        "Windows Uninstall Registry",
                        registryEntry.UninstallString);
                }
            }

            //------------------------------------------------
            // 3. Search Installation Directory
            //------------------------------------------------

            UninstallFile uninstallFile =
                FindLocalUninstaller(game);

            if (uninstallFile != null)
            {
                if (TryLaunchLocalUninstaller(
                    uninstallFile))
                {
                    return Success(
                        "Installation Directory",
                        uninstallFile.Path);
                }
            }

            //------------------------------------------------
            // 4. Windows Installer
            //------------------------------------------------

            string windowsInstallerMessage;

            if (TryWindowsInstallerUninstall(
                game,
                out windowsInstallerMessage))
            {
                return new UninstallResult
                {
                    Success = true,
                    Method = "Windows Installer",
                    Message = windowsInstallerMessage
                };
            }
            //------------------------------------------------
            // 4. Nothing Found
            //------------------------------------------------

            return Fail(
                GetFallbackInstructions(game));
        }

        //====================================================
        // Launcher URI
        //====================================================

        private static string GetUninstallUri(
            GameInfo game)
        {
            if (string.IsNullOrWhiteSpace(
                game.Launcher))
            {
                return null;
            }

            switch (
                game.Launcher.Trim()
                    .ToLowerInvariant())
            {
                case "steam":

                    if (!string.IsNullOrWhiteSpace(
                        game.ProductId))
                    {
                        return
                            "steam://uninstall/" +
                            game.ProductId;
                    }

                    break;
            }

            return null;
        }

        private static bool TryLaunchUri(
            string uri)
        {
            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = uri,
                        UseShellExecute = true
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }

        //====================================================
        // Windows Registry
        //====================================================

        private static UninstallEntry
            FindRegistryUninstaller(
                GameInfo game)
        {
            List<UninstallEntry> entries =
                new List<UninstallEntry>();

            //------------------------------------------------
            // 64-bit / 32-bit machine locations
            //------------------------------------------------

            SearchRegistryRoot(
                RegistryHive.LocalMachine,
                RegistryView.Registry64,
                game,
                entries);

            SearchRegistryRoot(
                RegistryHive.LocalMachine,
                RegistryView.Registry32,
                game,
                entries);

            //------------------------------------------------
            // Current user
            //------------------------------------------------

            SearchRegistryRoot(
                RegistryHive.CurrentUser,
                RegistryView.Default,
                game,
                entries);

            if (entries.Count == 0)
                return null;

            //------------------------------------------------
            // Best match
            //------------------------------------------------

            return entries
                .OrderByDescending(
                    x => x.MatchScore)
                .FirstOrDefault();
        }

        private static void SearchRegistryRoot(
            RegistryHive hive,
            RegistryView view,
            GameInfo game,
            List<UninstallEntry> results)
        {
            try
            {
                using (RegistryKey baseKey =
                    RegistryKey.OpenBaseKey(
                        hive,
                        view))
                {
                    using (RegistryKey root =
                        baseKey.OpenSubKey(
                            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                    {
                        if (root == null)
                            return;

                        foreach (
                            string subKeyName
                            in root.GetSubKeyNames())
                        {
                            try
                            {
                                using (RegistryKey key =
                                    root.OpenSubKey(
                                        subKeyName))
                                {
                                    if (key == null)
                                        continue;

                                    string displayName =
                                        GetString(
                                            key,
                                            "DisplayName");

                                    string installLocation =
                                        GetString(
                                            key,
                                            "InstallLocation");

                                    string uninstallString =
                                        GetString(
                                            key,
                                            "UninstallString");

                                    string quietUninstallString =
                                        GetString(
                                            key,
                                            "QuietUninstallString");

                                    if (string.IsNullOrWhiteSpace(
                                        displayName) &&
                                        string.IsNullOrWhiteSpace(
                                        installLocation))
                                    {
                                        continue;
                                    }

                                    if (string.IsNullOrWhiteSpace(
                                        uninstallString) &&
                                        string.IsNullOrWhiteSpace(
                                        quietUninstallString))
                                    {
                                        continue;
                                    }

                                    int score =
                                        CalculateMatchScore(
                                            game,
                                            displayName,
                                            installLocation,
                                            subKeyName);

                                    if (score <= 0)
                                        continue;

                                    results.Add(
                                        new UninstallEntry
                                        {
                                            DisplayName =
                                                displayName,

                                            InstallLocation =
                                                installLocation,

                                            UninstallString =
                                                uninstallString,

                                            QuietUninstallString =
                                                quietUninstallString,

                                            RegistryKeyName =
                                                subKeyName,

                                            MatchScore =
                                                score
                                        });
                                }
                            }
                            catch
                            {
                                // Ignore malformed entries.
                            }
                        }
                    }
                }
            }
            catch
            {
                // Registry access can fail because of permissions.
            }
        }

        //====================================================
        // Registry Matching
        //====================================================

        private static int CalculateMatchScore(
            GameInfo game,
            string displayName,
            string installLocation,
            string registryKeyName)
        {
            int score = 0;

            string gameName =
                NormalizeGameName(
                    game.Name);

            string registryName =
                NormalizeGameName(
                    displayName);

            //------------------------------------------------
            // Installation path
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                game.InstallPath) &&
                !string.IsNullOrWhiteSpace(
                installLocation))
            {
                string gamePath =
                    NormalizePath(
                        game.InstallPath);

                string registryPath =
                    NormalizePath(
                        installLocation);

                if (string.Equals(
                    gamePath,
                    registryPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    score += 100;
                }
                else if (
                    registryPath.StartsWith(
                        gamePath + "\\",
                        StringComparison.OrdinalIgnoreCase) ||
                    gamePath.StartsWith(
                        registryPath + "\\",
                        StringComparison.OrdinalIgnoreCase))
                {
                    score += 70;
                }
            }

            //------------------------------------------------
            // Name
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                gameName) &&
                !string.IsNullOrWhiteSpace(
                registryName))
            {
                if (string.Equals(
                    gameName,
                    registryName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    score += 60;
                }
                else if (
    gameName.IndexOf(
        registryName,
        StringComparison.OrdinalIgnoreCase) >= 0 ||
    registryName.IndexOf(
        gameName,
        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 35;
                }
            }

            //------------------------------------------------
            // Registry key
            //------------------------------------------------

            string normalizedKey =
                NormalizeGameName(
                    registryKeyName);

            if (!string.IsNullOrWhiteSpace(
                gameName) &&
                string.Equals(
                    gameName,
                    normalizedKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                score += 40;
            }

            return score;
        }

        //====================================================
        // Local Uninstaller Search
        //====================================================

        private static UninstallFile
            FindLocalUninstaller(
                GameInfo game)
        {
            if (string.IsNullOrWhiteSpace(
                game.InstallPath))
            {
                return null;
            }

            if (!Directory.Exists(
                game.InstallPath))
            {
                return null;
            }

            try
            {
                List<string> files =
                    Directory.GetFiles(
                        game.InstallPath,
                        "*",
                        SearchOption.AllDirectories)
                    .Where(
                        IsPotentialUninstaller)
                    .ToList();

                if (files.Count == 0)
                    return null;

                //------------------------------------------------
                // Prefer files closest to game root
                //------------------------------------------------

                return files
                    .Select(
                        x => new UninstallFile
                        {
                            Path = x,
                            Depth =
                                GetDirectoryDepth(
                                    game.InstallPath,
                                    x)
                        })
                    .OrderBy(
                        x => x.Depth)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsPotentialUninstaller(
            string path)
        {
            string fileName =
                Path.GetFileNameWithoutExtension(
                    path);

            if (string.IsNullOrWhiteSpace(
                fileName))
            {
                return false;
            }

            string name =
                fileName.ToLowerInvariant();

            //------------------------------------------------
            // Known names
            //------------------------------------------------

            if (name == "uninstall" ||
                name == "uninstaller" ||
                name == "unins000" ||
                name == "unins001" ||
                name == "remove")
            {
                return true;
            }

            //------------------------------------------------
            // Name contains uninstall keywords
            //------------------------------------------------

            if (name.Contains("uninstall") ||
                name.Contains("unins"))
            {
                return true;
            }

            //------------------------------------------------
            // Remove
            //------------------------------------------------

            if (name == "remove" ||
                name.StartsWith("remove"))
            {
                return true;
            }

            return false;
        }

        private static int GetDirectoryDepth(
            string root,
            string file)
        {
            try
            {
                string directory =
                    Path.GetDirectoryName(
                        file);

                string relative =
                    directory.Substring(
                        root.Length)
                    .Trim(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);

                if (string.IsNullOrWhiteSpace(
                    relative))
                {
                    return 0;
                }

                return relative
                    .Split(
                        new[]
                        {
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar
                        },
                        StringSplitOptions.RemoveEmptyEntries)
                    .Length;
            }
            catch
            {
                return 999;
            }
        }

        private static bool
            TryLaunchLocalUninstaller(
                UninstallFile uninstallFile)
        {
            try
            {
                string extension =
                    Path.GetExtension(
                        uninstallFile.Path);

                //------------------------------------------------
                // EXE
                //------------------------------------------------

                if (string.Equals(
                    extension,
                    ".exe",
                    StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(
                        new ProcessStartInfo
                        {
                            FileName =
                                uninstallFile.Path,

                            WorkingDirectory =
                                Path.GetDirectoryName(
                                    uninstallFile.Path),

                            UseShellExecute = true
                        });

                    return true;
                }

                //------------------------------------------------
                // Shortcut
                //------------------------------------------------

                if (string.Equals(
                    extension,
                    ".lnk",
                    StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(
                        new ProcessStartInfo
                        {
                            FileName =
                                uninstallFile.Path,

                            UseShellExecute = true
                        });

                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        //====================================================
        // Registry Uninstaller Execution
        //====================================================

        private static bool
            TryLaunchUninstallEntry(
                UninstallEntry entry)
        {
            try
            {
                string command =
                    !string.IsNullOrWhiteSpace(
                        entry.QuietUninstallString)
                    ? entry.QuietUninstallString
                    : entry.UninstallString;

                if (string.IsNullOrWhiteSpace(
                    command))
                {
                    return false;
                }

                //------------------------------------------------
                // MSI
                //------------------------------------------------

                string guid =
                    ExtractMsiGuid(command);

                if (!string.IsNullOrWhiteSpace(
                    guid))
                {
                    Process.Start(
                        new ProcessStartInfo
                        {
                            FileName =
                                "msiexec.exe",

                            Arguments =
                                "/X " + guid,

                            UseShellExecute = true
                        });

                    return true;
                }

                //------------------------------------------------
                // Normal executable command
                //------------------------------------------------

                ParseCommand(
                    command,
                    out string fileName,
                    out string arguments);

                if (string.IsNullOrWhiteSpace(
                    fileName))
                {
                    return false;
                }

                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                            fileName,

                        Arguments =
                            arguments,

                        UseShellExecute = true
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }

        //====================================================
        // Command Parsing
        //====================================================

        private static void ParseCommand(
            string command,
            out string fileName,
            out string arguments)
        {
            fileName = null;
            arguments = string.Empty;

            command =
                command.Trim();

            if (command.StartsWith("\""))
            {
                int endQuote =
                    command.IndexOf(
                        '"',
                        1);

                if (endQuote > 0)
                {
                    fileName =
                        command.Substring(
                            1,
                            endQuote - 1);

                    if (command.Length >
                        endQuote + 1)
                    {
                        arguments =
                            command.Substring(
                                endQuote + 1)
                            .Trim();
                    }

                    return;
                }
            }

            int space =
                command.IndexOf(' ');

            if (space > 0)
            {
                fileName =
                    command.Substring(
                        0,
                        space);

                arguments =
                    command.Substring(
                        space + 1)
                    .Trim();
            }
            else
            {
                fileName =
                    command;
            }
        }

        private static string ExtractMsiGuid(
            string command)
        {
            Match match =
                Regex.Match(
                    command,
                    @"\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}");

            if (!match.Success)
                return null;

            if (command.IndexOf(
                "msiexec",
                StringComparison.OrdinalIgnoreCase) < 0)
            {
                return null;
            }

            return match.Value;
        }

        //====================================================
        // Helpers
        //====================================================

        private static string GetString(
            RegistryKey key,
            string name)
        {
            object value =
                key.GetValue(name);

            return value?.ToString();
        }

        private static string NormalizePath(
            string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            try
            {
                return Path.GetFullPath(
                    path.Trim()
                ).TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return path
                    .Trim()
                    .TrimEnd(
                        '\\',
                        '/');
            }
        }

        private static string NormalizeGameName(
            string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            name =
                name
                    .Replace("™", "")
                    .Replace("®", "")
                    .Replace("©", "")
                    .Replace("(TM)", "")
                    .Replace("(tm)", "")
                    .Replace("(R)", "")
                    .Replace("(r)", "")
                    .Replace("(C)", "")
                    .Replace("(c)", "");

            return new string(
                name
                    .Where(
                        c => char.IsLetterOrDigit(c))
                    .ToArray())
                .ToLowerInvariant();
        }

        //====================================================
        // Fallback Instructions
        //====================================================

        private static string
            GetFallbackInstructions(
                GameInfo game)
        {
            string launcher =
                game.Launcher ?? "";

            switch (
                launcher.Trim()
                    .ToLowerInvariant())
            {
                case "steam":

                    return
                        "Nexus Launcher could not find " +
                        "an automatic uninstall method.\n\n" +
                        "To uninstall this Steam game:\n\n" +
                        "1. Open Steam.\n" +
                        "2. Open your Library.\n" +
                        "3. Right-click the game.\n" +
                        "4. Select Manage.\n" +
                        "5. Select Uninstall.";

                case "epic":

                    return
                        "Nexus Launcher could not find " +
                        "an automatic uninstall method.\n\n" +
                        "To uninstall this Epic Games title:\n\n" +
                        "1. Open Epic Games Launcher.\n" +
                        "2. Open your Library.\n" +
                        "3. Click the three dots next " +
                        "to the game.\n" +
                        "4. Select Uninstall.";

                case "ea":

                    return
                        "Nexus Launcher could not find " +
                        "an automatic uninstall method.\n\n" +
                        "To uninstall this EA game:\n\n" +
                        "1. Open the EA app.\n" +
                        "2. Open your Library.\n" +
                        "3. Select the game.\n" +
                        "4. Open Manage.\n" +
                        "5. Select Uninstall.";

                case "gog":

                    return
                        "Nexus Launcher could not find " +
                        "an automatic uninstall method.\n\n" +
                        "Open GOG Galaxy and uninstall " +
                        "the game from your installed games.";

                default:

                    return
                        "Nexus Launcher could not find " +
                        "an automatic uninstall method " +
                        "for this game.\n\n" +
                        "Please uninstall the game using " +
                        "its original launcher or through " +
                        "Windows Settings > Apps.";
            }
        }

        private static UninstallResult Success(
            string method,
            string details)
        {
            return new UninstallResult
            {
                Success = true,
                Method = method,
                Message = details
            };
        }

        private static UninstallResult Fail(
            string message)
        {
            return new UninstallResult
            {
                Success = false,
                Method = "None",
                Message = message
            };
        }

        //====================================================
        // Result Classes
        //====================================================

        internal class UninstallResult
        {
            public bool Success { get; set; }

            public string Method { get; set; }

            public string Message { get; set; }
        }

        private class UninstallEntry
        {
            public string DisplayName { get; set; }

            public string InstallLocation { get; set; }

            public string UninstallString { get; set; }

            public string QuietUninstallString { get; set; }

            public string RegistryKeyName { get; set; }

            public int MatchScore { get; set; }
        }

        private class UninstallFile
        {
            public string Path { get; set; }

            public int Depth { get; set; }
        }
        private static List<InstalledProgram> GetInstalledPrograms()
        {
            List<InstalledProgram> programs =
                new List<InstalledProgram>();

            string[] registryPaths =
            {
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    };

            //------------------------------------------------
            // Machine-wide installations
            //------------------------------------------------

            foreach (string registryPath in registryPaths)
            {
                ReadUninstallRegistry(
                    Registry.LocalMachine,
                    registryPath,
                    programs);
            }

            //------------------------------------------------
            // Current-user installations
            //------------------------------------------------

            ReadUninstallRegistry(
                Registry.CurrentUser,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                programs);

            return programs;
        }
        private static void ReadUninstallRegistry(
    RegistryKey root,
    string registryPath,
    List<InstalledProgram> programs)
        {
            try
            {
                using (RegistryKey uninstallRoot =
                    root.OpenSubKey(registryPath))
                {
                    if (uninstallRoot == null)
                        return;

                    foreach (string subKeyName
                        in uninstallRoot.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey key =
                                uninstallRoot.OpenSubKey(subKeyName))
                            {
                                if (key == null)
                                    continue;

                                string displayName =
                                    key.GetValue("DisplayName")
                                    as string;

                                if (string.IsNullOrWhiteSpace(
                                    displayName))
                                {
                                    continue;
                                }

                                string installLocation =
                                    key.GetValue("InstallLocation")
                                    as string;

                                string uninstallString =
                                    key.GetValue("UninstallString")
                                    as string;

                                string quietUninstallString =
                                    key.GetValue("QuietUninstallString")
                                    as string;

                                //------------------------------------------------
                                // Some MSI entries don't have an obvious
                                // ProductCode property, but the registry
                                // subkey itself is the GUID.
                                //------------------------------------------------

                                string productCode = null;

                                if (IsProductCode(subKeyName))
                                {
                                    productCode =
                                        subKeyName;
                                }

                                //------------------------------------------------
                                // Also check Windows Installer ProductCode
                                //------------------------------------------------

                                string registryProductCode =
                                    key.GetValue("ProductCode")
                                    as string;

                                if (IsProductCode(
                                    registryProductCode))
                                {
                                    productCode =
                                        registryProductCode;
                                }

                                programs.Add(
                                    new InstalledProgram
                                    {
                                        DisplayName =
                                            displayName,

                                        InstallLocation =
                                            installLocation,

                                        UninstallString =
                                            uninstallString,

                                        QuietUninstallString =
                                            quietUninstallString,

                                        ProductCode =
                                            productCode
                                    });
                            }
                        }
                        catch
                        {
                            // Ignore individual broken entries.
                        }
                    }
                }
            }
            catch
            {
            }
        }
        private static bool IsProductCode(
    string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            Guid guid;

            return Guid.TryParse(
                value.Trim(),
                out guid);
        }
        private static InstalledProgram FindInstalledProgram(
    GameInfo game)
        {
            if (game == null)
                return null;

            List<InstalledProgram> programs =
                GetInstalledPrograms();

            //------------------------------------------------
            // First: exact normalized name
            //------------------------------------------------

            string gameName =
                NormalizeName(game.Name);

            InstalledProgram exactMatch =
                programs.FirstOrDefault(
                    x =>
                        string.Equals(
                            NormalizeName(x.DisplayName),
                            gameName,
                            StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
                return exactMatch;

            //------------------------------------------------
            // Second: install path match
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                game.InstallPath))
            {
                string gamePath =
                    NormalizePath(game.InstallPath);

                InstalledProgram pathMatch =
                    programs.FirstOrDefault(
                        x =>
                            !string.IsNullOrWhiteSpace(
                                x.InstallLocation) &&
                            string.Equals(
                                NormalizePath(
                                    x.InstallLocation),
                                gamePath,
                                StringComparison.OrdinalIgnoreCase));

                if (pathMatch != null)
                    return pathMatch;
            }

            //------------------------------------------------
            // Third: name contained in DisplayName
            //------------------------------------------------

            InstalledProgram partialMatch =
                programs.FirstOrDefault(
                    x =>
                        NormalizeName(
                            x.DisplayName)
                        .IndexOf(
                            gameName,
                            StringComparison.OrdinalIgnoreCase)
                        >= 0);

            if (partialMatch != null)
                return partialMatch;

            return null;
        }
        private static string NormalizeName(
    string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            string result =
                name.Trim();

            result =
                result.Replace(
                    "™",
                    "");

            result =
                result.Replace(
                    "®",
                    "");

            result =
                result.Replace(
                    "©",
                    "");

            result =
                result.Replace(
                    "(TM)",
                    "");

            result =
                result.Replace(
                    "(tm)",
                    "");

            result =
                result.Replace(
                    "(R)",
                    "");

            result =
                result.Replace(
                    "(r)",
                    "");

            result =
                result.Replace(
                    "(C)",
                    "");

            result =
                result.Replace(
                    "(c)",
                    "");

            //------------------------------------------------
            // Normalize spaces
            //------------------------------------------------

            while (result.Contains("  "))
            {
                result =
                    result.Replace(
                        "  ",
                        " ");
            }

            return result.Trim();
        }
        
        private static bool TryWindowsInstallerUninstall(
    GameInfo game,
    out string message)
        {
            message = null;

            InstalledProgram program =
                FindInstalledProgram(game);

            if (program == null)
            {
                message =
                    "Nexus Launcher could not find this game in " +
                    "the Windows installed-program registry.";

                return false;
            }

            //------------------------------------------------
            // ProductCode is the safest MSI method
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                program.ProductCode))
            {
                try
                {
                    ProcessStartInfo startInfo =
                        new ProcessStartInfo();

                    startInfo.FileName =
                        "msiexec.exe";

                    startInfo.Arguments =
                        "/x " +
                        program.ProductCode;

                    startInfo.UseShellExecute =
                        true;

                    Process.Start(startInfo);

                    message =
                        "Windows Installer uninstall was started.";

                    return true;
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);

                    message =
                        "Windows Installer could not be started.";

                    return false;
                }
            }

            //------------------------------------------------
            // Check uninstall string
            //------------------------------------------------

            string uninstallString =
                program.UninstallString;

            if (string.IsNullOrWhiteSpace(
                uninstallString))
            {
                message =
                    "Windows found the installed program, " +
                    "but no uninstall command was registered.";

                return false;
            }

            //------------------------------------------------
            // MSI uninstall string
            //------------------------------------------------

            if (uninstallString.IndexOf(
                "msiexec",
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                try
                {
                    string arguments =
                        ExtractMsiArguments(
                            uninstallString);

                    ProcessStartInfo startInfo =
                        new ProcessStartInfo();

                    startInfo.FileName =
                        "msiexec.exe";

                    startInfo.Arguments =
                        arguments;

                    startInfo.UseShellExecute =
                        true;

                    Process.Start(startInfo);

                    message =
                        "Windows Installer uninstall was started.";

                    return true;
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);

                    message =
                        "Windows Installer could not be started.";

                    return false;
                }
            }

            //------------------------------------------------
            // Not MSI
            //------------------------------------------------

            message =
                "The installed program was found, but its " +
                "uninstaller is not a Windows Installer package.";

            return false;
        }
        private static string ExtractMsiArguments(
    string uninstallString)
        {
            if (string.IsNullOrWhiteSpace(
                uninstallString))
            {
                return string.Empty;
            }

            string result =
                uninstallString.Trim();

            //------------------------------------------------
            // Remove executable portion
            //------------------------------------------------

            int exeIndex =
                result.IndexOf(
                    "msiexec",
                    StringComparison.OrdinalIgnoreCase);

            if (exeIndex >= 0)
            {
                int start =
                    result.IndexOf(
                        ' ',
                        exeIndex);

                if (start >= 0)
                {
                    result =
                        result.Substring(
                            start + 1)
                        .Trim();
                }
            }

            //------------------------------------------------
            // Convert installation mode to uninstall
            //------------------------------------------------

            result =
                result.Replace(
                    "/I",
                    "/X");

            result =
                result.Replace(
                    "/i",
                    "/x");

            //------------------------------------------------
            // Handle GUID directly
            //------------------------------------------------

            if (result.StartsWith(
                "{") &&
                result.EndsWith("}"))
            {
                result =
                    "/x " +
                    result;
            }

            return result;
        }
    }
}