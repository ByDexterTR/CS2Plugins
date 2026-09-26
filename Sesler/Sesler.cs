using System.Text.Json;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Collections.Concurrent;
using ByDexter.Shared;

public enum MuteMode : byte { None, Enemy, Team, All }

public enum SoundType : byte { Knife, Weapon, Foot, Player, Mvp }

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
  public override string ModuleVersion => "1.1.7";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  public SeslerConfig Config { get; set; } = new();

  private const int SoundMessage = 208;
  private const int WeaponSoundMessage = 369;
  private const int FireBulletsMessage = 452;
  private const int MaxSlots = 64;

  private readonly ConcurrentDictionary<ulong, Pref> _prefs = new();
  private readonly Pref?[] _slotPrefs = new Pref?[MaxSlots];
  private readonly ulong[] _slotSteamIds = new ulong[MaxSlots];

  private string _dbConnectionString = "";
  private bool _useMySql;
  private bool _storageReady;
  private volatile bool _unloaded;

  private readonly object _ioLock = new();
  private int _jsonVersion;
  private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
  private string JsonPath => Path.Combine(ModuleDirectory, "players.json");

  private UserMessage.UserMessageHandler _onSound = null!;
  private UserMessage.UserMessageHandler _onWeaponSound = null!;
  private UserMessage.UserMessageHandler _onFireBullets = null!;
  private bool _soundHooked;
  private bool _weaponSoundHooked;
  private bool _fireBulletsHooked;

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

    _onSound = OnSound;
    _onWeaponSound = OnWeaponSound;
    _onFireBullets = OnFireBullets;

    RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorized);
    RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
    RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
    RegisterEventHandler<EventRoundMvp>(OnRoundMvp);

    var provider = Config.Database.TryGetValue("provider", out var p) ? p.ToLowerInvariant() : "json";

    if (provider != "mysql")
    {
      LoadJsonPrefs();
      _storageReady = true;
      if (hotReload)
        BindConnectedPlayers();
      return;
    }

    Task.Run(() =>
    {
      bool ok = TryInitMySql();
      Server.NextFrame(() =>
      {
        if (_unloaded)
          return;

        _useMySql = ok;
        if (!ok)
        {
          Logger.LogWarning("[Sesler] MySQL baglantisi basarisiz, JSON'a dusuluyor.");
          LoadJsonPrefs();
        }

        _storageReady = true;
        BindConnectedPlayers();
      });
    });
  }

  public override void Unload(bool hotReload)
  {
    _unloaded = true;
    _menus.Clear();

    SetHook(SoundMessage, _onSound, false, ref _soundHooked);
    SetHook(WeaponSoundMessage, _onWeaponSound, false, ref _weaponSoundHooked);
    SetHook(FireBulletsMessage, _onFireBullets, false, ref _fireBulletsHooked);

    if (_storageReady && !_useMySql)
      WriteJsonPrefs(SerializePrefs(), Interlocked.Increment(ref _jsonVersion));
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
        Pooling = true,
        ConnectionTimeout = 5
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
          if (ulong.TryParse(key, out var steamId) && IsValidSteamId(steamId) && value != null)
            _prefs[steamId] = value;
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "[Sesler] players.json okunamadı");
      }
    }
  }

  private string SerializePrefs()
  {
    var snapshot = new SortedDictionary<string, Pref>();
    foreach (var (steamId, pref) in _prefs)
      if (!pref.IsDefault)
        snapshot[steamId.ToString()] = pref.Clone();

    return JsonSerializer.Serialize(snapshot, JsonOpts);
  }

  private void WriteJsonPrefs(string json, int version)
  {
    lock (_ioLock)
    {
      if (version != Volatile.Read(ref _jsonVersion))
        return;

      try
      {
        var tmp = JsonPath + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, JsonPath, true);
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "[Sesler] players.json yazılamadı");
      }
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

  private async Task<Pref?> LoadFromMySqlAsync(ulong steamId)
  {
    using var conn = new MySqlConnection(_dbConnectionString);
    await conn.OpenAsync();

    using var cmd = new MySqlCommand("SELECT knife, weapon, foot, player, mvp FROM `player_preferences` WHERE steamid = @s", conn);
    cmd.Parameters.AddWithValue("@s", steamId.ToString());
    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
      return null;

    return new Pref
    {
      Knife = (MuteMode)reader.GetByte(0),
      Weapon = (MuteMode)reader.GetByte(1),
      Foot = (MuteMode)reader.GetByte(2),
      Player = (MuteMode)reader.GetByte(3),
      Mvp = (MuteMode)reader.GetByte(4)
    };
  }

  private void SavePreference(ulong steamId, Pref pref)
  {
    if (_useMySql)
    {
      _ = SaveToMySqlAsync(steamId, pref.Clone());
      return;
    }

    var json = SerializePrefs();
    int version = Interlocked.Increment(ref _jsonVersion);
    Task.Run(() => WriteJsonPrefs(json, version));
  }

  private static bool IsValidSteamId(ulong steamId) =>
    steamId > 76561197960265728UL && (steamId & 0xFFFFFFFFUL) != 0xFFFFFFFFUL;

  private static ulong AuthorizedSteamId(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
      return 0;

    ulong steamId = player.AuthorizedSteamID?.SteamId64 ?? 0;
    return IsValidSteamId(steamId) ? steamId : 0;
  }

  private void BindConnectedPlayers()
  {
    List<CCSPlayerController> players;
    try
    {
      players = Utilities.GetPlayers();
    }
    catch (NativeException)
    {
      return;
    }

    foreach (var player in players)
      TryBind(player);
  }

  private void OnClientAuthorized(int playerSlot, SteamID steamId)
  {
    TryBind(Utilities.GetPlayerFromSlot(playerSlot));
  }

  private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
  {
    TryBind(@event.Userid);
    return HookResult.Continue;
  }

  private void OnClientDisconnect(int playerSlot)
  {
    if ((uint)playerSlot >= MaxSlots)
      return;

    ulong steamId = _slotSteamIds[playerSlot];
    _slotSteamIds[playerSlot] = 0;
    _slotPrefs[playerSlot] = null;

    if (_useMySql && steamId != 0)
      _prefs.TryRemove(steamId, out _);

    UpdateHooks();
  }

  private void TryBind(CCSPlayerController? player)
  {
    if (!_storageReady)
      return;

    ulong steamId = AuthorizedSteamId(player);
    if (steamId == 0)
      return;

    int slot = player!.Slot;
    if ((uint)slot >= MaxSlots || _slotSteamIds[slot] == steamId)
      return;

    _slotSteamIds[slot] = steamId;
    _slotPrefs[slot] = null;

    if (!_useMySql)
    {
      _slotPrefs[slot] = _prefs.TryGetValue(steamId, out var pref) ? pref : null;
      UpdateHooks();
      return;
    }

    UpdateHooks();

    Task.Run(async () =>
    {
      try
      {
        var loaded = await LoadFromMySqlAsync(steamId);
        if (loaded == null)
          return;

        Server.NextFrame(() =>
        {
          if (_unloaded || _slotSteamIds[slot] != steamId || _slotPrefs[slot] != null)
            return;

          _prefs[steamId] = loaded;
          _slotPrefs[slot] = loaded;
          UpdateHooks();
        });
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, $"[Sesler] Oyuncu tercihleri yüklenemedi - SteamID: {steamId}");
      }
    });
  }

  private Pref ViewPref(CCSPlayerController player) =>
    (uint)player.Slot < MaxSlots ? _slotPrefs[player.Slot] ?? Pref.Default : Pref.Default;

  private Pref? EditablePref(CCSPlayerController player)
  {
    int slot = player.Slot;
    if ((uint)slot >= MaxSlots)
      return null;

    ulong steamId = _slotSteamIds[slot];
    if (steamId == 0 || steamId != AuthorizedSteamId(player))
      return null;

    var pref = _slotPrefs[slot];
    if (pref == null)
    {
      pref = _prefs.GetOrAdd(steamId, _ => new Pref());
      _slotPrefs[slot] = pref;
    }

    return pref;
  }

  private void UpdateHooks()
  {
    bool sound = false, weaponSound = false, fireBullets = false;

    foreach (var pref in _slotPrefs)
    {
      if (pref == null)
        continue;

      sound |= pref.Knife != MuteMode.None || pref.Foot != MuteMode.None || pref.Player != MuteMode.None;
      weaponSound |= pref.Knife != MuteMode.None || pref.Weapon != MuteMode.None;
      fireBullets |= pref.Weapon != MuteMode.None;
    }

    SetHook(SoundMessage, _onSound, sound, ref _soundHooked);
    SetHook(WeaponSoundMessage, _onWeaponSound, weaponSound, ref _weaponSoundHooked);
    SetHook(FireBulletsMessage, _onFireBullets, fireBullets, ref _fireBulletsHooked);
  }

  private void SetHook(int messageId, UserMessage.UserMessageHandler handler, bool enable, ref bool hooked)
  {
    if (enable == hooked)
      return;

    if (enable)
      HookUserMessage(messageId, handler, HookMode.Pre);
    else
      UnhookUserMessage(messageId, handler, HookMode.Pre);

    hooked = enable;
  }

  [ConsoleCommand("css_ses", "Ses menüsünü açar")]
  [ConsoleCommand("css_sesler", "Ses menüsünü açar")]
  public void OnCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || player.IsBot) return;

    TryBind(player);
    if (EditablePref(player) == null) return;

    ShowMainMenu(player);
  }

  private void ShowMainMenu(CCSPlayerController player)
  {
    var pref = ViewPref(player);

    var categories = new (string Label, SoundType Type)[]
    {
      (Localizer["sesler.category_knife"], SoundType.Knife),
      (Localizer["sesler.category_weapon"], SoundType.Weapon),
      (Localizer["sesler.category_foot"], SoundType.Foot),
      (Localizer["sesler.category_player"], SoundType.Player),
      (Localizer["sesler.category_mvp"], SoundType.Mvp)
    };

    var items = new List<WasdItem>();
    foreach (var (label, type) in categories)
    {
      items.Add(new WasdItem
      {
        Text = $"{label}: {StateLabel(pref.Get(type), type == SoundType.Mvp)}",
        OnSelect = p => ShowSubMenu(p, label, type)
      });
    }

    _menus.Open(player, Localizer["sesler.menu_title"], items);
  }

  private void ShowSubMenu(CCSPlayerController player, string label, SoundType type)
  {
    bool mvpOnly = type == SoundType.Mvp;
    var current = ViewPref(player).Get(type);

    var items = new List<WasdItem>
    {
      WasdItem.Back(Localizer["sesler.back"], p => ShowMainMenu(p))
    };

    int maxOptions = mvpOnly ? 2 : 4;
    for (int i = 0; i < maxOptions; i++)
    {
      var modeIndex = mvpOnly ? i * 3 : i;
      var mode = (MuteMode)modeIndex;
      var prefix = current == mode ? "► " : "";

      items.Add(new WasdItem
      {
        Text = $"{prefix}<font color='{ModeColors[modeIndex]}'>{GetModeLabel(modeIndex)}</font>",
        OnSelect = p =>
        {
          var pref = EditablePref(p);
          if (pref == null)
            return;

          ulong steamId = _slotSteamIds[p.Slot];
          pref.Set(type, mode);
          if (pref.IsDefault && !_useMySql)
          {
            _prefs.TryRemove(steamId, out _);
            _slotPrefs[p.Slot] = null;
          }

          SavePreference(steamId, pref);
          UpdateHooks();
          ShowMainMenu(p);
        }
      });
    }

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
      if (!SoundTypeMap.TryGetValue(hash, out var type))
        return HookResult.Continue;

      return FilterRecipients(msg, msg.ReadInt("source_entity_index"), type, VictimSourcedHashes.Contains(hash));
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[Sesler] OnSound hook error");
      return HookResult.Continue;
    }
  }

  private HookResult OnWeaponSound(UserMessage msg)
  {
    try
    {
      var sound = msg.ReadString("sound");
      if (string.IsNullOrEmpty(sound))
        return HookResult.Continue;

      var type = sound.Contains("knife", StringComparison.OrdinalIgnoreCase) ? SoundType.Knife : SoundType.Weapon;
      return FilterRecipients(msg, msg.ReadInt("entidx"), type);
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[Sesler] OnWeaponSound hook error");
      return HookResult.Continue;
    }
  }

  private HookResult OnFireBullets(UserMessage msg)
  {
    try
    {
      return FilterRecipients(msg, (int)(msg.ReadUInt("player") & 0x3FFF), SoundType.Weapon);
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[Sesler] OnFireBullets hook error");
      return HookResult.Continue;
    }
  }

  private HookResult OnRoundMvp(EventRoundMvp @event, GameEventInfo info)
  {
    RecipientFilter? filter = null;

    for (int slot = 0; slot < MaxSlots; slot++)
    {
      if (_slotPrefs[slot]?.Mvp != MuteMode.All)
        continue;

      var player = Utilities.GetPlayerFromSlot(slot);
      if (player != null && player.IsValid && !player.IsBot)
        (filter ??= new RecipientFilter()).Add(player);
    }

    if (filter == null)
      return HookResult.Continue;

    var world = Utilities.GetEntityFromIndex<CBaseEntity>(0);
    if (world != null && world.IsValid)
      world.EmitSound("StopSoundEvents.StopAllMusic", filter);

    return HookResult.Continue;
  }

  private HookResult FilterRecipients(UserMessage msg, int sourceIndex, SoundType type, bool closedOnly = false)
  {
    var recipients = msg.Recipients;
    bool resolved = false;
    int sourceTeam = 0, sourceSlot = -1;

    for (int i = recipients.Count - 1; i >= 0; i--)
    {
      var listener = recipients[i];
      int slot = listener.Slot;
      if ((uint)slot >= MaxSlots)
        continue;

      var mode = _slotPrefs[slot]?.Get(type) ?? MuteMode.None;
      if (mode == MuteMode.None)
        continue;

      if (mode != MuteMode.All)
      {
        if (closedOnly)
          continue;

        if (!resolved)
        {
          ResolveSource(sourceIndex, out sourceTeam, out sourceSlot);
          resolved = true;
        }

        if (sourceTeam < 2 || slot == sourceSlot)
          continue;

        int listenerTeam = listener.TeamNum;
        if (listenerTeam < 2)
          continue;

        bool sameTeam = listenerTeam == sourceTeam;
        if (mode == MuteMode.Enemy ? sameTeam : !sameTeam)
          continue;
      }

      recipients.RemoveAt(i);
    }

    return recipients.Count == 0 ? HookResult.Stop : HookResult.Continue;
  }

  private static void ResolveSource(int index, out int team, out int slot)
  {
    team = 0;
    slot = -1;

    if (index <= 0)
      return;

    var entity = Utilities.GetEntityFromIndex<CBaseEntity>(index);
    if (entity == null || !entity.IsValid)
      return;

    if (entity.DesignerName != "player")
    {
      var owner = entity.OwnerEntity.Value;
      if (owner == null || !owner.IsValid || owner.DesignerName != "player")
      {
        team = entity.TeamNum;
        return;
      }

      entity = owner;
    }

    team = entity.TeamNum;
    var controller = entity.As<CCSPlayerPawn>().Controller;
    if (controller.IsValid)
      slot = (int)controller.Index - 1;
  }

  private static readonly uint[] KnifeHashes =
  {
    3475734633, 1769891506, 3634660983, 2486534908, 1211716720, 2017356964, 850427726, 2036741268, 3000481630, 901164697, 3539811726, 1641604058, 2294996470, 3008946998, 1746731373, 3932137706, 3756387178, 2164398983, 3263095419, 1657823620, 477362134, 3393546924, 3439870439, 1452069807, 736669604
  };

  private static readonly uint[] FootHashes =
  {
    2800858936, 70011614, 1194677450, 1016523349, 2240518199, 3218103073, 520432428, 1818046345, 2207486967, 2302139631, 1939055066, 1409986305, 1803111098, 4113422219, 3997353267, 3009312615, 123085364, 782454593, 3257325156, 3434104102, 2745524735, 117596568, 29217150, 3460445620, 2684452812, 2067683805, 1388885460, 413358161, 988265811, 3802757032, 2633527058, 1627020521, 602548457, 859178236, 3749333696, 2899365092, 2061955732, 1535891875, 3368720745, 3057812547, 135189076, 2790760284, 2448803175, 3753692454, 3666896632, 3166948458, 3099536373, 1690105992, 115843229, 1763490157, 2546391140, 515548944, 1517575510, 1248619277, 1395892944, 2300993891, 1183624286, 540697918, 2829617974, 1826799645, 3193435079, 2860219006, 1855038793, 2892812682, 3342414459, 144629619, 721782259, 2133235849, 3161194970, 819435812, 2804393637, 4222899547, 1664187801, 2714245023, 1692050905, 961838155, 2638406226, 3008782656, 2070478448, 1247386781, 58439651, 3172583021, 1557420499, 1485322532, 1598540856, 4163677892, 4082928848, 2708661994, 893108375, 1506215040, 2231399653, 1116700262, 2594927130, 1019414932, 1218015996, 417910549, 3299941720, 931543849, 2026488395, 84876002, 1403457606, 2189706910, 1543034, 892882552, 70939233, 1404198078, 1664329401, 822973253, 3797950766, 4203793682, 3952104171, 1163426340, 870100484, 935062317, 1161855519, 1253503839, 1635413700, 2333790984, 96240187, 1165397261, 4084367249, 3109879199, 3984387113, 4045299578, 2551626319, 2479376962, 4085076160, 1661204257, 2236021746, 1440734007, 585390608, 1194093029, 3755338324, 4152012084, 757978684, 1448154350, 2053595705, 1909915699, 765706800, 2722081556, 1540837791, 3123711576, 1770765328, 1761772772, 1424056132, 4160462271, 3806690332, 740474905, 1342713723, 3847761506, 809738584, 3295206520, 3184465677, 3023174225, 2162652424, 4074593561, 167638268
  };

  private static readonly uint[] PlayerHashes =
  {
    3688939408, 2703682875, 46413566, 2735369596, 1961884255, 318971924, 662078688, 3469219129, 4161440937, 3568181087, 663530947, 1499777741, 202030084, 3065316423, 1682747253, 427534867, 2369733616, 3666239815, 297379099, 2804654127, 4188085033, 3030200692, 1734994609, 4077119393, 2696334288, 129081149, 2158707679, 3601478655, 3616089666, 2064477315, 1489357772, 3745215916, 839762874, 850911881, 4146949428, 4204174059, 1412313471, 1792523944, 1815352525, 2967038404, 142772671, 1407794113, 3204513405, 2883205713, 769561685, 3103360935, 2381346641, 803727624, 1284373691, 1543118744, 2056150061, 3767841471, 3988751453, 1771184788, 708038349, 3049902652, 3638082858, 1193078452, 3535174312, 2831007164, 524041390, 2447320252, 3124768561, 856190898, 3663341586, 1904605142, 795825195, 4242317911, 4002300972, 3259510958, 2106508305, 963985059, 62938228, 3926353328, 282152614, 2284698275, 2019962436, 3663896169, 3573863551, 1823342283, 2192712263, 3396420465, 2323025056, 3524038396, 2719685137, 2310318859, 2020934318, 3740948313, 2902143738, 400609565, 2316086169, 604181152, 3642452443, 2746487563, 3067118482
  };

  private static readonly HashSet<uint> VictimSourcedHashes = new()
  {
    3000481630, 2294996470, 3008946998, 3756387178, 2164398983, 3263095419, 3393546924, 3439870439, 1452069807
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
}

public class Pref
{
  public static readonly Pref Default = new();

  public MuteMode Knife { get; set; }
  public MuteMode Foot { get; set; }
  public MuteMode Player { get; set; }
  public MuteMode Weapon { get; set; }
  public MuteMode Mvp { get; set; }

  [System.Text.Json.Serialization.JsonIgnore]
  public bool IsDefault =>
    Knife == MuteMode.None && Foot == MuteMode.None && Player == MuteMode.None && Weapon == MuteMode.None && Mvp == MuteMode.None;

  public MuteMode Get(SoundType type) => type switch
  {
    SoundType.Knife => Knife,
    SoundType.Weapon => Weapon,
    SoundType.Foot => Foot,
    SoundType.Player => Player,
    SoundType.Mvp => Mvp,
    _ => MuteMode.None
  };

  public void Set(SoundType type, MuteMode mode)
  {
    switch (type)
    {
      case SoundType.Knife: Knife = mode; break;
      case SoundType.Weapon: Weapon = mode; break;
      case SoundType.Foot: Foot = mode; break;
      case SoundType.Player: Player = mode; break;
      case SoundType.Mvp: Mvp = mode; break;
    }
  }

  public Pref Clone() => (Pref)MemberwiseClone();
}
