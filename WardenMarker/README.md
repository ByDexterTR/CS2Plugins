# WardenMarker

*Read this in [Turkish / Türkçe](README.tr.md).*

Gives the warden a single marker ring that jumps to the point they are looking at the moment they press their key. A glowing disc sits in the middle and stays visible through walls, so prisoners can see where to gather from anywhere.

## Features

- Drops the ring instantly at the point the warden is looking at
- One marker only; look somewhere else and press the key to move it
- Placement key is up to the player: Use (E), Inspect (F), Ping (Middle Mouse) or off
- Color, size and width are picked from the menu
- The middle disc and its glow can be turned off separately, alpha is adjustable
- Every player's choices are saved and restored on their next connect
- CT side only; the marker disappears when the player joins T
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371
- The `gamedata` file: `addons/counterstrikesharp/gamedata/NativeTrace.gamedata.json`

## Installation

1. Copy the compiled `WardenMarker` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/WardenMarker/
   ```
2. Restart the server or run `css_plugins load WardenMarker`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_marker` | Opens the marker menu | `marker_flag` |
| `css_isaret` | Same as `css_marker` | `marker_flag` |

### Menu

| Option | Function |
| --- | --- |
| Clear My Marker | Removes your own marker |
| Marker Settings | Color / Size / Width |
| Disc Settings | Disc / Glow / Alpha |
| Key | Use (E) → Inspect (F) → Ping (Middle Mouse) → Off |

## Configuration

`addons/counterstrikesharp/configs/plugins/WardenMarker/WardenMarker.json`

| Setting | Default | Description |
| --- | --- | --- |
| `marker_cmd` | `css_marker,css_isaret` | Menu commands |
| `marker_flag` | `@jailbreak/warden` | Marker permission (root gets no implicit access) |
| `marker_default_key` | `use` | Default key: `use`, `inspect`, `ping`, `off` |
| `marker_cooldown` | `0.3` | Delay between two moves (seconds) |
| `marker_max_distance` | `8192` | Furthest distance a marker can be placed at |
| `marker_clear_on_roundend` | `true` | Clears every marker when the round ends |
| `marker_clear_on_death` | `false` | Clears a player's marker when they die |

### `marker_ring`

| Setting | Default | Description |
| --- | --- | --- |
| `colors` | 6 colors | Colors offered by the menu (`#RRGGBB` or `R G B`) |
| `sizes` | `100, 150, 200, 250, 300` | Ring sizes offered by the menu |
| `widths` | `2, 4, 6, 8, 10` | Ring widths offered by the menu |
| `default_color` | `#00AFFF` | Default color |
| `default_size` | `150` | Default size |
| `default_width` | `4` | Default width |

### `marker_disc`

| Setting | Default | Description |
| --- | --- | --- |
| `enabled` | `true` | Whether the middle disc starts enabled |
| `glow` | `true` | Glow master switch; nobody can turn it on when `false` |
| `glow_range` | `4096` | Distance the glow is visible from |
| `alphas` | `64, 128, 178, 255` | Alpha values offered by the menu |
| `default_alpha` | `178` | Default alpha |

Defaults that are missing from their list fall back to the first value of that list.

## Notes

- The key does not move the marker while the menu is open; E selects in the menu, R closes it.
