using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace ByDexter.Shared;

public static class HudGuard
{
  private const int LegacyEventMessageId = 207;
  private const int ShowCenterHtmlEventId = 226;
  private const int BlockTicks = 16;
  private const int MaxSlots = 64;

  private static readonly Regex HtmlValue = new(@"val_string:\s*""(.*?)""", RegexOptions.Compiled);
  private static readonly Regex MenuTag = new(@"^<[A-Za-z0-9]+/>", RegexOptions.Compiled);
  private static readonly int[] BlockedUntil = new int[MaxSlots];

  private static bool _installed;

  public static void Install(BasePlugin plugin)
  {
    if (_installed)
      return;

    plugin.HookUserMessage(LegacyEventMessageId, OnCenterHtml);
    _installed = true;
  }

  public static bool Blocked(int slot) =>
    slot >= 0 && slot < MaxSlots && Server.TickCount < BlockedUntil[slot];

  public static bool Blocked(CCSPlayerController? player) =>
    player != null && player.IsValid && Blocked(player.Slot);

  private static HookResult OnCenterHtml(UserMessage message)
  {
    if (message.ReadUInt("eventid") != ShowCenterHtmlEventId)
      return HookResult.Continue;

    var match = HtmlValue.Match(message.DebugString);
    if (!match.Success || !MenuTag.IsMatch(match.Groups[1].Value))
      return HookResult.Continue;

    int until = Server.TickCount + BlockTicks;

    foreach (var recipient in message.Recipients)
    {
      if (recipient == null || !recipient.IsValid)
        continue;

      int slot = recipient.Slot;
      if (slot >= 0 && slot < MaxSlots)
        BlockedUntil[slot] = until;
    }

    return HookResult.Continue;
  }
}
