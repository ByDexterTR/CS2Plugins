# FortniteArmor

*Read this in [Turkish / Türkçe](README.tr.md).*

Makes incoming damage hit armor first, like in Fortnite: as long as there is armor left your health never drops, and once the armor is gone the remaining damage goes to health. For example, on a 50 damage hit with 40 armor your armor drops to 0 and you only lose 10 health.

## Features

- While you have armor all damage comes off the armor first; health only starts dropping once the armor runs out
- The vanilla kevlar ratio (partial absorption) is disabled — armor absorbs damage 1 to 1
- Can be restricted with a permission flag; works for everyone when the flag is empty
- Fall damage does not come off armor by default (can be enabled in the config)
- Steps in **before** the damage reaches the player, so the health and death calculation is always correct
- Works on every damage type including bullets, HE, molotov and knife

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `FortniteArmor` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/FortniteArmor/
   ```
2. Restart the server or run `css_plugins load FortniteArmor`.
3. The config file is created automatically on first load.

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/FortniteArmor/FortniteArmor.json
```

| Setting | Type | Description |
| --- | --- | --- |
| `armor_flag` | string | Comma separated permission flags; if left empty everyone benefits (default `""`) |
| `absorb_fall_damage` | bool | When `true` fall damage also comes off armor (default `false`) |

### Example Config

```json
{
  "armor_flag": "@css/vip,@css/root,@css/ban",
  "absorb_fall_damage": false
}
```

## Notes

- The plugin catches the damage before it is taken off the player's health. Otherwise the player would already have died by the normal calculation.
- When the damage is fully absorbed by armor the engine sees 0 damage, so hit feedback (aim punch, the `player_hurt` event) may not happen.
- The helmet is not tracked separately; once armor drops to 0 the vanilla behavior (unprotected) applies.
- When `armor_flag` is set, players without the permission get the vanilla armor behavior. The `@css/root` flag is always treated as valid.
