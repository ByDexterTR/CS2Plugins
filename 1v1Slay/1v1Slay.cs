using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace OneVOneSlay;

public class OneVOneSlayConfig : BasePluginConfig
{
  [JsonPropertyName("min_players")]
  public int MinPlayers { get; set; } = 3;

  [JsonPropertyName("countdown_time")]
  public int CountdownTime { get; set; } = 30;

  [JsonPropertyName("enable_chat_announce")]
  public bool EnableChatAnnounce { get; set; } = true;

  [JsonPropertyName("enable_hud_announce")]
  public bool EnableHudAnnounce { get; set; } = true;
}

public class OneVOneSlay : BasePlugin, IPluginConfig<OneVOneSlayConfig>
{
  public override string ModuleName => "1v1Slay";
  public override string ModuleVersion => "1.0.4";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public OneVOneSlayConfig Config { get; set; } = new OneVOneSlayConfig();

  private CounterStrikeSharp.API.Modules.Timers.Timer? _countdownTimer = null;
  private int _remainingTime = 0;
  private bool _isCountdownActive = false;
  private string _hudHtml = "";
  private bool _showHud = false;

  public void OnConfigParsed(OneVOneSlayConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
    RegisterListener<OnTick>(OnTickHud);
  }
  private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
  {
    AddTimer(0.1f, () => CheckOneVsOne());
    return HookResult.Continue;
  }

  [ConsoleCommand("css_stopslay", "css_stopslay")]
  [RequiresPermissionsOr("@css/generic", "@css/slay")]
  public void OnStopSlayCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (!_isCountdownActive)
    {
      info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["onevsoneslay.not_active"]}");
      return;
    }

    StopCountdown();
    Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["onevsoneslay.stopped", player?.PlayerName ?? "CONSOLE"]}");
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    StopCountdown();
    return HookResult.Continue;
  }

  private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
  {
    StopCountdown();
    return HookResult.Continue;
  }

  private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
  {
    AddTimer(0.1f, () => CheckOneVsOne());
    return HookResult.Continue;
  }

  private void OnTickHud()
  {
    if (_showHud && Config.EnableHudAnnounce && _hudHtml.Length > 0)
    {
      foreach (var player in Utilities.GetPlayers())
      {
        if (player.IsValid && !player.IsBot)
        {
          player.PrintToCenterHtml(_hudHtml);
        }
      }
    }
  }

  private void CheckOneVsOne()
  {
    var allPlayers = Utilities.GetPlayers().Where(p => p.IsValid).ToList();

    var alivePlayers = allPlayers
        .Where(p => p.PawnIsAlive && (p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist))
        .ToList();

    var totalPlayers = allPlayers
        .Where(p => p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist || p.Team == CsTeam.Spectator)
        .Count();

    if (totalPlayers < Config.MinPlayers)
    {
      StopCountdown();
      return;
    }

    var aliveT = alivePlayers.Count(p => p.Team == CsTeam.Terrorist);
    var aliveCT = alivePlayers.Count(p => p.Team == CsTeam.CounterTerrorist);

    if (aliveT == 1 && aliveCT == 1)
    {
      if (!_isCountdownActive)
      {
        StartCountdown();
      }
    }
    else
    {
      if (_isCountdownActive)
      {
        StopCountdown();
      }
    }
  }

  private void StartCountdown()
  {
    _isCountdownActive = true;
    _remainingTime = Config.CountdownTime;

    if (Config.EnableChatAnnounce)
    {
      Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["onevsoneslay.countdown_warning", _remainingTime]}");
    }

    if (Config.EnableHudAnnounce)
    {
      _showHud = true;
      _hudHtml = BuildHudHtml(_remainingTime);
    }

    _countdownTimer = AddTimer(1.0f, () =>
    {
      if (!_isCountdownActive) return;

      _remainingTime--;

      if (_remainingTime > 0)
      {
        if (Config.EnableHudAnnounce)
        {
          _hudHtml = BuildHudHtml(_remainingTime);
        }

        if (Config.EnableChatAnnounce && (_remainingTime % 5 == 0 || _remainingTime < 5))
        {
          Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["onevsoneslay.countdown_warning", _remainingTime]}");
        }
      }
      else
      {
        SlayRemainingPlayers();
        StopCountdown();
      }
    }, TimerFlags.REPEAT);
  }

  private static string BuildHudHtml(int remaining)
  {
    return $"<font color='#ff0000' class='fontSize-m'><img src='https://raw.githubusercontent.com/ByDexterTR/CS2Plugins/refs/heads/main/img/skull.png'> 1v1 Ölüm sayacı: {remaining} saniye</font>";
  }

  private void StopCountdown()
  {
    _countdownTimer?.Kill();
    _countdownTimer = null;
    _isCountdownActive = false;
    _showHud = false;
    _hudHtml = "";
  }

  private void SlayRemainingPlayers()
  {
    var alivePlayers = Utilities.GetPlayers()
        .Where(p => p.IsValid && p.PawnIsAlive &&
                   (p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist))
        .ToList();

    if (Config.EnableChatAnnounce)
    {
      Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["onevsoneslay.time_up"]}");
    }

    foreach (var player in alivePlayers)
    {
      player.PlayerPawn.Value?.CommitSuicide(false, true);
    }
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