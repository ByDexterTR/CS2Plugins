using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class AutoHS : VipModule
{
    private class Cfg
    {
        public float Multiplier { get; set; } = 4f;
        public string OnlyWithWeapon { get; set; } = "";
        public bool IgnoreTeammates { get; set; } = true;
        public int Limit { get; set; } = 0;

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWithWeapon);
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "AutoHS";
    public override string DisplayName => Core.Localizer["vip.module.autohs"];

    private static readonly string[] NoHsWeapons =
    {
        "hegrenade", "molotov", "incgrenade", "inferno", "decoy", "flashbang",
        "smokegrenade", "taser", "world", "planted_c4", "c4"
    };

    public override void OnLoad()
    {
        Core.RegisterListener<OnEntityTakeDamagePre>(OnDamage);
        Core.RegisterEventHandler<EventPlayerDeath>(OnDeathPre, HookMode.Pre);
    }

    private HookResult OnDeathPre(EventPlayerDeath ev, GameEventInfo info)
    {
        if (ev.Headshot)
            return HookResult.Continue;

        var attacker = ev.Attacker;
        var victim = ev.Userid;
        if (!Active(attacker) || victim == null || !victim.IsValid || victim.Slot == attacker!.Slot)
            return HookResult.Continue;

        string weapon = ev.Weapon;
        if (string.IsNullOrEmpty(weapon) || weapon.StartsWith("knife") || weapon.Contains("bayonet") || NoHsWeapons.Contains(weapon))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker) ?? DefaultCfg;
        if (cfg.IgnoreTeammates && victim.Team == attacker.Team)
            return HookResult.Continue;

        var allow = cfg.Allow;
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, "weapon_" + weapon))
            return HookResult.Continue;

        ev.Headshot = true;
        return HookResult.Changed;
    }

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        if (info.Attacker?.Value == null)
            return HookResult.Continue;

        var attacker = PawnController(info.Attacker.Value);
        if (!Active(attacker))
            return HookResult.Continue;

        var victim = PawnController(entity);
        if (victim == null || victim.Slot == attacker!.Slot)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker) ?? DefaultCfg;
        if (cfg.IgnoreTeammates && victim.Team == attacker.Team)
            return HookResult.Continue;

        var allow = cfg.Allow;
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(attacker)))
            return HookResult.Continue;

        if (LimitReached(attacker.Slot, cfg.Limit))
            return HookResult.Continue;

        info.Damage *= cfg.Multiplier;
        LimitUse(attacker.Slot);
        return HookResult.Changed;
    }
}
