# MapBlock

*Read this in [Turkish / Türkçe](README.tr.md).*

Automatically closes off certain areas of the map with fence models while the player count is low, and removes the blocks by itself once the teams fill up. You place the blocks from an in-game menu.

## Features

- Create and delete blocks from an in-game menu; every change is saved immediately
- A separate layout file per map (`maps/` folder)
- Automatic open/close by player count, re-evaluated at the start of every round
- No blocks are placed during warmup
- Two counting modes: CT only, or both teams at once
- Bots and the HLTV/GOTV proxy are not counted
- Chat announcement on every round the blocks go up
- Edit mode lets you see and arrange the blocks even when the server is full
- The models offered by the menu come from the config
- Reload command so you can edit by hand without restarting the server
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.373
- `gamedata` file: `addons/counterstrikesharp/gamedata/NativeTrace.gamedata.json`

## Installation

1. Copy the compiled `MapBlock` folder to the server (**including `MapBlock.example.json`**):
   ```
   csgo/addons/counterstrikesharp/plugins/MapBlock/
   ```
2. Restart the server or run `css_plugins load MapBlock`.
3. On first load the example layouts are split map by map into the `maps/` folder; after that the menu writes them itself.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_mapblock` | Opens the block menu (you must be alive) | `mapblock_flag` |
| `css_engel` | Opens the same menu | `mapblock_flag` |
| `css_mapblock_reload` | Reloads and applies the current map's layout file | `mapblock_reload_flag` |

### Menu Options

| Option | What it does |
| --- | --- |
| Create | Places the selected model where you are looking and saves it |
| Change Model | Cycles through the models in `mapblock_models` |
| Delete Aimed Block | Removes the block you are aiming at (max. 256 units away) and deletes it from the layout |
| Delete All Blocks On Map | Clears every block on the current map |
| Edit Mode | Keeps the blocks up regardless of the player count |

Blocks are shifted sideways by the model's `offset` value, so aim at the point where the right-hand edge of the model should sit. When you turn edit mode off the plugin returns to the automatic rule immediately.

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/MapBlock/MapBlock.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `mapblock_mode` | int | `2` | `0`: off, `1`: looks at the CT count, `2`: looks at both teams |
| `mapblock_count` | int | `4` | Threshold; blocks are placed while the count is **below** this value (`0` = always place) |
| `mapblock_announce` | bool | `true` | Writes a chat announcement on every round the blocks go up |
| `mapblock_cmd` | string | `"css_mapblock,css_engel"` | Commands that open the menu, separated by commas |
| `mapblock_flag` | string | `"@css/root"` | Flags allowed to use the menu, separated by commas |
| `mapblock_reload_cmd` | string | `"css_mapblock_reload"` | Reload commands, separated by commas |
| `mapblock_reload_flag` | string | `"@css/root"` | Flags allowed to use the reload command, separated by commas |
| `mapblock_models` | object | 6 models | The models offered by the menu |

`mapblock_mode: 2` looks at the smaller of the two teams: with `mapblock_count: 4` the blocks stay up in 3v3, 4v3 and 3v4, and come down at the start of the next round once the teams reach 4v4. No blocks are placed during warmup, whatever the mode is.

### `mapblock_announce`

The wording follows the mode:

| Settings | Message in chat |
| --- | --- |
| `mapblock_mode: 1`, `mapblock_count: 4` | `The map was shrunk because there are fewer than 4 CTs.` |
| `mapblock_mode: 2`, `mapblock_count: 5` | `The map was shrunk because it is not 5v5.` |

The announcement is written at the start of every round the blocks go up; nothing is written when they come down. Nothing is announced if the map has no saved blocks, if `mapblock_count` is `0`, or while edit mode is on. You can change the wording from the `lang/` folder.

### `mapblock_models`

The menu name is the key, and the value holds the model path plus the placement offset:

```json
"mapblock_models": {
  "Cit 128": {
    "model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_128_capped.vmdl",
    "offset": 64.0
  }
}
```

| Field | Description |
| --- | --- |
| `model` | Model path |
| `offset` | How far the model is shifted sideways when placed; use half the model's width |

Any model you add here shows up correctly the moment you place it.

### Layout Files

They sit in the `maps/` folder inside the plugin folder, next to `lang/`. Every map has its own file, named after the map — `maps/de_mirage.json`:

```json
[
  {
    "Model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_128_capped.vmdl",
    "Origin": [512.0, -128.0, 64.0],
    "Angles": [0.0, 90.0, 0.0]
  }
]
```

| Field | Description |
| --- | --- |
| `Model` | Model path |
| `Origin` | `[x, y, z]` world coordinate |
| `Angles` | `[pitch, yaw, roll]` angle values |

The menu writes these files for you; you only need to open them if you want to edit coordinates by hand. Field names are case insensitive and trailing commas are fine. After editing by hand run `css_mapblock_reload`.

## Notes

- **Back up the `maps/` folder before updating the plugin.** It lives inside the plugin folder, so an installation that replaces that folder wipes your layouts.
- A single-file layout from an older version (`MapBlock.placements.json`, or `MapBlock.json` in the plugin folder) is split into the `maps/` folder on first load, and the old file is kept as a `.bak`.
- File names are lower-cased, so map name matching is case insensitive. A map with no blocks has no file at all.
- Blocks stay put, cannot be pushed and take no damage. Cleanup only removes the blocks placed by this plugin and never touches the map's own objects.
- The models used are the `de_nuke` chainlink fence models; they work on every official map.
