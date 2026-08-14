# Silahsil

*Read this in [Turkish / Türkçe](README.tr.md).*

Clears unowned weapons on the ground with a single command. Used in Jailbreak to clean up weapons before the cells open.

## Features

- Removes every unowned weapon lying on the map
- Does not touch weapons held or carried by players
- The number of removed weapons is reported to the player who used the command
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `Silahsil` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/Silahsil/
   ```
2. Restart the server or run `css_plugins load Silahsil`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_silahsil` | Deletes every weapon on the ground | `@css/slay` |

## Configuration

Messages can be edited through `lang/tr.json` / `lang/en.json`.

## Usage Example

```
!silahsil → [ByDexter] AdminName deleted 12 weapons on the ground.
```
