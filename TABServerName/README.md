# TABServerName

*Read this in [Turkish / Türkçe](README.tr.md).*

Customizes the map part of the `{mod} | {map}` label on the TAB scoreboard by replacing `CNetworkGameServer::m_MapName` with the text from the config at map start (through the engine's own `tier0` allocator, via `CUtlString::Set`).

## Features

- Applied automatically on every `OnMapStart`, before players connect (the client only reads this value once, at connect time)
- The `{MAP}` placeholder in the config text is replaced with the real map name
- Never touches the mod name (Competitive/Casual/etc.), it stays natively correct
- Uses the engine's own memory manager (`tier0` export), so it does not crash on a live map change
- No offsets or export names are baked into the code, they live in a `gamedata` file (CounterStrikeSharp's own native gamedata system); if a CS2/CounterStrikeSharp update shifts these values the file can be fixed without waiting for a new plugin version

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `TABServerName` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/TABServerName/
   ```
2. Copy the `gamedata/TABServerName.gamedata.json` file into CounterStrikeSharp's shared gamedata folder:
   ```
   csgo/addons/counterstrikesharp/gamedata/TABServerName.gamedata.json
   ```
3. Restart the server or run `css_plugins load TABServerName`.

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/TABServerName/TABServerName.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `servername_text` | string | `"{MAP} \| github.com/ByDexterTR/CS2Plugins"` | Text shown in the map area on TAB; placeholders are replaced with real values |

### Placeholders

| Placeholder | Value |
| --- | --- |
| `{MAP}` | Map name |
| `{HOSTNAME}` | The `hostname` convar (server name) |
| `{IP}` | The `ip` convar (server IP address) |
| `{PORT}` | The `hostport` convar (server port) |

### Example Config

```json
{
  "servername_text": "{MAP} | {HOSTNAME} | {IP}:{PORT}"
}
```

## Notes

- The `offsets` (`INetworkServerService_GetIGameServer`, `CNetworkGameServer_MapName`) and `signatures` (`CUtlString_Set`, tier0's real export name) values in `gamedata/TABServerName.gamedata.json` were found by reverse engineering; if a CS2/CounterStrikeSharp/tier0 update breaks them the plugin disables itself silently (writing `[TABServerName] DEVRE DISI: ...` to the console) rather than crashing the server.
- The change is only visible on the top label of the TAB scoreboard; it does not affect the Steam server browser / A2S query (that is a separate data path).

## Known Issues

- `CNetworkGameServer::m_MapName` is a single field; it is not only what the client TAB reads — CounterStrikeSharp's own `Server.MapName` / `NativeAPI.GetMapName()` call (and other plugins relying on it) may read the same field. Because TABServerName replaces this field with its own text (`{MAP} | ...`), another plugin that uses the map name somewhere like a file/folder name will now get this spoofed text instead of the real map name.
- Workaround: avoid characters that are problematic for the file system (`/`, `\`, `:`, `|`) in the `servername_text` value, AND test any other plugins on the same server that read the map name for files/APIs. There is no permanent fix yet; this is an inherent limitation of the method of writing directly to `CNetworkGameServer::m_MapName`.
