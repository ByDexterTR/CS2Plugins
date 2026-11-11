using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using static CounterStrikeSharp.API.Core.Listeners;

namespace DiscordLogger;

public enum LogCategory
{
  Map,
  Connect,
  Command,
  Chat,
  Kill,
  Round,
  Damage,
  Grenade,
  Bomb,
  Activity
}

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

  [JsonPropertyName("webhook_damage")]
  public string WebhookDamage { get; set; } = "";

  [JsonPropertyName("webhook_grenade")]
  public string WebhookGrenade { get; set; } = "";

  [JsonPropertyName("webhook_bomb")]
  public string WebhookBomb { get; set; } = "";

  [JsonPropertyName("webhook_activity")]
  public string WebhookActivity { get; set; } = "";

  [JsonPropertyName("log_to_file")]
  public bool LogToFile { get; set; } = false;

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
  public override string ModuleVersion => "1.0.5";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  public DiscordLoggerConfig Config { get; set; } = new DiscordLoggerConfig();

  private readonly Dictionary<string, StringBuilder> _messageBuffers = new();
  private readonly Dictionary<string, Queue<string>> _pendingChunks = new();
  private readonly Dictionary<string, string> _templates = new();
  private readonly object _bufferLock = new();
  private readonly List<string> _fileLines = new();
  private readonly object _fileLock = new();
  private readonly HttpClient _httpClient = new();
  private readonly Dictionary<ulong, DateTime> _playerConnectTime = new();
  private const int MaxDiscordMessageLength = 2000;
  private const int MaxPendingChunks = 50;
  private const int SendSpacingMs = 500;
  private const int MaxSendAttempts = 3;
  private bool _isSending = false;
  private DateTime _roundStartTime;
  private string _lastMapName = "";
  private string? _lastMvpRef;

  public void OnConfigParsed(DiscordLoggerConfig config) => Config = config;

  public override void Load(bool hotReload)
  {
    _httpClient.Timeout = TimeSpan.FromSeconds(10);
    _templates.Clear();

    RegisterHandlers();

    AddTimer(3.0f, ProcessBuffers, TimerFlags.REPEAT);

    _lastMapName = Server.MapName ?? "";
    AddTimer(1.0f, InitializePlayerTimes);
  }

  private string? WebhookFor(LogCategory category) => category switch
  {
    LogCategory.Map => Config.WebhookMap,
    LogCategory.Connect => Config.WebhookConnect,
    LogCategory.Command => Config.WebhookCommand,
    LogCategory.Chat => Config.WebhookChat,
    LogCategory.Kill => Config.WebhookKill,
    LogCategory.Round => Config.WebhookRound,
    LogCategory.Damage => Config.WebhookDamage,
    LogCategory.Grenade => Config.WebhookGrenade,
    LogCategory.Bomb => Config.WebhookBomb,
    LogCategory.Activity => Config.WebhookActivity,
    _ => null
  };

  private bool IsActive(LogCategory category) =>
    !string.IsNullOrEmpty(WebhookFor(category)) || Config.LogToFile;

  private void Send(LogCategory category, string message, string? fileOnlySuffix = null)
  {
    var webhook = WebhookFor(category);
    if (!string.IsNullOrEmpty(webhook))
      AddToBuffer(webhook, message);

    if (Config.LogToFile)
    {
      var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{category}] {StripMarkdown(message + (fileOnlySuffix ?? ""))}";
      lock (_fileLock)
        _fileLines.Add(line);
    }
  }

  private static string StripMarkdown(string message)
  {
    message = Regex.Replace(message, @"\[([^\]]+)\]\(<([^)>]+)>\)", "$1 ($2)");
    return Regex.Replace(message, "[*`]", "");
  }

  private void RegisterHandlers()
  {
    if (IsActive(LogCategory.Map))
      RegisterEventHandler<EventRoundAnnounceMatchStart>(OnMatchStart);

    if (IsActive(LogCategory.Connect))
    {
      RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
      RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
      RegisterEventHandler<EventPlayerChangename>(OnPlayerChangename);
    }

    if (IsActive(LogCategory.Command))
      AddCommandListener(null, OnCommand, HookMode.Post);

    if (IsActive(LogCategory.Chat))
      RegisterListener<OnPlayerChat>(OnPlayerChatListener);

    if (IsActive(LogCategory.Kill))
      RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);

    if (IsActive(LogCategory.Round))
    {
      RegisterEventHandler<EventRoundStart>(OnRoundStart);
      RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
      RegisterEventHandler<EventRoundMvp>(OnRoundMvp);
    }

    if (IsActive(LogCategory.Damage))
      RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);

    if (IsActive(LogCategory.Grenade))
    {
      RegisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown);
      RegisterEventHandler<EventHegrenadeDetonate>(OnHegrenadeDetonate);
      RegisterEventHandler<EventFlashbangDetonate>(OnFlashbangDetonate);
      RegisterEventHandler<EventPlayerBlind>(OnPlayerBlind);
      RegisterEventHandler<EventSmokegrenadeDetonate>(OnSmokegrenadeDetonate);
      RegisterEventHandler<EventSmokegrenadeExpired>(OnSmokegrenadeExpired);
      RegisterEventHandler<EventMolotovDetonate>(OnMolotovDetonate);
      RegisterEventHandler<EventDecoyDetonate>(OnDecoyDetonate);
    }

    if (IsActive(LogCategory.Bomb))
    {
      RegisterEventHandler<EventBombPlanted>(OnBombPlanted);
      RegisterEventHandler<EventBombDefused>(OnBombDefused);
      RegisterEventHandler<EventBombExploded>(OnBombExploded);
      RegisterEventHandler<EventBombDropped>(OnBombDropped);
      RegisterEventHandler<EventBombPickup>(OnBombPickup);
    }

    if (IsActive(LogCategory.Activity))
    {
      RegisterEventHandler<EventPlayerPing>(OnPlayerPing);
      RegisterEventHandler<EventWeaponZoom>(OnWeaponZoom);
      RegisterEventHandler<EventItemPurchase>(OnItemPurchase);
    }
  }

  private void InitializePlayerTimes()
  {
    foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot))
    {
      _playerConnectTime.TryAdd(player.EntityHandle.Raw, DateTime.UtcNow);
    }
  }

  private static bool IsRealPlayer(CCSPlayerController? player) =>
    player != null && player.IsValid && !player.IsBot && !player.IsHLTV;

  private static string PlayerRef(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid)
      return "-";

    if (player.IsBot)
      return $"**{player.PlayerName} (BOT)**";

    ulong steamId = player.AuthorizedSteamID?.SteamId64 ?? 0UL;
    if (steamId == 0UL)
      return $"**{player.PlayerName}**";

    return $"[**{player.PlayerName}**](<https://steamcommunity.com/profiles/{steamId}>)";
  }

  private static string CleanWeapon(string? weapon)
  {
    if (string.IsNullOrEmpty(weapon))
      return "-";
    return weapon.StartsWith("weapon_") ? weapon[7..] : weapon;
  }

  private static string Coord(float x, float y, float z) =>
    string.Create(CultureInfo.InvariantCulture, $"({x:F0}, {y:F0}, {z:F0})");

  private string T(string key)
  {
    if (!_templates.TryGetValue(key, out var template))
      _templates[key] = template = Localizer[key].Value;

    return template;
  }

  private string T(string key, params object[] args) => string.Format(T(key), args);

  private string HitgroupName(int hitgroup) => hitgroup switch
  {
    1 => T("discord.hitgroup_head"),
    2 => T("discord.hitgroup_chest"),
    3 => T("discord.hitgroup_stomach"),
    4 => T("discord.hitgroup_leftarm"),
    5 => T("discord.hitgroup_rightarm"),
    6 => T("discord.hitgroup_leftleg"),
    7 => T("discord.hitgroup_rightleg"),
    8 => T("discord.hitgroup_neck"),
    10 => T("discord.hitgroup_gear"),
    _ => T("discord.hitgroup_generic")
  };

  private string RoundEndReason(int reason, string message)
  {
    string key = $"discord.round_reason_{reason}";
    string localized = Localizer[key];
    if (localized != key)
      return localized;

    return string.IsNullOrEmpty(message) ? reason.ToString() : message.Replace("#SFUI_Notice_", "");
  }

  private HookResult OnMatchStart(EventRoundAnnounceMatchStart @event, GameEventInfo info)
  {
    if (!IsActive(LogCategory.Map))
      return HookResult.Continue;

    var currentMap = Server.MapName;
    if (!string.IsNullOrEmpty(currentMap) && currentMap != _lastMapName)
    {
      _lastMapName = currentMap;
      Send(LogCategory.Map, Localizer["discord.map_changed", currentMap]);
    }

    return HookResult.Continue;
  }

  private HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
  {
    if (!IsActive(LogCategory.Connect))
      return HookResult.Continue;

    var player = @event.Userid;
    if (!IsRealPlayer(player))
      return HookResult.Continue;

    _playerConnectTime[player!.EntityHandle.Raw] = DateTime.UtcNow;
    Send(LogCategory.Connect, Localizer["discord.connected", PlayerRef(player)]);

    return HookResult.Continue;
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    if (!IsActive(LogCategory.Connect))
      return HookResult.Continue;

    var player = @event.Userid;
    if (!IsRealPlayer(player))
      return HookResult.Continue;

    string? playTimeSuffix = null;
    if (_playerConnectTime.TryGetValue(player!.EntityHandle.Raw, out var connectTime))
    {
      var totalSeconds = (int)(DateTime.UtcNow - connectTime).TotalSeconds;
      if (totalSeconds > 0)
        playTimeSuffix = Localizer["discord.played_for", FormatPlayTime(totalSeconds)];
      _playerConnectTime.Remove(player.EntityHandle.Raw);
    }

    Send(LogCategory.Connect, Localizer["discord.disconnected", PlayerRef(player)], playTimeSuffix);

    return HookResult.Continue;
  }

  private HookResult OnPlayerChangename(EventPlayerChangename @event, GameEventInfo info)
  {
    if (!IsActive(LogCategory.Connect))
      return HookResult.Continue;

    var player = @event.Userid;
    if (!IsRealPlayer(player))
      return HookResult.Continue;

    Send(LogCategory.Connect, Localizer["discord.name_changed", PlayerRef(player), @event.Oldname, @event.Newname]);
    return HookResult.Continue;
  }

  private HookResult OnCommand(CCSPlayerController? player, CommandInfo commandInfo)
  {
    if (!IsActive(LogCategory.Command))
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
      Send(LogCategory.Command, Localizer["discord.console_command", command, args]);
      return HookResult.Continue;
    }

    if (!player.IsValid || player.IsBot)
      return HookResult.Continue;

    Send(LogCategory.Command, Localizer["discord.player_command", PlayerRef(player), command, args]);

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
    if (!IsActive(LogCategory.Chat))
      return;

    if (string.IsNullOrEmpty(text) || !player.IsValid || player.IsBot)
      return;

    if (IsBlacklisted(text, Config.ChatBlacklist))
      return;

    var chatType = teamChat ? Localizer["discord.team_chat_suffix"].Value : "";
    Send(LogCategory.Chat, Localizer["discord.chat", PlayerRef(player), chatType, text]);
  }

  private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
  {
    if (!IsActive(LogCategory.Kill))
      return HookResult.Continue;

    var victim = @event.Userid;
    var attacker = @event.Attacker;

    if (victim == null || !victim.IsValid)
      return HookResult.Continue;

    if (attacker == null || !attacker.IsValid || attacker.Slot == victim.Slot)
    {
      Send(LogCategory.Kill, T("discord.suicide", PlayerRef(victim)));
      return HookResult.Continue;
    }

    var sb = new StringBuilder();
    sb.Append(T("discord.kill", PlayerRef(victim), PlayerRef(attacker), CleanWeapon(@event.Weapon)));

    sb.Append(T("discord.kill_hitgroup", HitgroupName(@event.Hitgroup)));
    sb.Append(T("discord.kill_damage", @event.DmgHealth, @event.DmgArmor));
    sb.Append(T("discord.kill_distance", @event.Distance.ToString("F1", CultureInfo.InvariantCulture)));

    var tags = new List<string>();
    if (@event.Headshot)
      tags.Add(T("discord.tag_headshot"));
    if (@event.Noscope)
      tags.Add(T("discord.tag_noscope"));
    if (@event.Thrusmoke)
      tags.Add(T("discord.tag_thrusmoke"));
    if (@event.Attackerblind)
      tags.Add(T("discord.tag_attackerblind"));
    if (@event.Attackerinair)
      tags.Add(T("discord.tag_attackerinair"));
    if (@event.Penetrated > 0)
      tags.Add(T("discord.tag_penetrated", @event.Penetrated));

    if (tags.Count > 0)
      sb.Append(T("discord.kill_tags", string.Join(", ", tags)));

    var assister = @event.Assister;
    if (assister != null && assister.IsValid)
    {
      var flashSuffix = @event.Assistedflash ? T("discord.kill_assist_flash") : "";
      sb.Append(T("discord.kill_assist", PlayerRef(assister), flashSuffix));
    }

    Send(LogCategory.Kill, sb.ToString());

    return HookResult.Continue;
  }

  private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
  {
    if (!IsActive(LogCategory.Damage))
      return HookResult.Continue;

    var victim = @event.Userid;
    var attacker = @event.Attacker;

    if (victim == null || !victim.IsValid)
      return HookResult.Continue;

    if (attacker == null || !attacker.IsValid || attacker.Slot == victim.Slot)
      return HookResult.Continue;

    Send(LogCategory.Damage, T("discord.hurt",
      PlayerRef(victim), PlayerRef(attacker), CleanWeapon(@event.Weapon), HitgroupName(@event.Hitgroup),
      @event.DmgHealth, @event.DmgArmor, @event.Health, @event.Armor));

    return HookResult.Continue;
  }

  private HookResult OnPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
  {
    if (!IsActive(LogCategory.Grenade))
      return HookResult.Continue;

    var victim = @event.Userid;
    if (victim == null || !victim.IsValid)
      return HookResult.Continue;

    if (@event.BlindDuration <= 0f)
      return HookResult.Continue;

    var duration = @event.BlindDuration.ToString("F1", CultureInfo.InvariantCulture);
    Send(LogCategory.Grenade, Localizer["discord.blind", PlayerRef(victim), PlayerRef(@event.Attacker), duration]);

    return HookResult.Continue;
  }

  private HookResult OnGrenadeThrown(EventGrenadeThrown @event, GameEventInfo info)
  {
    if (!IsActive(LogCategory.Grenade))
      return HookResult.Continue;

    var player = @event.Userid;
    if (player == null || !player.IsValid)
      return HookResult.Continue;

    Send(LogCategory.Grenade, Localizer["discord.grenade_thrown", PlayerRef(player), CleanWeapon(@event.Weapon)]);
    return HookResult.Continue;
  }

  private HookResult OnHegrenadeDetonate(EventHegrenadeDetonate @event, GameEventInfo info) =>
    LogDetonate("discord.he_detonate", @event.Userid, @event.X, @event.Y, @event.Z);

  private HookResult OnFlashbangDetonate(EventFlashbangDetonate @event, GameEventInfo info) =>
    LogDetonate("discord.flash_detonate", @event.Userid, @event.X, @event.Y, @event.Z);

  private HookResult OnSmokegrenadeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo info) =>
    LogDetonate("discord.smoke_detonate", @event.Userid, @event.X, @event.Y, @event.Z);

  private HookResult OnSmokegrenadeExpired(EventSmokegrenadeExpired @event, GameEventInfo info) =>
    LogDetonate("discord.smoke_expired", @event.Userid, @event.X, @event.Y, @event.Z);

  private HookResult OnMolotovDetonate(EventMolotovDetonate @event, GameEventInfo info) =>
    LogDetonate("discord.molotov_detonate", @event.Userid, @event.X, @event.Y, @event.Z);

  private HookResult OnDecoyDetonate(EventDecoyDetonate @event, GameEventInfo info) =>
    LogDetonate("discord.decoy_detonate", @event.Userid, @event.X, @event.Y, @event.Z);

  private HookResult LogDetonate(string key, CCSPlayerController? player, float x, float y, float z)
  {
    if (!IsActive(LogCategory.Grenade))
      return HookResult.Continue;

    Send(LogCategory.Grenade, Localizer[key, PlayerRef(player), Coord(x, y, z)]);
    return HookResult.Continue;
  }

  private HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info) =>
    LogBomb("discord.bomb_planted", @event.Userid);

  private HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info) =>
    LogBomb("discord.bomb_defused", @event.Userid);

  private HookResult OnBombExploded(EventBombExploded @event, GameEventInfo info) =>
    LogBomb("discord.bomb_exploded", @event.Userid);

  private HookResult OnBombDropped(EventBombDropped @event, GameEventInfo info) =>
    LogBomb("discord.bomb_dropped", @event.Userid);

  private HookResult OnBombPickup(EventBombPickup @event, GameEventInfo info) =>
    LogBomb("discord.bomb_pickup", @event.Userid);

  private HookResult LogBomb(string key, CCSPlayerController? player)
  {
    if (!IsActive(LogCategory.Bomb))
      return HookResult.Continue;

    Send(LogCategory.Bomb, Localizer[key, PlayerRef(player)]);
    return HookResult.Continue;
  }

  private HookResult OnPlayerPing(EventPlayerPing @event, GameEventInfo info)
  {
    if (!IsActive(LogCategory.Activity))
      return HookResult.Continue;

    var player = @event.Userid;
    if (player == null || !player.IsValid)
      return HookResult.Continue;

    Send(LogCategory.Activity, Localizer["discord.player_ping", PlayerRef(player), Coord(@event.X, @event.Y, @event.Z)]);
    return HookResult.Continue;
  }

  private HookResult OnWeaponZoom(EventWeaponZoom @event, GameEventInfo info)
  {
    if (!IsActive(LogCategory.Activity))
      return HookResult.Continue;

    var player = @event.Userid;
    if (player == null || !player.IsValid || player.IsBot)
      return HookResult.Continue;

    Send(LogCategory.Activity, Localizer["discord.weapon_zoom", PlayerRef(player)]);
    return HookResult.Continue;
  }

  private HookResult OnItemPurchase(EventItemPurchase @event, GameEventInfo info)
  {
    if (!IsActive(LogCategory.Activity))
      return HookResult.Continue;

    var player = @event.Userid;
    if (player == null || !player.IsValid)
      return HookResult.Continue;

    Send(LogCategory.Activity, Localizer["discord.item_purchase", PlayerRef(player), CleanWeapon(@event.Weapon)]);
    return HookResult.Continue;
  }

  private HookResult OnRoundMvp(EventRoundMvp @event, GameEventInfo info)
  {
    var mvp = @event.Userid;
    if (mvp != null && mvp.IsValid)
      _lastMvpRef = PlayerRef(mvp);
    return HookResult.Continue;
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    _lastMvpRef = null;

    if (!IsActive(LogCategory.Round))
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

    foreach (var team in Utilities.FindAllEntitiesByDesignerName<CTeam>("cs_team_manager"))
    {
      if (team.TeamNum == 3)
        ctScore = team.Score;
      else if (team.TeamNum == 2)
        tScore = team.Score;
    }

    var roundNumber = gameRules?.GameRules?.TotalRoundsPlayed + 1 ?? 1;

    Send(LogCategory.Round, Localizer["discord.round_start", roundNumber, ctScore, tScore, playerCount, adminCount]);

    return HookResult.Continue;
  }

  private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
  {
    if (!IsActive(LogCategory.Round))
      return HookResult.Continue;

    var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
    if (gameRules?.GameRules?.WarmupPeriod == true)
      return HookResult.Continue;

    var durationText = FormatPlayTime((int)(DateTime.UtcNow - _roundStartTime).TotalSeconds);

    var winner = @event.Winner switch
    {
      2 => "T",
      3 => "CT",
      _ => Localizer["discord.draw"].Value
    };

    var roundNumber = gameRules?.GameRules?.TotalRoundsPlayed ?? 1;
    var mvp = _lastMvpRef ?? "-";
    var reason = RoundEndReason(@event.Reason, @event.Message);

    Send(LogCategory.Round,
      Localizer["discord.round_end", roundNumber, winner, reason, mvp, @event.PlayerCount],
      Localizer["discord.round_duration", durationText]);

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

  private void AddToBuffer(string webhook, string content)
  {
    if (string.IsNullOrEmpty(webhook))
      return;

    var newContent = content + "\n";

    lock (_bufferLock)
    {
      if (!_messageBuffers.TryGetValue(webhook, out var buffer))
      {
        buffer = new StringBuilder();
        _messageBuffers[webhook] = buffer;
      }

      if (buffer.Length > 0 && buffer.Length + newContent.Length > MaxDiscordMessageLength)
      {
        Enqueue(webhook, buffer.ToString().TrimEnd('\n'));
        buffer.Clear();
      }

      buffer.Append(newContent);
    }
  }

  private void Enqueue(string webhook, string chunk)
  {
    if (!_pendingChunks.TryGetValue(webhook, out var queue))
      _pendingChunks[webhook] = queue = new Queue<string>();

    if (queue.Count >= MaxPendingChunks)
      queue.Dequeue();

    queue.Enqueue(chunk);
  }

  private void ProcessBuffers()
  {
    FlushFileLines();

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
              Enqueue(kvp.Key, kvp.Value.ToString().TrimEnd('\n'));
              kvp.Value.Clear();
            }
          }

          foreach (var kvp in _pendingChunks)
          {
            while (kvp.Value.Count > 0)
              pending.Add((kvp.Key, kvp.Value.Dequeue()));
          }
        }

        foreach (var (webhook, messages) in pending)
        {
          await SendToDiscordAsync(webhook, messages);
          await Task.Delay(SendSpacingMs);
        }
      }
      finally
      {
        _isSending = false;
      }
    });
  }

  private void FlushFileLines()
  {
    List<string> lines;
    lock (_fileLock)
    {
      if (_fileLines.Count == 0)
        return;
      lines = new List<string>(_fileLines);
      _fileLines.Clear();
    }

    var logDir = Path.Combine(ModuleDirectory, "logs");
    var logFile = Path.Combine(logDir, $"DiscordLogger-{DateTime.Now:yyyy-MM-dd}.log");

    Task.Run(() =>
    {
      try
      {
        Directory.CreateDirectory(logDir);
        File.AppendAllLines(logFile, lines);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[DiscordLogger] Dosya logu yazılamadı: {ex.Message}");
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
      for (int attempt = 1; attempt <= MaxSendAttempts; attempt++)
      {
        using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(webhook, httpContent);
        if (response.IsSuccessStatusCode)
          return;

        if ((int)response.StatusCode != 429 || attempt == MaxSendAttempts)
        {
          Console.WriteLine($"[DiscordLogger] Webhook gönderimi başarısız: {response.StatusCode}");
          return;
        }

        var wait = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1);
        if (wait > TimeSpan.FromSeconds(10))
          wait = TimeSpan.FromSeconds(10);

        await Task.Delay(wait);
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[DiscordLogger] Hata: {ex.Message}");
    }
  }

  public override void Unload(bool hotReload)
  {
    FlushFileLines();
    _httpClient.Dispose();
  }
}
