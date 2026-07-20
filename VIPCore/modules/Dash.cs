using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class Dash : VipModule
{
    private class Cfg
    {
        public int Limit { get; set; } = 3;
        public float Unit { get; set; } = 600f;
    }

    private class State
    {
        public int Used;
        public bool WasOnGround = true;
        public int JumpReleasedTicks = 10;
        public bool Dashed;
    }

    private readonly State?[] _states = new State?[64];

    public override string Name => "Dash";
    public override string DisplayName => Core.Localizer["vip.module.dash"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerSpawn>((ev, _) =>
        {
            int slot = ev.Userid?.Slot ?? -1;
            if (slot >= 0 && slot < 64)
                _states[slot] = new State();
            return HookResult.Continue;
        });
        Core.RegisterListener<OnTick>(OnTick);
    }

    private void OnTick()
    {
        foreach (var player in Core.Players)
        {
            if (player == null || !player.IsValid || player.IsBot || !IsAlive(player) || !Active(player))
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            int slot = player.Slot;
            var state = _states[slot] ??= new State();

            var buttons = player.Buttons;
            bool jumpPressed = (buttons & PlayerButtons.Jump) != 0
                || ((pawn.MovementServices?.QueuedButtonChangeMask ?? 0) & (ulong)PlayerButtons.Jump) != 0;

            bool isOnGround = pawn.GroundEntity?.Value != null;

            if (state.WasOnGround && !isOnGround && jumpPressed)
                state.Dashed = false;
            else if (isOnGround)
                state.Dashed = false;
            else if (!isOnGround && jumpPressed && !state.Dashed)
            {
                var cfg = GroupValue<Cfg>(player) ?? new Cfg();
                if (state.JumpReleasedTicks >= 3 && (cfg.Limit <= 0 || state.Used < cfg.Limit))
                {
                    state.Dashed = true;
                    state.Used++;

                    float moveX = 0f;
                    float moveY = 0f;

                    if ((buttons & PlayerButtons.Forward) != 0) moveY += 1f;
                    if ((buttons & PlayerButtons.Back) != 0) moveY -= 1f;
                    if ((buttons & PlayerButtons.Moveleft) != 0) moveX += 1f;
                    if ((buttons & PlayerButtons.Moveright) != 0) moveX -= 1f;

                    if (moveX == 0f && moveY == 0f)
                        moveY = 1f;

                    float moveAngle = MathF.Atan2(moveX, moveY) * (180f / MathF.PI);
                    var dashAngles = new QAngle(0, pawn.EyeAngles.Y + moveAngle, 0);

                    var forward = new Vector();
                    NativeAPI.AngleVectors(dashAngles.Handle, forward.Handle, 0, 0);

                    pawn.AbsVelocity.X = forward.X * cfg.Unit;
                    pawn.AbsVelocity.Y = forward.Y * cfg.Unit;
                    pawn.AbsVelocity.Z = Math.Max(pawn.AbsVelocity.Z, 150f);
                }
            }

            state.WasOnGround = isOnGround;
            state.JumpReleasedTicks = jumpPressed ? 0 : state.JumpReleasedTicks + 1;
        }
    }
}
