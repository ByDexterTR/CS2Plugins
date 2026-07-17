using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using static CounterStrikeSharp.API.Core.Listeners;

public class PlayerRGB : BasePlugin
{
  public override string ModuleName => "PlayerRGB";
  public override string ModuleVersion => "1.0.4";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  private readonly Dictionary<ulong, bool> rgbEnabled = new();
  private readonly HashSet<ulong> _savedEnabled = new();
  private readonly object _saveLock = new();

  private string SavePath => Path.Combine(ModuleDirectory, "PlayerRGB.json");

  private const float RainbowDegreesPerSecond = 60f;

  public override void Load(bool hotReload)
  {
    LoadSaved();
    RegisterListener<OnTick>(OnTickRGB);
    RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
  }

  public override void Unload(bool hotReload)
  {
    foreach (var (steamId, enabled) in rgbEnabled)
    {
      if (!enabled)
        continue;

      var pawn = Utilities.GetPlayerFromSteamId(steamId)?.PlayerPawn.Value;
      if (pawn == null || !pawn.IsValid)
        continue;

      pawn.Render = Color.FromArgb(255, 255, 255, 255);
      Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }
    rgbEnabled.Clear();
  }

  private void LoadSaved()
  {
    try
    {
      if (!File.Exists(SavePath))
        return;

      var list = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(SavePath));
      if (list == null)
        return;

      foreach (var entry in list)
      {
        if (ulong.TryParse(entry, out var steamId))
          _savedEnabled.Add(steamId);
      }
    }
    catch
    {
    }
  }

  private void SaveAsync()
  {
    List<string> snapshot;
    lock (_saveLock)
      snapshot = _savedEnabled.Select(id => id.ToString()).ToList();

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

  private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player != null && player.IsValid && !player.IsBot && _savedEnabled.Contains(player.SteamID))
      rgbEnabled[player.SteamID] = true;
    return HookResult.Continue;
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player != null)
      rgbEnabled.Remove(player.SteamID);
    return HookResult.Continue;
  }

  [ConsoleCommand("css_rgb", "css_rgb")]
  [RequiresPermissions("@css/cheats")]
  public void OnRGBCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    bool enabled = rgbEnabled.TryGetValue(player.SteamID, out var val) ? !val : true;
    rgbEnabled[player.SteamID] = enabled;

    lock (_saveLock)
    {
      if (enabled)
        _savedEnabled.Add(player.SteamID);
      else
        _savedEnabled.Remove(player.SteamID);
    }
    SaveAsync();

    if (enabled)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["playerrgb.enabled"]}");
    }
    else
    {
      var pawn = player.PlayerPawn.Value;
      if (pawn != null)
      {
        pawn.Render = Color.FromArgb(255, 255, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
      }
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["playerrgb.disabled"]}");
    }
  }

  private void OnTickRGB()
  {
    if (rgbEnabled.Count == 0 || !rgbEnabled.ContainsValue(true))
      return;

    var color = FromHue(Server.CurrentTime * RainbowDegreesPerSecond % 360.0);

    foreach (var (steamId, enabled) in rgbEnabled)
    {
      if (!enabled)
        continue;

      var player = Utilities.GetPlayerFromSteamId(steamId);
      if (player == null || !player.IsValid || !IsAlive(player))
        continue;

      var pawn = player.PlayerPawn.Value;
      if (pawn != null)
      {
        pawn.Render = color;
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
      }
    }
  }

  private static Color FromHue(double h)
  {
    double x = 1 - Math.Abs(h / 60.0 % 2 - 1);
    double r = 0, g = 0, b = 0;
    if (h < 60) { r = 1; g = x; }
    else if (h < 120) { r = x; g = 1; }
    else if (h < 180) { g = 1; b = x; }
    else if (h < 240) { g = x; b = 1; }
    else if (h < 300) { r = x; b = 1; }
    else { r = 1; b = x; }
    return Color.FromArgb(255, (int)(r * 255), (int)(g * 255), (int)(b * 255));
  }

  static bool IsAlive(CCSPlayerController? player)
  {
    return player?.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
  }
}
