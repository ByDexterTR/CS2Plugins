# Lazer

*Read this in [Turkish / Türkçe](README.tr.md).*

Shows dead players and spectators where living players are currently aiming, as a laser beam. The beam runs from the player's eye along their view direction to the first obstacle (wall, floor, player); living players cannot see the beams.

## Features

- Every living player's view direction is drawn as a laser in real time (every tick)
- Native ray-trace ends the beam at the first obstacle (it does not go through walls)
- Beams are only sent to dead players/spectators who have the laser enabled (`CheckTransmit`); living players and GOTV never see them
- Toggle per player with `css_lazer`; the default state can be set from the config
- Team based beam color (separate T / CT, `R G B` or `#RRGGBB` from the config)
- Beam width and maximum distance are adjustable
- Beams are cleaned up safely on death, disconnect, round start/end and plugin unload
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `Lazer` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/Lazer/
   ```
2. Restart the server or run `css_plugins load Lazer`.
3. The config file is created automatically on first load.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_lazer` / `css_laser` | Toggles laser visibility (the preference applies while dead) | None |

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/Lazer/Lazer.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `lazer_cmd` | string | `"css_lazer,css_laser"` | Comma separated command names |
| `lazer_default_on` | bool | `true` | Initial laser state for newly connecting players |
| `lazer_width` | float | `0.4` | Beam width (minimum 0.1) |
| `lazer_max_distance` | float | `8192` | Maximum beam length (minimum 256) |
| `lazer_t_color` | string | `"234 210 139"` | T team beam color (`R G B` or `#RRGGBB`) |
| `lazer_ct_color` | string | `"182 212 238"` | CT team beam color (`R G B` or `#RRGGBB`) |

### Example Config

```json
{
  "lazer_cmd": "css_lazer,css_laser",
  "lazer_default_on": true,
  "lazer_width": 0.4,
  "lazer_max_distance": 8192,
  "lazer_t_color": "234 210 139",
  "lazer_ct_color": "182 212 238"
}
```

## Notes

- Beam cutting is bound to the game engine's trace function via a signature scan (`CNavPhysicsInterface`). If a game update breaks the signature the plugin keeps working but the beams extend to the maximum distance instead of being cut at obstacles (an error is written to the console).
- If no dead player has the laser enabled, no beams are created and trace is never called; the performance cost drops to zero.
- When a player changes team the beam color updates automatically on their next spawn.
- Command name changes (`lazer_cmd`) take effect when the server/plugin is restarted.
