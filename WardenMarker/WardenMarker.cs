using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;
using ByDexter.Shared;

namespace WardenMarker;

public class MarkerRingConfig
{
  [JsonPropertyName("colors")]
  public List<string> Colors { get; set; } = new()
  {
    "#00AFFF", "#FF3232", "#00FF00", "#FFFB00", "#FFFFFF", "#FF004B"
  };

  [JsonPropertyName("sizes")]
  public List<float> Sizes { get; set; } = new() { 100f, 150f, 200f, 250f, 300f };

  [JsonPropertyName("widths")]
  public List<float> Widths { get; set; } = new() { 2f, 4f, 6f, 8f, 10f };

  [JsonPropertyName("default_color")]
  public string DefaultColor { get; set; } = "#00AFFF";

  [JsonPropertyName("default_size")]
  public float DefaultSize { get; set; } = 150f;

  [JsonPropertyName("default_width")]
  public float DefaultWidth { get; set; } = 4f;
}

public class MarkerDiscConfig
{
  [JsonPropertyName("enabled")]
  public bool Enabled { get; set; } = true;

  [JsonPropertyName("glow")]
  public bool Glow { get; set; } = true;

  [JsonPropertyName("glow_range")]
  public int GlowRange { get; set; } = 4096;

  [JsonPropertyName("alphas")]
  public List<int> Alphas { get; set; } = new() { 64, 128, 178, 255 };

  [JsonPropertyName("default_alpha")]
  public int DefaultAlpha { get; set; } = 178;
}

public class WardenMarkerConfig : BasePluginConfig
{
  [JsonPropertyName("marker_cmd")]
  public string Commands { get; set; } = "css_marker,css_isaret";

  [JsonPropertyName("marker_flag")]
  public string Flag { get; set; } = "@jailbreak/warden";

  [JsonPropertyName("marker_default_key")]
  public string DefaultKey { get; set; } = "use";

  [JsonPropertyName("marker_cooldown")]
  public float Cooldown { get; set; } = 0.3f;

  [JsonPropertyName("marker_max_distance")]
  public float MaxDistance { get; set; } = 8192f;

  [JsonPropertyName("marker_clear_on_roundend")]
  public bool ClearOnRoundEnd { get; set; } = true;

  [JsonPropertyName("marker_clear_on_death")]
  public bool ClearOnDeath { get; set; } = false;

  [JsonPropertyName("marker_ring")]
  public MarkerRingConfig Ring { get; set; } = new();

  [JsonPropertyName("marker_disc")]
  public MarkerDiscConfig Disc { get; set; } = new();
}

public class MarkerRingSettings
{
  [JsonPropertyName("color")]
  public string Color { get; set; } = "";

  [JsonPropertyName("size")]
  public float Size { get; set; }

  [JsonPropertyName("width")]
  public float Width { get; set; }
}

public class MarkerDiscSettings
{
  [JsonPropertyName("enabled")]
  public bool Enabled { get; set; } = true;

  [JsonPropertyName("glow")]
  public bool Glow { get; set; } = true;

  [JsonPropertyName("alpha")]
  public int Alpha { get; set; }
}

public class MarkerSettings
{
  [JsonPropertyName("key")]
  public string Key { get; set; } = "use";

  [JsonPropertyName("ring")]
  public MarkerRingSettings Ring { get; set; } = new();

  [JsonPropertyName("disc")]
  public MarkerDiscSettings Disc { get; set; } = new();
}

public partial class WardenMarker : BasePlugin, IPluginConfig<WardenMarkerConfig>
{
  public override string ModuleName => "WardenMarker";
  public override string ModuleVersion => "1.0.1";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public WardenMarkerConfig Config { get; set; } = new();

  private const int MaxSlots = 64;
  private const ulong InspectButton = 0x800000000;

  private readonly Marker?[] _markers = new Marker?[MaxSlots];
  private readonly MarkerSettings?[] _slots = new MarkerSettings?[MaxSlots];
  private readonly ulong[] _oldButtons = new ulong[MaxSlots];
  private readonly float[] _nextPlace = new float[MaxSlots];

  private readonly Dictionary<ulong, MarkerSettings> _saved = new();
  private readonly object _saveLock = new();
  private string SavePath => Path.Combine(ModuleDirectory, "WardenMarker.json");

  private WasdMenuManager _menus = null!;

  public void OnConfigParsed(WardenMarkerConfig config)
  {
    if (config.MaxDistance < 256f)
      config.MaxDistance = 256f;
    if (config.Cooldown < 0f)
      config.Cooldown = 0f;

    if (config.Ring.Colors.Count == 0)
      config.Ring.Colors.Add("#FFFFFF");
    if (config.Ring.Sizes.Count == 0)
      config.Ring.Sizes.Add(150f);
    if (config.Ring.Widths.Count == 0)
      config.Ring.Widths.Add(4f);
    if (config.Disc.Alphas.Count == 0)
      config.Disc.Alphas.Add(178);

    if (!config.Ring.Colors.Contains(config.Ring.DefaultColor))
      config.Ring.DefaultColor = config.Ring.Colors[0];
    if (!config.Ring.Sizes.Contains(config.Ring.DefaultSize))
      config.Ring.DefaultSize = config.Ring.Sizes[0];
    if (!config.Ring.Widths.Contains(config.Ring.DefaultWidth))
      config.Ring.DefaultWidth = config.Ring.Widths[0];
    if (!config.Disc.Alphas.Contains(config.Disc.DefaultAlpha))
      config.Disc.DefaultAlpha = config.Disc.Alphas[0];

    Config = config;
  }

  public override void Load(bool hotReload)
  {
    LoadSaved();

    _menus = new WasdMenuManager(this,
      () => Localizer["menu.scroll"],
      () => Localizer["menu.select"],
      () => Localizer["menu.exit"]);

    foreach (var name in Util.Split(Config.Commands))
      AddCommand(name, "Marker menusunu acar", OnMarkerCommand);

    RegisterListener<OnServerPrecacheResources>(m => m.AddResource(MarkerRing.DiscModel));
    RegisterListener<OnTick>(OnTick);
    RegisterListener<OnMapEnd>(ClearAll);
    RegisterListener<OnClientPutInServer>(OnClientPutInServer);
    RegisterListener<OnClientDisconnectPost>(OnClientDisconnectPost);

    AddCommandListener("player_ping", OnPingCommand);

    RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
    RegisterEventHandler<EventRoundEnd>(OnRoundEnd);

    if (hotReload)
    {
      foreach (var player in Utilities.GetPlayers())
      {
        if (player.IsValid && !player.IsBot && player.Slot < MaxSlots)
          _slots[player.Slot] = Resolve(player);
      }
    }
  }

  public override void Unload(bool hotReload)
  {
    _menus.Clear();
    ClearAll();
  }

  private void OnClientPutInServer(int slot)
  {
    if (slot < 0 || slot >= MaxSlots)
      return;

    _oldButtons[slot] = 0;
    _nextPlace[slot] = 0f;
    _slots[slot] = Resolve(Utilities.GetPlayerFromSlot(slot));
  }

  private void OnClientDisconnectPost(int slot)
  {
    if (slot < 0 || slot >= MaxSlots)
      return;

    _slots[slot] = null;
    _oldButtons[slot] = 0;
    Clear(slot);
  }

  private HookResult OnPlayerDeath(EventPlayerDeath ev, GameEventInfo info)
  {
    if (Config.ClearOnDeath && ev.Userid != null && ev.Userid.IsValid)
      Clear(ev.Userid.Slot);

    return HookResult.Continue;
  }

  private HookResult OnPlayerTeam(EventPlayerTeam ev, GameEventInfo info)
  {
    if (ev.Userid != null && ev.Userid.IsValid && ev.Team != (int)CsTeam.CounterTerrorist)
      Clear(ev.Userid.Slot);

    return HookResult.Continue;
  }

  private HookResult OnRoundEnd(EventRoundEnd ev, GameEventInfo info)
  {
    if (Config.ClearOnRoundEnd)
      ClearAll();

    return HookResult.Continue;
  }

  private HookResult OnPingCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || player.Slot >= MaxSlots)
      return HookResult.Continue;

    if (Get(player).Key != "ping" || !CanPlace(player))
      return HookResult.Continue;

    PlaceFromEyes(player);
    return HookResult.Handled;
  }

  private void OnTick()
  {
    for (int slot = 0; slot < MaxSlots; slot++)
    {
      var player = Utilities.GetPlayerFromSlot(slot);
      if (player == null || !player.IsValid || player.IsBot)
      {
        _oldButtons[slot] = 0;
        continue;
      }

      if (!Util.TryGetButtons(player, out var current))
      {
        _oldButtons[slot] = 0;
        continue;
      }

      ulong buttons = (ulong)current;
      ulong old = _oldButtons[slot];
      _oldButtons[slot] = buttons;

      string key = Get(player).Key;
      if (key == "ping" || key == "off" || _menus.IsOpen(player))
        continue;

      ulong wanted = key == "inspect" ? InspectButton : (ulong)PlayerButtons.Use;
      if ((buttons & wanted) == 0 || (old & wanted) != 0)
        continue;

      if (CanPlace(player))
        PlaceFromEyes(player);
    }
  }

  private bool CanPlace(CCSPlayerController player)
  {
    if (!Util.IsAlive(player) || player.TeamNum != (byte)CsTeam.CounterTerrorist)
      return false;

    if (!HasAccess(player))
      return false;

    return Server.CurrentTime >= _nextPlace[player.Slot];
  }

  private bool HasAccess(CCSPlayerController player)
  {
    var flags = Util.Split(Config.Flag);
    if (flags.Length == 0)
      return true;

    var data = AdminManager.GetPlayerAdminData(player);
    if (data == null)
      return false;

    var owned = data.GetAllFlags();
    return flags.Any(flag => owned.Contains(flag));
  }

  private void PlaceFromEyes(CCSPlayerController player)
  {
    var pawn = player.PlayerPawn.Value as CCSPlayerPawn;
    if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
      return;

    var hit = NativeTrace.TraceFromEyes(pawn);
    if (hit == null || hit.Value.Length() == 0)
    {
      player.PrintToChat(NativeTrace.LastError != null
        ? $" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["marker.trace_unavailable", NativeTrace.LastError]}"
        : $" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["marker.no_hit"]}");
      return;
    }

    var eye = new System.Numerics.Vector3(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + pawn.ViewOffset.Z);
    if (System.Numerics.Vector3.Distance(eye, hit.Value) > Config.MaxDistance)
      return;

    Place(player, hit.Value);
  }

  private void Place(CCSPlayerController player, System.Numerics.Vector3 center)
  {
    int slot = player.Slot;
    var settings = Get(player);

    if (_markers[slot] is { } existing)
    {
      MarkerRing.Destroy(existing);
      _markers[slot] = null;
    }

    _markers[slot] = MarkerRing.Create(slot, center, settings, Config);
    _nextPlace[slot] = Server.CurrentTime + Config.Cooldown;
  }

  private void Clear(int slot)
  {
    if (slot < 0 || slot >= MaxSlots)
      return;

    if (_markers[slot] is { } marker)
      MarkerRing.Destroy(marker);

    _markers[slot] = null;
  }

  private void ClearAll()
  {
    for (int slot = 0; slot < MaxSlots; slot++)
      Clear(slot);
  }

  private MarkerSettings Get(CCSPlayerController player)
  {
    int slot = player.Slot;
    if (slot >= 0 && slot < MaxSlots && _slots[slot] is { } cached)
      return cached;

    var settings = Resolve(player);
    if (slot >= 0 && slot < MaxSlots)
      _slots[slot] = settings;

    return settings;
  }

  private MarkerSettings Resolve(CCSPlayerController? player)
  {
    var settings = new MarkerSettings
    {
      Key = NormalizeKey(Config.DefaultKey),
      Ring = new MarkerRingSettings
      {
        Color = Config.Ring.DefaultColor,
        Size = Config.Ring.DefaultSize,
        Width = Config.Ring.DefaultWidth
      },
      Disc = new MarkerDiscSettings
      {
        Enabled = Config.Disc.Enabled,
        Glow = Config.Disc.Glow,
        Alpha = Config.Disc.DefaultAlpha
      }
    };

    if (player == null || !player.IsValid || player.IsBot)
      return settings;

    lock (_saveLock)
    {
      if (!_saved.TryGetValue(player.SteamID, out var stored))
        return settings;

      settings.Key = NormalizeKey(stored.Key);
      settings.Ring.Color = Config.Ring.Colors.Contains(stored.Ring.Color) ? stored.Ring.Color : Config.Ring.DefaultColor;
      settings.Ring.Size = Config.Ring.Sizes.Contains(stored.Ring.Size) ? stored.Ring.Size : Config.Ring.DefaultSize;
      settings.Ring.Width = Config.Ring.Widths.Contains(stored.Ring.Width) ? stored.Ring.Width : Config.Ring.DefaultWidth;
      settings.Disc.Enabled = stored.Disc.Enabled;
      settings.Disc.Glow = stored.Disc.Glow;
      settings.Disc.Alpha = Config.Disc.Alphas.Contains(stored.Disc.Alpha) ? stored.Disc.Alpha : Config.Disc.DefaultAlpha;
    }

    return settings;
  }

  private static string NormalizeKey(string key) => key.ToLowerInvariant() switch
  {
    "inspect" or "f" => "inspect",
    "ping" or "mouse3" => "ping",
    "off" or "kapali" => "off",
    _ => "use"
  };

  private void Remember(CCSPlayerController player)
  {
    if (!player.IsValid || player.IsBot)
      return;

    var settings = Get(player);

    lock (_saveLock)
    {
      _saved[player.SteamID] = new MarkerSettings
      {
        Key = settings.Key,
        Ring = new MarkerRingSettings { Color = settings.Ring.Color, Size = settings.Ring.Size, Width = settings.Ring.Width },
        Disc = new MarkerDiscSettings { Enabled = settings.Disc.Enabled, Glow = settings.Disc.Glow, Alpha = settings.Disc.Alpha }
      };
    }

    SaveAsync();
  }

  private void LoadSaved()
  {
    try
    {
      if (!File.Exists(SavePath))
        return;

      var stored = JsonSerializer.Deserialize<Dictionary<string, MarkerSettings>>(File.ReadAllText(SavePath));
      if (stored == null)
        return;

      foreach (var (key, value) in stored)
      {
        if (ulong.TryParse(key, out var steamId) && value != null)
          _saved[steamId] = value;
      }
    }
    catch
    {
    }
  }

  private void SaveAsync()
  {
    Dictionary<string, MarkerSettings> snapshot;
    lock (_saveLock)
      snapshot = _saved.ToDictionary(entry => entry.Key.ToString(), entry => entry.Value);

    var path = SavePath;
    Task.Run(() =>
    {
      try
      {
        lock (_saveLock)
          File.WriteAllText(path, JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
      }
      catch
      {
      }
    });
  }
}
