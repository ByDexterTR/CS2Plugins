using CounterStrikeSharp.API.Core;

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
        Core.HookPostThink(OnPostThink);
    }

    private void OnPostThink(CCSPlayerController player, CCSPlayerPawn pawn)
    {
        if (player.Slot >= 64 || PlayerCache.DuckAmount(PlayerCache.Pointer(player)) <= 0.01f || !Active(player) || Core.IsBotSlot(player))
            return;

        float percent = Math.Clamp((GroupValue<Cfg>(player) ?? DefaultCfg).Percent, VanillaDuckPercent, 100f);
        float target = percent / VanillaDuckPercent;

        if (pawn.VelocityModifier < target)
        {
            pawn.VelocityModifier = target;
            NetState.Changed(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        }

        SpeedPool.Floor(player.Slot, target);
    }
}
