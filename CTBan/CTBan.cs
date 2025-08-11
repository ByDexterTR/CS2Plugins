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

public class CTBanConfig : BasePluginConfig
{
    [JsonPropertyName("chat_prefix")]
    public string ChatPrefix { get; set; } = "[ByDexter]";
}

public class CTBan : BasePlugin, IPluginConfig<CTBanConfig>
{
    public override string ModuleName => "CTBan";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "CTBan";

    public CTBanConfig Config { get; set; } = new CTBanConfig();
    private CTBanList BanList = new();

    private string BanListPath => Path.Combine(ModuleDirectory, "CTBanList.json");


    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerTeam>(OnSwitchTeam);
        AddCommandListener("jointeam", OnJoinTeam);
        LoadBanList();
    }

    public void OnConfigParsed(CTBanConfig config)
    {
        Config = config;
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

    private void SaveBanList()
    {
        var json = JsonSerializer.Serialize(BanList, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(BanListPath, json);
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

    private static string FormatTimeLeft(long seconds)
    {
        if (seconds <= 0)
            return $"{CC.Green}0{CC.Default} saniye";

        var parts = new List<string>();

        long years = seconds / 31536000;
        seconds %= 31536000;
        if (years > 0)
            parts.Add($"{CC.Green}{years}{CC.Default} yıl");

        long months = seconds / 2592000;
        seconds %= 2592000;
        if (months > 0)
            parts.Add($"{CC.Green}{months}{CC.Default} ay");

        long weeks = seconds / 604800;
        seconds %= 604800;
        if (weeks > 0)
            parts.Add($"{CC.Green}{weeks}{CC.Default} hafta");

        long days = seconds / 86400;
        seconds %= 86400;
        if (days > 0)
            parts.Add($"{CC.Green}{days}{CC.Default} gün");

        long hours = seconds / 3600;
        seconds %= 3600;
        if (hours > 0)
            parts.Add($"{CC.Green}{hours}{CC.Default} saat");

        long minutes = seconds / 60;
        seconds %= 60;
        if (minutes > 0)
            parts.Add($"{CC.Green}{minutes}{CC.Default} dakika");

        if (seconds > 0)
            parts.Add($"{CC.Green}{seconds}{CC.Default} saniye");

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
            info.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Kullanım: css_ctban <Hedef> <Süre> [Sebep]");
            return;
        }

        var targetArg = info.GetArg(1);
        var target = FindPlayer(targetArg);
        if (target == null)
        {
            info.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Hedef bulunamadı");
            return;
        }
        var steamid = target.SteamID.ToString();

        var durationStr = info.GetArg(2);
        var durationSec = ParseDuration(durationStr);
        if (durationSec <= 0)
        {
            info.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Geçerli bir süre giriniz. (Örnek: 30m, 2h, 1d)");
            return;
        }

        var reason = info.ArgCount > 3 ? string.Join(" ", Enumerable.Range(3, info.ArgCount - 3).Select(i => info.GetArg(i))) : "Sebepsiz";
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
        Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{target.PlayerName}{CC.Default}, {CC.Gold}{adminName}{CC.Default} tarafından {FormatTimeLeft(durationSec)} CTden {CC.Red}banlandı! {CC.Default}Sebep: {CC.Green}{reason}");
    }

    [ConsoleCommand("css_ctunban", "css_ctunban <Hedef>")]
    [RequiresPermissions("@css/ban")]
    public void OnCTUnbanCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        if (info.ArgCount < 2)
        {
            info.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Kullanım: css_ctunban <Hedef>");
            return;
        }

        var targetArg = info.GetArg(1);
        var target = FindPlayer(targetArg);
        if (target == null)
        {
            info.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Hedef bulunamadı");
            return;
        }
        var steamid = target.SteamID.ToString();

        if (BanList.BannedPlayers.Remove(steamid))
        {
            SaveBanList();
            Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{target.PlayerName}{CC.Default}, CT banı {CC.Gold}{player.PlayerName}{CC.Default} tarafından {CC.Red}kaldırıldı!");
        }
        else
        {
            info.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{target.PlayerName}{CC.Default} CT banı bulunamadı.");
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
            info.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Kullanım: css_ctaddban <STEAMID64> <Süre> [Sebep]");
            return;
        }

        var steamid = info.GetArg(1);
        if (string.IsNullOrEmpty(steamid))
        {
            info.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Geçerli bir STEAMID64 giriniz.");
            return;
        }

        var durationStr = info.GetArg(2);
        var durationSec = ParseDuration(durationStr);
        if (durationSec <= 0)
        {
            info.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Geçerli bir süre giriniz. (Örnek: 30m, 2h, 1d)");
            return;
        }

        var reason = info.ArgCount > 3 ? string.Join(" ", Enumerable.Range(3, info.ArgCount - 3).Select(i => info.GetArg(i))) : "Sebepsiz";
        var adminSteamId = player?.SteamID.ToString() ?? "unknown";
        BanList.BannedPlayers[steamid] = new CTBanEntry
        {
            Nickname = steamid,
            BanTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + durationSec,
            Reason = reason,
            Admin = adminSteamId
        };
        SaveBanList();

        info.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{steamid}{CC.Default} {FormatTimeLeft(durationSec)} CTden {CC.Red}banlandı! {CC.Default}Sebep: {CC.Green}{reason}");
    }

    [ConsoleCommand("css_ctbanlist", "css_ctbanlist")]
    public void OnCTBanListCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var found = false;

        foreach (var kvp in BanList.BannedPlayers)
        {
            var steamid = kvp.Key;
            var banEntry = kvp.Value;
            if (banEntry.BanTime > now)
            {
                player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} CT Ban Listesi:");
                var timeLeft = FormatTimeLeft(banEntry.BanTime - now);
                var msg = $" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{banEntry.Nickname}{CC.Default}, (Kalan Süre: {timeLeft}) Sebep: {CC.Green}{banEntry.Reason}{CC.Default}";
                player.PrintToChat(msg);
                found = true;
            }
        }

        if (!found)
        {
            var msg = $" {CC.Orchid}{Config.ChatPrefix}{CC.Default} CTBanlı oyuncu yok.";
            player.PrintToChat(msg);
        }
    }

    public HookResult OnSwitchTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (@event.Team == (int)CsTeam.CounterTerrorist)
        {
            var steamid = player.SteamID.ToString();
            if (BanList.BannedPlayers.TryGetValue(steamid, out var banEntry))
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (banEntry.BanTime > now)
                {
                    player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} CT takımdan {CC.Red}banlısın! {CC.Default}(Kalan Süre: {FormatTimeLeft(banEntry.BanTime - now)}) Sebep: {CC.Green}{banEntry.Reason}");
                    player.ChangeTeam(CsTeam.Terrorist);
                    return HookResult.Handled;
                }
                else
                {
                    BanList.BannedPlayers.Remove(steamid);
                    SaveBanList();
                }
            }
        }
        return HookResult.Continue;
    }

    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (player.Team == CsTeam.CounterTerrorist)
        {
            var steamid = player.SteamID.ToString();
            if (BanList.BannedPlayers.TryGetValue(steamid, out var banEntry))
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (banEntry.BanTime > now)
                {
                    player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} CT takımından {CC.Red}banlısın! {CC.Default}(Kalan Süre: {FormatTimeLeft(banEntry.BanTime - now)}) Sebep: {CC.Green}{banEntry.Reason}");
                    player.ChangeTeam(CsTeam.Terrorist);
                    return HookResult.Continue;
                }
                else
                {
                    BanList.BannedPlayers.Remove(steamid);
                    SaveBanList();
                }
            }
        }
        return HookResult.Continue;
    }

    public HookResult OnJoinTeam(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        if (player.Team == CsTeam.CounterTerrorist)
        {
            var steamid = player.SteamID.ToString();
            if (BanList.BannedPlayers.TryGetValue(steamid, out var banEntry))
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (banEntry.BanTime > now)
                {
                    player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} CT takımından {CC.Red}banlısın! {CC.Default}(Kalan Süre: {FormatTimeLeft(banEntry.BanTime - now)}) Sebep: {CC.Green}{banEntry.Reason}");
                    player.ChangeTeam(CsTeam.Terrorist);
                    return HookResult.Handled;
                }
                else
                {
                    BanList.BannedPlayers.Remove(steamid);
                    SaveBanList();
                }
            }
        }
        return HookResult.Continue;
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
            if (uint.TryParse(arg.Substring(1), out var id))
                return Utilities.GetPlayers().FirstOrDefault(p => p.Index == id);
            return null;
        }
        else
        {
            return Utilities.GetPlayers().FirstOrDefault(p => p.PlayerName.Equals(arg, StringComparison.OrdinalIgnoreCase));
        }
    }
}

public static class CC
{
    public static char Default => '\x01';
    public static char Red => '\x07';
    public static char LightRed => '\x0F';
    public static char DarkRed => '\x02';
    public static char BlueGrey => '\x0A';
    public static char Blue => '\x0B';
    public static char DarkBlue => '\x0C';
    public static char Purple => '\x0C';
    public static char Orchid => '\x0E';
    public static char Yellow => '\x09';
    public static char Gold => '\x10';
    public static char LightGreen => '\x05';
    public static char Green => '\x04';
    public static char Lime => '\x06';
    public static char Grey => '\x08';
    public static char Grey2 => '\x0D';
}