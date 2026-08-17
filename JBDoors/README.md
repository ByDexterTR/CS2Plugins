# JBDoors

*Read this in [Turkish / Türkçe](README.tr.md).*

Opens or closes every door on the map with a single command. Used for cell doors on Jailbreak servers.

## Features

- Opens every kind of door on the map with one command (sliding, rotating, moving)
- The open command also breaks breakable cell doors
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

```
csgo/addons/counterstrikesharp/configs/plugins/JBDoors/JBDoors.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `dooropen_cmd` | string | `"css_kapiac,css_dooropen"` | Commands that open the doors, separated by commas |
| `dooropen_flag` | string | `"@jailbreak/warden,@css/generic"` | Flags allowed to open them |
| `doorclose_cmd` | string | `"css_kapikapat,css_doorclose"` | Commands that close the doors, separated by commas |
| `doorclose_flag` | string | `"@jailbreak/warden,@css/generic"` | Flags allowed to close them |
| `doorbreak` | bool | `true` | Whether breakable doors are broken as well as opened |

Messages can be edited through `lang/tr.json` / `lang/en.json`.

## Usage Example

```
!kapiac    → [ByDexter] WardenName opened all doors!
!kapikapat → [ByDexter] WardenName closed all doors!
```

## Notes

- Broken doors do not come back until the round restarts; that is the map's own behavior.
- The command affects **every** door on the map; you cannot pick individual doors.
