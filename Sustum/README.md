# Sustum

*Read this in [Turkish / Türkçe](README.tr.md).*

The Jailbreak "sustum" (typing) event: the first player to type the word shown on screen into chat wins (or in CTSustum, whoever fails to type it loses). Includes 4 different game modes.

## Game Modes

| Mode | Command | Rule |
| --- | --- | --- |
| **CTSustum** | `css_ctsustum` | Every CT except the warden must type the word; the **last remaining CT loses** and is sent to the T team |
| **TSustum** | `css_tsustum` | The first T to type the word **moves to the CT team** |
| **DSustum** | `css_dsustum` | The first living T to type the word **wins a Deagle** (single shot, removed after firing) and is colored orange |
| **DeadSustum** | `css_olusustum` | The first **dead T** to type the word is revived |

## Features

- After a 3 second countdown a phrase made of 1-4 random words appears in the middle of the screen
- The word pool is read from the `sustum.json` file (the repo ships with ~1000+ Turkish words)
- Word comparison is case insensitive
- Only one event runs at a time; the winner/loser is announced on the HUD and in chat
- Command to cancel the event
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `Sustum` folder to the server (**including the word file**):
   ```
   csgo/addons/counterstrikesharp/plugins/Sustum/
   ```
2. Make sure the word file is named **`sustum.json`** (lowercase) — see the note below.
3. Restart the server or run `css_plugins load Sustum`.

## Commands

Every command requires `@css/generic` **or** `@jailbreak/warden`:

| Command | Description |
| --- | --- |
| `css_ctsustum` | Starts CTSustum |
| `css_tsustum` | Starts TSustum |
| `css_dsustum` | Starts DSustum |
| `css_olusustum` | Starts DeadSustum |
| `css_sustum0` (+ `css_ctsustum0`, `css_tsustum0`, `css_dsustum0`, `css_olusustum0`) | Cancels the active event |

## Configuration

The word pool is the `sustum.json` file inside the plugin folder — a plain string array:

```json
[
  "apple",
  "pear",
  "computer",
  "keyboard"
]
```

## Notes

- In CTSustum the warden (`@jailbreak/warden`) is exempt automatically.
- A word can consist of multiple words; the whole thing must be typed exactly into chat.
