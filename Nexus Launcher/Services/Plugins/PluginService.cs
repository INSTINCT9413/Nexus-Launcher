using Nexus.Plugin;
using Nexus_Launcher.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Nexus_Launcher.Services.Plugins
{
    /// <summary>
    /// One plugin, as the launcher sees it: where it came from, what
    /// it says about itself, and whether it is behaving.
    /// </summary>
    internal class PluginRecord
    {
        public string Id;

        public string Name;

        public string Author;

        public string Version;

        public string Description;

        public string File;

        /// <summary>Bundled with Nexus rather than user installed.</summary>
        public bool IsBuiltIn;

        public bool Enabled;

        /// <summary>
        /// Set when the plugin failed to load or threw on startup.
        /// The plugin is left in the list either way, so the user can
        /// see what happened instead of it silently vanishing.
        /// </summary>
        public string Error;

        public INexusPlugin Instance;

        public bool Loaded
        {
            get { return Instance != null && Error == null; }
        }

        public int LibrarySources;

        public int GameActions;

        public int Widgets;

        /// <summary>A short "2 sources, 1 action" for the list.</summary>
        public string Contributions
        {
            get
            {
                List<string> parts = new List<string>();

                if (LibrarySources > 0)
                    parts.Add(LibrarySources +
                        (LibrarySources == 1 ? " library source" : " library sources"));

                if (GameActions > 0)
                    parts.Add(GameActions +
                        (GameActions == 1 ? " game action" : " game actions"));

                if (Widgets > 0)
                    parts.Add(Widgets +
                        (Widgets == 1 ? " panel tile" : " panel tiles"));

                // A plugin that only listens for launches and exits
                // registers nothing, and is working perfectly.
                return parts.Count == 0
                    ? "Runs in the background"
                    : string.Join(", ", parts.ToArray());
            }
        }
    }

    /// <summary>
    /// Finds, loads and holds the plugins.
    ///
    /// Plugins run in this process, so the guarantee here is not that
    /// a bad one cannot misbehave: it is that it cannot take Nexus
    /// down with it. Every call into a plugin goes through Guard,
    /// which catches, records the failure against the plugin and
    /// leaves the launcher running. A plugin that throws on startup is
    /// shown in the list with its error rather than disappearing.
    ///
    /// There is no unloading. .NET Framework cannot unload an assembly
    /// without a separate AppDomain, and marshalling the API across
    /// one would make plugin authoring far harder for a gain the user
    /// would never see. Disabling a plugin stops it being used and
    /// takes effect fully on the next start, which the list says.
    /// </summary>
    internal static class PluginService
    {
        private static readonly List<PluginRecord> plugins =
            new List<PluginRecord>();

        private static readonly List<RegisteredSource> sources =
            new List<RegisteredSource>();

        private static readonly List<RegisteredAction> actions =
            new List<RegisteredAction>();

        private static readonly List<RegisteredWidget> widgets =
            new List<RegisteredWidget>();

        private static bool started;

        internal class RegisteredSource
        {
            public PluginRecord Owner;
            public ILibrarySource Source;
        }

        internal class RegisteredAction
        {
            public PluginRecord Owner;
            public IGameAction Action;
        }

        internal class RegisteredWidget
        {
            public PluginRecord Owner;
            public ISidePanelWidget Widget;
        }

        public static IEnumerable<PluginRecord> All
        {
            get { return plugins; }
        }

        public static IEnumerable<RegisteredSource> Sources
        {
            get { return sources; }
        }

        public static IEnumerable<RegisteredAction> Actions
        {
            get { return actions; }
        }

        public static IEnumerable<RegisteredWidget> Widgets
        {
            get { return widgets; }
        }

        //--------------------------------------------------------------
        // Where plugins live
        //--------------------------------------------------------------

        /// <summary>
        /// Shipped with Nexus. Replaced wholesale by an update, so
        /// nothing the user owns should ever be put here.
        /// </summary>
        public static string BuiltInFolder
        {
            get
            {
                return Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Plugins");
            }
        }

        /// <summary>
        /// Where the user's own plugins go. Survives updates, which is
        /// why this is the folder the Plugins page opens.
        /// </summary>
        public static string UserFolder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "NexusLauncher",
                    "Plugins");
            }
        }

        //--------------------------------------------------------------
        // Enabling
        //--------------------------------------------------------------

        private static HashSet<string> DisabledIds()
        {
            HashSet<string> set =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string raw = Settings.Default.DisabledPlugins;

            if (string.IsNullOrWhiteSpace(raw))
                return set;

            foreach (string id in raw.Split(
                         new[] { ';' },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                set.Add(id.Trim());
            }

            return set;
        }

        public static void SetEnabled(
            PluginRecord plugin,
            bool enabled)
        {
            if (plugin == null)
                return;

            plugin.Enabled = enabled;

            HashSet<string> disabled = DisabledIds();

            if (enabled)
                disabled.Remove(plugin.Id);
            else
                disabled.Add(plugin.Id);

            Settings.Default.DisabledPlugins =
                string.Join(";", disabled.ToArray());

            Settings.Default.Save();
        }

        //--------------------------------------------------------------
        // Which sources show in the sidebar
        //--------------------------------------------------------------

        private static HashSet<string> HiddenSourceIds()
        {
            HashSet<string> set =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string raw = Settings.Default.HiddenPluginSources;

            if (string.IsNullOrWhiteSpace(raw))
                return set;

            foreach (string id in raw.Split(
                         new[] { ';' },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                set.Add(id.Trim());
            }

            return set;
        }

        /// <summary>
        /// Whether a plugin's library source gets a sidebar heading.
        ///
        /// Shown unless the user has said otherwise, so a newly
        /// installed plugin works without a trip to Settings first.
        /// </summary>
        public static bool IsSourceVisible(
            string sourceId)
        {
            return !HiddenSourceIds().Contains(sourceId ?? string.Empty);
        }

        public static void SetSourceVisible(
            string sourceId,
            bool visible)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
                return;

            HashSet<string> hidden = HiddenSourceIds();

            if (visible)
                hidden.Remove(sourceId);
            else
                hidden.Add(sourceId);

            Settings.Default.HiddenPluginSources =
                string.Join(";", hidden.ToArray());

            Settings.Default.Save();
        }

        //--------------------------------------------------------------
        // Starting up
        //--------------------------------------------------------------

        /// <summary>
        /// Discovers and starts every enabled plugin. Safe to call
        /// twice; the second call does nothing.
        /// </summary>
        public static void Start()
        {
            if (started)
                return;

            started = true;

            try
            {
                Directory.CreateDirectory(UserFolder);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            HashSet<string> disabled = DisabledIds();

            foreach (string file in Discover())
            {
                LoadFile(file, disabled);
            }
        }

        private static IEnumerable<string> Discover()
        {
            List<string> found = new List<string>();

            foreach (string root in new[] { BuiltInFolder, UserFolder })
            {
                try
                {
                    if (!Directory.Exists(root))
                        continue;

                    // A plugin is a dll in the folder, or in a folder
                    // of its own beside its dependencies. Both work,
                    // so a one file plugin needs no ceremony.
                    found.AddRange(Directory.GetFiles(root, "*.dll"));

                    foreach (string dir in Directory.GetDirectories(root))
                        found.AddRange(Directory.GetFiles(dir, "*.dll"));
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                }
            }

            return found;
        }

        private static void LoadFile(
            string file,
            HashSet<string> disabled)
        {
            Assembly assembly;

            try
            {
                // The SDK itself will be sitting next to plugins that
                // were copied wholesale. Loading a second copy would
                // give types that look identical but are not, so it is
                // skipped: the one beside Nexus is the real one.
                if (string.Equals(
                        Path.GetFileName(file),
                        "Nexus.Plugin.SDK.dll",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                assembly = Assembly.LoadFrom(file);
            }
            catch (BadImageFormatException)
            {
                // Not a managed assembly. Perfectly normal for a
                // native dependency sitting beside a plugin.
                return;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return;
            }

            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Built against something that is not here. Report the
                // plugin rather than losing it silently.
                types = ex.Types.Where(t => t != null).ToArray();

                Record(file, null, null,
                    "It needs something that is missing: " +
                    Describe(ex));
            }
            catch (Exception ex)
            {
                Record(file, null, null, ex.Message);

                return;
            }

            foreach (Type type in types)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                if (!typeof(INexusPlugin).IsAssignableFrom(type))
                    continue;

                NexusPluginAttribute info =
                    (NexusPluginAttribute)Attribute.GetCustomAttribute(
                        type, typeof(NexusPluginAttribute));

                if (info == null)
                {
                    Record(file, null, type,
                        "It has no [NexusPlugin] attribute, so Nexus " +
                        "does not know what to call it.");

                    continue;
                }

                StartPlugin(file, type, info, disabled);
            }
        }

        private static string Describe(
            ReflectionTypeLoadException ex)
        {
            StringBuilder text = new StringBuilder();

            foreach (Exception inner in ex.LoaderExceptions.Take(2))
                text.Append(inner.Message).Append(" ");

            return text.ToString().Trim();
        }

        private static void Record(
            string file,
            NexusPluginAttribute info,
            Type type,
            string error)
        {
            PluginRecord record = new PluginRecord
            {
                Id = info != null
                    ? info.Id
                    : (type != null ? type.FullName : Path.GetFileName(file)),
                Name = info != null
                    ? info.Name
                    : Path.GetFileNameWithoutExtension(file),
                Author = info != null ? info.Author : null,
                Version = info != null ? info.Version : null,
                Description = info != null ? info.Description : null,
                File = file,
                IsBuiltIn = file.StartsWith(
                    BuiltInFolder, StringComparison.OrdinalIgnoreCase),
                Enabled = true,
                Error = error
            };

            plugins.Add(record);
        }

        private static void StartPlugin(
            string file,
            Type type,
            NexusPluginAttribute info,
            HashSet<string> disabled)
        {
            if (plugins.Any(p => string.Equals(
                    p.Id, info.Id, StringComparison.OrdinalIgnoreCase)))
            {
                // Two copies of the same plugin. The first one found
                // wins, which means a built in one cannot be quietly
                // shadowed by a dropped in file.
                return;
            }

            PluginRecord record = new PluginRecord
            {
                Id = info.Id,
                Name = info.Name,
                Author = info.Author,
                Version = info.Version,
                Description = info.Description,
                File = file,
                IsBuiltIn = file.StartsWith(
                    BuiltInFolder, StringComparison.OrdinalIgnoreCase),
                Enabled = !disabled.Contains(info.Id)
            };

            plugins.Add(record);

            if (!record.Enabled)
                return;

            PluginHostContext host =
                new PluginHostContext(record);

            Guard(record, "start", () =>
            {
                record.Instance =
                    (INexusPlugin)Activator.CreateInstance(type);

                record.Instance.Initialize(host);
            });
        }

        /// <summary>
        /// Runs a piece of plugin code and refuses to let it bring the
        /// launcher down. The failure is recorded against the plugin
        /// so the Plugins page can show it.
        /// </summary>
        public static bool Guard(
            PluginRecord plugin,
            string what,
            Action work)
        {
            try
            {
                work();

                return true;
            }
            catch (Exception ex)
            {
                if (plugin != null)
                {
                    plugin.Error =
                        "Failed to " + what + ": " + ex.Message;
                }

                Program.LogCrash(ex);

                return false;
            }
        }

        //--------------------------------------------------------------
        // Registration, called by the host context
        //--------------------------------------------------------------

        internal static void Register(
            PluginRecord owner,
            ILibrarySource source)
        {
            if (source == null)
                return;

            sources.Add(new RegisteredSource
            {
                Owner = owner,
                Source = source
            });

            owner.LibrarySources++;
        }

        internal static void Register(
            PluginRecord owner,
            IGameAction action)
        {
            if (action == null)
                return;

            actions.Add(new RegisteredAction
            {
                Owner = owner,
                Action = action
            });

            owner.GameActions++;
        }

        internal static void Register(
            PluginRecord owner,
            ISidePanelWidget widget)
        {
            if (widget == null)
                return;

            widgets.Add(new RegisteredWidget
            {
                Owner = owner,
                Widget = widget
            });

            owner.Widgets++;
        }

        //--------------------------------------------------------------
        // Shutting down
        //--------------------------------------------------------------

        public static void Stop()
        {
            foreach (PluginRecord plugin in plugins)
            {
                if (plugin.Instance == null)
                    continue;

                PluginRecord captured = plugin;

                Guard(captured, "shut down",
                    () => captured.Instance.Shutdown());
            }
        }

        //--------------------------------------------------------------
        // Events plugins can listen to
        //--------------------------------------------------------------

        public static void RaiseGameLaunched(
            GameRef game)
        {
            foreach (PluginRecord plugin in plugins.ToList())
            {
                PluginHostContext host = PluginHostContext.For(plugin);

                if (host == null)
                    continue;

                PluginRecord captured = plugin;

                Guard(captured, "handle a game launch",
                    () => host.RaiseLaunched(game));
            }
        }

        public static void RaiseGameExited(
            GameRef game)
        {
            foreach (PluginRecord plugin in plugins.ToList())
            {
                PluginHostContext host = PluginHostContext.For(plugin);

                if (host == null)
                    continue;

                PluginRecord captured = plugin;

                Guard(captured, "handle a game exit",
                    () => host.RaiseExited(game));
            }
        }
    }
}
