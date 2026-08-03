# ChatCleaner

*Read this in [Turkish / Türkçe](README.tr.md).*

Chat clearing tool. Players can clear their own screen, admins can clear the whole server's chat.

## Features

- Per-player chat clearing (own screen only)
- Server-wide chat clearing for admins (the clearing admin's name is announced)
- Pushes history off the screen by printing 500 blank lines
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `ChatCleaner` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/ChatCleaner/
   ```
2. Restart the server or run `css_plugins load ChatCleaner`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_selfcc` | Clears only your own chat screen | — (everyone) |
| `css_cc` | Clears the whole server's chat | `@css/chat` |

## Configuration

Messages and the chat prefix can be edited through `lang/tr.json` / `lang/en.json`.

## Usage Example

```
!selfcc   → Your chat has been cleared.
!cc       → Chat cleared. Cleared by: AdminName
```
