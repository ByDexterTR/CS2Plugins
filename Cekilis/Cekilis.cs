using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace Cekilis;

public class Cekilis : BasePlugin
{
    public override string ModuleName => "Cekiliş";
    public override string ModuleVersion => "1.0.6";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

    [ConsoleCommand("css_cek", "css_cek")]
    [RequiresPermissions("@css/chat")]
    public void OnCekilisCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        if (info.ArgCount < 2)
        {
            player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["cekilis.help_all"]}");
            player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["cekilis.help_dead"]}");
            player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["cekilis.help_live"]}");
            player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["cekilis.help_t"]}");
            player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["cekilis.help_ct"]}");
            player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["cekilis.help_tdead"]}");
            player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["cekilis.help_tlive"]}");
            player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["cekilis.help_ctdead"]}");
            player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["cekilis.help_ctlive"]}");
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
            player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["cekilis.no_player"]}");
            return;
        }

        var winner = pool[Random.Shared.Next(pool.Count)];
        var adminName = player != null ? player.PlayerName : "unknown";
        var categoryName = arg.ToUpper();
        Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["cekilis.winner", adminName, categoryName, winner.PlayerName]}");
    }

    static bool IsAlive(CCSPlayerController? player)
    {
        return player?.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
    }
}
