# FPS

*Read this in [Turkish / Türkçe](README.tr.md).*

Gives players an FPS mode that removes things they do not need to see: players hidden behind walls, corpses, their own legs, blood, bullet holes and small junk props on the map. Everything is on by default. With `!fps` each player either changes their own settings from a menu or turns FPS mode on/off, depending on the server setting. The choices are saved and applied again when they rejoin.

## Features

- `css_fps` (`!fps` in chat) is always available: it opens a WASD settings menu, or turns FPS mode on/off when `player_config` is `false`
- Player choices are saved to `players.json` by SteamID and applied on every join
- **Behind walls:** players out of your line of sight are not sent to you. Choose everyone, teammates only or enemies only
- **Behind walls sound:** gunshots and weapon sounds of the players hidden from you are muted. Choose everyone, teammates only or enemies only
- Hidden teammates are not shown through walls (no model, name or outline), but they and the enemies your team has spotted keep moving on the radar as usual
- **Killfeed:** only your own kills (and your own death) appear in the killfeed
- **Corpses:** dead players' bodies disappear after a short delay
- **Legs:** you do not see your own legs when you look down
- **Blood and bullet holes:** blood and hit effects on players are not shown; blood stains and bullet holes are wiped at a set interval
- **Junk props:** small throwable props such as bottles, cans and pots are not shown. The list is kept per map in the `maps` folder
- Corpses, blood and junk props can be forced on for everyone: props are deleted from the map, corpses and blood are removed for every player
- Each feature can be turned off in `settings.json`. A feature that is off does no work at all
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.375

## Installation

1. Copy the compiled `FPS` folder, together with its `maps` folder, to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/FPS/
   ```
2. Restart the server or run `css_plugins load FPS`.
3. `settings.json` is created in the plugin folder when it is missing. If the file cannot be read, it is kept as `settings.old.json` and written again with the default settings.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_fps` | `player_config: true`: opens the FPS settings menu. `player_config: false`: turns FPS mode on/off. The choice is saved | `fps_flag` (default: everyone) |

In the menu, **W/S** scrolls, **E** changes the selected setting and **R** closes the menu.

## Configuration

```
csgo/addons/counterstrikesharp/plugins/FPS/settings.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `fps_cmd` | string | `"css_fps"` | Comma separated command names |
| `fps_flag` | string | `""` | Required permission; empty string = everyone can use it |
| `player_config` | bool | `true` | `true`: players change each setting themselves from the menu, `false`: players can only turn FPS mode on/off and get the settings below |
| `hide_unseen` | int | `3` | Default for hiding players behind walls. `0`: off (not in the menu), `1`: teammates, `2`: enemy team, `3`: everyone |
| `mute_unseen` | int | `3` | Default for muting the weapons of hidden players. `0`: off (not in the menu), `1`: teammates, `2`: enemy team, `3`: everyone. Has no effect while `hide_unseen` is `0` |
| `own_killfeed` | bool | `true` | `true`: on by default, can be changed from the menu, `false`: off, not in the menu |
| `hide_corpses` | int | `1` | `0`: off, `1`: on by default and can be changed from the menu, `2`: hidden for every player, not in the menu |
| `corpse_delay` | float | `0.5` | Seconds after death before the corpse disappears |
| `hide_legs` | bool | `true` | `true`: on by default, can be changed from the menu, `false`: off, not in the menu |
| `hide_blood` | int | `1` | `0`: off, `1`: on by default and can be changed from the menu, `2`: applied to every player, not in the menu |
| `blood_delay` | float | `0.5` | Interval in seconds at which blood stains and bullet holes are wiped (minimum `0.1`) |
| `hide_props` | int | `1` | `0`: off, `1`: on by default and can be changed from the menu, `2`: junk props are deleted from the map for everyone, not in the menu |

### Example Config

```json
{
  "fps_cmd": "css_fps,css_fpsboost",
  "fps_flag": "",
  "player_config": true,
  "hide_unseen": 2,
  "mute_unseen": 0,
  "own_killfeed": true,
  "hide_corpses": 1,
  "corpse_delay": 0.5,
  "hide_legs": true,
  "hide_blood": 2,
  "blood_delay": 0.5,
  "hide_props": 2
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
    "hide_props": 0
  },
  "76561198111111111": {
    "fps": 0
  }
}
```

Only the settings a player changed from the server default are stored; `0` means that feature is off for them. `"fps": 0` means the player turned FPS mode off while `player_config` was `false`. A player who puts everything back to the defaults is removed from the file. It can also be edited by hand; the change is read when the plugin is reloaded.

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

## Notes

- **Behind walls** only works while you are alive. While dead or spectating, every player stays visible.
- A hidden player appears as soon as there is a line of sight, including when they are about to peek. Players with higher ping see them earlier, and a player who was visible is not hidden right away. Collision, bullets and damage are not affected.
- Players who are very close to you are never hidden.
- When `mp_teammates_are_enemies` is `1` (e.g. deathmatch), everyone counts as an enemy for the team/enemy choices.
- While you spectate a player, their corpse is not hidden, so the camera does not get stuck.
- **Behind walls sound** only mutes players that are hidden from you at that moment; footsteps and other sounds are not affected.
- **Blood and bullet holes** only wipes marks left during play; decals that are part of the map stay.
- With `hide_props: 2` the props are deleted every round and nobody can pick them up or kick them.
- With `hide_corpses: 2` GOTV does not see corpses either.
- **Legs** makes your own model slightly transparent, which other players cannot notice. Another plugin that changes player transparency can turn your legs back on.
- **Killfeed:** GOTV still receives the full killfeed. If another plugin (e.g. DM) also changes the killfeed, kills can show up twice; set `own_killfeed: false` on that server.
- Changes to `settings.json` and command names take effect when the server/plugin is restarted.
