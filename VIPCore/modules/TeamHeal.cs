using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class TeamHeal : VipModule
{
    private class Cfg
    {
        public int MinHp { get; set; } = 1;
        public int Percent { get; set; } = 50;
        public float SoundVolume { get; set; } = 0.5f;
        public string OnlyWithWeapon { get; set; } = "";

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWithWeapon);
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "TeamHeal";
    public override string DisplayName => Core.Localizer["vip.module.teamheal"];

    public override void OnLoad() => Core.HookDamage(OnDamage);

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        var attacker = DamageAttacker(info);
        if (!Active(attacker))
            return HookResult.Continue;

        var victim = DamageVictim(entity);
        if (victim == null || victim.Slot == attacker!.Slot || victim.Team != attacker.Team)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker) ?? DefaultCfg;
        var allow = cfg.Allow;
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(attacker)))
            return HookResult.Continue;

        var victimPawn = PlayerCache.Pawn(victim);
        if (victimPawn == null || !victimPawn.IsValid || victimPawn.Health <= 0)
            return HookResult.Continue;

        int maxHp = victimPawn.MaxHealth > 0 ? victimPawn.MaxHealth : 100;
        int heal = Math.Max((int)(info.Damage * cfg.Percent / 100f), cfg.MinHp);
        int newHp = Math.Min(victimPawn.Health + heal, maxHp);

        info.Damage = 0;

        if (newHp > victimPawn.Health)
        {
            victimPawn.Health = newHp;
            NetState.Changed(victimPawn, "CBaseEntity", "m_iHealth");
            SoundUtil.EmitToPlayer(victim, "Healthshot.Success", cfg.SoundVolume);
        }

        return HookResult.Handled;
    }
}
