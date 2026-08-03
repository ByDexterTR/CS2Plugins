# Sesler (Sounds)

*Read this in [Turkish / Türkçe](README.tr.md).*

Lets players mute game sounds they do not want to hear, by category. Preferences are stored per player in a database and applied automatically when they rejoin.

## Features

- 5 sound categories: **Knife**, **Weapon**, **Footsteps**, **Player sounds**, **MVP music**
- 4 modes per category: **On**, **Mute Enemy**, **Mute Team**, **Off** (MVP only has On/Off)
- Easy management through a CenterHtml menu; the active option is highlighted with a ► marker and color
- **JSON (default) or MySQL** storage; falls back to JSON if the MySQL connection fails, the table is created automatically
- Sound blocking is done server side by filtering UserMessage recipients — other players hear the sounds normally
- MVP muting works only on the player in question via `StopSoundEvents.StopAllMusic`
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371
- (If MySQL will be used) MySQL 8+ server

## Installation

1. Copy the compiled `Sesler` folder to the server **together with all dependency DLLs**:
   ```
   csgo/addons/counterstrikesharp/plugins/Sesler/
   ```
2. Restart the server or run `css_plugins load Sesler`.
3. JSON is used by default (`players.json` in the plugin folder); edit the config for MySQL.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_ses` / `css_sesler` | Opens the sound preferences menu | — (everyone) |

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/Sesler/Sesler.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `Database.provider` | string | `"json"` | `"json"` or `"mysql"` |
| `Database.host` | string | `"localhost"` | MySQL server address |
| `Database.name` | string | `"bydexter_sesler"` | Database name (created if missing) |
| `Database.port` | string | `"3306"` | MySQL port |
| `Database.user` | string | `"root"` | MySQL user |
| `Database.password` | string | `""` | MySQL password |

### Example Config

```json
{
  "Database": {
    "provider": "mysql",
    "host": "127.0.0.1",
    "name": "bydexter_sesler",
    "port": "3306",
    "user": "cs2",
    "password": "secret"
  }
}
```

## Notes

- Sound hash lists can change with game updates; if new sounds start coming through, the hash lists need updating.
- Database operations run in the background and do not block the game loop.
