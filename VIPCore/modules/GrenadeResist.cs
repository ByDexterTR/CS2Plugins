using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class GrenadeResist : VipModule
{
    private class Cfg
    {
        public int Percent { get; set; } = 0;
        public string OnlyWithGrenade { get; set; } = "";
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
    }

    public override string Name => "GrenadeResist";
    public override string DisplayName => Core.Localizer["vip.module.grenaderesist"];

    public override void OnLoad() => Core.RegisterListener<OnEntityTakeDamagePre>(OnDamage);

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        var victim = PawnController(entity);
        if (!Active(victim))
            return HookResult.Continue;

        long bits = (long)info.BitsDamageType;
        bool isBlast = (bits & (long)DamageTypes_t.DMG_BLAST) != 0;
        bool isBurn = (bits & (long)DamageTypes_t.DMG_BURN) != 0;
        if (!isBlast && !isBurn)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(victim!) ?? new Cfg();
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

        var allow = WeaponUtil.ParseCsv(cfg.OnlyWithGrenade);
        if (allow.Count > 0 && !allow.Any(a => TypeMatches(a, isBlast, isBurn)))
            return HookResult.Continue;

        info.Damage *= 1f - Math.Min(cfg.Percent, 100) / 100f;
        return HookResult.Changed;
    }

    private static bool TypeMatches(string key, bool isBlast, bool isBurn) => key.Trim().ToLowerInvariant() switch
    {
        "he" => isBlast,
        "molotov" or "inferno" or "fire" or "incendiary" => isBurn,
        _ => false
    };
}
