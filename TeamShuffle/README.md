# TeamShuffle

*Read this in [Turkish / Türkçe](README.tr.md).*

Team balancer that shares players out between T and CT according to their strength. Strength is not read from the scoreboard, it is measured live from the damage, kills and MVPs of every round. Team changes do not fire a death event, so nobody loses rank points.

## Features

- Balances the teams by player count and by **damage + kill + MVP + clutch + aim power**
- Statistics are collected live and written to disk, the scoreboard is never used; a player with no records counts as the server average
- Damage is taken from the health the enemy actually lost (an AWP headshot on a full health player is 100 damage)
- Teams are changed with `SwitchTeam` so the player is not killed; the move is applied right before the new round starts
- Automatic shuffle: win streak (`streak`), every X rounds (`interval`) or team power gap (`points`); pistol rounds never trigger it
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
| `css_debugshuffle` | Writes the team powers and the player breakdown to the console | `@css/root` |

`css_karistir` is registered as well by default.

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/TeamShuffle/TeamShuffle.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `shuffle_mode` | string | `"points"` | `off`, `streak`, `interval` or `points` |
| `shuffle_streak_round` | int | `3` | `streak`: shuffles after this many rounds won in a row by one side |
| `shuffle_interval_round` | int | `5` | `interval`: shuffles this many rounds after the previous shuffle |
| `shuffle_points_ratio` | float | `0.3` | `points`: shuffles when the stronger team leads the weaker one by this share (`0.3` = 30%) |
| `shuffle_points_min_round` | int | `3` | `points`: minimum rounds that must pass since the previous shuffle |
| `shuffle_cmd` | string | `"css_shuffle,css_karistir"` | Shuffle commands, separated by commas |
| `shuffle_cmd_flag` | string | `"@css/generic,@css/ban"` | Flags for the shuffle command |
| `shuffle_debug_cmd` | string | `"css_debugshuffle"` | Breakdown commands, separated by commas |
| `shuffle_debug_flag` | string | `"@css/root"` | Flags for the breakdown command |
| `disable_valve_balance` | bool | `true` | Sets `mp_autoteambalance 0` and `mp_limitteams 0` |
| `disable_changeteam` | bool | `true` | Players cannot change their own team, a joining player is placed in the proper team |
| `disable_select_spec` | bool | `true` | Players cannot go to spectator |
| `shuffle_spec_immune_flag` | string | `"@css/ban"` | Players with these flags can still go to spectator; left empty, everyone can |
| `shuffle_min_players` | int | `4` | Below this the plugin does not interfere at all (at least 2) |
| `shuffle_limitteams` | int | `2` | The counts are evened out when the difference reaches this number (at least 2) |
| `shuffle_damage_rating` | int | `1` | Score multiplier of the average damage per round |
| `shuffle_kill_rating` | int | `50` | Score multiplier of the average kills per round |
| `shuffle_mvp_rating` | int | `25` | Score multiplier of the average MVPs per round |
| `shuffle_clutch_rating` | int | `40` | Score multiplier of the weighted clutches per round, `0` = off |
| `shuffle_aim_rating` | int | `60` | Score multiplier of the head hit ratio, `0` = off |
| `shuffle_tolerance_ratio` | float | `0.15` | Power gap up to this share still counts as balanced (`0.15` = 15%) |
| `shuffle_announce` | bool | `true` | Announces the shuffle to everyone in chat |

Messages can be edited through `lang/tr.json` / `lang/en.json`.

## Scoring

```
base  = (damage per round × shuffle_damage_rating)
      + (kills per round × shuffle_kill_rating)
      + (MVPs per round × shuffle_mvp_rating)
      + (weighted clutches per round × shuffle_clutch_rating)
      + (head hit ratio × shuffle_aim_rating)

score = (rounds × base + 5 × server average) / (rounds + 5)
```

Weighted clutches: 1v2 = 1, 1v3 = 2, 1v4 = 3, 1v5 = 5. The head hit ratio is `head / (total hits + 20)`, so a couple of lucky hits do not top the chart. The last line pulls players with few rounds towards the server average.

Players are sorted from strongest to weakest and handed out one by one to whichever team has the lower score at that moment, while the counts are kept equal. If the gap over the weaker team stays below `shuffle_tolerance_ratio`, nobody is moved.

## Usage Example

```
!shuffle
```

> `Teams will be shuffled at the end of the round.`
> At the end of the round: `Teams shuffled (manual), 4 players will switch at the start of the next round.`

## Notes

- Statistics are stored permanently in `players/<steamid>.json`, so a player comes back with their history every time.
- The plugin does nothing during warmup or while the player count is below `shuffle_min_players`; players pick their team freely.
- The blocks only apply to the player's own `jointeam` command; team changes made by admins and other plugins are not blocked.
- Bots are left out of both the shuffle and the scoring.
