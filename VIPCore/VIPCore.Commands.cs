using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace VIPCore;

public partial class VIPCore
{
    public void RegisterCommands()
    {
        Register(Config.Commands.Menu, OnVipCommand);
        Register(Config.Commands.ListOnline, OnVipsCommand);
        Register(Config.Commands.ListAll, OnVipListCommand);
        Register(Config.Commands.AddVip, OnAddVipCommand);
        Register(Config.Commands.RemoveVip, OnRemoveVipCommand);
        Register(Config.Commands.Reload, OnVipReloadCommand);
    }

    private void Register(string names, CommandInfo.CommandCallback handler)
    {
        foreach (var name in names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            AddCommand(name, "VIPCore", handler);
    }

    public void RegisterAliasedCommand(string names, CommandInfo.CommandCallback handler) => Register(names, handler);

    public string TpCommands => Config.Commands.Tp;

    public void OnVipsCommand(CCSPlayerController? player, CommandInfo info)
    {
        var vips = Utilities.GetPlayers()
            .Where(p => p != null && p.IsValid && !p.IsBot && !p.IsHLTV && IsClientVip(p))
            .ToList();

        if (vips.Count == 0)
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.list_empty"]}");
            return;
        }

        info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.list_title"]}");
        foreach (var p in vips)
            info.ReplyToCommand($" {CC.Gold}{p.PlayerName}{CC.Default} - {GetClientGroup(p)}");
    }

    public void OnVipListCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (!HasAdmin(player))
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.no_permission"]}");
            return;
        }

        List<KeyValuePair<ulong, VipEntry>> all;
        lock (_lock)
            all = _vips.OrderBy(kv => kv.Value.Expires == 0 ? long.MaxValue : kv.Value.Expires).ToList();

        if (all.Count == 0)
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.list_empty_all"]}");
            return;
        }

        info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.list_all_title", all.Count]}");

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (var (steamId, entry) in all)
        {
            string status = entry.Expires == 0 ? Localizer["vip.permanent"] : FormatTimeLeft(entry.Expires - now);
            string? name = OnlineNameOf(steamId);
            string who = name != null ? $"{name} ({steamId})" : steamId.ToString();
            info.ReplyToCommand($" {CC.Gold}{who}{CC.Default} - {entry.Group} - {status}");
        }
    }

    private static string? OnlineNameOf(ulong steamId)
    {
        var p = Utilities.GetPlayers().FirstOrDefault(x => x != null && x.IsValid && !x.IsBot && x.SteamID == steamId);
        return p?.PlayerName;
    }

    public void OnVipCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        PurgeIfExpired(player.SteamID);

        if (!IsClientVip(player))
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.no_access"]}");
            return;
        }

        long left = GetClientTimeLeft(player.SteamID);
        string timeText = left == long.MaxValue ? Localizer["vip.permanent"] : FormatTimeLeft(left);
        player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.time_left", GetClientGroup(player) ?? "", timeText]}");

        OpenMainMenu(player);
    }

    public void OnAddVipCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (!HasAdmin(player))
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.no_permission"]}");
            return;
        }

        if (info.ArgCount < 4)
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.usage_addvip"]}");
            return;
        }

        if (!ulong.TryParse(info.GetArg(1), out var steamId) || steamId < 76561197960265728UL)
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.invalid_steamid"]}");
            return;
        }

        string group = info.GetArg(2);
        if (!GroupExists(group))
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.invalid_group", string.Join(", ", GroupNames())]}");
            return;
        }

        string firstToken = info.GetArg(3).Trim().ToLower();
        long expires;
        string durationText;

        if (firstToken is "0" or "perm" or "permanent" or "-1")
        {
            expires = 0;
            durationText = Localizer["vip.permanent"];
        }
        else
        {
            long totalSeconds = 0;
            for (int i = 3; i < info.ArgCount; i++)
                totalSeconds += ParseDuration(info.GetArg(i));

            if (totalSeconds <= 0)
            {
                info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.invalid_duration"]}");
                return;
            }

            expires = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + totalSeconds;
            durationText = FormatTimeLeft(totalSeconds);
        }

        SetVip(steamId, group, expires);
        info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.added", steamId.ToString(), group, durationText]}");
    }

    public void OnRemoveVipCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (!HasAdmin(player))
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.no_permission"]}");
            return;
        }

        if (info.ArgCount < 2)
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.usage_removevip"]}");
            return;
        }

        if (!ulong.TryParse(info.GetArg(1), out var steamId))
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.invalid_steamid"]}");
            return;
        }

        bool existed;
        lock (_lock)
            existed = _vips.ContainsKey(steamId);

        if (!existed)
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.not_found", steamId.ToString()]}");
            return;
        }

        RemoveVip(steamId);
        info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.removed", steamId.ToString()]}");
    }

    public void OnVipReloadCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (!HasAdmin(player))
        {
            info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.no_permission"]}");
            return;
        }

        LoadConfig();
        ReloadData();
        ActivateModules();
        info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.reloaded"]}");
    }

    private void SetVip(ulong steamId, string group, long expires)
    {
        var entry = new VipEntry { Group = group, Expires = expires };
        lock (_lock)
            _vips[steamId] = entry;

        var storage = _storage;
        Task.Run(() =>
        {
            try { storage.UpsertVip(steamId, entry); }
            catch { }
        });
    }

    private void RemoveVip(ulong steamId)
    {
        lock (_lock)
        {
            _vips.Remove(steamId);
            _settings.Remove(steamId);
        }

        var storage = _storage;
        Task.Run(() =>
        {
            try { storage.DeleteVip(steamId); }
            catch { }
        });
    }

    private bool GroupExists(string group)
    {
        lock (_lock)
            return _groups.ContainsKey(group);
    }

    private IEnumerable<string> GroupNames()
    {
        lock (_lock)
            return _groups.Keys.ToList();
    }

    public long GetClientTimeLeft(ulong steamId)
    {
        if (!IsSteamIdVip(steamId))
            return -1;

        lock (_lock)
        {
            long expires = _vips[steamId].Expires;
            return expires == 0 ? long.MaxValue : expires - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    private bool HasAdmin(CCSPlayerController? player) =>
        player == null || AdminManager.PlayerHasPermissions(player, Config.AdminFlag);

    private static long ParseDuration(string input)
    {
        input = input.Trim().ToLower();
        if (string.IsNullOrEmpty(input))
            return 0;

        var number = new string(input.TakeWhile(char.IsDigit).ToArray());
        var unit = input.Substring(number.Length);

        if (!long.TryParse(number, out long value))
            return 0;
        if (string.IsNullOrEmpty(unit))
            unit = "m";

        return unit switch
        {
            "s" => value,
            "m" => value * 60,
            "h" => value * 60 * 60,
            "d" => value * 60 * 60 * 24,
            "w" => value * 60 * 60 * 24 * 7,
            "mo" => value * 60 * 60 * 24 * 30,
            "y" => value * 60 * 60 * 24 * 365,
            _ => value * 60
        };
    }

    private string FormatTimeLeft(long seconds)
    {
        if (seconds <= 0)
            return Localizer["vip.time_now"];

        long days = seconds / 86400;
        seconds %= 86400;
        long hours = seconds / 3600;
        seconds %= 3600;
        long minutes = seconds / 60;

        var parts = new List<string>();
        if (days > 0) parts.Add(Localizer["vip.unit_day", days]);
        if (hours > 0) parts.Add(Localizer["vip.unit_hour", hours]);
        if (minutes > 0) parts.Add(Localizer["vip.unit_minute", minutes]);

        return parts.Count > 0 ? string.Join(" ", parts) : Localizer["vip.time_now"];
    }
}
