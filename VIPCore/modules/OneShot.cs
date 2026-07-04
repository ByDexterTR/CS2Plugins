using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class OneShot : VipModule
{
    private class Cfg
    {
        public string Weapons { get; set; } = "";
    }

    public override string Name => "OneShot";
    public override string DisplayName => Core.Localizer["vip.module.oneshot"];

    public override void OnLoad() => Core.RegisterListener<OnEntityTakeDamagePre>(OnDamage);

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        if (info.Attacker?.Value == null)
            return HookResult.Continue;

        var attacker = PawnController(info.Attacker.Value);
        if (!Active(attacker))
            return HookResult.Continue;

        var allow = WeaponUtil.ParseCsv((GroupValue<Cfg>(attacker!) ?? new Cfg()).Weapons);
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(attacker!)))
            return HookResult.Continue;

        info.Damage = 1000f;
        return HookResult.Changed;
    }
}
