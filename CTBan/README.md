# CTBan

*Read this in [Turkish / Türkçe](README.tr.md).*

Temporarily bans players from joining the CT (guard) team. An essential moderation tool for Jailbreak servers.

## Features

- Timed CT ban (flexible time units from seconds to years)
- A banned player is sent to the T team automatically when they try to join CT (team change, spawn and the `jointeam` command are all checked)
- Add bans by SteamID64 for players who are not on the server
- The ban list is persistent — stored in `CTBanList.json`, kept across server restarts
- Expired bans are cleaned up automatically
- Records the ban reason and the admin who issued it
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `CTBan` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/CTBan/
   ```
2. Restart the server or run `css_plugins load CTBan`.
3. `CTBanList.json` is created automatically on first load.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_ctban <target> <time> [reason]` | CT bans a player on the server (moved to T if they are on CT) | `@css/ban` |
| `css_ctunban <target>` | Removes the CT ban of a player on the server | `@css/ban` |
| `css_ctaddban <steamid64> <time> [reason]` | Adds an (offline) ban by SteamID64 | `@css/ban` |
| `css_ctbanlist` | Lists the active CT bans | — (everyone) |

- `<target>`: player name or `#userid`
- `<time>` units: `s` seconds, `m` minutes (default), `h` hours, `d` days, `w` weeks, `mo` months, `y` years

## Data File

Bans are kept in the `CTBanList.json` file inside the plugin folder:

```json
{
  "BannedPlayers": {
    "76561198000000000": {
      "Nickname": "PlayerName",
      "BanTime": 1751630000,
      "Reason": "Rule violation",
      "Admin": "76561198000000001"
    }
  }
}
```

> `BanTime` is the **expiry** time of the ban (Unix timestamp, seconds).

## Usage Examples

```
!ctban Player 30m Freeday rule violation
!ctban #42 2h
!ctaddban 76561198000000000 1d No microphone
!ctunban Player
!ctbanlist
```

## Notes

- When a ban expires the record is deleted automatically the moment the player connects, changes team or spawns.
- File writes are done in the background (async) and do not block the game loop.
