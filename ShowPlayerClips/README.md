# ShowPlayerClips

*Read this in [Turkish / Türkçe](README.tr.md).*

Shows the map's invisible tool brushes as colored lines: clip, player clip, ladder, grenade clip, trigger and more. Everyone turns them on for themselves with a single command.

## Features

- The invisible volumes around the player are drawn as colored lines
- Every type has its own color, taken from the color of its tool texture
- Which types are drawn is chosen from the config
- Only players who turned it on see the lines; other players and GOTV never see them
- Lines are drawn slightly off the surface so they stay readable on walls and floors
- Trigger volumes (teleport, push, hurt, buy zone, bomb site) are drawn as a box
- Works on workshop maps as well
- Command access can be restricted with a flag
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `ShowPlayerClips` folder to the server as it is:
   ```
   csgo/addons/counterstrikesharp/plugins/ShowPlayerClips/
   ```
2. Restart the server or run `css_plugins load ShowPlayerClips`.
3. The config file is created automatically on first load.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_showclips` / `css_clips` | Turns the lines on/off for you | `showclips_flag` |

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/ShowPlayerClips/ShowPlayerClips.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `showclips_cmd` | string | `"css_showclips,css_clips"` | Comma separated command names |
| `showclips_flag` | string | `"@css/generic"` | Flag needed for the command (empty means everyone) |
| `showclips_types` | string | `"clip,playerclip,trigger,ladder"` | Comma separated list of the types to draw |
| `showclips_colors` | object | see below | Color per type (`#RRGGBB` or `R G B`) |
| `showclips_radius` | float | `4096` | Lines further away than this are not drawn (minimum 128) |
| `showclips_max_beams` | int | `1000` | Maximum number of lines drawn at the same time (16 - 4096) |
| `showclips_width` | float | `0.5` | Line thickness (minimum 0.1) |
| `showclips_offset` | float | `1` | How far the lines sit off the surface, in units |
| `showclips_refresh` | float | `0.4` | How often the lines are refreshed, in seconds (minimum 0.1) |
| `showclips_move_step` | float | `24` | The lines are recalculated after the player moves this far |

### Example Config

```json
{
  "showclips_cmd": "css_showclips,css_clips",
  "showclips_flag": "@css/generic",
  "showclips_types": "clip,playerclip,trigger,ladder",
  "showclips_colors": {
    "clip": "#CD3920",
    "playerclip": "#C00078",
    "npcclip": "#8820CD",
    "grenadeclip": "#B6FC16",
    "ladder": "#F84A00",
    "blockbullets": "#F88005",
    "passbullets": "#25B9F5",
    "blocklos": "#0000F8",
    "blocksound": "#B5E51E",
    "blocklight": "#95C04A",
    "sky": "#B2E1FD",
    "water": "#00E8CA",
    "navclip": "#C508A7",
    "navspaceclip": "#527097",
    "teleportclip": "#2E9DA6",
    "controlclip": "#CD20A8",
    "otherclip": "#7821D3",
    "blockbomb": "#31D3AE",
    "trigger": "#F89A00",
    "ignorenpc": "#BA6D9C"
  },
  "showclips_radius": 4096,
  "showclips_max_beams": 1000,
  "showclips_width": 0.5,
  "showclips_offset": 1,
  "showclips_refresh": 0.4,
  "showclips_move_step": 24
}
```

## Types

| Type | Tool texture | What it is |
| --- | --- | --- |
| `clip` | `toolsclip` | Invisible wall that blocks both players and bots |
| `playerclip` | `toolsplayerclip` | Invisible wall that blocks only players |
| `npcclip` | `toolsnpcclip` | Invisible wall that blocks only bots |
| `grenadeclip` | `toolsgrenadeclip` | Volume that blocks grenades |
| `ladder` | `toolsinvisibleladder` | Invisible ladder |
| `trigger` | `toolstrigger` | Trigger volumes: teleport, push, hurt, buy zone, bomb site |
| `blockbullets` | `toolsblockbullets` | Blocks bullets, players walk through |
| `passbullets` | — | Solid surface that lets bullets through |
| `blocklos` | `toolsblock_los` | Volume that blocks bot vision |
| `blocksound` | `toolsblocksound` | Volume that blocks sound |
| `blocklight` | `toolsblocklight` | Volume that blocks light |
| `blockbomb` | `toolsblockbomb` | Volume where the bomb cannot be planted |
| `navclip`, `navspaceclip` | `toolsnavclip`, `toolsnavspaceclip` | Areas closed to bot navigation |
| `teleportclip` | `toolsteleportclip` | Volume that blocks teleporting |
| `controlclip`, `otherclip` | `toolscontrolclip`, `toolsotherclip` | Special clip types |
| `ignorenpc` | `toolsignorenpc` | Geometry bots ignore |
| `sky` | `toolsskybox` | Sky boundary |
| `water` | — | Water volume |

Not every map contains every type. The types a map contains are written to the server console when the map loads.

## Notes

- On the first load of a map the lines are prepared in the background. If you run the command during that time you get a "still being prepared" message; run it again a few seconds later.
- The prepared data is kept in the `cache` folder inside the plugin folder. It refreshes itself when the map file or the `showclips_types` list changes, and the folder can be deleted at any time.
- Adding a type that covers very large areas (`blockbullets` for example) makes the first load of big maps longer.
- Trigger volumes are drawn as the box around the trigger, so on triggers with an unusual shape the box can look bigger than the trigger itself.
- If the lines sit exactly on a wall and look broken, raise `showclips_offset` (`2` for example) or make `showclips_width` thicker.
- Command name changes (`showclips_cmd`) take effect when the server/plugin is restarted.
