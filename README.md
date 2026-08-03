# CS2Plugins

*Read this in [Turkish / Türkçe](README.tr.md).*

Every plugin ships with Turkish/English language support, lives in its own folder as a standalone project, and is documented with its own `README.md`.

## Plugins

### 🔒 Jailbreak

| Plugin | Description |
| --- | --- |
| [CTBan](CTBan/README.md) | Temporarily bans players from the CT (guard) team |
| [CTKit](CTKit/README.md) | Weapon kit given to CTs automatically on spawn |
| [CTKov](CTKov/README.md) | Moves every CT except the warden to T with a single command |
| [CTPerk](CTPerk/README.md) | Round-based CT perks unlocked according to the T count |
| [CTRev](CTRev/README.md) | Revives dead CTs from a menu or automatically |
| [CTSpawnKill](CTSpawnKill/README.md) | Short damage protection for CTs after spawn |
| [GoBhop](GoBhop/README.md) | Teleports dead Ts to a hidden bhop area |
| [JBDoors](JBDoors/README.md) | Opens/closes every cell door with a single command |
| [JBRace](JBRace/README.md) | Race event with start and finish points |
| [JBTeams](JBTeams/README.md) | Splits living Ts into colored teams |
| [Meslekmenu](Meslekmenu/README.md) | One job per round for Ts: Doctor, Flash, Bomber, Rambo, Zeus |
| [Sustum](Sustum/README.md) | Typing event with 4 modes (CTSustum, TSustum, DSustum, DeadSustum) |
| [Cit](Cit/README.md) | Menu for placing fence/barricade models where you are looking |
| [Silahsil](Silahsil/README.md) | Clears unowned weapons on the ground with a single command |
| [Cekilis](Cekilis/README.md) | Random player raffle filtered by team/state |

### ⚙️ General / Utility

| Plugin | Description |
| --- | --- |
| [1v1Slay](1v1Slay/README.md) | Countdown during a 1v1; slays the remaining players when time runs out |
| [AntiCapsLock](AntiCapsLock/README.md) | Lowercases excessive caps in chat or deletes the message |
| [AntiTeamFlash](AntiTeamFlash/README.md) | Stops teammate flashbangs from blinding you |
| [BhopDoorFix](BhopDoorFix/README.md) | Freezes doors on bhop/KZ maps |
| [ChatCleaner](ChatCleaner/README.md) | Clears your own screen or the whole server chat |
| [CommandMaker](CommandMaker/README.md) | Creates custom server commands from JSON without writing code |
| [FortniteArmor](FortniteArmor/README.md) | Damage hits armor first; health only drops once armor is gone |
| [HideTeammates](HideTeammates/README.md) | Hides teammates (or enemies/everyone) |
| [Lazer](Lazer/README.md) | Shows dead players where living players are aiming with a laser |
| [MapBlock](MapBlock/README.md) | Fences off map areas while the player count is low |
| [PlayerRGB](PlayerRGB/README.md) | RGB (rainbow) effect on the player model |
| [Postprocessing](Postprocessing/README.md) | Per-player post processing effect; 106 ready-made effects (bloom, blur, color, zoom) |
| [PrivateMessage](PrivateMessage/README.md) | Private messages between players (!pm) |
| [Redbull](Redbull/README.md) | Timed speed boost with limit and cooldown support |
| [ScreenText](ScreenText/README.md) | Persistent text defined in JSON, placed anywhere on screen |
| [Sesler](Sesler/README.md) | Mutes knife/weapon/footstep/player/MVP sounds by category |
| [Slowmode](Slowmode/README.md) | Server-wide chat slow mode; enforces a second limit between messages |
| [SpawnkillProtection](SpawnkillProtection/README.md) | Flag and team based spawn protection with color transitions |
| [Speedometer](Speedometer/README.md) | Live speed readout (u/s) on the HUD with color transitions |
| [Thirdperson](Thirdperson/README.md) | Third person camera with wall blocking |

### 🛡️ Infrastructure / Administration

| Plugin | Description |
| --- | --- |
| [AdminList](AdminList/README.md) | Lists online admins with group tags and colors; groups come from config |
| [BringGoto](BringGoto/README.md) | Teleport players to you (bring) and teleport to a player (goto) |
| [DiscordLogger](DiscordLogger/README.md) | Logs 35+ server events to 10 Discord webhook channels and a daily file |
| [PlayerHourCheck](PlayerHourCheck/README.md) | CS2 playtime check with tiered kick/ban punishments |
| [VIPCore](VIPCore/README.md) | Group based VIP system with 75+ modules and JSON/MySQL support |
| [TABServerName](TABServerName/README.md) | Changes the map name shown in the top left of the scoreboard |

## Installation (Server)

1. CounterStrikeSharp must be installed on the server.
2. Copy the compiled folder of the plugin you want:
   ```
   csgo/addons/counterstrikesharp/plugins/<PluginName>/
   ```
3. Restart the server or run `css_plugins load <PluginName>`.
4. See the plugin's own README for its specific settings.

## Building

```powershell
# Single plugin
dotnet build 1v1Slay/1v1Slay.csproj -c Debug

# Output: <Plugin>/bin/Debug/ → ready to copy to the server
```

## Repository Layout

```
CS2Plugins/
├── <PluginName>/            # Every plugin in its own folder
│   ├── <PluginName>.csproj
│   ├── <PluginName>.cs
│   ├── README.md            # Plugin documentation (English)
│   ├── README.tr.md         # Plugin documentation (Turkish)
│   └── lang/                # tr.json / en.json language files
├── img/                     # HUD/menu icons (used via raw URL)
└── LICENSE                  # MIT
```
