using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using ByDexter.Shared;

namespace CTKit;

public class WeaponConfig
{
  [JsonPropertyName("weapon_name")]
  public string WeaponName { get; set; } = "";

  [JsonPropertyName("display_name")]
  public string DisplayName { get; set; } = "";
}

public class CTKitConfig : BasePluginConfig
{
  [JsonPropertyName("default_primary_weapon")]
  public string DefaultPrimaryWeapon { get; set; } = "weapon_ak47";

  [JsonPropertyName("default_secondary_weapon")]
  public string DefaultSecondaryWeapon { get; set; } = "weapon_deagle";

  [JsonPropertyName("primary_weapons")]
  public List<WeaponConfig> PrimaryWeapons { get; set; } = new List<WeaponConfig>
    {
        new WeaponConfig { WeaponName = "weapon_ak47", DisplayName = "AK47" },
        new WeaponConfig { WeaponName = "weapon_m4a4", DisplayName = "M4A4" },
        new WeaponConfig { WeaponName = "weapon_m4a1_silencer", DisplayName = "M4A1-S" },
        new WeaponConfig { WeaponName = "weapon_awp", DisplayName = "AWP" },
        new WeaponConfig { WeaponName = "weapon_mag7", DisplayName = "MAG7" }
    };

  [JsonPropertyName("secondary_weapons")]
  public List<WeaponConfig> SecondaryWeapons { get; set; } = new List<WeaponConfig>
    {
        new WeaponConfig { WeaponName = "weapon_deagle", DisplayName = "DEAGLE" },
        new WeaponConfig { WeaponName = "weapon_cz75a", DisplayName = "CZ75A" },
        new WeaponConfig { WeaponName = "weapon_tec9", DisplayName = "TEC9" },
        new WeaponConfig { WeaponName = "weapon_elite", DisplayName = "ÇİFT BERETTA" },
        new WeaponConfig { WeaponName = "weapon_usp_silencer", DisplayName = "USP-S" },
        new WeaponConfig { WeaponName = "weapon_glock", DisplayName = "GLOCK" },
        new WeaponConfig { WeaponName = "weapon_revolver", DisplayName = "REVOLVER" }
    };
}

public class CTKit : BasePlugin, IPluginConfig<CTKitConfig>
{
  public override string ModuleName => "CTKit";
  public override string ModuleVersion => "1.0.6";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public CTKitConfig Config { get; set; } = new CTKitConfig();

  private Dictionary<ulong, string> playerPrimaryWeapon = new();
  private Dictionary<ulong, string> playerSecondaryWeapon = new();

  private WasdMenuManager _menus = null!;

  public void OnConfigParsed(CTKitConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    _menus = new WasdMenuManager(this,
      () => Localizer["menu.scroll"],
      () => Localizer["menu.select"],
      () => Localizer["menu.exit"]);
    RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
  }

  public override void Unload(bool hotReload)
  {
    _menus.Clear();
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player != null)
    {
      playerPrimaryWeapon.Remove(player.SteamID);
      playerSecondaryWeapon.Remove(player.SteamID);
    }
    return HookResult.Continue;
  }

  private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || player.IsBot || player.Team != CsTeam.CounterTerrorist)
      return HookResult.Continue;

    var steamId = player.SteamID;

    var primaryWeapon = playerPrimaryWeapon.TryGetValue(steamId, out var selectedPrimary)
        ? selectedPrimary
        : Config.DefaultPrimaryWeapon;

    var secondaryWeapon = playerSecondaryWeapon.TryGetValue(steamId, out var selectedSecondary)
        ? selectedSecondary
        : Config.DefaultSecondaryWeapon;

    if (string.IsNullOrEmpty(primaryWeapon) && string.IsNullOrEmpty(secondaryWeapon))
      return HookResult.Continue;

    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
      return HookResult.Continue;

    var weaponServices = pawn.WeaponServices;
    if (weaponServices?.MyWeapons == null)
      return HookResult.Continue;

    foreach (var weaponOpt in weaponServices.MyWeapons)
    {
      if (weaponOpt?.Value == null)
        continue;

      var weaponName = weaponOpt.Value.DesignerName;
      if (weaponName.Contains("knife") || weaponName.Contains("bayonet"))
        continue;

      weaponOpt.Value.Remove();
    }

    if (!string.IsNullOrEmpty(primaryWeapon))
      player.GiveNamedItem(primaryWeapon);

    if (!string.IsNullOrEmpty(secondaryWeapon))
      player.GiveNamedItem(secondaryWeapon);

    return HookResult.Continue;
  }

  [ConsoleCommand("css_kit", "Open CT weapon kit menu")]
  public void OnKitCommand(CCSPlayerController? player, CommandInfo commandInfo)
  {
    if (player == null || !player.IsValid || player.IsBot)
      return;

    if (player.Team != CsTeam.CounterTerrorist)
    {
      commandInfo.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctkit.ct_only"]}");
      return;
    }

    ShowMainMenu(player);
  }

  private void ShowMainMenu(CCSPlayerController player)
  {
    if (player == null || !player.IsValid)
      return;

    var steamId = player.SteamID;
    var currentPrimary = playerPrimaryWeapon.TryGetValue(steamId, out var primary)
        ? GetWeaponDisplayName(primary, true)
        : Localizer["ctkit.not_selected"].ToString();
    var currentSecondary = playerSecondaryWeapon.TryGetValue(steamId, out var secondary)
        ? GetWeaponDisplayName(secondary, false)
        : Localizer["ctkit.not_selected"].ToString();

    var items = new List<WasdItem>
    {
      new() { Text = Localizer["ctkit.menu_primary", currentPrimary], OnSelect = p => ShowPrimaryWeaponMenu(p) },
      new() { Text = Localizer["ctkit.menu_secondary", currentSecondary], OnSelect = p => ShowSecondaryWeaponMenu(p) },
      new()
      {
        Text = Localizer["ctkit.menu_reset"],
        OnSelect = p =>
        {
          var sid = p.SteamID;
          playerPrimaryWeapon.Remove(sid);
          playerSecondaryWeapon.Remove(sid);
          p.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctkit.kit_reset"]}");
          ShowMainMenu(p);
        }
      }
    };

    _menus.Open(player, Localizer["ctkit.menu_title"], items);
  }

  private void ShowPrimaryWeaponMenu(CCSPlayerController player)
  {
    if (player == null || !player.IsValid)
      return;

    var items = Config.PrimaryWeapons.Select(weapon => new WasdItem
    {
      Text = weapon.DisplayName,
      OnSelect = p =>
      {
        playerPrimaryWeapon[p.SteamID] = weapon.WeaponName;
        p.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctkit.weapon_set", weapon.DisplayName]}");
        ShowMainMenu(p);
      }
    }).ToList();

    _menus.Open(player, Localizer["ctkit.menu_primary_title"], items);
  }

  private void ShowSecondaryWeaponMenu(CCSPlayerController player)
  {
    if (player == null || !player.IsValid)
      return;

    var items = Config.SecondaryWeapons.Select(weapon => new WasdItem
    {
      Text = weapon.DisplayName,
      OnSelect = p =>
      {
        playerSecondaryWeapon[p.SteamID] = weapon.WeaponName;
        p.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctkit.pistol_set", weapon.DisplayName]}");
        ShowMainMenu(p);
      }
    }).ToList();

    _menus.Open(player, Localizer["ctkit.menu_secondary_title"], items);
  }

  private string GetWeaponDisplayName(string weaponName, bool isPrimary)
  {
    var weaponList = isPrimary ? Config.PrimaryWeapons : Config.SecondaryWeapons;
    return weaponList.FirstOrDefault(w => w.WeaponName == weaponName)?.DisplayName ?? weaponName;
  }
}
