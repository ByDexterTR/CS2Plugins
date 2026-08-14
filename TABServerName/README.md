# TABServerName

*Read this in [Turkish / Türkçe](README.tr.md).*

Replaces the map part of the `{mod} | {map}` label at the top of the TAB scoreboard with the text you write in the config. That way, when players press TAB they see your server name, your IP or any text you like next to the map name.

```
Normally:            Competitive | Mirage
With this plugin:    Competitive | de_mirage | bydexter.net | 5v5 RETAKE
```

## Features

- Applied automatically at the start of every map
- The text can include placeholders for the map name, server name, IP and port
- Never touches the mod name (Competitive / Casual / etc.), that part stays as it is
- Does not affect the server across live map changes

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

> The second step is required. That file goes into the shared `gamedata` folder, not into the plugin folder.

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/TABServerName/TABServerName.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `servername_text` | string | `"{MAP} \| github.com/ByDexterTR/CS2Plugins"` | Text shown in the map area on TAB |

### Placeholders

| Placeholder | Value |
| --- | --- |
| `{MAP}` | Map name |
| `{HOSTNAME}` | Server name (`hostname`) |
| `{IP}` | Server IP address |
| `{PORT}` | Server port |

### Example Config

```json
{
  "servername_text": "{MAP} | {HOSTNAME} | {IP}:{PORT}"
}
```

## Notes

- The change is only visible on the TAB scoreboard. Your name in the Steam server browser and the replies to server queries are not affected.
- If the text stops changing after a CS2 or CounterStrikeSharp update, the `gamedata` file needs updating. In that case the plugin disables itself and writes `[TABServerName] DEVRE DISI: ...` to the server console; your server will not crash.

## Known Issues

- Because this plugin changes where the map name is stored, **other plugins** that use the map name read this new text too. For example, a plugin that creates a file per map will try to use your text as the file name instead of the real map name, and may throw an error.
- For that reason, avoid characters that cause trouble in file names — `/`, `\`, `:` and `|` — in `servername_text`. If your server runs other plugins that use the map name (stats, spawn saving, map voting and so on), be sure to test them after enabling this plugin.
- There is no permanent fix for this; it is an inherent limitation of the method used.
