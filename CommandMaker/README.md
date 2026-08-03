# CommandMaker

*Read this in [Turkish / Türkçe](README.tr.md).*

Lets you create custom server commands from a JSON file without writing code. Targeted admin commands, info commands, cvar/exec macros and player commands are all defined in a single file.

## Features

- Unlimited custom command definitions in `commands.json`; created with 11 example commands on first run
- 4 command types: `default`, `target`, `playertarget`, `execute`
- Nearly 30 actions: set health/armor/money/speed/gravity, give/strip weapons, teleport, freeze, noclip, godmode, slap, respawn, change model/name, play a sound and more
- Rich placeholder system: player/target info, server info, scores, random player selection
- Chat color tags: `[GOLD]`, `[RED]`, `[GREEN]`, `[ORCHID]` etc.
- Target selectors: name, `#userid`, `@all`, `@ct`, `@t`, `@alive`, `@dead`, `@me`, `@random`
- Per command: permission flags, team filter, alive/dead filter, cooldown, argument validation (number range / word length)
- Reload the commands without restarting the server
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `CommandMaker` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/CommandMaker/
   ```
2. Restart the server or run `css_plugins load CommandMaker`.
3. On first load a `commands.json` full of examples is created in the plugin folder; edit it and run `!cm_reload`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_cm_reload` | Reloads the `commands.json` file | `@css/root` |
| *(the ones you define)* | Every command in `commands.json` is registered automatically | per definition |

## Configuration

### Main config

```
csgo/addons/counterstrikesharp/configs/plugins/CommandMaker/CommandMaker.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `ConfigPath` | string | `"commands.json"` | Path of the command definition file relative to the plugin folder |

### Command definition file (`commands.json`)

```json
{
  "Commands": [
    {
      "command": ["css_hp", "css_health"],
      "type": "target",
      "args": 1,
      "arg1": "number",
      "arg1_number_min": 1,
      "arg1_number_max": 500,
      "flag": ["@css/slay", "@css/cheats"],
      "cooldown": 3,
      "sethealth": "[TARGET] [ARG1]",
      "chat": ["[GOLD][TARGET][DEFAULT]'s health was set to [GOLD][ARG1][DEFAULT]."],
      "center": "<font color='green'>Health: [ARG1]</font>",
      "centertime": 3.0
    }
  ]
}
```

#### General fields

| Field | Type | Description |
| --- | --- | --- |
| `command` | string / array | Command names (can also be separated with `;`) |
| `type` | string | `default`, `target`, `playertarget`, `execute` |
| `args` | int | Number of extra arguments expected (0-3) |
| `arg1..arg3` | string | Argument type: `number` or `word` |
| `argN_number_min` / `argN_number_max` | int | Limits for a number argument |
| `argN_word_length` | int | Maximum length of a word argument |
| `flag` | string / array | Required permission flags (any one is enough) |
| `team_filter` | string | `T` or `CT` — only that team can use it |
| `alive_filter` | string | `alive` or `dead` |
| `cooldown` | float | Per-player cooldown (seconds) |
| `announce` | bool | Announce the command usage to the whole server |

#### Command types

| Type | Behavior |
| --- | --- |
| `default` | Takes no target; runs message/`execute`/`setcvar` |
| `target` | The 1st argument is a required target; actions are applied to the target(s) |
| `playertarget` | Target is optional; if not given the player running the command is targeted |
| `execute` | Only runs the `execute`/`setcvar` lines |

#### Action fields (applied to the target)

`sethealth`, `setmaxhealth`, `setarmor`, `sethelmet`, `setmoney`, `setclip`, `setammo`, `giveweapon`, `stripweapons`, `setfreeze`, `setnoclip`, `setgodmode`, `setmovetype`, `setspeed`, `setgravity`, `kill`, `respawn`, `slapdamage`, `teleport`, `setplayercolor`, `setmodel`, `setname`, `changeteam`, `playsound`

The value format is usually `"[TARGET] <value>"`; e.g. `"sethealth": "[TARGET] [ARG1]"`.

#### Message fields

| Field | Target |
| --- | --- |
| `chat` | Chat message to the command user (can be an array) |
| `console` | Console message to the command user |
| `center` + `centertime` | Center screen message to the command user |
| `serverchat` | Chat message to the whole server |
| `servercenter` | Center screen message to the whole server |
| `execute` | Run a command in the server console |
| `setcvar` | Set a cvar (`"mp_warmuptime 60"`) |

#### Placeholders

- **Player:** `[PLAYER]`, `[PLAYERHEALTH]`, `[PLAYERARMOR]`, `[PLAYERMONEY]`, `[PLAYERSTEAMID]`, `[PLAYERTEAM]`, `[PLAYERWEAPON]`, `[PLAYERCOORDINATE]`
- **Target:** `[TARGET]`, `[TARGETHEALTH]`, `[TARGETARMOR]`, `[TARGETMONEY]`, `[TARGETSTEAMID]`, `[TARGETTEAM]`, `[TARGETWEAPON]`, `[TARGETCOORDINATE]`
- **Arguments:** `[ARG1]`, `[ARG2]`, `[ARG3]`
- **Server:** `[HOSTNAME]`, `[SERVERIP]`, `[SERVERPORT]`, `[MAPNAME]`, `[TIME]`, `[ROUND]`, `[CTSCORE]`, `[TSCORE]`
- **Counts:** `[PLAYERCOUNT]`, `[ALIVECOUNT]`, `[TCOUNT]`, `[CTCOUNT]`, `[SPECCOUNT]`, `[ALIVET]`, `[ALIVECT]`
- **Random:** `[RANDOMPLAYER]`, `[RANDOMT]`, `[RANDOMCT]`, `[RANDOMALIVE]`, `[RANDOMDEAD]`, `[RANDOMTALIVE]`, `[RANDOMTDEAD]`, `[RANDOMCTALIVE]`, `[RANDOMCTDEAD]`
- **Colors:** `[DEFAULT]`, `[RED]`, `[LIGHTRED]`, `[DARKRED]`, `[BLUEGREY]`, `[BLUE]`, `[DARKBLUE]`, `[PURPLE]`, `[ORCHID]`, `[YELLOW]`, `[GOLD]`, `[LIGHTGREEN]`, `[GREEN]`, `[LIME]`, `[GREY]`, `[GREY2]`

## Usage Examples

```
!hp Player 200        → sets the target's health to 200
!slap @t 10           → slaps every T for 10 damage
!team #42 3           → moves the player with id 42 to CT
!serverinfo           → shows the server info
!can                  → (T, alive, 30 s cooldown) refills your own health
```

## Notes

- `setspeed` / `setgravity` effects are persistent (applied per tick); to reset them define a second command that sets the value to `1.0`.
- Players given `setgodmode` take no damage until they leave the server or it is turned off.
- For group targets (`@all` etc.) the `[TARGET]` placeholder in messages is replaced with the group label.
