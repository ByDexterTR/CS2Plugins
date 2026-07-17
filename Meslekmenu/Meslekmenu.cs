using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace Meslekmenu;

public class MeslekmenuConfig : BasePluginConfig
{
  [JsonPropertyName("doktor_enabled")] public bool DoktorEnabled { get; set; } = true;
  [JsonPropertyName("doktor_regen")] public int DoktorRegen { get; set; } = 50;
  [JsonPropertyName("doktor_drop_healthshot")] public bool DoktorDropHealthshot { get; set; } = true;

  [JsonPropertyName("flash_enabled")] public bool FlashEnabled { get; set; } = true;
  [JsonPropertyName("flash_speed")] public float FlashSpeed { get; set; } = 3f;
  [JsonPropertyName("flash_duration")] public int FlashDuration { get; set; } = 5;

  [JsonPropertyName("bombaci_enabled")] public bool BombaciEnabled { get; set; } = true;
  [JsonPropertyName("bombaci_give_smoke")] public bool BombaciGiveSmoke { get; set; } = true;
  [JsonPropertyName("bombaci_give_grenade")] public bool BombaciGiveGrenade { get; set; } = true;
  [JsonPropertyName("bombaci_give_flashbang")] public bool BombaciGiveFlashbang { get; set; } = true;
  [JsonPropertyName("bombaci_give_molotov")] public bool BombaciGiveMolotov { get; set; } = true;

  [JsonPropertyName("rambo_enabled")] public bool RamboEnabled { get; set; } = true;
  [JsonPropertyName("rambo_hp")] public int RamboHp { get; set; } = 150;
  [JsonPropertyName("rambo_armor")] public int RamboArmor { get; set; } = 100;
  [JsonPropertyName("rambo_helmet")] public bool RamboHelmet { get; set; } = true;
  [JsonPropertyName("rambo_fix")] public bool RamboFix { get; set; } = true;

  [JsonPropertyName("zeus_enabled")] public bool ZeusEnabled { get; set; } = true;
  [JsonPropertyName("zeus_recharge_taser")] public int ZeusRechargeTaser { get; set; } = 30;
  [JsonPropertyName("zeus_drop_taser")] public bool ZeusDropTaser { get; set; } = true;

  [JsonPropertyName("meslek_cmd")] public string MeslekCommands { get; set; } = "css_meslekmenu,css_meslek,css_job,css_jobmenu";
}

public class MeslekmenuPlugin : BasePlugin, IPluginConfig<MeslekmenuConfig>
{
  public override string ModuleName => "Meslekmenu";
  public override string ModuleVersion => "1.0.7";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];
  public MeslekmenuConfig Config { get; set; } = new MeslekmenuConfig();

  private Dictionary<ulong, bool> meslekAktif = new();

  private WasdMenuManager _menus = null!;

  public void OnConfigParsed(MeslekmenuConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    _menus = new WasdMenuManager(this,
      () => Localizer["menu.scroll"],
      () => Localizer["menu.select"],
      () => Localizer["menu.exit"]);
    RegisterEventHandler<EventRoundStart>(OnRoundStart);

    foreach (var name in Util.Split(Config.MeslekCommands))
      AddCommand(name, "Meslek menusunu acar", OnMeslekCommand);
  }

  public override void Unload(bool hotReload)
  {
    _menus.Clear();
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    var healthshothp = ConVar.Find("healthshot_health");
    if (healthshothp != null)
      healthshothp.SetValue(Config.DoktorRegen);

    var drophealthshot = ConVar.Find("mp_death_drop_healthshot");
    if (drophealthshot != null)
      drophealthshot.SetValue(Config.DoktorDropHealthshot);

    var taserRecharge = ConVar.Find("mp_taser_recharge_time");
    if (taserRecharge != null)
      taserRecharge.SetValue((float)Config.ZeusRechargeTaser);

    var dropTaser = ConVar.Find("mp_death_drop_taser");
    if (dropTaser != null)
      dropTaser.SetValue(Config.ZeusDropTaser);

    meslekAktif.Clear();
    return HookResult.Continue;
  }

  public void OnMeslekCommand(CCSPlayerController? player, CommandInfo commandInfo)
  {
    if (player == null || !player.IsValid)
      return;

    if (!CanSelect(player))
      return;

    if (commandInfo.ArgCount >= 2)
    {
      TryApplyProfession(player, commandInfo.GetArg(1).ToLower());
      return;
    }

    ShowMeslekMenu(player);
  }

  private bool CanSelect(CCSPlayerController player)
  {
    if (!IsAlive(player))
    {
      PrintPrefix(player, Localizer["meslekmenu.alive_only"]);
      return false;
    }

    if (player.Team != CsTeam.Terrorist)
    {
      PrintPrefix(player, Localizer["meslekmenu.t_only"]);
      return false;
    }

    if (meslekAktif.TryGetValue(player.SteamID, out bool secildi) && secildi)
    {
      PrintPrefix(player, Localizer["meslekmenu.already_selected"]);
      return false;
    }

    return true;
  }

  private void ShowMeslekMenu(CCSPlayerController player)
  {
    var items = new List<WasdItem>();

    if (Config.DoktorEnabled)
      items.Add(BuildItem("doktor", Localizer["meslekmenu.item_doktor", Config.DoktorRegen]));
    if (Config.FlashEnabled)
      items.Add(BuildItem("flash", Localizer["meslekmenu.item_flash", Config.FlashDuration, Config.FlashSpeed]));
    if (Config.BombaciEnabled && (Config.BombaciGiveSmoke || Config.BombaciGiveGrenade || Config.BombaciGiveFlashbang || Config.BombaciGiveMolotov))
      items.Add(BuildItem("bombaci", Localizer["meslekmenu.item_bombaci", GetBombaciNades()]));
    if (Config.RamboEnabled)
      items.Add(BuildItem("rambo", Localizer["meslekmenu.item_rambo", Config.RamboHp, Config.RamboArmor]));
    if (Config.ZeusEnabled)
      items.Add(BuildItem("zeus", Localizer["meslekmenu.item_zeus"]));

    if (items.Count == 0)
    {
      PrintPrefix(player, Localizer["meslekmenu.no_active"]);
      return;
    }

    _menus.Open(player, Localizer["meslekmenu.menu_title"], items);
  }

  private WasdItem BuildItem(string key, string text)
  {
    return new WasdItem
    {
      Text = text,
      OnSelect = p =>
      {
        if (!CanSelect(p))
        {
          _menus.Close(p);
          return;
        }

        if (TryApplyProfession(p, key))
          _menus.Close(p);
      }
    };
  }

  private bool TryApplyProfession(CCSPlayerController player, string meslek)
  {
    switch (meslek)
    {
      case "doktor":
      case "doctor":
        if (!Config.DoktorEnabled)
        {
          PrintPrefix(player, Localizer["meslekmenu.disabled", Localizer["meslekmenu.prof_doktor"]]);
          return false;
        }
        player.GiveNamedItem("weapon_healthshot");
        PrintPrefix(player, Localizer["meslekmenu.profession_changed", Localizer["meslekmenu.prof_doktor"]]);
        break;

      case "flash":
        if (!Config.FlashEnabled)
        {
          PrintPrefix(player, Localizer["meslekmenu.disabled", Localizer["meslekmenu.prof_flash"]]);
          return false;
        }
        var pawn = player.PlayerPawn.Value as CCSPlayerPawn;
        if (pawn == null)
          return false;

        float normalSpeed = pawn.VelocityModifier;
        pawn.VelocityModifier = Config.FlashSpeed;
        PrintPrefix(player, Localizer["meslekmenu.profession_changed", Localizer["meslekmenu.prof_flash"]]);
        AddTimer(Config.FlashDuration, () =>
        {
          if (pawn.IsValid)
            pawn.VelocityModifier = normalSpeed;
        }, TimerFlags.STOP_ON_MAPCHANGE);
        break;

      case "bombacı":
      case "bombaci":
      case "bomber":
        if (!Config.BombaciEnabled)
        {
          PrintPrefix(player, Localizer["meslekmenu.disabled", Localizer["meslekmenu.prof_bombaci"]]);
          return false;
        }
        var bombs = new List<string>();
        if (Config.BombaciGiveSmoke) bombs.Add("weapon_smokegrenade");
        if (Config.BombaciGiveGrenade) bombs.Add("weapon_hegrenade");
        if (Config.BombaciGiveFlashbang) bombs.Add("weapon_flashbang");
        if (Config.BombaciGiveMolotov) bombs.Add("weapon_molotov");
        if (bombs.Count == 0)
        {
          PrintPrefix(player, Localizer["meslekmenu.disabled", Localizer["meslekmenu.prof_bombaci"]]);
          return false;
        }
        var randomBomb = bombs[Random.Shared.Next(bombs.Count)];
        player.GiveNamedItem(randomBomb);
        PrintPrefix(player, Localizer["meslekmenu.profession_changed", Localizer["meslekmenu.prof_bombaci"]]);
        break;

      case "rambo":
        if (!Config.RamboEnabled)
        {
          PrintPrefix(player, Localizer["meslekmenu.disabled", Localizer["meslekmenu.prof_rambo"]]);
          return false;
        }
        var ramboPawn = player.PlayerPawn.Value as CCSPlayerPawn;
        if (ramboPawn == null)
          return false;

        if (Config.RamboFix && ramboPawn.Health < 100)
        {
          PrintPrefix(player, Localizer["meslekmenu.rambo_low_hp"]);
          return false;
        }
        ramboPawn.Health = Config.RamboHp;
        ramboPawn.MaxHealth = Config.RamboHp;
        ramboPawn.ArmorValue = Config.RamboArmor;
        Utilities.SetStateChanged(ramboPawn, "CBaseEntity", "m_iHealth");
        Utilities.SetStateChanged(ramboPawn, "CBaseEntity", "m_iMaxHealth");
        Utilities.SetStateChanged(ramboPawn, "CCSPlayerPawn", "m_ArmorValue");
        if (Config.RamboHelmet)
          player.GiveNamedItem("item_assaultsuit");

        PrintPrefix(player, Localizer["meslekmenu.profession_changed", Localizer["meslekmenu.prof_rambo"]]);
        break;

      case "zeus":
        if (!Config.ZeusEnabled)
        {
          PrintPrefix(player, Localizer["meslekmenu.disabled", Localizer["meslekmenu.prof_zeus"]]);
          return false;
        }
        player.GiveNamedItem("weapon_taser");
        PrintPrefix(player, Localizer["meslekmenu.profession_changed", Localizer["meslekmenu.prof_zeus"]]);
        break;

      default:
        PrintPrefix(player, Localizer["meslekmenu.unknown"]);
        return false;
    }

    meslekAktif[player.SteamID] = true;
    return true;
  }

  private void PrintPrefix(CCSPlayerController player, string message)
  {
    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {message}");
  }

  private string GetBombaciNades()
  {
    var nades = new List<string>();
    if (Config.BombaciGiveSmoke) nades.Add("Smoke");
    if (Config.BombaciGiveGrenade) nades.Add("HE");
    if (Config.BombaciGiveFlashbang) nades.Add("Flash");
    if (Config.BombaciGiveMolotov) nades.Add("Molotov");

    return nades.Count > 0 ? string.Join(", ", nades) : Localizer["meslekmenu.none"].ToString();
  }

  static bool IsAlive(CCSPlayerController? player)
  {
    return player?.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
  }
}
