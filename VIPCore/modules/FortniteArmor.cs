using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class FortniteArmor : VipModule
{
    private class Cfg
    {
        public int Percent { get; set; } = 100;
        public bool AbsorbFallDamage { get; set; } = false;
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "FortniteArmor";
    public override string DisplayName => Core.Localizer["vip.module.fortnitearmor"];

    public override void OnLoad() => Core.HookDamage(OnDamage);

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        if (info.Damage <= 0f)
            return HookResult.Continue;

        var victim = PawnController(entity);
        if (!Active(victim) || !IsAlive(victim))
            return HookResult.Continue;

        var pawn = victim!.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        int armor = pawn.ArmorValue;
        if (armor <= 0)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(victim) ?? DefaultCfg;
        if (cfg.Percent <= 0)
            return HookResult.Continue;

        if (!cfg.AbsorbFallDamage && ((long)info.BitsDamageType & (long)DamageTypes_t.DMG_FALL) != 0)
            return HookResult.Continue;

        float share = info.Damage * Math.Min(cfg.Percent, 100) / 100f;
        int absorbed = Math.Min((int)MathF.Round(share), armor);
        if (absorbed <= 0)
            return HookResult.Continue;

        pawn.ArmorValue = armor - absorbed;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

        info.Damage = MathF.Max(info.Damage - absorbed, 0f);
        return HookResult.Changed;
    }
}
