using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class DecoyTeleport : VipModule
{
    private class Cfg
    {
        public int Limit { get; set; } = 0;
    }

    private readonly int[] _used = new int[64];

    public override string Name => "DecoyTeleport";
    public override string DisplayName => Core.Localizer["vip.module.decoyteleport"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventDecoyStarted>(OnDecoy);
        Core.RegisterEventHandler<EventRoundStart>((_, __) => { Array.Clear(_used); return HookResult.Continue; });
    }

    private HookResult OnDecoy(EventDecoyStarted ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player) || !IsAlive(player))
            return HookResult.Continue;

        int slot = player!.Slot;
        int limit = (GroupValue<Cfg>(player) ?? new Cfg()).Limit;
        if (limit > 0 && _used[slot] >= limit)
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        pawn.Teleport(new Vector(ev.X, ev.Y, ev.Z + 10f), pawn.EyeAngles, new Vector(0, 0, 0));
        _used[slot]++;
        return HookResult.Continue;
    }
}
