using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class AntiHS : VipModule
{
    private class Cfg
    {
        public int Percent { get; set; } = 0;
        public string OnlyWithWeapon { get; set; } = "";
        public int Limit { get; set; } = 0;

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWithWeapon);
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "AntiHS";
    public override string DisplayName => Core.Localizer["vip.module.antihs"];

    public override void OnLoad() => Core.HookDamage(OnDamage);

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        if (info.Damage <= 0f || ((long)info.BitsDamageType & (long)DamageTypes_t.DMG_BULLET) == 0
            || info.GetHitGroup() != HitGroup_t.HITGROUP_HEAD)
            return HookResult.Continue;

        var victim = PawnController(entity);
        if (!Active(victim))
            return HookResult.Continue;

        var attacker = PawnController(info.Attacker?.Value);
        if (attacker != null && attacker.Slot == victim!.Slot)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(victim!) ?? DefaultCfg;
        if (cfg.Percent >= 100)
            return HookResult.Continue;

        var allow = cfg.Allow;
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(victim!)))
            return HookResult.Continue;

        if (LimitReached(victim!.Slot, cfg.Limit))
            return HookResult.Continue;

        LimitUse(victim.Slot);
        info.Damage = MathF.Max(info.Damage * Math.Max(cfg.Percent, 0) / 100f, 0f);
        return HookResult.Changed;
    }
}
