# BhopDoorFix

*Read this in [Turkish / Türkçe](README.tr.md).*

Stops `func_door` doors on bhop / KZ maps from moving. Prevents doors from launching players or creating exploits by moving.

## Features

- Freezes every `func_door` entity on the map (`Speed = 0` + `Lock` input)
- Catches newly spawned doors automatically (`OnEntitySpawned`)
- Re-freezes every door at the start of each round
- Hot reload supported — existing doors are frozen instantly when the plugin is reloaded
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
