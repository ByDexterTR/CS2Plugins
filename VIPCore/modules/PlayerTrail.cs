using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class PlayerTrail : VipModule
{
    private class Cfg
    {
        public float Width { get; set; } = 1.0f;
        public float Lifetime { get; set; } = 2.0f;
        public List<string> Colors { get; set; } = new();
    }

    private readonly Vector[] _last = new Vector[64];
    private int _tick;

    public override string Name => "PlayerTrail";
    public override string DisplayName => Core.Localizer["vip.module.playertrail"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player) =>
        TrailBeam.ParseColorOptions(GroupValue<Cfg>(player)?.Colors ?? new());

    public override void OnLoad()
    {
        for (int i = 0; i < 64; i++)
            _last[i] = new Vector(0, 0, 0);

        EffectHide.Ensure(Core);
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.RegisterListener<OnTick>(OnTick);
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        int slot = ev.Userid?.Slot ?? -1;
        if (slot >= 0 && slot < 64)
            _last[slot].X = _last[slot].Y = _last[slot].Z = 0;
        return HookResult.Continue;
    }

    private void OnTick()
    {
        if (++_tick < 2)
            return;
        _tick = 0;

        foreach (var player in Core.Players)
        {
            if (player == null || !player.IsValid || player.IsBot || !IsAlive(player) || !Active(player))
                continue;

            var origin = player.PlayerPawn.Value?.AbsOrigin;
            if (origin == null)
                continue;

            int slot = player.Slot;
            var last = _last[slot];
            if (last.LengthSqr() == 0)
            {
                last.X = origin.X; last.Y = origin.Y; last.Z = origin.Z;
                continue;
            }

            float dist = TrailBeam.Distance(last, origin);
            if (dist <= 5)
                continue;

            if (dist < 250)
            {
                var cfg = GroupValue<Cfg>(player) ?? new Cfg();
                string setting = Setting(player);
                var color = TrailBeam.IsRandom(setting) ? Core.RoundColor(slot) : TrailBeam.Resolve(setting);
                TrailBeam.Create(Core, origin, last, color, cfg.Width, cfg.Lifetime, EffectHide.PlayerTrail, slot);
            }

            last.X = origin.X; last.Y = origin.Y; last.Z = origin.Z;
        }
    }
}
