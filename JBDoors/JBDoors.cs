using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace JBDoors;

public class JBDoorsConfig : BasePluginConfig
{
  [JsonPropertyName("dooropen_cmd")]
  public string DoorOpenCommands { get; set; } = "css_kapiac,css_dooropen";

  [JsonPropertyName("dooropen_flag")]
  public string DoorOpenFlag { get; set; } = "@jailbreak/warden,@css/generic";

  [JsonPropertyName("doorclose_cmd")]
  public string DoorCloseCommands { get; set; } = "css_kapikapat,css_doorclose";

  [JsonPropertyName("doorclose_flag")]
  public string DoorCloseFlag { get; set; } = "@jailbreak/warden,@css/generic";

  [JsonPropertyName("doorbreak")]
  public bool DoorBreak { get; set; } = true;
}

public class JBDoors : BasePlugin, IPluginConfig<JBDoorsConfig>
{
  public override string ModuleName => "JBDoors";
  public override string ModuleVersion => "1.0.1";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public JBDoorsConfig Config { get; set; } = new();

  public void OnConfigParsed(JBDoorsConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    foreach (var name in Util.Split(Config.DoorOpenCommands))
      AddCommand(name, "Tum kapilari acar", OnConsoleOpen);

    foreach (var name in Util.Split(Config.DoorCloseCommands))
      AddCommand(name, "Tum kapilari kapatir", OnConsoleClose);
  }

  public void OnConsoleOpen(CCSPlayerController? player, CommandInfo info)
  {
    if (!Util.HasAccess(player, Config.DoorOpenFlag))
      return;

    Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbdoors.doors_opened", player?.PlayerName ?? ""]}");
    ForceEntInput("func_door", "Open");
    ForceEntInput("func_movelinear", "Open");
    ForceEntInput("func_door_rotating", "Open");
    ForceEntInput("prop_door_rotating", "Open");
    if (Config.DoorBreak)
      ForceEntInput("func_breakable", "Break");
  }

  public void OnConsoleClose(CCSPlayerController? player, CommandInfo info)
  {
    if (!Util.HasAccess(player, Config.DoorCloseFlag))
      return;

    Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbdoors.doors_closed", player?.PlayerName ?? ""]}");
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
