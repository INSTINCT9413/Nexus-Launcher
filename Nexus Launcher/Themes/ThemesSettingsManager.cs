using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows.Forms;
using static Nexus_Launcher.MainView;


public class ThemesSettings
{
    public string SkinName { get; set; }

    public string PaletteName { get; set; }
}

public static class ThemeSettingsManager
{
    private static readonly string SettingsFile =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "NexusLauncher",
            "ThemeSettings.json");

    public static void Save(
        string skinName,
        string paletteName)
    {
        if (!Directory.Exists(Path.GetDirectoryName(SettingsFile)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile));
        }
        ThemesSettings settings =
            new ThemesSettings();

        settings.SkinName =
            skinName;

        settings.PaletteName =
            paletteName;

        string json =
            JsonConvert.SerializeObject(
                settings,
                Formatting.Indented);

        File.WriteAllText(
            SettingsFile,
            json);
    }

    public static ThemesSettings Load()
    {
        if (!File.Exists(SettingsFile))
        {
            return new ThemesSettings();
        }

        string json =
            File.ReadAllText(
                SettingsFile);

        return JsonConvert.DeserializeObject<ThemesSettings>(
            json);
    }
}