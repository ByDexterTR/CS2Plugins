# FPS

*Read this in [Turkish / Türkçe](README.tr.md).*

Gives players an FPS mode that removes things they do not need to see: players hidden behind walls, corpses, their own legs, blood, bullet holes and small junk props on the map. Everything is on by default. With `!fps` each player either changes their own settings from a menu or turns FPS mode on/off, depending on the server setting. The choices are saved and applied again when they rejoin.

## Features

- `css_fps` (`!fps` in chat) is always available: it opens a WASD settings menu, or turns FPS mode on/off when `player_configable` is `false`
- Player choices are saved to `players.json` by SteamID and applied on every join
- **Behind walls:** players out of your line of sight are not sent to you. Choose everyone, teammates only or enemies only
- **Behind walls sound:** gunshots and weapon sounds of the players hidden from you are muted. Choose everyone, teammates only or enemies only
- Hidden teammates are not shown through walls (no model, name or outline), but they and the enemies your team has spotted keep moving on the radar as usual
- **Killfeed:** only your own kills (and your own death) appear in the killfeed
- **Corpses:** dead players' bodies disappear after a short delay
- **Legs:** you do not see your own legs when you look down
- **Blood:** blood and hit effects on players are not shown
- **Bullet holes:** bullet holes and stains on walls are wiped a set time after they appear
- **Junk props:** small throwable props such as bottles, cans and pots are not shown. The list is kept per map in the `maps` folder
- Each feature can be turned off in `settings.json`, and its default for new players can be set. A feature that is off does no work at all
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)

## Installation

1. Copy the compiled `FPS` folder, together with its `maps` folder, to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/FPS/
   ```
2. Restart the server or run `css_plugins load FPS`.
3. `settings.json` is created in the plugin folder when it is missing. If the file cannot be read, it is kept as `settings.old.json` and written again with the default settings.
4. A `settings.json` from an older version is converted to the new layout automatically, keeping your choices. The old file is kept as `settings.v1.json`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_fps` | `player_configable: true`: opens the FPS settings menu. `player_configable: false`: turns FPS mode on/off. The choice is saved | `fps_flag` (default: everyone) |

In the menu, **W/S** scrolls, **E** changes the selected setting and **R** closes the menu.

## Configuration

```
csgo/addons/counterstrikesharp/plugins/FPS/settings.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `ConfigVersion` | int | `2` | Settings layout version, do not change |
| `fps_cmd` | string | `"css_fps"` | Comma separated command names |
| `fps_flag` | string | `""` | Required permission; empty string = everyone can use it |
| `player_configable` | bool | `true` | `true`: players change each setting themselves from the menu, `false`: the server settings are fixed and players can only turn FPS mode on/off |
| `hide_unseen_enable` | bool | `true` | Hiding players behind walls is used on the server |
| `hide_unseen_default` | int | `3` | Default for hiding players behind walls. `0`: off, `1`: teammates, `2`: enemy team, `3`: everyone |
| `mute_unseen_enable` | bool | `true` | Muting the weapons of hidden players is used on the server. Has no effect while `hide_unseen_enable` is `false` |
| `mute_unseen_default` | int | `3` | Default for muting hidden players. `0`: off, `1`: teammates, `2`: enemy team, `3`: everyone |
| `own_killfeed_enable` | bool | `true` | Showing only your own kills in the killfeed is used on the server |
| `own_killfeed_default` | int | `1` | `0`: off by default, `1`: on by default |
| `hide_ragdoll_enable` | bool | `true` | Hiding corpses is used on the server |
| `hide_ragdoll_default` | int | `1` | `0`: off by default, `1`: on by default |
| `hide_ragdoll_delay` | float | `0.5` | Seconds after death before the corpse disappears |
| `hide_legs_enable` | bool | `true` | Hiding your own legs is used on the server |
| `hide_legs_default` | int | `1` | `0`: off by default, `1`: on by default |
| `hide_blood_enable` | bool | `true` | Hiding blood is used on the server |
| `hide_blood_default` | int | `1` | `0`: off by default, `1`: on by default |
| `hide_bullethole_enable` | bool | `true` | Wiping bullet holes is used on the server |
| `hide_bullethole_default` | int | `1` | `0`: off by default, `1`: on by default |
| `hide_bullethole_delay` | float | `1.0` | Seconds after which bullet holes are wiped (minimum `0.1`) |
| `hide_props_enable` | bool | `true` | Hiding junk props is used on the server |
| `hide_props_default` | int | `1` | `0`: off by default, `1`: on by default |

A setting whose `_enable` is `false` is not in the menu and does no work. With `player_configable: false` every player gets the `_default` values and can only turn FPS mode on/off.

### Example Config

```json
{
  "ConfigVersion": 2,
  "fps_cmd": "css_fps,css_fpsboost",
  "fps_flag": "",
  "player_configable": true,
  "hide_unseen_enable": true,
  "hide_unseen_default": 2,
  "mute_unseen_enable": false,
  "mute_unseen_default": 3,
  "own_killfeed_enable": true,
  "own_killfeed_default": 0,
  "hide_ragdoll_enable": true,
  "hide_ragdoll_default": 1,
  "hide_ragdoll_delay": 1.0,
  "hide_legs_enable": true,
  "hide_legs_default": 1,
  "hide_blood_enable": true,
  "hide_blood_default": 0,
  "hide_bullethole_enable": true,
  "hide_bullethole_default": 1,
  "hide_bullethole_delay": 2.0,
  "hide_props_enable": true,
  "hide_props_default": 1
}
```

## Preference File

```
csgo/addons/counterstrikesharp/plugins/FPS/players.json
```

```json
{
  "76561198000000000": {
    "hide_unseen": 2,
    "hide_props": 0,
    "hide_blood": 1
  },
  "76561198111111111": {
    "fps": 0
  }
}
```

Only the settings a player changed from the server default are stored; `0` means that feature is off for them and `1` means it is on. `"fps": 0` means the player turned FPS mode off while `player_configable` was `false`. A player who puts everything back to the defaults is removed from the file. It can also be edited by hand; the change is read when the plugin is reloaded.

## Junk Prop Lists

```
csgo/addons/counterstrikesharp/plugins/FPS/maps/<map>.json
```

```json
[
  {
    "model": "models/props/de_inferno/claypot03.vmdl",
    "count": 17
  },
  {
    "model": "models/props_junk/garbage_sodacup01a.vmdl",
    "count": 7
  }
]
```

Ready lists come for de_ancient, de_ancient_night, de_anubis, de_cache, de_dust2, de_inferno, de_mirage, de_nuke, de_overpass, de_train and de_vertigo. `model` is the prop model to hide and `count` is how many of it the map has.

- On a map without a list, the plugin builds the list on the first round and saves it to this folder.
- Remove a line to keep that prop visible. An empty list (`[]`) hides nothing on that map.
- Only small props that players walk through and kick are hidden. A prop that blocks players is never hidden, even if it is in the list.
- The list is checked against the map when the map loads. After a CS2 update, a model that is gone or that now blocks players, a changed count, or a new junk prop that is not in the list is reported as a warning in the server console. Delete the map's file to build the list again.

## Visibility Data

```
csgo/addons/counterstrikesharp/plugins/FPS/maps/<map>.vis
```

**Behind walls** uses a visibility table per map, built from the map's own geometry. Most hidden players are decided from this table without extra line of sight checks, so the server does very little work even when every player uses the feature.

- Ready tables come for the same maps as the junk prop lists.
- On a map without a table (workshop maps included), the table is built in the background when the map loads and saved to this folder. It takes from a few seconds to about half a minute depending on the map and uses half of the CPU cores at low priority. Until it is ready, every player is checked with line of sight traces.
- A workshop map's table is saved as `<map>.<workshop id>.vis`, so workshop maps with the same name never share a table. When several map files carry the loaded map's name, the plugin compares each with the running map and uses the one that matches.
- After a CS2 update that changes a map, the table no longer matches the map and is built again automatically.
- Delete a map's `.vis` file to build it again.

## Notes

- **Behind walls** only works while you are alive. While dead or spectating, every player stays visible.
- A hidden player appears as soon as there is a line of sight, including when they are about to peek. Players with higher ping see them earlier, and a player who was visible is not hidden right away. Collision, bullets and damage are not affected.
- Players who are very close to you are never hidden.
- When `mp_teammates_are_enemies` is `1` (e.g. deathmatch), everyone counts as an enemy for the team/enemy choices.
- While you spectate a player, their corpse is not hidden, so the camera does not get stuck.
- **Behind walls sound** only mutes players that are hidden from you at that moment; footsteps and other sounds are not affected.
- **Bullet holes** only wipes marks left during play; decals that are part of the map stay. Blood stains on walls are wiped together with bullet holes.
- **Legs** makes your own model slightly transparent, which other players cannot notice. Another plugin that changes player transparency can turn your legs back on.
- **Killfeed:** GOTV still receives the full killfeed. If another plugin (e.g. DM) also changes the killfeed, kills can show up twice; set `own_killfeed_enable: false` on that server.
- Changes to `settings.json` and command names take effect when the server/plugin is restarted.
