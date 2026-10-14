using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class Bhop : VipModule
{
    private class Cfg
    {
        public bool Autostrafe { get; set; } = true;
        public float MaxSpeed { get; set; } = 500f;
        public float JumpBoost { get; set; } = 1.1f;
        public float JumpVelocity { get; set; } = 300f;
    }

    private static readonly Cfg DefaultCfg = new();
    private readonly int[] _lastJumpTick = new int[64];

    public override string Name => "Bhop";
    public override string DisplayName => Core.Localizer["vip.module.bhop"];

    public override void OnLoad() => Core.HookTick(OnTick);

    private void OnTick()
    {
        if (Core.IsFreezeTime())
            return;

        int tick = Server.TickCount;
        foreach (var player in ActivePlayers())
        {
            nint pawn = PlayerCache.Pointer(player);
            if (pawn == 0)
                continue;

            int slot = player.Slot;
            bool hasButtons = PlayerCache.Buttons(pawn, out ulong buttons, out ulong queued);
            if ((queued & (ulong)PlayerButtons.Jump) != 0)
                _lastJumpTick[slot] = tick;

            bool held = hasButtons && (buttons & (ulong)PlayerButtons.Jump) != 0;
            bool jumpPressed = held || _lastJumpTick[slot] + 20 >= tick;
            if (!jumpPressed || (PlayerCache.Flags(pawn) & (uint)PlayerFlags.FL_ONGROUND) == 0)
                continue;

            var moveType = (MoveType_t)PlayerCache.MoveType(pawn);
            if (moveType.HasFlag(MoveType_t.MOVETYPE_LADDER))
                continue;

            var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
            ref var velocity = ref PlayerCache.Velocity(pawn);
            velocity.Z = cfg.JumpVelocity;

            if (!cfg.Autostrafe)
                continue;

            float vX = velocity.X;
            float vY = velocity.Y;
            double speed = Math.Sqrt(vX * vX + vY * vY);
            double scale = 1d;

            if (speed < cfg.MaxSpeed)
                scale = Math.Min(speed * cfg.JumpBoost, cfg.MaxSpeed) / (speed == 0 ? 1 : speed);
            else if (speed > cfg.MaxSpeed)
                scale = cfg.MaxSpeed / speed;

            velocity.X = (float)(vX * scale);
            velocity.Y = (float)(vY * scale);
        }
    }
}
