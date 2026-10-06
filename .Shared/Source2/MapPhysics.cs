namespace ByDexter.Shared.Source2;

public static class MapPhysics
{
  public static bool ContainsMap(string vpkPath, string mapName)
  {
    try
    {
      using var archive = new VpkArchive(vpkPath);
      return FindEntry(archive, mapName) != null || archive.Contains($"maps/{mapName}.vpk");
    }
    catch
    {
      return false;
    }
  }

  public static byte[]? Read(string vpkPath, string mapName)
  {
    using var archive = new VpkArchive(vpkPath);

    string? entry = FindEntry(archive, mapName);
    if (entry != null)
      return archive.Read(entry);

    byte[]? nested = archive.Read($"maps/{mapName}.vpk");
    if (nested == null)
      return null;

    using var inner = new VpkArchive(nested);
    string? innerEntry = FindEntry(inner, mapName);
    return innerEntry == null ? null : inner.Read(innerEntry);
  }

  public static byte[]? ReadNav(string vpkPath, string mapName)
  {
    try
    {
      using var archive = new VpkArchive(vpkPath);
      byte[]? nav = archive.Read($"maps/{mapName}.nav");
      if (nav != null)
        return nav;

      byte[]? nested = archive.Read($"maps/{mapName}.vpk");
      if (nested == null)
        return null;

      using var inner = new VpkArchive(nested);
      return inner.Read($"maps/{mapName}.nav");
    }
    catch
    {
      return null;
    }
  }

  public static string? FindVpk(string gameDirectory, string mapName) => FindVpks(gameDirectory, mapName).FirstOrDefault();

  public static List<string> FindVpks(string gameDirectory, string mapName)
  {
    var found = new List<string>();
    if (string.IsNullOrEmpty(gameDirectory))
      return found;

    string game = Path.GetFullPath(gameDirectory);

    foreach (string root in new[] { Path.Combine(game, "maps"), Path.Combine(game, "csgo", "maps") })
    {
      if (!Directory.Exists(root))
        continue;

      string direct = Path.Combine(root, $"{mapName}.vpk");
      if (File.Exists(direct))
        Add(found, direct);

      foreach (string file in Directory.GetFiles(root, $"{mapName}.vpk", SearchOption.AllDirectories))
        Add(found, file);
    }

    foreach (string root in AddonRoots(game).Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
    {
      var candidates = Directory.GetFiles(root, "*.vpk", SearchOption.AllDirectories)
        .Where(file => !IsArchiveChunk(file))
        .OrderByDescending(File.GetLastWriteTimeUtc);

      foreach (string candidate in candidates)
      {
        if (!found.Contains(candidate, StringComparer.OrdinalIgnoreCase) && ContainsMap(candidate, mapName))
          found.Add(candidate);
      }
    }

    return found;
  }

  public static string? SourceId(string vpkPath)
  {
    string? directory = Path.GetDirectoryName(Path.GetFullPath(vpkPath));
    string? parent = directory == null ? null : Path.GetDirectoryName(directory);

    if (directory != null && parent != null
        && Path.GetFileName(directory).Equals("maps", StringComparison.OrdinalIgnoreCase)
        && Path.GetFileName(parent).Equals("csgo", StringComparison.OrdinalIgnoreCase))
      return null;

    for (string? current = directory; current != null; current = Path.GetDirectoryName(current))
    {
      string? up = Path.GetDirectoryName(current);
      if (up == null)
        break;

      string name = Path.GetFileName(up);
      if (name == "730" || name.Equals("csgo_addons", StringComparison.OrdinalIgnoreCase)
          || name.Equals("csgo_community_addons", StringComparison.OrdinalIgnoreCase))
        return Path.GetFileName(current);
    }

    return directory == null ? null : Path.GetFileName(directory);
  }

  private static void Add(List<string> found, string file)
  {
    if (!found.Contains(file, StringComparer.OrdinalIgnoreCase))
      found.Add(file);
  }

  private static string? FindEntry(VpkArchive archive, string mapName)
  {
    foreach (string candidate in new[] { $"maps/{mapName}/world_physics.vmdl_c", $"maps/{mapName}/world_physics.vphys_c" })
    {
      if (archive.Contains(candidate))
        return candidate;
    }

    return null;
  }

  private static bool IsArchiveChunk(string file)
  {
    string name = Path.GetFileNameWithoutExtension(file);
    int split = name.LastIndexOf('_');
    return split >= 0 && name.Length - split == 4 && name[(split + 1)..].All(char.IsAsciiDigit);
  }

  private static IEnumerable<string> AddonRoots(string gameDirectory)
  {
    string? current = gameDirectory;

    for (int depth = 0; depth < 5 && current != null; depth++)
    {
      yield return Path.Combine(current, "csgo_addons");
      yield return Path.Combine(current, "csgo_community_addons");
      yield return Path.Combine(current, "steamapps", "workshop", "content", "730");

      string bin = Path.Combine(current, "bin");
      if (Directory.Exists(bin))
      {
        foreach (string platform in Directory.GetDirectories(bin))
          yield return Path.Combine(platform, "steamapps", "workshop", "content", "730");
      }

      current = Path.GetDirectoryName(current);
    }
  }
}
