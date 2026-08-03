# AdminList

*Read this in [Turkish / Türkçe](README.tr.md).*

Lets players see the currently online admins with their group tags via the `css_admins` command. Groups are defined in the config; every group has a tag, a tag color, a name color and a permission flag.

```
Online Admins:
[OWNER] ByDexter
[DEV] Claude
[MOD] Grok
[VIP] Gemini
```

## Features

- The server owner can define unlimited groups in the config (`tag`, `tag_color`, `name_color`, `flag`)
- Groups are evaluated top to bottom in priority order; a player counts under the first group they match (e.g. someone with `@css/root` only appears in the topmost group, not repeated in lower ones)
- Tag and name colors can be set separately per group
- The config can be reloaded without restarting the server with `css_adminsreload` / `css_reloadadmins`
- Bots and GOTV are not listed
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `AdminList` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/AdminList/
   ```
2. Restart the server or run `css_plugins load AdminList`.
3. The config file is created automatically on first load.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_admins` | Lists online admins with their group tags | None |
| `css_adminsreload` / `css_reloadadmins` | Reloads the config | `@css/root` |

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/AdminList/AdminList.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `admins_cmd` | string | `"css_admins"` | Comma separated list command names |
| `reload_cmd` | string | `"css_adminsreload,css_reloadadmins"` | Comma separated reload command names |
| `reload_flag` | string | `"@css/root"` | Permission required for the reload command |
| `groups` | array | 4 example groups | Group definitions in priority order |

### Group Fields

| Field | Description |
| --- | --- |
| `tag` | Tag shown in chat (`[TAG]`) |
| `tag_color` | Tag color |
| `name_color` | Player name color |
| `flag` | The group's permission flag (e.g. `@css/mod`) |

Valid colors: `default`, `white`, `darkred`, `green`, `lightgreen`, `lime`, `red`, `grey`, `yellow`, `bluegrey`, `blue`, `darkblue`, `purple`, `orchid`, `lightred`, `gold`

### Example Config

```json
{
  "admins_cmd": "css_admins",
  "reload_cmd": "css_adminsreload,css_reloadadmins",
  "reload_flag": "@css/root",
  "groups": [
    { "tag": "OWNER", "tag_color": "darkred", "name_color": "gold", "flag": "@css/owner" },
    { "tag": "DEV", "tag_color": "purple", "name_color": "lightred", "flag": "@css/dev" },
    { "tag": "MOD", "tag_color": "blue", "name_color": "bluegrey", "flag": "@css/mod" },
    { "tag": "VIP", "tag_color": "gold", "name_color": "yellow", "flag": "@css/vip" }
  ]
}
```

## Notes

- Group order is priority: if a player has the flags of more than one group they are only shown in the group highest in the list. Since `@css/root` holders pass every `@css/*` flag, putting a flag like `@css/root` or `@css/owner` on the top group gives the correct ordering.
- Flags match the permissions in CounterStrikeSharp's `admins.json` file; custom flags like `@css/owner`, `@css/dev`, `@css/mod` can also be used.
- Command name changes (`admins_cmd`, `reload_cmd`) take effect when the plugin is restarted; the reload command only updates the group and permission settings instantly.
