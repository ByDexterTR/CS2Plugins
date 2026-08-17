using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using ByDexter.Shared;

namespace JBLaserWar;

public partial class JBLaserWar
{
  private void OnLaserWarCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    if (!Util.HasAccess(player, Config.Flag))
    {
      Tell(player, Text("lw.no_access"));
      return;
    }

    ShowMainMenu(player);
  }

  private void ShowMainMenu(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid)
      return;

    var items = new List<WasdItem>
    {
      new()
      {
        Text = _active ? Localizer["lw.stop"] : Localizer["lw.start"],
        Color = _active ? "#FF8077" : "#76C97A",
        OnSelect = p =>
        {
          if (_active)
          {
            Stop(p);
            ShowMainMenu(p);
            return;
          }

          if (Start(p))
            _menus.Close(p);
          else
            ShowMainMenu(p);
        }
      },
      new() { Text = Localizer["lw.settings"], OnSelect = ShowSettingsMenu }
    };

    _menus.Open(player, Localizer["lw.menu_title"], items);
  }

  private void ShowSettingsMenu(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid)
      return;

    var items = new List<WasdItem>
    {
      WasdItem.Back(Localizer["lw.back"], ShowMainMenu),
      new()
      {
        Text = Localizer["lw.team_count", TeamCountLabel()],
        OnSelect = p =>
        {
          _teamCount = _teamCount >= MaxTeams ? MinTeams : _teamCount + 1;
          SaveSettings();
          ShowSettingsMenu(p);
        }
      },
      new()
      {
        Text = Localizer["lw.weapon", WeaponLabel(_weapon)],
        OnSelect = p =>
        {
          _weapon = NextWeapon(_weapon);
          ReEquipAll();
          SaveSettings();
          ShowSettingsMenu(p);
        }
      },
      new()
      {
        Text = Localizer["lw.damage", DamageLabel()],
        OnSelect = p =>
        {
          _hitsToKill = _hitsToKill >= 2 ? 1 : 2;
          _hits.Clear();
          SaveSettings();
          ShowSettingsMenu(p);
        }
      },
      new()
      {
        Text = Localizer["lw.bounces", _bounces],
        OnSelect = p =>
        {
          _bounces = _bounces >= MaxBounces ? 1 : _bounces + 1;
          SaveSettings();
          ShowSettingsMenu(p);
        }
      },
      new()
      {
        Text = Localizer["lw.ammo", StateLabel(_infiniteAmmo)],
        OnSelect = p =>
        {
          _infiniteAmmo = !_infiniteAmmo;
          ApplyAmmoCvar();
          SaveSettings();
          ShowSettingsMenu(p);
        }
      },
      new()
      {
        Text = Localizer["lw.gravity", GravityLabel()],
        OnSelect = p =>
        {
          _gravity = NextGravity(_gravity);
          ApplyGravityCvar();
          SaveSettings();
          ShowSettingsMenu(p);
        }
      },
      new()
      {
        Text = Localizer["lw.laser_sound", StateLabel(_laserSound)],
        OnSelect = p =>
        {
          _laserSound = !_laserSound;
          SaveSettings();
          ShowSettingsMenu(p);
        }
      }
    };

    _menus.Open(player, Localizer["lw.settings_title"], items);
  }

  private string NextWeapon(string current)
  {
    var list = Config.Weapons;
    int index = list.IndexOf(current);
    return list[(index + 1) % list.Count];
  }

  private float NextGravity(float current)
  {
    var list = Config.Gravity;
    int index = list.IndexOf(current);
    return list[(index + 1) % list.Count];
  }

  private string TeamCountLabel() =>
    _teamCount <= 1 ? Localizer["lw.team_solo"].ToString() : _teamCount.ToString();

  private string GravityLabel() =>
    _gravity.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

  private string DamageLabel() =>
    _hitsToKill >= 2 ? Localizer["lw.damage_double"].ToString() : Localizer["lw.damage_single"].ToString();

  private string StateLabel(bool value) =>
    value ? Localizer["lw.on"].ToString() : Localizer["lw.off"].ToString();

  private static string WeaponLabel(string weapon)
  {
    string name = weapon.StartsWith("weapon_", StringComparison.Ordinal) ? weapon["weapon_".Length..] : weapon;

    return name switch
    {
      "m4a1_silencer" => "M4A1-S",
      "usp_silencer" => "USP-S",
      "mp5sd" => "MP5-SD",
      "m4a1" => "M4A4",
      "hkp2000" => "P2000",
      "galilar" => "Galil AR",
      _ => name.Replace('_', ' ').ToUpperInvariant()
    };
  }
}
