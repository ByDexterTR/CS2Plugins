using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class FlashDuration : VipModule
{
    private class Cfg
    {
        public float Multiplier { get; set; } = 1.5f;
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
        public int Limit { get; set; } = 0;
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "FlashDuration";
    public override string DisplayName => Core.Localizer["vip.module.flashduration"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerBlind>(OnBlind, HookMode.Post);

    private HookResult OnBlind(EventPlayerBlind ev, GameEventInfo info)
    {
        var attacker = ev.Attacker;
        if (!Active(attacker))
            return HookResult.Continue;

        var victim = ev.Userid;
        if (victim == null || !victim.IsValid)
            return HookResult.Continue;

        var pawn = victim.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.FlashDuration <= 0f)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker!) ?? DefaultCfg;
        float scale = Math.Clamp(cfg.Multiplier, 0f, 10f);
        if (Math.Abs(scale - 1f) < 0.001f)
            return HookResult.Continue;

        bool isSelf = victim.Slot == attacker!.Slot;
        if (isSelf && cfg.IgnoreSelf)
            return HookResult.Continue;
        if (!isSelf && victim.Team == attacker.Team && cfg.IgnoreTeammates)
            return HookResult.Continue;

        if (LimitReached(attacker.Slot, cfg.Limit))
            return HookResult.Continue;

        float duration = pawn.FlashDuration * scale;
        pawn.FlashDuration = duration;
        pawn.BlindUntilTime = Server.CurrentTime + duration;

        LimitUse(attacker.Slot);
        return HookResult.Continue;
    }
}
