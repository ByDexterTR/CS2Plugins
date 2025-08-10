using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using System.Text.Json.Serialization;

public class SilahsilConfig : BasePluginConfig
{
  [JsonPropertyName("chat_prefix")]
  public string ChatPrefix { get; set; } = "[ByDexter]";
}

public class Silahsil : BasePlugin, IPluginConfig<SilahsilConfig>
{
  public SilahsilConfig Config { get; set; } = new SilahsilConfig();

  public override string ModuleName => "Silahsil";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "Yerdeki silahları siler";

  public void OnConfigParsed(SilahsilConfig config)
  {
    Config = config;
  }

  [ConsoleCommand("css_silahsil", "css_silahsil")]
  [RequiresPermissions("@css/slay")]
  public void OnSilahSilCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    int groundWpn = 0;
    var weapons = Utilities.FindAllEntitiesByDesignerName<CCSWeaponBase>("weapon_");
    foreach (var weapon in weapons.Where(weapon => weapon != null && weapon.IsValid && !weapon.OwnerEntity.IsValid))
    {
      weapon.Remove();
      groundWpn++;
    }

    player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{player.PlayerName}{CC.Default}, Yerdeki {CC.Green}{groundWpn}{CC.Default} silahı sildi!");
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