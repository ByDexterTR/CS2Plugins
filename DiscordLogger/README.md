# DiscordLogger

*Read this in [Turkish / Türkçe](README.tr.md).*

Forwards server events to Discord webhooks and optionally keeps a daily file log. 10 independent categories, 25+ event types; a separate channel (webhook) can be defined for each category.

## Features

- **10 independent log categories** — map, connect/disconnect, command, chat, kill, round, damage, grenade, C4 and activity
- **Clickable player profiles** — player names link directly to their Steam profile in Discord; bots are shown as plain text with a `(BOT)` tag
- **Detailed kill log** — weapon, hitgroup, HP/armor damage, distance, headshot / noscope / through smoke / blind shot / airborne / wallbang tags, assist (+flash assist)
- **Detailed round summary** — winner, end reason, **MVP**, player count
- **Daily file log** — when `log_to_file` is enabled every active category is also written to `logs/DiscordLogger-YYYY-MM-DD.log`
- **No timestamps/durations in Discord messages** — since Discord already shows the message time no time prefix is added; playtime and round duration are only written to the file log
- **Zero wasted overhead** — event handlers are never registered for a category whose webhook is empty
- Messages are collected in a 3 second buffer and sent in one go (rate-limit friendly, respects the 2000 character limit)
- **Blacklist** for commands and chat; warmup rounds are not logged
- Every message template can be customized from the `lang/` files (including emoji)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

1. Copy the compiled `DiscordLogger` folder to the server:
   ```
   csgo/addons/counterstrikesharp/plugins/DiscordLogger/
   ```
2. Create webhook URLs for the log channels in Discord.
3. Enter the webhook URLs into the config file created on first load.
4. Reload the plugin with `css_plugins reload DiscordLogger`.

## Commands

After a config change `css_plugins reload DiscordLogger` is enough.

## Categories and Events

| Config key | Covered events |
| --- | --- |
| `webhook_map` | Map change |
| `webhook_connect` | Connect, disconnect, name change (`player_changename`) |
| `webhook_command` | Every command (except the blacklist) |
| `webhook_chat` | Chat messages (except the blacklist) |
| `webhook_kill` | `player_death` — with all details |
| `webhook_round` | Round start and end (including `round_mvp`) |
| `webhook_damage` | `player_hurt` (excluding self/world damage) |
| `webhook_grenade` | `grenade_thrown`, `hegrenade_detonate`, `flashbang_detonate`, `player_blind`, `smokegrenade_detonate`, `smokegrenade_expired`, `molotov_detonate`, `decoy_detonate` |
| `webhook_bomb` | `bomb_planted`, `bomb_defused`, `bomb_exploded`, `bomb_dropped`, `bomb_pickup` |
| `webhook_activity` | `player_ping`, `weapon_zoom`, `item_purchase` |

> A category with an empty webhook (and file logging off) is **completely disabled** — its event handlers are not registered and nothing is processed.

## Configuration

```
csgo/addons/counterstrikesharp/configs/plugins/DiscordLogger/DiscordLogger.json
```

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `webhook_map` … `webhook_activity` | string | `""` | Category webhook URLs (the 10 keys in the table) |
| `log_to_file` | bool | `false` | Daily file log (`logs/DiscordLogger-YYYY-MM-DD.log`) |
| `command_blacklist` | list | `["css_wp", "css_knife", ...]` | Commands that will not be logged |
| `chat_blacklist` | list | `["!wp", "!knife", ...]` | Chat patterns that will not be logged |

### Example Config

```json
{
  "webhook_map": "https://discord.com/api/webhooks/....",
  "webhook_connect": "https://discord.com/api/webhooks/....",
  "webhook_command": "https://discord.com/api/webhooks/....",
  "webhook_chat": "https://discord.com/api/webhooks/....",
  "webhook_kill": "https://discord.com/api/webhooks/....",
  "webhook_round": "https://discord.com/api/webhooks/....",
  "webhook_damage": "https://discord.com/api/webhooks/....",
  "webhook_grenade": "https://discord.com/api/webhooks/....",
  "webhook_bomb": "https://discord.com/api/webhooks/....",
  "webhook_activity": "https://discord.com/api/webhooks/....",
  "log_to_file": true,
  "command_blacklist": ["css_wp", "css_knife"],
  "chat_blacklist": ["!wp", "!knife"]
}
```

## Message Format

Player names are clickable Steam profile links and fields are separated with **|**:

```
🟢 ByDexter connected to the server                    (name → profile link)
☠️ Victim ⟵ Killer | Weapon: ak47 | Hitgroup: head | Damage: 108 HP / 12 armor | Distance: 23.4m | [headshot, wallbang x1] | Assist: Player (flash)
🩸 Victim ⟵ Attacker | deagle | left leg | -25 HP / -0 armor | Remaining: 0 HP, 88 armor
😵 Player was blinded | Thrower: Enemy | Duration: 3.2 s
⚡ Flash detonated | Thrower: Player | Position: (512, -128, 64)
🏁 Round 12 ended | Winner: CT | Reason: CTs killed the enemies | MVP: Player | Players: 18
📍 Player pinged | Position: (1024, 256, 32)
✏️ Player changed name: OldName → NewName
```

In the file log the links are flattened into `Name (profile-url)` and duration info is added:

```
[2026-07-04 17:42:10] [Connect] 🔴 ByDexter (https://steamcommunity.com/profiles/7656...) left the server | played for 2 hours 15 minutes
[2026-07-04 17:45:03] [Round] 🏁 Round 12 ended | Winner: CT | ... | Duration: 1 minute 35 seconds
```

## Notes

- **Suicide and self damage** only appear in the Kill channel as `committed suicide`; self/world damage is not written to the Damage channel.
- Bot ↔ bot deaths are logged correctly as killer/victim (bots are identified by slot rather than SteamID).
- Fake `player_blind` events with a duration of 0 are not logged.
- Players affected by a flash land in the `webhook_grenade` channel through the `player_blind` event.
- If the round end reason hits an unknown code, the game's raw notice (a trimmed `#SFUI_Notice_...`) is shown.
- The file log is written in the background on the same 3 second cycle as the Discord send; it does not block the game loop.
