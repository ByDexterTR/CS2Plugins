using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace VIPCore;

public class DuckEndurance : VipModule
{
    private const float IdealDuckSpeed = 8f;

    public override string Name => "DuckEndurance";
    public override string DisplayName => Core.Localizer["vip.module.duckendurance"];

    public override void OnLoad() =>
        VirtualFunctions.CCSPlayerPawnBase_PostThinkFunc.Hook(OnPostThink, HookMode.Post);

    public override void OnUnload() =>
        VirtualFunctions.CCSPlayerPawnBase_PostThinkFunc.Unhook(OnPostThink, HookMode.Post);

    private HookResult OnPostThink(DynamicHook hook)
    {
        var pawn = hook.GetParam<CCSPlayerPawnBase>(0);
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        var player = pawn.As<CCSPlayerPawn>().Controller.Value?.As<CCSPlayerController>();
        if (player == null || !player.IsValid || player.IsBot || !Active(player))
            return HookResult.Continue;

        var movement = pawn.MovementServices?.As<CCSPlayer_MovementServices>();
        if (movement == null)
            return HookResult.Continue;

        if (movement.DuckSpeed < IdealDuckSpeed)
            movement.DuckSpeed = IdealDuckSpeed;

        if (movement.LastDuckTime != 0f)
            movement.LastDuckTime = 0f;

        return HookResult.Continue;
    }
}
