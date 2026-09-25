using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public static class ScalePool
{
    private const int Sources = 2;

    public const int Bullet = 0;
    public const int Healthshot = 1;

    private static VIPCore? _owner;
    private static readonly float[,] _scale = new float[64, Sources];
    private static readonly int[,] _order = new int[64, Sources];
    private static readonly float[] _base = new float[64];
    private static int _stamp;

    public static void Ensure(VIPCore core)
    {
        if (ReferenceEquals(_owner, core))
            return;

        _owner = core;
        Array.Clear(_scale);

        core.RegisterEventHandler<EventPlayerSpawn>((ev, _) => { Restore(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { Drop(ev.Userid?.Slot ?? -1); return HookResult.Continue; }, HookMode.Pre);
        core.HookMapStart(_ => Array.Clear(_scale));
    }

    public static bool Active(int slot)
    {
        if (slot < 0 || slot >= 64)
            return false;

        for (int source = 0; source < Sources; source++)
            if (_scale[slot, source] > 0f)
                return true;

        return false;
    }

    public static void Push(CCSPlayerPawn pawn, int slot, int source, float scale)
    {
        if (_owner == null || slot < 0 || slot >= 64 || scale <= 0f || !pawn.IsValid)
            return;

        if (!Active(slot))
            _base[slot] = PlayerSize.Current(pawn);

        _scale[slot, source] = scale;
        _order[slot, source] = ++_stamp;
        PlayerSize.Set(pawn, scale);
    }

    public static void Pop(int slot, int source)
    {
        if (slot < 0 || slot >= 64 || _scale[slot, source] <= 0f)
            return;

        _scale[slot, source] = 0f;

        var pawn = Utilities.GetPlayerFromSlot(slot)?.PlayerPawn.Value;
        if (pawn != null && pawn.IsValid)
            PlayerSize.Set(pawn, Top(slot));
    }

    public static bool Rebase(int slot, float scale)
    {
        if (!Active(slot))
            return false;

        _base[slot] = scale;
        return true;
    }

    private static float Top(int slot)
    {
        float scale = _base[slot];
        int latest = 0;

        for (int source = 0; source < Sources; source++)
        {
            if (_scale[slot, source] > 0f && _order[slot, source] > latest)
            {
                latest = _order[slot, source];
                scale = _scale[slot, source];
            }
        }

        return scale;
    }

    private static void Restore(int slot)
    {
        if (!Active(slot))
            return;

        Drop(slot);

        var pawn = Utilities.GetPlayerFromSlot(slot)?.PlayerPawn.Value;
        if (pawn != null && pawn.IsValid)
            PlayerSize.Set(pawn, _base[slot]);
    }

    private static void Drop(int slot)
    {
        if (slot < 0 || slot >= 64)
            return;

        for (int source = 0; source < Sources; source++)
            _scale[slot, source] = 0f;
    }
}
