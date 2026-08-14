# BringGoto

*Read this in [Turkish / Türkçe](README.tr.md).*

Lets admins teleport players to the point under their crosshair (`!bring`) or teleport themselves next to a player (`!goto`). The teleport point is worked out live from exactly where you are aiming.

## Features

- `!bring <target>` teleports the target(s) exactly to the point you are looking at
- `!goto <target>` teleports you next to the target player
- Multi-target support: `@all`, `@t`, `@ct`, `#userid`, full or partial name
- Immunity check: the teleport is blocked if the target's immunity is higher than yours (can be disabled with `ignore_immunity`)
- Command names and permission flags can be changed from the config
- Teleported players receive an info message; bots can be targeted
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371
- The `gamedata` file: `addons/counterstrikesharp/gamedata/NativeTrace.gamedata.json`

## Installation

1. Copy the compiled `BringGoto` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/BringGoto/
   ```
2. Restart the server or run `css_plugins load BringGoto`.
3. The config file is created automatically on first load.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_bring` / `css_gel <target/@t/@ct/@all>` | Teleports the target(s) to the point under your crosshair | `@css/cheats` |
| `css_goto` / `css_git <target>` | Teleports you next to the target player | `@css/generic` |

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/BringGoto/BringGoto.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `bring_cmd` | string | `"css_bring,css_gel"` | Comma separated bring command names |
| `goto_cmd` | string | `"css_goto,css_git"` | Comma separated goto command names |
| `bring_flag` | string | `"@css/cheats"` | Permission required for bring (empty = everyone) |
| `goto_flag` | string | `"@css/generic"` | Permission required for goto (empty = everyone) |
| `ignore_immunity` | bool | `false` | When `true` the immunity check is skipped |

### Immunity Behavior

| `ignore_immunity` | User | Target | Result |
| --- | --- | --- | --- |
| `false` | 90 | 100 | Blocked |
| `false` | 90 | 90 | Teleported |
| `false` | 90 | 80 | Teleported |
| `true` | 90 | 100 | Teleported |

### Example Config

```json
{
  "bring_cmd": "css_bring,css_gel",
  "goto_cmd": "css_goto,css_git",
  "bring_flag": "@css/cheats",
  "goto_flag": "@css/generic",
  "ignore_immunity": false
}
```

## Notes

- Both commands require the player using them to be alive; dead and GOTV players cannot be targeted.
- If you are aiming at open sky and no point can be found, the target is teleported just in front of you instead. Targets are always placed slightly short of the surface so they do not get stuck inside a wall.
- In a multi-target bring every target arrives at the same point; targets with higher immunity are skipped silently, and if none are eligible a message is shown.
- Goto teleports you 80 units above the target, so the two players do not get stuck inside each other.
- Immunity is not checked on bots; you cannot target yourself with goto.
- Command name changes take effect when the plugin is restarted.
