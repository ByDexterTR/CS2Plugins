using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public static unsafe class Teleporter
{
    private static readonly int Offset = GameData.GetOffset("CBaseEntity_Teleport");

    public static void Teleport(CEntityInstance entity, Vector? position, QAngle? angles = null, Vector? velocity = null)
    {
        nint handle = entity.Handle;
        if (handle == 0)
            return;

        var call = (delegate* unmanaged<nint, nint, nint, nint, void>)(*(nint**)handle)[Offset];
        call(handle, position?.Handle ?? 0, angles?.Handle ?? 0, velocity?.Handle ?? 0);
    }
}
