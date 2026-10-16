using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace ByDexter.Shared.Source2;

public sealed class WorldBvh
{
  private struct Node
  {
    public Vector3 Min;
    public int LeftOrFirst;
    public Vector3 Max;
    public int Count;
  }

  private readonly record struct Triangle(Vector3 A, Vector3 B, Vector3 C, bool OneSided);

  private const int Bins = 12;
  private const int LeafSize = 4;

  private const int StackSize = 256;

  private Face[] _faces = [];
  private Node[] _nodes = [];
  private int _nodeCount;
  private Vector3 _min;
  private Vector3 _max;

  private int _width;
  private float[] _minX = [], _minY = [], _minZ = [], _maxX = [], _maxY = [], _maxZ = [];
  private int[] _child = [];
  private int[] _count = [];
  private int[] _valid = [];

  public int TriangleCount => _faces.Length;
  public Vector3 Min => _min;
  public Vector3 Max => _max;

  private static readonly string[] PlayerBlockers = ["playerclip", "passbullets", "window", "solid"];

  public static WorldBvh Solid(byte[] physics) => Build(Extract(physics, attribute => attribute.InteractAs.Count == 0, false));

  public static WorldBvh From(Kv3Binary document, IReadOnlyList<ClipAttribute> attributes, Func<ClipAttribute, bool> filter, bool twoSided) =>
    Build(Extract(document, attributes, filter, twoSided));

  public static WorldBvh PlayerSolid(byte[] physics) => Build(Extract(physics, attribute =>
    !Has(attribute.InteractExclude, "player") && (attribute.InteractAs.Count == 0 || PlayerBlockers.Any(tag => Has(attribute.InteractAs, tag))), true));

  public static List<(Vector3 Min, Vector3 Max)> Ladders(byte[] physics)
  {
    var triangles = Extract(physics, attribute => Has(attribute.InteractAs, "ladder"), true);
    var boxes = new List<(Vector3 Min, Vector3 Max)>();
    foreach (var t in triangles)
    {
      var min = Vector3.Min(t.A, Vector3.Min(t.B, t.C)) - new Vector3(LadderJoin);
      var max = Vector3.Max(t.A, Vector3.Max(t.B, t.C)) + new Vector3(LadderJoin);
      boxes.Add((min, max));
    }

    bool merged = true;
    while (merged)
    {
      merged = false;
      for (int i = 0; i < boxes.Count && !merged; i++)
      for (int j = i + 1; j < boxes.Count; j++)
      {
        var a = boxes[i];
        var b = boxes[j];
        if (a.Min.X > b.Max.X || b.Min.X > a.Max.X || a.Min.Y > b.Max.Y || b.Min.Y > a.Max.Y || a.Min.Z > b.Max.Z || b.Min.Z > a.Max.Z)
          continue;

        boxes[i] = (Vector3.Min(a.Min, b.Min), Vector3.Max(a.Max, b.Max));
        boxes.RemoveAt(j);
        merged = true;
        break;
      }
    }

    return boxes.Select(box => (box.Min + new Vector3(LadderJoin), box.Max - new Vector3(LadderJoin))).ToList();
  }

  private const float LadderJoin = 4f;

  private static bool Has(List<string> tags, string tag) => tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));

  private static List<Triangle> Extract(byte[] physics, Func<ClipAttribute, bool> filter, bool twoSided)
  {
    var block = Source2Resource.FindBlock(physics, "PHYS") ?? Source2Resource.FindBlock(physics, "DATA")
      ?? throw new InvalidDataException("world_physics PHYS");

    var document = Kv3Binary.Load(physics, block.Offset, block.Size);
    var attributes = new AttributeScanner();
    document.Walk(attributes);

    return Extract(document, attributes.Attributes, filter, twoSided);
  }

  private static List<Triangle> Extract(Kv3Binary document, IReadOnlyList<ClipAttribute> attributes, Func<ClipAttribute, bool> filter, bool twoSided)
  {
    var wanted = new HashSet<int>();
    for (int i = 0; i < attributes.Count; i++)
    {
      if (filter(attributes[i]))
        wanted.Add(i);
    }

    var geometry = new GeometryScanner(wanted);
    document.Walk(geometry);

    var triangles = new List<Triangle>();
    foreach (var hull in geometry.Hulls)
      AddHull(hull, triangles);

    foreach (var mesh in geometry.Meshes)
    {
      for (int i = 0; i + 2 < mesh.Triangles.Length; i += 3)
      {
        int a = mesh.Triangles[i], b = mesh.Triangles[i + 1], c = mesh.Triangles[i + 2];
        if (a < mesh.Vertices.Length && b < mesh.Vertices.Length && c < mesh.Vertices.Length)
          triangles.Add(new Triangle(mesh.Vertices[a], mesh.Vertices[b], mesh.Vertices[c], !twoSided));
      }
    }

    return triangles;
  }

  private static void AddHull(ClipHull hull, List<Triangle> triangles)
  {
    int edgeCount = hull.Edges.Length / 4;
    var faces = new HashSet<int>();
    var polygon = new List<int>();

    for (int edge = 0; edge < edgeCount; edge++)
    {
      if (!faces.Add(hull.Edges[edge * 4 + 3]))
        continue;

      polygon.Clear();
      int current = edge;
      for (int guard = 0; guard < 256; guard++)
      {
        polygon.Add(hull.Edges[current * 4 + 2]);
        current = hull.Edges[current * 4];
        if (current == edge || current >= edgeCount)
          break;
      }

      for (int k = 1; k + 1 < polygon.Count; k++)
      {
        int a = polygon[0], b = polygon[k], c = polygon[k + 1];
        if (a < hull.Positions.Length && b < hull.Positions.Length && c < hull.Positions.Length)
          triangles.Add(new Triangle(hull.Positions[a], hull.Positions[b], hull.Positions[c], false));
      }
    }
  }

  private static WorldBvh Build(List<Triangle> triangles)
  {
    int count = triangles.Count;
    if (count == 0)
      throw new InvalidDataException("world_physics bos");

    var bvh = new WorldBvh();
    var centers = new Vector3[count];
    var mins = new Vector3[count];
    var maxs = new Vector3[count];
    var index = new int[count];

    for (int i = 0; i < count; i++)
    {
      var t = triangles[i];
      mins[i] = Vector3.Min(t.A, Vector3.Min(t.B, t.C));
      maxs[i] = Vector3.Max(t.A, Vector3.Max(t.B, t.C));
      centers[i] = (mins[i] + maxs[i]) * 0.5f;
      index[i] = i;
    }

    bvh._nodes = new Node[2 * count];
    bvh._nodeCount = 1;
    bvh._nodes[0] = new Node { LeftOrFirst = 0, Count = count };
    bvh.Bounds(0, index, mins, maxs);

    var pending = new Stack<int>();
    pending.Push(0);
    while (pending.Count > 0)
    {
      int node = pending.Pop();
      if (bvh.Split(node, index, centers, mins, maxs))
      {
        pending.Push(bvh._nodes[node].LeftOrFirst);
        pending.Push(bvh._nodes[node].LeftOrFirst + 1);
      }
    }

    bvh._faces = new Face[count];

    for (int i = 0; i < count; i++)
    {
      var t = triangles[index[i]];
      bvh._faces[i] = new Face(t.A, t.B - t.A, t.C - t.A, t.OneSided);
    }

    bvh._min = bvh._nodes[0].Min;
    bvh._max = bvh._nodes[0].Max;
    bvh.Widen(Vector256.IsHardwareAccelerated ? 8 : 4);
    bvh._nodes = [];
    return bvh;
  }

  private void Widen(int width)
  {
    _width = width;
    var valid = new List<int> { 0 };
    var slots = new List<(Vector3 Min, Vector3 Max, int Child, int Count)>();
    var children = new List<int>(width);
    var order = new Queue<(int Binary, int Wide)>();
    order.Enqueue((0, 0));

    while (order.Count > 0)
    {
      var (binary, at) = order.Dequeue();
      children.Clear();

      if (_nodes[binary].Count > 0)
        children.Add(binary);
      else
      {
        children.Add(_nodes[binary].LeftOrFirst);
        children.Add(_nodes[binary].LeftOrFirst + 1);
      }

      while (children.Count < width)
      {
        int best = -1;
        float bestArea = -1f;
        for (int i = 0; i < children.Count; i++)
        {
          ref var c = ref _nodes[children[i]];
          if (c.Count > 0)
            continue;

          float area = Area(c.Min, c.Max);
          if (area > bestArea)
          {
            bestArea = area;
            best = i;
          }
        }

        if (best < 0)
          break;

        int split = children[best];
        children[best] = _nodes[split].LeftOrFirst;
        children.Add(_nodes[split].LeftOrFirst + 1);
      }

      while (slots.Count < (at + 1) * width)
        slots.Add((Vector3.Zero, Vector3.Zero, 0, 0));

      for (int i = 0; i < children.Count; i++)
      {
        ref var c = ref _nodes[children[i]];
        if (c.Count > 0)
        {
          slots[at * width + i] = (c.Min, c.Max, c.LeftOrFirst, c.Count);
          continue;
        }

        int next = valid.Count;
        valid.Add(0);
        order.Enqueue((children[i], next));
        slots[at * width + i] = (c.Min, c.Max, next, 0);
      }

      valid[at] = (1 << children.Count) - 1;
    }

    int n = valid.Count * width;
    _minX = new float[n]; _minY = new float[n]; _minZ = new float[n];
    _maxX = new float[n]; _maxY = new float[n]; _maxZ = new float[n];
    _child = new int[n];
    _count = new int[n];
    _valid = [.. valid];

    for (int i = 0; i < slots.Count; i++)
    {
      var (min, max, child, count) = slots[i];
      _minX[i] = min.X; _minY[i] = min.Y; _minZ[i] = min.Z;
      _maxX[i] = max.X; _maxY[i] = max.Y; _maxZ[i] = max.Z;
      _child[i] = child;
      _count[i] = count;
    }
  }

  private void Bounds(int nodeIndex, int[] index, Vector3[] mins, Vector3[] maxs)
  {
    ref var node = ref _nodes[nodeIndex];
    var min = new Vector3(float.MaxValue);
    var max = new Vector3(float.MinValue);

    for (int i = node.LeftOrFirst; i < node.LeftOrFirst + node.Count; i++)
    {
      min = Vector3.Min(min, mins[index[i]]);
      max = Vector3.Max(max, maxs[index[i]]);
    }

    node.Min = min;
    node.Max = max;
  }

  private bool Split(int nodeIndex, int[] index, Vector3[] centers, Vector3[] mins, Vector3[] maxs)
  {
    var node = _nodes[nodeIndex];
    if (node.Count <= LeafSize)
      return false;

    int first = node.LeftOrFirst, count = node.Count;
    var centerMin = new Vector3(float.MaxValue);
    var centerMax = new Vector3(float.MinValue);

    for (int i = first; i < first + count; i++)
    {
      centerMin = Vector3.Min(centerMin, centers[index[i]]);
      centerMax = Vector3.Max(centerMax, centers[index[i]]);
    }

    float bestCost = float.MaxValue;
    int bestAxis = -1;
    float bestSplit = 0f;
    Span<int> binCount = stackalloc int[Bins];
    Span<Vector3> binMin = stackalloc Vector3[Bins];
    Span<Vector3> binMax = stackalloc Vector3[Bins];

    for (int axis = 0; axis < 3; axis++)
    {
      float low = Axis(centerMin, axis), high = Axis(centerMax, axis);
      if (high - low < 1e-4f)
        continue;

      binCount.Clear();
      binMin.Fill(new Vector3(float.MaxValue));
      binMax.Fill(new Vector3(float.MinValue));
      float scale = Bins / (high - low);

      for (int i = first; i < first + count; i++)
      {
        int t = index[i];
        int bin = Math.Min(Bins - 1, (int)((Axis(centers[t], axis) - low) * scale));
        binCount[bin]++;
        binMin[bin] = Vector3.Min(binMin[bin], mins[t]);
        binMax[bin] = Vector3.Max(binMax[bin], maxs[t]);
      }

      for (int split = 1; split < Bins; split++)
      {
        int leftCount = 0, rightCount = 0;
        var leftMin = new Vector3(float.MaxValue);
        var leftMax = new Vector3(float.MinValue);
        var rightMin = leftMin;
        var rightMax = leftMax;

        for (int bin = 0; bin < Bins; bin++)
        {
          if (binCount[bin] == 0)
            continue;

          if (bin < split)
          {
            leftCount += binCount[bin];
            leftMin = Vector3.Min(leftMin, binMin[bin]);
            leftMax = Vector3.Max(leftMax, binMax[bin]);
          }
          else
          {
            rightCount += binCount[bin];
            rightMin = Vector3.Min(rightMin, binMin[bin]);
            rightMax = Vector3.Max(rightMax, binMax[bin]);
          }
        }

        if (leftCount == 0 || rightCount == 0)
          continue;

        float cost = leftCount * Area(leftMin, leftMax) + rightCount * Area(rightMin, rightMax);
        if (cost < bestCost)
        {
          bestCost = cost;
          bestAxis = axis;
          bestSplit = low + split / scale;
        }
      }
    }

    if (bestAxis < 0 || bestCost >= count * Area(node.Min, node.Max))
      return false;

    int l = first, r = first + count - 1;
    while (l <= r)
    {
      if (Axis(centers[index[l]], bestAxis) < bestSplit)
      {
        l++;
        continue;
      }

      (index[l], index[r]) = (index[r], index[l]);
      r--;
    }

    int left = l - first;
    if (left == 0 || left == count)
      return false;

    int child = _nodeCount;
    _nodeCount += 2;
    _nodes[child] = new Node { LeftOrFirst = first, Count = left };
    _nodes[child + 1] = new Node { LeftOrFirst = l, Count = count - left };
    _nodes[nodeIndex].LeftOrFirst = child;
    _nodes[nodeIndex].Count = 0;
    Bounds(child, index, mins, maxs);
    Bounds(child + 1, index, mins, maxs);
    return true;
  }

  private static float Axis(Vector3 value, int axis) => axis == 0 ? value.X : axis == 1 ? value.Y : value.Z;

  private static float Area(Vector3 min, Vector3 max)
  {
    var size = max - min;
    return size.X * size.Y + size.Y * size.Z + size.Z * size.X;
  }

  public bool Blocked(Vector3 from, Vector3 to) => Blocker(from, to) >= 0;

  public readonly record struct Face(Vector3 V0, Vector3 E1, Vector3 E2, bool OneSided);

  public Face FaceOf(int triangle) => _faces[triangle];

  public static bool Hits(in Face face, Vector3 from, Vector3 to) => Hit(face, from, to - from) < 1f;

  public int Blocker(Vector3 from, Vector3 to) => _width == 8 ? Blocker8(from, to) : Blocker4(from, to);

  public float Fraction(Vector3 from, Vector3 to)
  {
    var hint = default(Face);
    return Fraction(from, to, ref hint);
  }

  public float Fraction(Vector3 from, Vector3 to, ref Face hint) => _width == 8 ? Fraction8(from, to, ref hint) : Fraction4(from, to, ref hint);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static Vector3 Inverse(Vector3 d) => new(
    d.X == 0f ? 1e30f : 1f / d.X,
    d.Y == 0f ? 1e30f : 1f / d.Y,
    d.Z == 0f ? 1e30f : 1f / d.Z);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private int Slabs8(int b, Vector256<float> ox, Vector256<float> oy, Vector256<float> oz, Vector256<float> ix, Vector256<float> iy, Vector256<float> iz, Vector256<float> limit)
  {
    var x1 = (Vector256.LoadUnsafe(ref _minX[b]) - ox) * ix;
    var x2 = (Vector256.LoadUnsafe(ref _maxX[b]) - ox) * ix;
    var y1 = (Vector256.LoadUnsafe(ref _minY[b]) - oy) * iy;
    var y2 = (Vector256.LoadUnsafe(ref _maxY[b]) - oy) * iy;
    var z1 = (Vector256.LoadUnsafe(ref _minZ[b]) - oz) * iz;
    var z2 = (Vector256.LoadUnsafe(ref _maxZ[b]) - oz) * iz;
    var enter = Vector256.Max(Vector256.Max(Vector256.Min(x1, x2), Vector256.Min(y1, y2)), Vector256.Max(Vector256.Min(z1, z2), Vector256<float>.Zero));
    var exit = Vector256.Min(Vector256.Min(Vector256.Max(x1, x2), Vector256.Max(y1, y2)), Vector256.Min(Vector256.Max(z1, z2), limit));
    return (int)Vector256.LessThanOrEqual(enter, exit).ExtractMostSignificantBits();
  }

  [MethodImpl(MethodImplOptions.AggressiveOptimization), SkipLocalsInit]
  private int Blocker8(Vector3 from, Vector3 to)
  {
    var direction = to - from;
    var inverse = Inverse(direction);
    var ox = Vector256.Create(from.X); var oy = Vector256.Create(from.Y); var oz = Vector256.Create(from.Z);
    var ix = Vector256.Create(inverse.X); var iy = Vector256.Create(inverse.Y); var iz = Vector256.Create(inverse.Z);
    var limit = Vector256.Create(1f);
    Span<int> stack = stackalloc int[StackSize];
    int top = 0;
    stack[top++] = 0;

    while (top > 0)
    {
      int node = stack[--top];
      int b = node * 8;
      int bits = Slabs8(b, ox, oy, oz, ix, iy, iz, limit) & _valid[node];

      while (bits != 0)
      {
        int slot = b + BitOperations.TrailingZeroCount(bits);
        bits &= bits - 1;
        int count = _count[slot];

        if (count > 0)
        {
          for (int i = _child[slot], end = i + count; i < end; i++)
          {
            if (Hit(_faces[i], from, direction) < 1f)
              return i;
          }
        }
        else if (top < StackSize)
          stack[top++] = _child[slot];
      }
    }

    return -1;
  }

  [MethodImpl(MethodImplOptions.AggressiveOptimization), SkipLocalsInit]
  private float Fraction8(Vector3 from, Vector3 to, ref Face hint)
  {
    var direction = to - from;
    var inverse = Inverse(direction);
    var ox = Vector256.Create(from.X); var oy = Vector256.Create(from.Y); var oz = Vector256.Create(from.Z);
    var ix = Vector256.Create(inverse.X); var iy = Vector256.Create(inverse.Y); var iz = Vector256.Create(inverse.Z);
    float best = Hit(hint, from, direction);
    var limit = Vector256.Create(best);
    int found = -1;
    Span<int> stack = stackalloc int[StackSize];
    int top = 0;
    stack[top++] = 0;

    while (top > 0)
    {
      int node = stack[--top];
      int b = node * 8;
      int bits = Slabs8(b, ox, oy, oz, ix, iy, iz, limit) & _valid[node];

      while (bits != 0)
      {
        int slot = b + BitOperations.TrailingZeroCount(bits);
        bits &= bits - 1;
        int count = _count[slot];

        if (count > 0)
        {
          for (int i = _child[slot], end = i + count; i < end; i++)
          {
            float t = Hit(_faces[i], from, direction);
            if (t < best)
            {
              best = t;
              found = i;
              limit = Vector256.Create(best);
            }
          }
        }
        else if (top < StackSize)
          stack[top++] = _child[slot];
      }
    }

    if (found >= 0)
      hint = _faces[found];

    return best;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private int Slabs4(int b, Vector128<float> ox, Vector128<float> oy, Vector128<float> oz, Vector128<float> ix, Vector128<float> iy, Vector128<float> iz, Vector128<float> limit)
  {
    var x1 = (Vector128.LoadUnsafe(ref _minX[b]) - ox) * ix;
    var x2 = (Vector128.LoadUnsafe(ref _maxX[b]) - ox) * ix;
    var y1 = (Vector128.LoadUnsafe(ref _minY[b]) - oy) * iy;
    var y2 = (Vector128.LoadUnsafe(ref _maxY[b]) - oy) * iy;
    var z1 = (Vector128.LoadUnsafe(ref _minZ[b]) - oz) * iz;
    var z2 = (Vector128.LoadUnsafe(ref _maxZ[b]) - oz) * iz;
    var enter = Vector128.Max(Vector128.Max(Vector128.Min(x1, x2), Vector128.Min(y1, y2)), Vector128.Max(Vector128.Min(z1, z2), Vector128<float>.Zero));
    var exit = Vector128.Min(Vector128.Min(Vector128.Max(x1, x2), Vector128.Max(y1, y2)), Vector128.Min(Vector128.Max(z1, z2), limit));
    return (int)Vector128.LessThanOrEqual(enter, exit).ExtractMostSignificantBits();
  }

  [MethodImpl(MethodImplOptions.AggressiveOptimization), SkipLocalsInit]
  private int Blocker4(Vector3 from, Vector3 to)
  {
    var direction = to - from;
    var inverse = Inverse(direction);
    var ox = Vector128.Create(from.X); var oy = Vector128.Create(from.Y); var oz = Vector128.Create(from.Z);
    var ix = Vector128.Create(inverse.X); var iy = Vector128.Create(inverse.Y); var iz = Vector128.Create(inverse.Z);
    var limit = Vector128.Create(1f);
    Span<int> stack = stackalloc int[StackSize];
    int top = 0;
    stack[top++] = 0;

    while (top > 0)
    {
      int node = stack[--top];
      int b = node * 4;
      int bits = Slabs4(b, ox, oy, oz, ix, iy, iz, limit) & _valid[node];

      while (bits != 0)
      {
        int slot = b + BitOperations.TrailingZeroCount(bits);
        bits &= bits - 1;
        int count = _count[slot];

        if (count > 0)
        {
          for (int i = _child[slot], end = i + count; i < end; i++)
          {
            if (Hit(_faces[i], from, direction) < 1f)
              return i;
          }
        }
        else if (top < StackSize)
          stack[top++] = _child[slot];
      }
    }

    return -1;
  }

  [MethodImpl(MethodImplOptions.AggressiveOptimization), SkipLocalsInit]
  private float Fraction4(Vector3 from, Vector3 to, ref Face hint)
  {
    var direction = to - from;
    var inverse = Inverse(direction);
    var ox = Vector128.Create(from.X); var oy = Vector128.Create(from.Y); var oz = Vector128.Create(from.Z);
    var ix = Vector128.Create(inverse.X); var iy = Vector128.Create(inverse.Y); var iz = Vector128.Create(inverse.Z);
    float best = Hit(hint, from, direction);
    var limit = Vector128.Create(best);
    int found = -1;
    Span<int> stack = stackalloc int[StackSize];
    int top = 0;
    stack[top++] = 0;

    while (top > 0)
    {
      int node = stack[--top];
      int b = node * 4;
      int bits = Slabs4(b, ox, oy, oz, ix, iy, iz, limit) & _valid[node];

      while (bits != 0)
      {
        int slot = b + BitOperations.TrailingZeroCount(bits);
        bits &= bits - 1;
        int count = _count[slot];

        if (count > 0)
        {
          for (int i = _child[slot], end = i + count; i < end; i++)
          {
            float t = Hit(_faces[i], from, direction);
            if (t < best)
            {
              best = t;
              found = i;
              limit = Vector128.Create(best);
            }
          }
        }
        else if (top < StackSize)
          stack[top++] = _child[slot];
      }
    }

    if (found >= 0)
      hint = _faces[found];

    return best;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static float Hit(in Face face, Vector3 origin, Vector3 direction)
  {
    var p = Vector3.Cross(direction, face.E2);
    float det = Vector3.Dot(face.E1, p);
    if (det < 1e-9f && (face.OneSided || det > -1e-9f))
      return 1f;

    float inverse = 1f / det;
    var s = origin - face.V0;
    float u = Vector3.Dot(s, p) * inverse;
    if (u < 0f || u > 1f)
      return 1f;

    var q = Vector3.Cross(s, face.E1);
    float v = Vector3.Dot(direction, q) * inverse;
    if (v < 0f || u + v > 1f)
      return 1f;

    float t = Vector3.Dot(face.E2, q) * inverse;
    return t > 1e-4f && t < 0.9999f ? t : 1f;
  }
}
