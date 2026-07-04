using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class AutoHS : VipModule
{
    private class Cfg
    {
        public float Multiplier { get; set; } = 4f;
        public string OnlyWithWeapon { get; set; } = "";
    }

    public override string Name => "AutoHS";
    public override string DisplayName => Core.Localizer["vip.module.autohs"];

    public override void OnLoad() => Core.RegisterListener<OnEntityTakeDamagePre>(OnDamage);

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        if (info.Attacker?.Value == null)
            return HookResult.Continue;

        var attacker = PawnController(info.Attacker.Value);
        if (!Active(attacker) || PawnController(entity) == null)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker!) ?? new Cfg();
        var allow = WeaponUtil.ParseCsv(cfg.OnlyWithWeapon);
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(attacker!)))
            return HookResult.Continue;

        info.Damage *= cfg.Multiplier;
        return HookResult.Changed;
    }
}
