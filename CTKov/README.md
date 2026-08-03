# CTKov

*Read this in [Turkish / Türkçe](README.tr.md).*

Sends every CT player without warden permission to the T team with a single command. Used for the "kick the guards" event in Jailbreak.

## Features

- Players with the warden permission (`@jailbreak/warden`) are protected — they stay on the team
- Bots are not included
- The number of moved guards is announced to the whole server
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `CTKov` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/CTKov/
   ```
2. Restart the server or run `css_plugins load CTKov`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_ctkov` | Moves every non-warden CT to the T team | `@css/generic` **or** `@jailbreak/warden` |

## Configuration

Messages can be edited through `lang/tr.json` / `lang/en.json`.

## Usage Example

```
!ctkov
```

> `[ByDexter] AdminName kicked all guards! (5 players moved to the T team)`
