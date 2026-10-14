using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class WallHack : VipModule
{
    private class Cfg
    {
        public float DurationOn { get; set; } = 1f;
        public float DurationOff { get; set; } = 0f;
        public bool SeeTeammates { get; set; }
        public int OnlyMode { get; set; } = 0;
        public string Color { get; set; } = GlowPool.DefaultColor;
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "WallHack";
    public override string DisplayName => Core.Localizer["vip.module.wallhack"];

    public override void OnLoad()
    {
        GlowPool.Ensure(Core);
        GlowPool.Acquire();
        ActivityFilter.Ensure(Core);
        Core.HookTick(OnTick);
    }

    public override void OnUnload() => GlowPool.Release();

    private void OnTick()
    {
        var users = ActivePlayers();
        if (users.Count == 0)
            return;

        GlowPool.Build();
        float now = Server.CurrentTime;
        var living = PlayerCache.AlivePlayers(Core.Players);

        foreach (var player in users)
        {
            nint pawn = PlayerCache.Pointer(player);
            if (pawn == 0)
                continue;

            var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
            if (cfg.DurationOff > 0f)
            {
                float on = Math.Max(cfg.DurationOn, 0.1f);
                if (now % (on + cfg.DurationOff) >= on)
                    continue;
            }

            var color = TrailBeam.Resolve(cfg.Color);
            int slot = player.Slot;
            byte team = PlayerCache.Team(pawn);

            foreach (ref readonly var target in living)
            {
                if (target.Slot == slot || (!cfg.SeeTeammates && target.Team == team))
                    continue;
                if (!ActivityFilter.Matches(cfg.OnlyMode, target.Slot, target.Pawn))
                    continue;

                GlowPool.Show(slot, target.Slot, color);
            }
        }
    }
}
