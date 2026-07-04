using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

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

    private readonly int[] _lastJumpTick = new int[64];

    public override string Name => "Bhop";
    public override string DisplayName => Core.Localizer["vip.module.bhop"];

    public override void OnLoad() => Core.RegisterListener<OnTick>(OnTick);

    private void OnTick()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || player.IsBot || !Active(player))
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            int slot = player.Slot;
            var flags = (PlayerFlags)pawn.Flags;

            if ((pawn.MovementServices?.QueuedButtonChangeMask & (ulong)PlayerButtons.Jump) != 0)
                _lastJumpTick[slot] = Server.TickCount;

            bool jumpPressed = player.Buttons.HasFlag(PlayerButtons.Jump) || _lastJumpTick[slot] + 20 >= Server.TickCount;

            if (!jumpPressed || !flags.HasFlag(PlayerFlags.FL_ONGROUND) || pawn.MoveType.HasFlag(MoveType_t.MOVETYPE_LADDER))
                continue;

            var cfg = GroupValue<Cfg>(player) ?? new Cfg();
            pawn.AbsVelocity.Z = cfg.JumpVelocity;

            if (!cfg.Autostrafe)
                continue;

            float vX = pawn.AbsVelocity.X;
            float vY = pawn.AbsVelocity.Y;
            double speed = Math.Sqrt(vX * vX + vY * vY);
            double scale = 1d;

            if (speed < cfg.MaxSpeed)
                scale = Math.Min(speed * cfg.JumpBoost, cfg.MaxSpeed) / (speed == 0 ? 1 : speed);
            else if (speed > cfg.MaxSpeed)
                scale = cfg.MaxSpeed / speed;

            pawn.AbsVelocity.X = (float)(vX * scale);
            pawn.AbsVelocity.Y = (float)(vY * scale);
        }
    }
}
