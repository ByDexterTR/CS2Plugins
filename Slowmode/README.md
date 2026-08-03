# Slowmode

*Read this in [Turkish / Türkçe](README.tr.md).*

Applies a server-wide chat slow mode; while it is on, players have to wait the configured number of seconds between messages.

## Features

- Server-wide slow mode with `!slowmode <seconds>` (limits come from the config)
- Turn it off with `!slowmode off` or `!slowmode 0` (`0` is always valid, the minimum limit does not apply to it)
- A player who types before the delay expires has their message blocked and is told the remaining time
- The exempt flag can be set from the config (default `@css/chat`, leave empty and nobody is exempt)
- Enabling/disabling is announced to the whole server
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `Slowmode` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/Slowmode/
   ```
2. Restart the server or run `css_plugins load Slowmode`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_slowmode <seconds>` | Enables slow mode with the given second interval | `@css/chat` |
| `css_slowmode off` | Disables slow mode | `@css/chat` |

## Configuration

`configs/plugins/Slowmode/Slowmode.json` (created automatically on first load):

| Setting | Description | Default |
| --- | --- | --- |
| `slowignore_flag` | Admin flag not affected by slow mode (empty = everyone is affected) | `@css/chat` |
| `slow_min` | Lowest number of seconds that can be entered with the command (at least 1) | `1` |
| `slow_max` | Highest number of seconds that can be entered with the command | `300` |

Messages and the chat prefix can be edited through `lang/tr.json` / `lang/en.json`.

## Usage Example

```
!slowmode 10  → Slow mode enabled! You must wait 10 seconds between messages.
!slowmode off → Slow mode disabled.
```
