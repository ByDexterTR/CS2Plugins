using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class Dash : VipModule
{
    private class Cfg
    {
        public int Limit { get; set; } = 3;
        public float Unit { get; set; } = 600f;
        public float SoundVolume { get; set; } = 1f;
    }

    private class State
    {
        public int Used;
        public bool WasOnGround = true;
        public int JumpReleasedTicks = 10;
        public bool Dashed;
    }

    private const string JumpSound = "Default.WalkJump";

    private static readonly Cfg DefaultCfg = new();
    private readonly State?[] _states = new State?[64];
    private static readonly int[] _dashTick = CreateTickBuffer();

    private static int[] CreateTickBuffer()
    {
        var buffer = new int[64];
        Array.Fill(buffer, int.MinValue);
        return buffer;
    }

    public static bool DashedThisTick(int slot) =>
        slot >= 0 && slot < 64 && _dashTick[slot] == Server.TickCount;

    public override string Name => "Dash";
    public override string DisplayName => Core.Localizer["vip.module.dash"];
    public override int Priority => 100;

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerSpawn>((ev, _) =>
        {
            int slot = ev.Userid?.Slot ?? -1;
            if (slot >= 0 && slot < 64)
                _states[slot] = new State();
            return HookResult.Continue;
        });
        Core.HookTick(OnTick);
    }

    private void OnTick()
    {
        int tick = Server.TickCount;
        foreach (var player in ActivePlayers())
        {
            nint pawn = PlayerCache.Pointer(player);
            if (pawn == 0)
                continue;

            int slot = player.Slot;
            var state = _states[slot] ??= new State();

            if (!PlayerCache.Buttons(pawn, out ulong raw, out ulong queued))
                continue;

            var buttons = (PlayerButtons)raw;
            bool jumpPressed = (buttons & PlayerButtons.Jump) != 0 || (queued & (ulong)PlayerButtons.Jump) != 0;
            bool isOnGround = PlayerCache.OnGroundEntity(pawn);

            if (state.WasOnGround && !isOnGround && jumpPressed)
                state.Dashed = false;
            else if (isOnGround)
                state.Dashed = false;
            else if (!isOnGround && jumpPressed && !state.Dashed)
            {
                var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
                if (state.JumpReleasedTicks >= 3 && (cfg.Limit <= 0 || state.Used < cfg.Limit))
                {
                    state.Dashed = true;
                    state.Used++;
                    _dashTick[slot] = tick;

                    float moveX = 0f;
                    float moveY = 0f;

                    if ((buttons & PlayerButtons.Forward) != 0) moveY += 1f;
                    if ((buttons & PlayerButtons.Back) != 0) moveY -= 1f;
                    if ((buttons & PlayerButtons.Moveleft) != 0) moveX += 1f;
                    if ((buttons & PlayerButtons.Moveright) != 0) moveX -= 1f;

                    if (moveX == 0f && moveY == 0f)
                        moveY = 1f;

                    float moveAngle = MathF.Atan2(moveX, moveY) * (180f / MathF.PI);
                    float yaw = (PlayerCache.EyeAngles(pawn).Y + moveAngle) * (MathF.PI / 180f);

                    ref var velocity = ref PlayerCache.Velocity(pawn);
                    velocity.X = MathF.Cos(yaw) * cfg.Unit;
                    velocity.Y = MathF.Sin(yaw) * cfg.Unit;
                    velocity.Z = Math.Max(velocity.Z, 150f);

                    if (cfg.SoundVolume > 0f)
                        PlayerCache.Pawn(player)?.EmitSound(JumpSound, volume: cfg.SoundVolume);
                }
            }

            state.WasOnGround = isOnGround;
            state.JumpReleasedTicks = jumpPressed ? 0 : state.JumpReleasedTicks + 1;
        }
    }
}
