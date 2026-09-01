using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public static class ActivityFilter
{
    public const float ShotWindow = 1f;
    public const float MoveSpeed = 1f;

    private static VIPCore? _owner;
    private static readonly float[] _lastShot = new float[64];

    public static void Ensure(VIPCore core)
    {
        if (ReferenceEquals(_owner, core))
            return;

        _owner = core;
        Array.Clear(_lastShot);

        core.RegisterEventHandler<EventWeaponFire>((ev, _) =>
        {
            int slot = ev.Userid?.Slot ?? -1;
            if (slot >= 0 && slot < 64)
                _lastShot[slot] = Server.CurrentTime;
            return HookResult.Continue;
        });
        core.RegisterEventHandler<EventRoundStart>((_, _) => { Array.Clear(_lastShot); return HookResult.Continue; });
    }

    public static bool Shooting(int slot) =>
        slot >= 0 && slot < 64 && _lastShot[slot] > 0f && Server.CurrentTime - _lastShot[slot] <= ShotWindow;

    public static bool Moving(CCSPlayerPawn? pawn)
    {
        var velocity = pawn?.AbsVelocity;
        if (velocity == null)
            return false;

        return MathF.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y) >= MoveSpeed;
    }

    public static bool Matches(int mode, CCSPlayerController target, CCSPlayerPawn? pawn)
    {
        if (mode <= 0)
            return true;

        bool shooting = false;
        bool moving = false;

        for (int value = mode; value > 0; value /= 10)
        {
            int digit = value % 10;
            if (digit == 1)
                shooting = true;
            else if (digit == 2)
                moving = true;
        }

        if (!shooting && !moving)
            return true;

        return (shooting && Shooting(target.Slot)) || (moving && Moving(pawn));
    }
}
