using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class RadarHack : VipModule
{
    private class Cfg
    {
        public float DurationOn { get; set; } = 1f;
        public float DurationOff { get; set; } = 0f;
    }

    public override string Name => "RadarHack";
    public override string DisplayName => Core.Localizer["vip.module.radarhack"];

    public override void OnLoad() => Core.RegisterListener<OnTick>(OnTick);

    private void OnTick()
    {
        var vips = Utilities.GetPlayers().Where(p => p != null && p.IsValid && IsAlive(p) && Active(p)).ToList();
        if (vips.Count == 0)
            return;

        var bomb = Utilities.FindAllEntitiesByDesignerName<CC4>("weapon_c4").FirstOrDefault();

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

            foreach (var enemy in Utilities.GetPlayers())
            {
                if (enemy == null || !enemy.IsValid || enemy.Team == player.Team || !IsAlive(enemy))
                    continue;

                var enemyPawn = enemy.PlayerPawn.Value;
                if (enemyPawn == null || !enemyPawn.IsValid)
                    continue;

                if (enemyPawn.Render.A < 200)
                    continue;

                enemyPawn.EntitySpottedState.SpottedByMask[0] |= 1u << (slot % 32);
            }

            if (bomb != null && bomb.IsValid)
                bomb.EntitySpottedState.SpottedByMask[0] |= 1u << (slot % 32);
        }
    }
}
