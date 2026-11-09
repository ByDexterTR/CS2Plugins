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
        public float Delay { get; set; } = 0f;
    }

    private const string AutoBhop = "sv_autobunnyhopping";

    private static readonly Cfg DefaultCfg = new();
    private readonly bool[] _replicated = new bool[64];
    private readonly bool[] _announced = new bool[64];
    private readonly bool[] _wasGround = new bool[64];
    private float _freezeEnd;

    public override string Name => "Bhop";
    public override string DisplayName => Core.Localizer["vip.module.bhop"];

    public override void OnLoad()
    {
        Core.HookTick(OnTick);
        Core.HookMapStart(_ =>
        {
            _freezeEnd = 0f;
            Array.Clear(_replicated);
            Array.Clear(_announced);
        });
        Core.RegisterEventHandler<EventRoundStart>((_, _) =>
        {
            _freezeEnd = float.MaxValue;
            Array.Clear(_announced);
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventRoundFreezeEnd>((_, _) =>
        {
            _freezeEnd = Server.CurrentTime;
            foreach (var player in ActivePlayers())
            {
                float delay = (GroupValue<Cfg>(player) ?? DefaultCfg).Delay;
                if (delay > 0f && !player.IsBot)
                    player.PrintToChat($" {CC.Orchid}{Core.ChatPrefix}{CC.Default} {Core.Localizer["vip.bhop.wait", delay]}");
            }
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventPlayerConnectFull>((ev, _) =>
        {
            Reset(ev.Userid?.Slot ?? -1);
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) =>
        {
            Reset(ev.Userid?.Slot ?? -1);
            return HookResult.Continue;
        });
    }

    public override void OnUnload()
    {
        for (int slot = 0; slot < 64; slot++)
            if (_replicated[slot])
                Replicate(Core.PlayerAt(slot), slot, false);
    }

    private void Reset(int slot)
    {
        if ((uint)slot >= 64)
            return;

        _replicated[slot] = false;
        _announced[slot] = false;
        _wasGround[slot] = false;
    }

    private void Replicate(CCSPlayerController? player, int slot, bool on)
    {
        _replicated[slot] = on;
        if (player == null || !player.IsValid || player.IsBot)
            return;

        player.ReplicateConVar(AutoBhop, on ? "1" : ServerConVar.Value(AutoBhop));
    }

    private void OnTick()
    {
        float now = Server.CurrentTime;
        bool freeze = Core.IsFreezeTime();
        bool serverAuto = ServerConVar.Enabled(AutoBhop);
        Span<bool> keep = stackalloc bool[64];

        foreach (var player in ActivePlayers())
        {
            int slot = player.Slot;
            if ((uint)slot >= 64)
                continue;

            var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
            if (freeze || now < _freezeEnd + cfg.Delay)
                continue;

            keep[slot] = true;
            if (!_replicated[slot] && !serverAuto)
                Replicate(player, slot, true);

            if (cfg.Delay > 0f && !_announced[slot] && !player.IsBot)
            {
                _announced[slot] = true;
                player.PrintToChat($" {CC.Orchid}{Core.ChatPrefix}{CC.Default} {Core.Localizer["vip.bhop.ready"]}");
            }

            nint pointer = PlayerCache.Pointer(player);
            if (pointer == 0)
                continue;

            bool ground = (PlayerCache.Flags(pointer) & (uint)PlayerFlags.FL_ONGROUND) != 0;
            bool landed = ground && !_wasGround[slot];
            _wasGround[slot] = ground;

            if (!PlayerCache.Buttons(pointer, out ulong buttons, out _) || (buttons & (ulong)PlayerButtons.Jump) == 0)
                continue;

            var moveType = (MoveType_t)PlayerCache.MoveType(pointer);
            if (moveType.HasFlag(MoveType_t.MOVETYPE_LADDER))
                continue;

            if (!ground && !serverAuto)
                PlayerCache.ReleaseButton(pointer, (ulong)PlayerButtons.Jump);

            if (!landed || !cfg.Autostrafe)
                continue;

            ref var velocity = ref PlayerCache.Velocity(pointer);
            float vX = velocity.X;
            float vY = velocity.Y;
            double speed = Math.Sqrt(vX * vX + vY * vY);
            if (speed <= 0)
                continue;

            double target = speed < cfg.MaxSpeed ? Math.Min(speed * cfg.JumpBoost, cfg.MaxSpeed) : cfg.MaxSpeed;
            double scale = target / speed;
            velocity.X = (float)(vX * scale);
            velocity.Y = (float)(vY * scale);
        }

        for (int slot = 0; slot < 64; slot++)
            if (_replicated[slot] && (!keep[slot] || serverAuto))
                Replicate(Core.PlayerAt(slot), slot, false);
    }
}
