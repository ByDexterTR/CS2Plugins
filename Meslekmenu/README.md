# Meslekmenu (Job Menu)

*Read this in [Turkish / Türkçe](README.tr.md).*

Lets T players pick a "job" once per round. Every job gives a different advantage. Designed for Jailbreak servers.

## Features

- 5 jobs: **Doctor**, **Flash**, **Bomber**, **Rambo**, **Zeus**
- Every job can be enabled/disabled and customized separately from the config
- **One pick per round** (reset at round start)
- Only living T players can use it
- Used without arguments it shows the list of active jobs and their details
- Server cvars related to Doctor/Zeus are set automatically at round start
- Turkish / English language support (`lang/`)

## Jobs

| Job | Advantage |
| --- | --- |
| `doktor` | Gives a healthshot (heal amount from the config, via the `healthshot_health` cvar) |
| `flash` | Speed multiplier for a set duration (default 3x, 5 s) |
| `bombaci` | Gives a random grenade (smoke / HE / flash / molotov — which ones are allowed comes from the config) |
| `rambo` | High HP + armor (+ optional helmet); with `rambo_fix` on, players below 100 HP cannot pick it |
| `zeus` | Gives a taser (recharge time and drop behavior via cvars) |

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `Meslekmenu` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/Meslekmenu/
   ```
2. Restart the server or run `css_plugins load Meslekmenu`.
3. The config file is created automatically on first load.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_meslek` | Lists the active jobs and their details | — (everyone) |
| `css_meslek <job>` | Picks the given job | — (living T, once per round) |

Accepted job names: `doktor`/`doctor`, `flash`, `bombaci`/`bombacı`/`bomber`, `rambo`, `zeus`

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/Meslekmenu/Meslekmenu.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `doktor_enabled` | bool | `true` | Whether the Doctor job is active |
| `doktor_regen` | int | `50` | Healthshot heal amount (`healthshot_health`) |
| `doktor_drop_healthshot` | bool | `true` | Whether the healthshot drops on death (`mp_death_drop_healthshot`) |
| `flash_enabled` | bool | `true` | Whether the Flash job is active |
| `flash_speed` | float | `3.0` | Speed multiplier |
| `flash_duration` | int | `5` | Speed duration (seconds) |
| `bombaci_enabled` | bool | `true` | Whether the Bomber job is active |
| `bombaci_give_smoke` | bool | `true` | Whether a smoke can be given |
| `bombaci_give_grenade` | bool | `true` | Whether an HE can be given |
| `bombaci_give_flashbang` | bool | `true` | Whether a flashbang can be given |
| `bombaci_give_molotov` | bool | `true` | Whether a molotov can be given |
| `rambo_enabled` | bool | `true` | Whether the Rambo job is active |
| `rambo_hp` | int | `150` | Rambo HP value |
| `rambo_armor` | int | `100` | Rambo armor value |
| `rambo_helmet` | bool | `true` | Whether a helmet is given |
| `rambo_fix` | bool | `true` | Stop players below 100 HP from picking Rambo |
| `zeus_enabled` | bool | `true` | Whether the Zeus job is active |
| `zeus_recharge_taser` | int | `30` | Taser recharge time (`mp_taser_recharge_time`) |
| `zeus_drop_taser` | bool | `true` | Whether the taser drops on death (`mp_death_drop_taser`) |
| `meslek_cmd` | string | `"css_meslekmenu,css_meslek,css_job,css_jobmenu"` | Commands that open the menu, separated by commas |

### Example Config

```json
{
  "doktor_enabled": true,
  "doktor_regen": 50,
  "doktor_drop_healthshot": true,
  "flash_enabled": true,
  "flash_speed": 3.0,
  "flash_duration": 5,
  "bombaci_enabled": true,
  "bombaci_give_smoke": true,
  "bombaci_give_grenade": true,
  "bombaci_give_flashbang": true,
  "bombaci_give_molotov": true,
  "rambo_enabled": true,
  "rambo_hp": 150,
  "rambo_armor": 100,
  "rambo_helmet": true,
  "rambo_fix": true,
  "zeus_enabled": true,
  "zeus_recharge_taser": 30,
  "zeus_drop_taser": true
}
```

## Usage Example

```
!meslek           → list of active jobs
!meslek rambo     → 150 HP + 100 armor + helmet
!meslek bombaci   → a random grenade
```
