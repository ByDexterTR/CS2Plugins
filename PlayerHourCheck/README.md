# PlayerHourCheck

*Read this in [Turkish / Türkçe](README.tr.md).*

Checks the CS2 playtime of players connecting to the server; applies tiered punishments (kick/ban) to players with too few hours or a private profile.

## Features

- 3 stage playtime lookup: **Steam Web API** → **DecAPI** → **ByDexter API** (the first successful result is used)
- **JSON (default) or MySQL** storage; falls back to JSON if the MySQL connection fails
- Results are cached in the database — no API lookup is repeated until the missing hours are made up
- A configurable number of **warnings** for players with a private profile, then the punishment
- **Tiered punishment system** by violation count (e.g. 1st violation kick, 3rd violation 1 hour ban, 5th violation 1 day ban)
- Exemption list by permission flag or SteamID
- Reload command that reloads the config and re-checks every player
- Color coded message support (`{Gold}`, `{Red}` etc.)
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371
- An admin plugin providing the `css_kick` and `css_ban` commands so punishments can be applied (e.g. CS2-SimpleAdmin)
- (Recommended) [Steam Web API key](https://steamcommunity.com/dev/apikey)
- (If MySQL will be used) MySQL 8+ server

## Installation

1. Copy the compiled `PlayerHourCheck` folder to the server **together with all dependency DLLs**:
   ```
   csgo/addons/counterstrikesharp/plugins/PlayerHourCheck/
   ```
2. Edit the config file created on first load (at least `phc_required_playtime` and preferably `phc_steam_api_key`).
3. Reload with `css_plugins reload PlayerHourCheck`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_phc_reload` | Reloads the config from disk and re-checks every player | `@css/root` |

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/PlayerHourCheck/PlayerHourCheck.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `phc_db` | object | json | Storage settings (below) |
| `phc_steam_api_key` | string | `""` | Steam Web API key (if empty it goes straight to DecAPI) |
| `phc_required_playtime` | int | `100` | Minimum required CS2 hours |
| `phc_warn_enabled` | int | `1` | Private profile warning system (1: on, 0: punish directly) |
| `phc_warn_times` | int | `3` | Number of warnings before the punishment |
| `phc_warn_timer` | int | `30` | Wait between warnings (seconds) |
| `phc_warn_reason_private` | string | — | Private profile warning message (`{0}`: current, `{1}`: total warnings) |
| `phc_kick_reason_private` | string | — | Private profile kick reason |
| `phc_kick_reason_playtime` | string | — | Insufficient hours kick reason |
| `phc_penalty` | object | below | Violation count → punishment mapping |
| `phc_ignore_flags` | list | `["@bydexter/ignoreplaytime", "@css/root"]` | Exempt permission flags |
| `phc_ignore_steamids` | list | — | Exempt SteamID64 list |

### `phc_db`

```json
"phc_db": {
  "provider": "json",
  "host": "localhost",
  "name": "cs2_playerhourcheck",
  "port": "3306",
  "user": "root",
  "password": ""
}
```

- `provider`: `"json"` (default, `players.json` in the plugin folder) or `"mysql"`
- If MySQL is selected the database and table are created automatically.

### `phc_penalty`

Key = violation count, value = punishment. `type`: `"kick"` or `"ban"`, `time`: ban duration (minutes), and `reason` can use the `{PlayerPlaytime}` and `{RequiredPlaytime}` placeholders:

```json
"phc_penalty": {
  "1": { "type": "kick", "time": 0,    "reason": "Insufficient playtime ({PlayerPlaytime}/{RequiredPlaytime} hours)" },
  "3": { "type": "ban",  "time": 60,   "reason": "Insufficient playtime ({PlayerPlaytime}/{RequiredPlaytime} hours)" },
  "5": { "type": "ban",  "time": 1440, "reason": "Insufficient playtime ({PlayerPlaytime}/{RequiredPlaytime} hours)" }
}
```

> For violations in between, the punishment of the lower tier applies (e.g. 4th violation → the "3" entry).

## Notes

- Punishments are applied with the `css_kick` / `css_ban` console commands; if those commands are not defined on the server no punishment happens.
- All database operations run in the background and do not block the game loop.
