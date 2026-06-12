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
  public override string ModuleVersion => "1.0.3";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  private readonly Dictionary<ulong, bool> rgbEnabled = new();
  private readonly HashSet<ulong> _savedEnabled = new();
  private readonly object _saveLock = new();

  private string SavePath => Path.Combine(ModuleDirectory, "PlayerRGB.json");

  private int r1 = 255, g1 = 0, b1 = 0;

  public override void Load(bool hotReload)
  {
    LoadSaved();
    RegisterListener<OnTick>(OnTickRGB);
    RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
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

    if (r1 > 0 && b1 == 0)
    {
      r1--;
      g1++;
    }
    else if (g1 > 0 && r1 == 0)
    {
      g1--;
      b1++;
    }
    else if (b1 > 0 && g1 == 0)
    {
      b1--;
      r1++;
    }

    foreach (var player in Utilities.GetPlayers())
    {
      if (player == null || !player.IsValid || !IsAlive(player))
        continue;

      if (rgbEnabled.TryGetValue(player.SteamID, out bool enabled) && enabled)
      {
        var pawn = player.PlayerPawn.Value;
        if (pawn != null)
        {
          pawn.Render = Color.FromArgb(255, r1, g1, b1);
          Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }
      }
    }
  }

  static bool IsAlive(CCSPlayerController? player)
  {
    return player?.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
  }
}

public static class CC
{
  public static char Default => '\x01';
  public static char Red => '\x07';
  public static char LightRed => '\x0F';
  public static char DarkRed => '\x02';
  public static char BlueGrey => '\x0A';
  public static char Blue => '\x0B';
  public static char DarkBlue => '\x0C';
  public static char Purple => '\x0C';
  public static char Orchid => '\x0E';
  public static char Yellow => '\x09';
  public static char Gold => '\x10';
  public static char LightGreen => '\x05';
  public static char Green => '\x04';
  public static char Lime => '\x06';
  public static char Grey => '\x08';
  public static char Grey2 => '\x0D';
}