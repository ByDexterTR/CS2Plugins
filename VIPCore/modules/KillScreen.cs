using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace VIPCore;

public class KillScreen : VipModule
{
    private class Cfg
    {
        public float Duration { get; set; } = 1.0f;
    }

    private static ConVar? _cvFfa;

    public override string Name => "KillScreen";
    public override string DisplayName => Core.Localizer["vip.module.killscreen"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerDeath>(OnDeath);

    private HookResult OnDeath(EventPlayerDeath ev, GameEventInfo info)
    {
        var attacker = ev.Attacker;
        var victim = ev.Userid;
        if (!Active(attacker) || victim == null || !victim.IsValid || victim.Slot == attacker!.Slot)
            return HookResult.Continue;

        _cvFfa ??= ConVar.Find("mp_teammates_are_enemies");
        bool ffa = _cvFfa?.GetPrimitiveValue<bool>() ?? false;
        if (!ffa && victim.Team == attacker.Team)
            return HookResult.Continue;

        var pawn = attacker!.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        float duration = (GroupValue<Cfg>(attacker) ?? new Cfg()).Duration;
        pawn.HealthShotBoostExpirationTime = Server.CurrentTime + duration;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flHealthShotBoostExpirationTime");
        return HookResult.Continue;
    }
}
