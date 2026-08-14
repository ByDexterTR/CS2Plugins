# Cit (Fence)

*Read this in [Turkish / Türkçe](README.tr.md).*

Lets you place a fence (chainlink) or solid panel model at the point you are looking at. Designed for closing off areas / marking out a play area on Jailbreak servers.

## Features

- Managed from a single command through an on-screen menu
- 3 different sizes: Small (64x128), Medium (128x128), Large (256x128)
- 2 different types: **Fence** (chainlink, see-through) and **Barricade** (panel)
- Precise placement exactly where you are looking
- The placed model is aligned automatically according to your view direction
- Delete the fence you are looking at, or clear every fence at once
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371
- The `gamedata` file: `addons/counterstrikesharp/gamedata/NativeTrace.gamedata.json`

## Installation

1. Copy the compiled `Cit` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/Cit/
   ```
2. Restart the server or run `css_plugins load Cit`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_cit` | Opens the fence menu (you must be alive) | `@css/generic` **or** `@jailbreak/warden` |

### Menu Options

| Option | Function |
| --- | --- |
| Create | Places a fence of the selected size and type at the point you are looking at |
| Change Type | Switches between fence ↔ solid panel |
| Change Size | Cycles Small → Medium → Large |
| Delete Aimed | Removes the fence you are aiming at (max. 256 units away) |
| Delete All | Removes every fence created with this plugin |

## Configuration

There is no config file; size and type are picked from the menu.

## Notes

- Fences you place stay fixed in position and cannot be pushed. "Delete All" only removes fences placed with this plugin and leaves the map's own objects alone.
- If the placement point cannot be worked out, an error message is shown to the player.
- The models used are the `de_nuke` chainlink fence models; they work on every official map.
