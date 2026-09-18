using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class GrenadeTrail : VipModule
{
    private class Cfg
    {
        public float Width { get; set; } = 1.0f;
        public float Lifetime { get; set; } = 2.0f;
        public List<string> Colors { get; set; } = new();
        public List<ParticleEntry> Particles { get; set; } = new();
    }

    private class Tracked
    {
        public required CBaseCSGrenadeProjectile Projectile;
        public required string ColorValue;
        public required float Width;
        public required float Lifetime;
        public required int OwnerSlot;
        public System.Drawing.Color? Fixed;
        public Vector Last = new(0, 0, 0);
        public CParticleSystem? Particle;
        public float Offset;
    }

    private readonly List<Tracked> _tracked = new();

    public override string Name => "GrenadeTrail";
    public override string DisplayName => Core.Localizer["vip.module.grenadetrail"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        var options = TrailBeam.ParseColorOptions(cfg.Colors);
        ParticleTrail.AddOptions(options, cfg.Particles, player);
        return options;
    }

    public override void OnLoad()
    {
        EffectHide.Ensure(Core);
        Core.HookEntitySpawned(OnEntitySpawned);
        Core.HookTick(OnTick);
        Core.RegisterEventHandler<EventRoundStart>((_, __) => { Clear(); return HookResult.Continue; });
        Core.HookMapStart(_ => _tracked.Clear());
        Core.HookPrecache(manifest =>
        {
            foreach (var cfg in Core.GetAllGroupValues<Cfg>(Name))
                ParticleTrail.Precache(manifest, cfg.Particles);
        });
    }

    public override void OnUnload() => Clear();

    private void Clear()
    {
        foreach (var t in _tracked)
            ParticleTrail.Stop(t.Particle);

        _tracked.Clear();
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

            var entry = ParticleTrail.Find(cfg.Particles, setting, owner);
            var tracked = new Tracked
            {
                Projectile = projectile,
                ColorValue = setting,
                Width = cfg.Width,
                Lifetime = cfg.Lifetime,
                OwnerSlot = owner!.Slot,
                Fixed = entry == null && TrailBeam.IsRandom(setting) ? Core.RoundColor(owner.Slot) : null
            };

            if (entry != null)
            {
                tracked.Particle = ParticleTrail.Carry(projectile, entry, entry.Offset, EffectHide.GrenadeTrail, owner.Slot);
                tracked.Offset = entry.Offset;
                if (tracked.Particle == null)
                    return;
            }

            _tracked.Add(tracked);
        });
    }

    private void OnTick()
    {
        for (int i = _tracked.Count - 1; i >= 0; i--)
        {
            var t = _tracked[i];
            if (t.Projectile == null || !t.Projectile.IsValid)
            {
                ParticleTrail.Stop(t.Particle);
                _tracked.RemoveAt(i);
                continue;
            }

            var origin = t.Projectile.AbsOrigin;
            if (origin == null)
                continue;

            if (t.Particle != null)
            {
                if (!t.Particle.IsValid)
                {
                    _tracked.RemoveAt(i);
                    continue;
                }

                t.Particle.Teleport(new Vector(origin.X, origin.Y, origin.Z + t.Offset), new QAngle(), new Vector());
                continue;
            }

            if (t.Last.LengthSqr() == 0)
            {
                t.Last.X = origin.X; t.Last.Y = origin.Y; t.Last.Z = origin.Z;
                continue;
            }

            float dist = TrailBeam.Distance(t.Last, origin);
            if (dist <= 3)
                continue;

            if (dist < 600)
                TrailBeam.Create(Core, origin, t.Last, t.Fixed ?? TrailBeam.Resolve(t.ColorValue), t.Width, t.Lifetime,
                    EffectHide.GrenadeTrail, t.OwnerSlot);

            t.Last.X = origin.X; t.Last.Y = origin.Y; t.Last.Z = origin.Z;
        }
    }
}
