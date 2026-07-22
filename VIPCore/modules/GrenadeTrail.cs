using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class GrenadeTrail : VipModule
{
    private class Cfg
    {
        public float Width { get; set; } = 1.0f;
        public float Lifetime { get; set; } = 2.0f;
        public List<string> Colors { get; set; } = new();
    }

    private class Tracked
    {
        public required CBaseCSGrenadeProjectile Projectile;
        public required string ColorValue;
        public required float Width;
        public required float Lifetime;
        public System.Drawing.Color? Fixed;
        public Vector Last = new(0, 0, 0);
    }

    private readonly List<Tracked> _tracked = new();

    public override string Name => "GrenadeTrail";
    public override string DisplayName => Core.Localizer["vip.module.grenadetrail"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player) =>
        TrailBeam.ParseColorOptions(GroupValue<Cfg>(player)?.Colors ?? new());

    public override void OnLoad()
    {
        Core.RegisterListener<OnEntitySpawned>(OnEntitySpawned);
        Core.RegisterListener<OnTick>(OnTick);
        Core.RegisterEventHandler<EventRoundStart>((_, __) => { _tracked.Clear(); return HookResult.Continue; });
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (entity == null || !entity.IsValid || !entity.DesignerName.EndsWith("_projectile"))
            return;

        var projectile = entity.As<CBaseCSGrenadeProjectile>();
        Server.NextFrame(() =>
        {
            if (projectile == null || !projectile.IsValid)
                return;

            var owner = projectile.Thrower.Value?.Controller.Value?.As<CCSPlayerController>();
            if (!Active(owner))
                return;

            var cfg = GroupValue<Cfg>(owner!) ?? new Cfg();
            string setting = Setting(owner!);
            _tracked.Add(new Tracked
            {
                Projectile = projectile,
                ColorValue = setting,
                Width = cfg.Width,
                Lifetime = cfg.Lifetime,
                Fixed = TrailBeam.IsRandom(setting) ? Core.RoundColor(owner!.Slot) : null
            });
        });
    }

    private void OnTick()
    {
        for (int i = _tracked.Count - 1; i >= 0; i--)
        {
            var t = _tracked[i];
            if (t.Projectile == null || !t.Projectile.IsValid)
            {
                _tracked.RemoveAt(i);
                continue;
            }

            var origin = t.Projectile.AbsOrigin;
            if (origin == null)
                continue;

            if (t.Last.LengthSqr() == 0)
            {
                t.Last.X = origin.X; t.Last.Y = origin.Y; t.Last.Z = origin.Z;
                continue;
            }

            float dist = TrailBeam.Distance(t.Last, origin);
            if (dist <= 3)
                continue;

            if (dist < 600)
                TrailBeam.Create(Core, origin, t.Last, t.Fixed ?? TrailBeam.Resolve(t.ColorValue), t.Width, t.Lifetime);

            t.Last.X = origin.X; t.Last.Y = origin.Y; t.Last.Z = origin.Z;
        }
    }
}
