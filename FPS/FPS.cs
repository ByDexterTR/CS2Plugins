using System.Drawing;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using static CounterStrikeSharp.API.Core.Listeners;
using ByDexter.Shared;
using CSVector = CounterStrikeSharp.API.Modules.Utils.Vector;
using Stopwatch = System.Diagnostics.Stopwatch;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace FPS;

public class FPSConfig
{
  [JsonPropertyName("fps_cmd")]
  public string Commands { get; set; } = "css_fps";

  [JsonPropertyName("fps_flag")]
  public string Flag { get; set; } = "";

  [JsonPropertyName("player_config"), JsonConverter(typeof(LooseBool))]
  public bool PlayerConfig { get; set; } = true;

  [JsonPropertyName("hide_unseen"), JsonConverter(typeof(LooseInt))]
  public int HideUnseen { get; set; } = 3;

  [JsonPropertyName("mute_unseen"), JsonConverter(typeof(LooseInt))]
  public int MuteUnseen { get; set; } = 3;

  [JsonPropertyName("own_killfeed"), JsonConverter(typeof(LooseBool))]
  public bool OwnKillfeed { get; set; } = true;

  [JsonPropertyName("hide_corpses"), JsonConverter(typeof(LooseInt))]
  public int HideCorpses { get; set; } = 1;

  [JsonPropertyName("corpse_delay")]
  public float CorpseDelay { get; set; } = 0.5f;

  [JsonPropertyName("hide_legs"), JsonConverter(typeof(LooseBool))]
  public bool HideLegs { get; set; } = true;

  [JsonPropertyName("hide_blood"), JsonConverter(typeof(LooseInt))]
  public int HideBlood { get; set; } = 1;

  [JsonPropertyName("blood_delay")]
  public float BloodDelay { get; set; } = 0.5f;

  [JsonPropertyName("hide_props"), JsonConverter(typeof(LooseInt))]
  public int HideProps { get; set; } = 1;
}

public class LooseInt : JsonConverter<int>
{
  public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
  {
    JsonTokenType.True => 1,
    JsonTokenType.False => 0,
    JsonTokenType.Number => (int)reader.GetDouble(),
    JsonTokenType.String when int.TryParse(reader.GetString(), out int value) => value,
    _ => throw new JsonException()
  };

  public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
}

public class LooseBool : JsonConverter<bool>
{
  public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
  {
    JsonTokenType.True => true,
    JsonTokenType.False => false,
    JsonTokenType.Number => reader.GetDouble() != 0,
    JsonTokenType.String when bool.TryParse(reader.GetString(), out bool value) => value,
    JsonTokenType.String when int.TryParse(reader.GetString(), out int value) => value != 0,
    _ => throw new JsonException()
  };

  public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteBooleanValue(value);
}

public class PropRecord
{
  [JsonPropertyName("model")]
  public string Model { get; set; } = "";

  [JsonPropertyName("count")]
  public int Count { get; set; }
}

public class FPS : BasePlugin
{
  public override string ModuleName => "FPS";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public FPSConfig Config { get; set; } = new();

  private const int Off = 0;
  private const int Enable = 1;
  private const int Force = 2;

  private const int Killfeed = 1;
  private const int Corpses = 2;
  private const int Legs = 4;
  private const int Blood = 8;
  private const int Props = 16;

  private const string UnseenKey = "hide_unseen";
  private const string MuteKey = "mute_unseen";
  private const string FpsKey = "fps";
  private static readonly (int Flag, string Key)[] FlagKeys =
  {
    (Killfeed, "own_killfeed"),
    (Corpses, "hide_corpses"),
    (Legs, "hide_legs"),
    (Blood, "hide_blood"),
    (Props, "hide_props")
  };

  private const int MaxSlots = 64;
  private const int MaxWeapons = 16;
  private const int MaxEntities = 16384;
  private const int HoldTicks = 64;
  private const int SafeHoldTicks = 16;
  private const int RecheckTicks = 2;
  private const int MaxHiddenTicks = 32;
  private const int GraceTicks = 16;
  private const float ShoulderBase = 48f;
  private const float ShoulderPerMs = 0.64f;
  private const float ShoulderMax = 144f;
  private const float BodyPad = 20f;
  private const float MovingSqr = 10f * 10f;
  private const float MoveSqr = 24f * 24f;
  private const float NearDistanceSqr = 160f * 160f;
  private const ulong LosMask = (ulong)Contents.Solid;
  private const ulong LosExclude = (ulong)(Contents.Player | Contents.Npc | Contents.Debris | Contents.Window | Contents.PassBullets);
  private const int OriginTicks = 4;
  private const float ClipMargin = 8f;
  private const int DyingTicks = 8;
  private const int FfaTicks = 64;
  private const int EffectDispatchMessage = 400;
  private const int WeaponSoundMessage = 369;
  private const int FireBulletsMessage = 452;
  private const int ClearWorldDecalsMessage = 202;
  private const int ClearEntityDecalsMessage = 203;
  private const uint DynamicDecals = 0xFFFFFFFE;
  private const string PropClass = "prop_physics_multiplayer";
  private static readonly long TraceBudget = Stopwatch.Frequency * 150 / 1_000_000;

  private readonly bool[] _human = new bool[MaxSlots];
  private readonly ulong[] _steamIds = new ulong[MaxSlots];
  private readonly int[] _unseen = new int[MaxSlots];
  private readonly int[] _mute = new int[MaxSlots];
  private readonly ulong[] _hiddenMask = new ulong[MaxSlots];
  private ulong _muteMask;
  private readonly int[] _flags = new int[MaxSlots];
  private readonly float[] _deathTime = new float[MaxSlots];
  private readonly int[] _dyingTick = new int[MaxSlots];
  private ulong _bloodMask;
  private int _killfeedCount;
  private readonly Dictionary<ulong, Dictionary<string, int>> _saved = new();
  private readonly object _ioLock = new();
  private static readonly JsonSerializerOptions JsonOpts = new()
  {
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString
  };

  private string SettingsPath => Path.Combine(ModuleDirectory, "settings.json");
  private string PlayersPath => Path.Combine(ModuleDirectory, "players.json");
  private string MapsDirectory => Path.Combine(ModuleDirectory, "maps");

  private struct Snap
  {
    public int Pawn;
    public bool Alive;
    public bool Corpse;
    public byte Team;
    public int Ping;
    public float EyeZ;
    public Vector3 Origin;
    public Vector3 Velocity;
    public int PosTick;
    public int WeaponTick;
    public int WeaponCount;
  }

  private readonly Snap[] _snap = new Snap[MaxSlots];
  private readonly CCSPlayerController?[] _players = new CCSPlayerController?[MaxSlots];
  private readonly CCSPlayerPawn?[] _pawns = new CCSPlayerPawn?[MaxSlots];
  private readonly int[] _weapons = new int[MaxSlots * MaxWeapons];
  private readonly int[] _visibleUntil = new int[MaxSlots * MaxSlots];
  private readonly int[] _nextCheck = new int[MaxSlots * MaxSlots];
  private readonly int[] _checkedAt = new int[MaxSlots * MaxSlots];
  private readonly bool[] _seen = new bool[MaxSlots * MaxSlots];
  private readonly Vector3[] _fromPos = new Vector3[MaxSlots * MaxSlots];
  private readonly Vector3[] _toPos = new Vector3[MaxSlots * MaxSlots];
  private long _traceTime;
  private int _snapTick = -1;
  private int _slotOffset;
  private ConVar? _teammatesAreEnemies;
  private bool _ffa;

  private readonly CSVector _traceStart = new();
  private readonly CSVector _traceEnd = new();
  private readonly TraceOptions _traceOptions = new() { InteractsWith = (Contents)LosMask, InteractsExclude = (Contents)LosExclude };
  private readonly Vector3[] _current = new Vector3[5];
  private readonly Vector3[] _ahead = new Vector3[2];
  private readonly Vector3[] _origins = new Vector3[MaxSlots * 6];
  private readonly int[] _originTick = new int[MaxSlots];
  private readonly bool[] _moving = new bool[MaxSlots];

  private readonly bool[] _isProp = new bool[MaxEntities];
  private readonly List<int> _props = new();
  private readonly uint[] _propMask = new uint[MaxEntities / 32];
  private readonly List<int> _propWords = new();
  private Dictionary<string, int>? _mapProps;
  private string _propsMap = "";
  private bool _propsVerified;

  private WasdMenuManager _menus = null!;
  private CheckTransmit _onCheckTransmit = null!;
  private UserMessage.UserMessageHandler _onEffect = null!;
  private UserMessage.UserMessageHandler _onFireBullets = null!;
  private UserMessage.UserMessageHandler _onWeaponSound = null!;
  private bool _soundHooked;
  private bool _transmitHooked;
  private bool _bloodHooked;
  private Timer? _decalTimer;

  public override void Load(bool hotReload)
  {
    LoadSettings();
    LoadPlayers();

    _slotOffset = GameData.GetOffset("CheckTransmitPlayerSlot");
    _teammatesAreEnemies = ConVar.Find("mp_teammates_are_enemies");
    _ffa = _teammatesAreEnemies?.GetPrimitiveValue<bool>() == true;
    _onCheckTransmit = OnCheckTransmit;
    _onEffect = OnEffect;
    _onFireBullets = msg => MuteHidden(msg, (int)(msg.ReadUInt("player") & 0x3FFF));
    _onWeaponSound = msg => MuteHidden(msg, msg.ReadInt("entidx"));

    _menus = new WasdMenuManager(this,
      () => Localizer["menu.scroll"],
      () => Localizer["menu.select"],
      () => Localizer["menu.exit"]);

    foreach (var name in Util.Split(Config.Commands))
      AddCommand(name, "FPS ayarlari menusunu acar", OnFpsCommand);

    RegisterListener<OnClientPutInServer>(OnClientPutInServer);
    RegisterListener<OnClientAuthorized>(OnClientAuthorized);
    RegisterListener<OnClientDisconnect>(OnClientDisconnect);

    if (Config.OwnKillfeed)
      RegisterEventHandler<EventPlayerDeath>(OnPlayerDeathPre, HookMode.Pre);

    if (Config.HideUnseen != 0)
      RegisterEventHandler<EventPlayerDeath>(OnPlayerDying, HookMode.Pre);

    if (Config.HideCorpses != Off)
      RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);

    if (Config.HideLegs)
      RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);

    if (Config.HideProps != Off)
    {
      RegisterEventHandler<EventRoundStart>(OnRoundStart);
      RegisterListener<OnEntityDeleted>(OnEntityDeleted);
      RegisterListener<OnMapStart>(_ => ClearProps());
    }

    Server.NextWorldUpdate(() =>
    {
      foreach (var player in Utilities.GetPlayers())
      {
        _players[player.Slot] = player;
        InitPlayer(player);
      }

      if (Config.HideProps != Off)
        FindProps();

      UpdateHooks();
    });
  }

  public override void Unload(bool hotReload)
  {
    SetTransmitHook(false);
    SetBloodHook(false);
    SetSoundHook(false);
    _menus.Clear();

    if (!Config.HideLegs)
      return;

    foreach (var player in Utilities.GetPlayers())
    {
      if (player.IsValid && (_flags[player.Slot] & Legs) != 0)
        SetLegs(player, false);
    }
  }

  private void LoadSettings()
  {
    lock (_ioLock)
    {
      try
      {
        if (File.Exists(SettingsPath))
          Config = JsonSerializer.Deserialize<FPSConfig>(File.ReadAllText(SettingsPath), JsonOpts) ?? new FPSConfig();
      }
      catch (Exception ex)
      {
        Config = new FPSConfig();
        Logger.LogError(ex, "[FPS] settings.json okunamadi, settings.old.json olarak saklanip varsayilan ayarlar yaziliyor");

        try
        {
          File.Copy(SettingsPath, Path.Combine(ModuleDirectory, "settings.old.json"), true);
        }
        catch (Exception copyEx)
        {
          Logger.LogError(copyEx, "[FPS] settings.old.json yazilamadi");
        }
      }

      try
      {
        Config.HideUnseen = Math.Clamp(Config.HideUnseen, 0, 3);
        Config.MuteUnseen = Config.HideUnseen == 0 ? 0 : Math.Clamp(Config.MuteUnseen, 0, 3);
        Config.HideCorpses = Math.Clamp(Config.HideCorpses, Off, Force);
        Config.HideBlood = Math.Clamp(Config.HideBlood, Off, Force);
        Config.HideProps = Math.Clamp(Config.HideProps, Off, Force);
        Config.CorpseDelay = Math.Max(0f, Config.CorpseDelay);
        Config.BloodDelay = Math.Max(0.1f, Config.BloodDelay);

        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Config, JsonOpts));
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "[FPS] settings.json yazilamadi: {Path}", SettingsPath);
      }
    }
  }

  private void LoadPlayers()
  {
    lock (_ioLock)
    {
      try
      {
        if (!File.Exists(PlayersPath))
          return;

        string json = File.ReadAllText(PlayersPath);
        if (!json.TrimStart().StartsWith('{'))
          return;

        var raw = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(json);
        if (raw == null)
          return;

        foreach (var (key, values) in raw)
        {
          if (ulong.TryParse(key, out var steamId) && values != null && values.Count > 0)
            _saved[steamId] = values;
        }
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "[FPS] players.json okunamadi");
      }
    }
  }

  private void SavePlayers()
  {
    var snapshot = _saved.ToDictionary(pair => pair.Key.ToString(), pair => new Dictionary<string, int>(pair.Value));

    Task.Run(() =>
    {
      try
      {
        lock (_ioLock)
          File.WriteAllText(PlayersPath, JsonSerializer.Serialize(snapshot, JsonOpts));
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "[FPS] players.json yazilamadi");
      }
    });
  }

  private int Available()
  {
    int flags = 0;
    if (Config.OwnKillfeed)
      flags |= Killfeed;
    if (Config.HideCorpses == Enable)
      flags |= Corpses;
    if (Config.HideLegs)
      flags |= Legs;
    if (Config.HideBlood == Enable)
      flags |= Blood;
    if (Config.HideProps == Enable)
      flags |= Props;
    return flags;
  }

  private static ulong VerifiedSteamId(CCSPlayerController player)
  {
    ulong id = player.AuthorizedSteamID?.SteamId64 ?? 0UL;
    return id > 76561197960265728UL && (id & 0xFFFFFFFFUL) != 0xFFFFFFFFUL ? id : 0UL;
  }

  private void InitPlayer(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
      return;

    ApplyPrefs(player.Slot, VerifiedSteamId(player));

    if (Config.HideLegs)
      SetLegs(player, (_flags[player.Slot] & Legs) != 0);
  }

  private void ApplyPrefs(int slot, ulong steamId)
  {
    _human[slot] = true;
    _steamIds[slot] = steamId;
    _unseen[slot] = Config.HideUnseen;
    _mute[slot] = Config.MuteUnseen;
    _flags[slot] = Available();

    if (steamId != 0 && _saved.TryGetValue(steamId, out var values))
    {
      if (!Config.PlayerConfig)
      {
        if (values.TryGetValue(FpsKey, out int fps) && fps == 0)
          SetFpsOff(slot);
      }
      else
      {
        if (Config.HideUnseen != 0 && values.TryGetValue(UnseenKey, out int unseen))
          _unseen[slot] = Math.Clamp(unseen, 0, 3);

        if (Config.MuteUnseen != 0 && values.TryGetValue(MuteKey, out int mute))
          _mute[slot] = Math.Clamp(mute, 0, 3);

        foreach (var (flag, key) in FlagKeys)
        {
          if ((_flags[slot] & flag) != 0 && values.TryGetValue(key, out int on) && on == 0)
            _flags[slot] &= ~flag;
        }
      }
    }

    ResetPairs(slot);
    UpdateHooks();
  }

  private void SetFpsOff(int slot)
  {
    _unseen[slot] = 0;
    _mute[slot] = 0;
    _flags[slot] = 0;
  }

  private void OnClientPutInServer(int slot)
  {
    if (slot < 0 || slot >= MaxSlots)
      return;

    _players[slot] = Utilities.GetPlayerFromSlot(slot);
    InitPlayer(_players[slot]);
  }

  private void OnClientAuthorized(int slot, SteamID steamId)
  {
    var player = Utilities.GetPlayerFromSlot(slot);
    if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
      return;

    ApplyPrefs(slot, steamId.SteamId64);

    if (Config.HideLegs)
      SetLegs(player, (_flags[slot] & Legs) != 0);
  }

  private void OnClientDisconnect(int slot)
  {
    if (slot < 0 || slot >= MaxSlots)
      return;

    _human[slot] = false;
    _steamIds[slot] = 0;
    _unseen[slot] = 0;
    _mute[slot] = 0;
    _hiddenMask[slot] = 0;
    _flags[slot] = 0;
    _players[slot] = null;
    _pawns[slot] = null;
    ResetPairs(slot);
    UpdateHooks();
  }

  private void ResetPairs(int slot)
  {
    for (int i = 0; i < MaxSlots; i++)
    {
      int row = slot * MaxSlots + i;
      int column = i * MaxSlots + slot;
      _visibleUntil[row] = _visibleUntil[column] = 0;
      _nextCheck[row] = _nextCheck[column] = 0;
      _seen[row] = _seen[column] = false;
    }
  }

  private void UpdateHooks()
  {
    bool anyHuman = false;
    bool transmit = Config.HideCorpses == Force;
    _bloodMask = 0;
    _muteMask = 0;
    _killfeedCount = 0;

    for (int slot = 0; slot < MaxSlots; slot++)
    {
      if (!_human[slot])
        continue;

      anyHuman = true;
      int flags = _flags[slot];

      if (_unseen[slot] != 0 || (flags & (Corpses | Props)) != 0)
        transmit = true;

      if ((flags & Blood) != 0)
        _bloodMask |= 1UL << slot;

      if ((flags & Killfeed) != 0)
        _killfeedCount++;

      if (_unseen[slot] != 0 && _mute[slot] != 0)
        _muteMask |= 1UL << slot;
    }

    SetTransmitHook(transmit);
    SetBloodHook(_bloodMask != 0 || (anyHuman && Config.HideBlood == Force));
    SetSoundHook(_muteMask != 0);
  }

  private void SetSoundHook(bool on)
  {
    if (on == _soundHooked)
      return;

    _soundHooked = on;

    if (on)
    {
      HookUserMessage(FireBulletsMessage, _onFireBullets, HookMode.Pre);
      HookUserMessage(WeaponSoundMessage, _onWeaponSound, HookMode.Pre);
    }
    else
    {
      UnhookUserMessage(FireBulletsMessage, _onFireBullets, HookMode.Pre);
      UnhookUserMessage(WeaponSoundMessage, _onWeaponSound, HookMode.Pre);
    }
  }

  private void SetTransmitHook(bool on)
  {
    if (on == _transmitHooked)
      return;

    _transmitHooked = on;
    _snapTick = -1;

    if (on)
      RegisterListener(_onCheckTransmit);
    else
      RemoveListener(_onCheckTransmit);
  }

  private void SetBloodHook(bool on)
  {
    if (on == _bloodHooked)
      return;

    _bloodHooked = on;

    if (on)
    {
      HookUserMessage(EffectDispatchMessage, _onEffect, HookMode.Pre);
      _decalTimer = AddTimer(Config.BloodDelay, ClearDecals, TimerFlags.REPEAT);
    }
    else
    {
      UnhookUserMessage(EffectDispatchMessage, _onEffect, HookMode.Pre);
      _decalTimer?.Kill();
      _decalTimer = null;
    }
  }

  private void OnFpsCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || player.IsBot)
      return;

    if (!Util.HasAccess(player, Config.Flag))
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["fps.no_permission"]}");
      return;
    }

    if (Config.HideUnseen == 0 && Available() == 0)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["fps.no_options"]}");
      return;
    }

    if (_steamIds[player.Slot] == 0)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["fps.not_ready"]}");
      return;
    }

    if (Config.PlayerConfig)
    {
      ShowMenu(player);
      return;
    }

    int slot = player.Slot;
    ulong steamId = _steamIds[slot];
    bool on = _flags[slot] == 0 && _unseen[slot] == 0;

    if (on)
      ApplyPrefs(slot, 0);
    else
    {
      SetFpsOff(slot);
      ResetPairs(slot);
      UpdateHooks();
    }

    _steamIds[slot] = steamId;
    Remember(slot);

    if (Config.HideLegs)
      SetLegs(player, (_flags[slot] & Legs) != 0);

    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer[on ? "fps.enabled" : "fps.disabled"]}");
  }

  private void ShowMenu(CCSPlayerController player)
  {
    int slot = player.Slot;
    var items = new List<WasdItem>();

    if (Config.HideUnseen != 0)
    {
      items.Add(new WasdItem
      {
        Text = $"{Localizer["fps.hide_unseen"]}: {UnseenLabel(_unseen[slot])}",
        OnSelect = p => ChangeMode(p, _unseen)
      });
    }

    if (Config.MuteUnseen != 0)
    {
      items.Add(new WasdItem
      {
        Text = $"{Localizer["fps.mute_unseen"]}: {UnseenLabel(_mute[slot])}",
        OnSelect = p => ChangeMode(p, _mute)
      });
    }

    int available = Available();
    foreach (var (flag, key) in FlagKeys)
    {
      if ((available & flag) == 0)
        continue;

      items.Add(new WasdItem
      {
        Text = $"{Localizer["fps." + key]}: {StateLabel(flag, (_flags[slot] & flag) != 0)}",
        OnSelect = p => ToggleFlag(p, flag)
      });
    }

    _menus.Open(player, Localizer["fps.menu_title"], items);
  }

  private string StateLabel(int flag, bool on)
  {
    string key = flag == Killfeed ? (on ? "fps.own" : "fps.all") : (on ? "fps.hidden" : "fps.shown");
    return $"<font color='{(on ? "green" : "red")}'>{Localizer[key]}</font>";
  }

  private string UnseenLabel(int mode) => mode switch
  {
    1 => $"<font color='green'>{Localizer["fps.unseen_team"]}</font>",
    2 => $"<font color='green'>{Localizer["fps.unseen_enemy"]}</font>",
    3 => $"<font color='green'>{Localizer["fps.unseen_all"]}</font>",
    _ => $"<font color='red'>{Localizer["fps.off"]}</font>"
  };

  private void ChangeMode(CCSPlayerController player, int[] modes)
  {
    int slot = player.Slot;
    modes[slot] = (modes[slot] + 1) % 4;
    ResetPairs(slot);
    Remember(slot);
    UpdateHooks();
    ShowMenu(player);
  }

  private void ToggleFlag(CCSPlayerController player, int flag)
  {
    int slot = player.Slot;
    _flags[slot] ^= flag;
    Remember(slot);
    UpdateHooks();

    if (flag == Legs)
      SetLegs(player, (_flags[slot] & Legs) != 0);

    ShowMenu(player);
  }

  private void Remember(int slot)
  {
    ulong steamId = _steamIds[slot];
    if (steamId == 0)
      return;

    var values = new Dictionary<string, int>();

    if (!Config.PlayerConfig)
    {
      if (_flags[slot] == 0 && _unseen[slot] == 0)
        values[FpsKey] = 0;
    }
    else
    {
      if (Config.HideUnseen != 0 && _unseen[slot] != Config.HideUnseen)
        values[UnseenKey] = _unseen[slot];

      if (Config.MuteUnseen != 0 && _mute[slot] != Config.MuteUnseen)
        values[MuteKey] = _mute[slot];

      int available = Available();
      foreach (var (flag, key) in FlagKeys)
      {
        if ((available & flag) != 0 && (_flags[slot] & flag) == 0)
          values[key] = 0;
      }
    }

    if (values.Count == 0)
      _saved.Remove(steamId);
    else
      _saved[steamId] = values;

    SavePlayers();
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    Server.NextFrame(FindProps);
    return HookResult.Continue;
  }

  private void ClearProps()
  {
    foreach (int index in _props)
      _isProp[index] = false;

    foreach (int word in _propWords)
      _propMask[word] = 0;

    _props.Clear();
    _propWords.Clear();
  }

  private void FindProps()
  {
    ClearProps();

    string map = Server.MapName ?? "";
    if (map != _propsMap)
    {
      _propsMap = map;
      _mapProps = LoadMapProps(map);
      _propsVerified = false;
    }

    var found = new Dictionary<string, int>();
    var remove = new List<CBaseModelEntity>();

    foreach (var prop in Utilities.FindAllEntitiesByDesignerName<CBaseModelEntity>(PropClass))
    {
      var collision = prop.Collision;
      if (collision == null || (collision.CollisionAttribute.InteractsAs & (ulong)Contents.Debris) == 0)
        continue;

      string model = prop.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState.ModelName ?? "";
      if (model.Length == 0)
        continue;

      found[model] = found.GetValueOrDefault(model) + 1;
      if (_mapProps != null && !_mapProps.ContainsKey(model))
        continue;

      if (Config.HideProps == Force)
      {
        remove.Add(prop);
        continue;
      }

      int index = (int)prop.Index;
      if (index <= 0 || index >= MaxEntities || _isProp[index])
        continue;

      _isProp[index] = true;
      _props.Add(index);

      int word = index >> 5;
      if (_propMask[word] == 0)
        _propWords.Add(word);
      _propMask[word] |= 1u << (index & 31);
    }

    foreach (var prop in remove)
    {
      if (prop.IsValid)
        prop.Remove();
    }

    if (_mapProps == null)
    {
      if (found.Count == 0)
        return;

      _mapProps = found;
      _propsVerified = true;
      SaveMapProps(map, found);
    }
    else if (!_propsVerified)
    {
      _propsVerified = true;
      VerifyMapProps(map, found);
    }
  }

  private void OnEntityDeleted(CEntityInstance entity)
  {
    int index = (int)entity.Index;
    if (index <= 0 || index >= MaxEntities || !_isProp[index])
      return;

    _isProp[index] = false;
    _props.Remove(index);
    _propMask[index >> 5] &= ~(1u << (index & 31));
  }

  private string? MapFilePath(string map)
  {
    var builder = new StringBuilder(map.Length);
    foreach (var character in map)
    {
      if (char.IsLetterOrDigit(character) || character == '_' || character == '-')
        builder.Append(char.ToLowerInvariant(character));
    }

    return builder.Length == 0 ? null : Path.Combine(MapsDirectory, builder + ".json");
  }

  private Dictionary<string, int>? LoadMapProps(string map)
  {
    var path = MapFilePath(map);
    if (path == null || !File.Exists(path))
      return null;

    try
    {
      var records = JsonSerializer.Deserialize<List<PropRecord?>>(File.ReadAllText(path));
      var props = new Dictionary<string, int>();

      foreach (var record in records ?? new())
      {
        if (record != null && !string.IsNullOrWhiteSpace(record.Model))
          props[record.Model.Trim()] = record.Count;
      }

      return props;
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[FPS] {Path} okunamadi", path);
      return new Dictionary<string, int>();
    }
  }

  private void SaveMapProps(string map, Dictionary<string, int> found)
  {
    var path = MapFilePath(map);
    if (path == null || found.Count == 0)
      return;

    var records = found
      .OrderByDescending(pair => pair.Value)
      .ThenBy(pair => pair.Key, StringComparer.Ordinal)
      .Select(pair => new PropRecord { Model = pair.Key, Count = pair.Value })
      .ToList();

    Task.Run(() =>
    {
      try
      {
        lock (_ioLock)
        {
          Directory.CreateDirectory(MapsDirectory);
          File.WriteAllText(path, JsonSerializer.Serialize(records, JsonOpts));
        }
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "[FPS] {Path} yazilamadi", path);
      }
    });

    Logger.LogInformation("[FPS] {Map} icin {Count} cop prop modeli maps klasorune kaydedildi", map, records.Count);
  }

  private void VerifyMapProps(string map, Dictionary<string, int> found)
  {
    if (_mapProps == null)
      return;

    var missing = new List<string>();
    var changed = new List<string>();

    foreach (var (model, count) in _mapProps)
    {
      int actual = found.GetValueOrDefault(model);
      if (actual == 0)
        missing.Add(model);
      else if (count > 0 && actual != count)
        changed.Add($"{model} ({count} -> {actual})");
    }

    int unlisted = found.Keys.Count(model => !_mapProps.ContainsKey(model));

    if (missing.Count > 0)
      Logger.LogWarning("[FPS] {Map}: listedeki {Count} model haritada yok ya da artik gizlenemez: {Models}", map, missing.Count, string.Join(", ", missing));

    if (changed.Count > 0)
      Logger.LogWarning("[FPS] {Map}: {Count} modelin adedi degismis: {Models}", map, changed.Count, string.Join(", ", changed));

    if (unlisted > 0)
      Logger.LogWarning("[FPS] {Map}: listede olmayan {Count} cop prop modeli var, gizlenmiyor", map, unlisted);
  }

  private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || player.IsBot || (_flags[player.Slot] & Legs) == 0)
      return HookResult.Continue;

    Server.NextFrame(() =>
    {
      if (player.IsValid && (_flags[player.Slot] & Legs) != 0)
        SetLegs(player, true);
    });

    return HookResult.Continue;
  }

  private static void SetLegs(CCSPlayerController player, bool hide)
  {
    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
      return;

    var color = pawn.Render;
    int from = hide ? 255 : 254;
    if (color.A != from)
      return;

    pawn.Render = Color.FromArgb(hide ? 254 : 255, color.R, color.G, color.B);
    Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
  }

  private HookResult OnPlayerDeathPre(EventPlayerDeath @event, GameEventInfo info)
  {
    if (_killfeedCount == 0)
      return HookResult.Continue;

    int attacker = @event.Attacker?.IsValid == true ? @event.Attacker.Slot : -1;
    int victim = @event.Userid?.IsValid == true ? @event.Userid.Slot : -1;

    info.DontBroadcast = true;

    foreach (var player in Utilities.GetPlayers())
    {
      if (player.IsBot && !player.IsHLTV)
        continue;

      int slot = player.Slot;
      if ((_flags[slot] & Killfeed) != 0 && slot != attacker && slot != victim)
        continue;

      @event.FireEventToClient(player);
    }

    return HookResult.Continue;
  }

  private HookResult OnPlayerDying(EventPlayerDeath @event, GameEventInfo info)
  {
    var victim = @event.Userid;
    if (victim != null && victim.IsValid && victim.Slot < MaxSlots)
    {
      _dyingTick[victim.Slot] = Server.TickCount;
      _snap[victim.Slot].Alive = false;
    }

    return HookResult.Continue;
  }

  private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
  {
    var victim = @event.Userid;
    if (victim != null && victim.IsValid && victim.Slot < MaxSlots)
      _deathTime[victim.Slot] = Server.CurrentTime;

    return HookResult.Continue;
  }

  private ulong BloodTargets()
  {
    if (Config.HideBlood != Force)
      return _bloodMask;

    ulong mask = 0;
    for (int slot = 0; slot < MaxSlots; slot++)
    {
      if (_human[slot])
        mask |= 1UL << slot;
    }

    return mask;
  }

  private HookResult OnEffect(UserMessage msg)
  {
    ulong mask = msg.Recipients.GetRecipientMask();
    ulong targets = Config.HideBlood == Force ? mask : mask & _bloodMask;
    if (targets == 0 || !OnPlayer(msg.DebugString))
      return HookResult.Continue;

    ulong rest = mask & ~targets;
    if (rest == 0)
      return HookResult.Stop;

    msg.Recipients = new RecipientFilter(rest);
    return HookResult.Continue;
  }

  private HookResult MuteHidden(UserMessage msg, int source)
  {
    int t = SlotOf(source);
    if (t < 0)
      return HookResult.Continue;

    ulong mask = msg.Recipients.GetRecipientMask();
    ulong listeners = mask & _muteMask;
    ulong remove = 0;

    while (listeners != 0)
    {
      int v = BitOperations.TrailingZeroCount(listeners);
      listeners &= listeners - 1;

      if (((_hiddenMask[v] >> t) & 1) == 0)
        continue;

      int mode = _mute[v];
      if (mode == 3 || Teammates(t, v) == (mode == 1))
        remove |= 1UL << v;
    }

    if (remove == 0)
      return HookResult.Continue;

    ulong rest = mask & ~remove;
    if (rest == 0)
      return HookResult.Stop;

    msg.Recipients = new RecipientFilter(rest);
    return HookResult.Continue;
  }

  private int SlotOf(int index)
  {
    if (index <= 0)
      return -1;

    for (int slot = 0; slot < MaxSlots; slot++)
    {
      ref var s = ref _snap[slot];
      if (s.Pawn == index)
        return slot;

      if (s.WeaponTick != _snapTick)
        continue;

      for (int w = 0; w < s.WeaponCount; w++)
      {
        if (_weapons[slot * MaxWeapons + w] == index)
          return slot;
      }
    }

    return -1;
  }

  private static bool OnPlayer(string debug)
  {
    int i = debug.IndexOf(" entity: ", StringComparison.Ordinal);
    if (i < 0)
      return false;

    uint raw = 0;
    for (i += 9; i < debug.Length && char.IsAsciiDigit(debug[i]); i++)
      raw = raw * 10 + (uint)(debug[i] - '0');

    var entity = Utilities.GetEntityFromIndex<CBaseEntity>((int)(raw & 0x3FFF));
    return entity != null && entity.IsValid && entity.DesignerName == "player";
  }

  private void ClearDecals()
  {
    ulong targets = BloodTargets();
    if (targets == 0)
      return;

    SendClear(ClearWorldDecalsMessage, targets);
    SendClear(ClearEntityDecalsMessage, targets);
  }

  private static void SendClear(int id, ulong targets)
  {
    using var msg = UserMessage.FromId(id);
    msg.SetUInt("flagstoclear", DynamicDecals);
    msg.Send(new RecipientFilter(targets));
  }

  private void BuildSnapshot(int tick)
  {
    _snapTick = tick;
    _traceTime = 0;

    if (tick % FfaTicks == 0)
      _ffa = _teammatesAreEnemies?.GetPrimitiveValue<bool>() == true;
    float now = Server.CurrentTime;
    float delay = Config.CorpseDelay;

    for (int slot = 0; slot < MaxSlots; slot++)
    {
      ref var s = ref _snap[slot];
      s.Pawn = 0;
      s.Alive = false;
      s.Corpse = false;
      s.PosTick = -1;
      s.WeaponTick = -1;

      var player = _players[slot];
      if (player == null || !player.IsValid)
        continue;

      uint raw = player.PlayerPawn.Raw;
      nint ptr = EntitySystem.GetEntityByHandle(raw) ?? IntPtr.Zero;
      if (ptr == IntPtr.Zero)
      {
        _pawns[slot] = null;
        continue;
      }

      var pawn = _pawns[slot];
      if (pawn == null || pawn.Handle != ptr)
        _pawns[slot] = pawn = new CCSPlayerPawn(ptr);

      s.Pawn = (int)(raw & 0x3FFF);
      s.Team = player.TeamNum;
      s.Alive = pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE && pawn.Health > 0 && (uint)(tick - _dyingTick[slot]) > DyingTicks;
      s.Corpse = !s.Alive && now - _deathTime[slot] >= delay;
    }
  }

  private bool Position(int slot, int tick)
  {
    ref var s = ref _snap[slot];
    if (s.PosTick == tick)
      return true;

    var pawn = _pawns[slot];
    var origin = pawn?.AbsOrigin;
    if (pawn == null || origin == null)
      return false;

    var velocity = pawn.AbsVelocity;
    s.Origin = new Vector3(origin.X, origin.Y, origin.Z);
    s.Velocity = new Vector3(velocity.X, velocity.Y, velocity.Z);
    s.EyeZ = pawn.ViewOffset.Z;
    s.Ping = (int)(_players[slot]?.Ping ?? 0);
    s.PosTick = tick;
    return true;
  }

  private unsafe void OnCheckTransmit(CCheckTransmitInfoList infoList)
  {
    int tick = Server.TickCount;
    if (tick != _snapTick)
      BuildSnapshot(tick);

    bool forceCorpses = Config.HideCorpses == Force;
    nint* list = (nint*)infoList.Handle;
    nint* entries = (nint*)list[0];
    int count = (int)list[1];

    for (int k = 0; k < count; k++)
    {
      nint entry = entries[(k + tick) % count];
      int viewer = *(int*)(entry + _slotOffset);
      if ((uint)viewer >= MaxSlots)
        continue;

      _hiddenMask[viewer] = 0;
      int flags = _flags[viewer];
      bool corpses = forceCorpses || (flags & Corpses) != 0;
      if (!corpses && _unseen[viewer] == 0 && (flags & Props) == 0)
        continue;

      uint* dont = *(uint**)(entry + 8);
      if (dont != null)
        Filter(*(uint**)entry, dont, viewer, flags, corpses, tick);
    }
  }

  private unsafe void Filter(uint* bits, uint* dont, int v, int flags, bool corpses, int tick)
  {
    if ((flags & Props) != 0)
    {
      foreach (int word in _propWords)
      {
        uint hide = bits[word] & _propMask[word];
        bits[word] &= ~hide;
        dont[word] |= hide;
      }
    }

    int mode = _unseen[v];
    ref var sv = ref _snap[v];
    bool unseen = mode != 0 && sv.Alive;
    int observed = corpses && !sv.Alive ? ObservedPawn(v) : 0;
    ulong hidden = 0;

    for (int t = 0; t < MaxSlots; t++)
    {
      if (t == v)
        continue;

      ref var st = ref _snap[t];
      if (st.Pawn == 0)
        continue;

      if (!st.Alive)
      {
        if (corpses && st.Corpse && st.Pawn != observed)
          Clear(bits, dont, st.Pawn);
        continue;
      }

      if (!unseen || (bits[st.Pawn >> 5] & (1u << (st.Pawn & 31))) == 0)
        continue;

      if (mode != 3 && Teammates(t, v) != (mode == 1))
        continue;

      if (IsVisible(v, t, tick))
        continue;

      hidden |= 1UL << t;

      Clear(bits, dont, st.Pawn);

      int weapons = Weapons(t, tick);
      for (int w = 0; w < weapons; w++)
        Clear(bits, dont, _weapons[t * MaxWeapons + w]);
    }

    _hiddenMask[v] = hidden;
  }

  private bool Teammates(int a, int b) => !_ffa && _snap[a].Team == _snap[b].Team;

  private int ObservedPawn(int slot)
  {
    var target = _players[slot]?.ObserverPawn.Value?.ObserverServices?.ObserverTarget;
    if (target == null)
      return 0;

    uint raw = target.Raw;
    return raw == uint.MaxValue ? 0 : (int)(raw & 0x3FFF);
  }

  private static unsafe void Clear(uint* bits, uint* dont, int index)
  {
    if (index <= 0 || index >= MaxEntities)
      return;

    int word = index >> 5;
    uint bit = 1u << (index & 31);
    if ((bits[word] & bit) == 0)
      return;

    bits[word] &= ~bit;
    dont[word] |= bit;
  }

  private int Weapons(int slot, int tick)
  {
    ref var s = ref _snap[slot];
    if (s.WeaponTick == tick)
      return s.WeaponCount;

    s.WeaponTick = tick;
    s.WeaponCount = 0;

    var weapons = _pawns[slot]?.WeaponServices?.MyWeapons;
    if (weapons == null)
      return 0;

    foreach (var handle in weapons)
    {
      uint raw = handle.Raw;
      if (raw == uint.MaxValue)
        continue;

      _weapons[slot * MaxWeapons + s.WeaponCount++] = (int)(raw & 0x3FFF);
      if (s.WeaponCount == MaxWeapons)
        break;
    }

    return s.WeaponCount;
  }

  private bool IsVisible(int v, int t, int tick)
  {
    int pair = v * MaxSlots + t;
    if (_visibleUntil[pair] > tick)
      return true;

    if (_nextCheck[pair] > tick)
      return false;

    if (!Position(v, tick) || !Position(t, tick))
      return Show(pair, tick, HoldTicks);

    ref var sv = ref _snap[v];
    ref var st = ref _snap[t];

    if (!_seen[pair] && tick - _checkedAt[pair] < MaxHiddenTicks
        && Vector3.DistanceSquared(sv.Origin, _fromPos[pair]) < MoveSqr
        && Vector3.DistanceSquared(st.Origin, _toPos[pair]) < MoveSqr)
    {
      _nextCheck[pair] = tick + RecheckTicks;
      return false;
    }

    if (_traceTime > TraceBudget)
      return Show(pair, tick, SafeHoldTicks);

    long start = Stopwatch.GetTimestamp();
    bool visible = CanSee(v, t);
    _traceTime += Stopwatch.GetTimestamp() - start;

    if (visible)
      return Show(pair, tick, HoldTicks);

    if (_seen[pair])
    {
      _seen[pair] = false;
      _checkedAt[pair] = tick - MaxHiddenTicks;
      _visibleUntil[pair] = tick + GraceTicks;
      return true;
    }

    _seen[pair] = false;
    _checkedAt[pair] = tick;
    _fromPos[pair] = sv.Origin;
    _toPos[pair] = st.Origin;
    _nextCheck[pair] = tick + RecheckTicks;
    return false;
  }

  private bool Show(int pair, int tick, int hold)
  {
    _seen[pair] = true;
    _visibleUntil[pair] = tick + hold;
    return true;
  }

  private bool CanSee(int v, int t)
  {
    ref var sv = ref _snap[v];
    ref var st = ref _snap[t];

    var delta = st.Origin - sv.Origin;
    if (delta.LengthSquared() < NearDistanceSqr)
      return true;

    var pawn = _pawns[v];
    if (pawn == null)
      return true;

    int o = v * 6;
    Origins(v, pawn);

    var head = st.Origin + new Vector3(0f, 0f, st.EyeZ + 4f);
    var chest = st.Origin + new Vector3(0f, 0f, 36f);
    var flat = new Vector2(delta.X, delta.Y);
    flat = flat.LengthSquared() > 1f ? Vector2.Normalize(flat) : Vector2.UnitX;
    var pad = new Vector3(-flat.Y, flat.X, 0f) * BodyPad;

    _current[0] = head;
    _current[1] = chest;
    _current[2] = chest + pad;
    _current[3] = chest - pad;
    _current[4] = st.Origin + new Vector3(0f, 0f, 8f);

    foreach (var point in _current)
    {
      if (Clear(pawn, _origins[o], point))
        return true;
    }

    if (Clear(pawn, _origins[o + 1], head) || Clear(pawn, _origins[o + 2], head)
        || Clear(pawn, _origins[o + 1], chest) || Clear(pawn, _origins[o + 2], chest))
      return true;

    var targetMove = new Vector3(st.Velocity.X, st.Velocity.Y, 0f);
    if (!_moving[v] && targetMove.LengthSquared() < MovingSqr)
      return false;

    var target = st.Origin + targetMove * Lead(sv.Ping);
    _ahead[0] = target + new Vector3(0f, 0f, st.EyeZ + 4f);
    _ahead[1] = target + new Vector3(0f, 0f, 36f);

    foreach (var point in _ahead)
    {
      for (int k = 3; k < 6; k++)
      {
        if (Clear(pawn, _origins[o + k], point))
          return true;
      }
    }

    return false;
  }

  private static float Lead(int ping) => Math.Clamp(0.15f + ping / 1000f, 0.15f, 0.5f);

  private void Origins(int v, CCSPlayerPawn pawn)
  {
    int tick = _snapTick;
    if ((uint)(tick - _originTick[v]) < OriginTicks)
      return;

    _originTick[v] = tick;
    ref var sv = ref _snap[v];
    int o = v * 6;

    float yaw = pawn.EyeAngles.Y * (MathF.PI / 180f);
    var right = new Vector3(MathF.Sin(yaw), -MathF.Cos(yaw), 0f);
    float shoulder = Math.Min(ShoulderBase + MathF.Floor(sv.Ping / 25f) * 25f * ShoulderPerMs, ShoulderMax);
    var eye = sv.Origin + new Vector3(0f, 0f, sv.EyeZ);

    _origins[o] = eye;
    _origins[o + 1] = Reach(pawn, eye, eye + right * shoulder);
    _origins[o + 2] = Reach(pawn, eye, eye - right * shoulder);

    var move = new Vector3(sv.Velocity.X, sv.Velocity.Y, 0f);
    _moving[v] = move.LengthSquared() >= MovingSqr;
    var ahead = _moving[v] ? Reach(pawn, eye, eye + move * Lead(sv.Ping)) : eye;

    _origins[o + 3] = ahead;
    _origins[o + 4] = Reach(pawn, ahead, ahead + right * shoulder);
    _origins[o + 5] = Reach(pawn, ahead, ahead - right * shoulder);
  }

  private Vector3 Reach(CCSPlayerPawn pawn, Vector3 from, Vector3 to)
  {
    _traceStart.X = from.X; _traceStart.Y = from.Y; _traceStart.Z = from.Z;
    _traceEnd.X = to.X; _traceEnd.Y = to.Y; _traceEnd.Z = to.Z;

    try
    {
      var result = Trace.TraceEndShape(_traceStart, _traceEnd, pawn, _traceOptions);
      if (result.Fraction >= 0.99f && !result.IsAllSolid)
        return to;

      float length = Vector3.Distance(from, to);
      float keep = length > 0f ? Math.Max(0f, result.Fraction - ClipMargin / length) : 0f;
      return from + (to - from) * keep;
    }
    catch
    {
      return from;
    }
  }

  private bool Clear(CCSPlayerPawn pawn, Vector3 from, Vector3 to)
  {
    _traceStart.X = from.X; _traceStart.Y = from.Y; _traceStart.Z = from.Z;
    _traceEnd.X = to.X; _traceEnd.Y = to.Y; _traceEnd.Z = to.Z;

    try
    {
      var result = Trace.TraceEndShape(_traceStart, _traceEnd, pawn, _traceOptions);
      return result.Fraction >= 0.99f && !result.IsAllSolid;
    }
    catch
    {
      return true;
    }
  }
}
