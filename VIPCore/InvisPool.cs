using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public static class InvisPool
{
    public const int Module = 1;
    public const int Healthshot = 2;

    private const int HiddenAlpha = 127;

    private static VIPCore? _owner;
    private static readonly int[] _sources = new int[64];
    private static int _count;
    private static readonly List<(uint PawnIndex, nint PawnHandle, CsTeam Team)> _pawns = new();
    private static readonly Dictionary<uint, int> _attached = new();
    private static readonly List<(uint Index, nint OwnerHandle, CsTeam Team)> _extras = new();
    private static readonly CsTeam[] _hiddenTeam = new CsTeam[64];
    private static readonly nint[] _hiddenHandle = new nint[64];

    public static void Ensure(VIPCore core)
    {
        if (ReferenceEquals(_owner, core))
            return;

        _owner = core;
        Array.Clear(_sources);
        _count = 0;
        _attached.Clear();

        core.HookTransmit(OnCheckTransmit);
        core.HookEntityDeleted(entity =>
        {
            if (_attached.Count > 0)
                _attached.Remove(entity.Index);
        });
        core.RegisterEventHandler<EventPlayerDeath>((ev, _) => { Clear(ev.Userid?.Slot ?? -1); return HookResult.Continue; }, HookMode.Pre);
        core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { Clear(ev.Userid?.Slot ?? -1); return HookResult.Continue; }, HookMode.Pre);
        core.RegisterEventHandler<EventRoundStart>((_, _) => { ClearAll(); return HookResult.Continue; });
        core.HookMapStart(_ => { Array.Clear(_sources); _count = 0; _attached.Clear(); });
    }

    public static void Attach(int ownerSlot, CEntityInstance? entity)
    {
        if (_owner == null || entity == null || !entity.IsValid || ownerSlot < 0 || ownerSlot >= 64)
            return;

        _attached[entity.Index] = ownerSlot;
    }

    public static void Attach(int ownerSlot, uint entityIndex)
    {
        if (_owner != null && ownerSlot >= 0 && ownerSlot < 64)
            _attached[entityIndex] = ownerSlot;
    }

    public static bool IsHidden(int slot) => slot >= 0 && slot < 64 && _sources[slot] != 0;

    public static int Alpha(int slot) => IsHidden(slot) ? HiddenAlpha : PlayerModel.LegsHidden(slot) ? 254 : 255;

    public static void Set(int slot, int source, bool hidden)
    {
        if (slot < 0 || slot >= 64)
            return;

        int before = _sources[slot];
        int after = hidden ? before | source : before & ~source;
        if (before == after)
            return;

        _sources[slot] = after;

        if (before == 0)
        {
            _count++;
            Paint(slot);
        }
        else if (after == 0)
        {
            _count--;
            Paint(slot);
        }
    }

    public static void Clear(int slot)
    {
        if (slot < 0 || slot >= 64 || _sources[slot] == 0)
            return;

        _sources[slot] = 0;
        _count--;
        Paint(slot);
    }

    public static void ClearAll()
    {
        for (int slot = 0; slot < 64; slot++)
            Clear(slot);

        _count = 0;
    }

    private static void Paint(int slot)
    {
        var pawn = Utilities.GetPlayerFromSlot(slot)?.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        var color = pawn.Render;
        pawn.Render = Color.FromArgb(Alpha(slot), color.R, color.G, color.B);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    private static void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_count <= 0)
            return;

        _pawns.Clear();
        _extras.Clear();
        Array.Clear(_hiddenHandle);
        for (int slot = 0; slot < 64; slot++)
        {
            if (_sources[slot] == 0)
                continue;

            var player = Utilities.GetPlayerFromSlot(slot);
            if (player == null || !player.IsValid)
            {
                _sources[slot] = 0;
                _count--;
                continue;
            }

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                continue;

            _pawns.Add((pawn.Index, pawn.Handle, player.Team));
            _hiddenHandle[slot] = pawn.Handle;
            _hiddenTeam[slot] = player.Team;
        }

        if (_pawns.Count == 0)
            return;

        foreach (var (index, owner) in _attached)
            if (_hiddenHandle[owner] != nint.Zero)
                _extras.Add((index, _hiddenHandle[owner], _hiddenTeam[owner]));

        foreach (var (info, viewer) in infoList)
        {
            if (viewer == null || !viewer.IsValid || viewer.Team == CsTeam.Spectator)
                continue;

            var target = viewer.Pawn.Value?.ObserverServices?.ObserverTarget.Value?.Handle ?? nint.Zero;

            foreach (var (pawnIndex, pawnHandle, team) in _pawns)
            {
                if (team == viewer.Team)
                    continue;

                if (target != nint.Zero && pawnHandle == target)
                    continue;

                if (info.TransmitEntities.Contains(pawnIndex))
                    info.TransmitEntities.Remove(pawnIndex);
            }

            foreach (var (index, ownerHandle, team) in _extras)
            {
                if (team == viewer.Team || (target != nint.Zero && ownerHandle == target))
                    continue;

                if (info.TransmitEntities.Contains(index))
                    info.TransmitEntities.Remove(index);
            }
        }
    }
}
