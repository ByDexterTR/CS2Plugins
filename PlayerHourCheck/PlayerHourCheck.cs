using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Collections.Concurrent;
using CssTimer = CounterStrikeSharp.API.Modules.Timers.Timer;

public class PenaltyConfig
{
  [JsonPropertyName("type")]
  public string Type { get; set; } = "kick";

  [JsonPropertyName("time")]
  public int Time { get; set; } = 0;

  [JsonPropertyName("reason")]
  public string Reason { get; set; } = "";
}

public class PlayerHourCheckConfig : BasePluginConfig
{
  [JsonPropertyName("phc_db")]
  public Dictionary<string, string> Database { get; set; } = new Dictionary<string, string>()
  {
    { "provider", "json" }, // "mysql" - "json"
    { "host", "localhost" },
    { "name", "cs2_playerhourcheck" },
    { "port", "3306" },
    { "user", "root" },
    { "password", "" }
  };

  [JsonPropertyName("phc_steam_api_key")]
  public string SteamApiKey { get; set; } = "";

  [JsonPropertyName("phc_required_playtime")]
  public int RequiredPlaytime { get; set; } = 100;

  [JsonPropertyName("phc_warn_times")]
  public int WarnTimes { get; set; } = 3;

  [JsonPropertyName("phc_warn_enabled")]
  public int WarnEnabled { get; set; } = 1;

  [JsonPropertyName("phc_warn_timer")]
  public int WarnTimer { get; set; } = 30;

  [JsonPropertyName("phc_warn_reason_private")]
  public string WarnReasonPrivate { get; set; } = "{Gold}Oyun detaylarınızı açmazsanız atılacaksınız. {Red}[{0}/{1}]";

  [JsonPropertyName("phc_kick_reason_private")]
  public string KickReasonPrivate { get; set; } = "Oyun detaylarınız gizli olduğu için oyundan atıldınız.";

  [JsonPropertyName("phc_kick_reason_playtime")]
  public string KickReasonPlaytime { get; set; } = "Gereken oyun oynama saatini karşılayamadığınız için atıldınız.";

  [JsonPropertyName("phc_penalty")]
  public Dictionary<string, PenaltyConfig> Penalties { get; set; } = new()
  {
    { "1", new PenaltyConfig { Type = "kick", Time = 0, Reason = "Yetersiz oyun saati ({PlayerPlaytime}/{RequiredPlaytime} saat)" } },
    { "3", new PenaltyConfig { Type = "ban", Time = 60, Reason = "Yetersiz oyun saati ({PlayerPlaytime}/{RequiredPlaytime} saat)" } },
    { "5", new PenaltyConfig { Type = "ban", Time = 1440, Reason = "Yetersiz oyun saati ({PlayerPlaytime}/{RequiredPlaytime} saat)" } },
  };

  [JsonPropertyName("phc_ignore_flags")]
  public List<string> IgnoreFlags { get; set; } = new() { "@bydexter/ignoreplaytime", "@css/root" };

  [JsonPropertyName("phc_ignore_steamids")]
  public List<string> IgnoreSteamIds { get; set; } = new() { "76561198843494248" };
}

public class PlayerHourCheck : BasePlugin, IPluginConfig<PlayerHourCheckConfig>
{
  public override string ModuleName => "PlayerHourCheck";
  public override string ModuleVersion => "1.0.8";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  private const int CS2AppId = 730;

  private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
  private readonly Dictionary<ulong, int> _warnCounts = new();
  private readonly Dictionary<ulong, CssTimer?> _pendingChecks = new();



  private readonly ConcurrentQueue<Action> _mainThreadActions = new();
  private CssTimer? _mainThreadProcessor;

  private class PlayerRec
  {
    public int Playtime { get; set; }
    public int PenaltyCount { get; set; }
    public long CheckedAt { get; set; }
  }

  private string _dbConnectionString = "";
  private bool _useMySql = false;
  private readonly Dictionary<string, PlayerRec> _records = new();
  private readonly object _ioLock = new();
  private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
  private string JsonPath => Path.Combine(ModuleDirectory, "players.json");

  public PlayerHourCheckConfig Config { get; set; } = new();

  public void OnConfigParsed(PlayerHourCheckConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    var provider = Config.Database.TryGetValue("provider", out var p) ? p.ToLower() : "json";

    _useMySql = provider == "mysql" && TryInitMySql();
    if (provider == "mysql" && !_useMySql)
      Logger.LogWarning("[PlayerHourCheck] MySQL baglantisi basarisiz, JSON'a dusuluyor.");

    if (!_useMySql)
      LoadJsonRecords();
    RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

    _mainThreadProcessor = AddTimer(0.1f, () =>
    {
      try
      {
        while (_mainThreadActions.TryDequeue(out var act))
        {
          try
          {
            act();
          }
          catch (Exception ex)
          {
            Logger.LogError(ex, "[PlayerHourCheck] main-thread action error");
          }
        }
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "[PlayerHourCheck] error in main-thread processor");
      }
    });

    if (hotReload)
    {
      foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV))
      {
        CheckExistingPlayer(player);
      }
    }

    AddCommand("css_phc_reload", "Konfigi yeniden yukler ve tum oyunculari yeniden kontrol eder", (player, info) =>
    {
      if (player != null && !AdminManager.PlayerHasPermissions(player, "@css/root"))
      {
        info.ReplyToCommand(Localizer["playerhourcheck.no_permission"]);
        return;
      }

      ReloadConfigFromDisk();

      int checkedCount = 0;
      foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV))
      {
        CheckExistingPlayer(p);
        checkedCount++;
      }

      info.ReplyToCommand(Localizer["playerhourcheck.reloaded", checkedCount]);
    });
  }

  private void ReloadConfigFromDisk()
  {
    try
    {
      var configPath = Path.GetFullPath(Path.Combine(
        ModuleDirectory, "..", "..", "configs", "plugins", "PlayerHourCheck", "PlayerHourCheck.json"));

      if (!File.Exists(configPath))
        return;

      var config = JsonSerializer.Deserialize<PlayerHourCheckConfig>(File.ReadAllText(configPath));
      if (config != null)
        OnConfigParsed(config);
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[PlayerHourCheck] Config yeniden yüklenemedi.");
    }
  }

  public override void Unload(bool hotReload)
  {
    foreach (var timer in _pendingChecks.Values)
      timer?.Kill();
    _pendingChecks.Clear();
    _http.Dispose();
    try
    {
      _mainThreadProcessor?.Kill();
      _mainThreadProcessor = null;
    }
    catch { }
  }

  private bool TryInitMySql()
  {
    try
    {
      var dbName = Config.Database["name"];
      var builder = new MySqlConnectionStringBuilder
      {
        Server = Config.Database["host"],
        Database = dbName,
        UserID = Config.Database["user"],
        Password = Config.Database["password"],
        Port = uint.Parse(Config.Database["port"]),
        Pooling = true
      };
      _dbConnectionString = builder.ToString();

      var builderWithoutDb = new MySqlConnectionStringBuilder(_dbConnectionString) { Database = "" };
      using (var conn = new MySqlConnection(builderWithoutDb.ToString()))
      {
        conn.Open();
        Exec(conn, $"CREATE DATABASE IF NOT EXISTS `{dbName}`");
      }

      using (var conn = new MySqlConnection(_dbConnectionString))
      {
        conn.Open();
        Exec(conn, @"CREATE TABLE IF NOT EXISTS `player_records` (
          `steamid` VARCHAR(20) PRIMARY KEY,
          `playtime` INT NOT NULL DEFAULT 0,
          `penalty_count` INT NOT NULL DEFAULT 0,
          `checked_at` BIGINT NOT NULL DEFAULT 0
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
      }

      return true;
    }
    catch (Exception ex)
    {
      Logger.LogWarning(ex, "[PlayerHourCheck] MySQL yükleme hatası");
      return false;
    }
  }

  private static void Exec(MySqlConnection conn, string sql)
  {
    using var cmd = new MySqlCommand(sql, conn);
    cmd.ExecuteNonQuery();
  }

  private void LoadJsonRecords()
  {
    lock (_ioLock)
    {
      _records.Clear();
      try
      {
        if (!File.Exists(JsonPath))
          return;

        var raw = JsonSerializer.Deserialize<Dictionary<string, PlayerRec>>(File.ReadAllText(JsonPath));
        if (raw == null)
          return;

        foreach (var (key, value) in raw)
          _records[key] = value;
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "[PlayerHourCheck] players.json okunamadı.");
      }
    }
  }

  private (int Playtime, int PenaltyCount, long CheckedAt)? GetPlayerRecord(string steamId)
  {
    try
    {
      if (!_useMySql)
      {
        lock (_ioLock)
          return _records.TryGetValue(steamId, out var rec)
            ? (rec.Playtime, rec.PenaltyCount, rec.CheckedAt)
            : null;
      }

      using var conn = new MySqlConnection(_dbConnectionString);
      conn.Open();

      using var cmd = new MySqlCommand("SELECT playtime, penalty_count, checked_at FROM `player_records` WHERE steamid = @s", conn);
      cmd.Parameters.AddWithValue("@s", steamId);
      using var reader = cmd.ExecuteReader();
      if (reader.Read())
        return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt64(2));
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[PlayerHourCheck] Veritabanı okuma hatası.");
    }
    return null;
  }

  private void SavePlayerRecord(string steamId, int playtime, int penaltyCount = 0)
  {
    var checkedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    if (!_useMySql)
    {
      string json;
      lock (_ioLock)
      {
        _records[steamId] = new PlayerRec { Playtime = playtime, PenaltyCount = penaltyCount, CheckedAt = checkedAt };
        json = JsonSerializer.Serialize(_records, JsonOpts);
      }

      Task.Run(() =>
      {
        try
        {
          lock (_ioLock)
            File.WriteAllText(JsonPath, json);
        }
        catch (Exception ex)
        {
          _mainThreadActions.Enqueue(() => Logger.LogError(ex, "[PlayerHourCheck] players.json yazılamadı."));
        }
      });
      return;
    }

    Task.Run(async () =>
    {
      try
      {
        using var conn = new MySqlConnection(_dbConnectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand(@"INSERT INTO `player_records` (steamid, playtime, penalty_count, checked_at)
          VALUES (@s, @p, @c, @t)
          ON DUPLICATE KEY UPDATE playtime = @p, penalty_count = @c, checked_at = @t", conn);
        cmd.Parameters.AddWithValue("@s", steamId);
        cmd.Parameters.AddWithValue("@p", playtime);
        cmd.Parameters.AddWithValue("@c", penaltyCount);
        cmd.Parameters.AddWithValue("@t", checkedAt);
        await cmd.ExecuteNonQueryAsync();
      }
      catch (Exception ex)
      {
        _mainThreadActions.Enqueue(() => Logger.LogError(ex, "[PlayerHourCheck] Veritabanı kaydetme hatası."));
      }
    });
  }

  private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || player.IsBot || player.IsHLTV) return HookResult.Continue;

    if (IsIgnored(player)) return HookResult.Continue;

    BeginConnectCheck(player);

    return HookResult.Continue;
  }

  private void BeginConnectCheck(CCSPlayerController player)
  {
    var steamId = player.SteamID;
    var steamIdStr = steamId.ToString();

    Task.Run(() =>
    {
      var record = GetPlayerRecord(steamIdStr);

      _mainThreadActions.Enqueue(() =>
      {
        try
        {
          if (player == null || !player.IsValid) return;

          if (record.HasValue)
          {
            var requiredHours = Config.RequiredPlaytime;
            var playerHours = record.Value.Playtime;

            if (playerHours >= requiredHours)
              return;

            var missingHours = requiredHours - playerHours;
            var lastChecked = record.Value.CheckedAt;
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var hoursSinceCheck = (currentTime - lastChecked) / 3600.0;

            if (hoursSinceCheck < missingHours)
            {
              var penaltyCount = record.Value.PenaltyCount + 1;
              SavePlayerRecord(steamIdStr, playerHours, penaltyCount);
              ApplyPenalty(player, penaltyCount, playerHours);
              return;
            }
          }

          _pendingChecks[steamId] = AddTimer(2.0f, () => CheckPlayerAsync(player));
        }
        catch (Exception ex)
        {
          Logger.LogError(ex, "[PlayerHourCheck] connect check error on main thread");
        }
      });
    });
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || player.IsBot) return HookResult.Continue;

    var steamId = player.SteamID;
    if (_pendingChecks.TryGetValue(steamId, out var timer))
    {
      timer?.Kill();
      _pendingChecks.Remove(steamId);
    }
    _warnCounts.Remove(steamId);

    return HookResult.Continue;
  }

  private void CheckExistingPlayer(CCSPlayerController player)
  {
    if (player == null || !player.IsValid || player.IsBot || player.IsHLTV) return;

    if (IsIgnored(player))
    {
      return;
    }

    BeginConnectCheck(player);
  }

  private async void CheckPlayerAsync(CCSPlayerController player)
  {
    if (player == null || !player.IsValid) return;

    var steamId = player.SteamID;
    _pendingChecks.Remove(steamId);

    try
    {
      var result = await GetPlaytimeAsync(steamId);
      var steamIdStr = steamId.ToString();

      if (result.Status == PlaytimeStatus.Success)
      {
        var requiredHours = Config.RequiredPlaytime;
        if (result.Hours >= requiredHours)
        {
          SavePlayerRecord(steamIdStr, result.Hours, 0);
          _mainThreadActions.Enqueue(() =>
          {
            try { _warnCounts.Remove(steamId); } catch { }
          });
        }
        else
        {
          var record = GetPlayerRecord(steamIdStr);
          var penaltyCount = (record?.PenaltyCount ?? 0) + 1;
          SavePlayerRecord(steamIdStr, result.Hours, penaltyCount);

          _mainThreadActions.Enqueue(() =>
          {
            try
            {
              if (player == null || !player.IsValid) return;
              ApplyPenalty(player, penaltyCount, result.Hours);
            }
            catch (Exception ex)
            {
              Logger.LogError(ex, "[PlayerHourCheck] error applying penalty on main thread");
            }
          });
        }
      }
      else
      {
        _mainThreadActions.Enqueue(() =>
        {
          try
          {
            if (player == null || !player.IsValid) return;
            HandlePrivateProfile(player);
          }
          catch (Exception ex)
          {
            Logger.LogError(ex, "[PlayerHourCheck] error handling private/error profile on main thread");
          }
        });
      }
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[PlayerHourCheck] Kontrol hatası.");
    }
  }

  private void ApplyPenalty(CCSPlayerController player, int penaltyCount, int playerHours)
  {
    var penaltyKeys = Config.Penalties.Keys
      .Select(k => int.TryParse(k, out var num) ? num : 0)
      .Where(k => k > 0)
      .OrderBy(k => k)
      .ToList();

    if (penaltyKeys.Count == 0)
    {
      KickPlayer(player, Localizer["playerhourcheck.fallback_kick", playerHours, Config.RequiredPlaytime]);
      return;
    }

    var selectedKey = penaltyKeys.Where(k => k <= penaltyCount).DefaultIfEmpty(penaltyKeys.First()).Max();

    if (penaltyCount > penaltyKeys.Max())
    {
      selectedKey = penaltyKeys.Max();
    }

    if (!Config.Penalties.TryGetValue(selectedKey.ToString(), out var penalty))
    {
      KickPlayer(player, Localizer["playerhourcheck.fallback_kick", playerHours, Config.RequiredPlaytime]);
      return;
    }

    var reason = FormatPenaltyReason(penalty.Reason, playerHours);

    if (penalty.Type.Equals("ban", StringComparison.OrdinalIgnoreCase))
    {
      BanPlayer(player, penalty.Time, reason);
    }
    else
    {
      KickPlayer(player, reason);
    }
  }

  private string FormatPenaltyReason(string reason, int playerHours)
  {
    return CC.Parse(reason
      .Replace("{RequiredPlaytime}", Config.RequiredPlaytime.ToString())
      .Replace("{PlayerPlaytime}", playerHours.ToString()));
  }

  private void BanPlayer(CCSPlayerController player, int minutes, string reason)
  {
    if (player == null || !player.IsValid) return;

    var userId = player.UserId;
    Server.ExecuteCommand($"css_ban #{userId} {minutes} \"{reason}\"");
  }

  private void HandlePrivateProfile(CCSPlayerController player)
  {
    var steamId = player.SteamID;
    var steamIdStr = steamId.ToString();

    if (Config.WarnEnabled != 1)
    {
      ApplyPrivatePenalty(player);
      return;
    }

    _warnCounts.TryGetValue(steamId, out var count);
    count++;
    _warnCounts[steamId] = count;

    if (count >= Config.WarnTimes)
    {
      ApplyPrivatePenalty(player);
      return;
    }

    var formatted = Config.WarnReasonPrivate.Replace("{0}", count.ToString()).Replace("{1}", Config.WarnTimes.ToString());
    PrintPrefix(player, formatted);

    _pendingChecks[steamId] = AddTimer(Config.WarnTimer, () => CheckPlayerAsync(player));
  }

  private void ApplyPrivatePenalty(CCSPlayerController player)
  {
    var steamIdStr = player.SteamID.ToString();

    Task.Run(() =>
    {
      var record = GetPlayerRecord(steamIdStr);

      _mainThreadActions.Enqueue(() =>
      {
        if (player == null || !player.IsValid) return;

        var penaltyCount = (record?.PenaltyCount ?? 0) + 1;
        var playerHours = record?.Playtime ?? 0;

        SavePlayerRecord(steamIdStr, playerHours, penaltyCount);
        ApplyPenalty(player, penaltyCount, playerHours);
      });
    });
  }

  private bool IsIgnored(CCSPlayerController player)
  {
    if (player == null) return false;

    var hasValidSteamIds = Config.IgnoreSteamIds != null && Config.IgnoreSteamIds.Any(s => !string.IsNullOrWhiteSpace(s));
    var hasValidFlags = Config.IgnoreFlags != null && Config.IgnoreFlags.Any(f => !string.IsNullOrWhiteSpace(f));

    if (!hasValidSteamIds && !hasValidFlags)
      return false;

    var steamId = player.SteamID.ToString();
    if (hasValidSteamIds)
    {
      foreach (var id in Config.IgnoreSteamIds!)
      {
        if (string.IsNullOrWhiteSpace(id)) continue;
        if (id.Trim() == steamId)
        {
          return true;
        }
      }
    }

    if (hasValidFlags)
    {
      foreach (var flag in Config.IgnoreFlags!)
      {
        if (string.IsNullOrWhiteSpace(flag)) continue;
        try
        {
          if (AdminManager.PlayerHasPermissions(player, flag))
          {
            return true;
          }
        }
        catch (Exception ex)
        {
          Logger.LogWarning(ex, "[PlayerHourCheck] PlayerHasPermissions threw for flag={0}", flag);
        }
      }
    }

    return false;
  }

  private void PrintPrefix(CCSPlayerController player, string message)
  {
    var parsed = CC.Parse($"{ChatPrefix} {message}");
    player.PrintToChat($" {parsed}");
  }

  private void KickPlayer(CCSPlayerController player, string reason)
  {
    if (player == null || !player.IsValid) return;

    var userId = player.UserId;
    Server.ExecuteCommand($"css_kick #{userId} \"{reason}\"");
  }

  private enum PlaytimeStatus { Success, Private, Error }

  private record PlaytimeResult(PlaytimeStatus Status, int Hours = 0);

  private async Task<PlaytimeResult> GetPlaytimeAsync(ulong steamId)
  {
    if (!string.IsNullOrWhiteSpace(Config.SteamApiKey))
    {
      var steamResult = await TrySteamApiAsync(steamId);
      if (steamResult.Status != PlaytimeStatus.Error) return steamResult;
    }

    var decResult = await TryDecApiAsync(steamId);
    if (decResult.Status != PlaytimeStatus.Error) return decResult;

    var bydexterResult = await TryByDexterApiAsync(steamId);
    return bydexterResult;
  }

  private async Task<PlaytimeResult> TrySteamApiAsync(ulong steamId)
  {
    if (string.IsNullOrWhiteSpace(Config.SteamApiKey))
      return new PlaytimeResult(PlaytimeStatus.Error);

    try
    {
      var url = $"https://api.steampowered.com/IPlayerService/GetRecentlyPlayedGames/v0001/?key={Config.SteamApiKey}&steamid={steamId}&format=json";
      var response = await _http.GetStringAsync(url);

      using var doc = JsonDocument.Parse(response);
      var root = doc.RootElement;

      if (!root.TryGetProperty("response", out var resp))
        return new PlaytimeResult(PlaytimeStatus.Error);

      if (!resp.TryGetProperty("games", out var games))
        return new PlaytimeResult(PlaytimeStatus.Private);

      foreach (var game in games.EnumerateArray())
      {
        if (game.TryGetProperty("appid", out var appId) && appId.GetInt32() == CS2AppId)
        {
          if (game.TryGetProperty("playtime_forever", out var playtime))
          {
            var minutes = playtime.GetInt32();
            var hours = minutes / 60;
            return new PlaytimeResult(PlaytimeStatus.Success, hours);
          }
        }
      }

      return new PlaytimeResult(PlaytimeStatus.Success, 0);
    }
    catch
    {
      return new PlaytimeResult(PlaytimeStatus.Error);
    }
  }

  private async Task<PlaytimeResult> TryDecApiAsync(ulong steamId)
  {
    try
    {
      var url = $"https://decapi.me/steam/hours/{steamId}/{CS2AppId}";
      var response = await _http.GetStringAsync(url);

      if (response.Contains("private", StringComparison.OrdinalIgnoreCase) ||
          response.Contains("Cannot retrieve", StringComparison.OrdinalIgnoreCase))
        return new PlaytimeResult(PlaytimeStatus.Private);

      var match = Regex.Match(response, @"([\d.,]+)", RegexOptions.IgnoreCase);
      if (match.Success)
      {
        var numStr = match.Groups[1].Value.Replace(",", ".");
        if (double.TryParse(numStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var hours))
        {
          return new PlaytimeResult(PlaytimeStatus.Success, (int)hours);
        }
      }

      return new PlaytimeResult(PlaytimeStatus.Error);
    }
    catch
    {
      return new PlaytimeResult(PlaytimeStatus.Error);
    }
  }

  private async Task<PlaytimeResult> TryByDexterApiAsync(ulong steamId)
  {
    try
    {
      var url = $"https://api.bydexter.net/steam/v1/playtime/{steamId}/{CS2AppId}";
      var response = await _http.GetStringAsync(url);

      if (response.Contains("bulunamadı", StringComparison.OrdinalIgnoreCase) ||
          response.Contains("alınamadı", StringComparison.OrdinalIgnoreCase))
        return new PlaytimeResult(PlaytimeStatus.Private);

      var match = Regex.Match(response, @"(\d+)");
      if (match.Success && int.TryParse(match.Groups[1].Value, out var hours))
      {
        return new PlaytimeResult(PlaytimeStatus.Success, hours);
      }

      return new PlaytimeResult(PlaytimeStatus.Error);
    }
    catch
    {
      return new PlaytimeResult(PlaytimeStatus.Error);
    }
  }
}
