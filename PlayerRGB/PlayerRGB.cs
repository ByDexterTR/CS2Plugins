using System.Drawing;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using static CounterStrikeSharp.API.Core.Listeners;

public class PlayerRGBConfig : BasePluginConfig
{
  [JsonPropertyName("chat_prefix")]
  public string ChatPrefix { get; set; } = "[ByDexter]";
}

public class PlayerRGB : BasePlugin, IPluginConfig<PlayerRGBConfig>
{
  public override string ModuleName => "PlayerRGB";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "Oyuncu RGB efekti";

  public PlayerRGBConfig Config { get; set; } = new PlayerRGBConfig();

  private readonly Dictionary<ulong, bool> rgbEnabled = new();

  private int r1 = 255, g1 = 0, b1 = 0;

  public void OnConfigParsed(PlayerRGBConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    RegisterListener<OnTick>(OnTickRGB);
  }

  [ConsoleCommand("css_rgb", "css_rgb")]
  [RequiresPermissions("@css/cheats")]
  public void OnRGBCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    bool enabled = rgbEnabled.TryGetValue(player.SteamID, out var val) ? !val : true;
    rgbEnabled[player.SteamID] = enabled;

    if (enabled)
    {
      player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} RGB efekti {CC.Green}açıldı{CC.Default}!");
    }
    else
    {
      var pawn = player.PlayerPawn.Value;
      if (pawn != null)
      {
        pawn.Render = Color.FromArgb(255, 255, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
      }
      player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} RGB efekti {CC.Red}kapatıldı{CC.Default}!");
    }
  }

  private void OnTickRGB()
  {
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