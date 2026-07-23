using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class Respawn : VipModule
{
    private class Cfg
    {
        public int Limit { get; set; } = 1;
        public float Time { get; set; } = 3f;
    }

    private readonly int[] _used = new int[64];
    private int _round;

    public override string Name => "Respawn";
    public override string DisplayName => Core.Localizer["vip.module.respawn"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerDeath>(OnDeath);
        Core.RegisterEventHandler<EventRoundStart>((_, __) =>
        {
            _round++;
            Array.Clear(_used);
            return HookResult.Continue;
        });
    }

    private HookResult OnDeath(EventPlayerDeath ev, GameEventInfo info)
    {
        var player = ev.Userid;
        int slot = player?.Slot ?? -1;
        if (slot < 0 || slot >= 64 || !Active(player))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(player!) ?? new Cfg();
        if (cfg.Limit > 0 && _used[slot] >= cfg.Limit)
            return HookResult.Continue;

        _used[slot]++;
        int round = _round;
        int userId = player!.UserId ?? -1;

        Core.AddTimer(Math.Max(cfg.Time, 0.1f), () =>
        {
            if (round != _round || userId < 0)
                return;

            var p = Utilities.GetPlayerFromUserid(userId);
            if (p == null || !p.IsValid || IsAlive(p) || !Active(p))
                return;

            if (p.Team != CsTeam.Terrorist && p.Team != CsTeam.CounterTerrorist)
                return;

            p.Respawn();

            string left = cfg.Limit > 0 ? (cfg.Limit - _used[p.Slot]).ToString() : "∞";
            p.PrintToChat($" {CC.Orchid}{Core.ChatPrefix}{CC.Default} {Core.Localizer["vip.respawn.left", left]}");
        }, TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }
}
