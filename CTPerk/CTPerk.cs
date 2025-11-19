using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace CTPerk;

public class SelectionRight
{
  [JsonPropertyName("t_count")]
  public int TCount { get; set; } = 8;

  [JsonPropertyName("hak")]
  public int Hak { get; set; } = 1;
}

public class CTPerkConfig : BasePluginConfig
{
  [JsonPropertyName("chat_prefix")]
  public string ChatPrefix { get; set; } = "[ByDexter]";
  [JsonPropertyName("perk_hparmor_hp")]
  public int PerkHpArmorHp { get; set; } = 200;

  [JsonPropertyName("perk_hparmor_armor")]
  public int PerkHpArmorArmor { get; set; } = 100;

  [JsonPropertyName("perk_lifesteal_ratio")]
  public float PerkLifestealRatio { get; set; } = 0.25f;

  [JsonPropertyName("perk_damagereducation_ratio")]
  public float PerkDamageReductionRatio { get; set; } = 0.25f;

  [JsonPropertyName("perk_damageboost_ratio")]
  public float PerkDamageBoostRatio { get; set; } = 1.50f;

  [JsonPropertyName("enabled_perk_hparmor")]
  public bool EnabledPerkHpArmor { get; set; } = true;

  [JsonPropertyName("enabled_perk_lifesteal")]
  public bool EnabledPerkLifesteal { get; set; } = true;

  [JsonPropertyName("enabled_perk_infammo")]
  public bool EnabledPerkInfAmmo { get; set; } = true;

  [JsonPropertyName("enabled_perk_damagereducation")]
  public bool EnabledPerkDamageReduction { get; set; } = true;

  [JsonPropertyName("enabled_perk_damageboost")]
  public bool EnabledPerkDamageBoost { get; set; } = true;

  [JsonPropertyName("selection_rights")]
  public List<SelectionRight> SelectionRights { get; set; } = new List<SelectionRight>
  {
    new SelectionRight { TCount = 0, Hak = 1 },
    new SelectionRight { TCount = 9, Hak = 2 },
    new SelectionRight { TCount = 20, Hak = 3 }
  };
}


public enum PerkType
{
  Health,
  Lifesteal,
  InfiniteAmmo,
  DamageReduction,
  DamageBoost
}

public class CTPerk : BasePlugin, IPluginConfig<CTPerkConfig>
{
  public override string ModuleName => "CTPerk";
  public override string ModuleVersion => "1.1.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "[JB] CT Perk System - Komutçu başlangıç özellikleri (tekil seçim)";

  public CTPerkConfig Config { get; set; } = new CTPerkConfig();

  private readonly HashSet<PerkType> activePerks = new();
  private int allowedSelections = 1;
  private int usedSelections = 0;

  public void OnConfigParsed(CTPerkConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
    RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
    RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
  }

  public override void Unload(bool hotReload)
  {
    try { VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre); } catch { }
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    activePerks.Clear();
    usedSelections = 0;
    var tCount = Utilities.GetPlayers().Count(p => p?.IsValid == true && p.Team == CsTeam.Terrorist);
    allowedSelections = GetAllowedUsages(tCount);
    BroadcastCT($"Perk hakkınız: {CC.Gold}{allowedSelections}");
    foreach (var ct in Utilities.GetPlayers().Where(pl => pl?.IsValid == true && !pl.IsBot && pl.Team == CsTeam.CounterTerrorist))
    {
      var pawn = ct.PlayerPawn.Value;
      if (pawn?.IsValid != true || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
        continue;
      if (pawn.Health > 100 || pawn.MaxHealth > 100)
      {
        pawn.Health = 100;
        pawn.MaxHealth = 100;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
      }
      if (pawn.ArmorValue > 0)
      {
        pawn.ArmorValue = 0;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
      }
    }
    return HookResult.Continue;
  }

  [ConsoleCommand("css_ctperk", "CT Perk seçim menüsü")]
  [RequiresPermissionsOr("@css/generic", "@jailbreak/warden")]
  public void OnCTPerkCommand(CCSPlayerController? player, CommandInfo commandInfo)
  {
    if (player == null || !player.IsValid || player.IsBot)
      return;
    ShowPerkMenu(player);
  }

  private int GetAllowedUsages(int tCount)
  {
    var sorted = Config.SelectionRights.OrderByDescending(r => r.TCount).ToList();
    foreach (var right in sorted)
    {
      if (tCount >= right.TCount)
        return right.Hak;
    }
    return sorted.LastOrDefault()?.Hak ?? 0;
  }

  private void ShowPerkMenu(CCSPlayerController player)
  {
    if (player == null || !player.IsValid)
      return;

    CenterHtmlMenu menu = new("<font color='#f7e882' class='fontSize-l'><img src='https://images.weserv.nl/?url=em-content.zobj.net/source/lg/307/shield_1f6e1-fe0f.png&w=24&h=24&fit=cover'> CT Perk (" + usedSelections + "/" + allowedSelections + ") <img src='https://images.weserv.nl/?url=em-content.zobj.net/source/lg/307/shield_1f6e1-fe0f.png&w=24&h=24&fit=cover'></font>", this);
    int hp = Config.PerkHpArmorHp;
    int armor = Config.PerkHpArmorArmor;
    int lifestealPct = (int)(Config.PerkLifestealRatio * 100);
    int dmgRedPct = (int)(Config.PerkDamageReductionRatio * 100);
    int dmgBoostPct = (int)((Config.PerkDamageBoostRatio - 1f) * 100);
    if (Config.EnabledPerkHpArmor)
      AddPerkOption(menu, PerkType.Health, $"{hp} Can & {armor} Armor", () => ActivatePerk(PerkType.Health));
    if (Config.EnabledPerkLifesteal)
      AddPerkOption(menu, PerkType.Lifesteal, $"Can Çalma (%{lifestealPct})", () => ActivatePerk(PerkType.Lifesteal));
    if (Config.EnabledPerkInfAmmo)
      AddPerkOption(menu, PerkType.InfiniteAmmo, "Sınırsız Mermi", () => ActivatePerk(PerkType.InfiniteAmmo));
    if (Config.EnabledPerkDamageReduction)
      AddPerkOption(menu, PerkType.DamageReduction, $"Hasar Azaltma (%{dmgRedPct})", () => ActivatePerk(PerkType.DamageReduction));
    if (Config.EnabledPerkDamageBoost)
      AddPerkOption(menu, PerkType.DamageBoost, $"Hasar Arttırma (+%{dmgBoostPct})", () => ActivatePerk(PerkType.DamageBoost));

    MenuManager.OpenCenterHtmlMenu(this, player, menu);
  }

  private void AddPerkOption(CenterHtmlMenu menu, PerkType type, string display, Action action)
  {
    bool already = activePerks.Contains(type);
    string label = already ? $"<font color='green'>{display} ✔</font>" : display;
    menu.AddMenuOption(label, (p, o) =>
    {
      if (already)
      {
        return;
      }

      if (usedSelections >= allowedSelections)
      {
        p.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Perk hakkınız {CC.Red}bitti{CC.Default}!");
        MenuManager.CloseActiveMenu(p);
        return;
      }
      action();
      usedSelections++;
      AnnouncePerk(type);
      if (usedSelections < allowedSelections)
      {
        ShowPerkMenu(p);
      }
      else
      {
        MenuManager.CloseActiveMenu(p);
      }
    }, already);
  }

  private void ActivatePerk(PerkType type)
  {
    activePerks.Add(type);
    if (type == PerkType.Health)
    {
      foreach (var ct in Utilities.GetPlayers().Where(pl => pl?.IsValid == true && !pl.IsBot && pl.Team == CsTeam.CounterTerrorist))
      {
        var pawn = ct.PlayerPawn.Value;
        if (pawn?.IsValid == true && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
        {
          ApplyHealthArmor(ct, pawn);
        }
      }
    }
  }

  private void AnnouncePerk(PerkType type)
  {
    int hp = Config.PerkHpArmorHp; int ar = Config.PerkHpArmorArmor;
    int ls = (int)(Config.PerkLifestealRatio * 100);
    int dr = (int)(Config.PerkDamageReductionRatio * 100);
    int db = (int)((Config.PerkDamageBoostRatio - 1f) * 100);

    switch (type)
    {
      case PerkType.Health:
        BroadcastCT($"Perk: {CC.Green}{hp} HP / {ar} Armor{CC.Default} aktif");
        break;
      case PerkType.Lifesteal:
        BroadcastCT($"Perk: {CC.Green}Can Çalma %{ls}{CC.Default} aktif");
        break;
      case PerkType.InfiniteAmmo:
        BroadcastCT($"Perk: {CC.Green}Sınırsız Mermi{CC.Default} aktif");
        break;
      case PerkType.DamageReduction:
        BroadcastCT($"Perk: {CC.Green}Hasar Azaltma %{dr}{CC.Default} aktif");
        break;
      case PerkType.DamageBoost:
        BroadcastCT($"Perk: {CC.Green}Hasar Arttırma %{db}{CC.Default} aktif");
        break;
      default:
        BroadcastCT($"Perk: {CC.Green}Aktif{CC.Default}");
        break;
    }
  }

  private void BroadcastCT(string message)
  {
    foreach (var ct in Utilities.GetPlayers().Where(pl => pl?.IsValid == true && !pl.IsBot && pl.Team == CsTeam.CounterTerrorist))
    {
      ct.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {message}");
    }
  }

  private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || player.IsBot || player.Team != CsTeam.CounterTerrorist)
      return HookResult.Continue;

    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
      return HookResult.Continue;

    Server.NextFrame(() =>
    {
      if (player == null || !player.IsValid || pawn == null || !pawn.IsValid)
        return;

      if (activePerks.Contains(PerkType.Health))
        ApplyHealthArmor(player, pawn);
    });

    return HookResult.Continue;
  }

  private void ApplyHealthArmor(CCSPlayerController player, CCSPlayerPawn pawn)
  {
    int hp = Config.PerkHpArmorHp;
    int armor = Config.PerkHpArmorArmor;
    pawn.Health = hp;
    pawn.MaxHealth = hp;
    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    pawn.ArmorValue = armor;
    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
    player.GiveNamedItem("item_assaultsuit");
  }

  private void SetAmmo(CBasePlayerWeapon? weapon)
  {
    if (weapon == null || !weapon.IsValid)
      return;
    var data = weapon.As<CCSWeaponBase>().VData;
    int maxClip = data?.MaxClip1 ?? weapon.Clip1;
    int newClip = weapon.Clip1 + 1;
    if (newClip > maxClip) newClip = maxClip;
    weapon.Clip1 = newClip;
    Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
  }

  private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
  {
    if (!activePerks.Contains(PerkType.InfiniteAmmo))
      return HookResult.Continue;
    var player = @event.Userid;
    if (player == null || !player.IsValid || player.Team != CsTeam.CounterTerrorist || player.IsBot)
      return HookResult.Continue;
    var pawn = player.PlayerPawn.Value;
    if (pawn?.IsValid != true || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
      return HookResult.Continue;
    var weapon = pawn.WeaponServices?.ActiveWeapon.Get();
    if (weapon == null || !weapon.IsValid)
      return HookResult.Continue;
    if (weapon.DesignerName.Contains("knife") || weapon.DesignerName.Contains("bayonet"))
      return HookResult.Continue;
    SetAmmo(weapon);
    return HookResult.Continue;
  }

  private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
  {
    var attacker = @event.Attacker;
    var victim = @event.Userid;

    if (attacker == null || !attacker.IsValid || attacker.IsBot)
      return HookResult.Continue;

    if (victim == null || !victim.IsValid)
      return HookResult.Continue;

    if (attacker.UserId == victim.UserId)
      return HookResult.Continue;

    if (attacker.Team != CsTeam.CounterTerrorist)
      return HookResult.Continue;

    var attackerPawn = attacker.PlayerPawn.Value;
    if (attackerPawn == null || !attackerPawn.IsValid || attackerPawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
      return HookResult.Continue;
    if (!activePerks.Contains(PerkType.Lifesteal))
      return HookResult.Continue;
    float lifesteal = Config.PerkLifestealRatio;
    if (lifesteal <= 0) return HookResult.Continue;

    var damage = @event.DmgHealth;
    var healAmount = (int)(damage * lifesteal);

    if (healAmount > 0)
    {
      var newHealth = attackerPawn.Health + healAmount;
      var maxHealth = attackerPawn.MaxHealth;

      if (newHealth > maxHealth)
        newHealth = maxHealth;

      attackerPawn.Health = newHealth;
      Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");
    }

    return HookResult.Continue;
  }

  public HookResult OnTakeDamage(DynamicHook h)
  {
    var victimEnt = h.GetParam<CEntityInstance>(0);
    var info = h.GetParam<CTakeDamageInfo>(1);
    if (victimEnt == null || info == null)
      return HookResult.Continue;

    var attackerEnt = info.Attacker.Value;
    CCSPlayerPawn? victimPawn = victimEnt as CCSPlayerPawn ?? new CCSPlayerPawn(victimEnt.Handle);
    CCSPlayerPawn? attackerPawn = attackerEnt as CCSPlayerPawn ?? (attackerEnt != null ? new CCSPlayerPawn(attackerEnt.Handle) : null);

    var victimController = victimPawn?.OriginalController.Value;
    var attackerController = attackerPawn?.OriginalController.Value;

    if (activePerks.Contains(PerkType.DamageReduction) && victimController != null && victimController.IsValid && victimController.Team == CsTeam.CounterTerrorist)
    {
      float reduction = Config.PerkDamageReductionRatio;
      if (reduction > 0.0f)
        info.Damage *= (1f - reduction);
    }

    if (activePerks.Contains(PerkType.DamageBoost) && attackerController != null && attackerController.IsValid && attackerController.Team == CsTeam.CounterTerrorist)
    {
      float mult = Math.Max(1.0f, Config.PerkDamageBoostRatio);
      info.Damage *= mult;
    }

    return HookResult.Continue;
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
