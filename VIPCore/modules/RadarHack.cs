using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class RadarHack : VipModule
{
    private class Cfg
    {
        public float DurationOn { get; set; } = 1f;
        public float DurationOff { get; set; } = 0f;
        public bool SeeTeammates { get; set; }
        public int OnlyMode { get; set; } = 0;
    }

    public override string Name => "RadarHack";
    public override string DisplayName => Core.Localizer["vip.module.radarhack"];

    public override void OnLoad()
    {
        ActivityFilter.Ensure(Core);
        Core.HookTick(OnTick);
    }

    private void OnTick()
    {
        var vips = ActivePlayers();
        if (vips.Count == 0)
            return;

        foreach (var player in vips)
        {
            var cfg = GroupValue<Cfg>(player) ?? new Cfg();
            if (cfg.DurationOff > 0)
            {
                float on = Math.Max(cfg.DurationOn, 1f);
                if (Server.CurrentTime % (on + cfg.DurationOff) >= on)
                    continue;
            }

            int slot = player.Slot;

            foreach (var enemy in Core.Players)
            {
                if (enemy == null || !enemy.IsValid || enemy.Slot == slot || !IsAlive(enemy))
                    continue;
                if (!cfg.SeeTeammates && enemy.Team == player.Team)
                    continue;

                var enemyPawn = enemy.PlayerPawn.Value;
                if (enemyPawn == null || !enemyPawn.IsValid)
                    continue;

                if (enemyPawn.Render.A < 200)
                    continue;
                if (!ActivityFilter.Matches(cfg.OnlyMode, enemy, enemyPawn))
                    continue;

                enemyPawn.EntitySpottedState.SpottedByMask[slot / 32] |= 1u << (slot % 32);
            }
        }
    }
}
