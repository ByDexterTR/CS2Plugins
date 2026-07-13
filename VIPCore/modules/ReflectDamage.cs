using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class ReflectDamage : VipModule
{
    private class Cfg
    {
        public int ReflectPercent { get; set; } = 30;
        public int MaxPerShot { get; set; } = 50;
        public string OnlyWithWeapon { get; set; } = "";
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
    }

    public override string Name => "ReflectDamage";
    public override string DisplayName => Core.Localizer["vip.module.reflectdamage"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerHurt>(OnHurt);

    private HookResult OnHurt(EventPlayerHurt ev, GameEventInfo info)
    {
        var victim = ev.Userid;
        if (!Active(victim))
            return HookResult.Continue;

        var attacker = ev.Attacker;
        if (attacker == null || !attacker.IsValid)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(victim!) ?? new Cfg();
        if (attacker.Slot == victim!.Slot)
        {
            if (cfg.IgnoreSelf)
                return HookResult.Continue;
        }
        else if (cfg.IgnoreTeammates && attacker.Team == victim.Team)
        {
            return HookResult.Continue;
        }

        var pawn = attacker.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.Health <= 0)
            return HookResult.Continue;

        var allow = WeaponUtil.ParseCsv(cfg.OnlyWithWeapon);
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(victim!)))
            return HookResult.Continue;

        int dmg = Math.Min(ev.DmgHealth * cfg.ReflectPercent / 100, cfg.MaxPerShot);
        if (dmg <= 0)
            return HookResult.Continue;

        pawn.Health = Math.Max(pawn.Health - dmg, 1);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        return HookResult.Continue;
    }
}
