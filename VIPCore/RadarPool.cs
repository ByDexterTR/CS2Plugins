using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public static class RadarPool
{
    private const string Radar = "sv_disable_radar";

    private static VIPCore? _owner;
    private static readonly int[] _until = new int[64];
    private static readonly bool[] _sent = new bool[64];

    public static void Ensure(VIPCore core)
    {
        if (ReferenceEquals(_owner, core))
            return;

        _owner = core;
        Array.Clear(_until);
        Array.Clear(_sent);

        core.HookTick(OnTick, 4);
        core.RegisterEventHandler<EventRoundStart>((_, _) => { Array.Clear(_until); return HookResult.Continue; });
        core.RegisterEventHandler<EventPlayerConnectFull>((ev, _) =>
        {
            int slot = ev.Userid?.Slot ?? -1;
            if (slot >= 0 && slot < 64)
            {
                _until[slot] = 0;
                _sent[slot] = false;
            }
            return HookResult.Continue;
        });
        core.HookMapStart(_ => { Array.Clear(_until); Array.Clear(_sent); });
    }

    public static int TicksFor(float seconds) => Math.Max((int)MathF.Ceiling(seconds * 64f), 1) + 4;

    public static void Jam(int slot, int ticks)
    {
        if (_owner == null || slot < 0 || slot >= 64)
            return;

        int until = Server.TickCount + Math.Max(ticks, 1);
        if (until > _until[slot])
            _until[slot] = until;
    }

    public static void Restore()
    {
        if (_owner == null)
            return;

        string value = ServerConVar.Value(Radar);
        for (int slot = 0; slot < 64; slot++)
        {
            _until[slot] = 0;
            if (!_sent[slot])
                continue;

            _sent[slot] = false;
            var player = _owner.PlayerAt(slot);
            if (player != null && player.IsValid && !player.IsBot)
                player.ReplicateConVar(Radar, value);
        }
    }

    private static void OnTick()
    {
        var core = _owner;
        if (core == null)
            return;

        int now = Server.TickCount;
        foreach (var player in core.Humans)
        {
            if (player == null || !player.IsValid || player.Slot >= 64)
                continue;

            int slot = player.Slot;
            bool jam = player.PawnIsAlive ? _until[slot] > now : ObservedJammed(player, now);
            if (jam == _sent[slot])
                continue;

            _sent[slot] = jam;
            player.ReplicateConVar(Radar, jam ? "1" : ServerConVar.Value(Radar));
        }
    }

    private static bool ObservedJammed(CCSPlayerController player, int now)
    {
        var observed = VipModule.PawnController(player.Pawn.Value?.ObserverServices?.ObserverTarget.Value);
        int slot = observed?.Slot ?? -1;
        return slot >= 0 && slot < 64 && _until[slot] > now;
    }
}
