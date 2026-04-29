# CS2 Plugins

**Languages:** 
- 🇹🇷 [Türkçe](README.md) 
- 🇬🇧 [English](README.en.md)

## 📥 Installation

You can compile the plugins yourself or get the compiled versions from the `.Compiled` folder and upload them directly to your server.

> **Note:** Some plugins use external libraries. Check the plugin descriptions for details.

---

## 1v1Slay

**Description:** Automatic countdown and slay system for 1v1 scenarios

| Command | Description | Permission |
|---------|-------------|-----------|
| Automatic | System runs automatically | None |

**Settings:**
| Setting | Description | Default |
|---------|-------------|---------|
| `chat_prefix` | Chat message prefix | `[ByDexter]` |
| `min_players` | Minimum players for system to be active | `3` |
| `countdown_time` | Countdown duration (seconds) | `30` |
| `enable_announcements` | Enable/disable all announcements | `true` |

---

## Cekilis

**Description:** Random player selection tool

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_cek all` | Select from all players | `@css/chat` |
| `css_cek dead` | Select from dead players | `@css/chat` |
| `css_cek live` | Select from alive players | `@css/chat` |
| `css_cek T` | Select from Terrorist team | `@css/chat` |
| `css_cek CT` | Select from CT team | `@css/chat` |

---

## ChatCleaner

**Description:** Chat message cleanup system

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_cc` | Clear all chat | `@css/chat` |
| `css_selfcc` | Clear own chat | None |

---

## Cit

**Description:** Map barriers for Wardens

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_cit` | Open barrier menu | `@css/root` or `@jailbreak/warden` |

**Requirements:** [CS2TraceRay](https://github.com/schwarper/CS2TraceRay)

---

## CommandMaker

**Description:** JSON-based dynamic command creation system

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_cm_reload` | Reload commands | `@css/root` |

**Command Types:**
| Type | Description |
|------|-------------|
| `default` | Simple commands, no arguments |
| `target` | Requires target player |
| `playertarget` | Optional target player |
| `execute` | Executes server command |

**Validation Options:**
| Validation | Description | Example |
|------------|-------------|----------|
| `arg1: "number"` | Numeric argument | `Arg1NumberMin: 1, Arg1NumberMax: 500` |
| `arg1: "word"` | Word argument | `Arg1WordLength: 20` |
| `flag` | Permission check | `@css/slay;@css/cheats` (multi-flag) |

**Action System:**
| Action | Description | Parameters |
|--------|-------------|------------|
| `sethealth` | Set health | `[TARGET] [ARG1]` |
| `setarmor` | Set armor | `[TARGET] [ARG1]` |
| `setmoney` | Set money | `[TARGET] [ARG1]` |
| `setmaxhealth` | Set max health | `[TARGET] [ARG1]` |
| `setfreeze` | Freeze player | `[TARGET]` |
| `giveweapon` | Give weapon | `[TARGET] [ARG1]` |
| `stripweapons` | Remove weapons | `[TARGET]` |
| `setnoclip` | Toggle noclip mode | `[TARGET] [ARG1]` |
| `setgodmode` | Toggle god mode | `[TARGET] [ARG1]` |
| `kill` | Kill player | `[TARGET]` |
| `setname` | Change name | `[TARGET] [ARG1]` |
| `setmodel` | Change model | `[TARGET] [ARG1]` |
| `changeteam` | Change team | `[TARGET] [ARG1]` |
| `respawn` | Respawn player | `[TARGET]` |
| `setspeed` | Speed multiplier | `[TARGET] [ARG1]` |
| `setgravity` | Set gravity | `[TARGET] [ARG1]` |
| `teleport` | Teleport player | `[TARGET] [ARG1] [ARG2] [ARG3]` (X Y Z) |
| `setplayercolor` | Set player color | `[TARGET] [ARG1]` |
| `slapdamage` | Deal damage | `[TARGET] [ARG1]` |
| `sethelmet` | Set helmet | `[TARGET] [ARG1]` |
| `setclip` | Set ammo | `[TARGET] [ARG1]` |
| `setammo` | Set magazine ammo | `[TARGET] [ARG1]` |
| `playsound` | Play sound | `[TARGET] [ARG1]` |
| `execute` | Execute server command | `say [ARG1]` |

**Message System:**
| Message | Description | Features |
|---------|-------------|----------|
| `chat` | Send to player chat | Color codes: `[GOLD]`, `[DEFAULT]`, etc |
| `center` | Center message (HUD) | Duration set with `centertime` |
| `serverchat` | Send to all players | All players see the message |
| `servercenter` | Center to all players | Notify entire server |

**Placeholders:**
| Placeholder | Description | Example |
|-------------|-------------|----------|
| `[TARGET]` | Target player name | `Player1` |
| `[ARG1]` | 1st argument | `100` |
| `[ARG2]` | 2nd argument | `200` |
| `[ARG3]` | 3rd argument | `300` |
| `[PLAYER]` | Command executor | `Admin1` |
| `[GOLD]` | Gold color code | Color in messages |
| `[DEFAULT]` | Default color | Color in messages |
| `[RED]` | Red color code | Warning messages |
| `[ORCHID]` | Purple color code | Special messages |

**Example Command (Set HP):**
```json
{
  "command": "css_hp;css_health",
  "type": "target",
  "args": 1,
  "arg1": "number",
  "arg1_number_min": 1,
  "arg1_number_max": 500,
  "flag": "@css/slay;@css/cheats",
  "sethealth": "[TARGET] [ARG1]",
  "chat": "[GOLD][TARGET] [DEFAULT]health set to [GOLD][ARG1]",
  "center": "<font color='green'>Health: [ARG1]</font>",
  "centertime": 3.0,
  "announce": false
}
```

**Settings:**
| Setting | Description | Default |
|---------|-------------|---------|
| `ConfigPath` | JSON file with command definitions | `commands.json` |

---

## CTBan

**Description:** CT team ban system

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_ctban <player> <duration>` | Ban player from CT | `@css/ban` |
| `css_ctunban <player>` | Remove CT ban | `@css/ban` |
| `css_ctaddban <player> <duration>` | Add time to CT ban | `@css/ban` |
| `css_ctbanlist` | List banned players | None |

**Settings:**
| Setting | Description |
|---------|-------------|
| `chat_prefix` | Chat message prefix |

---

## CTKit

**Description:** CT team weapon kit menu

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_kit` | Open CT weapon menu | None |

**Settings:**
| Setting | Description | Default |
|---------|-------------|---------|
| `chat_prefix` | Chat message prefix | `[ByDexter]` |
| `default_primary_weapon` | Default primary weapon | `weapon_ak47` |
| `default_secondary_weapon` | Default secondary weapon | `weapon_deagle` |

---

## CTKov

**Description:** Move non-warden CTs to Terrorist team

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_ctkov` | Move CTs to T team | `@css/generic` or `@jailbreak/warden` |

**Settings:**
| Setting | Description | Default |
|---------|-------------|---------|
| `chat_prefix` | Chat message prefix | `[ByDexter]` |

---

## CTPerk

**Description:** CT team perk selection system

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_ctperk` | Open CT perk menu | `@css/generic` or `@jailbreak/warden` |

**Settings:**
| Setting | Description | Default |
|---------|-------------|---------|
| `chat_prefix` | Chat message prefix | `[ByDexter]` |
| `perk_hparmor_hp` | HP & Armor perk HP amount | `200` |
| `perk_hparmor_armor` | HP & Armor perk armor amount | `100` |
| `perk_lifesteal_ratio` | Lifesteal perk ratio | `0.25` |
| `perk_damagereducation_ratio` | Damage reduction perk ratio | `0.25` |
| `perk_damageboost_ratio` | Damage boost perk ratio | `1.50` |

---

## CTRev

**Description:** CT team revive/respawn system

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_ctr` / `css_ctrev` | Open revive menu | `@css/generic` or `@jailbreak/warden` |
| `css_hak0` / `css_haksifir` | Reset revive rights | `@css/generic` |

**Settings:**
| Setting | Description | Default |
|---------|-------------|---------|
| `chat_prefix` | Chat message prefix | `[ByDexter]` |
| `cooldown` | Respawn cooldown (seconds) | `15` |
| `revive_count` | Max revives per round | `3` |

---

## CTSpawnKill

**Description:** Temporary invulnerability for CTs on spawn (prevent spawn kills)

| Command | Description | Permission |
|---------|-------------|-----------|
| Automatic | System runs automatically | None |

**Settings:**
| Setting | Description | Default |
|---------|-------------|---------|
| `chat_prefix` | Chat message prefix | `[ByDexter]` |
| `spawn_protect_seconds` | Spawn protection duration (seconds) | `5` |

---

## DiscordLogger

**Description:** Discord webhook integration for server logging

| Command | Description | Permission |
|---------|-------------|-----------|
| Automatic | System runs automatically | None |

**Settings:**
| Setting | Description |
|---------|-------------|
| `webhook_map` | Webhook URL for map change logs |
| `webhook_connect` | Webhook URL for connection logs |
| `webhook_command` | Webhook URL for command logs |
| `webhook_chat` | Webhook URL for chat logs |
| `webhook_kill` | Webhook URL for kill logs |
| `webhook_round` | Webhook URL for round logs |

---

## JBDoors

**Description:** Quickly open/close all map doors

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_kapiac` | Open all doors | `@css/generic` or `@jailbreak/warden` |
| `css_kapikapat` | Close all doors | `@css/generic` or `@jailbreak/warden` |

---

## JBRace

**Description:** Jailbreak race system

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_race` | Open race menu | `@css/generic` or `@jailbreak/warden` |

**Settings:**
| Setting | Description | Default |
|---------|-------------|---------|
| `chat_prefix` | Chat message prefix | `[ByDexter]` |

---

## JBTeams

**Description:** Jailbreak team system

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_takim <0-5>` | Set number of teams | `@css/generic` |

**Settings:**
| Setting | Description |
|---------|-------------|
| `chat_prefix` | Chat message prefix |

---

## MapBlock

**Description:** Dynamically block map areas based on player count

| Command | Description | Permission |
|---------|-------------|-----------|
| Automatic | System runs automatically | None |

**Settings:**
| Setting | Description | Values |
|---------|-------------|--------|
| `mapblock_mode` | Operating mode | `0`: Off, `1`: CT count, `2`: Total players |
| `mapblock_count` | Player count to trigger | Numeric value |

---

## Meslekmenu

**Description:** Terrorist team profession selection system

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_meslek` | Open profession menu | None |
| `css_meslek doktor` / `css_meslek doctor` | Select Doctor | None |
| `css_meslek flash` | Select Flash | None |
| `css_meslek bombacı` / `css_meslek bomber` | Select Bomber | None |
| `css_meslek rambo` | Select Rambo | None |
| `css_meslek zeus` | Select Zeus | None |

---

## PlayerHourCheck

**Description:** Steam playtime verification with progressive penalty system

| Command | Description | Permission |
|---------|-------------|-----------|
| Automatic | System runs automatically | None |
**Placeholders:**
| Placeholder | Description | Example |
|-------------|-------------|----------|
| `{PlayerPlaytime}` | Player's playtime | `45` (in hours) |
| `{RequiredPlaytime}` | Required minimum playtime | `100` |
| `{PenaltyCount}` | Penalty count | `2` |
| `{Orchid}` | Purple color code | In chat messages |
| `{Gold}` | Gold color code | In warning messages |
| `{Red}` | Red color code | In threat messages |

**Penalty System:**
- Checks player's playtime
- If insufficient, applies progressive penalties
- Warnings are issued before penalties (configurable count)

**Example Config:**
```json
{
  "phc_db": {
    "provider": "sqlite",
    "host": "localhost",
    "name": "cs2_playerhourcheck",
    "port": "3306",
    "user": "root",
    "password": ""
  },
  "phc_chat_prefix": "{Orchid}[ByDexter]",
  "phc_steam_api_key": "Your-Steam-WebAPI-Key-Here",
  "phc_required_playtime": 100,
  "phc_warn_times": 3,
  "phc_warn_enabled": 1,
  "phc_warn_timer": 30,
  "phc_warn_reason_private": "{Gold}Show your game details or you will be kicked. {Red}[{0}/{1}]",
  "phc_penalty": {
    "1": {
      "type": "kick",
      "time": 0,
      "reason": "Insufficient playtime ({PlayerPlaytime}/{RequiredPlaytime} hours)"
    },
    "3": {
      "type": "ban",
      "time": 60,
      "reason": "Insufficient playtime ({PlayerPlaytime}/{RequiredPlaytime} hours)"
    },
    "5": {
      "type": "ban",
      "time": 1440,
      "reason": "Insufficient playtime ({PlayerPlaytime}/{RequiredPlaytime} hours)"
    }
  },
  "phc_ignore_flags": ["@bydexter/ignoreplaytime", "@css/root"],
  "phc_ignore_steamids": ["76561198843494248"]
}
```
**Settings:**
| Setting | Description | Default |
|---------|-------------|---------|
| `phc_required_playtime` | Required minimum playtime (hours) | `100` |
| `phc_warn_enabled` | Enable/disable warning system | `1` |
| `phc_warn_times` | Warning count for private profiles | `3` |
| `phc_warn_timer` | Time between warnings (seconds) | `30` |

**Requirements:** MySQL database, Steam API key (optional)

**Database:** MySQL / SQLite

---

## PlayerRGB

**Description:** RGB color customization for player models

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_rgb` | Open RGB menu | `@css/cheats` |

---

## Redbull

**Description:** Temporary speed and color effect for players

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_redbull` | Enable Redbull effect | None |

**Settings:**
| Setting | Description | Default |
|---------|-------------|---------|
| `chat_prefix` | Chat message prefix | `[ByDexter]` |
| `speed` | Speed multiplier | `2.0` |
| `duration` | Effect duration (seconds) | `10` |
| `round_limiter` | Usage limit per round | `2` |
| `cooldown` | Reuse cooldown (seconds) | `15` |

---

## Sesler

**Description:** Player sound control (knife, weapon, footstep, player/damage sounds)

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_ses` / `css_sesler` | Open sound settings menu | None |

**Settings:**
| Setting | Description | Values |
|---------|-------------|--------|
| `Database.provider` | Database type | `sqlite` / `mysql` |
| `Database.host` | MySQL server address | `localhost` |
| `Database.name` | Database name | `bydexter_sesler` |

**Database:** SQLite / MySQL

---

## Silahsil

**Description:** Clean dropped weapons

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_silahsil` | Clean weapons | `@css/slay` |

---

## Sustum

**Description:** Jailbreak fast-typing race system

| Command | Description | Permission |
|---------|-------------|-----------|
| `css_ctsustum` | Race between CTs | `@css/generic` or `@jailbreak/warden` |
| `css_tsustum` | Race between Terrorists | `@css/generic` or `@jailbreak/warden` |
| `css_dsustum` | Race between dead players | `@css/generic` or `@jailbreak/warden` |
| `css_olusustum` | Race between all players | `@css/generic` or `@jailbreak/warden` |
| `css_sustum0` | Stop race | `@css/generic` or `@jailbreak/warden` |

**Settings:**
| Setting | Description |
|---------|-------------|
| `chat_prefix` | Chat prefix |

---
