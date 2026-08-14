# CS2Plugins

*Read this in [Turkish / Türkçe](README.tr.md).*

44 CounterStrikeSharp plugins for CS2 servers. All of them ship with Turkish and English language support, each one can be installed on its own, and each has its own README.

## Plugins

| Plugin | Description | Category | Requirements |
| --- | --- | --- | --- |
| [1v1Slay](1v1Slay/README.md) | Countdown during a 1v1; slays the remaining players when time runs out | General | CounterStrikeSharp |
| [AdminList](AdminList/README.md) | Lists online admins with group tags and colors; groups come from config | Admin | CounterStrikeSharp |
| [Ads](Ads/README.md) | Props on the map, ScreenText and HudSay on screen, chat announcements; event driven ads, JSON/MySQL | General | CounterStrikeSharp, gamedata, MySQL (optional) |
| [AntiCapsLock](AntiCapsLock/README.md) | Lowercases excessive caps in chat or deletes the message | General | CounterStrikeSharp |
| [AntiTeamFlash](AntiTeamFlash/README.md) | Stops teammate flashbangs from blinding you | General | CounterStrikeSharp |
| [BhopDoorFix](BhopDoorFix/README.md) | Freezes doors on bhop/KZ maps | General | CounterStrikeSharp |
| [BringGoto](BringGoto/README.md) | Teleport players to you (bring) and teleport to a player (goto) | Admin | CounterStrikeSharp, gamedata |
| [Cekilis](Cekilis/README.md) | Random player raffle filtered by team/state | Jailbreak | CounterStrikeSharp |
| [ChatCleaner](ChatCleaner/README.md) | Clears your own screen or the whole server chat | General | CounterStrikeSharp |
| [Cit](Cit/README.md) | Menu for placing fence/barricade models where you are looking | Jailbreak | CounterStrikeSharp, gamedata |
| [CommandMaker](CommandMaker/README.md) | Creates custom server commands from JSON without writing code | General | CounterStrikeSharp |
| [CTBan](CTBan/README.md) | Temporarily bans players from the CT (guard) team | Jailbreak | CounterStrikeSharp |
| [CTKit](CTKit/README.md) | Weapon kit given to CTs automatically on spawn | Jailbreak | CounterStrikeSharp |
| [CTKov](CTKov/README.md) | Moves every CT except the warden to T with a single command | Jailbreak | CounterStrikeSharp |
| [CTPerk](CTPerk/README.md) | Round-based CT perks unlocked according to the T count | Jailbreak | CounterStrikeSharp |
| [CTRev](CTRev/README.md) | Revives dead CTs from a menu or automatically | Jailbreak | CounterStrikeSharp |
| [CTSpawnKill](CTSpawnKill/README.md) | Short damage protection for CTs after spawn | Jailbreak | CounterStrikeSharp |
| [DiscordLogger](DiscordLogger/README.md) | Logs 35+ server events to 10 Discord webhook channels and a daily file | Admin | CounterStrikeSharp |
| [FortniteArmor](FortniteArmor/README.md) | Damage hits armor first; health only drops once armor is gone | General | CounterStrikeSharp |
| [GoBhop](GoBhop/README.md) | Teleports dead Ts to a hidden bhop area | Jailbreak | CounterStrikeSharp |
| [HideTeammates](HideTeammates/README.md) | Hides teammates (or enemies/everyone) | General | CounterStrikeSharp |
| [JBDoors](JBDoors/README.md) | Opens/closes every cell door with a single command | Jailbreak | CounterStrikeSharp |
| [JBRace](JBRace/README.md) | Race event with start and finish points | Jailbreak | CounterStrikeSharp |
| [JBTeams](JBTeams/README.md) | Splits living Ts into colored teams | Jailbreak | CounterStrikeSharp |
| [Lazer](Lazer/README.md) | Shows dead players where living players are aiming with a laser | General | CounterStrikeSharp, gamedata |
| [MapBlock](MapBlock/README.md) | Fences off map areas while the player count is low | General | CounterStrikeSharp |
| [Meslekmenu](Meslekmenu/README.md) | One job per round for Ts: Doctor, Flash, Bomber, Rambo, Zeus | Jailbreak | CounterStrikeSharp |
| [PlayerHourCheck](PlayerHourCheck/README.md) | CS2 playtime check with tiered kick/ban punishments | Admin | CounterStrikeSharp, MySQL (optional), an admin plugin with kick/ban |
| [PlayerRGB](PlayerRGB/README.md) | RGB (rainbow) effect on the player model | General | CounterStrikeSharp |
| [Postprocessing](Postprocessing/README.md) | Per-player post processing effect; 106 ready-made effects (bloom, blur, color, zoom) | General | CounterStrikeSharp |
| [PrivateMessage](PrivateMessage/README.md) | Private messages between players (!pm) | General | CounterStrikeSharp, MySQL (optional) |
| [Redbull](Redbull/README.md) | Timed speed boost with limit and cooldown support | General | CounterStrikeSharp |
| [ScreenText](ScreenText/README.md) | Persistent text defined in JSON, placed anywhere on screen | General | CounterStrikeSharp |
| [Sesler](Sesler/README.md) | Mutes knife/weapon/footstep/player/MVP sounds by category | General | CounterStrikeSharp, MySQL (optional) |
| [ShowPlayerClips](ShowPlayerClips/README.md) | Shows the map's invisible clip brushes (playerclip, npcclip, grenadeclip, sky) as colored lines | Admin | CounterStrikeSharp |
| [Silahsil](Silahsil/README.md) | Clears unowned weapons on the ground with a single command | Jailbreak | CounterStrikeSharp |
| [Slowmode](Slowmode/README.md) | Server-wide chat slow mode; enforces a second limit between messages | General | CounterStrikeSharp |
| [SpawnkillProtection](SpawnkillProtection/README.md) | Flag and team based spawn protection with color transitions | General | CounterStrikeSharp |
| [Speedometer](Speedometer/README.md) | Live speed readout (u/s) on the HUD with color transitions | General | CounterStrikeSharp |
| [Sustum](Sustum/README.md) | Typing event with 4 modes (CTSustum, TSustum, DSustum, DeadSustum) | Jailbreak | CounterStrikeSharp |
| [TABServerName](TABServerName/README.md) | Changes the map name shown in the top left of the scoreboard | Admin | CounterStrikeSharp, gamedata |
| [Thirdperson](Thirdperson/README.md) | Third person camera with wall blocking | General | CounterStrikeSharp, gamedata |
| [VIPCore](VIPCore/README.md) | Group based VIP system with 75+ modules and JSON/MySQL support | Admin | CounterStrikeSharp, MySQL (optional) |
| [WardenMarker](WardenMarker/README.md) | A single glowing ring marker the warden keeps moving to the point they look at | Jailbreak | CounterStrikeSharp, gamedata |

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Installation

Ready to use files are in the `.Compiled` folder, so you do not have to build anything.

1. **To install everything:** copy the contents of `.Compiled/addons` into the `csgo/addons` folder on your server.
2. **To install a single plugin:** take that plugin's folder from `.Compiled/addons/counterstrikesharp/plugins/` and drop it in the same place on your server.
3. Restart the server or run `css_plugins load <PluginName>`.
4. Config files are created on the first load. See the plugin's own README for its specific settings.

## Notes

- **gamedata:** Plugins listed with `gamedata` in the table need the files in the `addons/counterstrikesharp/gamedata` folder. If you install a single plugin, copy that folder too; without it the plugin still loads but the feature that needs it will not work. If one of these plugins breaks after a CS2 update, updating just the gamedata file is usually the fix.
- **MySQL (optional):** These plugins use a JSON file by default, you do not have to switch to MySQL. Switch only if several servers should share the same records.
- The source code of every plugin is in its own folder; if you prefer, you can build it yourself with `dotnet build <PluginName>/<PluginName>.csproj -c Release`.
- License: MIT
