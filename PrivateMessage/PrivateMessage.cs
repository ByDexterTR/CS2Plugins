using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.UserMessages;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace PrivateMessage;

public class Pref
{
  public bool PmOff { get; set; }
  public bool SoundOff { get; set; }
}

public class PrivateMessageConfig : BasePluginConfig
{
  [JsonPropertyName("msg_cmd")]
  public string MsgCommands { get; set; } = "css_msg,css_pm";

  [JsonPropertyName("msgoff_cmd")]
  public string OffCommands { get; set; } = "css_msgoff,css_pmoff";

  [JsonPropertyName("msgon_cmd")]
  public string OnCommands { get; set; } = "css_msgon,css_pmon";

  [JsonPropertyName("msgsound_cmd")]
  public string SoundCommands { get; set; } = "css_msgsound,css_pmsound";

  [JsonPropertyName("receive_sound")]
  public string ReceiveSound { get; set; } = "sounds/ambient/common/water/rain_drip3.vsnd";

  [JsonPropertyName("send_sound")]
  public string SendSound { get; set; } = "sounds/ambient/common/water/rain_drip1.vsnd";

  [JsonPropertyName("log_enabled")]
  public bool LogEnabled { get; set; } = false;

  [JsonPropertyName("database")]
  public Dictionary<string, string> Database { get; set; } = new()
  {
    { "provider", "json" }, // "mysql" - "json"
    { "host", "localhost" },
    { "name", "bydexter_pm" },
    { "port", "3306" },
    { "user", "root" },
    { "password", "" }
  };
}

public class PrivateMessage : BasePlugin, IPluginConfig<PrivateMessageConfig>
{
  public override string ModuleName => "PrivateMessage";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public PrivateMessageConfig Config { get; set; } = new();

  private readonly ConcurrentDictionary<ulong, Pref> _prefs = new();
  private readonly HashSet<string> _triggers = new(StringComparer.OrdinalIgnoreCase);
  private string _dbConnectionString = "";
  private bool _useMySql = false;
  private readonly object _ioLock = new();
  private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
  private string JsonPath => Path.Combine(ModuleDirectory, "players.json");

  public void OnConfigParsed(PrivateMessageConfig config)
  {
    Config = config;
    _triggers.Clear();
    foreach (var name in Split($"{config.MsgCommands},{config.OffCommands},{config.OnCommands},{config.SoundCommands}"))
    {
      string alias = name.StartsWith("css_", StringComparison.OrdinalIgnoreCase) ? name[4..] : name;
      _triggers.Add("!" + alias);
      _triggers.Add("/" + alias);
    }
  }

  public override void Load(bool hotReload)
  {
    var provider = Config.Database.TryGetValue("provider", out var p) ? p.ToLower() : "json";

    _useMySql = provider == "mysql" && TryInitMySql();
    if (provider == "mysql" && !_useMySql)
      Logger.LogWarning("[PrivateMessage] MySQL baglantisi basarisiz, JSON'a dusuluyor.");

    if (_useMySql)
      LoadAllFromMySql();
    else
      LoadJsonPrefs();

    foreach (var name in Split(Config.MsgCommands))
      AddCommand(name, "Oyuncuya ozel mesaj gonderir", OnMsgCommand);

    foreach (var name in Split(Config.OffCommands))
      AddCommand(name, "Ozel mesajlari kapatir", OnOffCommand);

    foreach (var name in Split(Config.OnCommands))
      AddCommand(name, "Ozel mesajlari acar", OnOnCommand);

    foreach (var name in Split(Config.SoundCommands))
      AddCommand(name, "Ozel mesaj sesini acar/kapatir", OnSoundCommand);

    HookUserMessage(118, OnChat, HookMode.Pre);
  }

  public override void Unload(bool hotReload)
  {
    UnhookUserMessage(118, OnChat, HookMode.Pre);

    if (!_useMySql)
      WriteJsonPrefs();
  }

  private static string[] Split(string names) =>
    names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

  private HookResult OnChat(UserMessage um)
  {
    string message = um.ReadString("param2").Trim();
    if (message.Length == 0)
      return HookResult.Continue;

    string token = message.Split(' ', 2)[0];
    return _triggers.Contains(token) ? HookResult.Stop : HookResult.Continue;
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
        Exec(conn, @"CREATE TABLE IF NOT EXISTS `pm_preferences` (
          `steamid` VARCHAR(20) PRIMARY KEY,
          `pm_off` TINYINT NOT NULL DEFAULT 0,
          `sound_off` TINYINT NOT NULL DEFAULT 0
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
      }

      return true;
    }
    catch (Exception ex)
    {
      Logger.LogWarning(ex, "[PrivateMessage] MySQL yükleme hatası");
      return false;
    }
  }

  private static void Exec(MySqlConnection conn, string sql)
  {
    using var cmd = new MySqlCommand(sql, conn);
    cmd.ExecuteNonQuery();
  }

  private void LoadAllFromMySql()
  {
    try
    {
      using var conn = new MySqlConnection(_dbConnectionString);
      conn.Open();

      using var cmd = new MySqlCommand("SELECT steamid, pm_off, sound_off FROM `pm_preferences`", conn);
      using var reader = cmd.ExecuteReader();
      while (reader.Read())
      {
        if (!ulong.TryParse(reader.GetString(0), out var steamId))
          continue;

        _prefs[steamId] = new Pref
        {
          PmOff = reader.GetByte(1) != 0,
          SoundOff = reader.GetByte(2) != 0
        };
      }
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[PrivateMessage] Tercihler yüklenemedi");
    }
  }

  private void LoadJsonPrefs()
  {
    lock (_ioLock)
    {
      try
      {
        if (!File.Exists(JsonPath))
          return;

        var raw = JsonSerializer.Deserialize<Dictionary<string, Pref>>(File.ReadAllText(JsonPath));
        if (raw == null)
          return;

        foreach (var (key, value) in raw)
          if (ulong.TryParse(key, out var steamId))
            _prefs[steamId] = value;
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "[PrivateMessage] players.json okunamadı");
      }
    }
  }

  private void WriteJsonPrefs()
  {
    try
    {
      var snapshot = _prefs.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
      lock (_ioLock)
        File.WriteAllText(JsonPath, JsonSerializer.Serialize(snapshot, JsonOpts));
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[PrivateMessage] players.json yazılamadı");
    }
  }

  private async Task SaveToMySqlAsync(ulong steamId, Pref pref)
  {
    try
    {
      using var conn = new MySqlConnection(_dbConnectionString);
      await conn.OpenAsync();

      using var cmd = new MySqlCommand(@"INSERT INTO `pm_preferences` (steamid, pm_off, sound_off)
        VALUES (@s, @p, @o)
        ON DUPLICATE KEY UPDATE pm_off=@p, sound_off=@o", conn);
      cmd.Parameters.AddWithValue("@s", steamId.ToString());
      cmd.Parameters.AddWithValue("@p", pref.PmOff ? (byte)1 : (byte)0);
      cmd.Parameters.AddWithValue("@o", pref.SoundOff ? (byte)1 : (byte)0);
      await cmd.ExecuteNonQueryAsync();
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, $"[PrivateMessage] Tercih kaydedilemedi - SteamID: {steamId}");
    }
  }

  private void SavePreference(ulong steamId, Pref pref)
  {
    if (_useMySql)
    {
      _ = SaveToMySqlAsync(steamId, pref);
      return;
    }

    Task.Run(WriteJsonPrefs);
  }

  [GameEventHandler]
  public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || player.IsBot || player.SteamID == 0)
      return HookResult.Continue;

    if (!_useMySql)
      return HookResult.Continue;

    var steamId = player.SteamID;

    Task.Run(async () =>
    {
      try
      {
        using var conn = new MySqlConnection(_dbConnectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("SELECT pm_off, sound_off FROM `pm_preferences` WHERE steamid = @s", conn);
        cmd.Parameters.AddWithValue("@s", steamId.ToString());
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
          _prefs[steamId] = new Pref
          {
            PmOff = reader.GetByte(0) != 0,
            SoundOff = reader.GetByte(1) != 0
          };
        }
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, $"[PrivateMessage] Oyuncu tercihleri yüklenemedi - SteamID: {steamId}");
      }
    });

    return HookResult.Continue;
  }

  [GameEventHandler]
  public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || player.IsBot || player.SteamID == 0)
      return HookResult.Continue;

    if (_prefs.TryGetValue(player.SteamID, out var pref))
      SavePreference(player.SteamID, pref);

    return HookResult.Continue;
  }

  private Pref GetPref(CCSPlayerController p)
  {
    if (p?.IsValid != true || p.SteamID == 0)
      return new Pref();

    if (!_prefs.TryGetValue(p.SteamID, out var pref))
    {
      pref = new Pref();
      _prefs[p.SteamID] = pref;
    }
    return pref;
  }

  private void OnMsgCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    if (GetPref(player).PmOff)
    {
      Reply(player, Localizer["pm.self_off"]);
      return;
    }

    if (info.ArgCount < 3)
    {
      Reply(player, Localizer["pm.usage"]);
      return;
    }

    string targetArg = info.GetArg(1);
    string message = string.Join(' ', Enumerable.Range(2, info.ArgCount - 2).Select(info.GetArg)).Trim();
    if (message.Length == 0)
    {
      Reply(player, Localizer["pm.usage"]);
      return;
    }

    var target = FindTarget(player, targetArg, out string? error);
    if (target == null)
    {
      Reply(player, error!);
      return;
    }

    if (GetPref(target).PmOff)
    {
      Reply(player, Localizer["pm.target_off", target.PlayerName]);
      return;
    }

    target.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {CC.Gold}{player.PlayerName}{CC.Default}: {message}");
    Reply(player, Localizer["pm.sent", $"{CC.Gold}{target.PlayerName}{CC.Default}"]);

    if (!GetPref(target).SoundOff)
      target.ExecuteClientCommand($"play {Config.ReceiveSound}");
    if (!GetPref(player).SoundOff)
      player.ExecuteClientCommand($"play {Config.SendSound}");

    if (Config.LogEnabled)
      WriteLog(player.PlayerName, target.PlayerName, message);
  }

  private CCSPlayerController? FindTarget(CCSPlayerController sender, string arg, out string? error)
  {
    error = null;
    var players = Utilities.GetPlayers()
      .Where(p => p != null && p.IsValid && !p.IsBot && !p.IsHLTV)
      .ToList();

    var target = players.FirstOrDefault(p => p.PlayerName.Equals(arg, StringComparison.OrdinalIgnoreCase));
    if (target == null)
    {
      var matches = players.Where(p => p.PlayerName.Contains(arg, StringComparison.OrdinalIgnoreCase)).ToList();
      if (matches.Count == 0)
      {
        error = Localizer["pm.not_found", arg];
        return null;
      }
      if (matches.Count > 1)
      {
        error = Localizer["pm.multiple", arg];
        return null;
      }
      target = matches[0];
    }

    if (target.Slot == sender.Slot)
    {
      error = Localizer["pm.self"];
      return null;
    }

    return target;
  }

  private void OnOffCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || player.SteamID == 0)
      return;

    var pref = GetPref(player);
    pref.PmOff = true;
    SavePreference(player.SteamID, pref);
    Reply(player, Localizer["pm.off"]);
  }

  private void OnOnCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || player.SteamID == 0)
      return;

    var pref = GetPref(player);
    pref.PmOff = false;
    SavePreference(player.SteamID, pref);
    Reply(player, Localizer["pm.on"]);
  }

  private void OnSoundCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || player.SteamID == 0)
      return;

    var pref = GetPref(player);
    pref.SoundOff = !pref.SoundOff;
    SavePreference(player.SteamID, pref);
    Reply(player, pref.SoundOff ? Localizer["pm.sound_off"] : Localizer["pm.sound_on"]);
  }

  private void Reply(CCSPlayerController player, string text) =>
    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {text}");

  private void WriteLog(string sender, string receiver, string message)
  {
    string line = $"[{sender} -> {receiver}]: {message}";
    Logger.LogInformation("{Line}", line);

    string logDir = Path.Combine(ModuleDirectory, "logs");
    string logFile = Path.Combine(logDir, $"PrivateMessage-{DateTime.Now:yyyy-MM-dd}.log");
    string fileLine = $"[{DateTime.Now:HH:mm:ss}] {line}";

    Task.Run(() =>
    {
      try
      {
        Directory.CreateDirectory(logDir);
        File.AppendAllLines(logFile, new[] { fileLine });
      }
      catch (Exception ex)
      {
        Logger.LogError("PrivateMessage: log dosyasina yazilamadi. {0}", ex.Message);
      }
    });
  }
}
