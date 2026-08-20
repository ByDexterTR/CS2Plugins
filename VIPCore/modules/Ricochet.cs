using System.Drawing;
using Microsoft.Extensions.Logging;
using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace VIPCore;

public class Ricochet : VipModule
{
    private class Cfg
    {
        public int Bounces { get; set; } = 3;
        public float SegmentDistance { get; set; } = 1200f;
        public float DamageMultiplier { get; set; } = 0.5f;
        public float FallbackDamage { get; set; } = 25f;
        public bool RespectArmor { get; set; } = true;
        public float DamageFalloff { get; set; } = 0.75f;
        public int MaxImpactsPerTick { get; set; } = 2;
        public bool ShowTracer { get; set; } = true;
        public float TracerWidth { get; set; } = 0.5f;
        public float TracerSpeed { get; set; } = 2600f;
        public float TracerLength { get; set; } = 220f;
        public int MaxActiveTracers { get; set; } = 12;
        public float HitRadius { get; set; } = 20f;
        public bool IgnoreTeammates { get; set; } = true;
        public float SoundVolume { get; set; } = 1f;
        public string Color { get; set; } = "#FFE28C";
        public string OnlyWithWeapon { get; set; } = "";

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWithWeapon);
    }

    private class Flight
    {
        public required int OwnerSlot;
        public required Vector3[] Points;
        public required float[] Cumulative;
        public required float Total;
        public required int[] SoundAt;
        public required int DeadlineTick;
        public required Color Color;
        public required float Width;
        public required float Speed;
        public required float Length;
        public required float Volume;
        public float Travelled;
        public int NextSound;
        public CEnvBeam? Beam;
    }

    private const string BounceSound = "FX_RicochetSound.Ricochet_Legacy";

    private static readonly Cfg DefaultCfg = new();

    private readonly List<Flight> _flights = new();
    private readonly List<CEnvBeam> _pool = new();
    private readonly List<CEnvBeam> _owned = new();
    private bool _warned;
    private readonly int[] _impactTick = new int[64];
    private readonly int[] _impactCount = new int[64];

    public override string Name => "Ricochet";
    public override string DisplayName => Core.Localizer["vip.module.ricochet"];

    public override void OnLoad()
    {
        KillCredit.Ensure(Core);

        Core.RegisterListener<OnServerPrecacheResources>(manifest => manifest.AddResource(TrailBeam.Sprite));
        Core.RegisterEventHandler<EventBulletImpact>(OnBulletImpact);
        Core.RegisterListener<OnTick>(OnTick);
        Core.RegisterEventHandler<EventRoundStart>((_, _) => { Clear(); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundEnd>((_, _) => { Clear(); return HookResult.Continue; });
        Core.RegisterListener<OnMapStart>(_ => Clear());
    }

    public override void OnUnload() => Clear();

    private void Clear()
    {
        foreach (var flight in _flights)
            flight.Beam = null;

        _flights.Clear();
        _pool.Clear();

        foreach (var beam in _owned)
            if (beam != null && beam.IsValid)
                beam.Remove();

        _owned.Clear();
        Array.Clear(_impactTick);
        Array.Clear(_impactCount);
    }

    private HookResult OnBulletImpact(EventBulletImpact ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player) || !IsAlive(player))
            return HookResult.Continue;

        var pawn = player!.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;

        var allow = cfg.Allow;
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(player)))
            return HookResult.Continue;

        if (!ConsumeBudget(player.Slot, Math.Max(cfg.MaxImpactsPerTick, 1)))
            return HookResult.Continue;

        var eye = new Vector3(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + pawn.ViewOffset.Z);
        var impact = new Vector3(ev.X, ev.Y, ev.Z);

        var toImpact = impact - eye;
        if (toImpact.LengthSquared() < 1f)
            return HookResult.Continue;

        var direction = Vector3.Normalize(toImpact);

        var surface = NativeTrace.TraceRay(pawn, eye, impact + direction * 8f, NativeTrace.MaskShotNoPlayers);
        if (surface == null)
        {
            WarnOnce();
            return HookResult.Continue;
        }

        if (!surface.Value.DidHit)
            return HookResult.Continue;

        var normal = surface.Value.Normal;
        if (!IsFinite(normal) || normal.LengthSquared() < 0.0001f)
            return HookResult.Continue;

        normal = Vector3.Normalize(normal);
        if (Vector3.Dot(direction, normal) > -0.05f)
            return HookResult.Continue;

        Bounce(player, pawn, cfg, surface.Value.EndPos, direction, normal);
        return HookResult.Continue;
    }

    private void Bounce(CCSPlayerController player, CCSPlayerPawn pawn, Cfg cfg, Vector3 point, Vector3 direction, Vector3 normal)
    {
        int bounces = Math.Max(cfg.Bounces, 1);
        float segment = Math.Max(cfg.SegmentDistance, 64f);
        float damage = BaseDamage(pawn, cfg);
        string weapon = ActiveWeaponName(player) ?? "weapon_knife";

        var path = new List<Vector3>();
        var soundAt = new List<int>();

        for (int i = 0; i < bounces; i++)
        {
            direction = Vector3.Reflect(direction, normal);

            var start = point + normal * 2f;
            if (path.Count == 0)
                path.Add(start);

            var end = start + direction * segment;

            var hit = NativeTrace.TraceRay(pawn, start, end, NativeTrace.MaskShotNoPlayers);
            var wallEnd = hit is { DidHit: true } ? hit.Value.EndPos : end;

            var victim = FindVictim(player, cfg, start, wallEnd, out var hitPoint);
            if (victim != null)
            {
                path.Add(hitPoint);
                DealDamage(player, victim, damage, cfg, weapon);
                break;
            }

            path.Add(wallEnd);

            if (hit is not { DidHit: true })
                break;

            var next = hit.Value.Normal;
            if (!IsFinite(next) || next.LengthSquared() < 0.0001f)
                break;

            next = Vector3.Normalize(next);
            if (Vector3.Dot(direction, next) > -0.05f)
                break;

            normal = next;
            point = hit.Value.EndPos;
            soundAt.Add(path.Count - 1);
            damage *= cfg.DamageFalloff;
        }

        Launch(player, cfg, path, soundAt);
    }

    private float BaseDamage(CCSPlayerPawn pawn, Cfg cfg)
    {
        try
        {
            var weapon = pawn.WeaponServices?.ActiveWeapon?.Value;
            if (weapon != null && weapon.IsValid)
            {
                float damage = weapon.As<CCSWeaponBase>().GetVData<CCSWeaponBaseVData>()?.Damage ?? 0f;
                if (damage > 0f)
                    return damage * cfg.DamageMultiplier;
            }
        }
        catch { }

        return cfg.FallbackDamage;
    }

    private void DealDamage(CCSPlayerController attacker, CCSPlayerController victim, float damage, Cfg cfg, string weapon)
    {
        var pawn = victim.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return;

        if (cfg.RespectArmor && pawn.ArmorValue > 0)
            damage *= 0.5f;

        int final = (int)MathF.Round(damage);
        if (final <= 0)
            return;

        int health = pawn.Health - final;
        if (health > 0)
        {
            pawn.Health = health;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            return;
        }

        pawn.Health = 0;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        KillCredit.Register(victim.Slot, attacker.Slot, weapon);
        pawn.CommitSuicide(false, true);
    }

    private CCSPlayerController? FindVictim(CCSPlayerController shooter, Cfg cfg, Vector3 start, Vector3 end, out Vector3 impact)
    {
        impact = end;

        if (Vector3.DistanceSquared(start, end) < 1f)
            return null;

        CCSPlayerController? best = null;
        float bestFraction = 1f;
        float radius = Math.Clamp(cfg.HitRadius, 1f, 128f);

        foreach (var target in Core.Players)
        {
            if (target == null || !target.IsValid || target.Slot == shooter.Slot || !IsAlive(target))
                continue;
            if (cfg.IgnoreTeammates && target.Team == shooter.Team)
                continue;

            var pawn = target.PlayerPawn.Value;
            if (pawn?.AbsOrigin == null)
                continue;

            var feet = new Vector3(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + 4f);
            var head = new Vector3(feet.X, feet.Y, pawn.AbsOrigin.Z + pawn.ViewOffset.Z + 8f);

            if (!SegmentHitsCapsule(start, end, feet, head, radius, out float fraction) || fraction >= bestFraction)
                continue;

            bestFraction = fraction;
            best = target;
        }

        if (best != null)
            impact = Vector3.Lerp(start, end, bestFraction);

        return best;
    }

    private void Launch(CCSPlayerController player, Cfg cfg, List<Vector3> path, List<int> soundAt)
    {
        if (!cfg.ShowTracer || path.Count < 2)
            return;

        int maxActive = Math.Max(cfg.MaxActiveTracers, 1);
        if (_flights.Count >= maxActive)
            return;

        var points = path.ToArray();
        var cumulative = new float[points.Length];

        for (int i = 1; i < points.Length; i++)
            cumulative[i] = cumulative[i - 1] + Vector3.Distance(points[i - 1], points[i]);

        float total = cumulative[^1];
        if (total < 1f)
            return;

        float speed = Math.Max(cfg.TracerSpeed, 200f);

        _flights.Add(new Flight
        {
            OwnerSlot = player.Slot,
            Points = points,
            Cumulative = cumulative,
            Total = total,
            SoundAt = soundAt.Where(index => index < points.Length).ToArray(),
            DeadlineTick = Server.TickCount + (int)(total / speed * 64f) + 128,
            Color = TrailBeam.Resolve(cfg.Color),
            Width = Math.Max(cfg.TracerWidth, 0.1f),
            Speed = speed,
            Length = Math.Max(cfg.TracerLength, 16f),
            Volume = cfg.SoundVolume
        });
    }

    private void OnTick()
    {
        if (_flights.Count == 0)
            return;

        int tick = Server.TickCount;

        for (int i = _flights.Count - 1; i >= 0; i--)
        {
            var flight = _flights[i];
            flight.Travelled += flight.Speed / 64f;

            if (flight.Travelled - flight.Length >= flight.Total || tick >= flight.DeadlineTick)
            {
                Release(flight);
                _flights.RemoveAt(i);
                continue;
            }

            float head = MathF.Min(flight.Travelled, flight.Total);
            float tail = MathF.Max(head - flight.Length, flight.Cumulative[SegmentAt(flight, head)]);

            Draw(flight, PointAt(flight, tail), PointAt(flight, head));
            EmitBounce(flight, head);
        }
    }

    private void EmitBounce(Flight flight, float head)
    {
        if (flight.NextSound >= flight.SoundAt.Length || flight.Volume <= 0f)
            return;

        bool play = false;
        while (flight.NextSound < flight.SoundAt.Length && head >= flight.Cumulative[flight.SoundAt[flight.NextSound]])
        {
            flight.NextSound++;
            play = true;
        }

        if (play && flight.Beam != null && flight.Beam.IsValid)
            flight.Beam.EmitSound(BounceSound, volume: flight.Volume);
    }

    private void Draw(Flight flight, Vector3 tail, Vector3 head)
    {
        var beam = flight.Beam;
        if (beam != null && !beam.IsValid)
        {
            beam = null;
            flight.Beam = null;
        }

        beam ??= Acquire(flight);
        if (beam == null)
            return;

        beam.Teleport(new Vector(tail.X, tail.Y, tail.Z), new CounterStrikeSharp.API.Modules.Utils.QAngle(), new Vector());

        beam.EndPos.X = head.X;
        beam.EndPos.Y = head.Y;
        beam.EndPos.Z = head.Z;
        Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");
    }

    private CEnvBeam? Acquire(Flight flight)
    {
        while (_pool.Count > 0)
        {
            var pooled = _pool[^1];
            _pool.RemoveAt(_pool.Count - 1);

            if (pooled == null || !pooled.IsValid)
                continue;

            Style(pooled, flight);
            flight.Beam = pooled;
            return pooled;
        }

        if (_owned.Count >= Math.Max(flight.Points.Length, 1) * 4)
            _owned.RemoveAll(entity => entity == null || !entity.IsValid);

        var beam = Utilities.CreateEntityByName<CEnvBeam>("env_beam");
        if (beam == null || !beam.IsValid)
            return null;

        beam.DispatchSpawn();
        beam.AcceptInput("TurnOn");
        beam.SetModel(TrailBeam.Sprite);
        Style(beam, flight);

        _owned.Add(beam);
        flight.Beam = beam;
        return beam;
    }

    private static void Style(CEnvBeam beam, Flight flight)
    {
        beam.Width = flight.Width;
        beam.EndWidth = flight.Width;
        Utilities.SetStateChanged(beam, "CBeam", "m_fWidth");
        Utilities.SetStateChanged(beam, "CBeam", "m_fEndWidth");

        beam.Render = flight.Color;
        Utilities.SetStateChanged(beam, "CBaseModelEntity", "m_clrRender");
    }

    private void Release(Flight flight)
    {
        var beam = flight.Beam;
        flight.Beam = null;

        if (beam == null || !beam.IsValid)
            return;

        Hide(beam);
        _pool.Add(beam);
    }

    private static void Hide(CEnvBeam beam)
    {
        beam.Width = 0f;
        beam.EndWidth = 0f;
        Utilities.SetStateChanged(beam, "CBeam", "m_fWidth");
        Utilities.SetStateChanged(beam, "CBeam", "m_fEndWidth");

        beam.Render = Color.FromArgb(0, 0, 0, 0);
        Utilities.SetStateChanged(beam, "CBaseModelEntity", "m_clrRender");

        if (beam.AbsOrigin == null)
            return;

        beam.EndPos.X = beam.AbsOrigin.X;
        beam.EndPos.Y = beam.AbsOrigin.Y;
        beam.EndPos.Z = beam.AbsOrigin.Z;
        Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");
    }

    private void WarnOnce()
    {
        if (_warned)
            return;

        _warned = true;
        Core.Logger.LogWarning("VIPCore: Ricochet ray-trace kullanilamiyor: {0}", NativeTrace.LastError ?? "bilinmeyen hata");
    }

    private bool ConsumeBudget(int slot, int max)
    {
        if (slot < 0 || slot >= 64)
            return false;

        int tick = Server.TickCount;
        if (_impactTick[slot] != tick)
        {
            _impactTick[slot] = tick;
            _impactCount[slot] = 0;
        }

        if (_impactCount[slot] >= max)
            return false;

        _impactCount[slot]++;
        return true;
    }

    private static int SegmentAt(Flight flight, float distance)
    {
        for (int i = flight.Points.Length - 2; i >= 0; i--)
            if (distance >= flight.Cumulative[i])
                return i;

        return 0;
    }

    private static Vector3 PointAt(Flight flight, float distance)
    {
        int i = SegmentAt(flight, distance);

        float start = flight.Cumulative[i];
        float length = flight.Cumulative[i + 1] - start;
        if (length <= 0.0001f)
            return flight.Points[i];

        return Vector3.Lerp(flight.Points[i], flight.Points[i + 1], Math.Clamp((distance - start) / length, 0f, 1f));
    }

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    private static bool SegmentHitsCapsule(Vector3 rayStart, Vector3 rayEnd, Vector3 axisStart, Vector3 axisEnd, float radius, out float fraction)
    {
        const float epsilon = 0.00001f;

        var ray = rayEnd - rayStart;
        var axis = axisEnd - axisStart;
        var offset = rayStart - axisStart;

        float rayLength = Vector3.Dot(ray, ray);
        float axisLength = Vector3.Dot(axis, axis);
        float axisOffset = Vector3.Dot(axis, offset);
        float rayOffset = Vector3.Dot(ray, offset);

        fraction = 0f;
        float onAxis;

        if (rayLength <= epsilon)
            return false;

        if (axisLength <= epsilon)
        {
            onAxis = 0f;
            fraction = Math.Clamp(-rayOffset / rayLength, 0f, 1f);
        }
        else
        {
            float mixed = Vector3.Dot(ray, axis);
            float denominator = rayLength * axisLength - mixed * mixed;

            fraction = denominator > epsilon
                ? Math.Clamp((mixed * axisOffset - rayOffset * axisLength) / denominator, 0f, 1f)
                : 0f;

            onAxis = (mixed * fraction + axisOffset) / axisLength;

            if (onAxis < 0f)
            {
                onAxis = 0f;
                fraction = Math.Clamp(-rayOffset / rayLength, 0f, 1f);
            }
            else if (onAxis > 1f)
            {
                onAxis = 1f;
                fraction = Math.Clamp((mixed - rayOffset) / rayLength, 0f, 1f);
            }
        }

        var closestOnRay = rayStart + ray * fraction;
        var closestOnAxis = axisStart + axis * onAxis;

        return Vector3.DistanceSquared(closestOnRay, closestOnAxis) <= radius * radius;
    }
}
