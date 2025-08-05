using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace ChatCleaner;

[MinimumApiVersion(80)]
public class ChatCleaner : BasePlugin
{
  public override string ModuleName => "ChatCleaner";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "Players can clean their chat with !cc or /cc command.";

  [ConsoleCommand("css_selfcc", "Tests")]
  public void OnSelfChatCleanCommand(CCSPlayerController? player, CommandInfo commandInfo)
  {
    if (player == null) return;

    for (int i = 0; i < 500; i++)
    {
      player.PrintToChat(" ");
    }
    player.PrintToChat(Localizer.ForPlayer(player, "chatcleaner.selfcleaned"));
  }

  [ConsoleCommand("css_cc", "")]
  [RequiresPermissions("@css/chatcleaner")]
  public void OnAdminChatCleanCommand(CCSPlayerController? player, CommandInfo commandInfo)
  {
    if (player == null) return;

    for (int i = 0; i < 500; i++)
    {
      Server.PrintToChatAll(" ");
    }
    Server.PrintToChatAll(Localizer["chatcleaner.admincleaned", player.PlayerName]);
  }
}