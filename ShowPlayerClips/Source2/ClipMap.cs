using System.Numerics;
using System.Text;

namespace ShowPlayerClips.Source2;

public sealed class ClipMap
{
  private const uint CacheMagic = 0x43435053;
  private const int CacheVersion = 3;

  public Dictionary<string, List<ClipSegment>> Categories { get; } = new(StringComparer.OrdinalIgnoreCase);

  public List<string> Available { get; private set; } = [];

  public int SegmentCount
  {
    get
    {
      int total = 0;
      foreach (var list in Categories.Values)
        total += list.Count;
      return total;
    }
  }

  public static bool ContainsMap(string vpkPath, string mapName)
  {
    try
    {
      using var archive = new VpkArchive(vpkPath);
      return FindPhysicsEntry(archive, mapName) != null || archive.Contains($"maps/{mapName}.vpk");
    }
    catch
    {
      return false;
    }
  }

  private static string? FindPhysicsEntry(VpkArchive archive, string mapName)
  {
    string[] candidates =
    [
      $"maps/{mapName}/world_physics.vmdl_c",
      $"maps/{mapName}/world_physics.vphys_c",
    ];

    foreach (string candidate in candidates)
    {
      if (archive.Contains(candidate))
        return candidate;
    }

    return archive.Find("world_physics.vmdl_c") ?? archive.Find("world_physics.vphys_c");
  }

  private static byte[]? ReadPhysics(string vpkPath, string mapName)
  {
    using var archive = new VpkArchive(vpkPath);

    string? entry = FindPhysicsEntry(archive, mapName);
    if (entry != null)
      return archive.Read(entry);

    byte[]? nested = archive.Read($"maps/{mapName}.vpk");
    if (nested == null)
      return null;

    using var inner = new VpkArchive(nested);
    string? innerEntry = FindPhysicsEntry(inner, mapName);
    return innerEntry == null ? null : inner.Read(innerEntry);
  }

  public static ClipMap Extract(string vpkPath, string mapName, IEnumerable<string>? wantedCategories = null, int maxTriangles = 400000)
  {
    var wanted = wantedCategories == null
      ? null
      : new HashSet<string>(wantedCategories, StringComparer.OrdinalIgnoreCase);

    byte[]? resource = ReadPhysics(vpkPath, mapName);
    if (resource == null)
      throw new InvalidDataException($"'{Path.GetFileName(vpkPath)}' icinde world_physics bulunamadi.");

    var block = Source2Resource.FindBlock(resource, "PHYS") ?? Source2Resource.FindBlock(resource, "DATA");
    if (block == null)
      throw new InvalidDataException($"'{mapName}' world_physics icinde PHYS blogu yok.");

    var document = Kv3Binary.Load(resource, block.Value.Offset, block.Value.Size);

    var attributeScanner = new AttributeScanner();
    document.Walk(attributeScanner);

    var categoryByIndex = new Dictionary<int, string>();
    var available = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

    for (int i = 0; i < attributeScanner.Attributes.Count; i++)
    {
      string? category = ClipGeometry.Categorize(attributeScanner.Attributes[i]);
      if (category == null)
        continue;

      available.Add(category);

      if (wanted == null || wanted.Contains(category))
        categoryByIndex[i] = category;
    }

    var geometryScanner = new GeometryScanner([.. categoryByIndex.Keys], maxTriangles);
    document.Walk(geometryScanner);

    var map = new ClipMap { Available = [.. available] };

    foreach (var hull in geometryScanner.Hulls)
    {
      if (!categoryByIndex.TryGetValue(hull.AttributeIndex, out string? category))
        continue;

      map.Add(category, ClipGeometry.FromHull(hull));
    }

    foreach (var mesh in geometryScanner.Meshes)
    {
      if (!categoryByIndex.TryGetValue(mesh.AttributeIndex, out string? category))
        continue;

      map.Add(category, ClipGeometry.FromMesh(mesh));
    }

    return map;
  }

  private void Add(string category, List<ClipSegment> segments)
  {
    if (segments.Count == 0)
      return;

    if (!Categories.TryGetValue(category, out var list))
      Categories[category] = list = [];

    list.AddRange(segments);
  }

  public void Save(string path)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);

    using var stream = File.Create(path);
    using var writer = new BinaryWriter(stream, Encoding.UTF8);

    writer.Write(CacheMagic);
    writer.Write(CacheVersion);

    writer.Write(Available.Count);
    foreach (string category in Available)
      writer.Write(category);

    writer.Write(Categories.Count);

    foreach (var (category, segments) in Categories)
    {
      writer.Write(category);
      writer.Write(segments.Count);

      foreach (var segment in segments)
      {
        writer.Write(segment.Start.X);
        writer.Write(segment.Start.Y);
        writer.Write(segment.Start.Z);
        writer.Write(segment.End.X);
        writer.Write(segment.End.Y);
        writer.Write(segment.End.Z);
        writer.Write(segment.Normal.X);
        writer.Write(segment.Normal.Y);
        writer.Write(segment.Normal.Z);
      }
    }
  }

  public static ClipMap? Load(string path)
  {
    if (!File.Exists(path))
      return null;

    try
    {
      using var stream = File.OpenRead(path);
      using var reader = new BinaryReader(stream, Encoding.UTF8);

      if (reader.ReadUInt32() != CacheMagic || reader.ReadInt32() != CacheVersion)
        return null;

      var map = new ClipMap();

      int availableCount = reader.ReadInt32();
      var available = new List<string>(availableCount);
      for (int i = 0; i < availableCount; i++)
        available.Add(reader.ReadString());
      map.Available = available;

      int categoryCount = reader.ReadInt32();

      for (int i = 0; i < categoryCount; i++)
      {
        string category = reader.ReadString();
        int count = reader.ReadInt32();
        var segments = new List<ClipSegment>(count);

        for (int j = 0; j < count; j++)
        {
          var start = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
          var end = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
          var normal = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
          segments.Add(new ClipSegment(start, end, normal));
        }

        map.Categories[category] = segments;
      }

      return map;
    }
    catch
    {
      return null;
    }
  }
}
