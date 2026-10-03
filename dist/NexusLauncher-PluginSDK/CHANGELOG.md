# Nexus Launcher Plugin SDK — changes

## 1.1.0

- `PluginGame.Arguments` — arguments for `ExecutablePath`. Needed by
  launchers that will not start a game from its own exe: a Wargaming
  game is started with its folder's `wgc_api.exe --open`.
- Nexus now launches plugin games. Before this it recognised only its
  own launchers and silently did nothing for everything else.

## 1.0.0

First release. Requires Nexus Launcher build 1020 or newer.

**You can**

- Contribute games to the library, under your own sidebar heading
  (`ILibrarySource`).
- Add an entry to the right-click menu on a game (`IGameAction`).
- Add a tile to the side panel (`ISidePanelWidget`).
- Declare settings and let Nexus build the dialog (`IPluginSettings`).
- Put buttons in that dialog (`IPluginCommands`).
- React to games starting and finishing (`GameLaunched`, `GameExited`).
- Read the library (`GetGames`), your own settings (`GetSetting`),
  your own folder (`DataFolder`) and the log (`Log`).

**Known limits**

- Plugins run in the Nexus process. There is no sandbox.
- Enabling and disabling take full effect at the next start; .NET
  Framework cannot unload an assembly without a separate AppDomain.
- Tiles share the side panel's system section rather than getting a
  panel of their own.
