# VIPCore

*Read this in [Turkish / Türkçe](README.tr.md).*

Modular VIP system. Provides a complete VIP infrastructure with more than 75 built-in VIP features (modules), group based permissions, JSON or MySQL storage and three different menu types.

## Features

- **75+ ready-made modules** — all included with the plugin, nothing extra to install
- **Group system** — unlimited groups in `vipgroups.json`; each group decides which modules it gets and with which values
- **Storage** — JSON (default) or MySQL; falls back to JSON automatically if the MySQL connection drops
- **3 menu types** — `hud` (on screen), `chat`, `wasd` (menu navigated with the W/S/E/R keys)
- Timed or permanent VIP; when it expires every feature of the player is turned off and the VIP record **and** the player settings are deleted from storage (JSON/MySQL) automatically
- Per-player feature settings (toggle or selection) are stored persistently
- **Effect visibility** (`css_hidefx`) — every player decides who sees their own trail/particle/glow/sound effects (everyone, teammates, enemies, only themselves, nobody)
- Every command name can be changed from the config
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `VIPCore` folder to the server **together with its dependency DLLs**:
   ```
   csgo/addons/counterstrikesharp/plugins/VIPCore/
   ```
2. Restart the server or run `css_plugins load VIPCore`.
3. On first load `settings.json` and `vipgroups.json` (with the example groups `#Lite`, `#Plus`) are created in the plugin folder.
4. Edit the groups, then add VIPs with `css_addvip`.

## Commands

Command names can be changed from the `commands` section of `settings.json`; the defaults are:

| Command | Description | Permission |
| --- | --- | --- |
| `css_vip` / `css_vipmenu` | Opens the VIP menu and shows the remaining time | VIP |
| `css_vips` / `css_onlinevip` | Lists the online VIPs | — (everyone) |
| `css_viplist` | Lists every VIP record (with durations) | `admin_flag` |
| `css_addvip <steamid64> <group> <time>` | Adds a VIP (`0`/`perm` = permanent; `1h`, `2d`, `1mo`… can be combined) | `admin_flag` |
| `css_removevip <steamid64>` / `css_delvip` | Deletes the VIP record | `admin_flag` |
| `css_reloadvip` / `css_vipreload` | Reloads the config, groups and VIP data | `admin_flag` |
| `css_tp` / `css_thirdperson` | Toggles the third person camera (Thirdperson module) | VIP (if defined in the group) |
| `css_updatevip <steamid64>` / `css_vipupdate` | Re-reads the player's VIP record from storage (JSON/MySQL); applies a change written from a web panel without restarting the server | `admin_flag` |
| `css_hidevip` / `css_hidefx` | Effect visibility menu; the player picks who sees their own effect: Everyone → Teammates → Enemies → Myself → Off. The preference is stored persistently | — (everyone) |
| *(module commands)* | Defined with `module_commands` in `settings.json`; a Toggle module is turned on/off instantly, a selection/category module opens its menu. Can be bound (`bind x "css_fall"`) | VIP (if defined in the group) |

Time units: `s` seconds, `m` minutes (default), `h` hours, `d` days, `w` weeks, `mo` months, `y` years.

## Configuration

### `settings.json` (in the plugin folder)

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `storage` | string | `"json"` | `"json"` or `"mysql"` |
| `menu_type` | string | `"hud"` | `"hud"`, `"chat"` or `"wasd"` |
| `admin_flag` | string | `"@css/root"` | Permission required for the admin commands |
| `commands` | object | — | Command names (multiple aliases separated with commas) |
| `buy_commands` | object | — | BuyTeamWeapon command names; weapon key → comma separated commands (e.g. `"ak47": "css_ak47,css_ak"`) |
| `module_commands` | object | — | Binds a command directly to a module (can be bound). Toggle modules are turned on/off instantly by the command, selection/category modules open their menu. Delete a line you do not want, add a new one as `"ModuleName": "css_command,css_alias"`; if left empty no command is added |
| `hide` | object | — | Effect visibility defaults — who sees that player's own effect: `all` everyone, `team` teammates, `enemy` enemies, `self` only themselves, `hidden` nobody, `off` locked (does not appear in the menu). The player's own preference overrides the default |
| `mysql` | object | — | MySQL connection settings (`host`, `port`, `database`, `user`, `password`, `table_prefix`) |

```json
{
  "storage": "json",
  "menu_type": "hud",
  "admin_flag": "@css/root",
  "commands": {
    "menu": "css_vip,css_vipmenu",
    "list_online": "css_vips,css_onlinevip",
    "list_all": "css_viplist",
    "addvip": "css_addvip,css_vipadd",
    "removevip": "css_removevip,css_delvip",
    "reload": "css_reloadvip,css_vipreload",
    "tp": "css_tp,css_thirdperson",
    "hidevip": "css_hidevip,css_hidefx"
  },
  "module_commands": {
    "GiveWeapon": "css_weapons,css_kit",
    "GlueGrenade": "css_glue,css_gluegrenade",
    "PlayerModel": "css_vipmodel",
    "PlayerParticle": "css_particle",
    "Aura": "css_aura",
    "HitSound": "css_hitsound",
    "SaySound": "css_saysound"
  },
  "hide": {
    "BulletTrail": "all",
    "C4Effect": "team",
    "KillEffect": "all",
    "PlayerTrail": "all",
    "PlayerGlow": "self",
    "GrenadeTrail": "all",
    "SaySound": "all",
    "PlayerParticle": "all"
  },
  "mysql": {
    "host": "",
    "port": 3306,
    "database": "",
    "user": "",
    "password": "",
    "table_prefix": "vip_"
  }
}
```

### `vipgroups.json` (in the plugin folder)

A mapping of group name → module name → module value. A module that is **not defined in a group is disabled for that group**. On first run it is created with the `#Lite` and `#Plus` examples covering every module.

```json
{
  "#Lite": {
    "Armor": { "value": 100, "helmet": false },
    "ExtraHP": 110,
    "Bhop": { "autostrafe": false, "max_speed": 350, "jump_boost": 1.05, "jump_velocity": 300 },
    "Tag": { "tag": "{BlueGrey}[LITE]", "name_color": "bluegrey", "chat_color": "default", "tab": "[LITE]" }
  }
}
```

### Storage files

| Storage | Location |
| --- | --- |
| JSON | `vips.json` (VIP records) and `players.json` (player settings) in the plugin folder |
| MySQL | Tables are created automatically with the `table_prefix` prefix; the record is refreshed live when the player joins. `vip_users` holds one row per player, `vip_settings` keeps all of a player’s settings in a single JSON column. An old install with one row per setting is migrated to the new layout on startup |

## Modules

Module names are used as keys in `vipgroups.json` (case sensitive).

| Module | Description | Example group value |
| --- | --- | --- |
| `AdminFlags` | Gives the VIP permission flags automatically | `["@css/reservation", "@css/vip"]` |
| `AdminGroups` | Gives the VIP admin group membership (`#Group` names from SimpleAdmin etc.) | `["#VIP"]` |
| `AntiFlash` | Blocks flashbangs | `{ "self": true, "enemy": true, "teammates": true, "limit": 0 }` |
| `AntiHS` | Reduces headshot damage | `{ "percent": 0, "only_with_weapon": "", "limit": 0 }` |
| `Armor` | Armor (+helmet) on spawn | `{ "value": 100, "helmet": true }` |
| `ArmorRegen` | Armor regeneration | `{ "armor_per_tick": 10, "interval": 1.0, "delay_after_dmg": 2, "max_armor": 100, "give_helmet_when_full": true }` |
| `Aura` | A constant area effect around the player (heal/poison/slow/speed); the area is shown with a ring and blinks with `duration_on`/`duration_off` | `{ "heal": { "heal": 2, "tick": 0.5, "radius": 180, "beamcolor": "0 255 0", "duration_on": 1, "duration_off": 0, "ignore_teammates": false, "ignore_self": false, "ignore_enemy": true } }` |
| `AutoHS` | Hits count as headshots | `{ "multiplier": 4, "only_with_weapon": "", "ignore_teammates": true, "limit": 0 }` |
| `Berserk` | The damage multiplier grows per kill; `dpk` is the multiplier added per kill, `maxdpk` the cap | `{ "dpk": 0.2, "maxdpk": 5.0 }` |
| `Bhop` | Bunny hop (+optional autostrafe) | `{ "autostrafe": true, "max_speed": 500, "jump_boost": 1.1, "jump_velocity": 300 }` |
| `BombsiteAnnouncer` | HUD image (visual only) + chat message to CTs when the bomb is planted | `{ "img_a": "...Site-A.png", "img_b": "...Site-B.png", "duration": 5.0 }` |
| `BulletEffect` | The effect picked from the menu is applied to whoever you hit: `poison`, `slow`, `lower` (shrink), `upper` (enlarge). Hitting again extends the duration | `{ "poison": { "damage": 2, "tick": 0.5, "duration": 3, "ignore_teammates": true, "ignore_self": true, "ignore_enemy": false }, "slow": { "percent": 20, "duration": 3 }, "lower": { "size": 0.85, "duration": 5 }, "upper": { "size": 1.25, "duration": 5 }, "only_with_weapon": "" }` |
| `BulletTrail` | Bullet trail effect | `{ "width": 1.5, "lifetime": 0.6, "colors": [...] }` |
| `BuyTeamWeapon` | Buying the other team's weapons (only inside the buyzone and before `mp_buytime` expires); command names come from `buy_commands` in `settings.json` | `{ "ak47": true, "m4a4": true, ... }` |
| `C4Effect` | Particle effect while planting and defusing the bomb; two separate categories, an empty one is hidden from the menu | `[{ "name": "Duman", "particle": "...", "time": 6, "defuse": false }]` |
| `ColoredModel` | Colored player model; backs off if another plugin (e.g. jRandomSkills) changes the color | `["Rainbow rainbow", "Mavi #0000FF"]` |
| `CustomWeaponModel` | Custom look for a weapon; a number also changes the model in your hands, a file path only changes the one on the ground | `[{ "name": "M4A4 - AK47", "weapon": "weapon_m4a1", "model": "weapons/models/ak47/weapon_rif_ak47.vmdl" }]` |
| `DamageDealt` | Increases the damage dealt; **negative `percent` = debuff** (`-50` halves the damage dealt) | `{ "percent": 50, "only_with_weapon": "", "ignore_teammates": true, "ignore_self": true, "limit": 0 }` |
| `DamageResist` | Reduces the damage taken; **negative `percent` = debuff** (`-50` increases the damage taken by 50%) | `{ "percent": 40, "only_with_weapon": "", "ignore_teammates": true, "ignore_self": true, "limit": 0 }` |
| `Dash` | Pressing jump while airborne dashes you toward the direction key you are holding (forward if none); `limit`: budget per round (0 = unlimited), `unit`: push speed, `sound_volume`: jump sound volume (0 = silent) | `{ "limit": 3, "unit": 600, "sound_volume": 1 }` |
| `DecoyEffect` | Gives the decoy a feature: poison, healing, slowing or wallhack. The area is shown with a ring on the ground that grows with `radius` | `{ "poison": { "minhp": 10, "damage": 2, "tick": 0.5, "radius": 200, "ignore_teammates": true, "ignore_self": true, "limit": 0 }, "wallhack": { "tick": 0.25, "radius": 200, "color": "#612D53", "see_teammates": false, "limit": 0 } }` |
| `DecoyTeleport` | Teleport to where the decoy landed | `{ "limit": 3 }` |
| `DefuseKit` | Defuse kit on spawn (CT) | `true` |
| `DuckEndurance` | Unlimited crouching; crouching repeatedly never slows down | `true` |
| `DuckSpeed` | Movement speed while crouched; `percent` is how much of the normal run speed is kept. The game's own value is `34`, `100` = crouching does not slow you down | `{ "percent": 100 }` |
| `ExtraHP` | Spawn HP value | `150` |
| `ExtraJump` | Multi jump; `count` is the extra jumps per airtime, total budget = `count × limit` (`limit: 0` = unlimited). `Dash` takes priority when both are on. `sound_volume`: jump sound volume (0 = silent) | `{ "count": 2, "limit": 0, "sound_volume": 1 }` |
| `ExtraKillAwards` | Extra money by how you killed: headshot, noscope, in the air, blinded enemy, per weapon and by distance | `{ "headshot": 150, "noscope": 100, "inair": 200, "blind": 50, "distance": { "unit": 2048, "money": 100 }, "weapon_knife": 1000 }` |
| `ExtraMoney` | Extra money on spawn | `{ "amount": 4000 }` |
| `ExtraSpeed` | Speed multiplier | `{ "multiplier": 1.3, "only_with_weapon": "" }` |
| `FallDamage` | Takes `percent` of the fall damage (`0` = none, `100` = normal); **negative = debuff** (`-50` increases fall damage by 50%); `limit` is how many times per round (0 = unlimited) | `{ "percent": 0, "limit": 0 }` |
| `FastDefuse` | Fast bomb defuse; with `immune_while_burning: false` the speed advantage is disabled while burning / near fire or an airborne molotov | `{ "time": 1, "immune_while_burning": true }` |
| `FastPlant` | Fast bomb plant; with `immune_while_burning: false` the speed advantage is disabled while burning / near fire or an airborne molotov | `{ "time": 1, "immune_while_burning": true }` |
| `FastReload` | The magazine empties normally; on the last bullet it refills instantly from the reserve (1 magazine is taken from the reserve) | `{ "only_with_weapon": "", "limit": 0 }` |
| `FortniteArmor` | Damage hits armor first. `percent` is how much of the damage goes to armor; once armor runs out the rest goes to health | `{ "percent": 100, "absorb_fall_damage": false }` |
| `Fov` | FOV options | `[50, 60, 70, 80, 90]` |
| `GiveWeapon` | Weapon selection on spawn, one per category. With "Force Give" on in the menu the weapon in that slot is replaced | `{ "rifle": ["weapon_ak47", "weapon_awp"], "pistol": ["weapon_deagle"] }` |
| `GiveZeus` | Taser on spawn | `true` |
| `Glaz` | See through smoke | `true` |
| `GlueGrenade` | Thrown grenades stick on first contact (if you add decoy, there is a risk of teleporting inside a wall with DecoyTeleport) | `{ "only_grenades": "flashbang,hegrenade", "limit": 0 }` |
| `Gravity` | Gravity options | `[1.0, 0.8, 0.5]` |
| `GrenadeKit` | Grenade set on spawn; not given if already held, with 2+ it is given again after throwing (not re-given while InfiniteAmmo is on) | `{ "flash": 2, "smoke": 1, "he": 3, "molotov": 1, "decoy": 0 }` |
| `GrenadeResist` | Reduces grenade (HE/molotov/inferno) damage; **negative `percent` = debuff** (`-50` increases grenade damage by 50%) | `{ "percent": 50, "only_with_grenade": "he,molotov,inferno", "ignore_teammates": true, "ignore_self": true, "limit": 0 }` |
| `GrenadeTrail` | Grenade trail effect | `{ "width": 1.5, "lifetime": 2.5, "colors": [...] }` |
| `HealthRegen` | Health regeneration | `{ "hp_per_tick": 10, "interval": 1.0, "delay_after_dmg": 2 }` |
| `Healthshot` | Healthshot on spawn | `2` |
| `HitSound` | Plays a sound when you hit an enemy; spectators hear it too. 2 categories: `hs: true` entries on headshots, the rest on normal hits. If no HS sound is picked the normal one plays. `path` is a file path, `emit` a soundevent name | `[{ "name": "Killcard", "path": "sounds/ui/killcard_1.vsnd" }, { "name": "Ping", "emit": "UI.PlayerPing", "volume": 1, "hs": true }]` |
| `InfiniteAmmo` | Infinite ammo | `{ "only_weapon": "" }` |
| `Invisibility` | Invisibility (not transmitted to enemies) | `{ "only_stopped": true, "dmg_after_invis": 2.0, "only_with_weapon": "" }` |
| `Jammer` | Disables the radar of approaching players (`radius` range); if a dead spectator is watching someone jammed their radar is disabled too | `{ "radius": 500, "ignore_teammates": true, "ignore_enemy": false }` |
| `JoinMessage` | Join/leave announcement | `{ "join_message": "...", "leave_message": "..." }` |
| `KillEffect` | Particle effect on a kill; separate categories for normal, headshot and last kill. With nothing picked it falls back to the previous category | `[{ "name": "Simsek", "particle": "...", "time": 3, "hs": false, "lastkill": false }]` |
| `KillHeal` | Restores health by kill type: an `hp` (or `money`) key inside `distance` | `{ "headshot": 15, "noscope": 10, "inair": 20, "blind": 5, "distance": { "unit": 2048, "hp": 10 }, "weapon_knife": 50 }` |
| `KillScreen` | Kill screen effect (does not work on a teammate while FFA is off) | `{ "duration": 1.0 }` |
| `MagneticDecoy` | While the decoy is on the ground and beeping it pulls everyone within `radius` toward it; the pull weakens with distance (`strength` is the base force); `limit` is how many decoys per round | `{ "radius": 180, "strength": 30, "ignore_teammates": true, "ignore_enemy": false, "ignore_self": true, "limit": 0 }` |
| `Mole` | The damaged player is buried `unit` units into the ground for `time` seconds and cannot move; `limit` is how many burials per round (0 = unlimited) | `{ "time": 2.5, "unit": 30, "only_with_weapon": "weapon_deagle", "ignore_teammates": true, "ignore_enemy": false, "ignore_self": true, "limit": 0 }` |
| `OneShot` | One shot kill with specific weapons | `{ "weapons": "weapon_awp,weapon_ssg08", "limit": 0 }` |
| `PistolRoundDisable` | The listed modules are disabled on pistol rounds (a group setting, not a module) | `["GiveWeapon", "WeaponAmmo"]` |
| `Force` | The listed **Toggle** modules are always active; they are not shown in the menu and the player cannot toggle them (a group setting, not a module; the module must be defined in the group; selection/command based modules are not affected) | `["Dash", "ExtraHP"]` |
| `PlayerGlow` | Player glow (glow through walls) | `{ "range": 300, "team": -1, "colors": [...] }` |
| `Postprocessing` | Applies a colour/tone effect to the screen; only the player and whoever spectates them sees it. `fade` is the transition time in seconds | `[{ "name": "Kanli", "file": "lighting/postprocessing/effects/death_cam_phase1.vpost", "fade": 0.25 }]` |
| `PlayerParticle` | A particle attached to the player that follows them; removed on death and at round start (can be hidden with `css_hidefx`). `offset` is its height above the ground. Pick a continuously emitting (loop) particle, one-shot bursts do not follow | `[{ "name": "Duman", "particle": "particles/ambient_fx/ambient_smokestack.vpcf", "offset": 10 }]` |
| `PlayerModel` | Player model selection per team (separate CT and T menus); `leg: false` hides the first person legs. Applied on spawn only | `{ "ct": [{ "name": "Special Agent Ava", "model": "agents/models/ctm_swat/ctm_swat_variante.vmdl", "arm": "", "leg": true }], "t": [...] }` |
| `PlayerSize` | Player size selection; applied on spawn only; left alone if the size was already changed by another plugin | `[0.5, 0.75, 1.25, 1.5]` |
| `PlayerTrail` | Player movement trail | `{ "width": 1.5, "lifetime": 2.5, "colors": [...] }` |
| `Pyro` | The VIP's molotov/incendiary restores health instead of dealing damage (`multiplier` × damage; above 1 it nets health) | `{ "multiplier": 1.5, "ignore_teammates": false, "ignore_enemy": true, "ignore_self": false, "limit": 0 }` |
| `RadarHack` | Shows every enemy (and the C4) on the radar; blinks with `duration_on`/`duration_off` (`duration_off: 0` = always on, `duration_on` at least 1 s) | `{ "duration_on": 1, "duration_off": 0 }` |
| `RapidFire` | `firepercent` is the fire rate (`0.1` – `2.0`): `1.0` normal, `2.0` fastest, below that is slower. `recoilpercent` is the recoil left (`0.0` – `1.0`): `0.0` none, `1.0` normal | `{ "only_with_weapon": "", "recoilpercent": 0.0, "firepercent": 2.0 }` |
| `ReflectDamage` | Damage reflection | `{ "reflect_percent": 50, "max_per_shot": 100, "only_with_weapon": "", "ignore_teammates": true, "ignore_self": true, "limit": 0 }` |
| `Respawn` | A dead player respawns after `time` seconds; `limit` is the budget per round (0 = unlimited), cancelled when the round changes | `{ "limit": 1, "time": 3 }` |
| `Sacrifice` | When the VIP dies, gives living teammates health (capped at their own MaxHealth), armor (+helmet with `helmet`) and the weapons in the `weapons` list | `{ "hp": 25, "armor": 25, "helmet": false, "weapons": "weapon_hegrenade,weapon_flashbang" }` |
| `SaySound` | Plays a sound when a chat message is sent (`say` to everyone, `say_team` to the team); `cooldown` in seconds, `0` = no wait; `path` is a file path or `emit` is a soundevent name (`volume` only applies to `emit`); the old flat list is also supported | `{ "cooldown": 2, "sounds": [{ "name": "Beep", "path": "sounds/ui/beepclear.vsnd" }, { "name": "Sohbet", "emit": "UI.Lobby.Chat", "volume": 1 }] }` |
| `Silent` | Hides footsteps from other players | `{ "only_with_weapon": "" }` |
| `SmokeColor` | Colored smoke grenade; left alone if another plugin already set the smoke color | `["Beyaz #FFFFFF", "Kirmizi #FF0000"]` |
| `SmokeEffect` | Gives smoke a feature: poison, healing, slowing or wallhack smoke. `time` is how long the effect lasts (0 = until the smoke fades), `radius` the area, `limit` the per-round budget | `{ "poison": { "minhp": 10, "damage": 2, "time": 20, "tick": 0.5, "radius": 180, "smokecolor": [255, 0, 255], "ignore_teammates": true, "ignore_self": true, "limit": 0 }, "heal": { "heal": 2, "time": 20, "tick": 0.5, "radius": 180, "smokecolor": [0, 255, 0], "ignore_teammates": false, "ignore_self": false, "ignore_enemy": true, "limit": 0 }, "slow": { "percent": 30, "time": 20, "minspeed": 100, "radius": 180, "smokecolor": [0, 0, 255], "ignore_teammates": true, "ignore_self": true, "ignore_enemy": false, "limit": 0 }, "wallhack": { "time": 20, "tick": 0.25, "radius": 180, "smokecolor": [97, 45, 83], "color": "#612D53", "see_teammates": false, "limit": 0 } }` |
| `SpawnProtection` | Spawn protection; `time` seconds, `limit` how many times per round (0 = unlimited) | `{ "time": 4, "limit": 0 }` |
| `Spy` | Wears the model of a random enemy | `true` |
| `Tag` | Chat tag/colors + scoreboard (TAB) tag (if `tab` is empty TAB is left alone) | `{ "tag": "{Gold}[{Orchid}PLUS{Gold}]", "name_color": "gold", "chat_color": "default", "tab": "[PLUS]" }` |
| `TeamHeal` | Healing instead of damage when shooting a teammate | `{ "minhp": 5, "percent": 50, "sound_volume": 0.5, "only_with_weapon": "" }` |
| `Thirdperson` | Third person camera | `{ "distance": 120 }` |
| `WallHack` | Shows enemies glowing through walls. Blinks with `duration_on`/`duration_off` (`duration_off: 0` = always on), `see_teammates` also shows teammates, `color` is the glow colour | `{ "duration_on": 1, "duration_off": 3, "color": "#612D53", "see_teammates": false }` |
| `Vampire` | Steals health equal to the damage dealt | `{ "heal_percent": 75, "only_with_weapon": "", "max_overheal": 120, "ignore_teammates": true }` |
| `VIPChat` | Private chat channel for VIPs | `true` |
| `WeaponAmmo` | Per-weapon custom magazine/reserve ammo (on most weapons reserve = number of magazines; on nova/sawedoff/xm1014 it is shells). Works with plugins that destroy and re-give weapons (WeaponPaints `css_wp`), the ammo is kept | `[{ "weapon_name": "weapon_ak47", "ammo": 30, "reserve": 3 }]` |
| `ZeusCooldown` | Shortens the Zeus recharge time (`limit`: budget per round, 0 = unlimited) | `{ "cooldown": 5, "limit": 0 }` |

## Usage Examples

```
!addvip 76561198000000000 #Plus 1mo   → 1 month of Plus VIP
!addvip 76561198000000000 #Lite 0     → permanent Lite VIP
!vip                                  → VIP menu + remaining time
!viplist                              → every record
!removevip 76561198000000000          → delete the record
```

## Notes

- The config files are **inside the plugin folder** (`settings.json`, `vipgroups.json`), not in CounterStrikeSharp's `configs/plugins` directory.
- A module that is not defined in any group does not run at all.
- The sound modules (`HitSound`, `SaySound`) support two methods: `path` plays your own sound file, `emit` plays one of the game's built-in sounds. If both are given, `emit` wins. Not every built-in sound name works with `emit`; known working ones are `UI.PlayerPing`, `UI.Lobby.Chat`, `UI.CompetitiveAccept` and `UI.CoinLevelUp`. With both methods the sound only reaches the players it should, so `css_hidefx` preferences and the `say_team` filter work either way.
- Effect sounds (poison, healing, jump) are only heard by the affected player and their volume is set with `sound_volume`; `0` plays nothing.
- The `"Rainbow rainbow"` and `"Rastgele random"` entries can be used in the color lists (`ColoredModel`, `PlayerGlow`, `PlayerTrail`, `BulletTrail`, `GrenadeTrail`, `SmokeColor`). If random is selected the player is assigned one shared color per round — the model, glow, trails and smoke all use the same color that round.
