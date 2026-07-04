using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class KillScreen : VipModule
{
    private class Cfg
    {
        public float Duration { get; set; } = 1.0f;
    }

    public override string Name => "KillScreen";
    public override string DisplayName => Core.Localizer["vip.module.killscreen"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerDeath>(OnDeath);

    private HookResult OnDeath(EventPlayerDeath ev, GameEventInfo info)
    {
        var attacker = ev.Attacker;
        if (!Active(attacker) || attacker == ev.Userid)
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
