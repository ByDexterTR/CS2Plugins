using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class GrenadeResist : VipModule
{
    private class Cfg
    {
        public int HeDamagePercent { get; set; } = 0;
        public int MolotovDamagePercent { get; set; } = 0;
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
        public int Limit { get; set; } = 0;
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "GrenadeResist";
    public override string DisplayName => Core.Localizer["vip.module.grenaderesist"];

    public override void OnLoad() => Core.HookDamage(OnDamage);

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        long bits = (long)info.BitsDamageType;
        bool isBlast = (bits & (long)DamageTypes_t.DMG_BLAST) != 0;
        bool isBurn = (bits & (long)DamageTypes_t.DMG_BURN) != 0;
        if (!isBlast && !isBurn)
            return HookResult.Continue;

        var victim = DamageVictim(entity);
        if (!Active(victim))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(victim!) ?? DefaultCfg;
        int percent = isBlast ? cfg.HeDamagePercent : cfg.MolotovDamagePercent;
        if (percent == 0)
            return HookResult.Continue;

        var attacker = DamageAttacker(info);
        if (attacker != null)
        {
            if (cfg.IgnoreSelf && attacker.Slot == victim!.Slot)
                return HookResult.Continue;
            if (cfg.IgnoreTeammates && attacker.Slot != victim!.Slot && attacker.Team == victim.Team)
                return HookResult.Continue;
        }

        if (LimitReached(victim!.Slot, cfg.Limit))
            return HookResult.Continue;

        LimitUse(victim.Slot);
        info.Damage = MathF.Max(info.Damage * (1f - Math.Min(percent, 100) / 100f), 0f);
        return HookResult.Changed;
    }
}
