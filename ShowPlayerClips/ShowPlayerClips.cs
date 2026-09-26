using System.Drawing;
using System.Numerics;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
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
  public override string ModuleVersion => "1.1.1";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public ShowPlayerClipsConfig Config { get; set; } = new();

  private const int MaxSlots = 64;
  private const string BeamSprite = "materials/sprites/laserbeam.vmat";
  private const string BeamMarker = "showplayerclips_beam";
  private const float BoundsSize = 16384f;

  private static readonly HashSet<string> TriggerClasses = new(StringComparer.Ordinal)
  {
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
  };

  private readonly HashSet<int> _viewers = [];
  private readonly Dictionary<int, Vector3> _lastViewerOrigin = [];
  private readonly List<int> _staleViewers = [];
  private readonly Vector3[] _viewerPositions = new Vector3[MaxSlots];
  private int _viewerCount;

  private readonly List<CEnvBeam> _beams = [];
  private readonly List<int> _beamSegment = [];
  private readonly List<int> _beamCategory = [];
  private readonly List<bool> _beamFlipped = [];
  private readonly List<int> _freeSlots = [];
  private readonly List<uint> _transmitIndices = [];

  private List<ClipSegment> _triggerSegments = [];
  private List<ClipSegment> _pendingTriggers = [];
  private int _triggerCount;
  private ClipMap? _mapClips;
  private ClipSegment[] _segments = [];
  private int[] _segmentCategory = [];
  private string[] _categoryNames = [];
  private Color[] _categoryColors = [];

  private int[] _candidates = [];
  private float[] _candidateDistance = [];
  private int[] _candidateViewer = [];
  private int[] _wantedStamp = [];
  private int[] _keptStamp = [];
  private int _candidateCount;
  private int _stamp;

  private string _mapName = string.Empty;
  private string _status = string.Empty;
  private bool _loading;
  private bool _forceRefresh;
  private float _nextRefresh;
  private int _generation;

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

    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

    if (hotReload)
    {
      RemoveOrphanBeams();
      OnMapStart(Server.MapName);
      return;
    }

    Server.NextWorldUpdate(() =>
    {
      string mapName = Server.MapName;
      if (_mapName.Length == 0 && !string.IsNullOrEmpty(mapName))
        OnMapStart(mapName);
    });
  }

  public override void Unload(bool hotReload)
  {
    RemoveAllBeams();
    RemoveOrphanBeams();
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    if (RefreshTriggers())
      Rebuild();

    return HookResult.Continue;
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    int userId = Util.UserId(@event.Userid);

    if (userId >= 0 && _viewers.Remove(userId))
    {
      _lastViewerOrigin.Remove(userId);
      _forceRefresh = true;
    }

    return HookResult.Continue;
  }

  private void OnMapStart(string mapName)
  {
    int generation = ++_generation;

    _mapName = mapName;
    _mapClips = null;
    _triggerSegments.Clear();
    _segments = [];
    _segmentCategory = [];
    _categoryNames = [];
    _categoryColors = [];
    _status = string.Empty;
    _loading = true;
    _forceRefresh = true;

    _viewers.Clear();
    _lastViewerOrigin.Clear();
    RemoveAllBeams();

    AddTimer(3f, () =>
    {
      RefreshTriggers();
      Rebuild();
    }, TimerFlags.STOP_ON_MAPCHANGE);

    string moduleDirectory = ModuleDirectory;
    string gameDirectory = Server.GameDirectory;
    string[] types = Util.Split(Config.Types);

    Task.Run(() =>
    {
      try
      {
        var map = LoadOrExtract(gameDirectory, moduleDirectory, mapName, types, out string info);
        Server.NextFrame(() => Publish(generation, map, info));
      }
      catch (Exception ex)
      {
        Server.NextFrame(() => Publish(generation, null, ex.Message));
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

  private void Publish(int generation, ClipMap? map, string info)
  {
    if (generation != _generation)
      return;

    _loading = false;
    _status = map == null ? info : string.Empty;
    _mapClips = map;

    Rebuild();

    if (map == null)
    {
      Console.WriteLine($"[ShowPlayerClips] {info}");
      return;
    }

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

    int count = segments.Count;

    _categoryNames = [.. names];
    _categoryColors = [.. colors];
    _segments = [.. segments];
    _segmentCategory = [.. categories];
    _candidates = new int[count];
    _candidateDistance = new float[count];
    _candidateViewer = new int[count];
    _wantedStamp = new int[count];
    _keptStamp = new int[count];
    _candidateCount = 0;
    _stamp = 0;

    for (int slot = 0; slot < _beams.Count; slot++)
    {
      _beamSegment[slot] = -1;
      _beamCategory[slot] = -1;
    }

    if (count == 0)
      RemoveAllBeams();

    _forceRefresh = true;
  }

  private bool RefreshTriggers()
  {
    var found = _pendingTriggers;
    found.Clear();

    int count = 0;

    if (Util.Split(Config.Types).Contains("trigger", StringComparer.OrdinalIgnoreCase))
    {
      foreach (var instance in Utilities.GetAllEntities())
      {
        if (!TriggerClasses.Contains(instance.DesignerName))
          continue;

        var entity = instance.As<CBaseEntity>();
        var origin = entity.AbsOrigin;
        var angles = entity.AbsRotation;
        var collision = entity.Collision;

        if (origin == null || angles == null || collision == null)
          continue;

        var mins = (Vector3)collision.Mins;
        var maxs = (Vector3)collision.Maxs;

        if (Vector3.Distance(mins, maxs) < 1f)
          continue;

        AddBox(found, (Vector3)origin, (Vector3)angles, mins, maxs);
        count++;
      }
    }

    _triggerCount = count;

    if (found.SequenceEqual(_triggerSegments))
      return false;

    (_triggerSegments, _pendingTriggers) = (found, _triggerSegments);

    if (_mapClips != null && count > 0)
      Console.WriteLine($"[ShowPlayerClips] {_mapName}: {count} trigger.");

    return true;
  }

  private static void AddBox(List<ClipSegment> target, Vector3 origin, Vector3 angles, Vector3 low, Vector3 high)
  {
    var rotation = angles == Vector3.Zero
      ? Matrix4x4.Identity
      : Matrix4x4.CreateRotationX(angles.Z * MathF.PI / 180f)
        * Matrix4x4.CreateRotationY(angles.X * MathF.PI / 180f)
        * Matrix4x4.CreateRotationZ(angles.Y * MathF.PI / 180f);

    Span<Vector3> corners =
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

    for (int i = 0; i < corners.Length; i++)
      corners[i] = Vector3.Transform(corners[i], rotation) + origin;

    var center = Vector3.Transform((low + high) * 0.5f, rotation) + origin;

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
      var normal = (start + end) * 0.5f - center;

      float length = normal.Length();
      normal = length > 0.001f ? normal / length : Vector3.Zero;

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
    {
      string stamp = Path.GetFileNameWithoutExtension(file)[(mapName.Length + 1)..];
      if (stamp.Count(c => c == '_') == 2)
        File.Delete(file);
    }
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

    var addonRoots = AddonRoots(game).Where(Directory.Exists).ToList();

    foreach (string root in addonRoots)
    {
      foreach (string candidate in Directory.GetFiles(root, $"{mapName}.vpk", SearchOption.AllDirectories))
      {
        if (ClipMap.ContainsMap(candidate, mapName))
          return candidate;
      }
    }

    foreach (string root in addonRoots)
    {
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
    int userId = Util.UserId(player);
    if (player == null || userId < 0)
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
      string reason = _status.Length > 0 ? _status : string.Join(", ", Util.Split(Config.Types));
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["showclips.unavailable", reason]}");
      return;
    }

    bool enabled = _viewers.Add(userId);
    if (!enabled)
      _viewers.Remove(userId);

    _lastViewerOrigin.Remove(userId);
    _forceRefresh = true;

    if (enabled)
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
    int viewerCount = 0;
    bool changed = _forceRefresh;

    _staleViewers.Clear();

    foreach (int userId in _viewers)
    {
      var player = Util.FromUserId(userId);
      if (player == null)
      {
        _staleViewers.Add(userId);
        continue;
      }

      var origin = ViewerOrigin(player);
      if (origin == null || viewerCount >= MaxSlots)
      {
        if (_lastViewerOrigin.Remove(userId))
          changed = true;

        continue;
      }

      var position = origin.Value;
      _viewerPositions[viewerCount++] = position;

      if (!_lastViewerOrigin.TryGetValue(userId, out var last) || Vector3.Distance(last, position) > Config.MoveStep)
      {
        _lastViewerOrigin[userId] = position;
        changed = true;
      }
    }

    foreach (int userId in _staleViewers)
    {
      _viewers.Remove(userId);
      _lastViewerOrigin.Remove(userId);
      changed = true;
    }

    _forceRefresh = false;
    _viewerCount = viewerCount;

    if (viewerCount == 0)
    {
      if (_beams.Count > 0)
        RemoveAllBeams();

      return;
    }

    if (!changed && !BeamsLost())
      return;

    int count = 0;
    float radiusSquared = Config.Radius * Config.Radius;

    for (int i = 0; i < _segments.Length; i++)
    {
      var middle = _segments[i].Middle;
      float best = float.MaxValue;
      int nearest = 0;

      for (int v = 0; v < viewerCount; v++)
      {
        float distance = Vector3.DistanceSquared(middle, _viewerPositions[v]);
        if (distance < best)
        {
          best = distance;
          nearest = v;
        }
      }

      if (best > radiusSquared)
        continue;

      _candidateViewer[i] = nearest;
      _candidateDistance[count] = best;
      _candidates[count++] = i;
    }

    if (count > Config.MaxBeams)
    {
      SelectNearest(_candidateDistance, _candidates, count, Config.MaxBeams);
      count = Config.MaxBeams;
    }

    _candidateCount = count;

    Apply();
  }

  private static void SelectNearest(float[] keys, int[] items, int count, int keep)
  {
    int left = 0;
    int right = count - 1;

    while (left < right)
    {
      float pivot = keys[(left + right) >> 1];
      int i = left;
      int j = right;

      while (i <= j)
      {
        while (keys[i] < pivot)
          i++;
        while (keys[j] > pivot)
          j--;

        if (i > j)
          break;

        (keys[i], keys[j]) = (keys[j], keys[i]);
        (items[i], items[j]) = (items[j], items[i]);
        i++;
        j--;
      }

      if (keep <= j)
        right = j;
      else if (keep >= i)
        left = i;
      else
        return;
    }
  }

  private bool BeamsLost()
  {
    foreach (var beam in _beams)
    {
      if (!beam.IsValid)
        return true;
    }

    return false;
  }

  private void Apply()
  {
    int stamp = ++_stamp;

    for (int i = 0; i < _candidateCount; i++)
      _wantedStamp[_candidates[i]] = stamp;

    _freeSlots.Clear();

    for (int slot = 0; slot < _beams.Count; slot++)
    {
      int segment = _beamSegment[slot];

      if (segment >= 0 && _wantedStamp[segment] == stamp && _beams[slot].IsValid)
      {
        _keptStamp[segment] = stamp;

        bool flipped = ShouldFlip(segment);
        if (_beamFlipped[slot] != flipped)
          ApplySegment(slot, segment, flipped);

        continue;
      }

      _beamSegment[slot] = -1;
      _freeSlots.Add(slot);
    }

    int nextFree = 0;

    for (int i = 0; i < _candidateCount; i++)
    {
      int index = _candidates[i];
      if (_keptStamp[index] == stamp)
        continue;

      bool flipped = ShouldFlip(index);

      if (nextFree < _freeSlots.Count)
      {
        ApplySegment(_freeSlots[nextFree++], index, flipped);
        continue;
      }

      var beam = CreateBeam(index, flipped);
      if (beam == null)
        break;

      _beams.Add(beam);
      _beamSegment.Add(index);
      _beamCategory.Add(_segmentCategory[index]);
      _beamFlipped.Add(flipped);
    }

    for (int i = _freeSlots.Count - 1; i >= nextFree; i--)
      RemoveSlot(_freeSlots[i]);
  }

  private void ApplySegment(int slot, int index, bool flipped)
  {
    var beam = _beams[slot];
    int category = _segmentCategory[index];

    if (!beam.IsValid)
    {
      var created = CreateBeam(index, flipped);
      if (created == null)
        return;

      _beams[slot] = created;
      _beamSegment[slot] = index;
      _beamCategory[slot] = category;
      _beamFlipped[slot] = flipped;
      return;
    }

    if (_beamCategory[slot] != category)
    {
      beam.Render = _categoryColors[category];
      Utilities.SetStateChanged(beam, "CBaseModelEntity", "m_clrRender");
      _beamCategory[slot] = category;
    }

    var (start, end) = Endpoints(index, flipped);
    MoveBeam(beam, start, end);

    _beamSegment[slot] = index;
    _beamFlipped[slot] = flipped;
  }

  private static Vector3? ViewerOrigin(CCSPlayerController player)
  {
    var pawn = player.PlayerPawn.Value;
    if (pawn != null && pawn.IsValid && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE && pawn.AbsOrigin != null)
      return (Vector3)pawn.AbsOrigin;

    var observer = player.Pawn.Value;

    var target = observer?.ObserverServices?.ObserverTarget.Value?.As<CBaseEntity>();
    if (target != null && target.IsValid && target.AbsOrigin != null)
      return (Vector3)target.AbsOrigin;

    if (observer != null && observer.IsValid && observer.AbsOrigin != null)
      return (Vector3)observer.AbsOrigin;

    return null;
  }

  private bool ShouldFlip(int index)
  {
    var segment = _segments[index];
    if (segment.Normal == Vector3.Zero || _viewerCount == 0)
      return false;

    var middle = segment.Middle;
    return Vector3.Dot(segment.Normal, _viewerPositions[_candidateViewer[index]] - middle) < 0f;
  }

  private (Vector3 Start, Vector3 End) Endpoints(int index, bool flipped)
  {
    var segment = _segments[index];
    var offset = segment.Normal * (flipped ? -Config.Offset : Config.Offset);
    return (segment.Start + offset, segment.End + offset);
  }

  private static void MoveBeam(CEnvBeam beam, Vector3 start, Vector3 end)
  {
    beam.Teleport(start);
    beam.EndPos.X = end.X;
    beam.EndPos.Y = end.Y;
    beam.EndPos.Z = end.Z;
    Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");
  }

  private CEnvBeam? CreateBeam(int index, bool flipped)
  {
    var beam = Utilities.CreateEntityByName<CEnvBeam>("env_beam");
    if (beam == null || !beam.IsValid)
      return null;

    var (start, end) = Endpoints(index, flipped);

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
    MoveBeam(beam, start, end);

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
          if (!beam.IsValid || (beam.Entity?.Name != BeamMarker && beam.Globalname != BeamMarker))
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

    bool collected = false;

    foreach ((CCheckTransmitInfo info, CCSPlayerController? viewer) in infoList)
    {
      if (viewer != null && viewer.IsValid && !viewer.IsHLTV && _viewers.Contains(Util.UserId(viewer)))
        continue;

      if (!collected)
      {
        _transmitIndices.Clear();
        foreach (var beam in _beams)
        {
          if (beam.IsValid)
            _transmitIndices.Add(beam.Index);
        }

        collected = true;
      }

      for (int i = 0; i < _transmitIndices.Count; i++)
        info.TransmitEntities.Remove(_transmitIndices[i]);
    }
  }
}
