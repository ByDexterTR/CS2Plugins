using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace AntiTeamFlash;

public class AntiTeamFlash : BasePlugin
{
    public override string ModuleName => "Anti Team Flash";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

    private const int MaxSlots = 64;
    private readonly BlindState[] _legitBlind = new BlindState[MaxSlots];

    private struct BlindState
    {
        public float StartTime;
        public float UntilTime;
        public float Duration;
        public float MaxAlpha;
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerBlind>(OnPlayerBlind, HookMode.Post);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Array.Clear(_legitBlind);
        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        int slot = @event.Userid?.Slot ?? -1;
        if (slot >= 0 && slot < MaxSlots)
            _legitBlind[slot] = default;
        return HookResult.Continue;
    }

    private HookResult OnPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
    {
        var victim = @event.Userid;
        if (victim == null || !victim.IsValid)
            return HookResult.Continue;

        int slot = victim.Slot;
        if (slot < 0 || slot >= MaxSlots)
            return HookResult.Continue;

        var pawn = victim.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        var attacker = @event.Attacker;
        if (attacker == null || !attacker.IsValid)
            return HookResult.Continue;

        bool teamFlash = attacker.TeamNum == victim.TeamNum && attacker.UserId != victim.UserId;

        ref var state = ref _legitBlind[slot];

        if (!teamFlash)
        {
            state.StartTime = pawn.BlindStartTime;
            state.UntilTime = pawn.BlindUntilTime;
            state.Duration = pawn.FlashDuration;
            state.MaxAlpha = pawn.FlashMaxAlpha;
            return HookResult.Continue;
        }

        float now = Server.CurrentTime;

        if (state.UntilTime > now)
        {
            pawn.BlindStartTime = state.StartTime;
            pawn.BlindUntilTime = state.UntilTime;
            pawn.FlashDuration = state.Duration;
            pawn.FlashMaxAlpha = state.MaxAlpha;
        }
        else
        {
            pawn.BlindStartTime = now;
            pawn.BlindUntilTime = now;
            pawn.FlashDuration = 0f;
            pawn.FlashMaxAlpha = 0f;
        }

        return HookResult.Continue;
    }
}
