using Nexus.Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Nexus.Plugin.LibraryExport
{
    //==================================================================
    // Writes the library and its playtime out to CSV and JSON.
    //
    // The smallest plugin that does something people actually want,
    // and the one to read if you are wondering how to get at the
    // library rather than add to it.
    //==================================================================

    [NexusPlugin(
        "nexus.libraryexport",
        "Library Export",
        Author = "Nexus Launcher",
        Version = "1.1.0",
        Description = "Exports your games and playtime to CSV or " +
                      "JSON. Use the buttons in its settings.")]
    public class LibraryExportPlugin : INexusPlugin, IPluginCommands
    {
        private INexusHost host;

        public void Initialize(
            INexusHost host)
        {
            this.host = host;

            // Exporting is something you do occasionally, so it lives
            // in this plugin's settings rather than on the menu of
            // every game in the library, where it was only ever in
            // the way.
            host.AddSidePanelWidget(new LibrarySummaryWidget(host));
        }

        public void Shutdown()
        {
        }

        public IEnumerable<PluginCommand> GetCommands()
        {
            return new[]
            {
                new PluginCommand
                {
                    Id = "csv",
                    Label = "Export to CSV",
                    Description =
                        "A spreadsheet of every game, its launcher " +
                        "and its playtime."
                },
                new PluginCommand
                {
                    Id = "json",
                    Label = "Export to JSON",
                    Description =
                        "The same thing, for feeding into something " +
                        "else."
                }
            };
        }

        public void RunCommand(
            string commandId)
        {
            Exporter.Run(
                host,
                commandId == "json" ? ExportFormat.Json : ExportFormat.Csv);
        }
    }

    internal enum ExportFormat
    {
        Csv,
        Json
    }

    /// <summary>
    /// Writes the library out.
    /// </summary>
    internal static class Exporter
    {
        public static void Run(
            INexusHost host,
            ExportFormat format)
        {
            List<GameRef> games =
                host.GetGames().OrderBy(g => g.Name).ToList();

            string file = Path.Combine(
                host.DataFolder,
                "library-" +
                DateTime.Now.ToString("yyyy-MM-dd-HHmm") +
                (format == ExportFormat.Csv ? ".csv" : ".json"));

            string text = format == ExportFormat.Csv
                ? BuildCsv(games)
                : BuildJson(games);

            File.WriteAllText(file, text, Encoding.UTF8);

            host.Log.Info("exported " + games.Count + " game(s) to " + file);

            // Show it rather than announce it: the user asked for a
            // file, so the file is what they get.
            Process.Start(new ProcessStartInfo("explorer.exe",
                "/select,\"" + file + "\"")
            {
                UseShellExecute = true
            });
        }

        //--------------------------------------------------------------
        // Formats
        //--------------------------------------------------------------

        /// <summary>
        /// Doubles any quote and wraps the field, which is the whole
        /// of CSV escaping and the bit everyone forgets.
        /// </summary>
        private static string Csv(
            string value)
        {
            if (value == null)
                return "\"\"";

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string BuildCsv(
            List<GameRef> games)
        {
            StringBuilder text = new StringBuilder();

            text.AppendLine("Name,Source,Hours,Last played,Install path");

            foreach (GameRef game in games)
            {
                text.AppendLine(string.Join(",", new[]
                {
                    Csv(game.Name),
                    Csv(game.SourceId),
                    game.Played.TotalHours.ToString("0.0",
                        CultureInfo.InvariantCulture),
                    Csv(game.LastPlayedUtc.HasValue
                        ? game.LastPlayedUtc.Value.ToLocalTime()
                            .ToString("yyyy-MM-dd HH:mm")
                        : string.Empty),
                    Csv(game.InstallPath)
                }));
            }

            return text.ToString();
        }

        /// <summary>
        /// Hand written rather than pulling in a JSON library: the
        /// shape is fixed and tiny, and a plugin that needs no
        /// dependencies is a plugin that cannot break on someone
        /// else's version of one.
        /// </summary>
        private static string Json(
            string value)
        {
            if (value == null)
                return "null";

            StringBuilder text = new StringBuilder("\"");

            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': text.Append("\\\""); break;
                    case '\\': text.Append("\\\\"); break;
                    case '\n': text.Append("\\n"); break;
                    case '\r': text.Append("\\r"); break;
                    case '\t': text.Append("\\t"); break;
                    default:
                        if (c < 32)
                            text.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            text.Append(c);
                        break;
                }
            }

            return text.Append("\"").ToString();
        }

        private static string BuildJson(
            List<GameRef> games)
        {
            StringBuilder text = new StringBuilder();

            text.AppendLine("{");
            text.AppendLine("  \"exported\": " +
                Json(DateTime.Now.ToString("s")) + ",");
            text.AppendLine("  \"count\": " + games.Count + ",");
            text.AppendLine("  \"games\": [");

            for (int i = 0; i < games.Count; i++)
            {
                GameRef game = games[i];

                text.AppendLine("    {");
                text.AppendLine("      \"name\": " + Json(game.Name) + ",");
                text.AppendLine("      \"source\": " + Json(game.SourceId) + ",");
                text.AppendLine("      \"hours\": " +
                    game.Played.TotalHours.ToString("0.0",
                        CultureInfo.InvariantCulture) + ",");
                text.AppendLine("      \"lastPlayed\": " +
                    (game.LastPlayedUtc.HasValue
                        ? Json(game.LastPlayedUtc.Value.ToString("s"))
                        : "null") + ",");
                text.AppendLine("      \"installPath\": " +
                    Json(game.InstallPath));
                text.AppendLine("    }" + (i < games.Count - 1 ? "," : string.Empty));
            }

            text.AppendLine("  ]");
            text.AppendLine("}");

            return text.ToString();
        }
    }

    /// <summary>
    /// A tile with the shape of the collection: how many games, how
    /// many hours, and the one played most.
    /// </summary>
    internal class LibrarySummaryWidget : ISidePanelWidget
    {
        private readonly INexusHost host;

        public LibrarySummaryWidget(
            INexusHost host)
        {
            this.host = host;
        }

        public string Id
        {
            get { return "libraryexport.summary"; }
        }

        public string Title
        {
            get { return "Library"; }
        }

        public TimeSpan RefreshInterval
        {
            get { return TimeSpan.FromMinutes(1); }
        }

        public IEnumerable<string> GetLines()
        {
            List<GameRef> games = host.GetGames().ToList();

            if (games.Count == 0)
                return new[] { "No games yet" };

            double hours = games.Sum(g => g.Played.TotalHours);

            GameRef most = games
                .OrderByDescending(g => g.Played.TotalHours)
                .First();

            List<string> lines = new List<string>
            {
                games.Count + " games",
                hours.ToString("0") + " hours played"
            };

            if (most.Played.TotalHours >= 1)
            {
                lines.Add("Most: " + most.Name + " (" +
                    most.Played.TotalHours.ToString("0") + "h)");
            }

            return lines;
        }
    }
}
