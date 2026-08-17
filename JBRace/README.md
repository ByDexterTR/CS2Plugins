# JBRace

*Read this in [Turkish / Türkçe](README.tr.md).*

Race event for Jailbreak servers. The warden sets a start and a finish point, and T players race to be the first to reach the finish.

## Features

- Full race management through an on-screen menu: start, cancel, start/finish point, winner count, clear markers
- A spinning **coin model** and a **green beam** reaching to the sky as the finish marker
- 3 second HUD countdown; racers are teleported to the start point and frozen
- Racing Ts are colored red, players who reach the finish are colored green
- When the target winner count is reached the **Ts who did not win are slayed** and the race ends
- The winner count is set by typing it into chat (when requested from the menu)
- The race is reset automatically at round start
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `JBRace` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/JBRace/
   ```
2. Restart the server or run `css_plugins load JBRace`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_race` | Opens the race menu | `@css/generic` **or** `@jailbreak/warden` |

### Menu Options

| Option | Function |
| --- | --- |
| Start / Cancel Race | Starts the race (start+finish must be set) or cancels the active race |
| Start Point | Saves your current position and view direction as the start |
| Finish Point | Saves your current position as the finish and places the marker |
| Winner Count (N) | Asks you to type a number into chat; the first N players win |
| Clear Markers | Removes the finish model/beam |

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/JBRace/JBRace.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `race_cmd` | string | `"css_race,css_yaris"` | Commands that open the menu, separated by commas |
| `race_flag` | string | `"@jailbreak/warden,@css/generic"` | Flags allowed to use it, separated by commas |
| `race_model` | string | `"models/coop/challenge_coin.vmdl"` | The model placed at the finish line |
| `race_countdown` | int | `3` | Countdown in seconds before the race starts |

## Usage Example

1. The warden walks to the finish line → `!race` → "Finish Point".
2. They walk to where the race will start → "Start Point".
3. "Winner Count" → they type `3` into chat.
4. "Start Race" → every living T is teleported to the start and the race begins after a 3-2-1 countdown.
5. The first 3 to reach the finish win; the remaining Ts are slayed automatically.

## Notes

- Only **living T players** join the race.
