using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using static CounterStrikeSharp.API.Core.Listeners;
using ByDexter.Shared;

public class MapBlockModel
{
  [JsonPropertyName("model")]
  public string Model { get; set; } = string.Empty;

  [JsonPropertyName("offset")]
  public float Offset { get; set; }
}

public class MapBlockConfig : BasePluginConfig
{
  [JsonPropertyName("mapblock_mode")]
  public int MapBlockMode { get; set; } = 2;

  [JsonPropertyName("mapblock_count")]
  public int MapBlockCount { get; set; } = 4;

  [JsonPropertyName("mapblock_announce")]
  public bool MapBlockAnnounce { get; set; } = true;

  [JsonPropertyName("mapblock_cmd")]
  public string MenuCommands { get; set; } = "css_mapblock,css_engel";

  [JsonPropertyName("mapblock_flag")]
  public string MenuFlag { get; set; } = "@css/root";

  [JsonPropertyName("mapblock_reload_cmd")]
  public string ReloadCommands { get; set; } = "css_mapblock_reload";

  [JsonPropertyName("mapblock_reload_flag")]
  public string ReloadFlag { get; set; } = "@css/root";

  [JsonPropertyName("mapblock_models")]
  public Dictionary<string, MapBlockModel> Models { get; set; } = new()
  {
    ["Cit 64"] = new() { Model = "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_64_capped.vmdl", Offset = 32f },
    ["Cit 128"] = new() { Model = "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_128_capped.vmdl", Offset = 64f },
    ["Cit 256"] = new() { Model = "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_256_capped.vmdl", Offset = 128f },
    ["Barikat 64"] = new() { Model = "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_cover_001_64.vmdl", Offset = 32f },
    ["Barikat 128"] = new() { Model = "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_cover_001_128.vmdl", Offset = 64f },
    ["Barikat 256"] = new() { Model = "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_cover_001_256.vmdl", Offset = 128f }
  };
}

public class MapBlock : BasePlugin, IPluginConfig<MapBlockConfig>
{
  public override string ModuleName => "MapBlock";
  public override string ModuleVersion => "1.0.6";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  private const string FenceName = "bydexter_mapblock";
  private const string PropDesignerName = "prop_dynamic_override";
  private const string LegacyPropDesignerName = "prop_physics_override";
  private const int SolidVPhysics = 6;
  private const string MapsFolderName = "maps";
  private const string ExampleFileName = "MapBlock.example.json";
  private const string LegacyPlacementsFileName = "MapBlock.placements.json";
  private const string LegacyModuleFileName = "MapBlock.json";
  private const float PickRangeSquared = 256f * 256f;

  private static readonly JsonSerializerOptions ReadOpts = new()
  {
    PropertyNameCaseInsensitive = true,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip
  };

  private static readonly JsonSerializerOptions WriteOpts = new()
  {
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
  };

  private sealed class FenceRecord
  {
    public string? Model { get; set; }
    public float[]? Origin { get; set; }
    public float[]? Angles { get; set; }
  }

  private sealed record FencePlacement(string ModelPath, float X, float Y, float Z, float Pitch, float Yaw, float Roll);

  private readonly record struct ActiveFence(uint Index, FencePlacement Placement);

  private readonly List<ActiveFence> _active = new();
  private readonly Dictionary<int, string> _selectedModel = new();
  private List<FencePlacement> _mapPlacements = new();
  private string _loadedMap = string.Empty;
  private bool _editMode;
  private CCSGameRulesProxy? _gameRulesProxy;

  private WasdMenuManager _menus = null!;

  private string ConfigDirectory => Path.GetFullPath(Path.Combine(
    ModuleDirectory, "..", "..", "configs", "plugins", "MapBlock"));

  private string MapsDirectory => Path.Combine(ModuleDirectory, MapsFolderName);

  public MapBlockConfig Config { get; set; } = new();

  public void OnConfigParsed(MapBlockConfig config)
  {
    if (config.MapBlockMode < 0 || config.MapBlockMode > 2)
    {
      Logger.LogWarning("[MapBlock] Gecersiz mapblock_mode: {Mode}. 2 olarak varsayildi.", config.MapBlockMode);
      config.MapBlockMode = 2;
    }

    if (config.MapBlockCount < 0)
      config.MapBlockCount = 0;

    foreach (var name in config.Models.Keys.ToList())
    {
      if (string.IsNullOrWhiteSpace(config.Models[name].Model))
        config.Models.Remove(name);
    }

    if (config.Models.Count == 0)
      Logger.LogWarning("[MapBlock] mapblock_models bos, menuden engel olusturulamaz.");

    Config = config;
  }

  public override void Load(bool hotReload)
  {
    _menus = new WasdMenuManager(this,
      () => Localizer["menu.scroll"],
      () => Localizer["menu.select"],
      () => Localizer["menu.exit"]);

    MigrateLegacyFiles();
    LoadPlacements();

    RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);
    RegisterListener<OnMapStart>(OnMapStart);
    RegisterListener<OnMapEnd>(OnMapEnd);
    RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Post);

    foreach (var name in Util.Split(Config.MenuCommands))
      AddCommand(name, "MapBlock menusunu acar", OnMenuCommand);

    foreach (var name in Util.Split(Config.ReloadCommands))
      AddCommand(name, "MapBlock yerlesimlerini yeniden yukler", OnReloadCommand);

    if (hotReload)
    {
      SweepFences();
      Evaluate();
    }
  }

  public override void Unload(bool hotReload)
  {
    _menus.Clear();
    SweepFences();
  }

  private void OnServerPrecacheResources(ResourceManifest resource)
  {
    var models = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var entry in Config.Models.Values)
      models.Add(NormalizeModel(entry.Model));

    CollectMapModels(models);

    foreach (var model in models)
      resource.AddResource(model);
  }

  private void OnMapStart(string mapName)
  {
    _active.Clear();
    _editMode = false;
    _gameRulesProxy = null;
    LoadPlacements();
  }

  private void OnMapEnd()
  {
    _active.Clear();
    _editMode = false;
    _gameRulesProxy = null;
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    Evaluate();
    return HookResult.Continue;
  }

  public void OnMenuCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    if (!Util.HasAccess(player, Config.MenuFlag))
    {
      info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["mapblock.no_permission"]}");
      return;
    }

    if (!Util.IsAlive(player))
    {
      info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["mapblock.must_be_alive"]}");
      return;
    }

    if (Config.Models.Count == 0)
    {
      info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["mapblock.no_models"]}");
      return;
    }

    ShowMenu(player);
  }

  public void OnReloadCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (!Util.HasAccess(player, Config.ReloadFlag))
    {
      info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["mapblock.no_permission"]}");
      return;
    }

    if (!LoadPlacements())
    {
      info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["mapblock.load_failed"]}");
      return;
    }

    SweepFences();
    Evaluate();

    info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["mapblock.reloaded", CurrentMap, _active.Count, Placements().Count]}");
  }

  private void ShowMenu(CCSPlayerController player)
  {
    if (!player.IsValid || !Util.IsAlive(player))
      return;

    var modelName = SelectedModelName(player);

    var items = new List<WasdItem>
    {
      new()
      {
        Text = Localizer["mapblock.create", modelName],
        OnSelect = CreateFence
      },
      new()
      {
        Text = Localizer["mapblock.change_model"],
        OnSelect = p =>
        {
          CycleModel(p);
          ShowMenu(p);
        }
      },
      new() { Text = Localizer["mapblock.delete_aimed"], OnSelect = RemoveAimedFence },
      new() { Text = Localizer["mapblock.delete_all"], OnSelect = RemoveAllOnMap },
      new()
      {
        Text = Localizer["mapblock.edit_mode", Localizer[_editMode ? "mapblock.on" : "mapblock.off"]],
        OnSelect = p =>
        {
          SetEditMode(!_editMode);
          ShowMenu(p);
        }
      }
    };

    _menus.Open(player, Localizer["mapblock.menu_title", CurrentMap], items);
  }

  private string SelectedModelName(CCSPlayerController player)
  {
    int userId = Util.UserId(player);

    if (_selectedModel.TryGetValue(userId, out var name) && Config.Models.ContainsKey(name))
      return name;

    var first = Config.Models.Keys.First();
    _selectedModel[userId] = first;
    return first;
  }

  private void CycleModel(CCSPlayerController player)
  {
    var names = Config.Models.Keys.ToList();
    int index = names.IndexOf(SelectedModelName(player));
    _selectedModel[Util.UserId(player)] = names[(index + 1) % names.Count];
  }

  private void CreateFence(CCSPlayerController player)
  {
    if (!player.IsValid || !Util.IsAlive(player))
      return;

    if (player.PlayerPawn.Value is not CCSPlayerPawn pawn)
      return;

    var hit = NativeTrace.TraceFromEyes(pawn);
    if (hit == null || hit.Value.Length() == 0)
    {
      Reply(player, NativeTrace.LastError != null
        ? Localizer["mapblock.trace_unavailable", NativeTrace.LastError]
        : Localizer["mapblock.no_hit"]);
      return;
    }

    var modelName = SelectedModelName(player);
    var entry = Config.Models[modelName];

    float yaw = pawn.EyeAngles.Y;
    double yawRad = (yaw + 90) * Math.PI / 180.0;
    float offset = -entry.Offset;

    var placement = new FencePlacement(
      NormalizeModel(entry.Model),
      hit.Value.X + (float)Math.Cos(yawRad) * offset,
      hit.Value.Y + (float)Math.Sin(yawRad) * offset,
      hit.Value.Z,
      0f, yaw, 0f);

    if (!TrySpawn(placement))
    {
      Reply(player, Localizer["mapblock.spawn_failed"]);
      return;
    }

    var list = Placements();
    list.Add(placement);

    if (!SavePlacements())
    {
      Reply(player, Localizer["mapblock.save_failed"]);
      return;
    }

    Reply(player, Localizer["mapblock.created", modelName, list.Count]);
  }

  private void RemoveAimedFence(CCSPlayerController player)
  {
    if (!player.IsValid || !Util.IsAlive(player))
      return;

    if (player.PlayerPawn.Value is not CCSPlayerPawn pawn)
      return;

    var hit = NativeTrace.TraceFromEyes(pawn);
    if (hit == null)
    {
      Reply(player, NativeTrace.LastError != null
        ? Localizer["mapblock.trace_unavailable", NativeTrace.LastError]
        : Localizer["mapblock.no_hit"]);
      return;
    }

    float bestDistance = float.MaxValue;
    int bestIndex = -1;

    for (int i = 0; i < _active.Count; i++)
    {
      var placement = _active[i].Placement;
      float dx = placement.X - hit.Value.X;
      float dy = placement.Y - hit.Value.Y;
      float dz = placement.Z - hit.Value.Z;
      float distance = dx * dx + dy * dy + dz * dz;

      if (distance < bestDistance)
      {
        bestDistance = distance;
        bestIndex = i;
      }
    }

    if (bestIndex < 0 || bestDistance > PickRangeSquared)
    {
      Reply(player, Localizer["mapblock.not_found"]);
      return;
    }

    var target = _active[bestIndex];
    RemoveEntity(target.Index);
    _active.RemoveAt(bestIndex);

    var list = Placements();
    list.Remove(target.Placement);

    if (!SavePlacements())
    {
      Reply(player, Localizer["mapblock.save_failed"]);
      return;
    }

    Reply(player, Localizer["mapblock.deleted", list.Count]);
  }

  private void RemoveAllOnMap(CCSPlayerController player)
  {
    if (!player.IsValid)
      return;

    int removed = Placements().Count;

    RemoveFences();
    _mapPlacements.Clear();

    if (!SavePlacements())
    {
      Reply(player, Localizer["mapblock.save_failed"]);
      return;
    }

    Reply(player, Localizer["mapblock.deleted_all", CurrentMap, removed]);
  }

  private void SetEditMode(bool enabled)
  {
    _editMode = enabled;
    Evaluate();
  }

  private void Evaluate()
  {
    RemoveFences();

    if (!_editMode && (IsWarmup() || !ShouldBlock()))
      return;

    SpawnForCurrentMap();
    Announce();
  }

  private CCSGameRules? GameRules()
  {
    if (_gameRulesProxy == null || !_gameRulesProxy.IsValid)
      _gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();

    return _gameRulesProxy?.GameRules;
  }

  private bool IsWarmup()
  {
    var rules = GameRules();
    return rules == null || rules.WarmupPeriod;
  }

  private void Announce()
  {
    if (!Config.MapBlockAnnounce || _editMode)
      return;

    if (Config.MapBlockCount <= 0 || Placements().Count == 0)
      return;

    var message = Config.MapBlockMode == 2
      ? Localizer["mapblock.announce_teams", Config.MapBlockCount]
      : Localizer["mapblock.announce_ct", Config.MapBlockCount];

    Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {message}");
  }

  private bool ShouldBlock()
  {
    if (Config.MapBlockMode == 0)
      return false;

    int threshold = Config.MapBlockCount;
    if (threshold == 0)
      return true;

    CountTeams(out int terrorists, out int counterTerrorists);

    int count = Config.MapBlockMode == 2 ? Math.Min(terrorists, counterTerrorists) : counterTerrorists;

    return count < threshold;
  }

  private static void CountTeams(out int terrorists, out int counterTerrorists)
  {
    terrorists = 0;
    counterTerrorists = 0;

    foreach (var player in Utilities.GetPlayers())
    {
      if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
        continue;

      if (player.Team == CsTeam.Terrorist)
        terrorists++;
      else if (player.Team == CsTeam.CounterTerrorist)
        counterTerrorists++;
    }
  }

  private int SpawnForCurrentMap()
  {
    var placements = Placements();
    if (placements.Count == 0)
      return 0;

    int spawned = 0;
    foreach (var placement in placements)
    {
      if (TrySpawn(placement))
        spawned++;
    }

    if (spawned < placements.Count)
      Logger.LogWarning("[MapBlock] {Map} icin {Spawned}/{Total} engel kuruldu.", CurrentMap, spawned, placements.Count);

    return spawned;
  }

  private bool TrySpawn(FencePlacement placement)
  {
    var prop = Utilities.CreateEntityByName<CDynamicProp>(PropDesignerName);
    if (prop == null || prop.Entity == null || !prop.IsValid)
      return false;

    prop.Entity.Name = FenceName;

    using (var keyValues = new CEntityKeyValues())
    {
      keyValues.SetString("model", placement.ModelPath);
      keyValues.SetString("targetname", FenceName);
      keyValues.SetInt("solid", SolidVPhysics);
      prop.DispatchSpawn(keyValues);
    }

    if (!prop.IsValid)
      return false;

    prop.SetModel(placement.ModelPath);
    prop.Teleport(
      new Vector(placement.X, placement.Y, placement.Z),
      new QAngle(placement.Pitch, placement.Yaw, placement.Roll),
      Vector.Zero);

    _active.Add(new ActiveFence(prop.Index, placement));
    return true;
  }

  private void RemoveFences()
  {
    foreach (var fence in _active)
      RemoveEntity(fence.Index);

    _active.Clear();
  }

  private static void RemoveEntity(uint index)
  {
    var prop = Utilities.GetEntityFromIndex<CDynamicProp>((int)index);
    RemoveIfOurs(prop);
  }

  private void SweepFences()
  {
    foreach (var prop in Utilities.FindAllEntitiesByDesignerName<CDynamicProp>(PropDesignerName))
      RemoveIfOurs(prop);

    foreach (var prop in Utilities.FindAllEntitiesByDesignerName<CPhysicsPropOverride>(LegacyPropDesignerName))
      RemoveIfOurs(prop);

    _active.Clear();
  }

  private static void RemoveIfOurs(CBaseEntity? prop)
  {
    if (prop?.Entity == null || !prop.IsValid)
      return;

    if (!string.Equals(prop.Entity.Name, FenceName, StringComparison.Ordinal))
      return;

    prop.Remove();
  }

  private List<FencePlacement> Placements()
  {
    if (!string.Equals(_loadedMap, CurrentMap, StringComparison.OrdinalIgnoreCase))
      LoadPlacements();

    return _mapPlacements;
  }

  private bool LoadPlacements()
  {
    var mapName = CurrentMap;
    _loadedMap = mapName;
    _mapPlacements = new List<FencePlacement>();

    var path = MapFilePath(mapName);
    if (path == null || !File.Exists(path))
      return true;

    try
    {
      var records = JsonSerializer.Deserialize<List<FenceRecord?>>(File.ReadAllText(path), ReadOpts);
      if (records == null)
        return true;

      foreach (var record in records)
      {
        var placement = FromRecord(record);
        if (placement != null)
          _mapPlacements.Add(placement);
      }

      return true;
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[MapBlock] {Path} dosyasindan yerlesimler yuklenemedi.", path);
      return false;
    }
  }

  private bool SavePlacements()
  {
    var path = MapFilePath(_loadedMap);
    if (path == null)
      return false;

    try
    {
      if (_mapPlacements.Count == 0)
      {
        if (File.Exists(path))
          File.Delete(path);

        return true;
      }

      var records = new List<FenceRecord>(_mapPlacements.Count);
      foreach (var placement in _mapPlacements)
        records.Add(ToRecord(placement));

      Directory.CreateDirectory(MapsDirectory);
      File.WriteAllText(path, JsonSerializer.Serialize(records, WriteOpts));
      return true;
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[MapBlock] {Path} dosyasina yerlesimler yazilamadi.", path);
      return false;
    }
  }

  private void CollectMapModels(HashSet<string> models)
  {
    if (!Directory.Exists(MapsDirectory))
      return;

    foreach (var path in Directory.EnumerateFiles(MapsDirectory, "*.json"))
    {
      try
      {
        var records = JsonSerializer.Deserialize<List<FenceRecord?>>(File.ReadAllText(path), ReadOpts);
        if (records == null)
          continue;

        foreach (var record in records)
        {
          if (record != null && !string.IsNullOrWhiteSpace(record.Model))
            models.Add(NormalizeModel(record.Model));
        }
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "[MapBlock] {Path} dosyasindaki modeller okunamadi.", path);
      }
    }
  }

  private void MigrateLegacyFiles()
  {
    try
    {
      if (Directory.Exists(MapsDirectory) && Directory.EnumerateFiles(MapsDirectory, "*.json").Any())
        return;

      if (MoveMapFiles(Path.Combine(ConfigDirectory, MapsFolderName)))
        return;

      var examplePath = Path.Combine(ModuleDirectory, ExampleFileName);

      string[] sources =
      {
        Path.Combine(ConfigDirectory, LegacyPlacementsFileName),
        Path.Combine(ModuleDirectory, LegacyModuleFileName),
        examplePath
      };

      foreach (var source in sources)
      {
        if (!File.Exists(source) || !SplitIntoMapFiles(source))
          continue;

        if (!string.Equals(source, examplePath, StringComparison.OrdinalIgnoreCase))
          File.Move(source, source + ".bak", true);

        Logger.LogInformation("[MapBlock] {Source} dosyasi {Target} klasorune bolundu.", source, MapsDirectory);
        return;
      }
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[MapBlock] Eski yerlesim dosyalari bolunemedi.");
    }
  }

  private bool MoveMapFiles(string sourceDirectory)
  {
    if (!Directory.Exists(sourceDirectory))
      return false;

    var files = Directory.GetFiles(sourceDirectory, "*.json");
    if (files.Length == 0)
      return false;

    Directory.CreateDirectory(MapsDirectory);

    foreach (var file in files)
      File.Move(file, Path.Combine(MapsDirectory, Path.GetFileName(file)), true);

    Logger.LogInformation("[MapBlock] {Count} yerlesim dosyasi {Source} klasorunden {Target} klasorune tasindi.",
      files.Length, sourceDirectory, MapsDirectory);

    return true;
  }

  private bool SplitIntoMapFiles(string sourcePath)
  {
    var data = JsonSerializer.Deserialize<Dictionary<string, List<FenceRecord?>?>>(File.ReadAllText(sourcePath), ReadOpts);
    if (data == null || data.Count == 0)
      return false;

    Directory.CreateDirectory(MapsDirectory);

    foreach (var (mapName, list) in data)
    {
      var path = MapFilePath(mapName);
      if (path == null || list == null)
        continue;

      var records = new List<FenceRecord>(list.Count);
      foreach (var record in list)
      {
        var placement = FromRecord(record);
        if (placement != null)
          records.Add(ToRecord(placement));
      }

      if (records.Count == 0)
        continue;

      File.WriteAllText(path, JsonSerializer.Serialize(records, WriteOpts));
    }

    return true;
  }

  private string? MapFilePath(string mapName)
  {
    var fileName = SanitizeMapName(mapName);
    return fileName == null ? null : Path.Combine(MapsDirectory, fileName + ".json");
  }

  private static string? SanitizeMapName(string mapName)
  {
    if (string.IsNullOrWhiteSpace(mapName))
      return null;

    var builder = new StringBuilder(mapName.Length);
    foreach (var character in mapName)
    {
      if (char.IsLetterOrDigit(character) || character == '_' || character == '-')
        builder.Append(char.ToLowerInvariant(character));
    }

    return builder.Length > 0 ? builder.ToString() : null;
  }

  private static FencePlacement? FromRecord(FenceRecord? record)
  {
    if (record == null || string.IsNullOrWhiteSpace(record.Model))
      return null;

    if (record.Origin is not { Length: >= 3 } origin)
      return null;

    if (record.Angles is not { Length: >= 3 } angles)
      return null;

    return new FencePlacement(
      NormalizeModel(record.Model),
      origin[0], origin[1], origin[2],
      angles[0], angles[1], angles[2]);
  }

  private static FenceRecord ToRecord(FencePlacement placement) => new()
  {
    Model = placement.ModelPath,
    Origin = new[] { placement.X, placement.Y, placement.Z },
    Angles = new[] { placement.Pitch, placement.Yaw, placement.Roll }
  };

  private void Reply(CCSPlayerController player, string message)
  {
    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {message}");
  }

  private static string NormalizeModel(string modelPath) =>
    modelPath.EndsWith(".vmdl_c", StringComparison.Ordinal) ? modelPath[..^2] : modelPath;

  private static string CurrentMap => Server.MapName ?? string.Empty;
}
