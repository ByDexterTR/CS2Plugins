# SpawnkillProtection

*Read this in [Turkish / Türkçe](README.tr.md).*

Gives spawning players damage protection for a configurable duration. This is the full version of [CTSpawnKill](../CTSpawnKill): it has team based **and** permission (flag) based protection, colored visual feedback, and a **color that fades back to normal** over the protection period.

## Features

- **Flag based protection takes priority** over team protection — e.g. longer protection for VIPs
- Team protection that can be enabled/disabled per T and CT with customizable duration and color
- During the protection the player's color **fades gradually back to normal** — the fading color shows how much protection is left, so everyone can see when it ends
- Every kind of damage is blocked while the protection lasts
- The start and end of the protection are reported to the player in chat
- State is cleaned up safely at round start and when a player leaves
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `SpawnkillProtection` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/SpawnkillProtection/
   ```
2. Restart the server or run `css_plugins load SpawnkillProtection`.
3. The config file is created automatically on first load.

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/SpawnkillProtection/SpawnkillProtection.json
```

| Setting | Type | Description |
| --- | --- | --- |
| `flag_protections` | list | Permission based protections; **the order in the list is the priority order** and it overrides team protection |
| `flag_protections[].flag` | string | Required permission (e.g. `@css/vip`) |
| `flag_protections[].seconds` | float | Protection duration (seconds) |
| `flag_protections[].color` | int[3] | Protection color (RGB) |
| `team_t.enabled` | bool | Whether T team protection is active |
| `team_t.seconds` | float | T protection duration |
| `team_t.color` | int[3] | T protection color |
| `team_ct.enabled` / `seconds` / `color` | — | Same settings for CT |

### Example Config

```json
{
  "flag_protections": [
    { "flag": "@css/root", "seconds": 10, "color": [255, 0, 255] },
    { "flag": "@css/vip",  "seconds": 8,  "color": [255, 215, 0] }
  ],
  "team_t":  { "enabled": true, "seconds": 5, "color": [255, 64, 64] },
  "team_ct": { "enabled": true, "seconds": 5, "color": [64, 128, 255] }
}
```

In this example: root admins spawn with 10 s purple, VIPs with 8 s gold, other Ts with 5 s red and other CTs with 5 s blue protection.

## Notes

- Team protection applies to bots too; the flag check is only done for real players.
- A protection can be disabled completely with `seconds: 0` or `enabled: false`.
- For a simpler version of the same feature that only applies to CT with a fixed color see the [CTSpawnKill](../CTSpawnKill) plugin; **do not use both plugins at the same time**.
