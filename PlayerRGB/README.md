# PlayerRGB

*Read this in [Turkish / Türkçe](README.tr.md).*

Colors the player model with a smooth RGB (rainbow) cycle. Toggled with a command and the preference is stored persistently.

## Features

- The model color cycles smoothly through red → green → blue
- The preference is stored in the `PlayerRGB.json` file — it turns back on automatically when the player rejoins the server
- The model color returns to normal instantly when turned off
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `PlayerRGB` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/PlayerRGB/
   ```
2. Restart the server or run `css_plugins load PlayerRGB`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_rgb` | Toggles the RGB effect | `@css/cheats` |

## Configuration

The SteamID64 list of players who have it enabled is kept in the `PlayerRGB.json` file inside the plugin folder:

```json
[
  "76561198000000000",
  "76561198000000001"
]
```

## Notes

- The effect is only applied to living players.
- The color cycle is shared server-wide (every player with RGB gets the same color at the same time).
- File writes are done in the background.
