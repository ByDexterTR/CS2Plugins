# JBTeams

*Read this in [Turkish / Türkçe](README.tr.md).*

Event plugin that splits living T players into colored teams. Players on the same team cannot damage each other; the fight continues until one team is left standing.

## Features

- 2–5 team support: Red, Green, Blue, Yellow, Magenta
- Players are shuffled randomly and distributed in **equal numbers** (warns if they cannot be split evenly)
- Damage between members of the same team is zeroed automatically (friendly fire protection)
- Every player is colored in their team color; a dead player's color is reset
- When one team is left the winner is announced and the system shuts down
- Teams can be disabled manually with `!takim 0` or `!takim 1`
- Automatic reset at round start/end
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `JBTeams` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/JBTeams/
   ```
2. Restart the server or run `css_plugins load JBTeams`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_takim <2-5>` | Splits living Ts into the given number of teams | `@css/generic` **or** `@jailbreak/warden` |
| `css_takim <0-1>` | Shuts down the active team system | `@css/generic` **or** `@jailbreak/warden` |

## Configuration

Team colors and names are defined in the source code; messages can be edited through `lang/tr.json` / `lang/en.json`.

## Usage Example

```
!takim 2
```

> 8 living Ts → Red and Green teams of 4 players each are created.
> When someone from Red eliminates all of Green: `Red wins.`

## Notes

- If the living T count cannot be divided evenly by the team count the system will not start (e.g. 7 players / 2 teams).
- Only **living players on the T team** are included.
