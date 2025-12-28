using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Dapper;

public enum MuteMode : byte { None, Enemy, Team, All }

public class SeslerConfig : BasePluginConfig
{
  public Dictionary<string, string> Database { get; set; } = new Dictionary<string, string>()
  {
    { "provider", "sqlite" }, // "mysql" - "sqlite"
    { "host", "localhost" },
    { "name", "bydexter_sesler" },
    { "port", "3306" },
    { "user", "root" },
    { "password", "" }
  };
}

public class Sesler : BasePlugin, IPluginConfig<SeslerConfig>
{
  public override string ModuleName => "Sesler";
  public override string ModuleVersion => "1.0.7";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "Oyuncu ses kontrolü - Bıçak, Silah, Ayak/Yürüme, Oyuncu/Hasar, MVP Müzik";

  public SeslerConfig Config { get; set; } = new();

  private readonly Dictionary<ulong, Pref> _prefs = new();
  private string _dbConnectionString = "";
  private bool _usingSqlite = false;
  private bool _databaseInitialized = false;
  private readonly object _dbLock = new();
  private readonly List<Task> _pendingSaves = new();

  private static readonly string[] ModeLabels = { "Açık", "Düşmanı Sustur", "Takımı Sustur", "Kapalı" };
  private static readonly string[] ModeColors = { "green", "orange", "lightblue", "red" };

  public void OnConfigParsed(SeslerConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    var provider = Config.Database.TryGetValue("provider", out var p) ? p.ToLower() : "sqlite";

    if (provider == "mysql")
    {
      if (!TryLoadMySQL())
      {
        Logger.LogWarning("[Sesler] MySQL yüklenemedi, SQLite'a geçiliyor");
        if (!TryLoadSQLite())
        {
          throw new Exception("[Sesler] Hiçbir veritabanı yüklenemedi! Eklenti çalışamıyor.");
        }
      }
    }
    else
    {
      if (!TryLoadSQLite())
      {
        Logger.LogWarning("[Sesler] SQLite yüklenemedi, MySQL'e geçiliyor");
        if (!TryLoadMySQL())
        {
          throw new Exception("[Sesler] Hiçbir veritabanı yüklenemedi! Eklenti çalışamıyor.");
        }
      }
    }

    InitializeDatabase();
    LoadPreferencesFromDatabase();

    HookUserMessage(208, OnSound, HookMode.Pre);
    HookUserMessage(369, OnWeaponSound, HookMode.Pre);
    HookUserMessage(452, OnWeaponEvent, HookMode.Pre);
    RegisterEventHandler<EventRoundMvp>(OnRoundMvp);
    RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
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
      Logger.LogWarning(ex, "[Sesler] MySQL yükleme hatası");
      return false;
    }
  }

  private bool TryLoadSQLite()
  {
    try
    {
      _usingSqlite = true;
      var fullPath = Path.Combine(ModuleDirectory, "Sesler.sqlite");
      _dbConnectionString = $"Data Source={fullPath}";
      return true;
    }
    catch (Exception ex)
    {
      Logger.LogWarning(ex, "[Sesler] SQLite yükleme hatası");
      return false;
    }
  }

  private async Task EnsurePlayerPreferencesTableAsync()
  {
    try
    {
      using var conn = new SqliteConnection(_dbConnectionString);
      await conn.OpenAsync();
      var sql = @"CREATE TABLE IF NOT EXISTS player_preferences (
            steamid TEXT PRIMARY KEY,
            knife INTEGER NOT NULL DEFAULT 0,
            weapon INTEGER NOT NULL DEFAULT 0,
            foot INTEGER NOT NULL DEFAULT 0,
            player INTEGER NOT NULL DEFAULT 0,
            mvp INTEGER NOT NULL DEFAULT 0
          )";
        await conn.ExecuteAsync(sql);
    }
    catch (Exception ex)
    {
      Server.NextFrame(() => Logger.LogError(ex, "[Sesler] EnsurePlayerPreferencesTableAsync failed"));
      throw;
    }
  }

  public override void Unload(bool hotReload)
  {
    var saveTasks = new List<Task>();
    foreach (var kvp in _prefs)
    {
      saveTasks.Add(SavePreferenceToDatabaseAsync(kvp.Key, kvp.Value));
    }

    saveTasks.AddRange(_pendingSaves);
    Task.WaitAll(saveTasks.ToArray(), TimeSpan.FromSeconds(10));

    UnhookUserMessage(208, OnSound, HookMode.Pre);
    UnhookUserMessage(369, OnWeaponSound, HookMode.Pre);
    UnhookUserMessage(452, OnWeaponEvent, HookMode.Pre);
  }

  private void InitializeDatabase()
  {
    lock (_dbLock)
    {
      if (_databaseInitialized) return;

      try
      {
        if (string.IsNullOrEmpty(_dbConnectionString))
          throw new Exception("Database connection string boş!");

        if (_usingSqlite)
        {
            using var conn = new SqliteConnection(_dbConnectionString);
          conn.Open();

          conn.Execute(@"CREATE TABLE IF NOT EXISTS player_preferences (
            steamid TEXT PRIMARY KEY,
            knife INTEGER NOT NULL DEFAULT 0,
            weapon INTEGER NOT NULL DEFAULT 0,
            foot INTEGER NOT NULL DEFAULT 0,
            player INTEGER NOT NULL DEFAULT 0,
            mvp INTEGER NOT NULL DEFAULT 0
          )");
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
            conn.Open();
            conn.Execute($"CREATE DATABASE IF NOT EXISTS `{dbName}`");
          }

          using var connWithDb = new MySqlConnection(_dbConnectionString);
          connWithDb.Open();

          connWithDb.Execute(@"CREATE TABLE IF NOT EXISTS `player_preferences` (
            `steamid` VARCHAR(20) PRIMARY KEY,
            `knife` TINYINT NOT NULL DEFAULT 0,
            `weapon` TINYINT NOT NULL DEFAULT 0,
            `foot` TINYINT NOT NULL DEFAULT 0,
            `player` TINYINT NOT NULL DEFAULT 0,
            `mvp` TINYINT NOT NULL DEFAULT 0
          ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
        }

        _databaseInitialized = true;
        LoadPreferencesFromDatabase();
      }
      catch (Exception ex)
      {
        Server.NextFrame(() => Logger.LogError(ex, "[Sesler] Veritabanı başlatma hatası"));
        throw;
      }
    }
  }

  private void LoadPreferencesFromDatabase()
  {
    try
    {
      if (_usingSqlite)
      {
          using var conn = new SqliteConnection(_dbConnectionString);
        conn.Open();

        IEnumerable<dynamic> rows;
        try
        {
          rows = conn.Query("SELECT * FROM player_preferences");
        }
        catch (SqliteException sex) when (sex.Message?.Contains("no such table") == true)
        {
          Server.NextFrame(() => Logger.LogWarning(sex, "[Sesler] player_preferences table missing, attempting to create it and retry load"));
          EnsurePlayerPreferencesTableAsync().Wait();
          rows = conn.Query("SELECT * FROM player_preferences");
        }

        int loadedCount = 0;
        foreach (var row in rows)
        {
          string steamId = row.steamid;
          if (ulong.TryParse(steamId, out ulong steamIdNum))
          {
            var pref = new Pref
            {
              Knife = (MuteMode)(int)row.knife,
              Weapon = (MuteMode)(int)row.weapon,
              Foot = (MuteMode)(int)row.foot,
              Player = (MuteMode)(int)row.player,
              Mvp = (MuteMode)(int)row.mvp
            };

            var capturedSteamId = steamIdNum;
            var capturedPref = pref;
            Server.NextFrame(() => _prefs[capturedSteamId] = capturedPref);
            loadedCount++;
          }
        }
      }
      else
      {
        using var conn = new MySqlConnection(_dbConnectionString);
        conn.Open();

        var rows = conn.Query("SELECT * FROM player_preferences");

        int loadedCount = 0;
        foreach (var row in rows)
        {
          string steamId = row.steamid;
          if (ulong.TryParse(steamId, out ulong steamIdNum))
          {
            var pref = new Pref
            {
              Knife = (MuteMode)(byte)row.knife,
              Weapon = (MuteMode)(byte)row.weapon,
              Foot = (MuteMode)(byte)row.foot,
              Player = (MuteMode)(byte)row.player,
              Mvp = (MuteMode)(byte)row.mvp
            };

            var capturedSteamId = steamIdNum;
            var capturedPref = pref;
            Server.NextFrame(() => _prefs[capturedSteamId] = capturedPref);
            loadedCount++;
          }
        }
      }
    }
    catch (Exception ex)
    {
      Server.NextFrame(() => Logger.LogError(ex, "[Sesler] Tercihler yüklenemedi"));
    }
  }

  private async Task SavePreferenceToDatabaseAsync(ulong steamId, Pref pref)
  {
    try
    {
      if (_usingSqlite)
      {
          using var conn = new SqliteConnection(_dbConnectionString);
        await conn.OpenAsync();

        var sql = @"INSERT OR REPLACE INTO player_preferences (steamid, knife, weapon, foot, player, mvp)
          VALUES (@steamid, @knife, @weapon, @foot, @player, @mvp)";

        var rowsAffected = await conn.ExecuteAsync(sql, new
        {
          steamid = steamId.ToString(),
          knife = (byte)pref.Knife,
          weapon = (byte)pref.Weapon,
          foot = (byte)pref.Foot,
          player = (byte)pref.Player,
          mvp = (byte)pref.Mvp
        });
      }
      else
      {
        using var conn = new MySqlConnection(_dbConnectionString);
        await conn.OpenAsync();

        var sql = @"INSERT INTO player_preferences (steamid, knife, weapon, foot, player, mvp)
          VALUES (@steamid, @knife, @weapon, @foot, @player, @mvp)
          ON DUPLICATE KEY UPDATE knife=@knife, weapon=@weapon, foot=@foot, player=@player, mvp=@mvp";

        var rowsAffected = await conn.ExecuteAsync(sql, new
        {
          steamid = steamId.ToString(),
          knife = (byte)pref.Knife,
          weapon = (byte)pref.Weapon,
          foot = (byte)pref.Foot,
          player = (byte)pref.Player,
          mvp = (byte)pref.Mvp
        });
      }
    }
    catch (Exception ex)
    {
      if (_usingSqlite && ex is SqliteException sqlex && sqlex.Message?.Contains("no such table") == true)
      {
        try
        {
          await EnsurePlayerPreferencesTableAsync();
          using var conn = new SqliteConnection(_dbConnectionString);
          await conn.OpenAsync();

          var sql = @"INSERT OR REPLACE INTO player_preferences (steamid, knife, weapon, foot, player, mvp)
            VALUES (@steamid, @knife, @weapon, @foot, @player, @mvp)";

          await conn.ExecuteAsync(sql, new
          {
            steamid = steamId.ToString(),
            knife = (byte)pref.Knife,
            weapon = (byte)pref.Weapon,
            foot = (byte)pref.Foot,
            player = (byte)pref.Player,
            mvp = (byte)pref.Mvp
          });

          return;
        }
        catch (Exception rex)
        {
          Server.NextFrame(() => Logger.LogError(rex, $"[Sesler] Tercih kaydedilemedi after recreate - SteamID: {steamId}"));
          return;
        }
      }

      Server.NextFrame(() => Logger.LogError(ex, $"[Sesler] Tercih kaydedilemedi - SteamID: {steamId}"));
    }
  }

  private void SavePreferenceToDatabase(ulong steamId, Pref pref)
  {
    var task = SavePreferenceToDatabaseAsync(steamId, pref);
    _pendingSaves.Add(task);

    _pendingSaves.RemoveAll(t => t.IsCompleted);
  }

  private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player?.IsValid != true || player.IsBot || player.SteamID == 0)
      return HookResult.Continue;

    Task.Run(async () =>
    {
      try
      {
        if (_usingSqlite)
        {
              using var conn = new SqliteConnection(_dbConnectionString);
              await conn.OpenAsync();

              var row = await conn.QueryFirstOrDefaultAsync("SELECT * FROM player_preferences WHERE steamid = @steamid",
                new { steamid = player.SteamID.ToString() });

          if (row != null)
          {
            var pref = new Pref
            {
              Knife = (MuteMode)(int)row.knife,
              Weapon = (MuteMode)(int)row.weapon,
              Foot = (MuteMode)(int)row.foot,
              Player = (MuteMode)(int)row.player,
              Mvp = (MuteMode)(int)row.mvp
            };

            var capturedSteamId = player.SteamID;
            var capturedPref = pref;
            Server.NextFrame(() =>
            {
              _prefs[capturedSteamId] = capturedPref;
            });
          }
        }
        else
        {
          using var conn = new MySqlConnection(_dbConnectionString);
          await conn.OpenAsync();

          var row = await conn.QueryFirstOrDefaultAsync("SELECT * FROM player_preferences WHERE steamid = @steamid",
            new { steamid = player.SteamID.ToString() });

          if (row != null)
          {
            var pref = new Pref
            {
              Knife = (MuteMode)(byte)row.knife,
              Weapon = (MuteMode)(byte)row.weapon,
              Foot = (MuteMode)(byte)row.foot,
              Player = (MuteMode)(byte)row.player,
              Mvp = (MuteMode)(byte)row.mvp
            };

            var capturedSteamId = player.SteamID;
            var capturedPref = pref;
            Server.NextFrame(() =>
            {
              _prefs[capturedSteamId] = capturedPref;
            });
          }
        }
      }
      catch (Exception ex)
      {
        Server.NextFrame(() => Logger.LogError(ex, $"[Sesler] Oyuncu tercihleri yüklenemedi - SteamID: {player.SteamID}"));
      }
    });

    return HookResult.Continue;
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player?.IsValid == true && !player.IsBot && player.SteamID != 0)
    {
      if (_prefs.TryGetValue(player.SteamID, out var pref))
      {
        SavePreferenceToDatabase(player.SteamID, pref);
      }
    }
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

  [ConsoleCommand("css_ses", "Ses menüsünü açar")]
  [ConsoleCommand("css_sesler", "Ses menüsünü açar")]
  public void OnCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player?.IsValid != true || player.IsBot || player.SteamID == 0) return;
    ShowMainMenu(player);
  }

  private void ShowMainMenu(CCSPlayerController player)
  {
    var pref = GetPref(player);
    var menu = new CenterHtmlMenu("<font color='#8899a6' class='fontSize-l'><img src='https://images.weserv.nl/?url=em-content.zobj.net/source/twitter/408/speaker-high-volume_1f50a.png&w=24&h=24&fit=cover'> Sesler <img src='https://images.weserv.nl/?url=em-content.zobj.net/source/twitter/408/speaker-high-volume_1f50a.png&w=24&h=24&fit=cover'></font>", this);

    var items = new (string Label, Func<Pref, MuteMode> Get, Action<Pref, MuteMode> Set, bool MvpOnly)[]
    {
      ("Bıçak", p => p.Knife, (p, m) => p.Knife = m, false),
      ("Silah", p => p.Weapon, (p, m) => p.Weapon = m, false),
      ("Ayak/Yürüme", p => p.Foot, (p, m) => p.Foot = m, false),
      ("Oyuncu/Hasar", p => p.Player, (p, m) => p.Player = m, false),
      ("MVP Müzik", p => p.Mvp, (p, m) => p.Mvp = m, true)
    };

    foreach (var item in items)
    {
      var current = item.Get(pref);
      menu.AddMenuOption($"{item.Label}: {StateLabel(current, item.MvpOnly)}", (p, o) =>
      {
        ShowSubMenu(player, item.Label, item.Get, item.Set, item.MvpOnly);
      });
    }

    MenuManager.OpenCenterHtmlMenu(this, player, menu);
  }

  private void ShowSubMenu(CCSPlayerController player, string label, Func<Pref, MuteMode> get, Action<Pref, MuteMode> set, bool mvpOnly = false)
  {
    var pref = GetPref(player);
    var menu = new CenterHtmlMenu($"<font color='#8899a6' class='fontSize-l'><img src='https://images.weserv.nl/?url=em-content.zobj.net/source/twitter/408/speaker-high-volume_1f50a.png&w=24&h=24&fit=cover'> {label} <img src='https://images.weserv.nl/?url=em-content.zobj.net/source/twitter/408/speaker-high-volume_1f50a.png&w=24&h=24&fit=cover'></font>", this);

    int maxOptions = mvpOnly ? 2 : 4;
    int step = mvpOnly ? 3 : 1;

    for (int i = 0; i < maxOptions; i++)
    {
      var modeIndex = mvpOnly ? i * step : i;
      var mode = (MuteMode)modeIndex;
      var current = get(pref);
      var prefix = current == mode ? "► " : "";
      var color = ModeColors[modeIndex];
      var labelText = ModeLabels[modeIndex];

      menu.AddMenuOption($"{prefix}<font color='{color}'>{labelText}</font>", (p, o) =>
      {
        set(pref, mode);
        SavePreferenceToDatabase(player.SteamID, pref);
        ShowMainMenu(player);
      });
    }

    menu.AddMenuOption("Geri", (p, o) => ShowMainMenu(player));
    menu.ExitButton = false;
    MenuManager.OpenCenterHtmlMenu(this, player, menu);
  }

  private string StateLabel(MuteMode mode, bool mvpOnly = false)
  {
    var idx = (int)mode;
    if (mvpOnly && mode != MuteMode.None && mode != MuteMode.All)
      mode = MuteMode.All;

    idx = (int)mode;
    return $"<font color='{ModeColors[idx]}'>{ModeLabels[idx]}</font>";
  }

  private HookResult OnSound(UserMessage msg)
  {
    var hash = msg.ReadUInt("soundevent_hash");

    var soundType = GetSoundType(hash);
    if (soundType == SoundType.None) return HookResult.Continue;

    var entityIndex = msg.ReadInt("source_entity_index");
    var entity = Utilities.GetEntityFromIndex<CBaseEntity>(entityIndex);

    FilterRecipients(msg, entity, pref => soundType switch
    {
      SoundType.Knife => pref.Knife,
      SoundType.Foot => pref.Foot,
      SoundType.Player => pref.Player,
      _ => MuteMode.None
    });

    return msg.Recipients.Count == 0 ? HookResult.Stop : HookResult.Continue;
  }

  private enum SoundType : byte { None, Knife, Foot, Player }

  private static readonly HashSet<uint> KnifeHashes = new()
  {
    3475734633, 1769891506, 3634660983
  };

  private static readonly HashSet<uint> FootHashes = new()
  {
    2800858936, 70011614, 1194677450, 1016523349, 2240518199, 3218103073, 520432428, 1818046345, 2207486967, 2302139631, 1939055066, 1409986305, 1803111098, 4113422219, 3997353267, 3009312615, 123085364, 782454593, 3257325156, 3434104102, 2745524735, 117596568, 29217150, 3460445620, 2684452812, 2067683805, 1388885460, 413358161, 988265811, 3802757032, 2633527058, 1627020521, 602548457, 859178236, 3749333696, 2899365092, 2061955732, 1535891875, 3368720745, 3057812547, 135189076, 2790760284, 2448803175, 3753692454, 3666896632, 3166948458, 3099536373, 1690105992, 115843229, 1763490157, 2546391140, 515548944, 1517575510, 1248619277, 1395892944, 2300993891, 1183624286, 540697918, 2829617974, 1826799645, 3193435079, 2860219006, 1855038793, 2892812682, 3342414459, 144629619, 721782259, 2133235849, 3161194970, 819435812, 2804393637, 4222899547, 1664187801, 2714245023, 1692050905, 961838155, 2638406226, 3008782656, 2070478448, 1247386781, 58439651, 3172583021, 1557420499, 1485322532, 1598540856, 4163677892, 4082928848, 2708661994, 893108375, 1506215040, 2231399653, 1116700262, 2594927130, 1019414932, 1218015996, 417910549, 3299941720, 931543849, 2026488395, 84876002, 1403457606, 2189706910, 1543034, 892882552, 70939233, 1404198078, 1664329401, 822973253, 3797950766, 4203793682, 3952104171, 1163426340, 870100484, 935062317, 1161855519, 1253503839, 1635413700, 2333790984, 96240187, 1165397261, 4084367249, 3109879199, 3984387113, 4045299578, 2551626319, 2479376962, 4085076160, 1661204257, 2236021746, 1440734007, 585390608, 1194093029, 3755338324, 4152012084, 757978684, 1448154350, 2053595705, 1909915699, 765706800, 2722081556, 1540837791, 3123711576, 1770765328, 1761772772, 1424056132, 4160462271, 3806690332, 740474905
  };

  private static readonly HashSet<uint> PlayerHashes = new()
  {
    3688939408, 2703682875, 46413566, 2735369596, 1961884255, 318971924, 662078688, 3469219129, 4161440937, 3568181087, 663530947, 1499777741, 202030084, 3065316423, 1682747253, 427534867, 2369733616, 3666239815, 297379099, 2804654127, 4188085033, 3030200692, 1734994609, 4077119393, 2696334288, 129081149, 2158707679, 3601478655, 3616089666, 2064477315, 1489357772, 3745215916, 839762874, 850911881, 4146949428, 4204174059, 1412313471, 1792523944, 1815352525, 2967038404, 142772671, 1407794113, 3204513405, 2883205713, 769561685, 3103360935, 2381346641, 803727624, 1284373691, 1543118744, 2056150061, 3767841471, 3988751453, 1771184788, 708038349, 3049902652, 3638082858, 1193078452, 3535174312, 2831007164, 524041390, 2447320252, 3124768561, 856190898, 3663341586, 1904605142, 795825195, 4242317911, 4002300972, 3259510958, 2106508305, 963985059, 62938228, 3926353328, 282152614, 2284698275, 2019962436, 3663896169, 3573863551, 1823342283, 2192712263, 3396420465, 2323025056, 3524038396, 2719685137, 2310318859, 2020934318, 3740948313, 2902143738, 400609565, 2316086169, 604181152, 2486534908
  };

  private static readonly Dictionary<uint, SoundType> SoundTypeMap = BuildSoundTypeMap();

  private static Dictionary<uint, SoundType> BuildSoundTypeMap()
  {
    var map = new Dictionary<uint, SoundType>();
    foreach (var hash in KnifeHashes) map[hash] = SoundType.Knife;
    foreach (var hash in FootHashes) map[hash] = SoundType.Foot;
    foreach (var hash in PlayerHashes) map[hash] = SoundType.Player;
    return map;
  }

  private SoundType GetSoundType(uint hash)
  {
    return SoundTypeMap.TryGetValue(hash, out var type) ? type : SoundType.None;
  }

  private HookResult OnWeaponSound(UserMessage um)
  {
    var entityIndex = um.ReadInt("entidx");
    var entity = Utilities.GetEntityFromIndex<CBaseEntity>(entityIndex);
    if (entity?.IsValid != true)
      return HookResult.Continue;

    var pawn = entity.As<CBasePlayerPawn>();
    if (pawn?.IsValid != true || pawn.DesignerName != "player")
      return HookResult.Continue;

    var soundName = um.ReadString("sound") ?? string.Empty;
    if (soundName.Length == 0)
      return HookResult.Continue;

    bool looksLikeWeapon = soundName.Contains("weapons/", StringComparison.OrdinalIgnoreCase)
                        || soundName.Contains("weapon_", StringComparison.OrdinalIgnoreCase)
                        || soundName.Contains("wpn_", StringComparison.OrdinalIgnoreCase);
    if (!looksLikeWeapon)
      return HookResult.Continue;

    FilterRecipients(um, entity, pref => pref.Weapon);
    return um.Recipients.Count == 0 ? HookResult.Stop : HookResult.Continue;
  }

  private HookResult OnWeaponEvent(UserMessage um)
  {
    var entityHandle = um.ReadUInt("player");
    var entity = Utilities.GetEntityFromIndex<CBaseEntity>((int)(entityHandle & 0x7FF));
    if (entity?.IsValid != true)
      return HookResult.Continue;

    FilterRecipients(um, entity, pref => pref.Weapon);
    return um.Recipients.Count == 0 ? HookResult.Stop : HookResult.Continue;
  }

  private HookResult OnRoundMvp(EventRoundMvp @event, GameEventInfo info)
  {
    try
    {
      if (@event?.Userid?.IsValid != true) return HookResult.Continue;

      var worldEntity = Utilities.GetEntityFromIndex<CBaseEntity>(0);
      if (worldEntity?.IsValid != true) return HookResult.Continue;

      foreach (var player in Utilities.GetPlayers())
      {
        if (player?.IsValid != true || player.Connected != PlayerConnectedState.PlayerConnected) continue;
        if (GetPref(player).Mvp != MuteMode.All) continue;

        worldEntity.EmitSound("StopSoundEvents.StopAllMusic", new RecipientFilter(player));
      }
    }
    catch (CounterStrikeSharp.API.Core.NativeException nex)
    {
      Server.NextFrame(() => Logger.LogWarning(nex, "[Sesler] Game event field access failed in OnRoundMvp - ignoring event"));
      return HookResult.Continue;
    }
    catch (Exception ex)
    {
      Server.NextFrame(() => Logger.LogError(ex, "[Sesler] Unexpected error in OnRoundMvp"));
      return HookResult.Continue;
    }

    return HookResult.Continue;
  }

  private void FilterRecipients(UserMessage msg, CBaseEntity? soundSource, Func<Pref, MuteMode> getMode)
  {
    for (int i = msg.Recipients.Count - 1; i >= 0; i--)
    {
      var listener = msg.Recipients[i];
      if (listener?.IsValid != true) continue;

      var pref = GetPref(listener);
      var mode = getMode(pref);

      if (ShouldMute(listener, soundSource, mode))
        msg.Recipients.RemoveAt(i);
    }
  }

  private bool ShouldMute(CCSPlayerController listener, CBaseEntity? soundSource, MuteMode mode)
  {
    if (mode == MuteMode.None) return false;
    if (mode == MuteMode.All) return true;

    if (soundSource == null) return false;

    var listenerTeam = listener.TeamNum;
    var sourceTeam = soundSource.TeamNum;

    if (mode == MuteMode.Enemy)
      return listenerTeam != sourceTeam;

    if (mode == MuteMode.Team)
      return listenerTeam == sourceTeam;

    return false;
  }
}

public class Pref
{
  public MuteMode Knife;
  public MuteMode Foot;
  public MuteMode Player;
  public MuteMode Weapon;
  public MuteMode Mvp;
}