using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class ParticleEntry
{
    public string Name { get; set; } = "";
    public string File { get; set; } = "";
    public string Tint { get; set; } = "";
    public float Radius { get; set; } = 0f;
    public float Lifetime { get; set; } = 0f;
    public float Offset { get; set; } = 0f;
    public bool Follow { get; set; } = true;
}

public static class ParticleTrail
{
    public const char Marker = '@';

    public static bool IsParticle(string value) =>
        value.Length > 1 && value[0] == Marker;

    public static string Key(string name) => Marker + name;

    public static ParticleEntry? Find(List<ParticleEntry>? list, string value)
    {
        if (list == null || list.Count == 0 || !IsParticle(value))
            return null;

        string name = value[1..];
        foreach (var entry in list)
            if (entry.File.Length > 0 && entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return entry;

        return null;
    }

    public static void AddOptions(List<VipFeatureOption> options, List<ParticleEntry>? list)
    {
        if (list == null)
            return;

        foreach (var entry in list)
            if (entry.Name.Length > 0 && entry.File.Length > 0)
                options.Add(new VipFeatureOption(entry.Name, Key(entry.Name)));
    }

    public static void Precache(ResourceManifest manifest, List<ParticleEntry>? list)
    {
        if (list == null)
            return;

        foreach (var entry in list)
            if (entry.File.Length > 0)
                try { manifest.AddResource(entry.File); }
                catch { }
    }

    public static CParticleSystem? Tracer(BasePlugin plugin, Vector start, Vector end, ParticleEntry entry,
        float lifetime, int hideModule = -1, int ownerSlot = -1)
    {
        var particle = Create(entry, start, Aim(start, end), hideModule, ownerSlot);
        if (particle == null)
            return null;

        particle.DispatchSpawn();

        if (entry.Tint.Length == 0)
            SetPoint(particle, 1, end.X, end.Y, end.Z);

        SetPoint(particle, 5, start.X, start.Y, start.Z);
        SetPoint(particle, 6, end.X, end.Y, end.Z);
        if (entry.Radius > 0)
            SetPoint(particle, 4, 0, entry.Radius, 0);

        particle.AcceptInput("Start");

        float life = entry.Lifetime > 0 ? entry.Lifetime : lifetime;
        plugin.AddTimer(life, () =>
        {
            if (particle.IsValid)
                particle.Remove();
        }, TimerFlags.STOP_ON_MAPCHANGE);

        return particle;
    }

    public static CParticleSystem? Carry(CBaseEntity owner, ParticleEntry entry, float offsetZ = 0f,
        int hideModule = -1, int ownerSlot = -1)
    {
        if (owner == null || !owner.IsValid)
            return null;

        var origin = owner.AbsOrigin;
        if (origin == null)
            return null;

        var start = new Vector(origin.X, origin.Y, origin.Z + offsetZ);
        var particle = Create(entry, start, new QAngle(), hideModule, ownerSlot);
        if (particle == null)
            return null;

        particle.DispatchSpawn();
        particle.AcceptInput("Start");
        return particle;
    }

    public static CParticleSystem? Follow(CBaseEntity parent, ParticleEntry entry, float offsetZ = 0f,
        int hideModule = -1, int ownerSlot = -1)
    {
        if (parent == null || !parent.IsValid)
            return null;

        var origin = parent.AbsOrigin;
        if (origin == null)
            return null;

        var start = new Vector(origin.X, origin.Y, origin.Z + offsetZ);
        var particle = Create(entry, start, new QAngle(), hideModule, ownerSlot);
        if (particle == null)
            return null;

        particle.DispatchSpawn();
        particle.AcceptInput("SetParent", parent, particle, "!activator");
        particle.AcceptInput("Start");
        return particle;
    }

    public static void Stop(CParticleSystem? particle)
    {
        if (particle == null || !particle.IsValid || particle.DesignerName != "info_particle_system")
            return;

        particle.AcceptInput("Stop");
        particle.Remove();
    }

    private static QAngle Aim(Vector from, Vector to)
    {
        float dx = to.X - from.X, dy = to.Y - from.Y, dz = to.Z - from.Z;
        float flat = MathF.Sqrt(dx * dx + dy * dy);

        float yaw = MathF.Atan2(dy, dx) * (180f / MathF.PI);
        float pitch = -MathF.Atan2(dz, flat) * (180f / MathF.PI);

        return new QAngle(pitch, yaw, 0);
    }

    private static CParticleSystem? Create(ParticleEntry entry, Vector start, QAngle angles, int hideModule, int ownerSlot)
    {
        if (entry.File.Length == 0)
            return null;

        if (hideModule >= 0 && !EffectHide.AnyViewer(hideModule, ownerSlot))
            return null;

        var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
        if (particle == null || !particle.IsValid)
            return null;

        if (hideModule >= 0)
            EffectHide.Track(hideModule, particle.Index, ownerSlot);

        particle.EffectName = entry.File;
        particle.StartActive = true;
        particle.Teleport(start, angles, new Vector());

        if (entry.Tint.Length > 0)
        {
            particle.TintCP = 1;
            particle.Tint = TrailBeam.Resolve(entry.Tint);
        }

        return particle;
    }

    private static void SetPoint(CParticleSystem particle, int index, float x, float y, float z) =>
        particle.AcceptInput("SetControlPoint", value: string.Create(CultureInfo.InvariantCulture,
            $"{index}: {x} {y} {z}"));
}
