using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace FortniteArmor;

public class FortniteArmorConfig : BasePluginConfig
{
  [JsonPropertyName("absorb_fall_damage")]
  public bool AbsorbFallDamage { get; set; } = false;
}

public class FortniteArmor : BasePlugin, IPluginConfig<FortniteArmorConfig>
{
  public override string ModuleName => "FortniteArmor";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  public FortniteArmorConfig Config { get; set; } = new();

  public void OnConfigParsed(FortniteArmorConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    RegisterListener<OnEntityTakeDamagePre>(HandleEntityDamage);
  }

  private HookResult HandleEntityDamage(CEntityInstance victimEnt, CTakeDamageInfo info)
  {
    if (victimEnt == null || info.Damage <= 0f || victimEnt.DesignerName != "player")
      return HookResult.Continue;

    try
    {
      var pawn = victimEnt as CCSPlayerPawn ?? new CCSPlayerPawn(victimEnt.Handle);
      if (!pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
        return HookResult.Continue;

      int armor = pawn.ArmorValue;
      if (armor <= 0)
        return HookResult.Continue;

      if (!Config.AbsorbFallDamage && (info.BitsDamageType & DamageTypes_t.DMG_FALL) != 0)
        return HookResult.Continue;

      int damage = (int)info.Damage;
      if (damage <= 0)
        return HookResult.Continue;

      int absorbed = Math.Min(damage, armor);
      pawn.ArmorValue = armor - absorbed;
      Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
      info.Damage = damage - absorbed;

      return HookResult.Continue;
    }
    catch
    {
      return HookResult.Continue;
    }
  }
}
