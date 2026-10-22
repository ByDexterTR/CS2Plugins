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

    private readonly bool[] _jammed = new bool[64];
    private readonly bool[] _state = new bool[64];
    private static readonly Cfg DefaultCfg = new();
    private readonly List<(int Slot, byte Team, System.Numerics.Vector3 Pos, Cfg Cfg)> _jammers = new();

    public override string Name => "Jammer";
    public override string DisplayName => Core.Localizer["vip.module.jammer"];

    public override void OnLoad()
    {
        Core.HookTick(OnTick, 16);
        Core.RegisterEventHandler<EventRoundStart>((_, __) => { UnjamAll(); return HookResult.Continue; });
    }

    public override void OnUnload() => UnjamAll();

    private void UnjamAll()
    {
        for (int slot = 0; slot < 64; slot++)
        {
            if (!_jammed[slot])
                continue;

            _jammed[slot] = false;
            var p = Core.PlayerAt(slot);
            if (p != null && p.IsValid && !p.IsBot)
                p.ReplicateConVar("sv_disable_radar", "0");
        }
    }

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

        Array.Clear(_state);

        if (_jammers.Count > 0)
            foreach (ref readonly var target in PlayerCache.AlivePlayers(Core.Players))
                _state[target.Slot] = InJammerRange(target);

        foreach (var target in Core.Humans)
        {
            if (target == null || !target.IsValid || target.Slot >= 64)
                continue;

            int slot = target.Slot;
            bool jam = IsAlive(target) ? _state[slot] : ObservedJammed(target);

            if (jam == _jammed[slot])
                continue;

            _jammed[slot] = jam;
            target.ReplicateConVar("sv_disable_radar", jam ? "1" : "0");
        }
    }

    private bool ObservedJammed(CCSPlayerController target)
    {
        if (_jammers.Count == 0)
            return false;

        var observed = PawnController(target.Pawn.Value?.ObserverServices?.ObserverTarget.Value);
        int slot = observed?.Slot ?? -1;
        return slot >= 0 && slot < 64 && _state[slot];
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
