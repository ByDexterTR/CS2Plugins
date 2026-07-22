using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class ExtraJump : VipModule
{
    private class Cfg
    {
        public int Count { get; set; } = 1;
        public int Limit { get; set; } = 0;
    }

    private readonly int[] _airJumps = new int[64];
    private readonly int[] _usedThisLife = new int[64];
    private readonly PlayerButtons[] _lastButtons = new PlayerButtons[64];

    public override string Name => "ExtraJump";
    public override string DisplayName => Core.Localizer["vip.module.extrajump"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.RegisterListener<OnTick>(OnTick);
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        int slot = ev.Userid?.Slot ?? -1;
        if (slot >= 0 && slot < 64)
        {
            _airJumps[slot] = 0;
            _usedThisLife[slot] = 0;
        }
        return HookResult.Continue;
    }

    private void OnTick()
    {
        foreach (var player in Core.Players)
        {
            if (player == null || !player.IsValid || player.IsBot || !Active(player))
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            int slot = player.Slot;
            if (!TryGetButtons(player, out var buttons))
                continue;

            var flags = (PlayerFlags)pawn.Flags;

            bool jumpDown = (buttons & PlayerButtons.Jump) != 0;
            bool wasJumpDown = (_lastButtons[slot] & PlayerButtons.Jump) != 0;
            bool jumpPressed = (jumpDown && !wasJumpDown)
                || ((pawn.MovementServices?.QueuedButtonChangeMask ?? 0) & (ulong)PlayerButtons.Jump) != 0;

            if ((flags & PlayerFlags.FL_ONGROUND) != 0)
            {
                _airJumps[slot] = 0;
            }
            else if (jumpPressed)
            {
                var cfg = GroupValue<Cfg>(player) ?? new Cfg();
                int total = cfg.Limit > 0 ? cfg.Count * cfg.Limit : 0;
                bool budgetOk = total <= 0 || _usedThisLife[slot] < total;

                if (_airJumps[slot] < cfg.Count && budgetOk)
                {
                    _airJumps[slot]++;
                    _usedThisLife[slot]++;
                    pawn.AbsVelocity.Z = 300f;
                }
            }

            _lastButtons[slot] = buttons;
        }
    }
}
