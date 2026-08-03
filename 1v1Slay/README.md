# 1v1Slay

*Read this in [Turkish / Türkçe](README.tr.md).*

Starts an automatic countdown when only **1 living player** is left on each team and slays the remaining players when time runs out. Stops the round from stalling in a 1v1 situation.

## Features

- Detects a 1 T vs 1 CT situation automatically and starts the countdown
- Countdown is shown in chat and/or on the CenterHtml HUD
- The timer is cancelled automatically at round start, at round end, or when the 1v1 situation ends
- Minimum player count requirement (default: 3) — the timer will not start with too few players
- Surviving players are slayed when time runs out
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `1v1Slay` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/1v1Slay/
   ```
2. Restart the server or run `css_plugins load 1v1Slay`.
3. The config file is created automatically on first load.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_stopslay` | Stops the active 1v1 countdown | `@css/generic` **or** `@css/slay` |

## Configuration

The config file is created automatically on first load:

```
csgo/addons/counterstrikesharp/configs/plugins/1v1Slay/1v1Slay.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `min_players` | int | `3` | Minimum player count required for the timer (T + CT + spectators) |
| `countdown_time` | int | `30` | Countdown duration (seconds) |
| `enable_chat_announce` | bool | `true` | Enable/disable chat announcements |
| `enable_hud_announce` | bool | `true` | Enable/disable the HUD (CenterHtml) timer |

### Example Config

```json
{
  "min_players": 3,
  "countdown_time": 30,
  "enable_chat_announce": true,
  "enable_hud_announce": true
}
```

## Usage Example

1. Only 1 T and 1 CT are left alive in the round → a 30 second timer starts.
2. A warning drops into chat every 5 seconds (every second for the last 5); a red death timer appears on the HUD.
3. If time runs out both players are killed; if one kills the other the timer stops by itself.
4. An admin can cancel the timer with `!stopslay` if they want.

## Notes

- The chat prefix (`chat_prefix`) and all messages can be edited from `lang/tr.json` / `lang/en.json`.
- The icon on the HUD timer is loaded from [`img/skull.png`](../img/skull.png) in this repository.
