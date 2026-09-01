using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class OneShot : VipModule
{
    private class Cfg
    {
        public string Weapons { get; set; } = "";
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
        public int Limit { get; set; } = 0;

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(Weapons);
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "OneShot";
    public override string DisplayName => Core.Localizer["vip.module.oneshot"];

    public override void OnLoad() => Core.HookDamage(OnDamage);

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        if (info.Attacker?.Value == null)
            return HookResult.Continue;

        var attacker = PawnController(info.Attacker.Value);
        if (!Active(attacker))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker!) ?? DefaultCfg;

        var victim = PawnController(entity);
        if (victim != null)
        {
            if (cfg.IgnoreSelf && victim.Slot == attacker!.Slot)
                return HookResult.Continue;
            if (cfg.IgnoreTeammates && victim.Slot != attacker!.Slot && victim.Team == attacker.Team)
                return HookResult.Continue;
        }

        var allow = cfg.Allow;
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(attacker!)))
            return HookResult.Continue;

        if (LimitReached(attacker!.Slot, cfg.Limit))
            return HookResult.Continue;

        info.Damage = 1000f;
        LimitUse(attacker.Slot);
        return HookResult.Changed;
    }
}
