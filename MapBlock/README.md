# MapBlock

*Read this in [Turkish / Türkçe](README.tr.md).*

Automatically closes off certain areas of the map with fence models while the player count is low. The blocks disappear by themselves once the server fills up.

## Features

- Per-map persistent fence layouts (`MapBlock.json`)
- Automatic open/close by player count: fences are placed below the threshold, removed once the threshold is reached
- Two counting modes: CT only or T+CT
- The state is re-evaluated at the start of every round
- The example layout file (`MapBlock.example.json`) is copied automatically on first run
- Reload command so you can edit the JSON by hand without restarting the server
- Fence models are precached automatically
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `MapBlock` folder to the server (**including `MapBlock.example.json`**):
   ```
   csgo/addons/counterstrikesharp/plugins/MapBlock/
   ```
2. Restart the server or run `css_plugins load MapBlock`.
3. On first load `MapBlock.example.json` is copied to `MapBlock.json`; add your own layouts to that file.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_mapblock_reload` | Reloads the `MapBlock.json` file and applies the layouts | `@css/root` |

## Configuration

Config file:

```
csgo/addons/counterstrikesharp/configs/plugins/MapBlock/MapBlock.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `mapblock_mode` | int | `1` | `0`: off, `1`: look at the CT count, `2`: look at the T+CT count |
| `mapblock_count` | int | `5` | Threshold; fences are placed while the count is **below** this value (`0` = always place) |

### Layout File (`MapBlock.json` in the plugin folder)

Formatted as map name → layout list:

```json
{
  "jb_map_name": [
    {
      "Model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_128_capped.vmdl",
      "Origin": [512.0, -128.0, 64.0],
      "Angles": [0.0, 90.0, 0.0]
    }
  ]
}
```

| Field | Description |
| --- | --- |
| `Model` | One of the precached fence models (64/128/256 sizes) |
| `Origin` | `[x, y, z]` world coordinate |
| `Angles` | `[pitch, yaw, roll]` angle values |

## Usage Example

- `mapblock_mode: 1`, `mapblock_count: 5` → fences are placed while there are fewer than 5 CTs on the server; when the 5th CT joins the fences are removed at the start of the next round.
- To work out layout coordinates you can place a fence with the [Cit](../Cit) plugin and move the position into `MapBlock.json`.

## Notes

- Fences are tagged with the name `bydexter_mapblock`; cleanup only targets those props.
- Map name matching is case insensitive.
