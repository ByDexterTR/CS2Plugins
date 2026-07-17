using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace ChatCleaner;

public class ChatCleaner : BasePlugin
{
    public override string ModuleName => "ChatCleaner";
    public override string ModuleVersion => "1.0.4";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];
    [ConsoleCommand("css_selfcc", "css_selfcc")]
    public void OnSelfChatCleanCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null) return;

        for (int i = 0; i < 500; i++)
        {
            player.PrintToChat(" ");
        }

        player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["chatcleaner.selfcleaned"]}");
    }

    [ConsoleCommand("css_cc", "css_cc")]
    [RequiresPermissions("@css/chat")]
    public void OnAdminChatCleanCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null) return;

        for (int i = 0; i < 500; i++)
        {
            Server.PrintToChatAll(" ");
        }

        Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["chatcleaner.admincleaned"]} {CC.Gold}{player.PlayerName}");
    }
}
