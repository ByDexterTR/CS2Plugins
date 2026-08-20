using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace VIPCore;

public static class ScreenFade
{
    private const int Units = 1024;

    public static void Apply(CCSPlayerController? player, Color color, float fade, float hold)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        using var msg = UserMessage.FromPartialName("Fade");
        if (msg == null)
            return;

        int packed = (color.A << 24) | (color.B << 16) | (color.G << 8) | color.R;

        msg.SetInt("duration", (int)(Math.Max(fade, 0f) * Units));
        msg.SetInt("hold_time", (int)(Math.Max(hold, 0f) * Units));
        msg.SetInt("flags", 1);
        msg.SetInt("color", packed);

        msg.Send(player);
    }
}
