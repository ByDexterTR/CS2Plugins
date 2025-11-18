using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;

namespace JBDoors;

public class JBDoorsConfig : BasePluginConfig
{
  [JsonPropertyName("chat_prefix")]
  public string ChatPrefix { get; set; } = "[ByDexter]";
}

public class JBDoors : BasePlugin, IPluginConfig<JBDoorsConfig>
{
  public override string ModuleName => "JBDoors";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "Kapıları aç/kapat";

  public JBDoorsConfig Config { get; set; } = new();

  public void OnConfigParsed(JBDoorsConfig config)
  {
    Config = config;
  }

  [ConsoleCommand("css_kapiac", "Tüm kapıları açar")]
  [RequiresPermissionsOr("@css/generic", "@jailbreak/warden")]
  public void OnConsoleOpen(CCSPlayerController? player, CommandInfo info)
  {
    Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{player?.PlayerName}{CC.Default}: kapıları {CC.Green}açtı{CC.Default}.");
    ForceEntInput("func_door", "Open");
    ForceEntInput("func_movelinear", "Open");
    ForceEntInput("func_door_rotating", "Open");
    ForceEntInput("prop_door_rotating", "Open");
    ForceEntInput("func_breakable", "Break");
  }

  [ConsoleCommand("css_kapikapat", "Tüm kapıları kapatır")]
  [RequiresPermissionsOr("@css/generic", "@jailbreak/warden")]
  public void OnConsoleClose(CCSPlayerController? player, CommandInfo info)
  {
    Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{player?.PlayerName}{CC.Default}: kapıları {CC.Red}kapattı{CC.Default}.");
    ForceEntInput("func_door", "Close");
    ForceEntInput("func_movelinear", "Close");
    ForceEntInput("func_door_rotating", "Close");
    ForceEntInput("prop_door_rotating", "Close");
  }

  private static void ForceEntInput(string name, string input)
  {
    var targets = Utilities.FindAllEntitiesByDesignerName<CEntityInstance>(name);
    foreach (var ent in targets)
    {
      try
      {
        if (ent == null || !ent.IsValid)
          continue;

        ent.AcceptInput(input);
      }
      catch { }
    }
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