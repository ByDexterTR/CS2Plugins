# Cit (Fence)

*Read this in [Turkish / Türkçe](README.tr.md).*

Lets you place a fence (chainlink) or solid panel model at the point you are looking at. Designed for closing off areas / marking out a play area on Jailbreak servers.

## Features

- Managed from a single command through a CenterHtml menu
- 3 different sizes: Small (64x128), Medium (128x128), Large (256x128)
- 2 different types: **Fence** (chainlink, see-through) and **Barricade** (panel)
- Precise placement at the point you look at via native ray-trace (`NativeTrace`)
- The placed model is aligned automatically according to your view direction
- Delete the fence you are looking at, or clear every fence at once
- Models are precached automatically by the server
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

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

Model paths and sizes are defined in the `FenceOptions` dictionary in the source code.

## Notes

- Placed entities are created as `prop_physics_override`, have their motion disabled (`DisableMotion`) and are tagged with the name `bydexter_pluginfence` — "Delete All" only removes props with that tag, map props are left alone.
- If ray-trace is unavailable an error message is shown to the player (`NativeTrace.LastError`).
- The models used are the `de_nuke` chainlink fence models; they work on every official map.
