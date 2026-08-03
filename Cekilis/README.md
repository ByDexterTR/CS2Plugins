# Cekilis (Raffle)

*Read this in [Turkish / Türkçe](README.tr.md).*

Runs a filtered random raffle among the players on the server. Ideal for Jailbreak events, prize giveaways and similar.

## Features

- 9 different filters: everyone, alive, dead, team, and team+state combinations
- The winner is announced to the whole server (with the admin who ran the raffle and the category)
- Bots are not included in the raffle
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `Cekilis` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/Cekilis/
   ```
2. Restart the server or run `css_plugins load Cekilis`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_cek <filter>` | Picks a random player from the given pool | `@css/chat` |
| `css_cek` | Shows the list of available filters | `@css/chat` |

### Filters

| Filter | Pool |
| --- | --- |
| `all` | All players |
| `live` | All living players |
| `dead` | All dead players |
| `t` | The whole T team |
| `ct` | The whole CT team |
| `tlive` | Living Ts |
| `tdead` | Dead Ts |
| `ctlive` | Living CTs |
| `ctdead` | Dead CTs |

## Configuration

Messages and the chat prefix can be edited through `lang/tr.json` / `lang/en.json`.

## Usage Example

```
!cek tdead
```

> `[ByDexter] AdminName ran a raffle in the TDEAD category: Winner → PlayerName`
