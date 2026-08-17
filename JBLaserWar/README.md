# JBLaserWar

*Read this in [Turkish / Türkçe](README.tr.md).*

Turns the round into a laser war. Bullets stop hurting anyone; every shot sends a laser out of the weapon instead, it bounces off walls and kills whoever it touches. Everything runs from a single menu, played every man for himself or in teams.

## Features

- Bullet damage between players is off while the game is running
- Every shot sends a laser out of the weapon, and it bounces off walls
- The laser travels through the map instead of appearing instantly
- A sound plays when a laser leaves the weapon and at every bounce
- Played every man for himself, or split into 2, 3 or 4 random teams
- Teams get their own name, color and player model
- One shot or two shots to die; a survivor drops to half HP and gets a red flash
- Everyone gets the same weapon, and it comes back on every spawn
- A player who fires a weapon picked up off the ground dies and is called out in chat
- The last player or team standing is announced as the winner
- Starting and stopping is announced in chat
- Weapons, HP and the model go back to normal when the game ends
- Menu choices are saved and come back the same after a restart
- Turkish / English language support (`lang/`)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371
- The `gamedata` file: `addons/counterstrikesharp/gamedata/NativeTrace.gamedata.json`

## Installation

1. Copy the compiled `JBLaserWar` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/JBLaserWar/
   ```
2. Restart the server or run `css_plugins load JBLaserWar`.

## Commands

| Command | Description | Permission |
| --- | --- | --- |
| `css_lw` | Opens the LaserWar menu | `laserwar_flag` |
| `css_laserwar` | Same as `css_lw` | `laserwar_flag` |

### Menu

| Option | Function |
| --- | --- |
| Start The Game / Stop The Game | Starts or ends the laser war |
| Settings | Opens the settings menu |

### Settings

| Option | Function |
| --- | --- |
| Team Count | Every Man For Himself → 2 → 3 → 4 |
| Weapon | Walks through the weapons in `laserwar_weapons` |
| Damage | One Shot → Two Shots |
| Laser Bounces | 1 → 2 → 3 → 4 |
| Infinite Ammo | Whether the guns ever run out of ammo |
| Gravity | Walks through the values in `laserwar_gravity` |
| Laser Sound | The firing and bounce sounds of the laser |

Teams are handed out at random as the game starts; changing the team count while a game is running takes effect on the next game. The game does not start unless the players split evenly into the teams.

Every choice on the menu is saved as soon as it is made.

## Configuration

`addons/counterstrikesharp/configs/plugins/JBLaserWar/JBLaserWar.json`

| Setting | Default | Description |
| --- | --- | --- |
| `laserwar_cmd` | `css_lw,css_laserwar` | Menu commands |
| `laserwar_flag` | `@css/generic,@jailbreak/warden` | Menu permission |
| `laserwar_weapons` | 3 weapons | Weapons offered by the menu; the first one is the default |
| `laserwar_gravity` | `0.3, 0.5, 0.8, 1.0` | Gravity values offered by the menu; the first one is the default, `1.0` is normal gravity |
| `laserwar_max_distance` | `4096` | Distance a laser covers before a bounce |
| `laserwar_hit_radius` | `20` | How close a laser has to pass to count as a hit |
| `laserwar_killfeed_icon` | `spray0` | Icon shown in the killfeed |

The `weapon_` prefix can be left out of the weapon list, it is filled in.

### `laserwar_beam`

| Setting | Default | Description |
| --- | --- | --- |
| `width` | `0.5` | Laser thickness |
| `speed` | `3000` | Laser travel speed |
| `length` | `260` | Length of the visible laser |
| `max_active` | `128` | How many lasers can be in the air at once |

### `laserwar_teams`

Four teams are defined, and as many are used as the team count picked from the menu.

| Setting | Default | Description |
| --- | --- | --- |
| `name` | Sith, Jedi, Mandalor, Klon | Team name shown in chat |
| `color` | `#FF3C28`, `#28C8FF`, `#5CE05C`, `#FFD24A` | Laser and player color of the team (`#RRGGBB` or `R G B`) |
| `model` | four T agents | Player model of the team; left empty the model stays unchanged |

### `laserwar_sound`

| Setting | Default | Description |
| --- | --- | --- |
| `fire` | `Weapon_Taser.ChargeReady_Zap` | Sound played when a laser leaves the weapon |
| `fire_volume` | `1.0` | Volume of the firing sound |
| `bounce` | `FX_RicochetSound.Ricochet_Legacy` | Sound played at every bounce |
| `bounce_volume` | `0.8` | Volume of the bounce sound |

### `laserwar_flash`

The red tint on the screen of a player who takes a laser and lives through it.

| Setting | Default | Description |
| --- | --- | --- |
| `r` / `g` / `b` | `255` / `0` / `0` | Color of the flash |
| `a` | `90` | Strength of the flash; `0` turns it off |
| `duration` | `150` | Fade in time (ms) |
| `hold_time` | `500` | How long it stays on screen (ms) |

## Notes

- Your own laser does not hurt you, so you can shoot a wall right in front of you.
- Only T side players take part.
- Weapons and HP are handed back to those still alive when the game ends.
- The game lasts one round; after that it has to be started again from the menu.
