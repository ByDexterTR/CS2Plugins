# CTKit

*Read this in [Turkish / Türkçe](README.tr.md).*

Lets CT players pick from a menu which primary and secondary weapon they get automatically on every spawn. This is the Jailbreak guard kit system.

## Features

- Primary / secondary weapon selection through a CenterHtml menu
- Selections are remembered per player (until the player disconnects)
- Players who have not chosen get the default weapons from the config
- On spawn every weapon except the knife is cleared and the kit is given
- Weapon lists are fully customizable through the config
- "Reset kit" option in the menu
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `CTKit` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/CTKit/
   ```
2. Restart the server or run `css_plugins load CTKit`.
3. The config file is created automatically on first load.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_kit` | Opens the weapon kit menu | — (CT team only) |

## Configuration

The config file is created automatically on first load:

```
csgo/addons/counterstrikesharp/configs/plugins/CTKit/CTKit.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `default_primary_weapon` | string | `weapon_ak47` | Primary weapon given when nothing is selected |
| `default_secondary_weapon` | string | `weapon_deagle` | Secondary weapon given when nothing is selected |
| `primary_weapons` | list | AK47, M4A4, M4A1-S, AWP, MAG7 | Primary weapons offered in the menu |
| `secondary_weapons` | list | Deagle, CZ75A, Tec9, Dual Berettas, USP-S, Glock, Revolver | Secondary weapons offered in the menu |

Every weapon entry has two fields:

| Field | Description |
| --- | --- |
| `weapon_name` | In-game entity name (with the `weapon_` prefix) |
| `display_name` | Name shown in the menu |

### Example Config

```json
{
  "default_primary_weapon": "weapon_ak47",
  "default_secondary_weapon": "weapon_deagle",
  "primary_weapons": [
    { "weapon_name": "weapon_ak47", "display_name": "AK47" },
    { "weapon_name": "weapon_m4a4", "display_name": "M4A4" },
    { "weapon_name": "weapon_awp", "display_name": "AWP" }
  ],
  "secondary_weapons": [
    { "weapon_name": "weapon_deagle", "display_name": "DEAGLE" },
    { "weapon_name": "weapon_usp_silencer", "display_name": "USP-S" }
  ]
}
```

## Usage Example

1. A CT player types `!kit` → the menu opens (current selections are shown in the title).
2. "Primary Weapon" → they pick AWP from the list.
3. On their next spawn they automatically get the AWP + the selected pistol.

## Notes

- The kit is only applied to the **CT team** and only at spawn; T players are not affected.
- The icon in the menu title is loaded from [`img/pistol.png`](../img/pistol.png) in this repository.
