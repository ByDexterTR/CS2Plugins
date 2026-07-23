using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class DamageResist : VipModule
{
    private class Cfg
    {
        public int Percent { get; set; } = 0;
        public string OnlyWithWeapon { get; set; } = "";
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
        public int Limit { get; set; } = 0;

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWithWeapon);
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "DamageResist";
    public override string DisplayName => Core.Localizer["vip.module.damageresist"];

    public override void OnLoad() => Core.RegisterListener<OnEntityTakeDamagePre>(OnDamage);

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        var victim = PawnController(entity);
        if (!Active(victim))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(victim!) ?? DefaultCfg;
        if (cfg.Percent <= 0)
            return HookResult.Continue;

        var attacker = PawnController(info.Attacker?.Value);
        if (attacker != null)
        {
            if (cfg.IgnoreSelf && attacker.Slot == victim!.Slot)
                return HookResult.Continue;
            if (cfg.IgnoreTeammates && attacker.Slot != victim!.Slot && attacker.Team == victim.Team)
                return HookResult.Continue;
        }

        var allow = cfg.Allow;
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(victim!)))
            return HookResult.Continue;

        if (LimitReached(victim!.Slot, cfg.Limit))
            return HookResult.Continue;

        LimitUse(victim.Slot);

        if (cfg.Percent >= 100)
        {
            info.Damage = 0;
            return HookResult.Changed;
        }

        info.Damage *= 1f - cfg.Percent / 100f;
        return HookResult.Changed;
    }
}
