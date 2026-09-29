using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using ByDexter.Shared.Source2;

namespace ByDexter.Shared;

public sealed class MapMatch
{
  public const double MinScore = 0.8;

  private const int MaxSpawns = 24;
  private const float Height = 40f;
  private const float Reach = 1536f;
  private const float Drop = 1024f;
  private const float Tolerance = 8f;

  private static readonly string[] SpawnClasses =
    ["info_player_terrorist", "info_player_counterterrorist", "info_deathmatch_spawn", "info_player_start"];

  private readonly List<(Vector3 From, Vector3 To, float Fraction)> _rays = [];

  public int Count => _rays.Count;

  public static MapMatch Capture()
  {
    var match = new MapMatch();
    var spawns = new List<Vector3>();

    foreach (string name in SpawnClasses)
    {
      foreach (var spawn in Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(name))
      {
        var origin = spawn.AbsOrigin;
        if (origin != null)
          spawns.Add(new Vector3(origin.X, origin.Y, origin.Z));
      }
    }

    if (spawns.Count == 0)
      return match;

    var options = new TraceOptions
    {
      InteractsWith = Contents.Solid,
      InteractsExclude = Contents.Player | Contents.Npc | Contents.Debris | Contents.Window | Contents.PassBullets
    };

    var start = new CounterStrikeSharp.API.Modules.Utils.Vector();
    var end = new CounterStrikeSharp.API.Modules.Utils.Vector();
    int step = Math.Max(1, spawns.Count / MaxSpawns);

    for (int i = 0; i < spawns.Count; i += step)
    {
      var from = spawns[i] + new Vector3(0f, 0f, Height);
      match.Add(from, from - new Vector3(0f, 0f, Drop), start, end, options);

      for (int k = 0; k < 8; k++)
      {
        float angle = k * MathF.PI / 4f + i;
        match.Add(from, from + new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0f) * Reach, start, end, options);
      }
    }

    return match;
  }

  public static Task<MapMatch> CaptureAsync(int attempts = 640)
  {
    var result = new TaskCompletionSource<MapMatch>(TaskCreationOptions.RunContinuationsAsynchronously);

    void Try(int left)
    {
      try
      {
        var match = Capture();
        if (match.Count > 0 || left <= 0)
          result.TrySetResult(match);
        else
          Server.NextFrame(() => Try(left - 1));
      }
      catch (Exception ex)
      {
        result.TrySetException(ex);
      }
    }

    Server.NextFrame(() => Try(attempts));
    return result.Task;
  }

  private void Add(Vector3 from, Vector3 to, CounterStrikeSharp.API.Modules.Utils.Vector start,
    CounterStrikeSharp.API.Modules.Utils.Vector end, TraceOptions options)
  {
    start.X = from.X; start.Y = from.Y; start.Z = from.Z;
    end.X = to.X; end.Y = to.Y; end.Z = to.Z;

    var trace = Trace.TraceEndShape(start, end, null!, options);
    if (!float.IsFinite(trace.Fraction) || trace.IsAllSolid)
      return;

    _rays.Add((from, to, Math.Clamp(trace.Fraction, 0f, 1f)));
  }

  public double Score(WorldBvh world)
  {
    if (_rays.Count == 0)
      return 1.0;

    int agree = 0;
    foreach (var (from, to, fraction) in _rays)
    {
      float ours = world.Fraction(from, to);
      if (MathF.Abs(ours - fraction) * Vector3.Distance(from, to) <= Tolerance)
        agree++;
    }

    return (double)agree / _rays.Count;
  }

  public (string Vpk, byte[] Physics, WorldBvh World, double Score)? Pick(IReadOnlyList<string> candidates, string mapName)
  {
    (string, byte[], WorldBvh, double)? best = null;

    foreach (string vpk in candidates)
    {
      byte[]? physics;
      try
      {
        physics = MapPhysics.Read(vpk, mapName);
      }
      catch
      {
        continue;
      }

      if (physics == null)
        continue;

      var world = WorldBvh.Solid(physics);
      double score = Score(world);
      if (best == null || score > best.Value.Item4)
        best = (vpk, physics, world, score);

      if (score >= 0.99)
        break;
    }

    return best is { } found && found.Item4 >= MinScore ? found : null;
  }
}
