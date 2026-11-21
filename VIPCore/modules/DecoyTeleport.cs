using System.Numerics;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class DecoyTeleport : VipModule
{
    private class Cfg
    {
        public int Limit { get; set; } = 0;
    }

    private static readonly Vector3 HullMins = new(-16f, -16f, 0f);
    private static readonly Vector3 HullMaxs = new(16f, 16f, 72f);
    private static readonly float[] Heights = { 10f, 24f, 40f };
    private static readonly float[] Radii = { 0f, 20f, 36f };

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

        var pawn = PlayerCache.Pawn(player);
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        var target = SafeSpot(pawn, new Vector3(ev.X, ev.Y, ev.Z));
        if (target == null)
            return HookResult.Continue;

        var spot = target.Value;
        pawn.Teleport(new CounterStrikeSharp.API.Modules.Utils.Vector(spot.X, spot.Y, spot.Z), pawn.EyeAngles, null);
        _used[slot]++;
        return HookResult.Continue;
    }

    private static Vector3? SafeSpot(CCSPlayerPawn pawn, Vector3 decoy)
    {
        foreach (float radius in Radii)
        {
            int steps = radius == 0f ? 1 : 8;
            for (int i = 0; i < steps; i++)
            {
                float angle = i * MathF.PI / 4f;
                foreach (float height in Heights)
                {
                    var spot = new Vector3(decoy.X + MathF.Cos(angle) * radius, decoy.Y + MathF.Sin(angle) * radius, decoy.Z + height);
                    bool? free = NativeTrace.HullFree(pawn, spot, HullMins, HullMaxs);
                    if (free == null)
                        return new Vector3(decoy.X, decoy.Y, decoy.Z + Heights[0]);
                    if (free == true && (radius == 0f || Reachable(pawn, decoy, spot)))
                        return spot;
                }
            }
        }

        return null;
    }

    private static bool Reachable(CCSPlayerPawn pawn, Vector3 decoy, Vector3 spot)
    {
        var from = new Vector3(decoy.X, decoy.Y, spot.Z + 36f);
        var to = new Vector3(spot.X, spot.Y, spot.Z + 36f);
        var hit = NativeTrace.TraceRay(pawn, from, to, NativeTrace.MaskPlayerSolid);
        return hit is not { DidHit: true };
    }
}
