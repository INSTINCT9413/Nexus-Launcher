using Nexus_Launcher.Models;
using Nexus_Launcher.Services.Artwork;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Nexus_Launcher.Services
{
    internal class XboxScannerService
    {
        private const string LaunchPrefix = @"shell:appsFolder\";

        private static readonly string[] ExcludedPackagePrefixes =
        {
            "Microsoft.GamingApp_",
            "Microsoft.GamingServices_",
            "Microsoft.XboxApp_",
            "Microsoft.XboxGameOverlay_",
            "Microsoft.XboxGamingOverlay_",
            "Microsoft.XboxIdentityProvider_",
            "Microsoft.XboxSpeechToTextOverlay_",
            "Microsoft.Xbox.TCUI_"
        };

        private class PackageInfo
        {
            public string FullName { get; set; }
            public string InstallPath { get; set; }
            public string FamilyName { get; set; }
        }

        private class ApplicationInfo
        {
            public string Id { get; set; }
            public string Executable { get; set; }
            public string Logo { get; set; }
        }

        public List<GameInfo> ScanGames()
        {
            List<GameInfo> games = new List<GameInfo>();

            try
            {
                List<PackageInfo> packages = GetInstalledPackages();

                HashSet<string> seen =
                    new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase);

                foreach (PackageInfo package in packages)
                {
                    try
                    {
                        if (package == null ||
                            string.IsNullOrWhiteSpace(package.InstallPath) ||
                            !Directory.Exists(package.InstallPath))
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(package.FamilyName))
                        {
                            package.FamilyName =
                                GetFamilyName(package.FullName);
                        }

                        if (string.IsNullOrWhiteSpace(package.FamilyName))
                            continue;

                        if (IsExcluded(package.FullName) ||
                            IsExcluded(package.FamilyName))
                        {
                            continue;
                        }

                        string microsoftGameConfig =
                            Path.Combine(
                                package.InstallPath,
                                "MicrosoftGame.config");

                        string xboxServicesConfig =
                            Path.Combine(
                                package.InstallPath,
                                "xboxservices.config");

                        bool hasMicrosoftGameConfig =
                            File.Exists(microsoftGameConfig);

                        bool hasXboxServicesConfig =
                            File.Exists(xboxServicesConfig);

                        if (!hasMicrosoftGameConfig &&
                            !hasXboxServicesConfig)
                        {
                            continue;
                        }

                        XDocument manifest =
                            LoadManifest(package.InstallPath);

                        if (manifest == null)
                            continue;

                        List<ApplicationInfo> applications =
                            GetApplications(manifest);

                        if (applications.Count == 0)
                            continue;

                        string configExecutable = null;
                        string configLogo = null;

                        if (hasMicrosoftGameConfig)
                        {
                            ReadMicrosoftGameConfig(
                                microsoftGameConfig,
                                out configExecutable,
                                out configLogo);
                        }

                        ApplicationInfo selectedApplication =
                            SelectApplication(
                                applications,
                                configExecutable);

                        if (selectedApplication == null)
                            continue;

                        string appId =
                            selectedApplication.Id;

                        if (string.IsNullOrWhiteSpace(appId))
                            continue;

                        string familyName =
                            package.FamilyName;

                        string aumid =
                            familyName + "!" + appId;

                        if (!seen.Add(aumid))
                            continue;

                        string name =
                            GetApplicationName(
                                manifest,
                                selectedApplication,
                                package);

                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        string executable =
                            configExecutable;

                        if (string.IsNullOrWhiteSpace(executable))
                        {
                            executable =
                                selectedApplication.Executable;
                        }

                        string logo =
                            configLogo;

                        if (string.IsNullOrWhiteSpace(logo))
                        {
                            logo =
                                selectedApplication.Logo;
                        }

                        GameInfo game =
                            new GameInfo();

                        game.Name =
                            name;

                        game.AppUserModelId =
                            aumid;

                        game.ProductId =
                            familyName;

                        game.Launcher =
                            "Xbox";

                        game.IsInstalled =
                            true;

                        game.InstallPath =
                            package.InstallPath;

                        game.LaunchUri =
                            LaunchPrefix + aumid;

                        game.ExecutablePath =
                            ResolveExecutablePath(
                                package.InstallPath,
                                executable);

                        if (string.IsNullOrWhiteSpace(
                            game.ExecutablePath))
                        {
                            game.ExecutablePath =
                                package.InstallPath;
                        }

                        game.IconPath =
                            ResolveLogoPath(
                                package.InstallPath,
                                logo);

                        games.Add(game);
                    }
                    catch (Exception ex)
                    {
                        Program.LogCrash(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            games =
                games
                .GroupBy(
                    x => x.AppUserModelId,
                    StringComparer.OrdinalIgnoreCase)
                .Select(
                    x => x.First())
                .OrderBy(
                    x => x.Name)
                .ToList();

            foreach (GameInfo game in games)
            {
                ArtworkService.RegisterGame(game);
            }

            return games;
        }

        public static bool LaunchGame(
            string appUserModelId)
        {
            if (string.IsNullOrWhiteSpace(
                appUserModelId))
            {
                return false;
            }

            try
            {
                ProcessStartInfo startInfo =
                    new ProcessStartInfo();

                startInfo.FileName =
                    "explorer.exe";

                startInfo.Arguments =
                    LaunchPrefix + appUserModelId;

                startInfo.UseShellExecute =
                    true;

                Process.Start(startInfo);

                return true;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return false;
            }
        }

        private static List<PackageInfo> GetInstalledPackages()
        {
            Dictionary<string, PackageInfo> packages =
                new Dictionary<string, PackageInfo>(
                    StringComparer.OrdinalIgnoreCase);

            ReadPackageRegistry(
                RegistryHive.LocalMachine,
                RegistryView.Registry64,
                packages);

            ReadPackageRegistry(
                RegistryHive.LocalMachine,
                RegistryView.Registry32,
                packages);

            ReadPackageRegistry(
                RegistryHive.CurrentUser,
                RegistryView.Registry64,
                packages);

            ReadPackageRegistry(
                RegistryHive.CurrentUser,
                RegistryView.Registry32,
                packages);

            AddWindowsAppsFallback(packages);

            return packages.Values.ToList();
        }

        private static void ReadPackageRegistry(
            RegistryHive hive,
            RegistryView view,
            Dictionary<string, PackageInfo> packages)
        {
            try
            {
                using (RegistryKey baseKey =
                    RegistryKey.OpenBaseKey(
                        hive,
                        view))
                {
                    ReadRepositoryPackages(
                        baseKey,
                        packages);

                    ReadApplicationPackages(
                        baseKey,
                        packages);

                    ReadUserRepositoryPackages(
                        baseKey,
                        packages);
                }
            }
            catch
            {
            }
        }

        private static void ReadRepositoryPackages(
            RegistryKey baseKey,
            Dictionary<string, PackageInfo> packages)
        {
            const string keyPath =
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\PackageRepository\Packages";

            try
            {
                using (RegistryKey root =
                    baseKey.OpenSubKey(keyPath))
                {
                    if (root == null)
                        return;

                    foreach (string packageName in root.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey packageKey =
                                root.OpenSubKey(packageName))
                            {
                                if (packageKey == null)
                                    continue;

                                string installPath =
                                    GetRegistryString(
                                        packageKey,
                                        "PackageRootFolder");

                                if (string.IsNullOrWhiteSpace(
                                    installPath))
                                {
                                    installPath =
                                        GetRegistryString(
                                            packageKey,
                                            "InstallLocation");
                                }

                                if (string.IsNullOrWhiteSpace(
                                    installPath))
                                {
                                    installPath =
                                        GetRegistryString(
                                            packageKey,
                                            "Path");
                                }

                                if (string.IsNullOrWhiteSpace(
                                    installPath))
                                {
                                    installPath =
                                        GetRegistryString(
                                            packageKey,
                                            "PackagePath");
                                }

                                if (string.IsNullOrWhiteSpace(
                                    installPath))
                                    continue;

                                installPath =
                                    NormalizeInstallPath(
                                        installPath);

                                if (!Directory.Exists(
                                    installPath))
                                {
                                    continue;
                                }

                                AddPackage(
                                    packages,
                                    packageName,
                                    installPath);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private static void ReadApplicationPackages(
            RegistryKey baseKey,
            Dictionary<string, PackageInfo> packages)
        {
            const string keyPath =
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications";

            try
            {
                using (RegistryKey root =
                    baseKey.OpenSubKey(keyPath))
                {
                    if (root == null)
                        return;

                    foreach (string applicationName in
                        root.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey applicationKey =
                                root.OpenSubKey(applicationName))
                            {
                                if (applicationKey == null)
                                    continue;

                                string packageFullName =
                                    GetRegistryString(
                                        applicationKey,
                                        "PackageFullName");

                                string installPath =
                                    GetRegistryString(
                                        applicationKey,
                                        "Path");

                                if (string.IsNullOrWhiteSpace(
                                    installPath))
                                {
                                    installPath =
                                        GetRegistryString(
                                            applicationKey,
                                            "PackageRootFolder");
                                }

                                if (string.IsNullOrWhiteSpace(
                                    installPath))
                                {
                                    installPath =
                                        GetRegistryString(
                                            applicationKey,
                                            "InstallLocation");
                                }

                                if (string.IsNullOrWhiteSpace(
                                    packageFullName))
                                {
                                    packageFullName =
                                        ExtractPackageName(
                                            applicationName);
                                }

                                if (string.IsNullOrWhiteSpace(
                                    installPath))
                                    continue;

                                installPath =
                                    NormalizeInstallPath(
                                        installPath);

                                if (!Directory.Exists(
                                    installPath))
                                {
                                    continue;
                                }

                                AddPackage(
                                    packages,
                                    packageFullName,
                                    installPath);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private static void ReadUserRepositoryPackages(
            RegistryKey baseKey,
            Dictionary<string, PackageInfo> packages)
        {
            const string keyPath =
                @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages";

            try
            {
                using (RegistryKey root =
                    baseKey.OpenSubKey(keyPath))
                {
                    if (root == null)
                        return;

                    foreach (string packageName in root.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey packageKey =
                                root.OpenSubKey(packageName))
                            {
                                if (packageKey == null)
                                    continue;

                                string installPath =
                                    GetRegistryString(
                                        packageKey,
                                        "PackageRootFolder");

                                if (string.IsNullOrWhiteSpace(
                                    installPath))
                                {
                                    installPath =
                                        GetRegistryString(
                                            packageKey,
                                            "InstallLocation");
                                }

                                if (string.IsNullOrWhiteSpace(
                                    installPath))
                                {
                                    installPath =
                                        GetRegistryString(
                                            packageKey,
                                            "Path");
                                }

                                if (string.IsNullOrWhiteSpace(
                                    installPath))
                                    continue;

                                installPath =
                                    NormalizeInstallPath(
                                        installPath);

                                if (!Directory.Exists(
                                    installPath))
                                {
                                    continue;
                                }

                                AddPackage(
                                    packages,
                                    packageName,
                                    installPath);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private static void AddPackage(
            Dictionary<string, PackageInfo> packages,
            string fullName,
            string installPath)
        {
            if (string.IsNullOrWhiteSpace(
                installPath))
            {
                return;
            }

            string familyName =
                GetFamilyName(fullName);

            if (string.IsNullOrWhiteSpace(
                familyName))
            {
                return;
            }

            string key =
                familyName + "|" +
                installPath;

            if (packages.ContainsKey(key))
                return;

            packages.Add(
                key,
                new PackageInfo
                {
                    FullName = fullName,
                    InstallPath = installPath,
                    FamilyName = familyName
                });
        }

        private static void AddWindowsAppsFallback(
            Dictionary<string, PackageInfo> packages)
        {
            try
            {
                foreach (DriveInfo drive in
                    DriveInfo.GetDrives())
                {
                    try
                    {
                        if (drive.DriveType !=
                            DriveType.Fixed)
                        {
                            continue;
                        }

                        string windowsApps =
                            Path.Combine(
                                drive.RootDirectory.FullName,
                                "WindowsApps");

                        if (!Directory.Exists(
                            windowsApps))
                        {
                            continue;
                        }

                        IEnumerable<string> directories;

                        try
                        {
                            directories =
                                Directory.EnumerateDirectories(
                                    windowsApps);
                        }
                        catch
                        {
                            continue;
                        }

                        foreach (string directory in
                            directories)
                        {
                            try
                            {
                                string fullName =
                                    Path.GetFileName(
                                        directory);

                                if (string.IsNullOrWhiteSpace(
                                    fullName))
                                {
                                    continue;
                                }

                                AddPackage(
                                    packages,
                                    fullName,
                                    directory);
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        private static string GetRegistryString(
            RegistryKey key,
            string valueName)
        {
            try
            {
                object value =
                    key.GetValue(
                        valueName,
                        null,
                        RegistryValueOptions.DoNotExpandEnvironmentNames);

                if (value == null)
                    return null;

                return value.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeInstallPath(
            string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            path =
                Environment.ExpandEnvironmentVariables(
                    path.Trim());

            if (path.StartsWith(
                "file:///",
                StringComparison.OrdinalIgnoreCase))
            {
                path =
                    path.Substring(8)
                        .Replace('/', '\\');
            }

            return path.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
        }

        private static string ExtractPackageName(
            string applicationName)
        {
            if (string.IsNullOrWhiteSpace(
                applicationName))
            {
                return null;
            }

            int bang =
                applicationName.IndexOf('!');

            if (bang > 0)
            {
                return applicationName.Substring(
                    0,
                    bang);
            }

            return applicationName;
        }

        private static string GetFamilyName(
            string packageFullName)
        {
            if (string.IsNullOrWhiteSpace(
                packageFullName))
            {
                return null;
            }

            string value =
                packageFullName.Trim();

            string[] parts =
                value.Split('_');

            if (parts.Length < 5)
                return null;

            string publisherId =
                parts[parts.Length - 1];

            string architecture =
                parts[parts.Length - 3];

            string version =
                parts[parts.Length - 4];

            if (string.IsNullOrWhiteSpace(
                publisherId) ||
                string.IsNullOrWhiteSpace(
                    architecture) ||
                string.IsNullOrWhiteSpace(
                    version))
            {
                return null;
            }

            string name =
                string.Join(
                    "_",
                    parts,
                    0,
                    parts.Length - 4);

            if (string.IsNullOrWhiteSpace(name))
                return null;

            return name + "_" + publisherId;
        }

        private static XDocument LoadManifest(
            string installPath)
        {
            try
            {
                string manifestPath =
                    Path.Combine(
                        installPath,
                        "AppxManifest.xml");

                if (!File.Exists(
                    manifestPath))
                {
                    manifestPath =
                        Path.Combine(
                            installPath,
                            "AppxManifest.XML");
                }

                if (!File.Exists(
                    manifestPath))
                    return null;

                return XDocument.Load(
                    manifestPath,
                    LoadOptions.None);
            }
            catch
            {
                return null;
            }
        }

        private static List<ApplicationInfo> GetApplications(
            XDocument manifest)
        {
            List<ApplicationInfo> result =
                new List<ApplicationInfo>();

            if (manifest == null ||
                manifest.Root == null)
            {
                return result;
            }

            XNamespace foundation =
                "http://schemas.microsoft.com/appx/manifest/foundation/windows10";

            IEnumerable<XElement> applications =
                manifest.Root
                    .Descendants(foundation + "Application");

            foreach (XElement application in
                applications)
            {
                try
                {
                    string id =
                        GetAttribute(
                            application,
                            "Id");

                    string executable =
                        GetAttribute(
                            application,
                            "Executable");

                    string logo =
                        GetAttribute(
                            application,
                            "Logo");

                    XNamespace visualElementsNamespace =
                        "http://schemas.microsoft.com/appx/manifest/uap/windows10";

                    XElement visualElements =
                        application.Element(
                            visualElementsNamespace +
                            "VisualElements");

                    if (visualElements != null)
                    {
                        if (string.IsNullOrWhiteSpace(
                            logo))
                        {
                            logo =
                                GetAttribute(
                                    visualElements,
                                    "Square44x44Logo");
                        }

                        if (string.IsNullOrWhiteSpace(
                            logo))
                        {
                            logo =
                                GetAttribute(
                                    visualElements,
                                    "Square150x150Logo");
                        }
                    }

                    if (string.IsNullOrWhiteSpace(
                        id))
                    {
                        continue;
                    }

                    result.Add(
                        new ApplicationInfo
                        {
                            Id = id,
                            Executable = executable,
                            Logo = logo
                        });
                }
                catch
                {
                }
            }

            return result;
        }

        private static string GetAttribute(
            XElement element,
            string name)
        {
            if (element == null)
                return null;

            XAttribute attribute =
                element.Attribute(name);

            if (attribute == null)
                return null;

            return attribute.Value;
        }

        private static void ReadMicrosoftGameConfig(
            string configPath,
            out string executable,
            out string logo)
        {
            executable = null;
            logo = null;

            try
            {
                XDocument document =
                    XDocument.Load(
                        configPath,
                        LoadOptions.None);

                XElement game =
                    document.Root;

                if (game == null)
                    return;

                XElement executableElement =
                    game
                        .Descendants()
                        .FirstOrDefault(
                            x =>
                                x.Name.LocalName
                                    .Equals(
                                        "Executable",
                                        StringComparison.OrdinalIgnoreCase));

                if (executableElement != null)
                {
                    XAttribute name =
                        executableElement.Attribute(
                            "Name");

                    if (name != null)
                    {
                        executable =
                            name.Value;
                    }
                    else
                    {
                        executable =
                            executableElement.Value;
                    }
                }

                XElement shellVisuals =
                    game
                        .Descendants()
                        .FirstOrDefault(
                            x =>
                                x.Name.LocalName
                                    .Equals(
                                        "ShellVisuals",
                                        StringComparison.OrdinalIgnoreCase));

                if (shellVisuals != null)
                {
                    logo =
                        GetChildValue(
                            shellVisuals,
                            "Square44x44Logo");

                    if (string.IsNullOrWhiteSpace(
                        logo))
                    {
                        logo =
                            GetChildValue(
                                shellVisuals,
                                "Square150x150Logo");
                    }

                    if (string.IsNullOrWhiteSpace(
                        logo))
                    {
                        logo =
                            GetChildValue(
                                shellVisuals,
                                "StoreLogo");
                    }
                }
            }
            catch
            {
            }
        }

        private static string GetChildValue(
            XElement parent,
            string name)
        {
            XElement element =
                parent
                    .Elements()
                    .FirstOrDefault(
                        x =>
                            x.Name.LocalName
                                .Equals(
                                    name,
                                    StringComparison.OrdinalIgnoreCase));

            if (element == null)
                return null;

            return element.Value;
        }

        private static ApplicationInfo SelectApplication(
            List<ApplicationInfo> applications,
            string configuredExecutable)
        {
            if (applications == null ||
                applications.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(
                configuredExecutable))
            {
                string configuredFile =
                    Path.GetFileName(
                        configuredExecutable);

                ApplicationInfo match =
                    applications.FirstOrDefault(
                        x =>
                            !string.IsNullOrWhiteSpace(
                                x.Executable) &&
                            string.Equals(
                                Path.GetFileName(
                                    x.Executable),
                                configuredFile,
                                StringComparison.OrdinalIgnoreCase));

                if (match != null)
                    return match;
            }

            return applications[0];
        }

        private static string GetApplicationName(
            XDocument manifest,
            ApplicationInfo application,
            PackageInfo package)
        {
            try
            {
                if (manifest != null &&
                    manifest.Root != null)
                {
                    XNamespace foundation =
                        "http://schemas.microsoft.com/appx/manifest/foundation/windows10";

                    XElement properties =
                        manifest.Root
                            .Element(
                                foundation +
                                "Properties");

                    if (properties != null)
                    {
                        string displayName =
                            GetChildValue(
                                properties,
                                "DisplayName");

                        if (!string.IsNullOrWhiteSpace(
                            displayName) &&
                            !displayName.StartsWith(
                                "ms-resource:",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            return displayName;
                        }
                    }

                    XElement applicationElement =
                        manifest.Root
                            .Descendants(
                                foundation +
                                "Application")
                            .FirstOrDefault(
                                x =>
                                    string.Equals(
                                        GetAttribute(
                                            x,
                                            "Id"),
                                        application.Id,
                                        StringComparison.OrdinalIgnoreCase));

                    if (applicationElement != null)
                    {
                        XNamespace uap =
                            "http://schemas.microsoft.com/appx/manifest/uap/windows10";

                        XElement visualElements =
                            applicationElement.Element(
                                uap +
                                "VisualElements");

                        if (visualElements != null)
                        {
                            string displayName =
                                GetAttribute(
                                    visualElements,
                                    "DisplayName");

                            if (!string.IsNullOrWhiteSpace(
                                displayName) &&
                                !displayName.StartsWith(
                                    "ms-resource:",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                return displayName;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            if (package != null &&
                !string.IsNullOrWhiteSpace(
                    package.FullName))
            {
                string[] parts =
                    package.FullName.Split('_');

                if (parts.Length >= 5)
                {
                    return string.Join(
                        "_",
                        parts,
                        0,
                        parts.Length - 4);
                }
            }

            return application != null
                ? application.Id
                : null;
        }

        private static string ResolveExecutablePath(
            string installPath,
            string executable)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(
                    installPath) ||
                    string.IsNullOrWhiteSpace(
                        executable))
                {
                    return null;
                }

                executable =
                    executable
                        .Replace('/', '\\')
                        .TrimStart('\\');

                string direct =
                    Path.Combine(
                        installPath,
                        executable);

                if (File.Exists(direct))
                    return direct;

                string fileName =
                    Path.GetFileName(
                        executable);

                if (string.IsNullOrWhiteSpace(
                    fileName))
                    return null;

                string[] matches =
                    Directory.GetFiles(
                        installPath,
                        fileName,
                        SearchOption.AllDirectories);

                return matches.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveLogoPath(
            string installPath,
            string relativeLogo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(
                    installPath) ||
                    string.IsNullOrWhiteSpace(
                        relativeLogo))
                {
                    return null;
                }

                relativeLogo =
                    relativeLogo
                        .Replace('/', '\\')
                        .TrimStart('\\');

                string full =
                    Path.Combine(
                        installPath,
                        relativeLogo);

                if (File.Exists(full))
                    return full;

                string folder =
                    Path.GetDirectoryName(full);

                if (string.IsNullOrWhiteSpace(
                    folder) ||
                    !Directory.Exists(folder))
                {
                    return null;
                }

                string stem =
                    Path.GetFileNameWithoutExtension(
                        full);

                string extension =
                    Path.GetExtension(full);

                string[] matches =
                    Directory.GetFiles(
                        folder,
                        stem + "*" + extension);

                return matches
                    .OrderBy(
                        x =>
                            x.IndexOf(
                                "scale-100",
                                StringComparison.OrdinalIgnoreCase) >= 0
                                ? 0
                                :
                            x.IndexOf(
                                "scale-200",
                                StringComparison.OrdinalIgnoreCase) >= 0
                                ? 1
                                : 2)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsExcluded(
            string packageName)
        {
            if (string.IsNullOrWhiteSpace(
                packageName))
            {
                return false;
            }

            return ExcludedPackagePrefixes.Any(
                prefix =>
                    packageName.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase));
        }
    }
}