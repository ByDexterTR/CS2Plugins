# CTRev

*Read this in [Turkish / Türkçe](README.tr.md).*

Revives (respawns) dead CT players from a menu or automatically. Works with a limited per-round budget to keep the guard balance in Jailbreak.

## Features

- An on-screen menu listing dead CTs — revivable players are shown in green, players still on cooldown in gray
- **Cooldown** after death — a player cannot be revived before it expires
- **Limited revive budget** per round; refilled automatically at the start of each round
- **Automatic revive mode** — when enabled, CTs whose cooldown has expired respawn by themselves until the budget runs out
- Command to reset the budget mid-round
- The menu refreshes once per second while open (remaining times update live)
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `CTRev` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/CTRev/
   ```
2. Restart the server or run `css_plugins load CTRev`.
3. The config file is created automatically on first load.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_ctr` / `css_ctrev` / `css_ctrevmenu` | Opens the revive menu | `@css/generic` **or** `@jailbreak/warden` |
| `css_hak0` / `css_haksifir` / `css_haksifirla` | Resets (refills) the revive budget | `@css/generic` |

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/CTRev/CTRev.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `cooldown` | int | `15` | Time to wait after death before a player can be revived (seconds) |
| `revive_count` | int | `3` | Total revive budget per round |
| `ctrev_cmd` | string | `"css_ctrev,css_ctr,css_ctrevmenu"` | Commands that open the menu, separated by commas |
| `ctrev_flag` | string | `"@jailbreak/warden,@css/generic"` | Flags allowed to use the menu |
| `haksifirla_cmd` | string | `"css_hak0,css_haksifir,css_hakreset"` | Commands that reset the revive budget |
| `hak_flag` | string | `"@jailbreak/warden,@css/generic"` | Flags allowed to reset the budget |

### Example Config

```json
{
  "cooldown": 15,
  "revive_count": 3
}
```

## Usage Example

1. The warden types `!ctr` → the menu shows the remaining budget and the list of dead CTs.
2. They click a player shown in green → the player is revived and the remaining budget is announced to the whole server.
3. If they want, they enable the "Automatic Revive: Off" option → CTs whose cooldown expires respawn automatically until the budget runs out.

## Notes

- The revive budget is **team-wide**, not per player.
- When the budget runs out no revives can happen from the menu or automatic mode; it can be refilled with `!hak0`.
