using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class DuckEndurance : VipModule
{
    private const float IdealDuckSpeed = 8f;

    public override string Name => "DuckEndurance";
    public override string DisplayName => Core.Localizer["vip.module.duckendurance"];

    public override void OnLoad() => Core.HookPostThink(OnPostThink);

    private void OnPostThink(CCSPlayerController player, CCSPlayerPawn pawn)
    {
        if (!Active(player) || player.IsBot)
            return;

        PlayerCache.KeepDuckFresh(PlayerCache.Pointer(player), IdealDuckSpeed);
    }
}
