using System.Numerics;
using System.Runtime.CompilerServices;

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

  private Face[] _faces = [];
  private Node[] _nodes = [];
  private int _nodeCount;

  public int TriangleCount => _faces.Length;
  public Vector3 Min => _nodes[0].Min;
  public Vector3 Max => _nodes[0].Max;

  private static readonly string[] PlayerBlockers = ["playerclip", "passbullets", "window", "solid"];

  public static WorldBvh Solid(byte[] physics) => Build(Extract(physics, attribute => attribute.InteractAs.Count == 0, false));

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

    var wanted = new HashSet<int>();
    for (int i = 0; i < attributes.Attributes.Count; i++)
    {
      if (filter(attributes.Attributes[i]))
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

    Array.Resize(ref bvh._nodes, bvh._nodeCount);
    return bvh;
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

  [MethodImpl(MethodImplOptions.AggressiveOptimization)]
  public int Blocker(Vector3 from, Vector3 to)
  {
    var direction = to - from;
    var inverse = new Vector3(1f / direction.X, 1f / direction.Y, 1f / direction.Z);
    Span<int> stack = stackalloc int[64];
    int top = 0;
    stack[top++] = 0;

    while (top > 0)
    {
      ref var node = ref _nodes[stack[--top]];
      if (!Overlaps(node.Min, node.Max, from, inverse))
        continue;

      if (node.Count > 0)
      {
        for (int i = node.LeftOrFirst; i < node.LeftOrFirst + node.Count; i++)
        {
          if (Hit(_faces[i], from, direction) < 1f)
            return i;
        }
      }
      else if (top < 62)
      {
        stack[top++] = node.LeftOrFirst;
        stack[top++] = node.LeftOrFirst + 1;
      }
    }

    return -1;
  }

  public float Fraction(Vector3 from, Vector3 to)
  {
    var hint = default(Face);
    return Fraction(from, to, ref hint);
  }

  [MethodImpl(MethodImplOptions.AggressiveOptimization)]
  public float Fraction(Vector3 from, Vector3 to, ref Face hint)
  {
    var direction = to - from;
    var inverse = new Vector3(1f / direction.X, 1f / direction.Y, 1f / direction.Z);
    Span<int> stack = stackalloc int[64];
    int top = 0;
    stack[top++] = 0;
    float best = Hit(hint, from, direction);
    int found = -1;

    while (top > 0)
    {
      ref var node = ref _nodes[stack[--top]];
      if (!Overlaps(node.Min, node.Max, from, inverse, best))
        continue;

      if (node.Count > 0)
      {
        for (int i = node.LeftOrFirst; i < node.LeftOrFirst + node.Count; i++)
        {
          float t = Hit(_faces[i], from, direction);
          if (t < best)
          {
            best = t;
            found = i;
          }
        }
      }
      else if (top < 62)
      {
        stack[top++] = node.LeftOrFirst;
        stack[top++] = node.LeftOrFirst + 1;
      }
    }

    if (found >= 0)
      hint = _faces[found];

    return best;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static bool Overlaps(Vector3 min, Vector3 max, Vector3 origin, Vector3 inverse, float limit = 1f)
  {
    var t0 = (min - origin) * inverse;
    var t1 = (max - origin) * inverse;
    var near = Vector3.Min(t0, t1);
    var far = Vector3.Max(t0, t1);
    float enter = MathF.Max(MathF.Max(near.X, near.Y), MathF.Max(near.Z, 0f));
    float exit = MathF.Min(MathF.Min(far.X, far.Y), MathF.Min(far.Z, limit));
    return enter <= exit;
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
