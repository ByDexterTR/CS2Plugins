using System.IO.Compression;
using System.Numerics;
using System.Security.Cryptography;
using ByDexter.Shared.Source2;

namespace FPS.Visibility;

public sealed class VisMap
{
  private const uint Magic = 0x53495646;
  private const int Version = 6;

  public const float CellSize = 64f;
  public const float CellHeight = 64f;
  private const float EyeHeight = 64f;
  private const float CrouchEyeHeight = 46f;
  private const float StepUp = 64f;
  private const float MaxStepUp = 80f;
  private const float StairStep = 44f;
  private const int StairSamples = 4;
  private const float ClipGround = 24f;
  private const float LocateHeight = 36f;
  private const float LocateReach = 72f;
  private const float LocateRise = 48f;
  private const float MaxDrop = 240f;
  private const int ViewerDilation = 2;
  private const int TargetDilation = 1;
  private const int PlayMargin = 2;
  private const float LadderReach = 40f;
  private const float LadderBelow = 48f;
  private const float LadderAbove = 72f;
  private const float PlayAbove = 192f;
  private const float PlayBelow = 64f;
  private const float NavReach = 48f;
  private const float NavFootprint = 192f;
  private const float NavBelow = 48f;
  private const float NavAbove = 80f;
  private const float NavSpawns = 0.8f;
  private static readonly float[] TargetHeights = [68f, 36f, 8f, 100f];
  private const float TargetCorner = 28f;
  private static readonly float[] BodyHeights = [24f, 48f, 70f];

  public string Hash { get; private set; } = "";
  public int Count { get; private set; }
  public Vector3 Min { get; private set; }
  public int NX { get; private set; }
  public int NY { get; private set; }
  public int NZ { get; private set; }

  private int[] _cells = [];
  private Vector3[] _floors = [];
  private ulong[] _bits = [];
  private int _words;

  public long Bytes => _bits.LongLength * 8 + _cells.LongLength * 4;

  public static string HashOf(byte[] physics) => Convert.ToHexString(SHA256.HashData(physics), 0, 12);

  public int CellOf(Vector3 origin)
  {
    int x = (int)MathF.Floor((origin.X - Min.X) / CellSize);
    int y = (int)MathF.Floor((origin.Y - Min.Y) / CellSize);
    int z = (int)MathF.Floor((origin.Z - Min.Z) / CellHeight);
    if ((uint)x >= (uint)NX || (uint)y >= (uint)NY)
      return -1;

    for (int k = z; k >= z - 1; k--)
    {
      if ((uint)k >= (uint)NZ)
        continue;

      int cell = _cells[(k * NY + y) * NX + x];
      if (cell >= 0)
        return cell;
    }

    return -1;
  }

  public int Locate(Vector3 origin, WorldBvh? world)
  {
    int found = CellOf(origin);
    if (found >= 0 || world == null)
      return found;

    int x = (int)MathF.Floor((origin.X - Min.X) / CellSize);
    int y = (int)MathF.Floor((origin.Y - Min.Y) / CellSize);
    int z = (int)MathF.Floor((origin.Z - Min.Z) / CellHeight);
    var eye = origin + new Vector3(0f, 0f, LocateHeight);
    float best = float.MaxValue;

    for (int k = z + 1; k >= z - 1; k--)
    for (int dy = -1; dy <= 1; dy++)
    for (int dx = -1; dx <= 1; dx++)
    {
      int cx = x + dx, cy = y + dy;
      if ((uint)k >= (uint)NZ || (uint)cx >= (uint)NX || (uint)cy >= (uint)NY)
        continue;

      int cell = _cells[(k * NY + cy) * NX + cx];
      if (cell < 0)
        continue;

      var floor = _floors[cell];
      float flat = (floor.X - origin.X) * (floor.X - origin.X) + (floor.Y - origin.Y) * (floor.Y - origin.Y);
      if (flat > LocateReach * LocateReach || MathF.Abs(floor.Z - origin.Z) > LocateRise)
        continue;

      float distance = Vector3.DistanceSquared(floor, origin);
      if (distance >= best || world.Blocked(eye, floor + new Vector3(0f, 0f, LocateHeight)))
        continue;

      best = distance;
      found = cell;
    }

    return found;
  }

  public bool Visible(int from, int to) => (_bits[(long)from * _words + (to >> 6)] & (1UL << (to & 63))) != 0;

  public static VisMap Bake(WorldBvh world, WorldBvh clip, IReadOnlyList<(Vector3 Min, Vector3 Max)> ladders, IReadOnlyList<Vector3> spawns, IReadOnlyList<Vector3[]>? nav, string hash, int threads, CancellationToken token)
  {
    var map = new VisMap { Hash = hash, Min = world.Min - new Vector3(0f, 0f, 8f) };
    var size = world.Max - map.Min;
    map.NX = (int)(size.X / CellSize) + 1;
    map.NY = (int)(size.Y / CellSize) + 1;
    map.NZ = (int)(size.Z / CellHeight) + 2;
    map._cells = new int[map.NX * map.NY * map.NZ];
    Array.Fill(map._cells, -1);

    var all = map.FindFloors(world, clip);
    var floors = map.Reachable(world, clip, all, spawns, ladders);
    floors = Trusted(nav, spawns) ? map.WithNav(all, floors, nav!) : map.WithinPlay(all, floors);
    token.ThrowIfCancellationRequested();

    map.Count = floors.Count;
    map._floors = [.. floors];
    map._words = (floors.Count + 63) >> 6;
    var raw = new ulong[(long)floors.Count * map._words];
    Run(threads, floors.Count, token, a => map.BakeRow(world, floors, raw, a));
    token.ThrowIfCancellationRequested();

    map._bits = map.Dilate(floors, raw, threads, token);
    return map;
  }

  private List<Vector3> FindFloors(WorldBvh world, WorldBvh clip)
  {
    var floors = new List<Vector3>();
    float top = world.Max.Z + 1f;
    float bottom = world.Min.Z - 1f;

    float q = CellSize * 0.25f;
    Span<Vector2> samples = [Vector2.Zero, new(q, q), new(-q, q), new(q, -q), new(-q, -q)];

    for (int x = 0; x < NX; x++)
    for (int y = 0; y < NY; y++)
    foreach (var sample in samples)
    {
      float px = Min.X + (x + 0.5f) * CellSize + sample.X, py = Min.Y + (y + 0.5f) * CellSize + sample.Y;
      float start = top;

      for (int guard = 0; guard < 32 && start > bottom; guard++)
      {
        float t = clip.Fraction(new Vector3(px, py, start), new Vector3(px, py, bottom));
        if (t >= 1f)
          break;

        float z = start + t * (bottom - start);
        var floor = new Vector3(px, py, z);
        bool grounded = world.Fraction(floor + new Vector3(0f, 0f, 1f), floor - new Vector3(0f, 0f, ClipGround)) < 1f;

        if (grounded && !clip.Blocked(floor + new Vector3(0f, 0f, 2f), floor + new Vector3(0f, 0f, 70f)))
        {
          int k = (int)((z - Min.Z) / CellHeight);
          int slot = (k * NY + y) * NX + x;
          if ((uint)k < (uint)NZ && _cells[slot] < 0)
          {
            _cells[slot] = floors.Count;
            floors.Add(floor);
          }
        }

        start = z - 1f;
      }
    }

    return floors;
  }

  private int Seed(List<Vector3> floors, Vector3 ground)
  {
    var (gx, gy, gz) = Slot(ground);
    int best = -1;
    float bestDistance = float.MaxValue;

    for (int z = gz - 1; z <= gz + 1; z++)
    for (int y = gy - 1; y <= gy + 1; y++)
    for (int x = gx - 1; x <= gx + 1; x++)
    {
      if ((uint)x >= (uint)NX || (uint)y >= (uint)NY || (uint)z >= (uint)NZ)
        continue;

      int cell = _cells[(z * NY + y) * NX + x];
      if (cell < 0 || MathF.Abs(floors[cell].Z - ground.Z) > MaxStepUp)
        continue;

      float distance = Vector3.DistanceSquared(floors[cell], ground);
      if (distance < bestDistance)
      {
        bestDistance = distance;
        best = cell;
      }
    }

    return best;
  }

  private (int X, int Y, int Z) Slot(Vector3 floor) =>
    ((int)((floor.X - Min.X) / CellSize), (int)((floor.Y - Min.Y) / CellSize), (int)((floor.Z - Min.Z) / CellHeight));

  private List<Vector3> Reachable(WorldBvh world, WorldBvh clip, List<Vector3> floors, IReadOnlyList<Vector3> spawns, IReadOnlyList<(Vector3 Min, Vector3 Max)> ladders)
  {
    var climbs = new List<int>[floors.Count];
    var groups = new List<List<int>>();
    foreach (var (low, high) in ladders)
    {
      var group = new List<int>();
      for (int i = 0; i < floors.Count; i++)
      {
        var f = floors[i];
        float dx = MathF.Max(0f, MathF.Max(low.X - f.X, f.X - high.X));
        float dy = MathF.Max(0f, MathF.Max(low.Y - f.Y, f.Y - high.Y));
        if (dx * dx + dy * dy <= LadderReach * LadderReach && f.Z >= low.Z - LadderBelow && f.Z <= high.Z + LadderAbove)
          group.Add(i);
      }

      if (group.Count < 2)
        continue;

      foreach (int i in group)
        (climbs[i] ??= []).Add(groups.Count);
      groups.Add(group);
    }

    var seen = new bool[floors.Count];
    var queue = new Queue<int>();

    foreach (var spawn in spawns)
    {
      var from = spawn + new Vector3(0f, 0f, 32f);
      float t = clip.Fraction(from, from - new Vector3(0f, 0f, 232f));
      if (t >= 1f)
        continue;

      int cell = Seed(floors, from - new Vector3(0f, 0f, 232f * t));
      if (cell >= 0 && !seen[cell])
      {
        seen[cell] = true;
        queue.Enqueue(cell);
      }
    }

    if (queue.Count == 0)
      return floors;

    while (queue.Count > 0)
    {
      int a = queue.Dequeue();
      var fa = floors[a];
      var (ax, ay, az) = Slot(fa);

      if (climbs[a] != null)
      {
        foreach (int g in climbs[a])
        foreach (int b in groups[g])
        {
          if (!seen[b])
          {
            seen[b] = true;
            queue.Enqueue(b);
          }
        }
      }

      for (int z = az - 6; z <= az + 2; z++)
      for (int y = ay - 1; y <= ay + 1; y++)
      for (int x = ax - 1; x <= ax + 1; x++)
      {
        if ((uint)x >= (uint)NX || (uint)y >= (uint)NY || (uint)z >= (uint)NZ)
          continue;

        int b = _cells[(z * NY + y) * NX + x];
        if (b < 0 || seen[b])
          continue;

        var fb = floors[b];
        if (fb.Z - fa.Z > MaxStepUp || fa.Z - fb.Z > MaxDrop)
          continue;

        if (!Walkable(clip, fa, fb))
          continue;

        seen[b] = true;
        queue.Enqueue(b);
      }
    }

    var kept = new List<Vector3>();
    Array.Fill(_cells, -1);

    for (int i = 0; i < floors.Count; i++)
    {
      if (!seen[i])
        continue;

      var (x, y, z) = Slot(floors[i]);
      _cells[(z * NY + y) * NX + x] = kept.Count;
      kept.Add(floors[i]);
    }

    return kept;
  }

  private static bool Trusted(IReadOnlyList<Vector3[]>? nav, IReadOnlyList<Vector3> spawns) =>
    nav != null && nav.Count > 0 && (spawns.Count == 0 || spawns.Count(spawn => nav.Any(polygon => Near(polygon, spawn))) >= spawns.Count * NavSpawns);

  private List<Vector3> WithNav(List<Vector3> all, List<Vector3> floors, IReadOnlyList<Vector3[]> nav)
  {
    var near = Buckets(nav, NavReach);
    var footprint = Buckets(nav, NavFootprint);
    var island = Islands(nav);

    var reached = floors.Where(floor => footprint.ContainsKey(Column(floor))).ToList();
    var played = new HashSet<int>();
    foreach (var floor in reached)
    {
      if (!near.TryGetValue(Column(floor), out var list))
        continue;

      foreach (int i in list)
      {
        if (!played.Contains(island[i]) && Near(nav[i], floor, float.MaxValue))
          played.Add(island[i]);
      }
    }

    var kept = new List<Vector3>(reached);
    var known = new HashSet<Vector3>(reached);
    foreach (var floor in all)
    {
      if (!known.Contains(floor) && near.TryGetValue(Column(floor), out var list) && list.Exists(i => played.Contains(island[i]) && Near(nav[i], floor, NavAbove)))
        kept.Add(floor);
    }

    Array.Fill(_cells, -1);
    for (int i = 0; i < kept.Count; i++)
    {
      var (x, y, z) = Slot(kept[i]);
      _cells[(z * NY + y) * NX + x] = i;
    }

    return kept;
  }

  private int Column(Vector3 floor)
  {
    var (x, y, _) = Slot(floor);
    return y * NX + x;
  }

  private Dictionary<int, List<int>> Buckets(IReadOnlyList<Vector3[]> nav, float margin)
  {
    var buckets = new Dictionary<int, List<int>>();
    for (int i = 0; i < nav.Count; i++)
    {
      float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
      foreach (var corner in nav[i])
      {
        minX = MathF.Min(minX, corner.X); maxX = MathF.Max(maxX, corner.X);
        minY = MathF.Min(minY, corner.Y); maxY = MathF.Max(maxY, corner.Y);
      }

      int x0 = Math.Max(0, (int)((minX - margin - Min.X) / CellSize)), x1 = Math.Min(NX - 1, (int)((maxX + margin - Min.X) / CellSize));
      int y0 = Math.Max(0, (int)((minY - margin - Min.Y) / CellSize)), y1 = Math.Min(NY - 1, (int)((maxY + margin - Min.Y) / CellSize));
      for (int y = y0; y <= y1; y++)
      for (int x = x0; x <= x1; x++)
      {
        if (!buckets.TryGetValue(y * NX + x, out var list))
          buckets[y * NX + x] = list = [];
        list.Add(i);
      }
    }

    return buckets;
  }

  private static int[] Islands(IReadOnlyList<Vector3[]> nav)
  {
    var parent = new int[nav.Count];
    for (int i = 0; i < parent.Length; i++)
      parent[i] = i;

    int Root(int i)
    {
      while (parent[i] != i)
        i = parent[i] = parent[parent[i]];
      return i;
    }

    var owner = new Dictionary<Vector3, int>();
    for (int i = 0; i < nav.Count; i++)
    {
      foreach (var corner in nav[i])
      {
        if (owner.TryGetValue(corner, out int other))
          parent[Root(i)] = Root(other);
        else
          owner[corner] = i;
      }
    }

    var island = new int[nav.Count];
    for (int i = 0; i < island.Length; i++)
      island[i] = Root(i);
    return island;
  }

  private List<Vector3> WithinPlay(List<Vector3> all, List<Vector3> floors)
  {
    if (all.Count == floors.Count)
      return floors;

    var columns = new Dictionary<int, (float Low, float High)>();
    foreach (var floor in floors)
    {
      var (x, y, _) = Slot(floor);
      for (int dy = -PlayMargin; dy <= PlayMargin; dy++)
      for (int dx = -PlayMargin; dx <= PlayMargin; dx++)
      {
        if ((uint)(x + dx) >= (uint)NX || (uint)(y + dy) >= (uint)NY)
          continue;

        int key = (y + dy) * NX + x + dx;
        columns[key] = columns.TryGetValue(key, out var range)
          ? (MathF.Min(range.Low, floor.Z), MathF.Max(range.High, floor.Z))
          : (floor.Z, floor.Z);
      }
    }

    var kept = new List<Vector3>(floors);
    var known = new HashSet<Vector3>(floors);
    foreach (var floor in all)
    {
      var (x, y, _) = Slot(floor);
      if (!known.Contains(floor) && columns.TryGetValue(y * NX + x, out var range)
          && floor.Z >= range.Low - PlayBelow && floor.Z <= range.High + PlayAbove)
        kept.Add(floor);
    }

    Array.Fill(_cells, -1);
    for (int i = 0; i < kept.Count; i++)
    {
      var (x, y, z) = Slot(kept[i]);
      _cells[(z * NY + y) * NX + x] = i;
    }

    return kept;
  }

  private static bool Near(Vector3[] polygon, Vector3 point) => Near(polygon, point, NavAbove);

  private static bool Near(Vector3[] polygon, Vector3 point, float above)
  {
    float low = float.MaxValue, high = float.MinValue;
    foreach (var corner in polygon)
    {
      low = MathF.Min(low, corner.Z);
      high = MathF.Max(high, corner.Z);
    }

    if (above < float.MaxValue && (point.Z < low - NavBelow || point.Z > high + above))
      return false;

    bool inside = false;
    float best = float.MaxValue;
    for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
    {
      var a = new Vector2(polygon[i].X, polygon[i].Y);
      var b = new Vector2(polygon[j].X, polygon[j].Y);
      var p = new Vector2(point.X, point.Y);

      if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
        inside = !inside;

      var edge = b - a;
      float t = edge.LengthSquared() > 0f ? Math.Clamp(Vector2.Dot(p - a, edge) / edge.LengthSquared(), 0f, 1f) : 0f;
      best = MathF.Min(best, Vector2.DistanceSquared(p, a + edge * t));
    }

    return inside || best <= NavReach * NavReach;
  }

  private static bool Walkable(WorldBvh clip, Vector3 a, Vector3 b)
  {
    float top = MathF.Max(a.Z, b.Z);
    if (Vertical(clip, a, top) || Vertical(clip, b, top))
      return false;

    var middle = (a + b) * 0.5f;
    if (b.Z - a.Z > StepUp && !Stairs(clip, a, middle, b))
      return false;

    foreach (float height in BodyHeights)
    {
      var pa = new Vector3(a.X, a.Y, top + height);
      var pb = new Vector3(b.X, b.Y, top + height);
      if (clip.Blocked(pa, pb) || clip.Blocked(pb, pa))
        return false;
    }

    return true;
  }

  private static bool Stairs(WorldBvh clip, Vector3 low, Vector3 middle, Vector3 high)
  {
    float previous = low.Z;
    for (int i = 1; i < StairSamples; i++)
    {
      float f = i / (float)StairSamples;
      var point = f < 0.5f ? Vector3.Lerp(low, middle, f * 2f) : Vector3.Lerp(middle, high, f * 2f - 1f);
      var from = new Vector3(point.X, point.Y, high.Z + 70f);
      var to = new Vector3(point.X, point.Y, low.Z - 8f);
      float t = clip.Fraction(from, to);
      if (t >= 1f)
        return false;

      float z = from.Z + (to.Z - from.Z) * t;
      if (z - previous > StairStep || z < low.Z - StairStep)
        return false;

      previous = MathF.Max(previous, z);
    }

    return high.Z - previous <= StairStep;
  }

  private static bool Vertical(WorldBvh clip, Vector3 floor, float top)
  {
    if (top - floor.Z < 1f)
      return false;

    var high = new Vector3(floor.X, floor.Y, top + BodyHeights[0]);
    var low = floor + new Vector3(0f, 0f, BodyHeights[0]);
    return clip.Blocked(high, low) || clip.Blocked(low, high);
  }

  private void BakeRow(WorldBvh world, List<Vector3> floors, ulong[] raw, int a)
  {
    float h = CellSize * 0.45f;
    Span<Vector3> corners = [Vector3.Zero, new(h, h, 0f), new(-h, h, 0f), new(h, -h, 0f), new(-h, -h, 0f)];
    Span<Vector3> eyes = stackalloc Vector3[6];
    Span<WorldBvh.Face> hints = stackalloc WorldBvh.Face[6];
    var fa = floors[a];

    for (int i = 0; i < 5; i++)
      eyes[i] = fa + corners[i] + new Vector3(0f, 0f, EyeHeight);
    eyes[5] = fa + new Vector3(0f, 0f, CrouchEyeHeight);

    long row = (long)a * _words;

    for (int b = 0; b < floors.Count; b++)
    {
      if (a == b || Sees(world, eyes, hints, floors[b]))
        raw[row + (b >> 6)] |= 1UL << (b & 63);
    }
  }

  private static bool Sees(WorldBvh world, ReadOnlySpan<Vector3> eyes, Span<WorldBvh.Face> hints, Vector3 target)
  {
    for (int e = 0; e < eyes.Length; e++)
    {
      foreach (float height in TargetHeights)
      {
        if (Clear(world, eyes[e], ref hints[e], target + new Vector3(0f, 0f, height)))
          return true;
      }
    }

    var head = target + new Vector3(0f, 0f, TargetHeights[0]);
    Span<Vector3> sides = [new(TargetCorner, TargetCorner, 0f), new(-TargetCorner, TargetCorner, 0f), new(TargetCorner, -TargetCorner, 0f), new(-TargetCorner, -TargetCorner, 0f)];
    for (int e = 0; e < eyes.Length; e++)
    {
      foreach (var side in sides)
      {
        if (Clear(world, eyes[e], ref hints[e], head + side))
          return true;
      }
    }

    return false;
  }

  private static bool Clear(WorldBvh world, Vector3 eye, ref WorldBvh.Face hint, Vector3 target)
  {
    if (WorldBvh.Hits(hint, eye, target))
      return false;

    int blocker = world.Blocker(eye, target);
    if (blocker < 0)
      return true;

    hint = world.FaceOf(blocker);
    return false;
  }

  private int[][] Neighbours(List<Vector3> floors, int reach)
  {
    var neighbours = new int[floors.Count][];
    var list = new List<int>();

    for (int c = 0; c < floors.Count; c++)
    {
      list.Clear();
      var (cx, cy, cz) = Slot(floors[c]);

      for (int z = cz - 1; z <= cz + 1; z++)
      for (int y = cy - reach; y <= cy + reach; y++)
      for (int x = cx - reach; x <= cx + reach; x++)
      {
        if ((uint)x >= (uint)NX || (uint)y >= (uint)NY || (uint)z >= (uint)NZ)
          continue;

        int cell = _cells[(z * NY + y) * NX + x];
        if (cell >= 0)
          list.Add(cell);
      }

      neighbours[c] = [.. list];
    }

    return neighbours;
  }

  private ulong[] Dilate(List<Vector3> floors, ulong[] raw, int threads, CancellationToken token)
  {
    int n = floors.Count;
    var viewers = Neighbours(floors, ViewerDilation);
    var targets = Neighbours(floors, TargetDilation);

    var rows = new ulong[raw.Length];
    Run(threads, n, token, a =>
    {
      var target = rows.AsSpan((int)((long)a * _words), _words);
      foreach (int na in viewers[a])
      {
        var source = raw.AsSpan((int)((long)na * _words), _words);
        for (int w = 0; w < _words; w++)
          target[w] |= source[w];
      }
    });

    var result = new ulong[raw.Length];
    Run(threads, n, token, a =>
    {
      var source = rows.AsSpan((int)((long)a * _words), _words);
      var target = result.AsSpan((int)((long)a * _words), _words);

      for (int w = 0; w < _words; w++)
      {
        ulong word = source[w];
        while (word != 0)
        {
          int b = (w << 6) + BitOperations.TrailingZeroCount(word);
          word &= word - 1;
          foreach (int nb in targets[b])
            target[nb >> 6] |= 1UL << (nb & 63);
        }
      }
    });

    return result;
  }

  private static void Run(int threads, int count, CancellationToken token, Action<int> work)
  {
    int next = -1;
    var workers = new Thread[Math.Max(1, threads)];

    for (int i = 0; i < workers.Length; i++)
    {
      workers[i] = new Thread(() =>
      {
        int item;
        while (!token.IsCancellationRequested && (item = Interlocked.Increment(ref next)) < count)
          work(item);
      })
      {
        IsBackground = true,
        Priority = ThreadPriority.BelowNormal,
        Name = "FPS visibility"
      };
      workers[i].Start();
    }

    foreach (var worker in workers)
      worker.Join();
  }

  public void Save(string path)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    string temp = path + ".tmp";

    using (var file = File.Create(temp))
    using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
    using (var writer = new BinaryWriter(zip))
    {
      writer.Write(Magic);
      writer.Write(Version);
      writer.Write(Hash);
      writer.Write(CellSize);
      writer.Write(CellHeight);
      writer.Write(ViewerDilation * 10 + TargetDilation);
      writer.Write(Min.X);
      writer.Write(Min.Y);
      writer.Write(Min.Z);
      writer.Write(NX);
      writer.Write(NY);
      writer.Write(NZ);
      writer.Write(Count);

      foreach (int cell in _cells)
        writer.Write(cell);

      foreach (var floor in _floors)
      {
        writer.Write(floor.X);
        writer.Write(floor.Y);
        writer.Write(floor.Z);
      }

      foreach (ulong word in _bits)
        writer.Write(word);
    }

    File.Move(temp, path, true);
  }

  public static VisMap? Load(string path, string hash)
  {
    if (!File.Exists(path))
      return null;

    using var file = File.OpenRead(path);
    using var zip = new GZipStream(file, CompressionMode.Decompress);
    using var reader = new BinaryReader(zip);

    if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version || reader.ReadString() != hash
        || reader.ReadSingle() != CellSize || reader.ReadSingle() != CellHeight || reader.ReadInt32() != ViewerDilation * 10 + TargetDilation)
      return null;

    var map = new VisMap
    {
      Hash = hash,
      Min = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
      NX = reader.ReadInt32(),
      NY = reader.ReadInt32(),
      NZ = reader.ReadInt32(),
      Count = reader.ReadInt32()
    };

    map._cells = new int[map.NX * map.NY * map.NZ];
    for (int i = 0; i < map._cells.Length; i++)
      map._cells[i] = reader.ReadInt32();

    map._floors = new Vector3[map.Count];
    for (int i = 0; i < map.Count; i++)
      map._floors[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

    map._words = (map.Count + 63) >> 6;
    map._bits = new ulong[(long)map.Count * map._words];
    for (int i = 0; i < map._bits.Length; i++)
      map._bits[i] = reader.ReadUInt64();

    return map;
  }
}
