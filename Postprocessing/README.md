# Postprocessing

*Read this in [Turkish / Türkçe](README.tr.md).*

Applies a personal post processing effect (bloom, blur, color correction, exposure) to players. The effect is only visible on that player's own screen; other players see no change.

## Features

- **Every `.vpost` file in the game** ships ready to use: 105 unique files + one FOV (zoom) effect = 106 effects
- Effects work per player; different players can have different effects at the same time
- Categorized WASD menu (Effects / Color / General / UI / Maps / MapSpecific), or pick directly with `css_pp <effect>`
- An effect can be locked behind specific permissions with the `flag` field
- Unlimited effects can be added from the config; every effect has its own `.vpost` file, exposure settings and an optional FOV value
- Admins can give effects to other players with `css_givepp`
- The effect preference is saved per SteamID and restored when the player reconnects
- The map's own post processing volumes are hidden from the player while an effect is active (no conflict)
- Effects with a defined FOV value can also be used as a zoom effect
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `Postprocessing` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/Postprocessing/
   ```
2. Restart the server or run `css_plugins load Postprocessing`.
3. The config file is created automatically on first load.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_pp` / `css_postprocessing` | Opens the categorized effect menu | `pp_flag` |
| `css_pp <effect>` | Applies the effect directly | `pp_flag` + the effect's `flag` value |
| `css_pp off` | Turns the effect off | `pp_flag` |
| `css_givepp <player> <effect\|off>` | Gives an effect to the target player or turns it off | `pp_give_flag` |

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/Postprocessing/Postprocessing.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `pp_cmd` | string | `"css_pp,css_postprocessing"` | Comma separated menu commands |
| `pp_flag` | string | `""` | Permission for the menu command (empty = everyone) |
| `pp_give_cmd` | string | `"css_givepp"` | Comma separated admin commands |
| `pp_give_flag` | string | `"@css/generic"` | Permission for the admin command |
| `pp_remember` | bool | `true` | Saves the player's effect preference per SteamID |
| `pp_hide_map_effects` | bool | `true` | Hides the map's own post processing effect while an effect is active |
| `pp_presets` | list | 106 effects | Effect definitions |

### Effect Fields

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `name` | string | – | Effect name shown in the menu and typed in the command |
| `file` | string | – | `.vpost` file path (if left empty only the FOV is applied) |
| `category` | string | `""` | Category name in the menu (empty = the "Other" category) |
| `flag` | string | `""` | Permission required for the effect (empty = everyone) |
| `fade` | float | `0.25` | Transition time into the effect (seconds) |
| `exposure` | bool | `true` | Whether exposure control is on |
| `min_exposure` | float | `0.5` | Minimum exposure |
| `max_exposure` | float | `2.0` | Maximum exposure |
| `exposure_speed_up` | float | `1.0` | Exposure increase speed |
| `exposure_speed_down` | float | `1.0` | Exposure decrease speed |
| `fov` | int | `0` | FOV applied together with the effect (0 = do not change, 40 = zoom) |

### Example Config

```json
{
  "pp_cmd": "css_pp,css_postprocessing",
  "pp_flag": "",
  "pp_give_cmd": "css_givepp",
  "pp_give_flag": "@css/generic",
  "pp_remember": true,
  "pp_hide_map_effects": true,
  "pp_presets": [
    {
      "name": "bloomtest",
      "file": "lighting/postprocessing/correction/bloomtest.vpost",
      "category": "Renk",
      "flag": "",
      "fade": 0.25,
      "exposure": true,
      "min_exposure": 0.5,
      "max_exposure": 2.0,
      "exposure_speed_up": 1.0,
      "exposure_speed_down": 1.0,
      "fov": 0
    },
    {
      "name": "zoom",
      "file": "",
      "category": "Genel",
      "flag": "",
      "fade": 0.25,
      "exposure": true,
      "min_exposure": 0.5,
      "max_exposure": 2.0,
      "exposure_speed_up": 1.0,
      "exposure_speed_down": 1.0,
      "fov": 40
    },
    {
      "name": "de_fachwerk3_drunk",
      "file": "postprocess/de_fachwerk3_drunk.vpost",
      "category": "HaritaOzel",
      "flag": "",
      "fade": 0.25,
      "exposure": true,
      "min_exposure": 0.5,
      "max_exposure": 2.0,
      "exposure_speed_up": 1.0,
      "exposure_speed_down": 1.0,
      "fov": 0
    }
  ]
}
```

## Default Effects

Effect names are the `.vpost` file names themselves, so `css_pp de_fachwerk3_drunk` works directly.

| Category | Count | Contents |
| --- | --- | --- |
| `Efektler` (Effects) | 11 | `lighting/postprocessing/effects/` — death camera, scope, bomb end, buy blur, heavy armor, HLTV replay |
| `Renk` (Color) | 3 | `lighting/postprocessing/correction/` — `bloomtest`, `cc_freeze_ct`, `cc_freeze_t` |
| `Genel` (General) | 15 | Root `lighting/postprocessing/` and `postprocess/` files — `ar_dizzy`, `filmic_default`, `basepostprocess`, `inspect_laptop`, `graphics_settings` and the FOV effect `zoom` |
| `Arayuz` (UI) | 4 | `lighting/postprocessing/ui/` — inventory/case icon effects |
| `Haritalar` (Maps) | 49 | Official map prefabs (`de_dust2_prefab`, `de_train_postprocess_v2`, `de_mirage_vanity` …) |
| `HaritaOzel` (MapSpecific) | 24 | Effects specific to individual maps |

`HaritaOzel` effects belong to individual maps rather than the game as a whole. They appear in the menu on every map but only work while the map they belong to is loaded:

| Source map | Effects |
| --- | --- |
| `de_fachwerk` | `de_fachwerk`, `de_fachwerk2`, `de_fachwerk3`, `de_fachwerk3_drunk`, `de_fachwerk4`, `de_fachwerk5`, `drawbridge` |
| `de_boulder` | `de_boulder_postprocess`, `de_boulder_postprocess2`, `de_boulder_postprocess3`, `de_boulder_prefab`, `de_boulder_skybox`, `bldr_01_ct_spawn`, `bldr_04_b_site`, `de_inferno_postprocess_boulder` |
| `ar_pool_day` | `ar_pool_day`, `postprocess_filmic_pool_day`, `postprocess_filmic_pool_day_cs16`, `postprocess_filmic_underwater` |
| `de_eldorado` | `eldorado`, `eldorado_postprocess` |
| `de_poseidon` | `poseidon` |
| `de_debris` | `de_debris` |
| `ar_pool_day`, `cs_shelter`, `de_fachwerk`, `de_poseidon` | `basic_linear_post` (the same file in all four) |

## Notes

- The effect does not depend on where the player is standing; it applies everywhere on the map.
- `HaritaOzel` effects only work on their own maps. If selected on another map nothing changes on screen and the plugin keeps working normally.
- The effect is removed when the player dies and comes back automatically when they respawn. While dead, the spectated player's effect is not visible to you.
- On scoped weapons the game's own zoom overrides the `fov` setting.
- Effect names are case insensitive in commands (`css_pp bloomtest` = `css_pp BloomTest`).
- The menu has two levels: category first, then the effect list. Inside a category `R` (Back) returns to the upper menu.
- When the effect list is changed in the config, the existing `Postprocessing.json` file is **not updated automatically**. To get the new default list, delete the config file and restart the server.
- Command name changes (`pp_cmd`, `pp_give_cmd`) take effect when the server/plugin is restarted.
