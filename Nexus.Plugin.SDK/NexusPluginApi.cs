using System;
using System.Collections.Generic;

namespace Nexus.Plugin
{
    //==================================================================
    // The whole plugin API.
    //
    // Deliberately one small file of plain types. A plugin references
    // this one assembly and nothing else: no WinForms, no DevExpress,
    // no Nexus internals. That is what lets the launcher be rebuilt,
    // restyled or re-themed without breaking everybody's plugins, and
    // it means a plugin author needs no knowledge of how Nexus draws
    // anything.
    //==================================================================

    /// <summary>
    /// Marks a class as a Nexus plugin and describes it.
    ///
    /// An attribute rather than a separate manifest file: there is
    /// nothing to keep in step, and the description cannot drift away
    /// from the code that implements it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class NexusPluginAttribute : Attribute
    {
        /// <param name="id">
        /// A permanent, unique id, lower case, such as
        /// "acme.savebackup". Used for the plugin's settings folder and
        /// to remember whether the user disabled it, so it must not
        /// change between releases.
        /// </param>
        /// <param name="name">The name shown to the user.</param>
        public NexusPluginAttribute(
            string id,
            string name)
        {
            Id = id;
            Name = name;
        }

        /// <summary>The permanent unique id, such as "acme.savebackup".</summary>
        public string Id { get; private set; }

        /// <summary>The name shown in the plugin list.</summary>
        public string Name { get; private set; }

        /// <summary>Who wrote it. Shown beside the name.</summary>
        public string Author { get; set; }

        /// <summary>Your own version string, such as "1.2.0".</summary>
        public string Version { get; set; }

        /// <summary>One line on what the plugin does.</summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Every plugin implements this. One class per plugin.
    /// </summary>
    public interface INexusPlugin
    {
        /// <summary>
        /// Called once on startup. Register everything the plugin
        /// offers here.
        ///
        /// Keep it quick: Nexus is still starting up. Anything slow
        /// belongs on a thread of your own, or in the scan that the
        /// launcher will call later anyway.
        /// </summary>
        void Initialize(INexusHost host);

        /// <summary>
        /// Called when Nexus is closing, or when the user disables the
        /// plugin. Stop timers and release anything held.
        /// </summary>
        void Shutdown();
    }

    /// <summary>
    /// What a plugin is given to work with.
    /// </summary>
    public interface INexusHost
    {
        /// <summary>The running Nexus version, such as "0.0.7.3".</summary>
        string LauncherVersion { get; }

        /// <summary>The running Nexus build number.</summary>
        int LauncherBuild { get; }

        /// <summary>
        /// A folder of the plugin's own, already created, for settings
        /// and caches. Nothing else writes here.
        /// </summary>
        string DataFolder { get; }

        /// <summary>Writes to the Nexus log, tagged with your plugin.</summary>
        ILogger Log { get; }

        /// <summary>
        /// Contributes games to the library, under the source's own
        /// heading.
        /// </summary>
        void AddLibrarySource(ILibrarySource source);

        /// <summary>
        /// Adds an entry to the menu on a game.
        /// </summary>
        void AddGameAction(IGameAction action);

        /// <summary>
        /// Adds a tile to the side panel, beside the system readings.
        /// </summary>
        void AddSidePanelWidget(ISidePanelWidget widget);

        /// <summary>
        /// Reads one of your own settings, as the user left it.
        ///
        /// Returns the fallback until the user has changed it, so a
        /// plugin never has to deal with a missing value.
        /// </summary>
        string GetSetting(string key, string fallback = null);

        /// <summary>Reads a yes or no setting of your own.</summary>
        bool GetSetting(string key, bool fallback);

        /// <summary>
        /// Every game Nexus currently knows about, from every source
        /// including other plugins.
        ///
        /// A snapshot, not a live view: call it again for fresh
        /// results. Empty while Nexus is still scanning at startup,
        /// so a plugin that wants the whole library should ask when
        /// the user does something rather than in Initialize.
        /// </summary>
        IEnumerable<GameRef> GetGames();

        /// <summary>Raised after a game is launched from Nexus.</summary>
        event EventHandler<GameEventArgs> GameLaunched;

        /// <summary>
        /// Raised when a launched game is seen to close. Not
        /// guaranteed: Nexus can only time a game whose process it can
        /// find, so treat this as a courtesy rather than a promise.
        /// </summary>
        event EventHandler<GameEventArgs> GameExited;
    }

    /// <summary>Somewhere to write what your plugin is doing.</summary>
    public interface ILogger
    {
        /// <summary>Ordinary progress.</summary>
        void Info(string message);

        /// <summary>Something looks wrong but the plugin carried on.</summary>
        void Warn(string message);

        /// <summary>Something failed. Pass the exception when there is one.</summary>
        void Error(string message, Exception error = null);
    }

    //------------------------------------------------------------------
    // Library sources
    //------------------------------------------------------------------

    /// <summary>
    /// A place games come from: a launcher Nexus does not know about,
    /// a folder of portable games, anything.
    /// </summary>
    public interface ILibrarySource
    {
        /// <summary>
        /// Stable id for this source, such as "amazon". Games are
        /// remembered against it, so it must not change.
        /// </summary>
        string Id { get; }

        /// <summary>The heading its games appear under.</summary>
        string DisplayName { get; }

        /// <summary>
        /// Whether this source is present on the machine. Return false
        /// and Nexus leaves the heading out entirely rather than
        /// showing an empty one.
        /// </summary>
        bool IsInstalled();

        /// <summary>
        /// Finds the games. Called on a background thread, so it may
        /// take its time, but it must not touch the user interface.
        /// Throwing is safe: the failure is logged and the rest of the
        /// library still loads.
        /// </summary>
        IEnumerable<PluginGame> Scan();
    }

    /// <summary>
    /// A game as a plugin describes it. Only Name is required; fill in
    /// whatever else is known and Nexus will do the best it can with
    /// the rest, including finding artwork.
    /// </summary>
    public class PluginGame
    {
        /// <summary>What to call the game. The only required field.</summary>
        public string Name { get; set; }

        /// <summary>
        /// The source's own id for the game, if it has one. Used to
        /// tell two games with the same name apart.
        /// </summary>
        public string SourceGameId { get; set; }

        /// <summary>The folder the game lives in, if known.</summary>
        public string InstallPath { get; set; }

        /// <summary>The executable, when there is one to run.</summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// Arguments for that executable.
        ///
        /// Several launchers will not start a game from its own exe
        /// and want their own helper run instead: Wargaming games,
        /// for one, are started with their folder's wgc_api.exe and
        /// --open.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// A uri that starts the game through its own launcher, such
        /// as "amazon-games://play/...". Preferred over the executable
        /// where the launcher needs to be involved.
        /// </summary>
        public string LaunchUri { get; set; }

        /// <summary>An icon file for the game, if the source has one.</summary>
        public string IconPath { get; set; }

        /// <summary>Optional artwork already on disk or on the web.</summary>
        public string GridImageUrl { get; set; }

        /// <summary>Wide banner artwork, if the source has one.</summary>
        public string HeroImageUrl { get; set; }
    }

    //------------------------------------------------------------------
    // Game actions
    //------------------------------------------------------------------

    /// <summary>
    /// An entry on a game's menu: "Back up saves", "Open install
    /// folder", whatever the plugin does.
    /// </summary>
    public interface IGameAction
    {
        /// <summary>Stable id, used for ordering and settings.</summary>
        string Id { get; }

        /// <summary>The text on the menu entry.</summary>
        string Caption { get; }

        /// <summary>
        /// Whether the entry applies to this game. Return false and it
        /// is left out for that game, rather than shown greyed.
        /// </summary>
        bool AppliesTo(GameRef game);

        /// <summary>
        /// Runs the action. Called on the user interface thread, so
        /// anything slow belongs on a thread of your own or the
        /// launcher will appear to freeze.
        /// </summary>
        void Invoke(GameRef game);
    }

    /// <summary>
    /// A game as the launcher knows it, handed to plugins read only.
    /// </summary>
    public class GameRef
    {
        /// <summary>The game's name.</summary>
        public string Name { get; set; }

        /// <summary>Which launcher or source it came from.</summary>
        public string SourceId { get; set; }

        /// <summary>Where it is installed, if known.</summary>
        public string InstallPath { get; set; }

        /// <summary>Its executable, if known.</summary>
        public string ExecutablePath { get; set; }

        /// <summary>Total time played, as Nexus has recorded it.</summary>
        public TimeSpan Played { get; set; }

        /// <summary>When it was last played, or null if never.</summary>
        public DateTime? LastPlayedUtc { get; set; }
    }

    /// <summary>Carries the game a launch or exit event is about.</summary>
    public class GameEventArgs : EventArgs
    {
        /// <summary>Creates the arguments for a game event.</summary>
        public GameEventArgs(
            GameRef game)
        {
            Game = game;
        }

        /// <summary>The game the event is about.</summary>
        public GameRef Game { get; private set; }
    }

    //------------------------------------------------------------------
    // Side panel widgets
    //------------------------------------------------------------------

    /// <summary>
    /// A small read only tile in the side panel.
    ///
    /// Text rather than controls on purpose: the plugin says what it
    /// wants to show and Nexus draws it in the current theme, so these
    /// cannot look out of place and cannot break the layout.
    /// </summary>
    public interface ISidePanelWidget
    {
        /// <summary>Stable id, used to remember the tile's place.</summary>
        string Id { get; }

        /// <summary>The heading on the tile.</summary>
        string Title { get; }

        /// <summary>
        /// The lines to show. Called on a background thread on the
        /// interval below.
        /// </summary>
        IEnumerable<string> GetLines();

        /// <summary>
        /// How often to ask again. Anything under five seconds is
        /// treated as five.
        /// </summary>
        TimeSpan RefreshInterval { get; }
    }

    //------------------------------------------------------------------
    // Plugin settings
    //------------------------------------------------------------------

    /// <summary>What kind of control a setting needs.</summary>
    public enum PluginSettingKind
    {
        /// <summary>A line of text.</summary>
        Text,

        /// <summary>A tick box.</summary>
        Toggle,

        /// <summary>A whole number.</summary>
        Number,

        /// <summary>A folder, with a Browse button.</summary>
        Folder,

        /// <summary>One of a fixed list.</summary>
        Choice
    }

    /// <summary>
    /// One setting, as the plugin describes it.
    ///
    /// Declared rather than drawn: the plugin says what it needs and
    /// Nexus builds the dialog in the current theme. That keeps
    /// plugins free of any user interface code, and means every
    /// plugin's settings look like part of the launcher instead of
    /// like a bolted-on window.
    /// </summary>
    public class PluginSetting
    {
        /// <summary>How your plugin reads the value back.</summary>
        public string Key { get; set; }

        /// <summary>The label beside the control.</summary>
        public string Label { get; set; }

        /// <summary>Optional line of help under the label.</summary>
        public string Description { get; set; }

        /// <summary>Which control to show. Text if not set.</summary>
        public PluginSettingKind Kind { get; set; }

        /// <summary>The value before the user changes anything.</summary>
        public string Default { get; set; }

        /// <summary>The options, when Kind is Choice.</summary>
        public IEnumerable<string> Choices { get; set; }
    }

    /// <summary>
    /// Implement this as well as INexusPlugin and your plugin gets a
    /// Settings button on the Plugins page.
    /// </summary>
    public interface IPluginSettings
    {
        /// <summary>The settings to show, in the order to show them.</summary>
        IEnumerable<PluginSetting> GetSettings();

        /// <summary>
        /// Called after the user saves, with every value. Nexus has
        /// already stored them by the time this runs, so this is only
        /// for reacting to a change.
        /// </summary>
        void SettingsChanged(IDictionary<string, string> values);
    }

    /// <summary>
    /// A button in the plugin's settings dialog.
    ///
    /// For things a plugin does on request rather than things it
    /// remembers: exporting a file, clearing a cache, running a scan
    /// now. Settings dialogs are where a user goes looking for a
    /// plugin's own features, so they belong there rather than
    /// cluttering the menu on every game.
    /// </summary>
    public class PluginCommand
    {
        /// <summary>Passed back to RunCommand when pressed.</summary>
        public string Id { get; set; }

        /// <summary>The text on the button.</summary>
        public string Label { get; set; }

        /// <summary>Optional line of help beside the button.</summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Implement this as well as INexusPlugin to put buttons in your
    /// settings dialog. Works with or without IPluginSettings.
    /// </summary>
    public interface IPluginCommands
    {
        /// <summary>The buttons to show, in order.</summary>
        IEnumerable<PluginCommand> GetCommands();

        /// <summary>
        /// Runs one. Called on the user interface thread, so put
        /// anything slow on a thread of your own.
        /// </summary>
        void RunCommand(string commandId);
    }
}
