# PrivateMessage

*Read this in [Turkish / Türkçe](README.tr.md).*

Lets players send private messages to each other with `!pm` / `!msg`. The message commands are not visible to other players in chat; separate notification sounds are played for the receiver and the sender.

```
Receiver's screen: [ByDexter] Sender: How are you today
Sender's screen:   [ByDexter] Message sent to Receiver.
```

## Features

- Private message with `!pm <player> <message>`; the name is matched fully or partially
- Chat lines starting with `!pm` / `!msg` are not shown to anyone (UserMessage hook)
- Per-player toggle for private messages (`!pmoff` / `!pmon`); a player who turns it off cannot receive or send PMs
- Per-player toggle for the notification sound (`!pmsound`)
- Separate sounds are played for the receiver and the sender; the sounds can be changed from the config
- Preferences are persistent — JSON (default) or MySQL; falls back to JSON automatically if the MySQL connection drops
- Optional logging: writes to the console and to daily files under `logs/`
- Bots and GOTV cannot be targeted
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `PrivateMessage` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/PrivateMessage/
   ```
2. Restart the server or run `css_plugins load PrivateMessage`.
3. The config file is created automatically on first load.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_msg` / `css_pm <player> <message>` | Sends a private message to a player | None |
| `css_msgoff` / `css_pmoff` | Turns off private messages | None |
| `css_msgon` / `css_pmon` | Turns on private messages | None |
| `css_msgsound` / `css_pmsound` | Toggles the private message sound | None |

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/PrivateMessage/PrivateMessage.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `msg_cmd` | string | `"css_msg,css_pm"` | Comma separated message command names |
| `msgoff_cmd` | string | `"css_msgoff,css_pmoff"` | Comma separated turn-off command names |
| `msgon_cmd` | string | `"css_msgon,css_pmon"` | Comma separated turn-on command names |
| `msgsound_cmd` | string | `"css_msgsound,css_pmsound"` | Comma separated sound command names |
| `receive_sound` | string | `"sounds/ambient/common/water/rain_drip3.vsnd"` | Sound played for the receiver |
| `send_sound` | string | `"sounds/ambient/common/water/rain_drip1.vsnd"` | Sound played for the sender |
| `log_enabled` | bool | `false` | Writes messages to the console and a daily log file |
| `database` | object | JSON | Preference storage settings (below) |

### Storage

| Field | Default | Description |
| --- | --- | --- |
| `provider` | `"json"` | `"json"` or `"mysql"` |
| `host` | `"localhost"` | MySQL server |
| `name` | `"bydexter_pm"` | Database name (created if missing) |
| `port` | `"3306"` | MySQL port |
| `user` | `"root"` | MySQL user |
| `password` | `""` | MySQL password |

In JSON mode preferences are kept in `plugins/PrivateMessage/players.json`. In MySQL mode the `pm_preferences` table is created automatically; if the connection cannot be made it falls back to JSON.

### Example Config

```json
{
  "msg_cmd": "css_msg,css_pm",
  "msgoff_cmd": "css_msgoff,css_pmoff",
  "msgon_cmd": "css_msgon,css_pmon",
  "msgsound_cmd": "css_msgsound,css_pmsound",
  "receive_sound": "sounds/ambient/common/water/rain_drip3.vsnd",
  "send_sound": "sounds/ambient/common/water/rain_drip1.vsnd",
  "log_enabled": true,
  "database": {
    "provider": "mysql",
    "host": "localhost",
    "name": "bydexter_pm",
    "port": "3306",
    "user": "root",
    "password": ""
  }
}
```

## Logging

With `log_enabled: true` every message is written to the server console as `[SENDER -> RECEIVER]: Message` and saved to separate daily files:

```
plugins/PrivateMessage/logs/PrivateMessage-2026-07-16.log
```

File lines include a timestamp: `[21:45:03] [ByDexter -> Player]: hello`

## Notes

- `!pm ...` / `/pm ...` lines typed into chat (including every command name in the config) are blocked with a UserMessage hook and are not shown to anyone; the command still runs.
- Message and sound preferences are persistent per SteamID (default: messages on, sound on); they are saved on every change and when the player disconnects.
- Name matching tries the full name first, then a partial search; if there are multiple matches no message is sent.
- Command name changes take effect when the plugin is restarted.
