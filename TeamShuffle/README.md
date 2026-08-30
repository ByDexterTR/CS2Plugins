# TeamShuffle

*Read this in [Turkish / Türkçe](README.tr.md).*

Team balancer that shares players out between T and CT according to their strength. Strength is not read from the scoreboard, it is measured live from the damage, kills and MVPs of every round. Team changes do not fire a death event, so nobody loses rank points.

## Features

- Balances the teams by player count and by **damage + kill + MVP power**
- Statistics are collected live, the scoreboard is never used; a player with no records counts as the server average
- Damage is taken from the health the enemy actually lost (an AWP headshot on a full health player is 100 damage)
- Teams are changed with `SwitchTeam` so the player is not killed; the move is applied right before the new round starts
- Automatic shuffle: win streak (`streak`) or every X rounds (`interval`); pistol rounds never trigger it
- When the player difference between the teams reaches `shuffle_limitteams`, the counts are evened out at every round start
- Team changing and going to spectator can be blocked, with an immunity flag for spectator
- Valve's own team balance can be switched off
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `TeamShuffle` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/TeamShuffle/
   ```
2. Restart the server or run `css_plugins load TeamShuffle`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_shuffle` | Shuffles the teams; the split is calculated at the end of the round and applied at the start of the next one | `@css/generic` **or** `@css/ban` |
| `css_power` | Tells you the power and player count of both teams | `@css/generic` **or** `@css/ban` |

`css_karistir` and `css_guc` are registered as well by default.

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/TeamShuffle/TeamShuffle.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `shuffle_mode` | string | `"streak"` | `off`, `streak` or `interval` |
| `shuffle_streak_round` | int | `3` | `streak`: shuffles after this many rounds won in a row by one side |
| `shuffle_interval_round` | int | `5` | `interval`: shuffles this many rounds after the previous shuffle |
| `shuffle_cmd` | string | `"css_shuffle,css_karistir"` | Shuffle commands, separated by commas |
| `shuffle_cmd_flag` | string | `"@css/generic,@css/ban"` | Flags for the shuffle command |
| `shuffle_power_cmd` | string | `"css_power,css_guc"` | Power commands, separated by commas |
| `shuffle_power_flag` | string | `"@css/generic,@css/ban"` | Flags for the power command |
| `disable_valve_balance` | bool | `true` | Sets `mp_autoteambalance 0` and `mp_limitteams 0` |
| `disable_changeteam` | bool | `true` | Players cannot change their own team, a joining player is placed in the proper team |
| `disable_select_spec` | bool | `true` | Players cannot go to spectator |
| `shuffle_spec_immune_flag` | string | `"@css/ban"` | Players with these flags can still go to spectator; left empty, everyone can |
| `shuffle_min_players` | int | `4` | Below this the plugin does not interfere at all (at least 2) |
| `shuffle_limitteams` | int | `2` | The counts are evened out when the difference reaches this number (at least 2) |
| `reset_on_map_change` | bool | `true` | Clears the statistics when the map changes |
| `shuffle_damage_rating` | int | `1` | Score multiplier of the average damage per round |
| `shuffle_kill_rating` | int | `50` | Score multiplier of the average kills per round |
| `shuffle_mvp_rating` | int | `25` | Score multiplier of the average MVPs per round |
| `shuffle_balance_tolerance` | int | `10` | Score difference in percent that still counts as balanced |
| `shuffle_announce` | bool | `true` | Announces the shuffle to everyone in chat |

Messages can be edited through `lang/tr.json` / `lang/en.json`.

## Scoring

```
score = (average damage per round × shuffle_damage_rating)
      + (average kills per round × shuffle_kill_rating)
      + (average MVPs per round × shuffle_mvp_rating)
```

Players are sorted from strongest to weakest and handed out one by one to whichever team has the lower score at that moment, while the counts are kept equal. If the score difference is below `shuffle_balance_tolerance` percent, nobody is moved.

## Usage Example

```
!shuffle
```

> `Teams will be shuffled at the end of the round.`
> At the end of the round: `Teams shuffled (manual), 4 players will switch at the start of the next round.`
> `Power: CT 612 - T 598`

## Notes

- Statistics are held in memory per SteamID, so a player who reconnects during the map keeps their record.
- The plugin does nothing during warmup or while the player count is below `shuffle_min_players`; players pick their team freely.
- The blocks only apply to the player's own `jointeam` command; team changes made by admins and other plugins are not blocked.
- Bots are left out of both the shuffle and the scoring.
