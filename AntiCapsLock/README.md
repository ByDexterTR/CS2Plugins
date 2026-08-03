# AntiCapsLock

*Read this in [Turkish / Türkçe](README.tr.md).*

Prevents excessive caps in chat; if the uppercase ratio of a message passes the threshold, the message is automatically lowercased or deleted.

## Features

- Two modes: lowercase the message, or delete the message and warn the player
- Threshold ratio is set from the config (between `0.0` and `1.0`; `0.5` = 50% of the message)
- The ratio is calculated from letters only; digits, punctuation and color codes are not counted
- Minimum letter count limit for short messages (messages like `OK`, `AY` do not trigger it)
- Command messages starting with `!` and `/` are ignored
- The exempt flag can be set from the config (leave empty and everyone is affected)
- Lowercasing follows Turkish character rules (`lowercase_culture`)
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `AntiCapsLock` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/AntiCapsLock/
   ```
2. Restart the server or run `css_plugins load AntiCapsLock`.

## Configuration

`configs/plugins/AntiCapsLock/AntiCapsLock.json` (created automatically on first load):

| Setting | Description | Default |
| --- | --- | --- |
| `mode_capslock` | `1` = lowercase the characters in the message, `2` = warn the player and delete the message | `1` |
| `threshold_capslock` | Trigger threshold; the uppercase ratio of the letters in the message (`0.0` - `1.0`) | `0.5` |
| `minlength_capslock` | Minimum letter count in the message for the check to apply | `4` |
| `lowercase_culture` | Language rule used when lowercasing (empty = culture invariant) | `tr-TR` |
| `capsignore_flag` | Admin flag exempt from the check (empty = everyone is affected) | `` |

The warning message and the chat prefix can be edited through `lang/tr.json` / `lang/en.json`.

## Usage Example

With `threshold_capslock: 0.5`:

```
mode_capslock: 1
"HELLO EVERYONE" → "hello everyone"

mode_capslock: 2
"HELLO EVERYONE" → message is deleted + "You used too many capital letters, your message was deleted!"

"Hello everyone" → untouched in both modes (ratio is below 50%)
```
