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

        foreach (var player in users)
        {
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
                if (!ActivityFilter.Matches(cfg.OnlyMode, target, target.PlayerPawn.Value))
                    continue;

                GlowPool.Show(player.Slot, target.Slot, color);
            }
        }
    }
}
