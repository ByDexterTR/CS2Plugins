# CTSpawnKill

*Read this in [Turkish / Türkçe](README.tr.md).*

Gives CT players short damage protection after they spawn. Prevents guards from being hunted (spawn killed) at the start of a round in Jailbreak.

## Features

- **All damage is zeroed** for the configured duration when a CT spawns
- The protected player is colored orange; the color returns to normal when the duration ends
- The start and end of the protection are reported to the player in chat
- Timers are cleaned up safely when a player leaves / the map changes
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `CTSpawnKill` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/CTSpawnKill/
   ```
2. Restart the server or run `css_plugins load CTSpawnKill`.
3. The config file is created automatically on first load.

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/CTSpawnKill/CTSpawnKill.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `spawn_protect_seconds` | int | `5` | Duration of the spawn protection (seconds, minimum 1) |

### Example Config

```json
{
  "spawn_protect_seconds": 5
}
```

## Notes

- The protection only applies to the **CT team**.
- Damage blocking is done through `OnEntityTakeDamagePre`; every damage source (weapon, knife, explosion) is zeroed.
- For a more complete version with flag based protection, T support and color transitions see the [SpawnkillProtection](../SpawnkillProtection) plugin; do not use both plugins at the same time.
