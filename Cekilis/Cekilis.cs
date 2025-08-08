using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace Cekilis;

public class CekilisConfig : BasePluginConfig
{
    [JsonPropertyName("chat_prefix")]
    public string ChatPrefix { get; set; } = "[ByDexter]";
}

public class Cekilis : BasePlugin, IPluginConfig<CekilisConfig>
{
    public CekilisConfig Config { get; set; } = new CekilisConfig();

    public override string ModuleName => "Cekiliş";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "Çekiliş";

    public void OnConfigParsed(CekilisConfig config)
    {
        Config = config;
    }

    [ConsoleCommand("css_cek", "css_cek")]
    [RequiresPermissions("@css/chat")]
    public void OnCekilisCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        if (info.ArgCount < 2)
        {
            player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} !cek {CC.Green}all{CC.Default} » Herkesten rastgele bir isim seçer");
            player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} !cek {CC.Green}dead{CC.Default} » Ölü oyunculardan rastgele bir isim seçer");
            player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} !cek {CC.Green}live{CC.Default} » Canlı oyunculardan rastgele bir isim seçer");
            player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} !cek {CC.Green}T{CC.Default} » Terörist takımından rastgele bir isim seçer");
            player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} !cek {CC.Green}CT{CC.Default} » CT takımından rastgele bir isim seçer");
            player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} !cek {CC.Green}Tdead{CC.Default} » Ölü teröristlerden rastgele bir isim seçer");
            player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} !cek {CC.Green}Tlive{CC.Default} » Canlı teröristlerden rastgele bir isim seçer");
            player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} !cek {CC.Green}CTdead{CC.Default} » Ölü CT oyuncularından rastgele bir isim seçer");
            player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} !cek {CC.Green}CTlive{CC.Default} » Canlı CT oyuncularından rastgele bir isim seçer");
            return;
        }

        var arg = info.GetArg(1).ToLower();
        var players = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot).ToList();

        List<CCSPlayerController> pool = arg switch
        {
            "all" => players,
            "dead" => players.Where(p => !IsAlive(p)).ToList(),
            "live" => players.Where(p => IsAlive(p)).ToList(),
            "t" => players.Where(p => p.Team == CsTeam.Terrorist).ToList(),
            "ct" => players.Where(p => p.Team == CsTeam.CounterTerrorist).ToList(),
            "tdead" => players.Where(p => p.Team == CsTeam.Terrorist && !IsAlive(p)).ToList(),
            "tlive" => players.Where(p => p.Team == CsTeam.Terrorist && IsAlive(p)).ToList(),
            "ctdead" => players.Where(p => p.Team == CsTeam.CounterTerrorist && !IsAlive(p)).ToList(),
            "ctlive" => players.Where(p => p.Team == CsTeam.CounterTerrorist && IsAlive(p)).ToList(),
            _ => new List<CCSPlayerController>()
        };

        if (pool.Count == 0)
        {
            player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Çekiliş için uygun oyuncu bulunamadı!");
            return;
        }

        var rnd = new Random();
        var winner = pool[rnd.Next(pool.Count)];
        var adminName = player != null ? player.PlayerName : "unknown";
        Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{adminName}{CC.Default} Çekiliş yaptı, kazanan: {CC.Gold}{winner.PlayerName}{CC.Default}");
    }

    static bool IsAlive(CCSPlayerController? player)
    {
        return player?.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
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