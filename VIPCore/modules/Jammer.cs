using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class Jammer : VipModule
{
    private class Cfg
    {
        public float Radius { get; set; } = 500f;
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreEnemy { get; set; } = false;
    }

    private const int Interval = 16;
    private static readonly Cfg DefaultCfg = new();
    private readonly List<(int Slot, byte Team, System.Numerics.Vector3 Pos, Cfg Cfg)> _jammers = new();

    public override string Name => "Jammer";
    public override string DisplayName => Core.Localizer["vip.module.jammer"];

    public override void OnLoad()
    {
        RadarPool.Ensure(Core);
        Core.HookTick(OnTick, Interval);
    }

    public override void OnUnload() => RadarPool.Restore();

    private void OnTick()
    {
        _jammers.Clear();
        foreach (var player in ActivePlayers())
        {
            nint pawn = PlayerCache.Pointer(player);
            if (pawn == 0 || !PlayerCache.Origin(pawn, out var origin))
                continue;

            var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
            if (cfg.Radius <= 0)
                continue;

            _jammers.Add((player.Slot, PlayerCache.Team(pawn), origin, cfg));
        }

        if (_jammers.Count == 0)
            return;

        int ticks = Interval + 4;
        foreach (ref readonly var target in PlayerCache.AlivePlayers(Core.Players))
            if (InJammerRange(target))
                RadarPool.Jam(target.Slot, ticks);
    }

    private bool InJammerRange(in PlayerCache.Living target)
    {
        if (!PlayerCache.Origin(target.Pawn, out var origin))
            return false;

        foreach (var (slot, team, pos, cfg) in _jammers)
        {
            if (slot == target.Slot)
                continue;

            bool isTeammate = team == target.Team;
            if (isTeammate && cfg.IgnoreTeammates)
                continue;
            if (!isTeammate && cfg.IgnoreEnemy)
                continue;

            if (System.Numerics.Vector3.DistanceSquared(pos, origin) <= cfg.Radius * cfg.Radius)
                return true;
        }

        return false;
    }
}
