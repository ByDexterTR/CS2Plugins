using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace VIPCore;

public class DuckSpeed : VipModule
{
    private const float VanillaDuckPercent = 34f;

    private class Cfg
    {
        public float Percent { get; set; } = VanillaDuckPercent;
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "DuckSpeed";
    public override string DisplayName => Core.Localizer["vip.module.duckspeed"];

    public override void OnLoad()
    {
        SpeedPool.Ensure(Core);
        VirtualFunctions.CCSPlayerPawnBase_PostThinkFunc.Hook(OnPostThink, HookMode.Post);
    }

    public override void OnUnload() =>
        VirtualFunctions.CCSPlayerPawnBase_PostThinkFunc.Unhook(OnPostThink, HookMode.Post);

    private HookResult OnPostThink(DynamicHook hook)
    {
        var pawnBase = hook.GetParam<CCSPlayerPawnBase>(0);
        if (pawnBase == null || !pawnBase.IsValid)
            return HookResult.Continue;

        var pawn = pawnBase.As<CCSPlayerPawn>();
        var player = pawn.Controller.Value?.As<CCSPlayerController>();
        if (player == null || !player.IsValid || player.IsBot || player.Slot >= 64)
            return HookResult.Continue;

        var movement = pawnBase.MovementServices?.As<CCSPlayer_MovementServices>();
        if (movement == null || movement.DuckAmount <= 0.01f || !Active(player))
            return HookResult.Continue;

        float percent = Math.Clamp((GroupValue<Cfg>(player) ?? DefaultCfg).Percent, VanillaDuckPercent, 100f);
        float target = percent / VanillaDuckPercent;

        if (pawn.VelocityModifier < target)
        {
            pawn.VelocityModifier = target;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        }

        SpeedPool.Floor(player.Slot, target);
        return HookResult.Continue;
    }
}
