using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;

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
  [JsonPropertyName("chat_prefix")]
  public string ChatPrefix { get; set; } = "[ByDexter]";

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
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "[JB] CT Weapon Kit Menu";

  public CTKitConfig Config { get; set; } = new CTKitConfig();

  private Dictionary<ulong, string> playerPrimaryWeapon = new();
  private Dictionary<ulong, string> playerSecondaryWeapon = new();

  public void OnConfigParsed(CTKitConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
  }

  private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || player.IsBot || player.TeamNum != 3)
      return HookResult.Continue;

    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
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

      pawn.RemovePlayerItem(weaponOpt.Value);
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

    if (player.TeamNum != 3)
    {
      commandInfo.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Bu komutu sadece {CC.Gold}CT takımı{CC.Default} kullanabilir!");
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
        : "Seçilmedi";
    var currentSecondary = playerSecondaryWeapon.TryGetValue(steamId, out var secondary)
        ? GetWeaponDisplayName(secondary, false)
        : "Seçilmedi";

    var menu = new CenterHtmlMenu("<font color='#6f8083' class='fontSize-l'><img src='https://images.weserv.nl/?url=em-content.zobj.net/source/emoji-one/64/pistol_1f52b.png&w=24&h=24&fit=cover'> CT Silah Menü <img src='https://images.weserv.nl/?url=em-content.zobj.net/source/emoji-one/64/pistol_1f52b.png&w=24&h=24&fit=cover'></font>", this);

    menu.AddMenuOption($"Silah: {currentPrimary}", (p, option) =>
    {
      ShowPrimaryWeaponMenu(p);
    });

    menu.AddMenuOption($"Pistol: {currentSecondary}", (p, option) =>
    {
      ShowSecondaryWeaponMenu(p);
    });

    menu.AddMenuOption($"Kitini Sıfırla", (p, option) =>
    {
      var sid = p.SteamID;
      playerPrimaryWeapon.Remove(sid);
      playerSecondaryWeapon.Remove(sid);
      p.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Silah kitiniz {CC.Red}sıfırlandı{CC.Default}!");
      ShowMainMenu(p);
    });

    MenuManager.OpenCenterHtmlMenu(this, player, menu);
  }

  private void ShowPrimaryWeaponMenu(CCSPlayerController player)
  {
    if (player == null || !player.IsValid)
      return;

    var menu = new CenterHtmlMenu("<font color='#6f8083' class='fontSize-l'><img src='https://images.weserv.nl/?url=em-content.zobj.net/source/emoji-one/64/pistol_1f52b.png&w=24&h=24&fit=cover'> CT Silah Menü <img src='https://images.weserv.nl/?url=em-content.zobj.net/source/emoji-one/64/pistol_1f52b.png&w=24&h=24&fit=cover'></font>", this);

    foreach (var weapon in Config.PrimaryWeapons)
    {
      menu.AddMenuOption($"{CC.Green}{weapon.DisplayName}{CC.Default}", (p, option) =>
      {
        var steamId = p.SteamID;
        playerPrimaryWeapon[steamId] = weapon.WeaponName;
        p.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Silahınız {CC.Green}{weapon.DisplayName}{CC.Default} olarak ayarlandı!");
        ShowMainMenu(p);
      });
    }

    MenuManager.OpenCenterHtmlMenu(this, player, menu);
  }

  private void ShowSecondaryWeaponMenu(CCSPlayerController player)
  {
    if (player == null || !player.IsValid)
      return;

    var menu = new CenterHtmlMenu("<font color='#6f8083' class='fontSize-l'><img src='https://images.weserv.nl/?url=em-content.zobj.net/source/emoji-one/64/pistol_1f52b.png&w=24&h=24&fit=cover'> CT Silah Menü <img src='https://images.weserv.nl/?url=em-content.zobj.net/source/emoji-one/64/pistol_1f52b.png&w=24&h=24&fit=cover'></font>", this);

    foreach (var weapon in Config.SecondaryWeapons)
    {
      menu.AddMenuOption($"{CC.Green}{weapon.DisplayName}{CC.Default}", (p, option) =>
      {
        var steamId = p.SteamID;
        playerSecondaryWeapon[steamId] = weapon.WeaponName;
        p.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Pistolunuz {CC.Green}{weapon.DisplayName}{CC.Default} olarak ayarlandı!");
        ShowMainMenu(p);
      });
    }

    MenuManager.OpenCenterHtmlMenu(this, player, menu);
  }

  private string GetWeaponDisplayName(string weaponName, bool isPrimary)
  {
    var weaponList = isPrimary ? Config.PrimaryWeapons : Config.SecondaryWeapons;
    return weaponList.FirstOrDefault(w => w.WeaponName == weaponName)?.DisplayName ?? weaponName;
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
