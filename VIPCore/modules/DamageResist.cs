using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class DamageResist : VipModule
{
    private class Cfg
    {
        public int Percent { get; set; } = 0;
        public string OnlyWithWeapon { get; set; } = "";
    }

    public override string Name => "DamageResist";
    public override string DisplayName => Core.Localizer["vip.module.damageresist"];

    public override void OnLoad() => Core.RegisterListener<OnEntityTakeDamagePre>(OnDamage);

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        var victim = PawnController(entity);
        if (!Active(victim))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(victim!) ?? new Cfg();
        if (cfg.Percent <= 0)
            return HookResult.Continue;

        var allow = WeaponUtil.ParseCsv(cfg.OnlyWithWeapon);
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(victim!)))
            return HookResult.Continue;

        if (cfg.Percent >= 100)
        {
            info.Damage = 0;
            return HookResult.Changed;
        }

        info.Damage *= 1f - cfg.Percent / 100f;
        return HookResult.Changed;
    }
}
