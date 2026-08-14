using System.Drawing;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;
using ByDexter.Shared;
using ShowPlayerClips.Source2;

namespace ShowPlayerClips;

public class ShowPlayerClipsConfig : BasePluginConfig
{
  [JsonPropertyName("showclips_cmd")]
  public string Commands { get; set; } = "css_showclips,css_clips";

  [JsonPropertyName("showclips_flag")]
  public string Flag { get; set; } = "@css/generic";

  [JsonPropertyName("showclips_types")]
  public string Types { get; set; } = "clip,playerclip,trigger,ladder";

  [JsonPropertyName("showclips_colors")]
  public Dictionary<string, string> Colors { get; set; } = new()
  {
    ["clip"] = "#CD3920",
    ["playerclip"] = "#C00078",
    ["npcclip"] = "#8820CD",
    ["grenadeclip"] = "#B6FC16",
    ["ladder"] = "#F84A00",
    ["blockbullets"] = "#F88005",
    ["passbullets"] = "#25B9F5",
    ["blocklos"] = "#0000F8",
    ["blocksound"] = "#B5E51E",
    ["blocklight"] = "#95C04A",
    ["sky"] = "#B2E1FD",
    ["water"] = "#00E8CA",
    ["navclip"] = "#C508A7",
    ["navspaceclip"] = "#527097",
    ["teleportclip"] = "#2E9DA6",
    ["controlclip"] = "#CD20A8",
    ["otherclip"] = "#7821D3",
    ["blockbomb"] = "#31D3AE",
    ["trigger"] = "#F89A00",
    ["ignorenpc"] = "#BA6D9C",
  };

  [JsonPropertyName("showclips_radius")]
  public float Radius { get; set; } = 4096f;

  [JsonPropertyName("showclips_max_beams")]
  public int MaxBeams { get; set; } = 1000;

  [JsonPropertyName("showclips_width")]
  public float Width { get; set; } = 0.5f;

  [JsonPropertyName("showclips_offset")]
  public float Offset { get; set; } = 1f;

  [JsonPropertyName("showclips_refresh")]
  public float Refresh { get; set; } = 0.4f;

  [JsonPropertyName("showclips_move_step")]
  public float MoveStep { get; set; } = 24f;
}

public class ShowPlayerClips : BasePlugin, IPluginConfig<ShowPlayerClipsConfig>
{
  public override string ModuleName => "ShowPlayerClips";
  public override string ModuleVersion => "1.1.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public ShowPlayerClipsConfig Config { get; set; } = new();

  private const int MaxSlots = 64;
  private const string BeamSprite = "materials/sprites/laserbeam.vmat";
  private const string BeamMarker = "showplayerclips_beam";
  private const float BoundsSize = 16384f;

  private static readonly string[] TriggerClasses =
  [
    "trigger_teleport",
    "trigger_push",
    "trigger_hurt",
    "trigger_multiple",
    "trigger_once",
    "trigger_gravity",
    "trigger_look",
    "trigger_proximity",
    "trigger_soundscape",
    "trigger_physics_trap",
    "trigger_bomb_reset",
    "func_buyzone",
    "func_bomb_target",
    "func_hostage_rescue",
  ];

  private readonly bool[] _enabled = new bool[MaxSlots];
  private readonly System.Numerics.Vector3[] _lastViewerOrigin = new System.Numerics.Vector3[MaxSlots];
  private readonly bool[] _hadViewer = new bool[MaxSlots];

  private readonly List<CEnvBeam> _beams = [];
  private readonly List<int> _beamSegment = [];
  private readonly List<int> _beamCategory = [];
  private readonly List<bool> _beamFlipped = [];

  private readonly System.Numerics.Vector3[] _viewerPositions = new System.Numerics.Vector3[MaxSlots];
  private int _viewerCount;

  private readonly List<int> _candidates = [];
  private readonly HashSet<int> _wantedSegments = [];
  private readonly HashSet<int> _keptSegments = [];
  private readonly List<int> _freeSlots = [];

  private readonly List<ClipSegment> _triggerSegments = [];
  private int _triggerCount;
  private ClipMap? _mapClips;
  private ClipSegment[] _segments = [];
  private int[] _segmentCategory = [];
  private float[] _candidateDistance = [];
  private string[] _categoryNames = [];
  private Color[] _categoryColors = [];

  private string _mapName = string.Empty;
  private string _status = string.Empty;
  private bool _loading;
  private bool _forceRefresh;
  private float _nextRefresh;

  public void OnConfigParsed(ShowPlayerClipsConfig config)
  {
    if (config.Radius < 128f)
      config.Radius = 128f;
    if (config.MaxBeams < 16)
      config.MaxBeams = 16;
    if (config.MaxBeams > 4096)
      config.MaxBeams = 4096;
    if (config.Width < 0.1f)
      config.Width = 0.1f;
    if (config.Offset < 0f)
      config.Offset = 0f;
    if (config.Refresh < 0.1f)
      config.Refresh = 0.1f;
    if (config.MoveStep < 0f)
      config.MoveStep = 0f;

    Config = config;
  }

  public override void Load(bool hotReload)
  {
    foreach (string name in Util.Split(Config.Commands))
      AddCommand(name, "Show player clip brushes", OnToggleCommand);

    RegisterListener<OnMapStart>(OnMapStart);
    RegisterListener<OnMapEnd>(OnMapEnd);
    RegisterListener<OnTick>(OnTick);
    RegisterListener<CheckTransmit>(OnCheckTransmit);

    RegisterEventHandler<EventRoundStart>((_, _) =>
    {
      RefreshTriggers();
      Rebuild();
      return HookResult.Continue;
    });

    RegisterListener<OnClientDisconnect>(slot =>
    {
      if (slot >= 0 && slot < MaxSlots)
      {
        _enabled[slot] = false;
        _hadViewer[slot] = false;
        _forceRefresh = true;
      }
    });

    if (hotReload)
    {
      RemoveOrphanBeams();
      OnMapStart(Server.MapName);
    }
  }

  public override void Unload(bool hotReload)
  {
    RemoveAllBeams();
    RemoveOrphanBeams();
  }

  private void OnMapStart(string mapName)
  {
    _mapName = mapName;
    _mapClips = null;
    _triggerSegments.Clear();
    _segments = [];
    _segmentCategory = [];
    _categoryNames = [];
    _categoryColors = [];
    _candidateDistance = [];
    _status = string.Empty;
    _loading = true;
    _forceRefresh = true;

    Array.Clear(_enabled);
    Array.Clear(_hadViewer);
    RemoveAllBeams();

    AddTimer(3f, () =>
    {
      RefreshTriggers();
      Rebuild();
    });

    string moduleDirectory = ModuleDirectory;
    string gameDirectory = Server.GameDirectory;
    string[] types = Util.Split(Config.Types);

    Task.Run(() =>
    {
      try
      {
        var map = LoadOrExtract(gameDirectory, moduleDirectory, mapName, types, out string info);
        Server.NextFrame(() => Publish(map, info));
      }
      catch (Exception ex)
      {
        Server.NextFrame(() => Publish(null, ex.Message));
      }
    });
  }

  private void OnMapEnd()
  {
    RemoveAllBeams();
    RemoveOrphanBeams();
    _segments = [];
    _segmentCategory = [];
  }

  private void Publish(ClipMap? map, string info)
  {
    _loading = false;
    _status = info;
    _mapClips = map;

    if (map == null)
    {
      Console.WriteLine($"[ShowPlayerClips] {info}");
      Rebuild();
      return;
    }

    Rebuild();

    string drawn = string.Join(", ", map.Categories.Select(pair => $"{pair.Key}={pair.Value.Count}"));
    Console.WriteLine($"[ShowPlayerClips] {_mapName}: {_segments.Length} cizgi ({info}) [{drawn}]. Haritada bulunan turler: {string.Join(", ", map.Available)}");
  }

  private void Rebuild()
  {
    var wanted = new HashSet<string>(Util.Split(Config.Types), StringComparer.OrdinalIgnoreCase);
    var names = new List<string>();
    var colors = new List<Color>();
    var segments = new List<ClipSegment>();
    var categories = new List<int>();

    void AddCategory(string category, IReadOnlyList<ClipSegment> list)
    {
      if (list.Count == 0 || !wanted.Contains(category))
        return;

      int index = names.Count;
      names.Add(category);
      colors.Add(Config.Colors.TryGetValue(category, out string? value)
        ? Util.ParseColor(value, Color.White)
        : Color.White);

      foreach (var segment in list)
      {
        segments.Add(segment);
        categories.Add(index);
      }
    }

    if (_mapClips != null)
    {
      foreach (var (category, list) in _mapClips.Categories)
        AddCategory(category, list);
    }

    AddCategory("trigger", _triggerSegments);

    _categoryNames = [.. names];
    _categoryColors = [.. colors];
    _segments = [.. segments];
    _segmentCategory = [.. categories];
    _candidateDistance = new float[_segments.Length];
    _forceRefresh = true;
  }

  private void RefreshTriggers()
  {
    _triggerSegments.Clear();

    int found = 0;

    foreach (string designerName in TriggerClasses)
    {
      foreach (var entity in Utilities.FindAllEntitiesByDesignerName<CBaseTrigger>(designerName))
      {
        if (!entity.IsValid)
          continue;

        var origin = entity.AbsOrigin;
        var mins = entity.Collision?.Mins;
        var maxs = entity.Collision?.Maxs;

        if (origin == null || mins == null || maxs == null)
          continue;

        var low = new System.Numerics.Vector3(origin.X + mins.X, origin.Y + mins.Y, origin.Z + mins.Z);
        var high = new System.Numerics.Vector3(origin.X + maxs.X, origin.Y + maxs.Y, origin.Z + maxs.Z);

        if (System.Numerics.Vector3.Distance(low, high) < 1f)
          continue;

        AddBox(_triggerSegments, low, high);
        found++;
      }
    }

    _triggerCount = found;

    if (_mapClips != null && found > 0)
      Console.WriteLine($"[ShowPlayerClips] {_mapName}: {found} trigger.");
  }

  private static void AddBox(List<ClipSegment> target, System.Numerics.Vector3 low, System.Numerics.Vector3 high)
  {
    var center = (low + high) * 0.5f;

    Span<System.Numerics.Vector3> corners =
    [
      new(low.X, low.Y, low.Z),
      new(high.X, low.Y, low.Z),
      new(high.X, high.Y, low.Z),
      new(low.X, high.Y, low.Z),
      new(low.X, low.Y, high.Z),
      new(high.X, low.Y, high.Z),
      new(high.X, high.Y, high.Z),
      new(low.X, high.Y, high.Z),
    ];

    ReadOnlySpan<int> edges =
    [
      0, 1, 1, 2, 2, 3, 3, 0,
      4, 5, 5, 6, 6, 7, 7, 4,
      0, 4, 1, 5, 2, 6, 3, 7,
    ];

    for (int i = 0; i < edges.Length; i += 2)
    {
      var start = corners[edges[i]];
      var end = corners[edges[i + 1]];
      var middle = (start + end) * 0.5f;
      var normal = middle - center;

      float length = normal.Length();
      normal = length > 0.001f ? normal / length : System.Numerics.Vector3.Zero;

      target.Add(new ClipSegment(start, end, normal));
    }
  }

  private static ClipMap? LoadOrExtract(string gameDirectory, string moduleDirectory, string mapName, string[] types, out string info)
  {
    string? vpk = FindMapVpk(gameDirectory, mapName);

    if (vpk == null)
    {
      info = $"'{mapName}.vpk' bulunamadi.";
      return null;
    }

    var file = new FileInfo(vpk);
    string stamp = $"{file.Length:x}_{file.LastWriteTimeUtc.Ticks:x}_{TypeKey(types)}";
    string cachePath = Path.Combine(moduleDirectory, "cache", $"{mapName}_{stamp}.spc");

    var cached = ClipMap.Load(cachePath);
    if (cached != null)
    {
      info = "onbellek";
      return cached;
    }

    var map = ClipMap.Extract(vpk, mapName, types);

    try
    {
      CleanCache(Path.Combine(moduleDirectory, "cache"), mapName);
      map.Save(cachePath);
    }
    catch
    {
    }

    info = "vpk";
    return map;
  }

  private static string TypeKey(string[] types)
  {
    var sorted = types.Select(type => type.ToLowerInvariant()).OrderBy(type => type, StringComparer.Ordinal);

    uint hash = 2166136261;
    foreach (char c in string.Join(',', sorted))
    {
      hash ^= c;
      hash *= 16777619;
    }

    return hash.ToString("x8");
  }

  private static void CleanCache(string directory, string mapName)
  {
    if (!Directory.Exists(directory))
      return;

    foreach (string file in Directory.GetFiles(directory, $"{mapName}_*.spc"))
      File.Delete(file);
  }

  private static string? FindMapVpk(string gameDirectory, string mapName)
  {
    if (string.IsNullOrEmpty(gameDirectory))
      return null;

    string game = Path.GetFullPath(gameDirectory);

    var mapRoots = new List<string>
    {
      Path.Combine(game, "maps"),
      Path.Combine(game, "csgo", "maps"),
    };

    foreach (string root in mapRoots)
    {
      if (!Directory.Exists(root))
        continue;

      string direct = Path.Combine(root, $"{mapName}.vpk");
      if (File.Exists(direct))
        return direct;

      var found = Directory.GetFiles(root, $"{mapName}.vpk", SearchOption.AllDirectories);
      if (found.Length > 0)
        return found[0];
    }

    foreach (string root in AddonRoots(game))
    {
      if (!Directory.Exists(root))
        continue;

      foreach (string candidate in Directory.GetFiles(root, "*.vpk", SearchOption.AllDirectories))
      {
        if (ClipMap.ContainsMap(candidate, mapName))
          return candidate;
      }
    }

    return null;
  }

  private static IEnumerable<string> AddonRoots(string gameDirectory)
  {
    string? current = gameDirectory;

    for (int depth = 0; depth < 5 && current != null; depth++)
    {
      yield return Path.Combine(current, "csgo_addons");
      yield return Path.Combine(current, "csgo_community_addons");
      yield return Path.Combine(current, "steamapps", "workshop", "content", "730");

      current = Path.GetDirectoryName(current);
    }
  }

  private void OnToggleCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    if (!Util.HasAccess(player, Config.Flag))
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["showclips.no_access"]}");
      return;
    }

    if (_loading)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["showclips.loading"]}");
      return;
    }

    if (_segments.Length == 0)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["showclips.unavailable", _status]}");
      return;
    }

    _enabled[player.Slot] = !_enabled[player.Slot];
    _hadViewer[player.Slot] = false;
    _forceRefresh = true;

    if (_enabled[player.Slot])
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["showclips.enabled", _segments.Length, string.Join(", ", _categoryNames)]}");
    else
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["showclips.disabled"]}");
  }

  private void OnTick()
  {
    if (_segments.Length == 0)
      return;

    float now = Server.CurrentTime;
    if (now < _nextRefresh)
      return;

    _nextRefresh = now + Config.Refresh;

    Refresh();
  }

  private void Refresh()
  {
    var viewers = _viewerPositions;
    int viewerCount = 0;
    bool changed = _forceRefresh;

    for (int slot = 0; slot < MaxSlots; slot++)
    {
      if (!_enabled[slot])
      {
        if (_hadViewer[slot])
        {
          _hadViewer[slot] = false;
          changed = true;
        }

        continue;
      }

      var player = Utilities.GetPlayerFromSlot(slot);
      var origin = ViewerOrigin(player);

      if (origin == null)
      {
        if (_hadViewer[slot])
        {
          _hadViewer[slot] = false;
          changed = true;
        }

        continue;
      }

      var position = new System.Numerics.Vector3(origin.X, origin.Y, origin.Z);
      viewers[viewerCount++] = position;

      if (!_hadViewer[slot] || System.Numerics.Vector3.Distance(_lastViewerOrigin[slot], position) > Config.MoveStep)
      {
        _lastViewerOrigin[slot] = position;
        _hadViewer[slot] = true;
        changed = true;
      }
    }

    _forceRefresh = false;
    _viewerCount = viewerCount;

    if (viewerCount == 0)
    {
      if (_beams.Count > 0)
      {
        RemoveAllBeams();
        RemoveOrphanBeams();
      }

      return;
    }

    if (!changed)
      return;

    _candidates.Clear();
    float radiusSquared = Config.Radius * Config.Radius;

    for (int i = 0; i < _segments.Length; i++)
    {
      var middle = _segments[i].Middle;
      float best = float.MaxValue;

      for (int v = 0; v < viewerCount; v++)
      {
        float distance = System.Numerics.Vector3.DistanceSquared(middle, viewers[v]);
        if (distance < best)
          best = distance;
      }

      if (best > radiusSquared)
        continue;

      _candidateDistance[i] = best;
      _candidates.Add(i);
    }

    if (_candidates.Count > Config.MaxBeams)
    {
      _candidates.Sort((a, b) => _candidateDistance[a].CompareTo(_candidateDistance[b]));
      _candidates.RemoveRange(Config.MaxBeams, _candidates.Count - Config.MaxBeams);
    }

    Apply();
  }

  private void Apply()
  {
    _wantedSegments.Clear();
    foreach (int index in _candidates)
      _wantedSegments.Add(index);

    _keptSegments.Clear();
    _freeSlots.Clear();

    for (int slot = 0; slot < _beams.Count; slot++)
    {
      int segment = _beamSegment[slot];

      if (_beams[slot].IsValid && segment >= 0 && _wantedSegments.Contains(segment))
      {
        _keptSegments.Add(segment);

        if (_beamFlipped[slot] != ShouldFlip(segment))
          ApplySegment(slot, segment);

        continue;
      }

      _beamSegment[slot] = -1;
      _freeSlots.Add(slot);
    }

    int nextFree = 0;

    foreach (int index in _candidates)
    {
      if (_keptSegments.Contains(index))
        continue;

      if (nextFree < _freeSlots.Count)
      {
        ApplySegment(_freeSlots[nextFree++], index);
        continue;
      }

      var beam = CreateBeam(index);
      if (beam == null)
        break;

      _beams.Add(beam);
      _beamSegment.Add(index);
      _beamCategory.Add(_segmentCategory[index]);
      _beamFlipped.Add(ShouldFlip(index));
    }

    for (int i = _freeSlots.Count - 1; i >= nextFree; i--)
      RemoveSlot(_freeSlots[i]);
  }

  private void ApplySegment(int slot, int index)
  {
    var beam = _beams[slot];
    int category = _segmentCategory[index];

    if (!beam.IsValid)
    {
      var created = CreateBeam(index);
      if (created == null)
        return;

      _beams[slot] = created;
      _beamSegment[slot] = index;
      _beamCategory[slot] = category;
      _beamFlipped[slot] = ShouldFlip(index);
      return;
    }

    if (_beamCategory[slot] != category)
    {
      beam.Render = _categoryColors[category];
      Utilities.SetStateChanged(beam, "CBaseModelEntity", "m_clrRender");
      _beamCategory[slot] = category;
    }

    bool flipped = ShouldFlip(index);
    var (start, end) = Endpoints(index, flipped);

    beam.Teleport(new Vector(start.X, start.Y, start.Z), new QAngle(), new Vector());
    beam.EndPos.X = end.X;
    beam.EndPos.Y = end.Y;
    beam.EndPos.Z = end.Z;
    Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");

    _beamSegment[slot] = index;
    _beamFlipped[slot] = flipped;
  }

  private static Vector? ViewerOrigin(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid)
      return null;

    var pawn = player.PlayerPawn.Value;
    if (pawn != null && pawn.IsValid && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE && pawn.AbsOrigin != null)
      return pawn.AbsOrigin;

    var observer = player.Pawn.Value;

    var target = observer?.ObserverServices?.ObserverTarget.Value?.As<CBaseEntity>();
    if (target != null && target.IsValid && target.AbsOrigin != null)
      return target.AbsOrigin;

    if (observer != null && observer.IsValid && observer.AbsOrigin != null)
      return observer.AbsOrigin;

    return null;
  }

  private bool ShouldFlip(int index)
  {
    var segment = _segments[index];
    if (segment.Normal == System.Numerics.Vector3.Zero || _viewerCount == 0)
      return false;

    var middle = segment.Middle;
    int nearest = 0;
    float best = float.MaxValue;

    for (int i = 0; i < _viewerCount; i++)
    {
      float distance = System.Numerics.Vector3.DistanceSquared(middle, _viewerPositions[i]);
      if (distance < best)
      {
        best = distance;
        nearest = i;
      }
    }

    return System.Numerics.Vector3.Dot(segment.Normal, _viewerPositions[nearest] - middle) < 0f;
  }

  private (System.Numerics.Vector3 Start, System.Numerics.Vector3 End) Endpoints(int index, bool flipped)
  {
    var segment = _segments[index];
    var offset = segment.Normal * (flipped ? -Config.Offset : Config.Offset);
    return (segment.Start + offset, segment.End + offset);
  }

  private CEnvBeam? CreateBeam(int index)
  {
    var beam = Utilities.CreateEntityByName<CEnvBeam>("env_beam");
    if (beam == null || !beam.IsValid)
      return null;

    var (start, end) = Endpoints(index, ShouldFlip(index));

    beam.Globalname = BeamMarker;

    if (beam.Entity != null)
      beam.Entity.Name = BeamMarker;

    beam.DispatchSpawn();
    beam.AcceptInput("TurnOn");

    beam.SetModel(BeamSprite);
    ExpandBounds(beam);
    beam.Width = Config.Width;
    Utilities.SetStateChanged(beam, "CBeam", "m_fWidth");
    beam.Render = _categoryColors[_segmentCategory[index]];
    Utilities.SetStateChanged(beam, "CBaseModelEntity", "m_clrRender");
    beam.Teleport(new Vector(start.X, start.Y, start.Z), new QAngle(), new Vector());
    beam.EndPos.X = end.X;
    beam.EndPos.Y = end.Y;
    beam.EndPos.Z = end.Z;
    Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");

    return beam;
  }

  private void RemoveSlot(int slot)
  {
    var beam = _beams[slot];
    if (beam.IsValid)
      beam.Remove();

    _beams.RemoveAt(slot);
    _beamSegment.RemoveAt(slot);
    _beamCategory.RemoveAt(slot);
    _beamFlipped.RemoveAt(slot);
  }

  private static void ExpandBounds(CEnvBeam beam)
  {
    try
    {
      var collision = beam.Collision;
      if (collision == null)
        return;

      collision.SurroundingMins.X = -BoundsSize;
      collision.SurroundingMins.Y = -BoundsSize;
      collision.SurroundingMins.Z = -BoundsSize;
      collision.SurroundingMaxs.X = BoundsSize;
      collision.SurroundingMaxs.Y = BoundsSize;
      collision.SurroundingMaxs.Z = BoundsSize;

      collision.SpecifiedSurroundingMins.X = -BoundsSize;
      collision.SpecifiedSurroundingMins.Y = -BoundsSize;
      collision.SpecifiedSurroundingMins.Z = -BoundsSize;
      collision.SpecifiedSurroundingMaxs.X = BoundsSize;
      collision.SpecifiedSurroundingMaxs.Y = BoundsSize;
      collision.SpecifiedSurroundingMaxs.Z = BoundsSize;
    }
    catch
    {
    }
  }

  private static int RemoveOrphanBeams()
  {
    int removed = 0;

    try
    {
      foreach (var beam in Utilities.FindAllEntitiesByDesignerName<CEnvBeam>("env_beam"))
      {
        try
        {
          if (!beam.IsValid || (beam.Globalname != BeamMarker && beam.Entity?.Name != BeamMarker))
            continue;

          beam.Remove();
          removed++;
        }
        catch
        {
        }
      }
    }
    catch
    {
    }

    return removed;
  }

  private void RemoveAllBeams()
  {
    foreach (var beam in _beams)
    {
      try
      {
        if (beam.IsValid)
          beam.Remove();
      }
      catch
      {
      }
    }

    _beams.Clear();
    _beamSegment.Clear();
    _beamCategory.Clear();
    _beamFlipped.Clear();
  }

  private void OnCheckTransmit(CCheckTransmitInfoList infoList)
  {
    if (_beams.Count == 0)
      return;

    foreach ((CCheckTransmitInfo info, CCSPlayerController? viewer) in infoList)
    {
      if (viewer == null || !viewer.IsValid)
        continue;

      if (!viewer.IsHLTV && _enabled[viewer.Slot])
        continue;

      foreach (var beam in _beams)
      {
        if (beam.IsValid)
          info.TransmitEntities.Remove(beam);
      }
    }
  }
}
