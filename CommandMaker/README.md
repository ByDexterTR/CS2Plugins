# CommandMaker

*Read this in [Turkish / Türkçe](README.tr.md).*

Lets you create custom server commands from a JSON file without writing code. Targeted admin commands, info commands, cvar/exec macros and player commands are all defined in a single file.

## Features

- Unlimited custom command definitions in `commands.json`; created with 11 example commands on first run
- 5 command types: `default`, `target`, `playertarget`, `execute`, `menu`
- Over 30 actions: set health/armor/money/speed/gravity, give/strip weapons, teleport, freeze, noclip, godmode, slap, respawn, change model/name, play a sound and more
- Rich placeholder system: player/target info, server info, scores, random player selection
- Chat color tags: `[GOLD]`, `[RED]`, `[GREEN]`, `[ORCHID]` etc.
- Target selectors: name, `#userid`, `@all`, `@ct`, `@t`, `@alive`, `@dead`, `@me`, `@random`, `@aim`, `@nearest`, `@spec`, `@bot`, `@human`, `@!me`
- Per command: permission flags, team filter, alive/dead filter, cooldown, argument validation (number range / word length)
- WASD menus: gather your commands under a single menu command
- `css_cmdlist` lists every command the player is allowed to use
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
| `css_cm_reload` / `css_commandmaker_reload` | Reloads the `commands.json` file | `reload_flag` |
| `css_cmdlist` / `css_komutlar` | Lists the commands the player may use | everyone |
| *(the ones you define)* | Every command in `commands.json` is registered automatically | per definition |

## Configuration

### Main config

```
csgo/addons/counterstrikesharp/configs/plugins/CommandMaker/CommandMaker.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `ConfigPath` | string | `"commands.json"` | Path of the command definition file relative to the plugin folder |
| `reload_cmd` | string | `"css_cm_reload,css_commandmaker_reload"` | Reload command names, separated with commas |
| `reload_flag` | string | `"@css/root"` | Permission flag required for the reload command |
| `list_cmd` | string | `"css_cmdlist,css_komutlar"` | Command list command names, separated with commas |

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
| `type` | string | `default`, `target`, `playertarget`, `execute`, `menu` |
| `args` | int | Number of extra arguments expected (0-3) |
| `arg1..arg3` | string | Argument type: `number`, `float`, `word`, `list`, `player` |
| `argN_number_min` / `argN_number_max` | int | Limits for a `number` / `float` argument |
| `argN_word_length` | int | Maximum length of a `word` argument |
| `argN_list` | string | Allowed values for a `list` argument (`"t,ct,spec"`) |
| `argN_default` | string | Value used when the argument is left out |
| `flag` | string / array | Required permission flags (any one is enough) |
| `target_flag` | string / array | Permission flag required to run the command on another player |
| `ignore_immunity` | bool | Skips the admin immunity check (default `false`) |
| `team_filter` | string | `T` or `CT` — only that team can use it |
| `alive_filter` | string | `alive` or `dead` |
| `cooldown` | float | Per-player cooldown (seconds) |
| `global_cooldown` | float | Server-wide cooldown (seconds) |
| `uses_per_round` | int | How many times one player may use it per round |
| `min_players` | int | Minimum number of real players on the server |
| `warmup_only` / `no_warmup` | bool | Warmup filter |
| `description` | string | The line shown in `css_cmdlist` |
| `announce` | bool | Announce the command usage to the whole server |

#### Command types

| Type | Behavior |
| --- | --- |
| `default` | Takes no target; runs message/`execute`/`setcvar` |
| `target` | The 1st argument is a required target; actions are applied to the target(s) |
| `playertarget` | Target is optional; if not given the player running the command is targeted |
| `execute` | Only runs the `execute`/`setcvar` lines |
| `menu` | Opens a WASD menu built from the `menu` entries |

#### Action fields (applied to the target)

`sethealth`, `setmaxhealth`, `setarmor`, `sethelmet`, `setmoney`, `setclip`, `setammo`, `giveweapon`, `dropweapon`, `stripweapons`, `setfreeze`, `setnoclip`, `setgodmode`, `setmovetype`, `setspeed`, `setgravity`, `kill`, `respawn`, `slapdamage`, `teleport`, `setangle`, `setplayercolor`, `setmodel`, `setname`, `setclantag`, `changeteam`, `addhealth`, `addarmor`, `addmoney`, `screencolor`, `playsound`, `emitsound`

| Action | Value |
| --- | --- |
| `addhealth` / `addarmor` / `addmoney` | Relative change: `"50"` or `"-25"` |
| `setangle` | `pitch yaw roll` |
| `setclantag` | The clan tag text |
| `screencolor` | `R G B alpha fade hold` — e.g. `"255 0 0 90 0.35 0.05"`. Only `R G B` is required; the rest fall back to `90 0.35 0.05` |
| `emitsound` | `soundevent volume` — e.g. `"Player.DamageHelmet 1.0"` |
| `dropweapon` | Drops the active weapon (value is not used) |

The value format is `"[TARGET] <value>"`; e.g. `"sethealth": "[TARGET] [ARG1]"`. The `[TARGET]` prefix is optional — `"sethealth": "[ARG1]"` works the same way.

`setspeed` and `setgravity` are multipliers: `1.0` is normal, `2.0` is double, and the accepted range is `0` - `10`. `sethelmet` takes `true` / `false`.

#### Message fields

| Field | Target |
| --- | --- |
| `chat` | Chat message to the command user (can be an array) |
| `targetchat` | Chat message to the target(s) (can be an array) |
| `targetcenter` | Center screen message to the target(s) |
| `console` | Console message to the command user |
| `center` + `centertime` | Center screen message to the command user |
| `serverchat` | Chat message to the whole server |
| `servercenter` | Center screen message to the whole server (uses `centertime` as well) |
| `execute` | Run a command in the server console |
| `setcvar` | Set a cvar (`"mp_warmuptime 60"`) |

#### Placeholders

- **Player:** `[PLAYER]`, `[PLAYERHEALTH]`, `[PLAYERARMOR]`, `[PLAYERMONEY]`, `[PLAYERSTEAMID]`, `[PLAYERTEAM]`, `[PLAYERWEAPON]`, `[PLAYERCOORDINATE]`
- **Target:** `[TARGET]`, `[TARGETHEALTH]`, `[TARGETARMOR]`, `[TARGETMONEY]`, `[TARGETSTEAMID]`, `[TARGETTEAM]`, `[TARGETWEAPON]`, `[TARGETCOORDINATE]`
- **Arguments:** `[ARG1]`, `[ARG2]`, `[ARG3]`
- **Server:** `[HOSTNAME]`, `[SERVERIP]`, `[SERVERPORT]`, `[MAPNAME]`, `[TIME]`, `[ROUND]`, `[CTSCORE]`, `[TSCORE]`
- **Counts:** `[PLAYERCOUNT]`, `[ALIVECOUNT]`, `[TCOUNT]`, `[CTCOUNT]`, `[SPECCOUNT]`, `[ALIVET]`, `[ALIVECT]`
- **Random:** `[RANDOMPLAYER]`, `[RANDOMT]`, `[RANDOMCT]`, `[RANDOMALIVE]`, `[RANDOMDEAD]`, `[RANDOMTALIVE]`, `[RANDOMTDEAD]`, `[RANDOMCTALIVE]`, `[RANDOMCTDEAD]`
- **Stats:** `[PLAYERKILLS]`, `[PLAYERDEATHS]`, `[PLAYERASSISTS]`, `[PLAYERSCORE]`, `[PLAYERKDR]` and the `[TARGET...]` versions
- **Technical:** `[PLAYERUSERID]`, `[TARGETUSERID]`, `[PLAYERPING]`, `[TARGETPING]`, `[PLAYERCLAN]`, `[TARGETCLAN]`
- **Position:** `[PLAYERANGLE]`, `[TARGETANGLE]`, `[TARGETDISTANCE]`, `[PLAYERAIMTARGET]`
- **Weapon:** `[PLAYERCLIP]`, `[PLAYERAMMO]`, `[TARGETCLIP]`, `[TARGETAMMO]`
- **More server info:** `[MAXPLAYERS]`, `[DATE]`, `[BOTCOUNT]`, `[DEADCOUNT]`, `[DEADT]`, `[DEADCT]`, `[TIMELEFT]`, `[WARMUP]`
- **Random number:** `[RANDOM:1-100]` — picks a number in the given range
- **Colors:** `[DEFAULT]`, `[RED]`, `[LIGHTRED]`, `[DARKRED]`, `[BLUEGREY]`, `[BLUE]`, `[DARKBLUE]`, `[PURPLE]`, `[ORCHID]`, `[YELLOW]`, `[GOLD]`, `[LIGHTGREEN]`, `[GREEN]`, `[LIME]`, `[GREY]`, `[GREY2]`

### Menu commands

```json
{
  "command": "css_adminmenu",
  "type": "menu",
  "flag": "@css/generic",
  "menu_title": "[GOLD]Admin Menu",
  "menu": [
    { "text": "Start warmup", "command": "css_warmup", "flag": "@css/root" },
    { "text": "Refill my health", "command": "css_can" }
  ]
}
```

| Field | Description |
| --- | --- |
| `menu_title` | Menu heading (placeholders and colors work) |
| `text` | Line text |
| `command` | The command run when the line is picked |
| `flag` | Hides the line from players without the flag |
| `close` | Close the menu after picking (default `true`) |

The picked line is run as the player, so the flag, cooldown and filter of that command still apply.

## Usage Examples

```
!hp Player 200        → sets the target's health to 200
!slap @t 10           → slaps every T for 10 damage
!team #42 3           → moves the player with id 42 to CT
!serverinfo           → shows the server info
!can                  → (T, alive, 30 s cooldown) refills your own health
```

## Handy patterns

Some things are not separate fields, they come out of combining what is already there.

| Goal | Definition |
| --- | --- |
| Bring the target to where you stand | `"teleport": "[TARGET] [PLAYERCOORDINATE]"` |
| Give armor + helmet | `"giveweapon": "[TARGET] item_assaultsuit"` |
| Give a defuse kit | `"giveweapon": "[TARGET] item_cutters"` |
| Give armor only | `"giveweapon": "[TARGET] item_kevlar"` |
| Several chat lines | `"chat": ["first line", "second line"]` — `console` and `serverchat` work the same way |
| Kick the target | `"execute": "kickid [TARGETUSERID] reason"` |

`giveweapon` accepts both `weapon_*` and `item_*` names; a name with no prefix is treated as `weapon_*`.

## Notes

- `setspeed` / `setgravity` effects stay on until you change them; to reset one, define a second command that sets the value back to `1.0`.
- `screencolor` paints a colored tint over the screen; a low `alpha` gives a tint, a high one covers the screen.
- `@aim` picks the player you are looking at, `@nearest` the closest one.
- In a `playertarget` command a player can only affect themselves. To let it be used on other players, add a `target_flag` to the definition.
- Admins cannot target players above their own immunity level; add `"ignore_immunity": true` to switch that off for a command.
- Players given `setgodmode` take no damage until they leave the server or it is turned off.
- For group targets (`@all` etc.) the `[TARGET]` placeholder in messages is replaced with the group label.
