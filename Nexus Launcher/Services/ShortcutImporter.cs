using DevExpress.Skins.XtraForm;
using IWshRuntimeLibrary;
using Nexus_Launcher;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using File = System.IO.File;

public static class ShortcutImporter
{
    public static int ImportFolder(
    string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return 0;

        List<GameInfo> apps =
            NexusAddRemoveManager.Load();

        int imported = 0;

        var files =
            Directory.EnumerateFiles(
                folderPath,
                "*.*",
                SearchOption.AllDirectories)
            .Where(f =>
            {
                string ext =
                    Path.GetExtension(f)
                        .ToLowerInvariant();

                return ext == ".lnk" ||
                       ext == ".url" ||
                       ext == ".exe";
            });

        foreach (string file in files)
        {
            try
            {
                string target = null;
                string name = null;

                string ext =
                    Path.GetExtension(file)
                        .ToLowerInvariant();

                switch (ext)
                {
                    case ".lnk":
                        {
                            WshShell shell =
                                new WshShell();

                            IWshShortcut shortcut =
                                (IWshShortcut)
                                shell.CreateShortcut(file);

                            target =
                                Environment
                                .ExpandEnvironmentVariables(
                                    shortcut.TargetPath);

                            name =
                                Path.GetFileNameWithoutExtension(
                                    file);

                            break;
                        }

                    case ".url":
                        {
                            target = file;

                            name =
                                Path.GetFileNameWithoutExtension(
                                    file);

                            break;
                        }

                    case ".exe":
                        {
                            target = file;

                            name =
                                Path.GetFileNameWithoutExtension(
                                    file);

                            break;
                        }
                }

                if (string.IsNullOrWhiteSpace(target))
                    continue;

                bool exists =
                    apps.Any(x =>
                        string.Equals(
                            x.ExecutablePath,
                            target,
                            StringComparison.OrdinalIgnoreCase));

                if (exists)
                    continue;

                GameInfo game =
                    new GameInfo
                    {
                        Name = name,
                        ExecutablePath = target,
                        InstallPath =
                            Path.GetDirectoryName(target),
                        IsInstalled = true
                    };

                apps.Add(game);

                imported++;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                MessageBox.Show(
                    $"Failed to import:\n{file}\n\n{ex.Message}",
                    "Import Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        NexusAddRemoveManager.Save(apps);

        return imported;
    }
}