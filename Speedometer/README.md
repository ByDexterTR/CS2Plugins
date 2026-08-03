# Speedometer

*Read this in [Turkish / Türkçe](README.tr.md).*

Shows the player's live speed (u/s) in the middle of the screen. As speed increases the readout goes from white to blue, orange and red. Designed for bhop / surf / kz servers.

## Features

- Live horizontal speed readout (`u/s`) on the CenterHtml HUD
- Color transition by speed: 0 white → 1000 blue → 2000 orange → 3000+ red (values in between are interpolated)
- In spectator mode the spectated player's speed is shown
- The preference is persistent — players who turn it off are saved to the `Speedometer.json` file
- The readout is hidden automatically while a menu is open
- Slot based tracking; tick cost is minimal while nobody is using it
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `Speedometer` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/Speedometer/
   ```
2. Restart the server or run `css_plugins load Speedometer`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_hiz` / `css_hız` | Toggles the speed readout | — (everyone) |

## Configuration

The SteamID64 list of players who **turned off** the readout is kept in the `Speedometer.json` file inside the plugin folder (default behavior: on for everyone).

## Notes

- The vertical axis is not included in the speed calculation (X/Y plane only).
- The readout icon is loaded from [`img/speedometer.png`](../img/speedometer.png) in this repository.
