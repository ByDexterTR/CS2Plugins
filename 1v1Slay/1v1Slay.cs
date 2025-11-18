using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace OneVOneSlay;

public class OneVOneSlayConfig : BasePluginConfig
{
  [JsonPropertyName("chat_prefix")]
  public string ChatPrefix { get; set; } = "[ByDexter]";

  [JsonPropertyName("min_players")]
  public int MinPlayers { get; set; } = 3;

  [JsonPropertyName("countdown_time")]
  public int CountdownTime { get; set; } = 30;

  [JsonPropertyName("enable_announcements")]
  public bool EnableAnnouncements { get; set; } = true;
}

public class OneVOneSlay : BasePlugin, IPluginConfig<OneVOneSlayConfig>
{
  public override string ModuleName => "1v1Slay";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "1v1 durumunda geri sayımlı slay";

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
    RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
    RegisterListener<OnTick>(OnTickHud);
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
    if (_showHud && Config.EnableAnnouncements)
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

    if (Config.EnableAnnouncements)
    {
      _showHud = true;
      Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix} {CC.Red}{_remainingTime} {CC.Default}saniye içinde oyuncular öldürülecek!");
    }

    _countdownTimer = AddTimer(1.0f, () =>
    {
      if (!_isCountdownActive) return;

      _remainingTime--;

      if (_remainingTime > 0)
      {
        if (Config.EnableAnnouncements)
        {
          _hudHtml = $"<font color='#ff0000' class='fontSize-m'><img src='https://images.weserv.nl/?url=em-content.zobj.net/source/facebook/355/skull_1f480.png&w=24&h=24&fit=cover'> 1v1 Ölüm sayacı: {_remainingTime} saniye</font>";

          if (_remainingTime % 5 == 0 || _remainingTime < 5)
          {
            Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix} {CC.Red}{_remainingTime} {CC.Default}saniye içinde oyuncular öldürülecek!");
          }
        }
      }
      else
      {
        SlayRemainingPlayers();
        StopCountdown();
      }
    }, TimerFlags.REPEAT);
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

    if (Config.EnableAnnouncements)
    {
      Server.PrintToChatAll($"{CC.Gold}{Config.ChatPrefix} {CC.Red}Süre doldu! Kalan oyuncular öldürülüyor!");
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