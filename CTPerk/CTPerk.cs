using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

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

  [JsonPropertyName("ctperk_cmd")]
  public string CtperkCommands { get; set; } = "css_ctperk,css_ctp";

  [JsonPropertyName("ctperk_flag")]
  public string CtperkFlag { get; set; } = "@jailbreak/warden,@css/generic";
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
  public override string ModuleVersion => "1.0.5";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public CTPerkConfig Config { get; set; } = new CTPerkConfig();

  private readonly HashSet<PerkType> activePerks = new();
  private int allowedSelections = 1;
  private int usedSelections = 0;

  private WasdMenuManager _menus = null!;

  public void OnConfigParsed(CTPerkConfig config)
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
    RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
    RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterListener<OnEntityTakeDamagePre>(OnEntityDamagePre);

    foreach (var name in Util.Split(Config.CtperkCommands))
      AddCommand(name, "CT Perk secim menusu", OnCTPerkCommand);
  }

  public override void Unload(bool hotReload)
  {
    _menus.Clear();
  }

  private HookResult OnEntityDamagePre(CEntityInstance victimEnt, CTakeDamageInfo info)
  {
    if (!activePerks.Contains(PerkType.DamageBoost))
      return HookResult.Continue;

    if (victimEnt == null)
      return HookResult.Continue;

    var attackerPawn = info.Attacker.Value as CCSPlayerPawn;
    var attackerController = attackerPawn?.OriginalController.Value;
    if (attackerController == null || !attackerController.IsValid || attackerController.Team != CsTeam.CounterTerrorist)
      return HookResult.Continue;

    var victimPawn = victimEnt as CCSPlayerPawn ?? new CCSPlayerPawn(victimEnt.Handle);
    var victimController = victimPawn?.OriginalController.Value;
    if (victimController == null || !victimController.IsValid || victimController.Team == CsTeam.CounterTerrorist)
      return HookResult.Continue;

    float mult = Math.Max(1.0f, Config.PerkDamageBoostRatio);
    if (mult > 1.0f)
      info.Damage *= mult;

    return HookResult.Continue;
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

  public void OnCTPerkCommand(CCSPlayerController? player, CommandInfo commandInfo)
  {
    if (player == null || !player.IsValid || player.IsBot) return;
    if (!Util.HasAccess(player, Config.CtperkFlag)) return;
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

    int hp = Config.PerkHpArmorHp;
    int armor = Config.PerkHpArmorArmor;
    int lifestealPct = (int)(Config.PerkLifestealRatio * 100);
    int dmgRedPct = (int)(Config.PerkDamageReductionRatio * 100);
    int dmgBoostPct = (int)((Config.PerkDamageBoostRatio - 1f) * 100);

    var items = new List<WasdItem>();
    if (Config.EnabledPerkHpArmor)
      items.Add(BuildPerkItem(PerkType.Health, Localizer["ctperk.perk_hp_armor", hp, armor], () => ActivatePerk(PerkType.Health)));
    if (Config.EnabledPerkLifesteal)
      items.Add(BuildPerkItem(PerkType.Lifesteal, Localizer["ctperk.perk_lifesteal", lifestealPct], () => ActivatePerk(PerkType.Lifesteal)));
    if (Config.EnabledPerkInfAmmo)
      items.Add(BuildPerkItem(PerkType.InfiniteAmmo, Localizer["ctperk.perk_infinite_ammo"], () => ActivatePerk(PerkType.InfiniteAmmo)));
    if (Config.EnabledPerkDamageReduction)
      items.Add(BuildPerkItem(PerkType.DamageReduction, Localizer["ctperk.perk_damage_reduction", dmgRedPct], () => ActivatePerk(PerkType.DamageReduction)));
    if (Config.EnabledPerkDamageBoost)
      items.Add(BuildPerkItem(PerkType.DamageBoost, Localizer["ctperk.perk_damage_boost", dmgBoostPct], () => ActivatePerk(PerkType.DamageBoost)));

    _menus.Open(player, Localizer["ctperk.menu_title", usedSelections, allowedSelections], items);
  }

  private WasdItem BuildPerkItem(PerkType type, string display, Action action)
  {
    bool already = activePerks.Contains(type);
    return new WasdItem
    {
      Text = already ? $"<font color='#76C97A'>{display} ✔</font>" : display,
      Enabled = !already,
      OnSelect = p =>
      {
        if (usedSelections >= allowedSelections)
        {
          p.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctperk.no_rights"]}");
          _menus.Close(p);
          return;
        }
        action();
        usedSelections++;
        AnnouncePerk(type);
        if (usedSelections < allowedSelections)
          ShowPerkMenu(p);
        else
          _menus.Close(p);
      }
    };
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
      ct.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {message}");
    }
  }

  private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player?.IsValid != true || player.IsBot || player.Team != CsTeam.CounterTerrorist) return HookResult.Continue;

    var pawn = player.PlayerPawn?.Value;
    if (pawn?.IsValid != true || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) return HookResult.Continue;

    bool healthPerkActive = activePerks.Contains(PerkType.Health);

    Server.NextFrame(() =>
    {
      if (pawn?.IsValid != true) return;

      if (healthPerkActive)
      {
        ApplyHealthArmor(player, pawn);
      }
      else if (pawn.MaxHealth > 100 || pawn.Health > 100)
      {
        pawn.Health = 100;
        pawn.MaxHealth = 100;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
      }
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
    return HookResult.Continue;
  }

}
