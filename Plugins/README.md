# Writing a plugin for Nexus Launcher

A Nexus plugin is a single .NET Framework 4.8 class library that
references one assembly, `Nexus.Plugin.SDK.dll`. No installer, no
registry, no reference to Nexus itself, and nothing to keep in step
with the launcher's own code.

## The shortest possible plugin

```csharp
using Nexus.Plugin;

[NexusPlugin("yourname.hello", "Hello",
    Author = "You", Version = "1.0.0",
    Description = "Adds one entry to the game menu.")]
public class HelloPlugin : INexusPlugin
{
    public void Initialize(INexusHost host)
    {
        host.Log.Info("hello from a plugin");
    }

    public void Shutdown() { }
}
```

1. Create a **Class Library (.NET Framework)**, target **4.8**.
2. Add a reference to `Nexus.Plugin.SDK.dll`, which sits next to
   `Nexus Launcher.exe`.
3. Write one class with `[NexusPlugin]` that implements
   `INexusPlugin`.
4. Build, then copy the .dll into the plugins folder and restart
   Nexus.

The plugins folder is
`%AppData%\NexusLauncher\Plugins`, and **Settings → Plugins → Open
plugins folder** goes straight there. A plugin can be a loose .dll in
that folder, or a folder of its own if it ships dependencies beside
it.

Your plugin then appears in **Settings → Plugins**, with whatever
`Author`, `Version` and `Description` you gave it, and what it
registered.

## The three things a plugin can add

### A library source

Contributes games, under its own heading. This is how Amazon, Paradox
and Wargaming support is provided.

```csharp
host.AddLibrarySource(new MySource());
```

`Scan()` runs on a background thread, so it may take its time, but it
must not touch the user interface. Return whatever you know about each
game; only `Name` is required.

`IsInstalled()` returning false leaves the heading out altogether,
rather than showing an empty one.

### A game action

An entry on the menu for a game.

```csharp
host.AddGameAction(new MyAction());
```

`AppliesTo` decides whether the entry appears for that game at all —
return false and it is left out rather than greyed. `Invoke` runs on
the user interface thread, so put anything slow on a thread of your
own.

### A side panel tile

A few lines of text beside the system readings.

```csharp
host.AddSidePanelWidget(new MyWidget());
```

`GetLines()` is called on a background thread on your
`RefreshInterval`, which is floored at five seconds.

## Reacting to play

```csharp
host.GameLaunched += (s, e) => Log(e.Game.Name + " started");
host.GameExited   += (s, e) => Log(e.Game.Name + " ran for " + e.Game.Played);
```

`GameExited` is a courtesy, not a promise. Many launches go out as a
uri to another client, and Nexus can only time a game whose process it
can find; sessions it cannot follow are recorded as untracked and
raise nothing.

## Storage and logging

`host.DataFolder` is a folder of your own, already created, that
nothing else writes to. `host.Log` writes to the Nexus log with your
plugin's id in front, so a noisy plugin is obvious in a crash report.

`host.GetGames()` returns a snapshot of the whole library. It is empty
while Nexus is still scanning at startup, so ask when the user does
something rather than in `Initialize`.

## What the launcher guarantees

Every call into a plugin is wrapped. A plugin that throws is caught,
has the failure recorded against it, and is listed in Settings with
the error — it does not take Nexus down, and it does not silently
vanish, which would be worse when you are the one trying to debug it.

Plugins run **inside** the Nexus process. There is no sandbox: a
plugin can do anything the launcher can. Install ones you trust, and
expect your users to want to do the same.

Enabling or disabling takes full effect at the next start. .NET
Framework cannot unload an assembly without a separate AppDomain, and
marshalling this API across one would make plugin authoring much
harder for something the user would never see.

## Worked examples

All five ship with their source, in `Plugins/`:

| Plugin | Shows |
|---|---|
| `Nexus.Plugin.Sample` | All three hooks and both events. Start here. |
| `Nexus.Plugin.ExtraLaunchers` | Three library sources in one plugin — Amazon, Paradox, Wargaming. |
| `Nexus.Plugin.LibraryExport` | Reading the library; game actions; a tile. |
| `Nexus.Plugin.SaveBackup` | Per-game settings, its own dialogs, acting on `GameExited`. |
| `Nexus.Plugin.DiscordPresence` | Events only, and talking to another process without taking a dependency. |

## Versioning

`Nexus.Plugin.SDK` is versioned separately from the launcher, so the
launcher can move without breaking plugins. `host.LauncherVersion` and
`host.LauncherBuild` are there if you need to care.
