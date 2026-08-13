using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class WallHack : VipModule
{
    private class Cfg
    {
        public float DurationOn { get; set; } = 1f;
        public float DurationOff { get; set; } = 0f;
        public bool SeeTeammates { get; set; }
        public string Color { get; set; } = GlowPool.DefaultColor;
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "WallHack";
    public override string DisplayName => Core.Localizer["vip.module.wallhack"];

    public override void OnLoad()
    {
        GlowPool.Ensure(Core);
        GlowPool.Acquire();
        Core.RegisterListener<OnTick>(OnTick);
    }

    public override void OnUnload() => GlowPool.Release();

    private void OnTick()
    {
        GlowPool.Build();

        foreach (var player in Core.Players)
        {
            if (player == null || !player.IsValid || player.IsBot || !IsAlive(player) || !Active(player))
                continue;

            var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
            if (cfg.DurationOff > 0f)
            {
                float on = Math.Max(cfg.DurationOn, 0.1f);
                if (Server.CurrentTime % (on + cfg.DurationOff) >= on)
                    continue;
            }

            var color = TrailBeam.Resolve(cfg.Color);

            foreach (var target in Core.Players)
            {
                if (target == null || !target.IsValid || target.Slot == player.Slot || !IsAlive(target))
                    continue;
                if (!cfg.SeeTeammates && target.Team == player.Team)
                    continue;

                GlowPool.Show(player.Slot, target.Slot, color);
            }
        }
    }
}
