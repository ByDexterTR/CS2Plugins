using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class TeamHeal : VipModule
{
    private class Cfg
    {
        [JsonPropertyName("minhp")]
        public int MinHp { get; set; } = 1;
        public int Percent { get; set; } = 50;
        public string OnlyWithWeapon { get; set; } = "";
    }

    public override string Name => "TeamHeal";
    public override string DisplayName => Core.Localizer["vip.module.teamheal"];

    public override void OnLoad() => Core.RegisterListener<OnEntityTakeDamagePre>(OnDamage);

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        if (info.Attacker?.Value == null)
            return HookResult.Continue;

        var attacker = PawnController(info.Attacker.Value);
        if (!Active(attacker))
            return HookResult.Continue;

        var victim = PawnController(entity);
        if (victim == null || victim.Slot == attacker!.Slot || victim.Team != attacker.Team)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker) ?? new Cfg();
        var allow = WeaponUtil.ParseCsv(cfg.OnlyWithWeapon);
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(attacker)))
            return HookResult.Continue;

        var victimPawn = victim.PlayerPawn.Value;
        if (victimPawn == null || !victimPawn.IsValid || victimPawn.Health <= 0)
            return HookResult.Continue;

        int maxHp = victimPawn.MaxHealth > 0 ? victimPawn.MaxHealth : 100;
        int heal = Math.Max((int)(info.Damage * cfg.Percent / 100f), cfg.MinHp);
        int newHp = Math.Min(victimPawn.Health + heal, maxHp);

        info.Damage = 0;

        if (newHp > victimPawn.Health)
        {
            victimPawn.Health = newHp;
            Utilities.SetStateChanged(victimPawn, "CBaseEntity", "m_iHealth");
            victim.EmitSound("Healthshot.Success");
        }

        return HookResult.Handled;
    }
}
