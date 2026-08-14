# AntiTeamFlash

*Read this in [Turkish / Türkçe](README.tr.md).*

Cancels the blinding effect of flashbangs thrown by teammates. Enemy flashes keep working normally.

## Features

- Instantly clears the flash effect coming from a teammate
- Restores any still-running "legitimate" blindness caused by an enemy (a team flash cannot wipe legitimate blindness)
- No config required

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `AntiTeamFlash` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/AntiTeamFlash/
   ```
2. Restart the server or run `css_plugins load AntiTeamFlash`.

## Notes

- Flashbangs you throw yourself still affect you (only a *teammate's* flash is blocked).
