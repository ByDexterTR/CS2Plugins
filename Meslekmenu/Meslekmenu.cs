using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;

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

  [JsonPropertyName("chat_prefix")]
  public string ChatPrefix { get; set; } = "[ByDexter]";
}

[MinimumApiVersion(80)]
public class MeslekmenuPlugin : BasePlugin, IPluginConfig<MeslekmenuConfig>
{
  public override string ModuleName => "Meslekmenu";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "Meslek menü";
  public MeslekmenuConfig Config { get; set; } = new MeslekmenuConfig();

  private Dictionary<uint, bool> meslekAktif = new();

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    meslekAktif.Clear();
    return HookResult.Continue;
  }

  public void OnConfigParsed(MeslekmenuConfig config)
  {
    Config = config;

    var healthshothp = ConVar.Find("healthshot_health");
    if (healthshothp != null)
      healthshothp.SetValue(Config.DoktorRegen);

    var drophealthshot = ConVar.Find("mp_death_drop_healthshot");
    if (drophealthshot != null)
      drophealthshot.SetValue(Config.DoktorDropHealthshot);
  }

  public override void Load(bool hotReload)
  {
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
  }

  [ConsoleCommand("css_meslek", "Meslek seçimi ve yardım")]
  public void OnMeslekCommand(CCSPlayerController? player, CommandInfo commandInfo)
  {
    if (player == null || !player.IsValid)
      return;

    if (commandInfo.ArgCount < 2)
    {
      var helpLines = new List<string>();

      if (Config.DoktorEnabled)
        helpLines.Add($"!meslek {CC.Green}Doktor{CC.Default}: {Config.DoktorRegen} sağlık aşısı (weapon_healthshot)");
      if (Config.FlashEnabled)
        helpLines.Add($"!meslek {CC.Green}Flash{CC.Default}: {Config.FlashDuration} saniye flash efekti (Hız x{Config.FlashSpeed})");
      if (Config.BombaciEnabled && (Config.BombaciGiveSmoke || Config.BombaciGiveGrenade || Config.BombaciGiveFlashbang || Config.BombaciGiveMolotov))
        helpLines.Add($"!meslek {CC.Green}Bombacı{CC.Default}: Rastgele {GetBombaciNades()} verilir.");
      if (Config.RamboEnabled)
        helpLines.Add($"!meslek {CC.Green}Rambo{CC.Default}: {Config.RamboHp} can, {Config.RamboArmor} armor.");


      if (helpLines.Count == 0)
        helpLines.Add($"{CC.Gold}Hiçbir meslek aktif değil!{CC.Default}");

      foreach (var line in helpLines)
      {
        commandInfo.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {line}");
      }
      return;
    }

    if (!IsAlive(player))
    {
      commandInfo.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Bu komutu sadece {CC.Gold}canlı oyuncular{CC.Default} kullanabilir!");
      return;
    }

    if (player.TeamNum != 2)
    {
      commandInfo.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Bu komutu sadece {CC.Gold}Terörist takımı{CC.Default} kullanabilir!");
      return;
    }

    if (meslekAktif.TryGetValue(player.Index, out bool secildi) && secildi)
    {
      commandInfo.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Bu turda zaten meslek seçtin!");
      return;
    }

    string meslek = commandInfo.GetArg(1).ToLower();

    switch (meslek)
    {
      case "doktor":
        if (!Config.DoktorEnabled)
        {
          commandInfo.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Doktor mesleği kapalı.");
          return;
        }
        player.GiveNamedItem("weapon_healthshot");
        commandInfo.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Mesleğin değiştirildi: {CC.Green}Doktor");
        break;

      case "flash":
        if (!Config.FlashEnabled)
        {
          commandInfo.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Flash mesleği kapalı.");
          return;
        }
        var pawn = player.PlayerPawn.Value as CCSPlayerPawn;
        if (pawn != null)
        {
          float normalSpeed = pawn.VelocityModifier;
          pawn.VelocityModifier = Config.FlashSpeed;
          commandInfo.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Mesleğin değiştirildi: {CC.Green}Flash");
          AddTimer(Config.FlashDuration, () =>
          {
            if (pawn.IsValid)
              pawn.VelocityModifier = normalSpeed;
          });
        }
        break;

      case "bombacı":
        if (!Config.BombaciEnabled)
        {
          commandInfo.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Bombacı mesleği kapalı.");
          return;
        }
        var bombs = new List<string>();
        if (Config.BombaciGiveSmoke) bombs.Add("weapon_smokegrenade");
        if (Config.BombaciGiveGrenade) bombs.Add("weapon_hegrenade");
        if (Config.BombaciGiveFlashbang) bombs.Add("weapon_flashbang");
        if (Config.BombaciGiveMolotov) bombs.Add("weapon_molotov");
        if (bombs.Count == 0)
        {
          commandInfo.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Bombacı mesleği kapalı.");
          return;
        }
        var rnd = new Random();
        var randomBomb = bombs[rnd.Next(bombs.Count)];
        player.GiveNamedItem(randomBomb);
        commandInfo.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Mesleğin değiştirildi: {CC.Green}Bombacı");
        break;

      case "rambo":
        if (!Config.RamboEnabled)
        {
          commandInfo.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Rambo mesleği kapalı.");
          return;
        }
        var ramboPawn = player.PlayerPawn.Value as CCSPlayerPawn;
        if (ramboPawn != null)
        {
          if (Config.RamboFix && ramboPawn.Health < 100)
          {
            commandInfo.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Rambo mesleğini seçemezsin: {CC.Gold}Canın 100ün altında{CC.Default}!");
            return;
          }
          ramboPawn.Health = Config.RamboHp;
          ramboPawn.ArmorValue = Config.RamboArmor;
          if (Config.RamboHelmet)
            player.GiveNamedItem("item_kevlar");

          commandInfo.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Mesleğin değiştirildi: {CC.Green}Rambo");
        }
        break;

      default:
        commandInfo.ReplyToCommand($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Bilinmeyen meslek! Yardım için !meslek yazınız.");
        break;
    }

    meslekAktif[player.Index] = true;
  }

  private string GetBombaciNades()
  {
    var nades = new List<string>();
    if (Config.BombaciGiveSmoke) nades.Add("Smoke");
    if (Config.BombaciGiveGrenade) nades.Add("HE");
    if (Config.BombaciGiveFlashbang) nades.Add("Flash");
    if (Config.BombaciGiveMolotov) nades.Add("Molotov");

    return nades.Count > 0 ? string.Join(", ", nades) : "Yok";
  }

  static bool IsAlive(CCSPlayerController? player)
  {
    return player?.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
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