# HideTeammates

*Read this in [Turkish / Türkçe](README.tr.md).*

Lets players hide other player models with a command. Hidden players are never sent to the client (`CheckTransmit`), and optionally the sounds they make are muted too. Preferences are written to JSON per SteamID; hiding is enabled automatically when a saved player joins the server.

## Features

- Toggle with `css_hide` / `css_gizle`; command names can be changed from the config
- Permission (flag) check — leave it empty and everyone can use it
- 3 hiding modes: teammates, the enemy team or everyone (`mode_hide`)
- The hidden player's model and the weapons in their hands are not sent to the client
- With `disable_sound` the hidden players' footsteps, body sounds, knife and weapon sounds are muted too (257 sound hashes)
- Preferences are stored in `players.json` as a SteamID array; applied automatically on join
- Hiding is not applied while dead/spectating, so the spectated player's view is not broken
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `HideTeammates` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/HideTeammates/
   ```
2. Restart the server or run `css_plugins load HideTeammates`.
3. The config file is created automatically on first load.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_hide` / `css_gizle` | Toggles hiding; the preference is saved | `flag_hide` (default: everyone) |

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/HideTeammates/HideTeammates.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `cmd_hide` | string | `"css_hide,css_gizle"` | Comma separated command names |
| `flag_hide` | string | `""` | Required permission; empty string = everyone can use it |
| `mode_hide` | int | `1` | `1`: teammates, `2`: enemy team, `3`: everyone |
| `disable_sound` | int | `1` | `0`: sounds are heard, `1`: hidden players' sounds are muted too |

### Example Config

```json
{
  "cmd_hide": "css_hide,css_gizle",
  "flag_hide": "",
  "mode_hide": 1,
  "disable_sound": 1
}
```

## Preference File

```
csgo/addons/counterstrikesharp/plugins/HideTeammates/players.json
```

```json
[
  "76561198000000000",
  "76561198111111111"
]
```

The SteamIDs in the file are the players who have hiding enabled; the file is updated as they toggle it with the command. It can also be edited by hand, and the change is read when the plugin is reloaded.

## Notes

- Hiding is only applied **while alive**; while dead/spectating every player stays visible, so the spectated player's view is not broken.
- Hidden players only become invisible; collision and bullet blocking still apply.
- Sound muting is done through the `208` (soundevent), `369` (weapon sound) and `452` (weapon event) user messages; the footstep/body sound/knife hash list is a combination of the Sesler, VIPCore `Silent` and jRandomSkills sources.
- The `disable_sound` value takes effect when the server/plugin is restarted (hooks are bound at load).
- Command name changes (`cmd_hide`) take effect when the server/plugin is restarted.
- For players who want to set sounds by category themselves, see the [Sesler](../Sesler) plugin.
