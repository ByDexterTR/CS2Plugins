using System.Text.Json;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Collections.Concurrent;
using ByDexter.Shared;

public enum MuteMode : byte { None, Enemy, Team, All }

public class SeslerConfig : BasePluginConfig
{
  public Dictionary<string, string> Database { get; set; } = new Dictionary<string, string>()
  {
    { "provider", "json" }, // "mysql" - "json"
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
  public override string ModuleVersion => "1.1.3";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  public SeslerConfig Config { get; set; } = new();

  private readonly ConcurrentDictionary<ulong, Pref> _prefs = new();
  private string _dbConnectionString = "";
  private bool _useMySql = false;
  private readonly object _ioLock = new();
  private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
  private string JsonPath => Path.Combine(ModuleDirectory, "players.json");

  private static readonly string[] ModeColors = { "green", "orange", "lightblue", "red" };

  private string GetModeLabel(int idx) => idx switch
  {
    0 => Localizer["sesler.mode_open"],
    1 => Localizer["sesler.mode_mute_enemy"],
    2 => Localizer["sesler.mode_mute_team"],
    3 => Localizer["sesler.mode_closed"],
    _ => ""
  };

  public void OnConfigParsed(SeslerConfig config)
  {
    Config = config;
  }

  private WasdMenuManager _menus = null!;

  public override void Load(bool hotReload)
  {
    _menus = new WasdMenuManager(this,
      () => Localizer["menu.scroll"],
      () => Localizer["menu.select"],
      () => Localizer["menu.exit"]);

    var provider = Config.Database.TryGetValue("provider", out var p) ? p.ToLower() : "json";

    _useMySql = provider == "mysql" && TryInitMySql();
    if (provider == "mysql" && !_useMySql)
      Logger.LogWarning("[Sesler] MySQL baglantisi basarisiz, JSON'a dusuluyor.");

    if (_useMySql)
      LoadAllFromMySql();
    else
      LoadJsonPrefs();

    HookUserMessage(208, OnSound, HookMode.Pre);
    HookUserMessage(369, OnWeaponSound, HookMode.Pre);
    HookUserMessage(452, OnWeaponEvent, HookMode.Pre);
    RegisterEventHandler<EventRoundMvp>(OnRoundMvp);
    RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
  }

  public override void Unload(bool hotReload)
  {
    _menus.Clear();
    UnhookUserMessage(208, OnSound, HookMode.Pre);
    UnhookUserMessage(369, OnWeaponSound, HookMode.Pre);
    UnhookUserMessage(452, OnWeaponEvent, HookMode.Pre);

    if (!_useMySql)
      WriteJsonPrefs();
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
        Exec(conn, @"CREATE TABLE IF NOT EXISTS `player_preferences` (
          `steamid` VARCHAR(20) PRIMARY KEY,
          `knife` TINYINT NOT NULL DEFAULT 0,
          `weapon` TINYINT NOT NULL DEFAULT 0,
          `foot` TINYINT NOT NULL DEFAULT 0,
          `player` TINYINT NOT NULL DEFAULT 0,
          `mvp` TINYINT NOT NULL DEFAULT 0
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
      }

      return true;
    }
    catch (Exception ex)
    {
      Logger.LogWarning(ex, "[Sesler] MySQL yükleme hatası");
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

      using var cmd = new MySqlCommand("SELECT steamid, knife, weapon, foot, player, mvp FROM `player_preferences`", conn);
      using var reader = cmd.ExecuteReader();
      while (reader.Read())
      {
        if (!ulong.TryParse(reader.GetString(0), out var steamId))
          continue;

        _prefs[steamId] = new Pref
        {
          Knife = (MuteMode)reader.GetByte(1),
          Weapon = (MuteMode)reader.GetByte(2),
          Foot = (MuteMode)reader.GetByte(3),
          Player = (MuteMode)reader.GetByte(4),
          Mvp = (MuteMode)reader.GetByte(5)
        };
      }
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[Sesler] Tercihler yüklenemedi");
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
        Logger.LogError(ex, "[Sesler] players.json okunamadı");
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
      Logger.LogError(ex, "[Sesler] players.json yazılamadı");
    }
  }

  private async Task SaveToMySqlAsync(ulong steamId, Pref pref)
  {
    try
    {
      using var conn = new MySqlConnection(_dbConnectionString);
      await conn.OpenAsync();

      using var cmd = new MySqlCommand(@"INSERT INTO `player_preferences` (steamid, knife, weapon, foot, player, mvp)
        VALUES (@s, @k, @w, @f, @p, @m)
        ON DUPLICATE KEY UPDATE knife=@k, weapon=@w, foot=@f, player=@p, mvp=@m", conn);
      cmd.Parameters.AddWithValue("@s", steamId.ToString());
      cmd.Parameters.AddWithValue("@k", (byte)pref.Knife);
      cmd.Parameters.AddWithValue("@w", (byte)pref.Weapon);
      cmd.Parameters.AddWithValue("@f", (byte)pref.Foot);
      cmd.Parameters.AddWithValue("@p", (byte)pref.Player);
      cmd.Parameters.AddWithValue("@m", (byte)pref.Mvp);
      await cmd.ExecuteNonQueryAsync();
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, $"[Sesler] Tercih kaydedilemedi - SteamID: {steamId}");
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

  private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || player.IsBot || player.SteamID == 0) return HookResult.Continue;

    if (!_useMySql)
      return HookResult.Continue;

    var steamId = player.SteamID;

    Task.Run(async () =>
    {
      try
      {
        using var conn = new MySqlConnection(_dbConnectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("SELECT knife, weapon, foot, player, mvp FROM `player_preferences` WHERE steamid = @s", conn);
        cmd.Parameters.AddWithValue("@s", steamId.ToString());
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
          _prefs[steamId] = new Pref
          {
            Knife = (MuteMode)reader.GetByte(0),
            Weapon = (MuteMode)reader.GetByte(1),
            Foot = (MuteMode)reader.GetByte(2),
            Player = (MuteMode)reader.GetByte(3),
            Mvp = (MuteMode)reader.GetByte(4)
          };
        }
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, $"[Sesler] Oyuncu tercihleri yüklenemedi - SteamID: {steamId}");
      }
    });

    return HookResult.Continue;
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || player.IsBot || player.SteamID == 0) return HookResult.Continue;

    if (_prefs.TryGetValue(player.SteamID, out var pref)) SavePreference(player.SteamID, pref);

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

    var categories = new (string Label, Func<Pref, MuteMode> Get, Action<Pref, MuteMode> Set, bool MvpOnly)[]
    {
      (Localizer["sesler.category_knife"], p => p.Knife, (p, m) => p.Knife = m, false),
      (Localizer["sesler.category_weapon"], p => p.Weapon, (p, m) => p.Weapon = m, false),
      (Localizer["sesler.category_foot"], p => p.Foot, (p, m) => p.Foot = m, false),
      (Localizer["sesler.category_player"], p => p.Player, (p, m) => p.Player = m, false),
      (Localizer["sesler.category_mvp"], p => p.Mvp, (p, m) => p.Mvp = m, true)
    };

    var items = new List<WasdItem>();
    foreach (var category in categories)
    {
      var current = category.Get(pref);
      items.Add(new WasdItem
      {
        Text = $"{category.Label}: {StateLabel(current, category.MvpOnly)}",
        OnSelect = p => ShowSubMenu(p, category.Label, category.Get, category.Set, category.MvpOnly)
      });
    }

    _menus.Open(player, Localizer["sesler.menu_title"], items);
  }

  private void ShowSubMenu(CCSPlayerController player, string label, Func<Pref, MuteMode> get, Action<Pref, MuteMode> set, bool mvpOnly = false)
  {
    var pref = GetPref(player);

    int maxOptions = mvpOnly ? 2 : 4;
    int step = mvpOnly ? 3 : 1;

    var items = new List<WasdItem>();
    for (int i = 0; i < maxOptions; i++)
    {
      var modeIndex = mvpOnly ? i * step : i;
      var mode = (MuteMode)modeIndex;
      var current = get(pref);
      var prefix = current == mode ? "► " : "";
      var color = ModeColors[modeIndex];
      var labelText = GetModeLabel(modeIndex);

      items.Add(new WasdItem
      {
        Text = $"{prefix}<font color='{color}'>{labelText}</font>",
        OnSelect = p =>
        {
          set(pref, mode);
          SavePreference(p.SteamID, pref);
          ShowMainMenu(p);
        }
      });
    }

    items.Add(new WasdItem { Text = Localizer["sesler.back"], OnSelect = p => ShowMainMenu(p) });

    _menus.Open(player, label, items);
  }

  private string StateLabel(MuteMode mode, bool mvpOnly = false)
  {
    if (mvpOnly && mode != MuteMode.None && mode != MuteMode.All)
      mode = MuteMode.All;

    var idx = (int)mode;
    return $"<font color='{ModeColors[idx]}'>{GetModeLabel(idx)}</font>";
  }

  private HookResult OnSound(UserMessage msg)
  {
    try
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
    catch (Exception ex)
    {
      Logger.LogError(ex, "[Sesler] OnSound hook error");
      return HookResult.Continue;
    }
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
    try
    {
      var entityIndex = um.ReadInt("entidx");
      var entity = Utilities.GetEntityFromIndex<CBaseEntity>(entityIndex);
      if (entity == null || !entity.IsValid) return HookResult.Continue;

      var pawn = entity.As<CBasePlayerPawn>();
      if (pawn == null || !pawn.IsValid || pawn.DesignerName != "player") return HookResult.Continue;

      var soundName = um.ReadString("sound") ?? string.Empty;
      if (soundName.Length == 0) return HookResult.Continue;

      bool looksLikeWeapon = soundName.Contains("weapons/", StringComparison.OrdinalIgnoreCase) || soundName.Contains("weapon_", StringComparison.OrdinalIgnoreCase) || soundName.Contains("wpn_", StringComparison.OrdinalIgnoreCase);
      if (!looksLikeWeapon) return HookResult.Continue;

      FilterRecipients(um, entity, pref => pref.Weapon);
      return um.Recipients.Count == 0 ? HookResult.Stop : HookResult.Continue;
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[Sesler] OnWeaponSound hook error");
      return HookResult.Continue;
    }
  }

  private HookResult OnWeaponEvent(UserMessage um)
  {
    try
    {
      var entityHandle = um.ReadUInt("player");
      var entity = Utilities.GetEntityFromIndex<CBaseEntity>((int)(entityHandle & 0x7FF));
      if (entity == null || !entity.IsValid) return HookResult.Continue;

      FilterRecipients(um, entity, pref => pref.Weapon);
      return um.Recipients.Count == 0 ? HookResult.Stop : HookResult.Continue;
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[Sesler] OnWeaponEvent hook error");
      return HookResult.Continue;
    }
  }

  private HookResult OnRoundMvp(EventRoundMvp @event, GameEventInfo info)
  {
    if (@event == null) return HookResult.Continue;

    var mvp = @event.Userid;
    if (mvp == null || !mvp.IsValid) return HookResult.Continue;

    var worldEntity = Utilities.GetEntityFromIndex<CBaseEntity>(0);
    if (worldEntity == null || !worldEntity.IsValid || !worldEntity.DesignerName.Contains("world")) return HookResult.Continue;

    foreach (var player in Utilities.GetPlayers())
    {
      if (player == null || !player.IsValid) continue;
      if (GetPref(player).Mvp != MuteMode.All) continue;

      worldEntity.EmitSound("StopSoundEvents.StopAllMusic", new RecipientFilter(player));
    }

    return HookResult.Continue;
  }

  private void FilterRecipients(UserMessage msg, CBaseEntity? soundSource, Func<Pref, MuteMode> getMode)
  {
    if (msg.Recipients == null) return;

    for (int i = msg.Recipients.Count - 1; i >= 0; i--)
    {
      var listener = msg.Recipients[i];
      if (listener == null || !listener.IsValid) continue;

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
  public MuteMode Knife { get; set; }
  public MuteMode Foot { get; set; }
  public MuteMode Player { get; set; }
  public MuteMode Weapon { get; set; }
  public MuteMode Mvp { get; set; }
}
