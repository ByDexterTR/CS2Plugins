using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class AntiHS : VipModule
{
    private class Cfg
    {
        public int Percent { get; set; } = 0;
        public string OnlyWithWeapon { get; set; } = "";
    }

    public override string Name => "AntiHS";
    public override string DisplayName => Core.Localizer["vip.module.antihs"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerHurt>(OnHurt);

    private HookResult OnHurt(EventPlayerHurt ev, GameEventInfo info)
    {
        var victim = ev.Userid;
        if (!Active(victim) || ev.Attacker == victim)
            return HookResult.Continue;

        if (ev.Hitgroup != (int)HitGroup_t.HITGROUP_HEAD)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(victim!) ?? new Cfg();
        if (cfg.Percent >= 100)
            return HookResult.Continue;

        var allow = WeaponUtil.ParseCsv(cfg.OnlyWithWeapon);
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(victim!)))
            return HookResult.Continue;

        var pawn = victim!.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        int restore = ev.DmgHealth * (100 - cfg.Percent) / 100;
        if (restore <= 0)
            return HookResult.Continue;

        pawn.Health = Math.Min(pawn.Health + restore, pawn.MaxHealth);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        return HookResult.Continue;
    }
}
