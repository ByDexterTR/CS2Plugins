# Thirdperson

*Read this in [Turkish / Türkçe](README.tr.md).*

Standalone plugin that puts the player's camera into a third person (over the shoulder) view. Command names, permission, camera distance and wall blocking behavior are managed from the config.

## Features

- Toggle with a command; command names can be changed from the config
- Permission (flag) check — leave it empty and everyone can use it
- Adjustable camera distance
- **Wall blocking (`thirdperson_blockwall`)** — while enabled the camera cannot pass behind walls; native ray-trace pulls it toward the player at the point it hits the wall (prevents seeing through walls / wallhack abuse)
- The camera follows the player's view with no lag
- Every third person camera is force-disabled **at round start and round end**
- The camera safely returns to its old state on death, disconnect and plugin unload
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `Thirdperson` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/Thirdperson/
   ```
2. Restart the server or run `css_plugins load Thirdperson`.
3. The config file is created automatically on first load.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_tp` / `css_thirdperson` | Toggles the third person view (while alive) | `thirdperson_flag` (default `@css/thirdperson`) |

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/Thirdperson/Thirdperson.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `thirdperson_cmd` | string | `"css_tp,css_thirdperson"` | Comma separated command names |
| `thirdperson_flag` | string | `"@css/thirdperson"` | Required permission; empty string = everyone can use it |
| `thirdperson_distance` | float | `110` | Distance of the camera from the player (minimum 20) |
| `thirdperson_blockwall` | bool | `true` | `true`: walls block the camera; `false`: the camera can pass behind walls |

### Example Config

```json
{
  "thirdperson_cmd": "css_tp,css_thirdperson",
  "thirdperson_flag": "@css/thirdperson",
  "thirdperson_distance": 110,
  "thirdperson_blockwall": true
}
```

## Notes

- If after a CS2 update the camera starts passing behind walls, the plugin needs an update; it keeps working in the meantime.
- While wall blocking is on, the camera stops at the first obstacle between the eye and the target point (leaving a 16 unit margin); if the obstacle is very close the camera is pulled to eye level.
- Command name changes (`thirdperson_cmd`) take effect when the server/plugin is restarted.
- If you want to give third person to your VIP members on a per-group basis, use the `Thirdperson` module in [VIPCore](../VIPCore); do not use both systems on the same player at the same time.
