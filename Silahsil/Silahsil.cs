using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using System.Text.Json.Serialization;

public class Silahsil : BasePlugin
{
  public override string ModuleName => "Silahsil";
  public override string ModuleVersion => "1.0.5";
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
