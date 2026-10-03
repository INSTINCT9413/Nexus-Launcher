# Nexus Launcher Plugin SDK

Everything needed to write a plugin for Nexus Launcher.

```
lib/        Nexus.Plugin.SDK.dll — the one assembly you reference
            Nexus.Plugin.SDK.xml — IntelliSense docs, keep beside the dll
template/   A working starter plugin. Copy it and start there.
samples/    The five plugins that ship with Nexus, in full.
API.md      Every type and member, with what each is for.
```

## What you need

| | |
|---|---|
| Framework | .NET Framework **4.8** |
| Language | C# 7.3 or lower |
| Build | Visual Studio 2019+, or MSBuild on its own |
| Platform | AnyCPU — Nexus runs x64 but the SDK is architecture neutral |
| Nexus | Build 1020 or newer |

No NuGet packages, no reference to Nexus itself, and nothing to
install. A plugin is one `.dll`.

## Five minutes to a working plugin

1. Copy `template/MyNexusPlugin` somewhere of your own.
2. Open `MyNexusPlugin.csproj`. Fix the `HintPath` on the
   `Nexus.Plugin.SDK` reference so it points at this pack's
   `lib/Nexus.Plugin.SDK.dll`.
3. In `MyPlugin.cs`, change the id and name in `[NexusPlugin]`. **The
   id is permanent** — it names your settings folder and records
   whether the user disabled you, so pick it once.
4. Build.
5. Copy `bin\Debug\MyNexusPlugin.dll` into
   `%AppData%\NexusLauncher\Plugins`.
6. Start Nexus. **Settings → Plugins** lists it.

Settings → Plugins → **Add plugin** does step 5 for you.

## Where plugins live

Nexus looks in two folders, and treats whatever it finds in either
exactly the same way — there is no such thing as a built-in plugin:

| Folder | Use it for |
|---|---|
| `%AppData%\NexusLauncher\Plugins` | **Everything, normally.** Needs no special rights and survives updates. Searched first. |
| `Plugins` beside `Nexus Launcher.exe` | Plugins shipped with a build, or a portable install. Replaced by an update, and may need administrator rights. Searched second. |

The user folder is searched first, so a copy you install yourself wins
over one shipped beside the exe. If the same plugin id turns up in
both, the one in use is listed with a note naming the copy that was
ignored, rather than the second one silently disappearing.

A plugin can be a loose `.dll` in either folder, or a folder of its
own containing its dependencies.

> Do not copy `Nexus.Plugin.SDK.dll` next to your plugin. Nexus loads
> its own copy; a second one gives you types that look identical but
> are not, and your plugin will fail to load. The template sets
> `<Private>False</Private>` for exactly this reason.

## What a plugin can do

Implement `INexusPlugin`, then register whichever of these you want in
`Initialize`:

**Library source** (`ILibrarySource`) — contribute games under their
own sidebar heading. `IsInstalled()` returning false leaves the
heading out entirely rather than showing an empty one. `Scan()` runs
on a background thread.

**Game action** (`IGameAction`) — an entry on the right-click menu for
a game. `AppliesTo` is asked per game; return false and the entry is
left out rather than greyed. `Invoke` runs on the UI thread.

**Side panel tile** (`ISidePanelWidget`) — a few lines of text beside
the system readings. `GetLines()` is called on a background thread on
your `RefreshInterval`, which is floored at five seconds.

**Settings** (`IPluginSettings`) — you *declare* settings; Nexus
builds and themes the dialog. Text, toggle, number, folder-with-browse
and dropdown. You write no UI code.

**Buttons** (`IPluginCommands`) — things your plugin does on request,
shown in your settings dialog. Use these rather than game actions for
anything that is not about one particular game.

**Events** — `host.GameLaunched` and `host.GameExited`.

## Things worth knowing

`host.DataFolder` is a folder of your own, already created, that
nothing else writes to.

`host.GetSetting(key, fallback)` reads your settings back. There is a
`bool` overload.

`host.GetGames()` is a snapshot of the whole library. It is **empty
while Nexus is still scanning at startup**, so ask when the user does
something, not in `Initialize`.

`host.Log` writes to the Nexus log with your plugin's id in front.

`GameExited` is a courtesy, not a promise. Many launches go out as a
uri to another client, and Nexus can only time a game whose process it
can find; sessions it cannot follow raise nothing.

## What the launcher guarantees

Every call into your plugin is wrapped. If you throw, it is caught,
recorded against your plugin, and shown next to it in Settings. Your
plugin does not take Nexus down, and it does not silently vanish —
which matters most when the plugin failing is the one you are writing.

Plugins run **inside** the Nexus process. There is no sandbox: a
plugin can do anything the launcher can. Users are told this on the
Plugins page.

Enabling and disabling take full effect at the next start. .NET
Framework cannot unload an assembly without a separate AppDomain, and
marshalling this API across one would make plugin authoring much
harder for something no user would ever see.

## Samples

| Sample | Worth reading for |
|---|---|
| `Sample` | All three hooks and both events, in one short file. Start here. |
| `ExtraLaunchers` | Three library sources in one plugin. Registry scanning, and why it refuses to claim games that belong to another store. |
| `LibraryExport` | Reading the library; buttons in a settings dialog; writing CSV and JSON without a dependency. |
| `SaveBackup` | Per-game settings of its own, its own dialogs, acting on `GameExited`, and pruning what it writes. |
| `DiscordPresence` | Events only. Talking to another process over a named pipe without taking a dependency. |

Each sample's `.csproj` already references `../../lib/Nexus.Plugin.SDK.dll`,
so they build straight from this pack.

## Troubleshooting

**It is not in the list.** The class needs `[NexusPlugin]` *and* must
implement `INexusPlugin`. A plugin with one but not the other is
listed with that as its error; a dll with neither is ignored as an
ordinary dependency.

**It is listed with an error about a missing type.** Your plugin was
built against something not present. Ship the dependency in a folder
beside your dll — a plugin can be a loose dll, or its own folder.

**It loaded but nothing happens.** Check you registered in
`Initialize`. The Plugins page says what each plugin registered.

**Settings button missing.** It appears when you implement
`IPluginSettings` or `IPluginCommands`, or register a library source.

## Versioning

`Nexus.Plugin.SDK` is versioned separately from the launcher so the
launcher can move without breaking plugins. `host.LauncherVersion` and
`host.LauncherBuild` are there if you need to care.
