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

    private readonly float[] _applied = new float[64];

    public override string Name => "DuckSpeed";
    public override string DisplayName => Core.Localizer["vip.module.duckspeed"];

    public override void OnLoad() =>
        VirtualFunctions.CCSPlayerPawnBase_PostThinkFunc.Hook(OnPostThink, HookMode.Post);

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

        int slot = player.Slot;
        var movement = pawnBase.MovementServices?.As<CCSPlayer_MovementServices>();

        if (!Active(player) || movement == null || movement.DuckAmount <= 0.01f)
        {
            if (_applied[slot] > 0f)
            {
                if (Math.Abs(pawn.VelocityModifier - _applied[slot]) < 0.01f)
                {
                    pawn.VelocityModifier = 1f;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                }
                _applied[slot] = 0f;
            }
            return HookResult.Continue;
        }

        float percent = Math.Clamp((GroupValue<Cfg>(player) ?? DefaultCfg).Percent, VanillaDuckPercent, 100f);
        float target = percent / VanillaDuckPercent;

        if (pawn.VelocityModifier < target)
        {
            pawn.VelocityModifier = target;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        }

        _applied[slot] = target;
        return HookResult.Continue;
    }
}
