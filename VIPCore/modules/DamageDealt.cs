using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class DamageDealt : VipModule
{
    private class Cfg
    {
        public int Percent { get; set; } = 0;
        public string OnlyWithWeapon { get; set; } = "";
    }

    public override string Name => "DamageDealt";
    public override string DisplayName => Core.Localizer["vip.module.damagedealt"];

    public override void OnLoad() => Core.RegisterListener<OnEntityTakeDamagePre>(OnDamage);

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        if (info.Attacker?.Value == null)
            return HookResult.Continue;

        var attacker = PawnController(info.Attacker.Value);
        if (!Active(attacker))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker!) ?? new Cfg();
        if (cfg.Percent == 0)
            return HookResult.Continue;

        var allow = WeaponUtil.ParseCsv(cfg.OnlyWithWeapon);
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(attacker!)))
            return HookResult.Continue;

        info.Damage *= 1f + cfg.Percent / 100f;
        return HookResult.Changed;
    }
}
