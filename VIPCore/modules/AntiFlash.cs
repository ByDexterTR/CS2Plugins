using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class AntiFlash : VipModule
{
    private class Cfg
    {
        public bool Self { get; set; } = true;
        public bool Enemy { get; set; } = true;
        public bool Teammates { get; set; } = true;
        public int Limit { get; set; } = 0;
    }

    public override string Name => "AntiFlash";
    public override string DisplayName => Core.Localizer["vip.module.antiflash"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerBlind>(OnBlind, HookMode.Post);

    private HookResult OnBlind(EventPlayerBlind ev, GameEventInfo info)
    {
        var victim = ev.Userid;
        if (!Active(victim))
            return HookResult.Continue;

        var pawn = victim!.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(victim) ?? new Cfg();
        var attacker = ev.Attacker;

        bool block;
        if (attacker == null || !attacker.IsValid)
            block = cfg.Enemy;
        else if (attacker.UserId == victim.UserId)
            block = cfg.Self;
        else if (attacker.TeamNum == victim.TeamNum)
            block = cfg.Teammates;
        else
            block = cfg.Enemy;

        if (block)
        {
            if (LimitReached(victim.Slot, cfg.Limit))
                return HookResult.Continue;

            pawn.FlashDuration = 0f;
            pawn.FlashMaxAlpha = 0f;
            pawn.BlindUntilTime = Server.CurrentTime;
            LimitUse(victim.Slot);
        }

        return HookResult.Continue;
    }
}
