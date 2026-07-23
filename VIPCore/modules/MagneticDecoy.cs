using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class MagneticDecoy : VipModule
{
    private class Cfg
    {
        public float Radius { get; set; } = 180f;
        public float Strength { get; set; } = 30f;
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreEnemy { get; set; } = false;
        public bool IgnoreSelf { get; set; } = true;
        public int Limit { get; set; } = 0;
    }

    private class ActiveDecoy
    {
        public required Vector Pos;
        public int OwnerSlot;
    }

    private readonly Dictionary<int, ActiveDecoy> _decoys = new();

    public override string Name => "MagneticDecoy";
    public override string DisplayName => Core.Localizer["vip.module.magneticdecoy"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventDecoyStarted>(OnStarted);
        Core.RegisterEventHandler<EventDecoyDetonate>((ev, _) => { _decoys.Remove(ev.Entityid); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundStart>((_, __) => { _decoys.Clear(); return HookResult.Continue; });
        Core.RegisterListener<OnTick>(OnTick);
    }

    private HookResult OnStarted(EventDecoyStarted ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(player!) ?? new Cfg();
        if (LimitReached(player!.Slot, cfg.Limit))
            return HookResult.Continue;

        LimitUse(player.Slot);
        _decoys[ev.Entityid] = new ActiveDecoy
        {
            Pos = new Vector(ev.X, ev.Y, ev.Z),
            OwnerSlot = player.Slot
        };

        return HookResult.Continue;
    }

    private void OnTick()
    {
        if (_decoys.Count == 0)
            return;

        foreach (var decoy in _decoys.Values)
        {
            var owner = Utilities.GetPlayerFromSlot(decoy.OwnerSlot);
            if (!Active(owner))
                continue;

            var cfg = GroupValue<Cfg>(owner!) ?? new Cfg();
            if (cfg.Radius <= 0 || cfg.Strength <= 0)
                continue;

            foreach (var target in Core.Players)
            {
                if (target == null || !target.IsValid || !IsAlive(target))
                    continue;

                bool isSelf = target.Slot == decoy.OwnerSlot;
                bool isTeammate = !isSelf && target.Team == owner!.Team;
                bool isEnemy = !isSelf && !isTeammate;

                if (isSelf && cfg.IgnoreSelf)
                    continue;
                if (isTeammate && cfg.IgnoreTeammates)
                    continue;
                if (isEnemy && cfg.IgnoreEnemy)
                    continue;

                var pawn = target.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
                    continue;

                float distance = TrailBeam.Distance(decoy.Pos, pawn.AbsOrigin);
                if (distance > cfg.Radius || distance <= 10f)
                    continue;

                float dx = decoy.Pos.X - pawn.AbsOrigin.X;
                float dy = decoy.Pos.Y - pawn.AbsOrigin.Y;
                float length = MathF.Sqrt(dx * dx + dy * dy);
                if (length < 1f)
                    continue;

                float pull = cfg.Strength * (1f - distance / cfg.Radius);
                var vel = pawn.AbsVelocity;
                pawn.Teleport(null, null, new Vector(vel.X + dx / length * pull, vel.Y + dy / length * pull, vel.Z));
            }
        }
    }
}
