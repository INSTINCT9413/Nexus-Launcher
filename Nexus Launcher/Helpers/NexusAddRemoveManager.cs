using Newtonsoft.Json;
using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;

public static class NexusAddRemoveManager
{
    private static readonly string SaveFile =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "NexusLauncher",
            "CustomApps.json");

    public static List<GameInfo> Load()
    {
        if (!File.Exists(SaveFile))
            return new List<GameInfo>();

        string json =
            File.ReadAllText(SaveFile);

        return JsonConvert.DeserializeObject<List<GameInfo>>(json)
            ?? new List<GameInfo>();
    }

    public static void Save(
        List<GameInfo> games)
    {
        string folder =
            Path.GetDirectoryName(
                SaveFile);

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(
                folder);
        }

        string json =
            JsonConvert.SerializeObject(
                games,
                Formatting.Indented);

        File.WriteAllText(
            SaveFile,
            json);
    }
}