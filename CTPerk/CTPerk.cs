using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;

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
  public override string ModuleVersion => "1.0.2";
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
    RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
    RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    activePerks.Clear();
    usedSelections = 0;
    var tCount = Utilities.GetPlayers().Count(p => p?.IsValid == true && p.Team == CsTeam.Terrorist);
    allowedSelections = GetAllowedUsages(tCount);
    BroadcastCT(Localizer["ctperk.rights", allowedSelections]);
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
    if (player == null || !player.IsValid || player.IsBot) return;
    ShowPerkMenu(player);
  }

  private int GetAllowedUsages(int tCount)
  {
    var sorted = Config.SelectionRights.OrderByDescending(r => r.TCount).ToList();
    foreach (var right in sorted)
    {
      if (tCount >= right.TCount) return right.Hak;
    }
    return sorted.LastOrDefault()?.Hak ?? 0;
  }

  private void ShowPerkMenu(CCSPlayerController player)
  {
    if (player == null || !player.IsValid) return;

    CenterHtmlMenu menu = new("<font color='#f7e882' class='fontSize-l'><img src='https://images.weserv.nl/?url=em-content.zobj.net/source/lg/307/shield_1f6e1-fe0f.png&w=24&h=24&fit=cover'> CT Perk (" + usedSelections + "/" + allowedSelections + ") <img src='https://images.weserv.nl/?url=em-content.zobj.net/source/lg/307/shield_1f6e1-fe0f.png&w=24&h=24&fit=cover'></font>", this);
    int hp = Config.PerkHpArmorHp;
    int armor = Config.PerkHpArmorArmor;
    int lifestealPct = (int)(Config.PerkLifestealRatio * 100);
    int dmgRedPct = (int)(Config.PerkDamageReductionRatio * 100);
    int dmgBoostPct = (int)((Config.PerkDamageBoostRatio - 1f) * 100);
    if (Config.EnabledPerkHpArmor)
      AddPerkOption(menu, PerkType.Health, Localizer["ctperk.perk_hp_armor", hp, armor], () => ActivatePerk(PerkType.Health));
    if (Config.EnabledPerkLifesteal)
      AddPerkOption(menu, PerkType.Lifesteal, Localizer["ctperk.perk_lifesteal", lifestealPct], () => ActivatePerk(PerkType.Lifesteal));
    if (Config.EnabledPerkInfAmmo)
      AddPerkOption(menu, PerkType.InfiniteAmmo, Localizer["ctperk.perk_infinite_ammo"], () => ActivatePerk(PerkType.InfiniteAmmo));
    if (Config.EnabledPerkDamageReduction)
      AddPerkOption(menu, PerkType.DamageReduction, Localizer["ctperk.perk_damage_reduction", dmgRedPct], () => ActivatePerk(PerkType.DamageReduction));
    if (Config.EnabledPerkDamageBoost)
      AddPerkOption(menu, PerkType.DamageBoost, Localizer["ctperk.perk_damage_boost", dmgBoostPct], () => ActivatePerk(PerkType.DamageBoost));

    MenuManager.OpenCenterHtmlMenu(this, player, menu);
  }

  private void AddPerkOption(CenterHtmlMenu menu, PerkType type, string display, Action action)
  {
    bool already = activePerks.Contains(type);
    string label = already ? $"<font color='green'>{display} ✔</font>" : display;
    menu.AddMenuOption(label, (p, o) =>
    {
      if (already) return;


      if (usedSelections >= allowedSelections)
      {
        p.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {Localizer["ctperk.no_rights"]}");
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
        var pawn = ct.PlayerPawn?.Value;
        if (pawn?.IsValid != true || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
          continue;

        ApplyHealthArmor(ct, pawn);
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
        BroadcastCT(Localizer["ctperk.perk_active_hp_armor", hp, ar]);
        break;
      case PerkType.Lifesteal:
        BroadcastCT(Localizer["ctperk.perk_active_lifesteal", ls]);
        break;
      case PerkType.InfiniteAmmo:
        BroadcastCT(Localizer["ctperk.perk_active_infammo"]);
        break;
      case PerkType.DamageReduction:
        BroadcastCT(Localizer["ctperk.perk_active_dmgreduction", dr]);
        break;
      case PerkType.DamageBoost:
        BroadcastCT(Localizer["ctperk.perk_active_dmgboost", db]);
        break;
      default:
        BroadcastCT(Localizer["ctperk.perk_active_generic"]);
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
    if (!activePerks.Contains(PerkType.Health)) return HookResult.Continue;

    var player = @event.Userid;
    if (player?.IsValid != true || player.IsBot || player.Team != CsTeam.CounterTerrorist) return HookResult.Continue;

    var pawn = player.PlayerPawn?.Value;
    if (pawn?.IsValid != true || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) return HookResult.Continue;

    Server.NextFrame(() =>
    {
      if (pawn?.IsValid != true) return;
      ApplyHealthArmor(player, pawn);
    });

    return HookResult.Continue;
  }

  private void ApplyHealthArmor(CCSPlayerController player, CCSPlayerPawn pawn)
  {
    if (pawn?.IsValid != true) return;

    int hp = Config.PerkHpArmorHp;
    int armor = Config.PerkHpArmorArmor;

    if (pawn.Health >= 100)
    {
      pawn.Health = hp;
      pawn.MaxHealth = hp;
      Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }
    else
    {
      pawn.MaxHealth = hp;
      Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }

    pawn.ArmorValue = armor;
    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
    player.GiveNamedItem("item_assaultsuit");
  }

  private void SetAmmo(CBasePlayerWeapon? weapon)
  {
    if (weapon?.IsValid != true) return;

    var data = weapon.As<CCSWeaponBase>().VData;
    int maxClip = data?.MaxClip1 ?? weapon.Clip1;

    if (weapon.Clip1 <= maxClip / 2)
    {
      weapon.Clip1 = maxClip;
      Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
    }
  }

  private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
  {
    if (!activePerks.Contains(PerkType.InfiniteAmmo)) return HookResult.Continue;
    var player = @event.Userid;
    if (player == null || !player.IsValid || player.Team != CsTeam.CounterTerrorist || player.IsBot) return HookResult.Continue;
    var pawn = player.PlayerPawn.Value;
    if (pawn?.IsValid != true || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) return HookResult.Continue;
    var weapon = pawn.WeaponServices?.ActiveWeapon.Get();
    if (weapon == null || !weapon.IsValid) return HookResult.Continue;
    if (weapon.DesignerName.Contains("knife") || weapon.DesignerName.Contains("bayonet") || weapon.DesignerName.Contains("taser")) return HookResult.Continue;
    SetAmmo(weapon);
    return HookResult.Continue;
  }

  private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
  {
    var attacker = @event.Attacker;
    var victim = @event.Userid;
    if (victim?.IsValid != true) return HookResult.Continue;

    int dmgDealt = @event.DmgHealth;
    if (dmgDealt <= 0) return HookResult.Continue;

    // Lifesteal: CT saldırgan verdiği hasarın bir kısmını can olarak geri alır
    if (activePerks.Contains(PerkType.Lifesteal) &&
        attacker?.IsValid == true && !attacker.IsBot &&
        attacker.UserId != victim.UserId &&
        attacker.Team == CsTeam.CounterTerrorist)
    {
      var attackerPawn = attacker.PlayerPawn?.Value;
      if (attackerPawn?.IsValid == true && attackerPawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
      {
        float lifesteal = Config.PerkLifestealRatio;
        if (lifesteal > 0 && lifesteal <= 1.0f)
        {
          int healAmount = (int)(dmgDealt * lifesteal);
          if (healAmount > 0)
          {
            attackerPawn.Health = Math.Min(attackerPawn.Health + healAmount, attackerPawn.MaxHealth);
            Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");
          }
        }
      }
    }

    // Hasar Azaltma: CT kurban aldığı hasarın bir kısmını geri iyileşir
    if (activePerks.Contains(PerkType.DamageReduction) && victim.Team == CsTeam.CounterTerrorist)
    {
      var victimPawn = victim.PlayerPawn?.Value;
      if (victimPawn?.IsValid == true && victimPawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
      {
        float reduction = Config.PerkDamageReductionRatio;
        if (reduction > 0f)
        {
          int healBack = (int)(dmgDealt * reduction);
          if (healBack > 0)
          {
            victimPawn.Health = Math.Min(victimPawn.Health + healBack, victimPawn.MaxHealth);
            Utilities.SetStateChanged(victimPawn, "CBaseEntity", "m_iHealth");
          }
        }
      }
    }

    // Hasar Arttırma: CT saldırgan CT olmayan hedefe ek hasar verir
    if (activePerks.Contains(PerkType.DamageBoost) &&
        attacker?.IsValid == true &&
        attacker.Team == CsTeam.CounterTerrorist &&
        victim.Team != CsTeam.CounterTerrorist)
    {
      var victimPawn = victim.PlayerPawn?.Value;
      if (victimPawn?.IsValid == true && victimPawn.LifeState == (byte)LifeState_t.LIFE_ALIVE && victimPawn.Health > 0)
      {
        float mult = Math.Max(1.0f, Config.PerkDamageBoostRatio);
        int extraDmg = (int)(dmgDealt * (mult - 1f));
        if (extraDmg > 0)
        {
          int newHp = victimPawn.Health - extraDmg;
          victimPawn.Health = Math.Max(0, newHp);
          Utilities.SetStateChanged(victimPawn, "CBaseEntity", "m_iHealth");
        }
      }
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
