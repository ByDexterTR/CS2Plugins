# JBDoors

*Read this in [Turkish / Türkçe](README.tr.md).*

Opens or closes every door on the map with a single command. Used for cell doors on Jailbreak servers.

## Features

- Opens every door type with one command: `func_door`, `func_movelinear`, `func_door_rotating`, `prop_door_rotating`
- The open command also breaks `func_breakable` entities (for breakable cell doors)
- The close command closes the same door types
- The name of the player who ran the command is announced to the whole server
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `JBDoors` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/JBDoors/
   ```
2. Restart the server or run `css_plugins load JBDoors`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_kapiac` | Opens every door and breaks the breakable ones | `@css/generic` **or** `@jailbreak/warden` |
| `css_kapikapat` | Closes every door | `@css/generic` **or** `@jailbreak/warden` |

## Configuration

Messages can be edited through `lang/tr.json` / `lang/en.json`.

## Usage Example

```
!kapiac    → [ByDexter] WardenName opened all doors!
!kapikapat → [ByDexter] WardenName closed all doors!
```

## Notes

- Broken `func_breakable` entities do not come back until the round restarts (map behavior).
- The command targets **every** matching entity on the map; it does not distinguish between specific doors.
