using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class Vampire : VipModule
{
    private class Cfg
    {
        public int HealPercent { get; set; } = 50;
        public string OnlyWithWeapon { get; set; } = "";
        public int MaxOverheal { get; set; } = 100;
    }

    public override string Name => "Vampire";
    public override string DisplayName => Core.Localizer["vip.module.vampire"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerHurt>(OnHurt);

    private HookResult OnHurt(EventPlayerHurt ev, GameEventInfo info)
    {
        var attacker = ev.Attacker;
        if (!Active(attacker) || attacker == ev.Userid)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker!) ?? new Cfg();
        var allow = WeaponUtil.ParseCsv(cfg.OnlyWithWeapon);
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(attacker!)))
            return HookResult.Continue;

        var pawn = attacker!.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        int heal = ev.DmgHealth * cfg.HealPercent / 100;
        if (heal <= 0)
            return HookResult.Continue;

        int newHp = Math.Min(pawn.Health + heal, cfg.MaxOverheal);
        if (newHp > pawn.Health)
        {
            pawn.Health = newHp;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }

        return HookResult.Continue;
    }
}
