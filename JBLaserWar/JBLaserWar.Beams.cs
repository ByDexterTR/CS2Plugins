using System.Drawing;
using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using ByDexter.Shared;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace JBLaserWar;

public partial class JBLaserWar
{
  private const float MuzzleForward = 26f;
  private const float MuzzleRight = 6f;
  private const float MuzzleUp = -6f;

  private class Flight
  {
    public required int OwnerId;
    public required Vector3[] Points;
    public required float[] Cumulative;
    public required float Total;
    public required int[] SoundAt;
    public required int DeadlineTick;
    public required Color Color;
    public float Travelled;
    public int NextSound;
    public int VictimId = -1;
    public float HitDistance;
    public bool FirePending = true;
    public CEnvBeam? Beam;
  }

  private readonly List<Flight> _flights = new();
  private readonly List<CEnvBeam> _pool = new();
  private readonly List<CEnvBeam> _owned = new();
  private readonly Dictionary<int, int> _lastShotTick = new();

  private void Shoot(CCSPlayerController player)
  {
    int id = Util.UserId(player);
    if (id < 0 || !Util.IsAlive(player))
      return;

    int tick = Server.TickCount;
    if (_lastShotTick.TryGetValue(id, out int last) && last == tick)
      return;
    _lastShotTick[id] = tick;

    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
      return;

    var eye = new Vector3(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + pawn.ViewOffset.Z);
    AngleVectors(pawn.EyeAngles, out var forward, out var right, out var up);
    var muzzle = eye + forward * MuzzleForward + right * MuzzleRight + up * MuzzleUp;

    Fire(player, pawn, muzzle, eye, forward);
  }

  private void Fire(CCSPlayerController player, CCSPlayerPawn pawn, Vector3 muzzle, Vector3 eye, Vector3 direction)
  {
    int segments = _bounces + 1;

    var points = new List<Vector3>(segments + 1) { muzzle };
    var soundAt = new List<int>(segments);

    Vector3 origin = eye;
    CCSPlayerController? victim = null;

    for (int segment = 0; segment < segments; segment++)
    {
      var target = origin + direction * Config.MaxDistance;
      var trace = NativeTrace.TraceRay(pawn, origin, target, NativeTrace.MaskShotNoPlayers);
      bool wall = trace is { DidHit: true };
      var end = wall ? trace!.Value.EndPos : target;

      victim = FindVictim(player, origin, end, out var impact);
      if (victim != null)
      {
        points.Add(impact);
        break;
      }

      points.Add(end);

      if (!wall || segment == segments - 1)
        break;

      var normal = trace!.Value.Normal;
      if (!IsFinite(normal) || normal.LengthSquared() < .0001f)
        break;

      normal = Vector3.Normalize(normal);
      if (Vector3.Dot(direction, normal) > -.05f)
        break;

      direction = Vector3.Reflect(direction, normal);
      origin = end + normal * 2f;
      soundAt.Add(points.Count - 1);
    }

    Launch(player, points, soundAt, victim);
  }

  private void Launch(CCSPlayerController player, List<Vector3> path, List<int> soundAt, CCSPlayerController? victim)
  {
    if (path.Count < 2 || _flights.Count >= Config.Beam.MaxActive)
      return;

    var points = path.ToArray();
    var cumulative = new float[points.Length];

    for (int i = 1; i < points.Length; i++)
      cumulative[i] = cumulative[i - 1] + Vector3.Distance(points[i - 1], points[i]);

    float total = cumulative[^1];
    if (total < 1f)
      return;

    _flights.Add(new Flight
    {
      OwnerId = Util.UserId(player),
      Points = points,
      Cumulative = cumulative,
      Total = total,
      SoundAt = soundAt.ToArray(),
      DeadlineTick = Server.TickCount + (int)(total / Config.Beam.Speed * 64f) + 128,
      Color = BeamColor(player),
      VictimId = Util.UserId(victim),
      HitDistance = total
    });
  }

  private void OnTick()
  {
    if (_flights.Count == 0)
      return;

    float step = Config.Beam.Speed / 64f;
    int tick = Server.TickCount;

    for (int i = _flights.Count - 1; i >= 0; i--)
    {
      var flight = _flights[i];
      flight.Travelled += step;

      if (flight.Travelled - Config.Beam.Length >= flight.Total || tick >= flight.DeadlineTick)
      {
        Impact(flight);
        Release(flight);
        _flights.RemoveAt(i);
        continue;
      }

      float head = MathF.Min(flight.Travelled, flight.Total);
      float tail = MathF.Max(head - Config.Beam.Length, flight.Cumulative[SegmentAt(flight, head)]);

      Draw(flight, PointAt(flight, tail), PointAt(flight, head));
      EmitFire(flight);
      EmitBounce(flight, head);

      if (head >= flight.HitDistance)
        Impact(flight);
    }
  }

  private void Impact(Flight flight)
  {
    int victimId = flight.VictimId;
    flight.VictimId = -1;

    if (victimId < 0 || flight.OwnerId < 0)
      return;

    var attacker = Utilities.GetPlayerFromUserid(flight.OwnerId);
    var victim = Utilities.GetPlayerFromUserid(victimId);

    if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid)
      return;

    OnLaserHit(attacker, victim);
  }

  private void EmitFire(Flight flight)
  {
    if (!flight.FirePending)
      return;

    flight.FirePending = false;

    if (!_laserSound || Config.Sound.Fire.Length == 0 || Config.Sound.FireVolume <= 0f)
      return;

    if (flight.Beam != null && flight.Beam.IsValid)
      flight.Beam.EmitSound(Config.Sound.Fire, volume: Config.Sound.FireVolume);
  }

  private void EmitBounce(Flight flight, float head)
  {
    if (!_laserSound || flight.NextSound >= flight.SoundAt.Length
        || Config.Sound.Bounce.Length == 0 || Config.Sound.BounceVolume <= 0f)
      return;

    bool play = false;
    while (flight.NextSound < flight.SoundAt.Length && head >= flight.Cumulative[flight.SoundAt[flight.NextSound]])
    {
      flight.NextSound++;
      play = true;
    }

    if (play && flight.Beam != null && flight.Beam.IsValid)
      flight.Beam.EmitSound(Config.Sound.Bounce, volume: Config.Sound.BounceVolume);
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

    beam.Teleport(new Vector(tail.X, tail.Y, tail.Z), new QAngle(), new Vector());
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

      Style(pooled, flight.Color);
      flight.Beam = pooled;
      return pooled;
    }

    if (_owned.Count >= Config.Beam.MaxActive)
    {
      _owned.RemoveAll(entity => entity == null || !entity.IsValid);
      if (_owned.Count >= Config.Beam.MaxActive)
        return null;
    }

    var beam = Utilities.CreateEntityByName<CEnvBeam>("env_beam");
    if (beam == null || !beam.IsValid)
      return null;

    beam.DispatchSpawn();
    beam.AcceptInput("TurnOn");
    beam.SetModel(BeamSprite);
    Style(beam, flight.Color);

    _owned.Add(beam);
    flight.Beam = beam;
    return beam;
  }

  private void Style(CEnvBeam beam, Color color)
  {
    beam.Width = Config.Beam.Width;
    beam.EndWidth = Config.Beam.Width;
    Utilities.SetStateChanged(beam, "CBeam", "m_fWidth");
    Utilities.SetStateChanged(beam, "CBeam", "m_fEndWidth");

    beam.Render = color;
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

  private void ClearBeams()
  {
    foreach (var flight in _flights)
      flight.Beam = null;

    _flights.Clear();
    _pool.Clear();

    foreach (var beam in _owned)
    {
      if (beam != null && beam.IsValid)
        beam.Remove();
    }

    _owned.Clear();
    _lastShotTick.Clear();
  }

  private CCSPlayerController? FindVictim(CCSPlayerController shooter, Vector3 start, Vector3 end, out Vector3 impact)
  {
    impact = end;

    if (Vector3.DistanceSquared(start, end) < 1f)
      return null;

    CCSPlayerController? best = null;
    float bestFraction = 1f;

    int shooterId = Util.UserId(shooter);
    int shooterTeam = TeamOf(shooter);

    foreach (var target in Utilities.GetPlayers())
    {
      int targetId = Util.UserId(target);
      if (targetId < 0 || targetId == shooterId || !Util.IsAlive(target))
        continue;

      int targetTeam = TeamOf(target);
      if (targetTeam >= 0 && targetTeam == shooterTeam)
        continue;

      var pawn = target.PlayerPawn.Value;
      if (pawn?.AbsOrigin == null)
        continue;

      var feet = new Vector3(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + 4f);
      var head = new Vector3(feet.X, feet.Y, pawn.AbsOrigin.Z + pawn.ViewOffset.Z + 8f);

      if (!SegmentHitsCapsule(start, end, feet, head, Config.HitRadius, out float fraction))
        continue;

      if (fraction >= bestFraction)
        continue;

      bestFraction = fraction;
      best = target;
    }

    if (best != null)
      impact = Vector3.Lerp(start, end, bestFraction);

    return best;
  }

  private static bool SegmentHitsCapsule(Vector3 rayStart, Vector3 rayEnd, Vector3 axisStart, Vector3 axisEnd, float radius, out float fraction)
  {
    const float epsilon = .00001f;

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

  private static int SegmentAt(Flight flight, float distance)
  {
    for (int i = flight.Points.Length - 2; i >= 0; i--)
    {
      if (distance >= flight.Cumulative[i])
        return i;
    }

    return 0;
  }

  private static Vector3 PointAt(Flight flight, float distance)
  {
    int i = SegmentAt(flight, distance);

    float start = flight.Cumulative[i];
    float length = flight.Cumulative[i + 1] - start;
    if (length <= .0001f)
      return flight.Points[i];

    return Vector3.Lerp(flight.Points[i], flight.Points[i + 1], Math.Clamp((distance - start) / length, 0f, 1f));
  }

  private Color BeamColor(CCSPlayerController player)
  {
    int team = TeamOf(player);
    return team < 0 ? TeamColor(0) : TeamColor(team);
  }

  private static bool IsFinite(Vector3 value) =>
    float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

  private static void AngleVectors(QAngle angles, out Vector3 forward, out Vector3 right, out Vector3 up)
  {
    float pitch = angles.X * (MathF.PI / 180f);
    float yaw = angles.Y * (MathF.PI / 180f);
    float roll = angles.Z * (MathF.PI / 180f);

    float sp = MathF.Sin(pitch), cp = MathF.Cos(pitch);
    float sy = MathF.Sin(yaw), cy = MathF.Cos(yaw);
    float sr = MathF.Sin(roll), cr = MathF.Cos(roll);

    forward = new Vector3(cp * cy, cp * sy, -sp);
    right = new Vector3(-sr * sp * cy + cr * sy, -sr * sp * sy - cr * cy, -sr * cp);
    up = new Vector3(cr * sp * cy + sr * sy, cr * sp * sy - sr * cy, cr * cp);
  }
}
