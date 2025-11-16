using System.Drawing;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

public class RedbullConfig : BasePluginConfig
{
  [JsonPropertyName("chat_prefix")]
  public string ChatPrefix { get; set; } = "[ByDexter]";

  [JsonPropertyName("speed")]
  public float Speed { get; set; } = 2.0f;

  [JsonPropertyName("duration")]
  public int Duration { get; set; } = 10;

  [JsonPropertyName("filter_team")]
  public string FilterTeam { get; set; } = "T";

  [JsonPropertyName("player_color")]
  public int[] PlayerColor { get; set; } = [248, 123, 27];

  [JsonPropertyName("round_limiter")]
  public int RoundLimiter { get; set; } = 2;

  [JsonPropertyName("cooldown")]
  public int Cooldown { get; set; } = 15;
}

public class Redbull : BasePlugin, IPluginConfig<RedbullConfig>
{
  public override string ModuleName => "Redbull";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "Redbull hız efekti";

  public RedbullConfig Config { get; set; } = new RedbullConfig();

  private readonly Dictionary<ulong, DateTime> _redbullActive = new();
  private Color _playerColor = Color.FromArgb(255, 248, 123, 27);
  private readonly Dictionary<ulong, int> _roundUses = new();
  private readonly Dictionary<ulong, DateTime> _cooldownUntil = new();

  public void OnConfigParsed(RedbullConfig config)
  {
    Config = config;
    if (config.PlayerColor.Length == 3)
    {
      _playerColor = Color.FromArgb(255, config.PlayerColor[0], config.PlayerColor[1], config.PlayerColor[2]);
    }
  }

  public override void Load(bool hotReload)
  {
    RegisterListener<OnTick>(OnTickSpeed);
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
  }

  [ConsoleCommand("css_redbull", "Redbull hız efekti")]
  public void OnRedbullCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || !IsAlive(player))
      return;

    if (Config.FilterTeam != "Both")
    {
      if ((Config.FilterTeam == "CT" && player.Team != CsTeam.CounterTerrorist) || (Config.FilterTeam == "T" && player.Team != CsTeam.Terrorist))
      {
        player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Bu takımda Red Bull {CC.Gold}içemezsin{CC.Default}!");
        return;
      }
    }

    if (_redbullActive.ContainsKey(player.SteamID))
    {
      player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Red Bull etkileri zaten {CC.Green}aktif{CC.Default}!");
      return;
    }

    if (Config.Cooldown > 0 && _cooldownUntil.TryGetValue(player.SteamID, out var until))
    {
      var now = DateTime.Now;
      if (until > now)
      {
        var remain = (int)Math.Ceiling((until - now).TotalSeconds);
        player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Red Bull bekleme süresi: {CC.Gold}{remain}{CC.Default} sn kaldı!");
        return;
      }
    }

    var limit = Config.RoundLimiter;
    if (limit > 0)
    {
      var used = _roundUses.TryGetValue(player.SteamID, out var val) ? val : 0;
      if (used >= limit)
      {
        player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Red Bull limiti: Her raunt {CC.Gold}{limit}{CC.Default} Red Bull içebilirsin.");
        return;
      }
      _roundUses[player.SteamID] = used + 1;
    }

    _redbullActive[player.SteamID] = DateTime.Now.AddSeconds(Config.Duration);
    player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Redbull: {CC.Green}{Config.Duration} saniye{CC.Default} kanatlandın!");
  }

  private void OnTickSpeed()
  {
    var now = DateTime.Now;
    var expiredPlayers = new List<ulong>();

    foreach (var kvp in _redbullActive)
    {
      var player = Utilities.GetPlayers().FirstOrDefault(p => p?.IsValid == true && p.SteamID == kvp.Key);

      if (player == null || !IsAlive(player) || now >= kvp.Value)
      {
        expiredPlayers.Add(kvp.Key);
        if (player != null && IsAlive(player))
        {
          ResetPlayer(player);
          player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Redbull efekti {CC.Red}bitti{CC.Default}!");
          if (Config.Cooldown > 0)
          {
            _cooldownUntil[player.SteamID] = DateTime.Now.AddSeconds(Config.Cooldown);
          }
        }
        continue;
      }

      SetPlayerEffects(player);
    }

    foreach (var steamId in expiredPlayers)
      _redbullActive.Remove(steamId);
  }

  private void SetPlayerEffects(CCSPlayerController player)
  {
    var pawn = player.PlayerPawn.Value;
    if (pawn?.IsValid != true) return;

    pawn.VelocityModifier = Config.Speed;
    pawn.Render = _playerColor;
    Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
  }

  private void ResetPlayer(CCSPlayerController player)
  {
    var pawn = player.PlayerPawn.Value;
    if (pawn?.IsValid != true) return;

    pawn.VelocityModifier = 1.0f;
    pawn.Render = Color.FromArgb(255, 255, 255, 255);
    Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
  }

  private static bool IsAlive(CCSPlayerController? player)
  {
    var pawn = player?.PlayerPawn.Value;
    return pawn?.IsValid == true && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE && pawn.Health > 0;
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    _roundUses.Clear();
    return HookResult.Continue;
  }

  private HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
  {
    var p = @event.Userid;
    if (p != null && p.IsValid)
      _roundUses[p.SteamID] = 0;
    if (p != null && p.IsValid)
      _cooldownUntil.Remove(p.SteamID);
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
