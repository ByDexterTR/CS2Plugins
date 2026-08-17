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
  public override string ModuleVersion => "1.0.2";
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

  private void LoadSettings()
  {
    try
    {
      Config = _json.LoadSettings();
    }
    catch (Exception ex)
    {
      Logger.LogError("Ayarlar yuklenemedi, varsayilanlar kullanilacak: {message}", ex.Message);
      Config = new AdsConfig();
    }

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

    if (Config.Storage.Equals("mysql", StringComparison.OrdinalIgnoreCase))
    {
      try
      {
        _mysql = new AdsMySqlStorage(Config.MySql);
        _mysql.Init();
        _storage = _mysql;
      }
      catch (Exception ex)
      {
        Logger.LogError("MySQL baglantisi kurulamadi, JSON kullanilacak: {message}", ex.Message);
        _storage = _json;
      }
    }
    else
    {
      _storage = _json;
    }

    LoadData();
    RegisterCommands();

    RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);
    RegisterListener<OnMapStart>(OnMapStartHandler);
    RegisterListener<OnMapEnd>(OnMapEndHandler);

    RegisterEventAds();
    SyncListeners();

    if (hotReload)
    {
      _mapName = Server.MapName;
      SpawnWorldAds();
    }
  }

  public override void Unload(bool hotReload)
  {
    _menus.Clear();
    ClearScreenTexts();
    RemoveWorldAds();
  }

  private void LoadData()
  {
    try
    {
      _data = _storage.Load();
    }
    catch (Exception ex)
    {
      Logger.LogError("Reklamlar yuklenemedi: {message}", ex.Message);
      _data = new AdsData();
    }

    try
    {
      _propsData = _storage.LoadProps();
    }
    catch (Exception ex)
    {
      Logger.LogError("Proplar yuklenemedi: {message}", ex.Message);
      _propsData = new PropsData();
    }

    try
    {
      _mapsData = _storage.LoadMaps();
    }
    catch (Exception ex)
    {
      Logger.LogError("Harita kayitlari yuklenemedi: {message}", ex.Message);
      _mapsData = new MapsData();
    }

    _data.Props = _mapsData.Props;

    BuildQueues();
    BuildEvents();
    SyncListeners();
  }

  private void SaveMaps() => _storage.SaveMaps(_mapsData);

  private bool _tickHooked;
  private bool _transmitHooked;

  private void SyncListeners()
  {
    bool needTick = _data.ScreenTexts.Count > 0 || _data.HudSays.Count > 0 || _data.Events.Count > 0;
    bool needTransmit = _data.ScreenTexts.Count > 0 || _data.Events.Count > 0 || HasHiddenProp();

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
    LoadData();

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
    _entities.Clear();
    Array.Clear(_selected);
    Array.Clear(_awaiting);
    Array.Clear(_axis);
  }

  private void OnMapEndHandler()
  {
    _entities.Clear();
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

      for (int slot = 0; slot < MaxSlots; slot++)
      {
        if (slot == viewerSlot)
          continue;

        var text = _screenTexts[slot];
        if (text != null && text.IsValid)
          info.TransmitEntities.Remove(text);
      }

      foreach (var placed in _entities)
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
