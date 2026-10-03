# API reference

Namespace `Nexus.Plugin`, assembly `Nexus.Plugin.SDK.dll`.

The signatures below were generated from the built assembly, so they
match what you are referencing. Every member also carries an XML doc
comment, so IntelliSense will tell you the same thing in the editor —
keep `Nexus.Plugin.SDK.xml` beside the dll.

---

## Required

### `NexusPluginAttribute` (attribute)

Marks a class as a plugin and describes it. An attribute rather than a
manifest file, so there is nothing to keep in step.

```csharp
new NexusPluginAttribute(string id, string name)

string Id          { get; }   // permanent and unique, e.g. "acme.savebackup"
string Name        { get; }   // shown in the plugin list
string Author      { get; set; }
string Version     { get; set; }
string Description { get; set; }
```

`Id` names your settings folder and records whether the user disabled
you. **Changing it in a later release loses both.**

### `INexusPlugin` (interface)

```csharp
void Initialize(INexusHost host)
void Shutdown()
```

`Initialize` runs during startup — register what you offer and return.
Anything slow belongs on a thread of your own. `Shutdown` runs when
Nexus closes.

---

## The host

### `INexusHost` (interface)

```csharp
string  LauncherVersion { get; }
int     LauncherBuild   { get; }
string  DataFolder      { get; }   // yours alone, already created
ILogger Log             { get; }

void AddLibrarySource(ILibrarySource source)
void AddGameAction(IGameAction action)
void AddSidePanelWidget(ISidePanelWidget widget)

IEnumerable<GameRef> GetGames()
string GetSetting(string key, string fallback)
bool   GetSetting(string key, bool fallback)

event EventHandler<GameEventArgs> GameLaunched
event EventHandler<GameEventArgs> GameExited
```

`GetGames()` is a snapshot, empty while Nexus is still scanning.

`GameExited` only fires for sessions Nexus could time; see the README.

### `ILogger` (interface)

```csharp
void Info(string message)
void Warn(string message)
void Error(string message, Exception error)
```

Everything is tagged with your plugin's id in the Nexus log.

---

## Contributing to the library

### `ILibrarySource` (interface)

```csharp
string Id          { get; }   // stable, e.g. "amazon"
string DisplayName { get; }   // the sidebar heading

bool IsInstalled()
IEnumerable<PluginGame> Scan()
```

`IsInstalled()` false means no heading at all. `Scan()` runs on a
background thread and may throw safely — the failure is logged and the
rest of the library still loads.

A source whose `Id` matches a launcher Nexus already has a heading for
(`amazon`, `paradox`, `wargaming`, `steam`, `epic`, `gog`, `ubisoft`,
`ea`, `battlenet`, `xbox`) fills that heading, icons and all. Any
other id gets a heading created for it.

### `PluginGame` (class)

```csharp
string Name           { get; set; }   // the only one required
string SourceGameId   { get; set; }
string InstallPath    { get; set; }
string ExecutablePath { get; set; }
string Arguments      { get; set; }   // e.g. Wargaming needs "wgc_api.exe --open"
string LaunchUri      { get; set; }   // preferred where the launcher must be involved
string IconPath       { get; set; }
string GridImageUrl   { get; set; }
string HeroImageUrl   { get; set; }
```

---

## Acting on a game

### `IGameAction` (interface)

```csharp
string Id      { get; }
string Caption { get; }

bool AppliesTo(GameRef game)
void Invoke(GameRef game)
```

Asked per right-click. `Invoke` is on the UI thread.

### `GameRef` (class)

A read-only copy of a game. Plugins never see Nexus's own game type.

```csharp
string    Name           { get; set; }
string    SourceId       { get; set; }   // which launcher it came from
string    InstallPath    { get; set; }
string    ExecutablePath { get; set; }
TimeSpan  Played         { get; set; }
DateTime? LastPlayedUtc  { get; set; }
```

### `GameEventArgs` (class)

```csharp
new GameEventArgs(GameRef game)
GameRef Game { get; }
```

---

## The side panel

### `ISidePanelWidget` (interface)

```csharp
string   Id              { get; }
string   Title           { get; }
TimeSpan RefreshInterval { get; }   // floored at five seconds

IEnumerable<string> GetLines()
```

Text, not controls: you say what to show and Nexus draws it in the
current theme, so a tile cannot look out of place or break the layout.
`GetLines()` is called on a background thread.

---

## Settings and buttons

### `IPluginSettings` (interface)

```csharp
IEnumerable<PluginSetting> GetSettings()
void SettingsChanged(IDictionary<string, string> values)
```

Values are stored by Nexus before `SettingsChanged` runs, so that
callback is only for reacting. Read values back with
`host.GetSetting`.

### `PluginSetting` (class)

```csharp
string              Key         { get; set; }
string              Label       { get; set; }
string              Description { get; set; }
PluginSettingKind   Kind        { get; set; }
string              Default     { get; set; }
IEnumerable<string> Choices     { get; set; }   // when Kind is Choice
```

### `PluginSettingKind` (enum)

`Text`, `Toggle`, `Number`, `Folder`, `Choice`

### `IPluginCommands` (interface)

```csharp
IEnumerable<PluginCommand> GetCommands()
void RunCommand(string commandId)
```

Buttons in your settings dialog. `RunCommand` is on the UI thread.

### `PluginCommand` (class)

```csharp
string Id          { get; set; }
string Label       { get; set; }
string Description { get; set; }
```

---

## Added by Nexus

A plugin that registers a library source automatically gets a **Show
in the sidebar** switch per source in its settings dialog. You do not
declare these; whether a source is listed is the launcher's business,
not the plugin's.
