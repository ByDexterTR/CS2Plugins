# BhopDoorFix

*Read this in [Turkish / Türkçe](README.tr.md).*

Stops doors on bhop / KZ maps from moving. That way doors cannot launch players or create exploits by moving around.

## Features

- Freezes and locks every door on the map in place
- Also catches doors that appear later during the map
- Re-freezes every door at the start of each round
- If you reload the plugin mid-map, the doors already on the map are frozen instantly
- No config required

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `BhopDoorFix` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/BhopDoorFix/
   ```
2. Restart the server or run `css_plugins load BhopDoorFix`.

## Notes

- Because the doors are locked, map door mechanics (buttons, triggers, etc.) will not work — the plugin is designed for bhop/surf servers.
