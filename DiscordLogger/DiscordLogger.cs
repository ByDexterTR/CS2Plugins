using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using static CounterStrikeSharp.API.Core.Listeners;

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

  [JsonPropertyName("command_blacklist")]
  public List<string> CommandBlacklist { get; set; } = new()
  {
    "css_wp", "css_knife", "css_gloves", "css_agents", "css_music"
  };

  [JsonPropertyName("chat_blacklist")]
  public List<string> ChatBlacklist { get; set; } = new()
  {
    "!wp", "!knife", "!gloves", "!agents", "!music"
  };
}

public class DiscordLogger : BasePlugin, IPluginConfig<DiscordLoggerConfig>
{
  public override string ModuleName => "DiscordLogger";
  public override string ModuleVersion => "1.0.3";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  public DiscordLoggerConfig Config { get; set; } = new DiscordLoggerConfig();

  private readonly Dictionary<string, StringBuilder> _messageBuffers = new();
  private readonly object _bufferLock = new();
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

    RegisterListener<OnPlayerChat>(OnPlayerChatListener);

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
      AddToBuffer(Config.WebhookMap, $"{GetTimestampPrefix()}``{Localizer["discord.map_changed", currentMap]}``");
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
    AddToBuffer(Config.WebhookConnect, $"{GetTimestampPrefix()}``{Localizer["discord.connected", steamId, player.PlayerName]}``");

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
        playTimeText = Localizer["discord.played_for", FormatPlayTime(totalSeconds)];
      }
      _playerConnectTime.Remove(player.SteamID);
    }

    AddToBuffer(Config.WebhookConnect, $"{GetTimestampPrefix()}``{Localizer["discord.disconnected", steamId, player.PlayerName, playTimeText]}``");

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

    var fullCommand = string.IsNullOrWhiteSpace(args) ? command : $"{command} {args}";
    if (IsBlacklisted(fullCommand, Config.CommandBlacklist))
      return HookResult.Continue;

    if (player == null)
    {
      AddToBuffer(Config.WebhookCommand, $"{GetTimestampPrefix()}``{Localizer["discord.console_command", command, args]}``");
      return HookResult.Continue;
    }

    if (!player.IsValid || player.IsBot)
      return HookResult.Continue;

    var steamId = player.SteamID.ToString();
    AddToBuffer(Config.WebhookCommand, $"{GetTimestampPrefix()}``{Localizer["discord.player_command", steamId, player.PlayerName, command, args]}``");

    return HookResult.Continue;
  }

  private static bool IsBlacklisted(string text, List<string> blacklist)
  {
    if (blacklist.Count == 0)
      return false;

    foreach (var entry in blacklist)
    {
      if (!string.IsNullOrWhiteSpace(entry))
        text = text.Replace(entry, "", StringComparison.OrdinalIgnoreCase);
    }

    return text.Trim().Length == 0;
  }

  private void OnPlayerChatListener(CCSPlayerController player, string text, bool teamChat)
  {
    if (string.IsNullOrEmpty(Config.WebhookChat))
      return;

    if (string.IsNullOrEmpty(text))
      return;

    if (!player.IsValid || player.IsBot)
      return;

    if (IsBlacklisted(text, Config.ChatBlacklist))
      return;

    var steamId = player.SteamID.ToString();
    var chatType = teamChat ? Localizer["discord.team_chat_suffix"].Value : "";
    AddToBuffer(Config.WebhookChat, $"{GetTimestampPrefix()}``{Localizer["discord.chat", steamId, player.PlayerName, chatType, text]}``");
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
      if (victim.IsValid)
      {
        AddToBuffer(Config.WebhookKill, $"{GetTimestampPrefix()}``{Localizer["discord.suicide", victimSteamId, victim.PlayerName]}``");
      }
    }
    else
    {
      var attackerSteamId = attacker.IsBot ? "BOT" : attacker.SteamID.ToString();
      AddToBuffer(Config.WebhookKill, $"{GetTimestampPrefix()}``{Localizer["discord.kill", victimSteamId, victim.PlayerName, attackerSteamId, attacker.PlayerName, weapon]}``");
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

    var players = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot).ToList();
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

    AddToBuffer(Config.WebhookRound, $"{GetTimestampPrefix()}``{Localizer["discord.round_start", roundNumber, ctScore, tScore, playerCount, adminCount]}``");

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
      _ => Localizer["discord.draw"].Value
    };

    var roundNumber = gameRules?.GameRules?.TotalRoundsPlayed ?? 1;

    AddToBuffer(Config.WebhookRound, $"{GetTimestampPrefix()}``{Localizer["discord.round_end", roundNumber, durationText, winner]}``");

    return HookResult.Continue;
  }

  private bool IsAdmin(CCSPlayerController player)
  {
    return AdminManager.PlayerHasPermissions(player, "@css/generic");
  }

  private string FormatPlayTime(int totalSeconds)
  {
    if (totalSeconds <= 0)
      return Localizer["discord.duration_seconds", 0];

    var parts = new List<string>();

    int hours = totalSeconds / 3600;
    totalSeconds %= 3600;
    if (hours > 0)
      parts.Add(Localizer["discord.duration_hours", hours]);

    int minutes = totalSeconds / 60;
    totalSeconds %= 60;
    if (minutes > 0)
      parts.Add(Localizer["discord.duration_minutes", minutes]);

    if (totalSeconds > 0)
      parts.Add(Localizer["discord.duration_seconds", totalSeconds]);

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

    var newContent = content + "\n";
    string? messagesToSend = null;

    lock (_bufferLock)
    {
      if (!_messageBuffers.TryGetValue(webhook, out var buffer))
      {
        buffer = new StringBuilder();
        _messageBuffers[webhook] = buffer;
      }

      if (buffer.Length > 0 && buffer.Length + newContent.Length > MaxDiscordMessageLength)
      {
        messagesToSend = buffer.ToString().TrimEnd('\n');
        buffer.Clear();
      }

      buffer.Append(newContent);
    }

    if (messagesToSend != null)
      Task.Run(() => SendToDiscordAsync(webhook, messagesToSend));
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
        List<(string Webhook, string Messages)> pending = new();

        lock (_bufferLock)
        {
          foreach (var kvp in _messageBuffers)
          {
            if (kvp.Value.Length > 0)
            {
              pending.Add((kvp.Key, kvp.Value.ToString().TrimEnd('\n')));
              kvp.Value.Clear();
            }
          }
        }

        foreach (var (webhook, messages) in pending)
        {
          await SendToDiscordAsync(webhook, messages);
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
