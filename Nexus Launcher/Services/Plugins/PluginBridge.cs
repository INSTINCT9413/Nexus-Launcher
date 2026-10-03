using Nexus.Plugin;
using Nexus_Launcher.Models;
using Nexus_Launcher.Services.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nexus_Launcher.Services.Plugins
{
    /// <summary>
    /// Translates between Nexus's own types and the plugin API.
    ///
    /// Plugins never see GameInfo. It is a big mutable class that the
    /// launcher changes as it pleases, and handing it out would both
    /// freeze its shape and let a plugin corrupt the library. They get
    /// a GameRef copy instead, which is read only as far as they are
    /// concerned because nothing reads it back.
    /// </summary>
    internal static class PluginBridge
    {
        public static GameRef ToRef(
            GameInfo game)
        {
            if (game == null)
                return null;

            TimeSpan played = TimeSpan.Zero;
            DateTime? last = null;

            try
            {
                GamePlayStats stats =
                    PlayTrackingService.GetStats(game);

                if (stats != null)
                {
                    played = TimeSpan.FromSeconds(stats.TotalPlaySeconds);

                    last = stats.LastPlayedUtc;
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            return new GameRef
            {
                Name = game.Name,
                SourceId = game.Launcher,
                InstallPath = game.InstallPath,
                ExecutablePath = game.ExecutablePath,
                Played = played,
                LastPlayedUtc = last
            };
        }

        /// <summary>
        /// Turns what a plugin found into something the library can
        /// hold.
        /// </summary>
        public static GameInfo ToGame(
            PluginGame found,
            string sourceId)
        {
            if (found == null || string.IsNullOrWhiteSpace(found.Name))
                return null;

            return new GameInfo
            {
                Name = found.Name.Trim(),
                Launcher = sourceId,
                InstallPath = found.InstallPath,
                ExecutablePath = found.ExecutablePath,
                LaunchUri = found.LaunchUri,
                IconPath = found.IconPath,
                HeaderImageUrl = found.GridImageUrl,
                LogoUrl = found.HeroImageUrl
            };
        }

        //--------------------------------------------------------------
        // Play events
        //--------------------------------------------------------------

        private static bool hooked;

        /// <summary>
        /// Starts forwarding launches and exits to the plugins.
        ///
        /// Done here rather than each plugin subscribing to
        /// PlayTrackingService directly: plugins never see Nexus's own
        /// types, and this keeps the conversion in one place.
        /// </summary>
        public static void HookPlayEvents()
        {
            if (hooked)
                return;

            hooked = true;

            PlayTrackingService.GameLaunched += (game, when) =>
                PluginService.RaiseGameLaunched(ToRef(game));

            PlayTrackingService.GameExited += game =>
                PluginService.RaiseGameExited(ToRef(game));
        }

        //--------------------------------------------------------------
        // Game actions on the context menu
        //--------------------------------------------------------------

        /// <summary>
        /// One plugin action, resolved for a particular game and
        /// ready to hang on a menu.
        /// </summary>
        internal class ResolvedAction
        {
            public string Caption;
            public string PluginName;
            public Action Invoke;
        }

        /// <summary>
        /// The plugin actions that apply to this game.
        ///
        /// Worked out per right click rather than once at startup:
        /// AppliesTo is asked about a specific game, and the first
        /// version of this built the menu during load, when nothing
        /// was selected yet, so every action was asked about null and
        /// none of them ever appeared.
        /// </summary>
        public static List<ResolvedAction> GetGameActions(
            GameInfo game)
        {
            List<ResolvedAction> resolved =
                new List<ResolvedAction>();

            if (game == null)
                return resolved;

            GameRef reference = ToRef(game);

            foreach (PluginService.RegisteredAction entry in
                     PluginService.Actions.ToList())
            {
                if (!entry.Owner.Enabled || entry.Owner.Error != null)
                    continue;

                PluginService.RegisteredAction captured = entry;

                bool applies = false;

                PluginService.Guard(captured.Owner,
                    "decide whether " + captured.Action.Id + " applies",
                    () => applies = captured.Action.AppliesTo(reference));

                if (!applies)
                    continue;

                string caption = null;

                PluginService.Guard(captured.Owner,
                    "read its menu caption",
                    () => caption = captured.Action.Caption);

                if (string.IsNullOrWhiteSpace(caption))
                    continue;

                resolved.Add(new ResolvedAction
                {
                    Caption = caption,
                    PluginName = captured.Owner.Name,
                    Invoke = () => PluginService.Guard(
                        captured.Owner,
                        "run " + captured.Action.Id,
                        () => captured.Action.Invoke(reference))
                });
            }

            return resolved;
        }

        //--------------------------------------------------------------
        // Side panel tiles
        //--------------------------------------------------------------

        private class WidgetCache
        {
            public string Text = string.Empty;
            public DateTime NextDue = DateTime.MinValue;
            public bool Running;
        }

        private static readonly Dictionary<string, WidgetCache> widgetCache =
            new Dictionary<string, WidgetCache>();

        /// <summary>
        /// The text for the plugin tiles, ready to append to the side
        /// panel.
        ///
        /// Returns what it has and refreshes in the background. The
        /// API promises plugins that GetLines is called off the user
        /// interface thread, and the side panel ticks on a timer on
        /// it, so asking a plugin directly from here would both break
        /// that promise and let a slow plugin stall the window.
        /// </summary>
        public static string BuildWidgetText()
        {
            StringBuilder text = new StringBuilder();

            foreach (PluginService.RegisteredWidget entry in
                     PluginService.Widgets.ToList())
            {
                if (!entry.Owner.Enabled || entry.Owner.Error != null)
                    continue;

                PluginService.RegisteredWidget captured = entry;

                string key = captured.Owner.Id + "/" + SafeId(captured.Widget);

                WidgetCache cache;

                if (!widgetCache.TryGetValue(key, out cache))
                {
                    cache = new WidgetCache();

                    widgetCache[key] = cache;
                }

                if (!cache.Running && DateTime.UtcNow >= cache.NextDue)
                {
                    cache.Running = true;

                    WidgetCache target = cache;

                    System.Threading.Tasks.Task.Run(() =>
                        Refresh(captured, target));
                }

                if (!string.IsNullOrEmpty(cache.Text))
                {
                    if (text.Length > 0)
                        text.AppendLine();

                    text.Append(cache.Text);
                }
            }

            return text.ToString();
        }

        private static string SafeId(
            Nexus.Plugin.ISidePanelWidget widget)
        {
            try
            {
                return widget.Id ?? "widget";
            }
            catch
            {
                return "widget";
            }
        }

        private static void Refresh(
            PluginService.RegisteredWidget entry,
            WidgetCache cache)
        {
            try
            {
                string title = null;
                IEnumerable<string> lines = null;
                TimeSpan interval = TimeSpan.FromSeconds(30);

                PluginService.Guard(entry.Owner,
                    "read its side panel tile",
                    () =>
                    {
                        title = entry.Widget.Title;
                        lines = entry.Widget.GetLines();
                        interval = entry.Widget.RefreshInterval;
                    });

                StringBuilder text = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(title))
                    text.AppendLine(title);

                if (lines != null)
                {
                    foreach (string line in lines.Take(6))
                        text.AppendLine("  " + line);
                }

                cache.Text = text.ToString().TrimEnd();

                // The floor stops a plugin asking to be polled every
                // few milliseconds, as the API warns it will.
                if (interval < TimeSpan.FromSeconds(5))
                    interval = TimeSpan.FromSeconds(5);

                cache.NextDue = DateTime.UtcNow + interval;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                cache.Text = string.Empty;

                cache.NextDue = DateTime.UtcNow + TimeSpan.FromMinutes(5);
            }
            finally
            {
                cache.Running = false;
            }
        }

        //--------------------------------------------------------------
        // Library sources
        //--------------------------------------------------------------

        /// <summary>
        /// Asks every enabled plugin source for its games.
        ///
        /// Called on the scanning thread. A source that throws, hangs
        /// on nothing, or returns rubbish costs only its own games:
        /// the rest of the library is unaffected.
        /// </summary>
        public static List<GameInfo> ScanPluginSources()
        {
            List<GameInfo> games = new List<GameInfo>();

            foreach (PluginService.RegisteredSource entry in
                     PluginService.Sources.ToList())
            {
                if (!entry.Owner.Enabled || entry.Owner.Error != null)
                    continue;

                PluginService.RegisteredSource captured = entry;

                bool present = false;

                PluginService.Guard(captured.Owner,
                    "check whether " + captured.Source.Id + " is installed",
                    () => present = captured.Source.IsInstalled());

                if (!present)
                    continue;

                IEnumerable<PluginGame> found = null;

                PluginService.Guard(captured.Owner,
                    "scan " + captured.Source.Id,
                    () => found = captured.Source.Scan());

                if (found == null)
                    continue;

                PluginService.Guard(captured.Owner,
                    "read the games from " + captured.Source.Id,
                    () =>
                    {
                        foreach (PluginGame item in found)
                        {
                            GameInfo game =
                                ToGame(item, captured.Source.Id);

                            if (game != null)
                                games.Add(game);
                        }
                    });
            }

            return games;
        }
    }
}
