# ScreenText

*Read this in [Turkish / Türkçe](README.tr.md).*

Shows fully config-customizable text fixed on the player's screen. The text stays put while running and jumping and turns with the player. Every player only sees their own text; other players, spectators and GOTV can never see it.

## Features

- Unlimited number of texts; screen position (X/Y), size, color, alignment and background can be set separately for each
- The text stays fixed on screen while running and jumping, it never drags behind
- Texts only appear on their owner's screen; nobody watching from outside can see them
- Per-player toggle with `css_hidetext`; the preference is saved to JSON and remembered on reconnect
- Multi-line text support with `\n`
- Font, distance from the screen and pixel scale can be set from the config
- Texts are cleaned up safely on death, team change, disconnect and map end
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `ScreenText` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/ScreenText/
   ```
2. Restart the server or run `css_plugins load ScreenText`.
3. The config file is created automatically on first load.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_hidetext` | Toggles the screen texts (the preference is persistent) | None |

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/ScreenText/ScreenText.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `screentext_cmd` | string | `"css_hidetext"` | Comma separated command names |
| `screentext_default_on` | bool | `true` | Initial text state for newly connecting players |
| `screentext_font` | string | `"Arial Bold"` | Font |
| `screentext_forward` | float | `7` | Distance of the texts from the eye (world units, minimum 1) |
| `screentext_units_per_px` | float | `0.012` | World units per pixel; increasing it makes every text bigger |
| `screentext_texts` | array | 2 examples | List of texts to show (see below) |

### Text Item (`screentext_texts`)

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `text` | string | `""` | Text to show; `\n` for multiple lines |
| `x` | float | `0` | Horizontal position: `0` center, negative left, positive right (visible range ≈ -7 … +7) |
| `y` | float | `0` | Vertical position: `0` center, positive up, negative down (visible range ≈ -3.9 … +3.9) |
| `size` | float | `32` | Text size (px) |
| `color` | string | `"#FFFFFF"` | Color (`#RRGGBB` or `R G B`) |
| `justify` | string | `"left"` | Horizontal alignment: `left` / `center` / `right` |
| `background` | bool | `false` | Draws a semi-transparent panel behind the text |

### Example Config (GitHub under the radar on the left, site address top right)

```json
{
  "screentext_cmd": "css_hidetext",
  "screentext_default_on": true,
  "screentext_font": "Arial Bold",
  "screentext_forward": 7,
  "screentext_units_per_px": 0.012,
  "screentext_texts": [
    {
      "text": "github.com/ByDexterTR/CS2Plugins",
      "x": -6.4,
      "y": 1.3,
      "size": 32,
      "color": "#FFFFFF",
      "justify": "left",
      "background": false
    },
    {
      "text": "bydexter.net",
      "x": 6.4,
      "y": 2.3,
      "size": 32,
      "color": "#7CFC00",
      "justify": "right",
      "background": false
    }
  ]
}
```

## Coordinate System

The center of the screen is treated as `(0, 0)`; `x` increases to the right and `y` increases upward. With the default settings roughly `-7` to `+7` is visible horizontally and roughly `-3.9` to `+3.9` vertically. When pushing text to a corner, solve edge overflow with `justify`: `left` for the left edge, `right` for the right edge.

## Notes

- The game's own HUD elements (radar, score, money, health) are always drawn on top of the text.
- The `css_hidetext` preference is saved as a SteamID in `plugins/ScreenText/ScreenText.json`; while `screentext_default_on: false` the enable preference only lasts for the session.
- Text list changes take effect when the plugin is reloaded (`css_plugins reload ScreenText`).
- Text visibility is off for spectators too: someone spectating a player in first person does not see that player's texts.
- On very fast view flicks the text can slip for an instant and settle right back; this is normal.
- While in a third person camera (e.g. the Thirdperson plugin) the text appears floating at the character's eye level.
