using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;

namespace DiscordLogger;

public class DiscordLoggerConfig : BasePluginConfig
{
  [JsonPropertyName("webhook_map")]
  public string WebhookMap { get; set; } = "";

  [JsonPropertyName("webhook_connect")]
  public string WebhookConnect { get; set; } = "";

  [JsonPropertyName("webhook_command")]
  public string WebhookCommand { get; set; } = "";

  [JsonPropertyName("webhook_chat")]
  public string WebhookChat { get; set; } = "";

  [JsonPropertyName("webhook_kill")]
  public string WebhookKill { get; set; } = "";

  [JsonPropertyName("webhook_round")]
  public string WebhookRound { get; set; } = "";
}

public class DiscordLogger : BasePlugin, IPluginConfig<DiscordLoggerConfig>
{
  public override string ModuleName => "DiscordLogger";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "Discord logger";

  public DiscordLoggerConfig Config { get; set; } = new DiscordLoggerConfig();

  private readonly Dictionary<string, StringBuilder> _messageBuffers = new();
  private readonly HttpClient _httpClient = new();
  private readonly Dictionary<ulong, DateTime> _playerConnectTime = new();
  private const int MaxDiscordMessageLength = 2000;
  private bool _isSending = false;
  private DateTime _roundStartTime;
  private string _lastMapName = "";

  public void OnConfigParsed(DiscordLoggerConfig config) => Config = config;

  public override void Load(bool hotReload)
  {
    _httpClient.Timeout = TimeSpan.FromSeconds(10);

    RegisterEventHandler<EventRoundAnnounceMatchStart>(OnMatchStart);
    RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterEventHandler<EventRoundEnd>(OnRoundEnd);

    AddCommandListener("say", OnPlayerChat, HookMode.Post);
    AddCommandListener("say_team", OnPlayerChat, HookMode.Post);

    AddCommandListener(null, OnCommand, HookMode.Post);

    AddTimer(3.0f, ProcessMessageBuffers, TimerFlags.REPEAT);

    _lastMapName = Server.MapName;
    AddTimer(1.0f, InitializePlayerTimes);
  }

  private void InitializePlayerTimes()
  {
    foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot))
    {
      _playerConnectTime.TryAdd(player.SteamID, DateTime.UtcNow);
    }
  }

  private HookResult OnMatchStart(EventRoundAnnounceMatchStart @event, GameEventInfo info)
  {
    if (string.IsNullOrEmpty(Config.WebhookMap))
      return HookResult.Continue;

    var currentMap = Server.MapName;
    if (!string.IsNullOrEmpty(currentMap) && currentMap != _lastMapName)
    {
      _lastMapName = currentMap;
      AddToBuffer(Config.WebhookMap, $"{GetTimestampPrefix()}``Yeni harita: {currentMap}``");
    }

    return HookResult.Continue;
  }

  private HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
  {
    if (string.IsNullOrEmpty(Config.WebhookConnect))
      return HookResult.Continue;

    var player = @event.Userid;
    if (player == null || !player.IsValid || player.IsBot)
      return HookResult.Continue;

    _playerConnectTime[player.SteamID] = DateTime.UtcNow;

    var steamId = player.IsBot ? "BOT" : player.SteamID.ToString();
    AddToBuffer(Config.WebhookConnect, $"{GetTimestampPrefix()}``{steamId} - {player.PlayerName} sunucuya bağlandı.``");

    return HookResult.Continue;
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    if (string.IsNullOrEmpty(Config.WebhookConnect))
      return HookResult.Continue;

    var player = @event.Userid;
    if (player == null || !player.IsValid || player.IsBot)
      return HookResult.Continue;

    var steamId = player.IsBot ? "BOT" : player.SteamID.ToString();

    var playTimeText = "";
    if (_playerConnectTime.TryGetValue(player.SteamID, out var connectTime))
    {
      var duration = DateTime.UtcNow - connectTime;
      var totalSeconds = (int)duration.TotalSeconds;
      if (totalSeconds > 0)
      {
        playTimeText = $", {FormatPlayTime(totalSeconds)} oynadı";
      }
      _playerConnectTime.Remove(player.SteamID);
    }

    AddToBuffer(Config.WebhookConnect, $"{GetTimestampPrefix()}``{steamId} - {player.PlayerName} sunucudan ayrıldı{playTimeText}.``");

    return HookResult.Continue;
  }

  private HookResult OnCommand(CCSPlayerController? player, CommandInfo commandInfo)
  {
    if (string.IsNullOrEmpty(Config.WebhookCommand))
      return HookResult.Continue;

    var command = commandInfo.GetArg(0);

    if (string.IsNullOrEmpty(command) || command == "say" || command == "say_team")
      return HookResult.Continue;

    var args = commandInfo.ArgString;

    if (player == null)
    {
      AddToBuffer(Config.WebhookCommand, $"{GetTimestampPrefix()}``CONSOLE: {command} {args}``");
      return HookResult.Continue;
    }

    if (!player.IsValid || player.IsBot)
      return HookResult.Continue;

    var steamId = player.SteamID.ToString();
    AddToBuffer(Config.WebhookCommand, $"{GetTimestampPrefix()}``{steamId} - {player.PlayerName}: {command} {args}``");

    return HookResult.Continue;
  }

  private HookResult OnPlayerChat(CCSPlayerController? player, CommandInfo commandInfo)
  {
    if (string.IsNullOrEmpty(Config.WebhookChat))
      return HookResult.Continue;

    var message = commandInfo.ArgString.Trim().Trim('"');
    if (string.IsNullOrEmpty(message))
      return HookResult.Continue;

    if (player == null)
    {
      AddToBuffer(Config.WebhookChat, $"{GetTimestampPrefix()}``CONSOLE: {message}``");
      return HookResult.Continue;
    }

    if (!player.IsValid || player.IsBot)
      return HookResult.Continue;

    var steamId = player.SteamID.ToString();
    var isTeamChat = commandInfo.GetArg(0).Equals("say_team", StringComparison.OrdinalIgnoreCase);
    var chatType = isTeamChat ? " (Takım)" : "";
    AddToBuffer(Config.WebhookChat, $"{GetTimestampPrefix()}``{steamId} - {player.PlayerName}{chatType}: {message}``");

    return HookResult.Continue;
  }

  private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
  {
    if (string.IsNullOrEmpty(Config.WebhookKill))
      return HookResult.Continue;

    var victim = @event.Userid;
    var attacker = @event.Attacker;
    var weapon = @event.Weapon;

    if (victim == null || !victim.IsValid)
      return HookResult.Continue;

    var victimSteamId = victim.IsBot ? "BOT" : victim.SteamID.ToString();

    if (attacker == null || !attacker.IsValid || attacker.SteamID == victim.SteamID)
    {
      if (victim.Connected == PlayerConnectedState.PlayerConnected)
      {
        AddToBuffer(Config.WebhookKill, $"{GetTimestampPrefix()}``Ölen: {victimSteamId} - {victim.PlayerName} | İntihar etti.``");
      }
    }
    else
    {
      var attackerSteamId = attacker.IsBot ? "BOT" : attacker.SteamID.ToString();
      AddToBuffer(Config.WebhookKill, $"{GetTimestampPrefix()}``Ölen: {victimSteamId} - {victim.PlayerName} | Öldüren: {attackerSteamId} - {attacker.PlayerName} | Silah: {weapon}``");
    }

    return HookResult.Continue;
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    if (string.IsNullOrEmpty(Config.WebhookRound))
      return HookResult.Continue;

    var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
    if (gameRules?.GameRules?.WarmupPeriod == true)
      return HookResult.Continue;

    _roundStartTime = DateTime.UtcNow;

    var players = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && p.Connected == PlayerConnectedState.PlayerConnected).ToList();
    var playerCount = players.Count;
    var adminCount = players.Count(IsAdmin);

    var ctScore = 0;
    var tScore = 0;

    var teams = Utilities.FindAllEntitiesByDesignerName<CTeam>("cs_team_manager");
    foreach (var team in teams)
    {
      if (team.TeamNum == 3)
        ctScore = team.Score;
      else if (team.TeamNum == 2)
        tScore = team.Score;
    }

    var roundNumber = gameRules?.GameRules?.TotalRoundsPlayed + 1 ?? 1;

    AddToBuffer(Config.WebhookRound, $"{GetTimestampPrefix()}``{roundNumber}. Raunt başladı | Skor: {ctScore} CT - {tScore} T | Oyuncu: {playerCount} (Yetkili: {adminCount})``");

    return HookResult.Continue;
  }

  private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
  {
    if (string.IsNullOrEmpty(Config.WebhookRound))
      return HookResult.Continue;

    var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
    if (gameRules?.GameRules?.WarmupPeriod == true)
      return HookResult.Continue;

    var duration = DateTime.UtcNow - _roundStartTime;
    var totalSeconds = (int)duration.TotalSeconds;
    var durationText = FormatPlayTime(totalSeconds);

    var winner = @event.Winner switch
    {
      2 => "T",
      3 => "CT",
      _ => "Berabere"
    };

    var roundNumber = gameRules?.GameRules?.TotalRoundsPlayed ?? 1;

    AddToBuffer(Config.WebhookRound, $"{GetTimestampPrefix()}``{roundNumber}. Raunt bitti | Raunt Süresi: {durationText} | Kazanan: {winner}``");

    return HookResult.Continue;
  }

  private bool IsAdmin(CCSPlayerController player)
  {
    return AdminManager.PlayerHasPermissions(player, "@css/generic");
  }

  private static string FormatPlayTime(int totalSeconds)
  {
    if (totalSeconds <= 0)
      return "0 saniye";

    var parts = new List<string>();

    int hours = totalSeconds / 3600;
    totalSeconds %= 3600;
    if (hours > 0)
      parts.Add($"{hours} saat");

    int minutes = totalSeconds / 60;
    totalSeconds %= 60;
    if (minutes > 0)
      parts.Add($"{minutes} dakika");

    if (totalSeconds > 0)
      parts.Add($"{totalSeconds} saniye");

    return string.Join(" ", parts);
  }

  private static string GetTimestampPrefix()
  {
    return $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:f> ";
  }

  private void AddToBuffer(string webhook, string content)
  {
    if (string.IsNullOrEmpty(webhook))
      return;

    if (!_messageBuffers.ContainsKey(webhook))
    {
      _messageBuffers[webhook] = new StringBuilder();
    }

    var buffer = _messageBuffers[webhook];
    var newContent = content + "\n";

    if (buffer.Length > 0 && buffer.Length + newContent.Length > MaxDiscordMessageLength)
    {
      var messagesToSend = buffer.ToString().TrimEnd('\n');
      buffer.Clear();
      Task.Run(() => SendToDiscordAsync(webhook, messagesToSend));
    }

    buffer.Append(newContent);
  }

  private void ProcessMessageBuffers()
  {
    if (_isSending)
      return;

    _isSending = true;

    Task.Run(async () =>
    {
      try
      {
        foreach (var kvp in _messageBuffers.ToList())
        {
          if (kvp.Value.Length > 0)
          {
            var webhook = kvp.Key;
            var messages = kvp.Value.ToString().TrimEnd('\n');
            kvp.Value.Clear();

            await SendToDiscordAsync(webhook, messages);
          }
        }
      }
      finally
      {
        _isSending = false;
      }
    });
  }

  private async Task SendToDiscordAsync(string webhook, string content)
  {
    try
    {
      var payload = new
      {
        content = content
      };

      var json = JsonSerializer.Serialize(payload);
      var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

      var response = await _httpClient.PostAsync(webhook, httpContent);
      if (!response.IsSuccessStatusCode)
      {
        Console.WriteLine($"[DiscordLogger] Webhook gönderimi başarısız: {response.StatusCode}");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[DiscordLogger] Hata: {ex.Message}");
    }
  }

  public override void Unload(bool hotReload)
  {
    _httpClient.Dispose();
  }
}
