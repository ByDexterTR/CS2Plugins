using System.Drawing;
using System.Numerics;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using static CounterStrikeSharp.API.Core.Listeners;
using ByDexter.Shared;
using ByDexter.Shared.Source2;
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
  public override string ModuleVersion => "1.1.3";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public ShowPlayerClipsConfig Config { get; set; } = new();

  private const int MaxSlots = 64;
  private const int MaskWords = 16384 / 32;
  private const int CreateBudget = 64;
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
  private ulong _viewerMask;

  private readonly List<CEnvBeam> _beams = [];
  private readonly List<nint> _beamPointer = [];
  private readonly List<int> _beamIndex = [];
  private readonly List<bool> _beamAlive = [];
  private readonly List<int> _beamSegment = [];
  private readonly List<int> _beamCategory = [];
  private readonly List<bool> _beamFlipped = [];
  private readonly Dictionary<nint, int> _beamSlot = [];
  private readonly List<int> _freeSlots = [];
  private readonly List<int> _dropSlots = [];
  private int _lostBeams;
  private bool _pendingCreate;

  private readonly uint[] _hideMask = new uint[MaskWords];
  private readonly List<int> _hideWords = [];
  private bool _hideDirty;

  private int _slotOffset;
  private static class Offsets
  {
    internal static readonly int EndPosOffset = Schema.GetSchemaOffset("CBeam", "m_vecEndPos");
    internal static readonly int BeamChain = Schema.GetSchemaOffset("CBeam", "__m_pChainEntity");
    internal static readonly int ModelChain = Schema.GetSchemaOffset("CBaseModelEntity", "__m_pChainEntity");
    internal static readonly int RenderOffset = Schema.GetSchemaOffset("CBaseModelEntity", "m_clrRender");
    internal static readonly int WidthOffset = Schema.GetSchemaOffset("CBeam", "m_fWidth");
  }

  private List<ClipSegment> _triggerSegments = [];
  private List<ClipSegment> _pendingTriggers = [];
  private int _triggerCount;
  private ClipMap? _mapClips;
  private ClipSegment[] _segments = [];
  private float[] _startX = [];
  private float[] _startY = [];
  private float[] _startZ = [];
  private float[] _deltaX = [];
  private float[] _deltaY = [];
  private float[] _deltaZ = [];
  private float[] _inverseLength = [];
  private float[] _bestDistance = [];
  private int[] _segmentCategory = [];
  private string[] _categoryNames = [];
  private Color[] _categoryColors = [];

  private int[] _candidates = [];
  private float[] _candidateDistance = [];
  private int[] _nearestViewer = [];
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
    _slotOffset = GameData.GetOffset("CheckTransmitPlayerSlot");

    RegisterListener<OnTick>(OnTick);
    RegisterListener<CheckTransmit>(OnCheckTransmit);
    RegisterListener<OnEntityDeleted>(OnEntityDeleted);

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
      UpdateViewerMask();
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
    ClearSegmentLanes();
    _segmentCategory = [];
    _categoryNames = [];
    _categoryColors = [];
    _status = string.Empty;
    _loading = true;
    _forceRefresh = true;

    _viewers.Clear();
    _lastViewerOrigin.Clear();
    UpdateViewerMask();
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
    ClearSegmentLanes();
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
    BuildSegmentLanes(count);
    _segmentCategory = [.. categories];
    _candidates = new int[count];
    _candidateDistance = new float[count];
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
    var candidates = MapPhysics.FindVpks(gameDirectory, mapName);

    if (candidates.Count == 0)
    {
      info = $"'{mapName}.vpk' bulunamadi.";
      return null;
    }

    string vpk = candidates[0];
    if (candidates.Count > 1)
    {
      var match = MapMatch.CaptureAsync().GetAwaiter().GetResult();
      if (match.Pick(candidates, mapName) is not { } picked)
      {
        info = $"'{mapName}' icin {candidates.Count} harita dosyasi bulundu, hicbiri sunucudaki haritayla eslesmedi.";
        return null;
      }

      vpk = picked.Vpk;
    }

    string? source = MapPhysics.SourceId(vpk);
    string key = source == null ? mapName : $"{mapName}@{source}";
    var file = new FileInfo(vpk);
    string stamp = $"{file.Length:x}_{file.LastWriteTimeUtc.Ticks:x}_{TypeKey(types)}";
    string cachePath = Path.Combine(moduleDirectory, "cache", $"{key}_{stamp}.spc");

    var cached = ClipMap.Load(cachePath);
    if (cached != null)
    {
      info = "onbellek";
      return cached;
    }

    var map = ClipMap.Extract(vpk, mapName, types);

    try
    {
      CleanCache(Path.Combine(moduleDirectory, "cache"), key);
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
    UpdateViewerMask();
    _forceRefresh = true;

    if (enabled)
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["showclips.enabled", _segments.Length, string.Join(", ", _categoryNames)]}");
    else
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["showclips.disabled"]}");
  }

  private void UpdateViewerMask()
  {
    ulong mask = 0;
    foreach (int userId in _viewers)
      mask |= 1UL << (userId & 0x3F);

    _viewerMask = mask;
  }

  private void OnTick()
  {
    if (_segments.Length == 0 || (_viewers.Count == 0 && _beams.Count == 0))
      return;

    float now = Server.CurrentTime;
    if (now < _nextRefresh && !_pendingCreate)
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

    if (_staleViewers.Count > 0)
      UpdateViewerMask();

    _forceRefresh = false;
    _viewerCount = viewerCount;

    if (viewerCount == 0)
    {
      if (_beams.Count > 0)
        RemoveAllBeams();

      return;
    }

    if (!changed && _lostBeams == 0)
      return;

    int count = 0;
    float radiusSquared = Config.Radius * Config.Radius;

    MeasureSegments(viewerCount);

    for (int i = 0; i < _segments.Length; i++)
    {
      float best = _bestDistance[i];
      if (best > radiusSquared)
        continue;

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

  private void ClearSegmentLanes()
  {
    _startX = [];
    _startY = [];
    _startZ = [];
    _deltaX = [];
    _deltaY = [];
    _deltaZ = [];
    _inverseLength = [];
    _bestDistance = [];
    _nearestViewer = [];
  }

  private void BuildSegmentLanes(int count)
  {
    int width = Vector<float>.Count;
    int padded = (count + width - 1) / width * width;

    _startX = new float[padded];
    _startY = new float[padded];
    _startZ = new float[padded];
    _deltaX = new float[padded];
    _deltaY = new float[padded];
    _deltaZ = new float[padded];
    _inverseLength = new float[padded];
    _bestDistance = new float[padded];
    _nearestViewer = new int[padded];

    for (int i = 0; i < padded; i++)
    {
      if (i >= count)
      {
        _startX[i] = _startY[i] = _startZ[i] = 1e9f;
        continue;
      }

      var start = _segments[i].Start;
      var delta = _segments[i].End - start;
      float lengthSquared = delta.LengthSquared();

      _startX[i] = start.X;
      _startY[i] = start.Y;
      _startZ[i] = start.Z;
      _deltaX[i] = delta.X;
      _deltaY[i] = delta.Y;
      _deltaZ[i] = delta.Z;
      _inverseLength[i] = lengthSquared > 0f ? 1f / lengthSquared : 0f;
    }
  }

  private void MeasureSegments(int viewerCount)
  {
    int width = Vector<float>.Count;
    int padded = _startX.Length;

    for (int v = 0; v < viewerCount; v++)
    {
      var viewer = _viewerPositions[v];
      var viewerX = new Vector<float>(viewer.X);
      var viewerY = new Vector<float>(viewer.Y);
      var viewerZ = new Vector<float>(viewer.Z);
      var viewerIndex = new Vector<int>(v);

      for (int i = 0; i < padded; i += width)
      {
        var offsetX = viewerX - new Vector<float>(_startX, i);
        var offsetY = viewerY - new Vector<float>(_startY, i);
        var offsetZ = viewerZ - new Vector<float>(_startZ, i);
        var deltaX = new Vector<float>(_deltaX, i);
        var deltaY = new Vector<float>(_deltaY, i);
        var deltaZ = new Vector<float>(_deltaZ, i);

        var along = (offsetX * deltaX + offsetY * deltaY + offsetZ * deltaZ) * new Vector<float>(_inverseLength, i);
        var t = Vector.Min(Vector.Max(along, Vector<float>.Zero), Vector<float>.One);

        var restX = offsetX - deltaX * t;
        var restY = offsetY - deltaY * t;
        var restZ = offsetZ - deltaZ * t;
        var distance = restX * restX + restY * restY + restZ * restZ;

        if (v == 0)
        {
          distance.CopyTo(_bestDistance, i);
          viewerIndex.CopyTo(_nearestViewer, i);
          continue;
        }

        var best = new Vector<float>(_bestDistance, i);
        var closer = Vector.LessThan(distance, best);
        Vector.ConditionalSelect(closer, distance, best).CopyTo(_bestDistance, i);
        Vector.ConditionalSelect(closer, viewerIndex, new Vector<int>(_nearestViewer, i)).CopyTo(_nearestViewer, i);
      }
    }
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

  private void Apply()
  {
    int stamp = ++_stamp;

    for (int i = 0; i < _candidateCount; i++)
      _wantedStamp[_candidates[i]] = stamp;

    _freeSlots.Clear();
    _dropSlots.Clear();

    for (int slot = 0; slot < _beams.Count; slot++)
    {
      int segment = _beamSegment[slot];

      if (!_beamAlive[slot])
      {
        _dropSlots.Add(slot);
        continue;
      }

      if (segment >= 0 && _wantedStamp[segment] == stamp)
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
    int created = 0;
    _pendingCreate = false;

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

      if (created >= CreateBudget)
      {
        _pendingCreate = true;
        _forceRefresh = true;
        break;
      }

      var beam = CreateBeam(index, flipped);
      if (beam == null)
        break;

      created++;

      _beams.Add(beam);
      _beamPointer.Add(0);
      _beamIndex.Add(0);
      _beamAlive.Add(false);
      _beamSegment.Add(index);
      _beamCategory.Add(_segmentCategory[index]);
      _beamFlipped.Add(flipped);
      Track(_beams.Count - 1, beam);
    }

    for (int i = nextFree; i < _freeSlots.Count; i++)
      _dropSlots.Add(_freeSlots[i]);

    _dropSlots.Sort();

    for (int i = _dropSlots.Count - 1; i >= 0; i--)
      RemoveSlot(_dropSlots[i]);

    _lostBeams = 0;
  }

  private void Track(int slot, CEnvBeam beam)
  {
    nint pointer = beam.Handle;
    int index = (int)beam.Index;

    _beams[slot] = beam;
    _beamPointer[slot] = pointer;
    _beamIndex[slot] = index;
    _beamAlive[slot] = true;
    _beamSlot[pointer] = slot;

    if ((uint)index < MaskWords * 32)
    {
      _hideMask[index >> 5] |= 1u << (index & 31);
      _hideDirty = true;
    }
  }

  private void Untrack(int slot)
  {
    if (!_beamAlive[slot])
      return;

    _beamAlive[slot] = false;
    _beamSlot.Remove(_beamPointer[slot]);

    int index = _beamIndex[slot];
    if ((uint)index < MaskWords * 32)
    {
      _hideMask[index >> 5] &= ~(1u << (index & 31));
      _hideDirty = true;
    }
  }

  private void OnEntityDeleted(CEntityInstance entity)
  {
    if (_beamSlot.Count == 0 || !_beamSlot.TryGetValue(entity.Handle, out int slot))
      return;

    Untrack(slot);
    _lostBeams++;
  }

  private void ApplySegment(int slot, int index, bool flipped)
  {
    var beam = _beams[slot];
    int category = _segmentCategory[index];

    if (_beamCategory[slot] != category)
    {
      beam.Render = _categoryColors[category];
      StateChanged(beam, beam.Handle, Offsets.ModelChain, Offsets.RenderOffset);
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
    return Vector3.Dot(segment.Normal, _viewerPositions[_nearestViewer[index]] - middle) < 0f;
  }

  private (Vector3 Start, Vector3 End) Endpoints(int index, bool flipped)
  {
    var segment = _segments[index];
    var offset = segment.Normal * (flipped ? -Config.Offset : Config.Offset);
    return (segment.Start + offset, segment.End + offset);
  }

  private unsafe void MoveBeam(CEnvBeam beam, Vector3 start, Vector3 end)
  {
    beam.Teleport(start);

    nint handle = beam.Handle;
    *(Vector3*)(handle + Offsets.EndPosOffset) = end;
    StateChanged(beam, handle, Offsets.BeamChain, Offsets.EndPosOffset);
  }

  private static void StateChanged(CBaseEntity entity, nint handle, int chain, int offset)
  {
    if (chain != 0)
    {
      NativeAPI.SchemaNetworkStateChanged(handle + chain, (uint)offset, uint.MaxValue, uint.MaxValue);
      return;
    }

    NativeAPI.SchemaSetStateChanged(handle, (uint)offset, uint.MaxValue, uint.MaxValue);
    entity.LastNetworkChange = Server.CurrentTime;
    entity.IsSteadyState.Clear();
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
    StateChanged(beam, beam.Handle, Offsets.BeamChain, Offsets.WidthOffset);
    beam.Render = _categoryColors[_segmentCategory[index]];
    StateChanged(beam, beam.Handle, Offsets.ModelChain, Offsets.RenderOffset);
    MoveBeam(beam, start, end);

    return beam;
  }

  private void RemoveSlot(int slot)
  {
    if (_beamAlive[slot])
    {
      var beam = _beams[slot];
      Untrack(slot);
      beam.Remove();
    }

    int last = _beams.Count - 1;

    if (slot != last)
    {
      _beams[slot] = _beams[last];
      _beamPointer[slot] = _beamPointer[last];
      _beamIndex[slot] = _beamIndex[last];
      _beamAlive[slot] = _beamAlive[last];
      _beamSegment[slot] = _beamSegment[last];
      _beamCategory[slot] = _beamCategory[last];
      _beamFlipped[slot] = _beamFlipped[last];

      if (_beamAlive[slot])
        _beamSlot[_beamPointer[slot]] = slot;
    }

    _beams.RemoveAt(last);
    _beamPointer.RemoveAt(last);
    _beamIndex.RemoveAt(last);
    _beamAlive.RemoveAt(last);
    _beamSegment.RemoveAt(last);
    _beamCategory.RemoveAt(last);
    _beamFlipped.RemoveAt(last);
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
    for (int slot = 0; slot < _beams.Count; slot++)
    {
      if (!_beamAlive[slot])
        continue;

      try
      {
        _beams[slot].Remove();
      }
      catch
      {
      }
    }

    _beams.Clear();
    _beamPointer.Clear();
    _beamIndex.Clear();
    _beamAlive.Clear();
    _beamSegment.Clear();
    _beamCategory.Clear();
    _beamFlipped.Clear();
    _beamSlot.Clear();
    _lostBeams = 0;
    _pendingCreate = false;

    Array.Clear(_hideMask);
    _hideWords.Clear();
    _hideDirty = false;
  }

  private unsafe void OnCheckTransmit(CCheckTransmitInfoList infoList)
  {
    if (_beamSlot.Count == 0)
      return;

    if (_hideDirty)
    {
      _hideWords.Clear();
      for (int word = 0; word < MaskWords; word++)
      {
        if (_hideMask[word] != 0)
          _hideWords.Add(word);
      }

      _hideDirty = false;
    }

    nint* list = (nint*)infoList.Handle;
    nint* entries = (nint*)list[0];
    int count = (int)list[1];

    for (int k = 0; k < count; k++)
    {
      nint entry = entries[k];
      int slot = *(int*)(entry + _slotOffset);
      if ((uint)slot < MaxSlots && (_viewerMask & (1UL << slot)) != 0)
        continue;

      uint* bits = *(uint**)entry;
      uint* dont = *(uint**)(entry + 8);

      foreach (int word in _hideWords)
      {
        uint hide = bits[word] & _hideMask[word];
        if (hide == 0)
          continue;

        bits[word] &= ~hide;
        if (dont != null)
          dont[word] |= hide;
      }
    }
  }
}
