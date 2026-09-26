using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using static CounterStrikeSharp.API.Core.Listeners;

namespace Ads;

public class AdsMySqlSettings
{
  [JsonPropertyName("host")] public string Host { get; set; } = "";
  [JsonPropertyName("port")] public uint Port { get; set; } = 3306;
  [JsonPropertyName("database")] public string Database { get; set; } = "";
  [JsonPropertyName("user")] public string User { get; set; } = "";
  [JsonPropertyName("password")] public string Password { get; set; } = "";
  [JsonPropertyName("table_prefix")] public string TablePrefix { get; set; } = "ads_";
}

public class AdsConfig
{
  [JsonPropertyName("ads_storage")]
  public string Storage { get; set; } = "json";

  [JsonPropertyName("ads_queue_mode")]
  public string QueueMode { get; set; } = "channel";

  [JsonPropertyName("ads_flag")]
  public string Flag { get; set; } = "@css/root";

  [JsonPropertyName("ads_cmd")]
  public string Commands { get; set; } = "css_ads";

  [JsonPropertyName("ads_rotate_step")]
  public float RotateStep { get; set; } = 90f;

  [JsonPropertyName("ads_move_step")]
  public float MoveStep { get; set; } = 5f;

  [JsonPropertyName("ads_scale_step")]
  public float ScaleStep { get; set; } = 0.25f;

  [JsonPropertyName("ads_reload_cmd")]
  public string ReloadCommands { get; set; } = "css_adsreload";

  [JsonPropertyName("ads_importsql_cmd")]
  public string ImportSqlCommands { get; set; } = "css_adsimportsql";

  [JsonPropertyName("ads_exportsql_cmd")]
  public string ExportSqlCommands { get; set; } = "css_adsexportsql";

  [JsonPropertyName("ads_hud_tick")]
  public int HudTick { get; set; } = 4;

  [JsonPropertyName("ads_font")]
  public string Font { get; set; } = "Arial Bold";

  [JsonPropertyName("ads_forward")]
  public float Forward { get; set; } = 7f;

  [JsonPropertyName("ads_units_per_px")]
  public float UnitsPerPx { get; set; } = 0.012f;

  [JsonPropertyName("mysql")]
  public AdsMySqlSettings MySql { get; set; } = new();
}

public partial class Ads : BasePlugin
{
  public override string ModuleName => "Ads";
  public override string ModuleVersion => "1.0.3";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public AdsConfig Config { get; set; } = new();

  private const int MaxSlots = 64;
  private const string EntityName = "bydexter_ads";

  private AdsJsonStorage _json = null!;
  private AdsMySqlStorage? _mysql;
  private IAdsStorage _storage = null!;
  private WasdMenuManager _menus = null!;

  private AdsData _data = new();
  private PropsData _propsData = new();
  private MapsData _mapsData = new();
  private string _mapName = "";

  private void LoadSettings(bool keepOnError = false)
  {
    try
    {
      Config = _json.LoadSettings();
    }
    catch (Exception ex)
    {
      if (keepOnError)
        throw;

      Logger.LogError("Ayarlar yuklenemedi, varsayilanlar kullanilacak: {message}", ex.Message);
      Config = new AdsConfig();
    }

    if (Config.HudTick < 1)
      Config.HudTick = 1;
    if (Config.Forward < 1f)
      Config.Forward = 7f;
    if (Config.UnitsPerPx <= 0f)
      Config.UnitsPerPx = 0.012f;
    if (Config.RotateStep <= 0f)
      Config.RotateStep = 90f;
    if (Config.MoveStep <= 0f)
      Config.MoveStep = 5f;
    if (Config.ScaleStep <= 0f)
      Config.ScaleStep = 0.25f;
  }

  public override void Load(bool hotReload)
  {
    _json = new AdsJsonStorage(ModuleDirectory);
    _json.Init();

    LoadSettings();

    _menus = new WasdMenuManager(this,
      () => Localizer["menu.scroll"],
      () => Localizer["menu.select"],
      () => Localizer["menu.exit"]);

    HudGuard.Install(this);

    _storage = _json;
    RegisterCommands();

    RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);
    RegisterListener<OnMapStart>(OnMapStartHandler);
    RegisterListener<OnMapEnd>(OnMapEndHandler);

    RegisterEventAds();

    Action? spawn = hotReload
      ? () =>
      {
        _mapName = Server.MapName;
        SpawnWorldAds();
      }
      : null;

    if (Config.Storage.Equals("mysql", StringComparison.OrdinalIgnoreCase))
      StartMySql(spawn);
    else
      LoadData(false, spawn);
  }

  public override void Unload(bool hotReload)
  {
    _unloaded = true;
    _menus.Clear();
    ClearScreenTexts();
    RemoveWorldAds();
  }

  private void StartMySql(Action? after)
  {
    var mysql = new AdsMySqlStorage(Config.MySql);
    _mysql = mysql;
    _storage = mysql;

    Background(() =>
    {
      try
      {
        mysql.Init();
      }
      catch (Exception ex)
      {
        Main(() =>
        {
          Logger.LogError("MySQL baglantisi kurulamadi, JSON kullanilacak: {message}", ex.Message);
          if (ReferenceEquals(_storage, mysql))
          {
            _storage = _json;
            LoadData(false, after);
          }
        });
        return;
      }

      Main(() => LoadData(false, after));
    });
  }

  private sealed record LoadResult(AdsData? Ads, PropsData? Props, MapsData? Maps, List<Exception> Errors);

  private static LoadResult ReadAll(IAdsStorage storage)
  {
    var errors = new List<Exception>();
    AdsData? ads = null;
    PropsData? props = null;
    MapsData? maps = null;

    try { ads = storage.Load(); }
    catch (Exception ex) { errors.Add(ex); }

    try { props = storage.LoadProps(); }
    catch (Exception ex) { errors.Add(ex); }

    try { maps = storage.LoadMaps(); }
    catch (Exception ex) { errors.Add(ex); }

    return new LoadResult(ads, props, maps, errors);
  }

  private void LoadData(bool sync, Action? after = null, CCSPlayerController? player = null)
  {
    int version = ++_loadVersion;
    var storage = _storage;

    if (sync || !IsRemote(storage))
    {
      ApplyLoaded(ReadAll(storage), player);
      after?.Invoke();
      return;
    }

    Background(() =>
    {
      var result = ReadAll(storage);
      Main(() =>
      {
        if (version != _loadVersion)
          return;

        ApplyLoaded(result, player);
        after?.Invoke();
      });
    });
  }

  private void ApplyLoaded(LoadResult result, CCSPlayerController? player)
  {
    foreach (var error in result.Errors)
    {
      Logger.LogError("Veri yuklenemedi, onceki hali korunuyor: {message}", error.Message);
      if (player != null)
        Reply(player, ErrorText(error));
    }

    if (result.Ads != null)
      _data = result.Ads;

    if (result.Props != null)
      _propsData = result.Props;

    if (result.Maps != null)
      _mapsData = result.Maps;

    _data.Props = _mapsData.Props;

    ClearScreenTexts();
    BuildQueues();
    BuildEvents();
    SyncListeners();
  }

  private bool _tickHooked;
  private bool _transmitHooked;

  private void SyncListeners()
  {
    bool renderEvents = false;
    bool textEvents = false;

    foreach (var ad in _data.Events)
    {
      string type = ad.Type.Trim().ToLowerInvariant();
      if (type == "hudsay")
        renderEvents = true;
      else if (type == "screentext")
        renderEvents = textEvents = true;
    }

    bool needTick = _data.ScreenTexts.Count > 0 || _data.HudSays.Count > 0 || _data.ChatSays.Count > 0 || renderEvents;
    bool needTransmit = _data.ScreenTexts.Count > 0 || textEvents || HasHiddenProp();

    if (needTick != _tickHooked)
    {
      if (needTick)
        RegisterListener<OnTick>(OnTick);
      else
        RemoveListener<OnTick>(OnTick);

      _tickHooked = needTick;
    }

    if (needTransmit != _transmitHooked)
    {
      if (needTransmit)
        RegisterListener<CheckTransmit>(OnCheckTransmit);
      else
        RemoveListener<CheckTransmit>(OnCheckTransmit);

      _transmitHooked = needTransmit;
    }
  }

  private bool HasHiddenProp()
  {
    foreach (var ad in _data.Props)
    {
      if (!string.IsNullOrWhiteSpace(ad.Flag) || !string.IsNullOrWhiteSpace(ad.IgnoreFlag))
        return true;
    }

    return false;
  }

  private void OnServerPrecacheResources(ResourceManifest manifest)
  {
    LoadData(true);

    var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var model in _propsData.Models)
      paths.Add(model.Path);

    foreach (var ad in _mapsData.Props)
      paths.Add(ad.Path);

    foreach (var path in paths)
    {
      if (!string.IsNullOrWhiteSpace(path))
        manifest.AddResource(path);
    }
  }

  private void OnMapStartHandler(string mapName)
  {
    _mapName = mapName;
    ClearEntityCache();
    _entities.Clear();
    _hiddenEntities.Clear();
    Array.Clear(_selected);
    Array.Clear(_awaiting);
    Array.Clear(_axis);
  }

  private void OnMapEndHandler()
  {
    ClearEntityCache();
    _entities.Clear();
    _hiddenEntities.Clear();
    ClearScreenTexts();
    ResetQueues();
  }

  private void OnCheckTransmit(CCheckTransmitInfoList infoList)
  {
    foreach ((CCheckTransmitInfo info, CCSPlayerController? viewer) in infoList)
    {
      if (viewer == null || !viewer.IsValid)
        continue;

      int viewerSlot = viewer.Slot;

      if (_screenTextCount > 0)
      {
        for (int slot = 0; slot < MaxSlots; slot++)
        {
          if (slot == viewerSlot)
            continue;

          var text = _screenTexts[slot];
          if (text != null && text.IsValid)
            info.TransmitEntities.Remove(text);
        }
      }

      foreach (var placed in _hiddenEntities)
      {
        if (placed.Entity == null || !placed.Entity.IsValid)
          continue;

        if (!CanSee(viewer, placed.Flag, placed.IgnoreFlag))
          info.TransmitEntities.Remove(placed.Entity);
      }
    }
  }

  private static bool CanSee(CCSPlayerController player, string? flag, string? ignoreFlag)
  {
    if (!string.IsNullOrWhiteSpace(ignoreFlag) && HasExactFlag(player, ignoreFlag))
      return false;

    if (!string.IsNullOrWhiteSpace(flag) && !Util.HasAccess(player, flag))
      return false;

    return true;
  }

  private static bool HasExactFlag(CCSPlayerController player, string flags)
  {
    if (!player.IsValid)
      return false;

    var data = AdminManager.GetPlayerAdminData(player);
    if (data == null)
      return false;

    foreach (var wanted in Util.Split(flags))
    {
      foreach (var owned in data.Flags.Values)
      {
        if (owned.Contains(wanted))
          return true;
      }
    }

    return false;
  }

  private static bool MapMatches(string map, string current)
  {
    if (string.IsNullOrWhiteSpace(map) || map == "*")
      return true;

    foreach (var name in Util.Split(map))
    {
      if (string.Equals(name, current, StringComparison.OrdinalIgnoreCase))
        return true;
    }

    return false;
  }
}
