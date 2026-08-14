using System.Numerics;

namespace ShowPlayerClips.Source2;

public readonly record struct ClipSegment(Vector3 Start, Vector3 End, Vector3 Normal)
{
  public Vector3 Middle => (Start + End) * 0.5f;
}

public static class ClipGeometry
{
  public static string? Categorize(ClipAttribute attribute)
  {
    if (HasTag(attribute.InteractAs, "ladder") || HasTag(attribute.InteractAs, "invisibleladder")
        || HasTag(attribute.InteractAs, "invisibleladder_wood") || HasTag(attribute.InteractAs, "csgo_ladder"))
      return "ladder";

    if (HasTag(attribute.InteractAs, "teleportclip"))
      return "teleportclip";

    if (HasTag(attribute.InteractAs, "navspaceclip"))
      return "navspaceclip";

    if (HasTag(attribute.InteractAs, "navclip"))
      return "navclip";

    if (HasTag(attribute.InteractAs, "controlclip"))
      return "controlclip";

    if (HasTag(attribute.InteractAs, "otherclip"))
      return "otherclip";

    if (HasTag(attribute.InteractAs, "blockbomb") || HasTag(attribute.InteractAs, "csgo_blockbomb"))
      return "blockbomb";

    if (HasTag(attribute.InteractAs, "csgo_grenadeclip") || HasTag(attribute.InteractAs, "grenadeclip"))
      return "grenadeclip";

    bool player = HasTag(attribute.InteractAs, "playerclip");
    bool npc = HasTag(attribute.InteractAs, "npcclip");

    if (player && npc)
      return "clip";
    if (player)
      return "playerclip";
    if (npc)
      return "npcclip";

    if (HasTag(attribute.InteractAs, "blocklos") || HasTag(attribute.InteractAs, "csgo_blocklos"))
      return "blocklos";

    if (HasTag(attribute.InteractAs, "blocklight") || HasTag(attribute.InteractAs, "csgo_blocklight"))
      return "blocklight";

    if (HasTag(attribute.InteractAs, "blocksound"))
      return "blocksound";

    if (HasTag(attribute.InteractAs, "sky"))
      return "sky";

    if (HasTag(attribute.InteractAs, "water"))
      return "water";

    if (HasTag(attribute.InteractAs, "passbullets"))
      return "passbullets";

    if (HasTag(attribute.InteractAs, "trigger") || HasTag(attribute.InteractAs, "csgo_trigger")
        || HasTag(attribute.InteractAs, "trigger_only_player"))
      return "trigger";

    if (attribute.InteractAs.Count == 0)
    {
      if (HasTag(attribute.InteractExclude, "player"))
        return "blockbullets";

      if (HasTag(attribute.InteractExclude, "npc"))
        return "ignorenpc";
    }

    return null;
  }

  private static bool HasTag(List<string> tags, string tag)
  {
    foreach (string value in tags)
    {
      if (string.Equals(value, tag, StringComparison.OrdinalIgnoreCase))
        return true;
    }

    return false;
  }

  public static List<ClipSegment> FromHull(ClipHull hull)
  {
    var segments = new List<ClipSegment>();
    if (hull.Positions.Length == 0 || hull.Edges.Length < 4)
      return segments;

    var centroid = Vector3.Zero;
    foreach (var position in hull.Positions)
      centroid += position;
    centroid /= hull.Positions.Length;

    int edgeCount = hull.Edges.Length / 4;
    var seen = new HashSet<long>();

    for (int i = 0; i < edgeCount; i++)
    {
      int next = hull.Edges[i * 4];
      int origin = hull.Edges[i * 4 + 2];

      if (next >= edgeCount)
        continue;

      int target = hull.Edges[next * 4 + 2];
      if (origin >= hull.Positions.Length || target >= hull.Positions.Length || origin == target)
        continue;

      long key = Key(origin, target);
      if (!seen.Add(key))
        continue;

      var start = hull.Positions[origin];
      var end = hull.Positions[target];
      var normal = Vector3.Normalize((start + end) * 0.5f - centroid);

      if (!float.IsFinite(normal.X) || !float.IsFinite(normal.Y) || !float.IsFinite(normal.Z))
        normal = Vector3.Zero;

      segments.Add(new ClipSegment(start, end, normal));
    }

    return segments;
  }

  public static List<ClipSegment> FromMesh(ClipMesh mesh)
  {
    var segments = new List<ClipSegment>();

    int triangleCount = mesh.Triangles.Length / 3;
    if (triangleCount == 0 || mesh.Vertices.Length == 0)
      return segments;

    var normals = new Vector3[triangleCount];

    for (int i = 0; i < triangleCount; i++)
    {
      int a = mesh.Triangles[i * 3];
      int b = mesh.Triangles[i * 3 + 1];
      int c = mesh.Triangles[i * 3 + 2];

      if (a >= mesh.Vertices.Length || b >= mesh.Vertices.Length || c >= mesh.Vertices.Length)
        continue;

      var normal = Vector3.Cross(mesh.Vertices[b] - mesh.Vertices[a], mesh.Vertices[c] - mesh.Vertices[a]);
      float length = normal.Length();
      normals[i] = length > 0f ? normal / length : Vector3.Zero;
    }

    var edges = new Dictionary<long, (int First, int Second)>(triangleCount * 2);

    for (int i = 0; i < triangleCount; i++)
    {
      int a = mesh.Triangles[i * 3];
      int b = mesh.Triangles[i * 3 + 1];
      int c = mesh.Triangles[i * 3 + 2];

      if (a >= mesh.Vertices.Length || b >= mesh.Vertices.Length || c >= mesh.Vertices.Length)
        continue;

      AddEdge(edges, a, b, i);
      AddEdge(edges, b, c, i);
      AddEdge(edges, c, a, i);
    }

    var kept = new List<(int A, int B, Vector3 Normal)>(edges.Count);

    foreach (var (key, owners) in edges)
    {
      var normal = normals[owners.First];

      if (owners.Second >= 0)
      {
        if (Vector3.Dot(normal, normals[owners.Second]) > 0.999f)
          continue;

        var combined = normal + normals[owners.Second];
        float length = combined.Length();
        normal = length > 0.0001f ? combined / length : normal;
      }

      kept.Add(((int)(key >> 32), (int)(key & 0xFFFFFFFF), normal));
    }

    return MergeCollinear(mesh.Vertices, kept);
  }

  private static void AddEdge(Dictionary<long, (int First, int Second)> edges, int a, int b, int triangle)
  {
    long key = Key(a, b);

    if (edges.TryGetValue(key, out var owners))
      edges[key] = (owners.First, owners.Second < 0 ? triangle : owners.Second);
    else
      edges[key] = (triangle, -1);
  }

  private static List<ClipSegment> MergeCollinear(Vector3[] vertices, List<(int A, int B, Vector3 Normal)> edges)
  {
    var adjacency = new Dictionary<int, List<int>>(edges.Count);

    foreach (var (a, b, _) in edges)
    {
      if (!adjacency.TryGetValue(a, out var listA))
        adjacency[a] = listA = [];
      if (!adjacency.TryGetValue(b, out var listB))
        adjacency[b] = listB = [];

      listA.Add(b);
      listB.Add(a);
    }

    var used = new HashSet<long>(edges.Count);
    var segments = new List<ClipSegment>(edges.Count);

    foreach (var (a, b, normal) in edges)
    {
      long key = Key(a, b);
      if (!used.Add(key))
        continue;

      var direction = Direction(vertices, a, b);

      int end = Extend(vertices, adjacency, used, direction, b, a);
      int start = Extend(vertices, adjacency, used, direction, a, b);

      segments.Add(new ClipSegment(vertices[start], vertices[end], normal));
    }

    return segments;
  }

  private static int Extend(Vector3[] vertices, Dictionary<int, List<int>> adjacency, HashSet<long> used,
    Vector3 direction, int from, int previous)
  {
    int current = from;
    int back = previous;

    while (true)
    {
      if (!adjacency.TryGetValue(current, out var neighbours))
        return current;

      int found = -1;

      foreach (int candidate in neighbours)
      {
        if (candidate == back)
          continue;

        long key = Key(current, candidate);
        if (used.Contains(key))
          continue;

        if (MathF.Abs(Vector3.Dot(Direction(vertices, current, candidate), direction)) < 0.9999f)
          continue;

        used.Add(key);
        found = candidate;
        break;
      }

      if (found < 0)
        return current;

      back = current;
      current = found;
    }
  }

  private static Vector3 Direction(Vector3[] vertices, int a, int b)
  {
    var delta = vertices[b] - vertices[a];
    float length = delta.Length();
    return length > 0f ? delta / length : Vector3.Zero;
  }

  private static long Key(int a, int b) => a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
}
