# CTPerk

*Read this in [Turkish / Türkçe](README.tr.md).*

Lets the CT team pick round based perks. Designed to balance CTs against the T count in Jailbreak.

## Features

- 5 different perks:
  - **HP + Armor** — high HP and armor (+ helmet) for every living CT
  - **Lifesteal** — a percentage of the damage a CT deals comes back as health
  - **Infinite Ammo** — the magazine refills automatically when it drops to half
  - **Damage Reduction** — a percentage of the damage a CT takes is restored
  - **Damage Boost** — the damage CTs deal to Ts is increased by a multiplier
- **Selection budget based on the T count** at round start (e.g. 9+ Ts → 2 picks, 20+ Ts → 3 picks)
- Every perk can be enabled/disabled separately from the config and the ratios customized
- Perk selections are announced to every CT; selected ones are marked with a green ✔ in the menu
- Perks are reset at round start and the CTs' extra HP/armor is pulled back to normal
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `CTPerk` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/CTPerk/
   ```
2. Restart the server or run `css_plugins load CTPerk`.
3. The config file is created automatically on first load.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_ctperk` | Opens the perk selection menu | `@css/generic` **or** `@jailbreak/warden` |

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/CTPerk/CTPerk.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `perk_hparmor_hp` | int | `200` | HP given by the HP+Armor perk |
| `perk_hparmor_armor` | int | `100` | Armor given by the HP+Armor perk |
| `perk_lifesteal_ratio` | float | `0.25` | Lifesteal ratio (0.25 = 25%) |
| `perk_damagereducation_ratio` | float | `0.25` | Damage reduction ratio |
| `perk_damageboost_ratio` | float | `1.50` | Damage multiplier (1.5 = +50%) |
| `enabled_perk_hparmor` | bool | `true` | Whether the HP+Armor perk is active |
| `enabled_perk_lifesteal` | bool | `true` | Whether the lifesteal perk is active |
| `enabled_perk_infammo` | bool | `true` | Whether the infinite ammo perk is active |
| `enabled_perk_damagereducation` | bool | `true` | Whether the damage reduction perk is active |
| `enabled_perk_damageboost` | bool | `true` | Whether the damage boost perk is active |
| `selection_rights` | list | below | Perk selection budget by T count |
| `ctperk_cmd` | string | `"css_ctperk,css_ctp"` | Commands that open the menu, separated by commas |
| `ctperk_flag` | string | `"@jailbreak/warden,@css/generic"` | Flags allowed to use it, separated by commas |

`selection_rights` — the T count threshold (`t_count`) and the budget granted at that threshold (`hak`); the highest matching threshold applies:

```json
"selection_rights": [
  { "t_count": 0,  "hak": 1 },
  { "t_count": 9,  "hak": 2 },
  { "t_count": 20, "hak": 3 }
]
```

### Example Config

```json
{
  "perk_hparmor_hp": 200,
  "perk_hparmor_armor": 100,
  "perk_lifesteal_ratio": 0.25,
  "perk_damagereducation_ratio": 0.25,
  "perk_damageboost_ratio": 1.5,
  "enabled_perk_hparmor": true,
  "enabled_perk_lifesteal": true,
  "enabled_perk_infammo": true,
  "enabled_perk_damagereducation": true,
  "enabled_perk_damageboost": true,
  "selection_rights": [
    { "t_count": 0, "hak": 1 },
    { "t_count": 9, "hak": 2 },
    { "t_count": 20, "hak": 3 }
  ]
}
```

## Usage Example

1. The round starts → CTs are told "You have 2 perk picks this round".
2. The warden types `!ctperk` and picks "Lifesteal (25%)" from the menu.
3. The menu reopens until the budget runs out; every selection is announced to the CTs.

## Notes

- Perks work **team-wide**, not per player.
- The damage reduction perk does not cut the damage; it instantly restores part of the damage taken.
- Infinite ammo does not apply to weapons like the knife or taser.
