# Redbull

*Read this in [Turkish / Türkçe](README.tr.md).*

Gives the player who runs the command a short speed boost ("Redbull gives you wings"). Duration, speed, team restriction, round limit and cooldown are managed from the config.

## Features

- Adjustable speed multiplier and duration
- While the effect is active the player is colored with the config color; it returns to normal when the duration ends
- Team filter: T only, CT only or everyone
- Usage limit per round
- Cooldown between uses
- All limits/cooldowns/effects are reset at round start
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `Redbull` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/Redbull/
   ```
2. Restart the server or run `css_plugins load Redbull`.
3. The config file is created automatically on first load.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_redbull` | Starts the speed effect | — (depends on the team filter and being alive) |

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/Redbull/Redbull.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `speed` | float | `2.0` | Speed multiplier (1.0 = normal) |
| `duration` | int | `10` | Effect duration (seconds) |
| `filter_team` | string | `"T"` | `"T"`, `"CT"` or `"Both"` |
| `player_color` | int[3] | `[248, 123, 27]` | Player color while the effect is active (RGB) |
| `round_limiter` | int | `2` | Usage limit per round (`0` = unlimited) |
| `cooldown` | int | `15` | Wait between uses (seconds, `0` = none) |

### Example Config

```json
{
  "speed": 2.0,
  "duration": 10,
  "filter_team": "T",
  "player_color": [248, 123, 27],
  "round_limiter": 2,
  "cooldown": 15
}
```

## Usage Example

```
!redbull → 2x speed + orange color for 10 seconds
```

## Notes

- If another plugin lowers the speed while the effect is running, Redbull reapplies it (`VelocityModifier` is checked every tick).
- The effect only works on living players; if the player dies the effect ends by itself.
