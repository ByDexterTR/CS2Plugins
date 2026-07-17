using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace CTBan;

public class CTBanEntry
{
    public string Nickname { get; set; } = "";
    public long BanTime { get; set; }
    public string Reason { get; set; } = "";
    public string Admin { get; set; } = "";
}

public class CTBanList
{
    public Dictionary<string, CTBanEntry> BannedPlayers { get; set; } = new();
}

public class CTBan : BasePlugin
{
    public override string ModuleName => "CTBan";
    public override string ModuleVersion => "1.0.5";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

    private string ChatPrefix => Localizer["chat_prefix"];

    private CTBanList BanList = new();

    private string BanListPath => Path.Combine(ModuleDirectory, "CTBanList.json");


    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
        RegisterEventHandler<EventPlayerTeam>(OnSwitchTeam);
        AddCommandListener("jointeam", OnJoinTeam);
        LoadBanList();
    }

    private CTBanEntry? GetActiveBan(CCSPlayerController player)
    {
        var steamid = player.SteamID.ToString();
        if (!BanList.BannedPlayers.TryGetValue(steamid, out var banEntry))
            return null;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (banEntry.BanTime > now)
            return banEntry;

        BanList.BannedPlayers.Remove(steamid);
        SaveBanList();
        return null;
    }

    private void NotifyBlocked(CCSPlayerController player, CTBanEntry banEntry)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctban.join_blocked", FormatTimeLeft(banEntry.BanTime - now), banEntry.Reason]}");
    }

    private void LoadBanList()
    {
        if (File.Exists(BanListPath))
        {
            var json = File.ReadAllText(BanListPath);
            BanList = JsonSerializer.Deserialize<CTBanList>(json) ?? new CTBanList();
        }
        else
        {
            BanList = new CTBanList();
            SaveBanList();
        }
    }

    private readonly object _saveLock = new();

    private void SaveBanList()
    {
        var json = JsonSerializer.Serialize(BanList, new JsonSerializerOptions { WriteIndented = true });
        var path = BanListPath;

        Task.Run(() =>
        {
            try
            {
                lock (_saveLock)
                    File.WriteAllText(path, json);
            }
            catch
            {
            }
        });
    }

    private static long ParseDuration(string input)
    {
        input = input.Trim().ToLower();
        if (string.IsNullOrEmpty(input)) return 0;

        var number = new string(input.TakeWhile(char.IsDigit).ToArray());
        var unit = input.Substring(number.Length);

        if (!long.TryParse(number, out long value)) return 0;
        if (string.IsNullOrEmpty(unit)) unit = "m";

        switch (unit)
        {
            case "s": return value;
            case "m": return value * 60;
            case "h": return value * 60 * 60;
            case "d": return value * 60 * 60 * 24;
            case "w": return value * 60 * 60 * 24 * 7;
            case "mo": return value * 60 * 60 * 24 * 30;
            case "y": return value * 60 * 60 * 24 * 365;
            default: return value * 60;
        }
    }

    private string FormatTimeLeft(long seconds)
    {
        if (seconds <= 0)
            return $"{CC.Green}0{CC.Default} {Localizer["ctban.unit_second"]}";

        var units = new (long Seconds, string Key)[]
        {
            (31536000, "ctban.unit_year"),
            (2592000, "ctban.unit_month"),
            (604800, "ctban.unit_week"),
            (86400, "ctban.unit_day"),
            (3600, "ctban.unit_hour"),
            (60, "ctban.unit_minute"),
            (1, "ctban.unit_second")
        };

        var parts = new List<string>();
        foreach (var (unitSeconds, key) in units)
        {
            long value = seconds / unitSeconds;
            seconds %= unitSeconds;
            if (value > 0)
                parts.Add($"{CC.Green}{value}{CC.Default} {Localizer[key]}");
        }

        return string.Join(" ", parts);
    }

    [ConsoleCommand("css_ctban", "css_ctban <Hedef> <Süre> [Sebep]")]
    [RequiresPermissions("@css/ban")]
    public void OnCTBanCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        if (info.ArgCount < 3)
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctban.usage_ban"]}");
            return;
        }

        var targetArg = info.GetArg(1);
        var target = FindPlayer(targetArg);
        if (target == null)
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctban.target_not_found"]}");
            return;
        }
        var steamid = target.SteamID.ToString();

        var durationStr = info.GetArg(2);
        var durationSec = ParseDuration(durationStr);
        if (durationSec <= 0)
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctban.invalid_duration"]}");
            return;
        }

        var reason = info.ArgCount > 3 ? string.Join(" ", Enumerable.Range(3, info.ArgCount - 3).Select(i => info.GetArg(i))) : Localizer["ctban.no_reason"].ToString();
        var adminSteamId = player?.SteamID.ToString() ?? "unknown";
        BanList.BannedPlayers[steamid] = new CTBanEntry
        {
            Nickname = target.PlayerName,
            BanTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + durationSec,
            Reason = reason,
            Admin = adminSteamId
        };
        SaveBanList();

        if (target.Team == CsTeam.CounterTerrorist)
        {
            target.ChangeTeam(CsTeam.Terrorist);
        }

        var adminName = player != null ? player.PlayerName : "unknown";
        Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctban.banned", target.PlayerName, adminName, FormatTimeLeft(durationSec), reason]}");
    }

    [ConsoleCommand("css_ctunban", "css_ctunban <Hedef>")]
    [RequiresPermissions("@css/ban")]
    public void OnCTUnbanCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        if (info.ArgCount < 2)
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctban.usage_unban"]}");
            return;
        }

        var targetArg = info.GetArg(1);
        var target = FindPlayer(targetArg);
        if (target == null)
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctban.target_not_found"]}");
            return;
        }
        var steamid = target.SteamID.ToString();

        if (BanList.BannedPlayers.Remove(steamid))
        {
            SaveBanList();
            Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctban.unbanned", target.PlayerName, player.PlayerName]}");
        }
        else
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctban.unban_not_found", target.PlayerName]}");
        }
    }

    [ConsoleCommand("css_ctaddban", "css_ctaddban <STEAMID64> <Süre> [Sebep]")]
    [RequiresPermissions("@css/ban")]
    public void OnCTAddBanCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        if (info.ArgCount < 3)
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctban.usage_addban"]}");
            return;
        }

        var steamid = info.GetArg(1);
        if (string.IsNullOrEmpty(steamid))
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctban.invalid_steamid"]}");
            return;
        }

        var durationStr = info.GetArg(2);
        var durationSec = ParseDuration(durationStr);
        if (durationSec <= 0)
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctban.invalid_duration"]}");
            return;
        }

        var reason = info.ArgCount > 3 ? string.Join(" ", Enumerable.Range(3, info.ArgCount - 3).Select(i => info.GetArg(i))) : Localizer["ctban.no_reason"].ToString();
        var adminSteamId = player?.SteamID.ToString() ?? "unknown";
        BanList.BannedPlayers[steamid] = new CTBanEntry
        {
            Nickname = steamid,
            BanTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + durationSec,
            Reason = reason,
            Admin = adminSteamId
        };
        SaveBanList();

        info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctban.addban_applied", steamid, FormatTimeLeft(durationSec), reason]}");
    }

    [ConsoleCommand("css_ctbanlist", "css_ctbanlist")]
    public void OnCTBanListCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var activeBans = BanList.BannedPlayers.Values.Where(b => b.BanTime > now).ToList();

        if (activeBans.Count == 0)
        {
            player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctban.banlist_empty"]}");
            return;
        }

        player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctban.banlist_header"]}");
        foreach (var banEntry in activeBans)
        {
            var timeLeft = FormatTimeLeft(banEntry.BanTime - now);
            player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctban.banlist_entry", banEntry.Nickname, timeLeft, banEntry.Reason]}");
        }
    }

    public HookResult OnSwitchTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        if (@event.Team != (int)CsTeam.CounterTerrorist)
            return HookResult.Continue;

        var banEntry = GetActiveBan(player);
        if (banEntry == null)
            return HookResult.Continue;

        NotifyBlocked(player, banEntry);
        Server.NextFrame(() =>
        {
            if (player.IsValid && player.Team == CsTeam.CounterTerrorist)
                player.ChangeTeam(CsTeam.Terrorist);
        });
        return HookResult.Continue;
    }

    public HookResult OnJoinTeam(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        if (!int.TryParse(info.GetArg(1), out var targetTeam) || targetTeam != (int)CsTeam.CounterTerrorist)
            return HookResult.Continue;

        var banEntry = GetActiveBan(player);
        if (banEntry == null)
            return HookResult.Continue;

        NotifyBlocked(player, banEntry);
        return HookResult.Handled;
    }

    public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV) return HookResult.Continue;
        var steamIdStr = player.SteamID.ToString();
        if (BanList.BannedPlayers.TryGetValue(steamIdStr, out var banEntry))
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (banEntry.BanTime <= now)
            {
                BanList.BannedPlayers.Remove(steamIdStr);
                SaveBanList();
            }
        }
        return HookResult.Continue;
    }

    private static CCSPlayerController? FindPlayer(string arg)
    {
        if (arg.StartsWith("#"))
        {
            if (int.TryParse(arg.Substring(1), out var userid))
                return Utilities.GetPlayerFromUserid(userid);
            return null;
        }

        return Utilities.GetPlayers().FirstOrDefault(p => p.PlayerName.Equals(arg, StringComparison.OrdinalIgnoreCase));
    }
}
