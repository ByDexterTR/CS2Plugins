using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public static class StanceFilter
{
    public const float MoveSpeed = 10f;

    public static bool Matches(int mode, CCSPlayerPawn? pawn)
    {
        if (mode <= 0 || pawn == null || !pawn.IsValid)
            return true;

        bool walking = false, crouching = false, jumping = false, standing = false;

        for (int value = mode; value > 0; value /= 10)
        {
            switch (value % 10)
            {
                case 1: walking = true; break;
                case 2: crouching = true; break;
                case 3: jumping = true; break;
                case 4: standing = true; break;
            }
        }

        if (!walking && !crouching && !jumping && !standing)
            return true;

        bool onGround = ((PlayerFlags)pawn.Flags & PlayerFlags.FL_ONGROUND) != 0;

        if (jumping && !onGround)
            return true;

        if (crouching && pawn.MovementServices?.As<CCSPlayer_MovementServices>().DuckAmount > 0.5f)
            return true;

        if (!walking && !standing)
            return false;

        if (!onGround)
            return false;

        bool moving = Speed(pawn) >= MoveSpeed;
        return moving ? walking : standing;
    }

    private static float Speed(CCSPlayerPawn pawn)
    {
        var velocity = pawn.AbsVelocity;
        if (velocity == null)
            return 0f;

        return MathF.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y);
    }
}
