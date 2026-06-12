using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace CTKov;

public class CTKov : BasePlugin
{
    public override string ModuleName => "CTKov";
    public override string ModuleVersion => "1.0.2";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

    [ConsoleCommand("css_ctkov", "css_ctkov")]
    [RequiresPermissionsOr("@css/generic", "@jailbreak/warden")]
    public void OnCTKovCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid)
            return;

        int kickedCount = 0;
        var playersToMove = new List<CCSPlayerController>();

        foreach (var target in Utilities.GetPlayers())
        {
            if (target == null || !target.IsValid || target.IsBot)
                continue;

            if (target.TeamNum != 3)
                continue;

            if (AdminManager.PlayerHasPermissions(target, "@jailbreak/warden"))
                continue;

            playersToMove.Add(target);
        }

        foreach (var target in playersToMove)
        {
            if (target.IsValid)
            {
                target.ChangeTeam(CsTeam.Terrorist);
                kickedCount++;
            }
        }

        if (kickedCount > 0)
        {
            Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctkov.guards_moved", player.PlayerName, kickedCount]}");
        }
        else
        {
            player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctkov.no_guards"]}");
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
