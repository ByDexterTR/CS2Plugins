# Ads

*Read this in [Turkish / Türkçe](README.tr.md).*

Places props on the map, draws messages on screen and sends announcements to chat. Besides timed ads it also fires instant ads on game events; when several ads of the same kind are defined they queue up instead of overlapping.

## Features

- **Prop ad**: places a model where you are aiming through the `css_ads` menu; the prop stays fixed in place, it never falls or gets pushed around
- **Split file layout**: ads in `ads.json`, the prop catalog in `props.json`, everything placed on a map in `maps.json`, settings in `settings.json`
- **In game editing**: the `css_ads` menu places props and edits their axis-based rotation/movement, scale, collision, skin and flags
- **Flag system**: every ad kind takes `flag` (only these players see it) and `ignoreflag` (these players do not); a player who should not see a prop is never sent it at all
- **ScreenText**: world text pinned to the player's screen; position (x/y), size, color, justification and background are adjustable
- **HudSay**: HTML capable text in the center of the screen (`<br>`, `<font color>`, `class='fontSize-m'`)
- **ChatSay**: color coded (`{Lime}`, `{Orchid}` …) chat announcements
- **Event ads**: instant ChatSay, HudSay or ScreenText on 10 game events, aimed at a target (victim / attacker / team / everyone); per-player cooldown, percentage chance and placeholders such as `{victim}`, `{damage}`, `{winner}`
- **Modular**: only the parts you actually use are activated
- **Collision free queue system**: each channel (ScreenText / HudSay / ChatSay) shows only one ad at a time; `ads_queue_mode: "global"` merges all three into a single queue; an event ad takes precedence over the rotating ad for that one player only, for as long as it lasts
- Per map filter (`map`), `*` for every map
- Two way JSON ↔ MySQL transfer (`css_adsimportsql` / `css_adsexportsql`)
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `Ads` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/Ads/
   ```
2. Restart the server or run `css_plugins load Ads`.
3. `settings.json`, a sample `ads.json`, a sample `props.json` and an empty `maps.json` are created in the plugin folder automatically on first load.

All four files live in `csgo/addons/counterstrikesharp/plugins/Ads/`; the plugin has no file under `configs/plugins/`.

| File | Content | Written by |
| --- | --- | --- |
| `settings.json` | Settings (commands, queue mode, MySQL credentials) | By hand; per server, never pushed to MySQL |
| `ads.json` | ScreenText / HudSay / ChatSay / event ads | By hand |
| `props.json` | The prop catalog (the list shown in the menu) | By hand |
| `maps.json` | Props placed on maps | The menu |

## Queue System (Collision Prevention)

When several ads are defined they all enter a **queue**, so they never pile up on top of each other. The queue rotates like this: it waits for `timer`, shows the ad for `life`, then moves on to the next one.

Rules:

- Only **one ad is active at a time** in a queue. While a ScreenText is on screen, a second ScreenText is never opened.
- `timer` is how long to wait before that ad appears, counted from the moment the previous ad closed (the gap in between).
- `life` is how long the ad stays on screen. ChatSay has no `life`; the message is printed and the queue moves on immediately.
- When the end of the queue is reached it wraps around. One full round lasts `sum(life + timer)`.

`ads_queue_mode` decides how many queues exist:

| Mode | Behaviour |
| --- | --- |
| `channel` (default) | Three separate queues: ScreenText, HudSay, ChatSay. Each kind is sequential within itself; different kinds may appear at the same time (they use different screen areas, so they do not collide) |
| `global` | A single queue. ScreenText + HudSay + ChatSay rotate in one list, so **two ads are never visible at the same time** |

Example: two ScreenTexts (`life 8 / timer 30` and `life 6 / timer 20`) in `channel` mode rotate like this:

```
wait 30s -> text 1 for 8s -> wait 20s -> text 2 for 6s -> wait 30s -> text 1 ...
```

## Event Ads

Next to the rotating ads, instant ads bound to game events are defined in the `events` section. They do not enter a queue; they fire the moment the event happens, **only for the target players**, and for their `life` duration they take precedence over that player's rotating ScreenText/HudSay ad. When the duration ends the rotating ad continues where it left off, so event ads do not collide with rotating ads either.

### Supported events

| `event` | When | Usable targets |
| --- | --- | --- |
| `player_hurt` | A player takes damage | `victim`, `attacker`, `both`, `all`, `ct`, `t` |
| `player_death` | A player dies | `victim`, `attacker`, `both`, `all`, `ct`, `t` |
| `round_start` | The round starts | `all`, `ct`, `t` |
| `round_end` | The round ends | `all`, `ct`, `t` |
| `bomb_plant` (`bomb_beginplant`) | The bomb starts being planted | `player`, `all`, `ct`, `t` |
| `bomb_planted` | The bomb is planted | `player`, `all`, `ct`, `t` |
| `bomb_defuse` (`bomb_begindefuse`) | The bomb starts being defused | `player`, `all`, `ct`, `t` |
| `bomb_defused` | The bomb is defused | `player`, `all`, `ct`, `t` |
| `player_connect_full` | A player is fully connected (2s delay) | `player`, `all` |
| `player_team` | A player changes team | `player`, `all`, `ct`, `t` |

### Targets

| `target` | Who receives it |
| --- | --- |
| `all` | Everyone on the server |
| `victim` | The player who took damage / died |
| `attacker` | The player who dealt the damage / the kill |
| `player` | The player the event belongs to (planter, joiner, team switcher) |
| `both` | Both the victim and the attacker |
| `ct` / `t` | Everyone on that team |

### Fields

| Field | Default | Description |
| --- | --- | --- |
| `event` | — | One of the event names above |
| `target` | `"all"` | Target |
| `type` | `"chatsay"` | Display kind: `chatsay`, `hudsay`, `screentext` |
| `text` | — | Message; supports placeholders and color tags |
| `life` | `4` | How long the HudSay/ScreenText stays (unused for ChatSay) |
| `cooldown` | `10` | Seconds before this ad may fire **again** for the same player; `0` = no limit. Re-triggering while the ad is still on that player's screen skips the cooldown and refreshes the text instead (names stay current on back-to-back kills) |
| `chance` | `100` | Trigger chance in percent (0-100) |
| `flag` / `ignoreflag` | `""` | See [Flag System](#flag-system) |
| `x` / `y` / `size` / `color` / `justify` / `background` | — | Only for `type: "screentext"` |

`player_hurt` can fire dozens of times per second; always set `cooldown` on it.

### Event placeholders

| Placeholder | Where |
| --- | --- |
| `{victim}` | `player_hurt`, `player_death` — name of the damaged/killed player |
| `{attacker}` | `player_hurt`, `player_death` — name of the attacking player |
| `{player}` | Name of the player the event belongs to (attacker when there is no victim) |
| `{damage}` `{health}` `{armor}` | `player_hurt` |
| `{weapon}` | `player_hurt`, `player_death` |
| `{headshot}` | `player_death` (`1` / `0`) |
| `{winner}` | `round_end` (`T` / `CT` / `Draw`) |
| `{site}` | `bomb_planted`, `bomb_defused` (`A` / `B`); empty during `bomb_plant` because the bomb is not planted yet |
| `{kit}` | `bomb_defuse` (`1` / `0`) |
| `{team}` | `player_team` (`T` / `CT` / `Spectator`) |
| `{map}` | Every event |

## Placeholders

These work everywhere: ScreenText, HudSay, ChatSay and event ads.

| Placeholder | Value |
| --- | --- |
| `{map}` `{hostname}` `{ip}` `{port}` `{maxplayers}` | Server info |
| `{players}` `{bots}` | Human / bot count |
| `{alive}` `{dead}` | Alive / dead |
| `{t_count}` `{ct_count}` `{spec_count}` | Team sizes |
| `{alive_t}` `{alive_ct}` `{dead_t}` `{dead_ct}` | Alive / dead per team |
| `{round}` `{t_score}` `{ct_score}` | Round and score |
| `{time}` `{date}` | `HH:mm` and `dd.MM.yyyy` |
| `{player}` `{steamid}` `{team}` | The player seeing the ad |
| `{kills}` `{deaths}` `{assists}` `{score}` | Stats of the player seeing the ad |

`{players}` counts humans only, the other counts include bots.

Values are calculated at different times depending on the channel:

| Channel | When |
| --- | --- |
| ChatSay | While the ad is printed — live value |
| HudSay | Every `ads_hud_tick` — keeps updating |
| ScreenText | Once when the ad appears — stays fixed for that ad |

So `{time}` and the counters do not change in ScreenText; use HudSay for live values.

Unknown `{...}` tokens are left untouched, so color tags (`{Orchid}` etc.) are not broken. If the text has no `{`, nothing is processed.

## Commands

There are four commands; everything else is done from the `css_ads` menu.

| Command | Description | Permission |
| --- | --- | --- |
| `css_ads` | Opens the ads menu | `ads_flag` |
| `css_adsreload` | Reloads ads and props from the active source (JSON or MySQL) | `ads_flag` |
| `css_adsimportsql` | Imports `ads.json` + `props.json` + `maps.json` into MySQL (**replaces the whole tables**) | `ads_flag` |
| `css_adsexportsql` | Exports MySQL into those three files (keeps the old ones as `.backup`) | `ads_flag` |

### Menus

Navigate with **W/S**, select with **E**, exit with **R**. The menu stays open after every selection, so you can run several actions in a row. In the submenus **Back** is always the first row and is drawn in red.

```
css_ads
├─ Place Prop ............. prop catalog
├─ Edit Prop
│   ├─ Select a prop
│   ├─ Prop Placement
│   │   ├─ Reposition
│   │   ├─ Axis: X / Y / Z   (axis picker)
│   │   ├─ Rotate +/-
│   │   └─ Move +/-
│   ├─ Prop Properties
│   │   ├─ Scale up / down
│   │   ├─ Collision on / off
│   │   ├─ Change skin
│   │   ├─ Change flag
│   │   └─ Change ignoreflag
│   └─ Delete prop
├─ SQL Operations
│   ├─ Import the Json files
│   └─ Export into the Json files
└─ Plugin Management
    ├─ Reload props
    ├─ Reload ads
    └─ Reload settings
```

**Place Prop**: the catalog is listed one model per row. Selecting a model places it where you are aiming and saves it to `maps.json`. The catalog's `scale`/`skin`/`solid`/`flag`/`ignoreflag` values are copied onto the placed prop.

A prop always spawns at angle `"0 0 0"`; your view direction never leaks into it. Orientation is done from the Prop Placement menu.

**Edit Prop**: **Select a prop** picks the prop closest (within 128 units) to your aim point, and its name appears in brackets on the row. **Delete prop** removes the selected prop.

#### Prop Placement

Rotation and movement act on the **selected axis**. The axis is kept per player; selecting the **Axis** row cycles X → Y → Z → X and the row shows the current one.

| Row | What it does |
| --- | --- |
| Reposition | Moves the selection to the point you are aiming at |
| Axis: X / Y / Z | Changes the axis the two rows below act on |
| Rotate +/- | Turns by ± `ads_rotate_step` degrees on the selected axis |
| Move +/- | Nudges by ± `ads_move_step` units on the selected axis |

What each axis means:

| Axis | Rotate | Move |
| --- | --- | --- |
| X | Pitch (tilt forward/back) | World X |
| Y | Yaw (turn left/right) | World Y |
| Z | Roll (tilt sideways) | Height |

Rotation always lands on an exact multiple of the step: +90° from `45.32` gives `90`, not `135.32`. Fractional angles typed by hand are cleaned up on the first rotation.

Movement is on world axes, so the same row always moves the prop the same way no matter where you stand. Use Reposition for coarse placement and Move for fine tuning.

#### Prop Properties

| Row | What it does |
| --- | --- |
| Scale up / down | `scale` ± `ads_scale_step` (never below 0.05) | `width` and `height` scale by the same factor |
| Collision on / off | Flips `solid`; the row shows the current state | — |
| Change skin | Moves to the next value in the catalog's `skins` list | — |
| Change flag | The menu closes and you type the new value **in chat** | same |
| Change ignoreflag | The menu closes and you type the new value **in chat** | same |

Selecting the flag/ignoreflag row closes the menu, and the first message you type in chat is stored as the value; that message is not printed to chat. Type `-` to clear it. The menu reopens by itself once the value is saved.

The skin list comes from `props.json` → `models` → `skins` (`"skins": [0, 1, 2]`). The row warns you when the list is missing.

On scale, collision and skin changes the prop briefly disappears and comes back. Rotation and movement move the prop instantly with no respawn. `maps.json` is saved on every step. The selection is per player and is cleared on map change.

#### SQL Operations and Plugin Management

The two rows in **SQL Operations** do the same as the `css_adsimportsql` / `css_adsexportsql` commands.

**Plugin Management** runs the pieces of `css_adsreload` separately, so you only refresh the section you edited:

| Row | What is reloaded |
| --- | --- |
| Reload props | `maps.json` → `props`; world props are recreated |
| Reload ads | `ads.json` → `screentexts` + `hudsays` + `chatsays` + `events`; the queue resets |
| Reload settings | `settings.json`; every setting except command names takes effect immediately |

## Configuration

```
csgo/addons/counterstrikesharp/plugins/Ads/settings.json
```

Settings are per server: `css_adsimportsql` / `css_adsexportsql` never touch this file and it has no MySQL counterpart. Servers sharing one database can use different command names, queue modes and permissions.

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `ads_cmd` | string | `"css_ads"` | Main menu command; comma separated command names |
| `ads_rotate_step` | float | `90` | Rotation step used by the Prop Angle menu (degrees) |
| `ads_move_step` | float | `5` | Nudge step used by the Prop Position menu (units) |
| `ads_storage` | string | `"json"` | Source the ads are read from: `json` or `mysql` |
| `ads_queue_mode` | string | `"channel"` | Queue mode: `channel` or `global` |
| `ads_flag` | string | `"@css/root"` | Permission required by every admin command |
| `ads_scale_step` | float | `0.25` | Scale step used by the Prop Properties menu |
| `ads_reload_cmd` | string | `"css_adsreload"` | Comma separated command names |
| `ads_importsql_cmd` | string | `"css_adsimportsql"` | Comma separated command names |
| `ads_exportsql_cmd` | string | `"css_adsexportsql"` | Comma separated command names |
| `ads_hud_tick` | int | `4` | How many ticks between HudSay refreshes (4 = 16 per second) |
| `ads_font` | string | `"Arial Bold"` | ScreenText font |
| `ads_forward` | float | `7` | ScreenText distance from the eye (minimum 1) |
| `ads_units_per_px` | float | `0.012` | ScreenText pixel scale |
| `mysql.host` | string | `""` | MySQL server address |
| `mysql.port` | uint | `3306` | MySQL port |
| `mysql.database` | string | `""` | Database name (created if missing) |
| `mysql.user` | string | `""` | User name |
| `mysql.password` | string | `""` | Password |
| `mysql.table_prefix` | string | `"ads_"` | Table prefix; tables become `ads_props`, `ads_events` … |

### Example settings.json

```json
{
  "ads_cmd": "css_ads",
  "ads_rotate_step": 90,
  "ads_move_step": 5,
  "ads_scale_step": 0.25,
  "ads_storage": "json",
  "ads_queue_mode": "channel",
  "ads_flag": "@css/root",
  "ads_reload_cmd": "css_adsreload",
  "ads_importsql_cmd": "css_adsimportsql",
  "ads_exportsql_cmd": "css_adsexportsql",
  "ads_hud_tick": 4,
  "ads_font": "Arial Bold",
  "ads_forward": 7,
  "ads_units_per_px": 0.012,
  "mysql": {
    "host": "127.0.0.1",
    "port": 3306,
    "database": "cs2",
    "user": "root",
    "password": "",
    "table_prefix": "ads_"
  }
}
```

## Flag System

Every ad kind (`maps.json` → `props`; `ads.json` → `screentexts`, `hudsays`, `chatsays`, `events`) has two fields:

| Field | Meaning |
| --- | --- |
| `flag` | When not empty, **only** players holding this permission see the ad |
| `ignoreflag` | Players holding this permission do **not** see the ad; it wins over `flag` |

Examples:

```json
{ "flag": "", "ignoreflag": "" }                     // everyone sees it
{ "flag": "@css/vip", "ignoreflag": "" }             // only VIPs see it
{ "flag": "", "ignoreflag": "@css/vip" }             // everyone except VIPs
{ "flag": "@css/vip", "ignoreflag": "@css/root" }    // VIPs see it, root does not
```

Both fields take several comma separated permissions (`"@css/vip,@css/generic"`); holding **any** of them is enough.

The two fields treat `@css/root` **differently on purpose**:

| Field | Root behaviour |
| --- | --- |
| `flag` | Root is covered. A root player also sees an ad with `flag: "@css/generic"` — root counts as holding every permission |
| `ignoreflag` | Root is not covered. `ignoreflag: "@css/vip"` only hides the ad from players who actually have `@css/vip` assigned; a root player whose own permission list lacks it still sees the ad |

The reason: `flag` is an *access* check and root reaches everything, while `ignoreflag` is an *exemption* list — letting root fall into every exemption would mean admins never see any ad. So `ignoreflag` reads the raw permission list and the root wildcard is not applied.

The queue system is unaffected by flags: the rotation advances at the same time for everyone, only the display is filtered per player. So when a ScreenText with `flag: "@css/vip"` comes up, non-VIPs simply see nothing while the queue keeps moving.

## Catalog File

```
csgo/addons/counterstrikesharp/plugins/Ads/props.json
```

The model list shown in the Place Prop menu. This file is edited by hand only; the plugin never writes to it.

```json
{
  "Chicken": {
    "path": "models/chicken/chicken.vmdl",
    "skins": [0]
  },
  "Vending machine": {
    "path": "models/props/cs_office/vending_machine.vmdl"
  },
  "Stone statue": {
    "path": "models/generic/stone_statue_01/stone_statue_01.vmdl"
  },
  "Sauce bottle (VIP only)": {
    "path": "models/de_mirage/food/magixx_sauce_01a/magixx_sauce_bottle_01a.vmdl",
    "flag": "@css/vip"
  }
}
```

| Section | Field | Description |
| --- | --- | --- |
| `props` | `path` | Path of the model file (`.vmdl`) |
| | `map` | Map the prop appears on; `*` for every map, comma separated for several |
| | `pos` / `angle` | Position and angle as `"X Y Z"`; `angle` is in `"pitch yaw roll"` order |
| | `scale` / `skin` | Model scale and skin index |
| | `solid` | When `false` players walk through it |
| all | `flag` / `ignoreflag` | See [Flag System](#flag-system) |

## Ads File

```
csgo/addons/counterstrikesharp/plugins/Ads/ads.json
```

This file holds the ads printed to the screen and to chat, plus the event ads. It is created even when `ads_storage` is `mysql`; ads are written here and pushed to the database with `css_adsimportsql`. Props live in [props.json](#catalog-file) and [maps.json](#map-file) instead.

```json
{
  "screentexts": [
    {
      "text": "bydexter.net\nGitHub: github.com/ByDexterTR",
      "life": 8,
      "timer": 30,
      "x": -6.4,
      "y": 1.3,
      "size": 32,
      "color": "#FFFFFF",
      "justify": "left",
      "background": true
    },
    {
      "text": "Support <br> our server",
      "life": 6,
      "timer": 20,
      "x": -6.4,
      "y": 1.3,
      "size": 28,
      "color": "#7CFC00",
      "justify": "left",
      "background": false
    }
  ],
  "hudsays": [
    {
      "text": "<font color='#7CFC00' class='fontSize-m'>bydexter.net</font><br>GitHub: github.com/ByDexterTR",
      "life": 6,
      "timer": 45
    }
  ],
  "chatsays": [
    {
      "text": "{Orchid}[Ad]{Default} Visit {Lime}bydexter.net{Default} to support the server.",
      "timer": 60
    }
  ],
  "events": [
    {
      "event": "player_death",
      "target": "attacker",
      "type": "hudsay",
      "text": "<font color='#7CFC00' class='fontSize-m'>You killed {victim}</font><br>bydexter.net",
      "life": 3,
      "cooldown": 5,
      "chance": 100,
      "ignoreflag": ""
    },
    {
      "event": "player_hurt",
      "target": "attacker",
      "type": "screentext",
      "text": "-{damage} HP\nbydexter.net",
      "life": 2,
      "cooldown": 1,
      "chance": 100,
      "x": 0,
      "y": -1.2,
      "size": 24,
      "color": "#FF6347",
      "justify": "center",
      "background": false
    },
    {
      "event": "round_end",
      "target": "all",
      "type": "hudsay",
      "text": "<font color='#FFD700' class='fontSize-m'>{winner} won</font><br>bydexter.net",
      "life": 4,
      "cooldown": 0
    },
    {
      "event": "bomb_planted",
      "target": "all",
      "type": "chatsay",
      "text": "{Orchid}[Ad]{Default} The bomb was planted at {Red}{site}{Default}. {Lime}bydexter.net",
      "cooldown": 0
    },
    {
      "event": "player_connect_full",
      "target": "player",
      "type": "chatsay",
      "text": "{Orchid}[Ad]{Default} Welcome {Lime}{player}{Default}! GitHub: {Blue}github.com/ByDexterTR",
      "cooldown": 0
    }
  ]
}
```

Every model in the samples ships with CS2 as a **stock** asset, so no workshop package is required. The positions in the `maps.json` sample come from de_mirage's real spawn areas, so the props are visible on de_mirage right after install.

### Fields

| Section | Field | Description |
| --- | --- | --- |
| `screentexts` | `text` | Use `\n` or `<br>` for a new line |
| | `life` | How many seconds it stays on screen |
| | `timer` | How many seconds after the previous ad closed it appears |
| | `x` / `y` | Screen position; negative `x` = left, positive `y` = up |
| | `size` / `color` / `justify` | Font size, color (`#RRGGBB` or `R G B`), justification (`left`/`center`/`right`) |
| | `background` | Draws a dark panel behind the text |
| `hudsays` | `text` | Supports HTML: `<br>`, `<font color='#RRGGBB'>`, `class='fontSize-m'`, `<img src='...'>` |
| | `life` / `timer` | Same logic as ScreenText |
| `chatsays` | `text` | Color tags such as `{Orchid}`, `{Lime}`, `{Default}`; `\n` for multiple lines |
| | `timer` | How many seconds after the previous message it is printed |
| `events` | — | See [Event Ads](#event-ads) |
| all | `flag` / `ignoreflag` | See [Flag System](#flag-system) |

## MySQL

When `ads_storage` is set to `mysql`, every JSON section maps to its own table. The file layout is mirrored exactly in the database: `ads.json`, `props.json` and `maps.json` are transferred as three separate groups, and no group is touched when another one is written. `settings.json` has no counterpart.

| Table | Source | Content |
| --- | --- | --- |
| `ads_screentexts` | `ads.json` → `screentexts` | ScreenText ads |
| `ads_hudsays` | `ads.json` → `hudsays` | HudSay ads |
| `ads_chatsays` | `ads.json` → `chatsays` | ChatSay ads |
| `ads_events` | `ads.json` → `events` | Event ads |
| `ads_propmodels` | `props.json` → `models` | Prop catalog |
| `ads_props` | `maps.json` → `props` | Placed props |

The table prefix comes from `mysql.table_prefix`. The table columns match the fields in the JSON files one to one, so you can edit the same settings straight from the database.

Writes are separate as well: the menu only rewrites the placed prop table (`ads_props`) and never touches the catalog, screen, chat or event tables.

The database and the tables are created automatically on first load. The workflow is:

1. Write the screen/chat/event ads into `ads.json` and the catalogs into `props.json`.
2. Push them to MySQL with `css_adsimportsql` (the matching tables are wiped and refilled).
3. Set `ads_storage` to `mysql` and run `css_adsreload`.
4. Use `css_adsexportsql` to pull the rows back into the files; `ads.json`, `props.json` and `maps.json` are rewritten in the same layout.

While `ads_storage` is `mysql` the menu catalogs and the placed entries are read from the database too; the JSON files are then only used as the transfer source/target. `settings.json` is always read from disk.

## Notes

- Props stay fixed in place, they never fall or get pushed around. Set `solid: false` and players walk through them.
- When you add a new model to `props.json`, it only becomes usable on the next map change.
- The default catalog uses models that ship with CS2 only, so no extra package is needed. If you add your own model, the file must exist both on the server and on the players' side (workshop map or addon package), otherwise the prop stays invisible.
- While a menu is open no HudSay ad is printed to that player.
- The Prop Angle menu rotates left/right and up/down. To tilt sideways, edit the third value of the `angle` field in `maps.json` by hand.
- A `player_death` ad cannot combine `target: "victim"` with `type: "screentext"`; screen text cannot be shown to a dead player. Use `chatsay` or `hudsay` instead.
- `ignoreflag` does not cover root: a root player still sees the ad unless that flag is actually assigned to them. The `flag` check always covers root.
- Every player only sees their own ScreenText on their own screen. No screen text is shown to dead players.
- HudSay uses the center area of the screen; if another plugin (menu, warning) uses the same area, the two are printed alternately and may flicker.
- `settings.json` is refreshed instantly from **Reload settings**; only command names, `ads_storage` and the MySQL connection need a plugin reload.
- When entering a flag/ignoreflag through chat, nobody else's message can change your setting.
