# GoBhop

*Read this in [Turkish / Türkçe](README.tr.md).*

Teleports dead T players to a hidden bhop area set up on the map. The player is actually alive but shows as dead on TAB, nobody can spectate or see them; they take no damage and spawn without weapons. When only one player is left on the T team everyone in GoBhop is killed automatically and GoBhop closes for that round. This is the CS2 port of the CSGO plugin [csgo_GoBhop](https://github.com/ByDexterTR/csgo_GoBhop).

## Features

- A dead T player is teleported alive to the point they pick from the `css_gobhop` menu; if there is only one point they go straight there without a menu
- They show as dead on TAB and cannot be spectated; players inside and outside GoBhop cannot see or hear each other
- They take no damage and spawn without weapons; any weapon they pick up is removed instantly, they cannot drop weapons and cannot use blocked commands
- Points are stored per map with names in the `positions.json` file and managed in-game
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `GoBhop` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/GoBhop/
   ```
2. Restart the server or run `css_plugins load GoBhop`.
3. Stand at the GoBhop point on the map and save the position with `css_setbhop <name>`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_gobhop` | Opens the point menu; enters GoBhop when outside (straight in if there is a single point), and offers exit + change point when inside | None |
| `css_onbhop` | Enables going to GoBhop | `@css/ban` |
| `css_offbhop` | Disables going to GoBhop and removes everyone inside | `@css/ban` |
| `css_setbhop <name>` | Saves your position and view direction under the given name | `@css/root` |
| `css_delbhop <name>` | Deletes the named GoBhop point on the map | `@css/root` |
| `css_resetbhop` | Deletes every saved GoBhop point on the map | `@css/root` |

Command names and permissions can be changed from the config.

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/GoBhop/GoBhop.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `gobhop_cmd` | string | `"css_gobhop"` | Comma separated entry command names |
| `onbhop_cmd` | string | `"css_onbhop"` | Comma separated enable command names |
| `offbhop_cmd` | string | `"css_offbhop"` | Comma separated disable command names |
| `set_cmd` | string | `"css_setbhop"` | Comma separated save point command names |
| `del_cmd` | string | `"css_delbhop"` | Comma separated named point delete command names |
| `reset_cmd` | string | `"css_resetbhop"` | Comma separated bulk delete command names |
| `blocked_cmd` | string | `"css_wp"` | Commands that cannot be used while in GoBhop (comma separated) |
| `admin_flag` | string | `"@css/ban"` | Permission required for the enable/disable commands |
| `set_flag` | string | `"@css/root"` | Permission required for saving points |
| `gobhop_min_alive_t` | int | `2` | Minimum living T count for entry to be allowed |

### Example Config

```json
{
  "gobhop_cmd": "css_gobhop",
  "onbhop_cmd": "css_onbhop",
  "offbhop_cmd": "css_offbhop",
  "set_cmd": "css_setbhop",
  "del_cmd": "css_delbhop",
  "reset_cmd": "css_resetbhop",
  "blocked_cmd": "css_wp",
  "admin_flag": "@css/ban",
  "set_flag": "@css/root",
  "gobhop_min_alive_t": 2
}
```

GoBhop points are kept in the `positions.json` file inside the plugin folder and written automatically by `css_setbhop`/`css_delbhop`/`css_resetbhop`:

```
csgo/addons/counterstrikesharp/plugins/GoBhop/positions.json
```

```json
{
  "de_dust2": {
    "KZ": {
      "pos": [-500.0, 200.0, 64.0],
      "ang": [0.0, 90.0, 0.0]
    },
    "Main": {
      "pos": [128.0, 1024.0, 8.0],
      "ang": [0.0, 180.0, 0.0]
    }
  }
}
```

## Notes

- Showing as dead on the scoreboard is done with the `m_bPawnIsAlive` field; if the game reverts the value it is reapplied every tick.
- Round end, plugin unload and `css_offbhop` remove everyone in GoBhop safely; these deaths are not shown in the kill feed.
- While disabled (`css_offbhop` or the last T), if another plugin revives a player in GoBhop they are caught at spawn and removed.
- Command name changes take effect when the server/plugin is restarted.
