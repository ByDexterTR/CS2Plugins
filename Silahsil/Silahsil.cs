using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using System.Text.Json.Serialization;

public class Silahsil : BasePlugin
{
  public override string ModuleName => "Silahsil";
  public override string ModuleVersion => "1.0.4";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  [ConsoleCommand("css_silahsil", "css_silahsil")]
  [RequiresPermissions("@css/slay")]
  public void OnSilahSilCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    int groundWpn = 0;
    var weapons = Utilities.GetAllEntities()
      .Where(e => e != null && e.IsValid && e.DesignerName?.StartsWith("weapon_") == true)
      .Select(e => e.As<CCSWeaponBase>())
      .Where(weapon => weapon != null && weapon.IsValid && weapon.OwnerEntity?.IsValid != true);

    foreach (var weapon in weapons)
    {
      weapon.Remove();
      groundWpn++;
    }

    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["silahsil.removed", player.PlayerName, groundWpn]}");
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