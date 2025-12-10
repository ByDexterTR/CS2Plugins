using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Microsoft.Data.Sqlite;
using Dapper;
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
    { "provider", "sqlite" }, // "mysql" - "sqlite"
    { "host", "localhost" },
    { "name", "cs2_playerhourcheck" },
    { "port", "3306" },
    { "user", "root" },
    { "password", "" }
  };

  [JsonPropertyName("phc_chat_prefix")]
  public string ChatPrefix { get; set; } = "{Orchid}[ByDexter]";

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
  public override string ModuleVersion => "1.0.2";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "Oyuncu saat kontrolü";

  private const int CS2AppId = 730;

  private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
  private readonly Dictionary<ulong, int> _warnCounts = new();
  private readonly Dictionary<ulong, CssTimer?> _pendingChecks = new();

  private string _dbConnectionString = "";
  private bool _usingSqlite = false;

  public PlayerHourCheckConfig Config { get; set; } = new();

  public void OnConfigParsed(PlayerHourCheckConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    try
    {
      var pluginDir = ModuleDirectory;
      var nativeDllPath = Path.Combine(pluginDir, "e_sqlite3.dll");
      
      if (File.Exists(nativeDllPath))
      {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
          NativeLibrary.Load(nativeDllPath);
        }
        
        SQLitePCL.Batteries_V2.Init();
      }
      else
      {
        Logger.LogWarning($"[PlayerHourCheck] Native SQLite DLL bulunamadı: {nativeDllPath}");
      }
    }
    catch (Exception ex)
    {
      Logger.LogWarning(ex, "[PlayerHourCheck] SQLite native kütüphanesi yüklenirken hata (devam ediliyor)");
    }

    var provider = Config.Database.TryGetValue("provider", out var p) ? p.ToLower() : "sqlite";

    if (provider == "mysql")
    {
      if (!TryLoadMySQL())
      {
        Logger.LogWarning("[PlayerHourCheck] MySQL yüklenemedi, SQLite'a geçiliyor...");
        if (!TryLoadSQLite())
        {
          throw new Exception("[PlayerHourCheck] Hiçbir veritabanı yüklenemedi! Eklenti çalışamıyor.");
        }
      }
    }
    else
    {
      if (!TryLoadSQLite())
      {
        Logger.LogWarning("[PlayerHourCheck] SQLite yüklenemedi, MySQL'e geçiliyor...");
        if (!TryLoadMySQL())
        {
          throw new Exception("[PlayerHourCheck] Hiçbir veritabanı yüklenemedi! Eklenti çalışamıyor.");
        }
      }
    }

    InitializeDatabase();
    RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

    if (hotReload)
    {
      foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV))
      {
        CheckExistingPlayer(player);
      }
    }
  }

  public override void Unload(bool hotReload)
  {
    foreach (var timer in _pendingChecks.Values)
      timer?.Kill();
    _pendingChecks.Clear();
    _http.Dispose();
  }

  private bool TryLoadMySQL()
  {
    try
    {
      _usingSqlite = false;
      var builder = new MySqlConnectionStringBuilder
      {
        Server = Config.Database["host"],
        Database = Config.Database["name"],
        UserID = Config.Database["user"],
        Password = Config.Database["password"],
        Port = uint.Parse(Config.Database["port"])
      };
      _dbConnectionString = builder.ToString();
      return true;
    }
    catch (Exception ex)
    {
      Logger.LogWarning(ex, "[PlayerHourCheck] MySQL yükleme hatası");
      return false;
    }
  }

  private bool TryLoadSQLite()
  {
    try
    {
      _usingSqlite = true;
      var fullPath = Path.Combine(ModuleDirectory, "PlayerHourCheck.sqlite");
      _dbConnectionString = $"Data Source={fullPath}";
      return true;
    }
    catch (Exception ex)
    {
      Logger.LogWarning(ex, "[PlayerHourCheck] SQLite yükleme hatası");
      return false;
    }
  }

  private void InitializeDatabase()
  {
    Task.Run(async () =>
    {
      try
      {
        if (_usingSqlite)
        {
          using var conn = new SqliteConnection(_dbConnectionString);
          await conn.OpenAsync();

          var sql = @"CREATE TABLE IF NOT EXISTS player_records (
            steamid TEXT PRIMARY KEY,
            playtime INTEGER NOT NULL DEFAULT 0,
            penalty_count INTEGER NOT NULL DEFAULT 0,
            checked_at INTEGER NOT NULL DEFAULT 0
          )";

          await conn.ExecuteAsync(sql);
        }
        else
        {
          var dbName = Config.Database["name"];

          var builderWithoutDb = new MySqlConnectionStringBuilder
          {
            Server = Config.Database["host"],
            UserID = Config.Database["user"],
            Password = Config.Database["password"],
            Port = uint.Parse(Config.Database["port"])
          };

          using (var conn = new MySqlConnection(builderWithoutDb.ToString()))
          {
            await conn.OpenAsync();
            await conn.ExecuteAsync($"CREATE DATABASE IF NOT EXISTS `{dbName}`");
          }

          using (var conn = new MySqlConnection(_dbConnectionString))
          {
            await conn.OpenAsync();

            var sql = @"CREATE TABLE IF NOT EXISTS `player_records` (
              `steamid` VARCHAR(20) PRIMARY KEY,
              `playtime` INT NOT NULL DEFAULT 0,
              `penalty_count` INT NOT NULL DEFAULT 0,
              `checked_at` BIGINT NOT NULL DEFAULT 0
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";

            await conn.ExecuteAsync(sql);
          }
        }
      }
      catch (Exception ex)
      {
        Server.NextFrame(() => Logger.LogError(ex, "[PlayerHourCheck] Veritabanı bağlantı hatası."));
      }
    }).Wait();
  }

  private (int Playtime, int PenaltyCount, long CheckedAt)? GetPlayerRecord(string steamId)
  {
    try
    {
      if (_usingSqlite)
      {
        using var conn = new SqliteConnection(_dbConnectionString);
        conn.Open();

        var row = conn.QueryFirstOrDefault("SELECT playtime, penalty_count, checked_at FROM player_records WHERE steamid = @steamid",
          new { steamid = steamId });

        if (row != null)
        {
          return ((int)row.playtime, (int)row.penalty_count, (long)row.checked_at);
        }
      }
      else
      {
        using var conn = new MySqlConnection(_dbConnectionString);
        conn.Open();

        var row = conn.QueryFirstOrDefault("SELECT playtime, penalty_count, checked_at FROM player_records WHERE steamid = @steamid",
          new { steamid = steamId });

        if (row != null)
        {
          return ((int)row.playtime, (int)row.penalty_count, (long)row.checked_at);
        }
      }
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[PlayerHourCheck] Veritabanı okuma hatası.");
    }
    return null;
  }

  private void SavePlayerRecord(string steamId, int playtime, int penaltyCount = 0)
  {
    Task.Run(async () =>
    {
      try
      {
        var checkedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (_usingSqlite)
        {
          using var conn = new SqliteConnection(_dbConnectionString);
          await conn.OpenAsync();

          var sql = @"INSERT OR REPLACE INTO player_records (steamid, playtime, penalty_count, checked_at)
            VALUES (@steamid, @playtime, @penalty_count, @checked_at)";

          await conn.ExecuteAsync(sql, new
          {
            steamid = steamId,
            playtime = playtime,
            penalty_count = penaltyCount,
            checked_at = checkedAt
          });
        }
        else
        {
          using var conn = new MySqlConnection(_dbConnectionString);
          await conn.OpenAsync();

          var sql = @"INSERT INTO player_records (steamid, playtime, penalty_count, checked_at)
            VALUES (@steamid, @playtime, @penalty_count, @checked_at)
            ON DUPLICATE KEY UPDATE playtime = @playtime, penalty_count = @penalty_count, checked_at = @checked_at";

          await conn.ExecuteAsync(sql, new
          {
            steamid = steamId,
            playtime = playtime,
            penalty_count = penaltyCount,
            checked_at = checkedAt
          });
        }
      }
      catch (Exception ex)
      {
        Server.NextFrame(() => Logger.LogError(ex, "[PlayerHourCheck] Veritabanı kaydetme hatası."));
      }
    });
  }

  private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || player.IsBot || player.IsHLTV) return HookResult.Continue;

    var steamId = player.SteamID;
    var steamIdStr = steamId.ToString();

    if (IsIgnored(player))
    {
      return HookResult.Continue;
    }

    var record = GetPlayerRecord(steamIdStr);
    if (record.HasValue)
    {
      var requiredHours = Config.RequiredPlaytime;
      var playerHours = record.Value.Playtime;

      if (playerHours >= requiredHours)
      {
        return HookResult.Continue;
      }

      var missingHours = requiredHours - playerHours;
      var lastChecked = record.Value.CheckedAt;
      var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      var hoursSinceCheck = (currentTime - lastChecked) / 3600.0;

      if (hoursSinceCheck < missingHours)
      {
        var penaltyCount = record.Value.PenaltyCount + 1;
        SavePlayerRecord(steamIdStr, playerHours, penaltyCount);
        ApplyPenalty(player, penaltyCount, playerHours);
        return HookResult.Continue;
      }
    }

    _pendingChecks[steamId] = AddTimer(2.0f, () => CheckPlayerAsync(player));

    return HookResult.Continue;
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null) return HookResult.Continue;

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

    var steamId = player.SteamID;
    var steamIdStr = steamId.ToString();

    if (IsIgnored(player))
    {
      return;
    }

    var record = GetPlayerRecord(steamIdStr);
    if (record.HasValue)
    {
      var requiredHours = Config.RequiredPlaytime;
      var playerHours = record.Value.Playtime;

      if (playerHours >= requiredHours)
      {
        return;
      }

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

  private async void CheckPlayerAsync(CCSPlayerController player)
  {
    if (player == null || !player.IsValid) return;

    var steamId = player.SteamID;
    _pendingChecks.Remove(steamId);

    try
    {
      var result = await GetPlaytimeAsync(steamId);

      Server.NextFrame(() =>
      {
        if (player == null || !player.IsValid) return;

        switch (result.Status)
        {
          case PlaytimeStatus.Success:
            HandlePlaytimeResult(player, result.Hours);
            break;

          case PlaytimeStatus.Private:
          case PlaytimeStatus.Error:
            HandlePrivateProfile(player);
            break;
        }
      });
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[PlayerHourCheck] Kontrol hatası.");
    }
  }

  private void HandlePlaytimeResult(CCSPlayerController player, int hours)
  {
    var steamId = player.SteamID;
    var steamIdStr = steamId.ToString();
    var requiredHours = Config.RequiredPlaytime;

    if (hours >= requiredHours)
    {
      SavePlayerRecord(steamIdStr, hours, 0);
      _warnCounts.Remove(steamId);
    }
    else
    {
      var record = GetPlayerRecord(steamIdStr);
      var penaltyCount = (record?.PenaltyCount ?? 0) + 1;

      SavePlayerRecord(steamIdStr, hours, penaltyCount);
      ApplyPenalty(player, penaltyCount, hours);
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
      KickPlayer(player, $"Yetersiz oyun saati ({playerHours}/{Config.RequiredPlaytime} saat)");
      return;
    }

    var selectedKey = penaltyKeys.Where(k => k <= penaltyCount).DefaultIfEmpty(penaltyKeys.First()).Max();
    
    if (penaltyCount > penaltyKeys.Max())
    {
      selectedKey = penaltyKeys.Max();
    }

    if (!Config.Penalties.TryGetValue(selectedKey.ToString(), out var penalty))
    {
      KickPlayer(player, $"Yetersiz oyun saati ({playerHours}/{Config.RequiredPlaytime} saat)");
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
    var record = GetPlayerRecord(steamIdStr);
    var penaltyCount = (record?.PenaltyCount ?? 0) + 1;
    var playerHours = record?.Playtime ?? 0;

    SavePlayerRecord(steamIdStr, playerHours, penaltyCount);
    ApplyPenalty(player, penaltyCount, playerHours);
  }

  private bool IsIgnored(CCSPlayerController player)
  {
    var steamId = player.SteamID.ToString();
    if (Config.IgnoreSteamIds.Contains(steamId)) return true;

    foreach (var flag in Config.IgnoreFlags)
    {
      if (AdminManager.PlayerHasPermissions(player, flag)) return true;
    }

    return false;
  }

  private void PrintPrefix(CCSPlayerController player, string message)
  {
    var parsed = CC.Parse($"{Config.ChatPrefix} {message}");
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
  public static char White => '\x01';
  public static char Orange => '\x10';

  private static readonly Dictionary<string, char> ColorMap = new(StringComparer.OrdinalIgnoreCase)
  {
    { "Default", '\x01' }, { "White", '\x01' },
    { "Red", '\x07' }, { "LightRed", '\x0F' }, { "DarkRed", '\x02' },
    { "BlueGrey", '\x0A' }, { "Blue", '\x0B' }, { "DarkBlue", '\x0C' },
    { "Purple", '\x0C' }, { "Orchid", '\x0E' },
    { "Yellow", '\x09' }, { "Gold", '\x10' }, { "Orange", '\x10' },
    { "LightGreen", '\x05' }, { "Green", '\x04' }, { "Lime", '\x06' },
    { "Grey", '\x08' }, { "Gray", '\x08' }, { "Grey2", '\x0D' }
  };

  public static string Parse(string message)
  {
    return Regex.Replace(message, @"\{(\w+)\}", match =>
    {
      var colorName = match.Groups[1].Value;
      return ColorMap.TryGetValue(colorName, out var color) ? color.ToString() : match.Value;
    });
  }
}
