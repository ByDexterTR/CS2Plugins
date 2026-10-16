using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace GoBhop;

public class GoBhopPoint
{
  [JsonPropertyName("pos")]
  public float[] Pos { get; set; } = Array.Empty<float>();

  [JsonPropertyName("ang")]
  public float[] Ang { get; set; } = Array.Empty<float>();
}

public class GoBhopConfig : BasePluginConfig
{
  [JsonPropertyName("gobhop_cmd")]
  public string Commands { get; set; } = "css_gobhop";

  [JsonPropertyName("onbhop_cmd")]
  public string OnCommands { get; set; } = "css_onbhop";

  [JsonPropertyName("offbhop_cmd")]
  public string OffCommands { get; set; } = "css_offbhop";

  [JsonPropertyName("set_cmd")]
  public string SetCommands { get; set; } = "css_setbhop";

  [JsonPropertyName("del_cmd")]
  public string DelCommands { get; set; } = "css_delbhop";

  [JsonPropertyName("reset_cmd")]
  public string ResetCommands { get; set; } = "css_resetbhop";

  [JsonPropertyName("blocked_cmd")]
  public string BlockedCommands { get; set; } = "css_wp";

  [JsonPropertyName("admin_flag")]
  public string AdminFlag { get; set; } = "@css/ban";

  [JsonPropertyName("set_flag")]
  public string SetFlag { get; set; } = "@css/root";

  [JsonPropertyName("gobhop_min_alive_t")]
  public int MinAliveT { get; set; } = 2;
}

public class GoBhop : BasePlugin, IPluginConfig<GoBhopConfig>
{
  public override string ModuleName => "GoBhop";
  public override string ModuleVersion => "1.0.4";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private const int MaxSlots = 64;
  private const int MaxEntities = 16384;
  private const int MaxWeapons = 64;
  private const int SideHookTicks = 64;
  private const int TextMsgMessage = 124;
  private const int ParticleMessage = 145;
  private const int SoundEventMessage = 208;
  private const int WeaponSoundMessage = 369;
  private const int EffectMessage = 400;
  private const int FireBulletsMessage = 452;

  private string ChatPrefix => Localizer["chat_prefix"];

  public GoBhopConfig Config { get; set; } = new();

  private string PointsPath => Path.Combine(ModuleDirectory, "positions.json");

  private Dictionary<string, Dictionary<string, GoBhopPoint>> _points = new(StringComparer.OrdinalIgnoreCase);
  private readonly Dictionary<int, bool> _bhop = new();
  private bool _adminClosed;
  private bool _roundClosed;

  private readonly CCSPlayerController?[] _players = new CCSPlayerController?[MaxSlots];
  private readonly nint[] _controllers = new nint[MaxSlots];
  private readonly bool[] _member = new bool[MaxSlots];
  private readonly int[] _pawns = new int[MaxSlots];
  private readonly uint[] _pawnHandles = new uint[MaxSlots];
  private readonly int[] _weaponTick = new int[MaxSlots];
  private readonly int[] _weaponCount = new int[MaxSlots];
  private readonly int[] _weapons = new int[MaxSlots * MaxWeapons];
  private ulong _memberMask;
  private ulong _sideMask;
  private ulong _viewerMask;
  private bool _hooked;
  private bool _sideHooked;
  private int _sideSeen;

  private int _slotOffset;
  private int _pawnHandleOffset;
  private int _pawnIsAliveOffset;
  private int _observerPawnOffset;
  private int _lifeStateOffset;
  private int _observerServicesOffset;
  private int _observerTargetOffset;
  private int _weaponServicesOffset;
  private int _myWeaponsOffset;

  private WasdMenuManager _menus = null!;
  private OnTick _onTick = null!;
  private CheckTransmit _onCheckTransmit = null!;
  private UserMessage.UserMessageHandler _onSoundEvent = null!;
  private UserMessage.UserMessageHandler _onWeaponSound = null!;
  private UserMessage.UserMessageHandler _onFireBullets = null!;
  private UserMessage.UserMessageHandler _onParticle = null!;
  private UserMessage.UserMessageHandler _onEffect = null!;
  private UserMessage.UserMessageHandler _onTextMsg = null!;

  private bool InBhop(CCSPlayerController player) => _bhop.TryGetValue(Util.UserId(player), out var pendingDeath) && !pendingDeath;

  private int ActiveCount => System.Numerics.BitOperations.PopCount(_memberMask);

  public void OnConfigParsed(GoBhopConfig config)
  {
    if (config.MinAliveT < 1)
      config.MinAliveT = 1;
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    _slotOffset = GameData.GetOffset("CheckTransmitPlayerSlot");
    _pawnHandleOffset = Schema.GetSchemaOffset("CCSPlayerController", "m_hPlayerPawn");
    _pawnIsAliveOffset = Schema.GetSchemaOffset("CCSPlayerController", "m_bPawnIsAlive");
    _observerPawnOffset = Schema.GetSchemaOffset("CCSPlayerController", "m_hObserverPawn");
    _lifeStateOffset = Schema.GetSchemaOffset("CBaseEntity", "m_lifeState");
    _observerServicesOffset = Schema.GetSchemaOffset("CBasePlayerPawn", "m_pObserverServices");
    _observerTargetOffset = Schema.GetSchemaOffset("CPlayer_ObserverServices", "m_hObserverTarget");
    _weaponServicesOffset = Schema.GetSchemaOffset("CBasePlayerPawn", "m_pWeaponServices");
    _myWeaponsOffset = Schema.GetSchemaOffset("CPlayer_WeaponServices", "m_hMyWeapons");

    _onTick = OnTick;
    _onCheckTransmit = OnCheckTransmit;
    _onSoundEvent = um => FilterBySource(um, "source_entity_index", handle: false, fromMembers: true);
    _onWeaponSound = um => FilterBySource(um, "entidx", handle: false, fromMembers: false);
    _onFireBullets = um => FilterBySource(um, "player", handle: true, fromMembers: false);
    _onParticle = um => FilterByHandles(um, "entity_handle: ");
    _onEffect = um => FilterByHandles(um, "entity: ");
    _onTextMsg = OnTextMsg;

    _menus = new WasdMenuManager(this,
      () => Localizer["menu.scroll"],
      () => Localizer["menu.select"],
      () => Localizer["menu.exit"]);
    LoadPoints();

    foreach (var name in Util.Split(Config.Commands))
      AddCommand(name, "GoBhop menusunu acar", OnGoBhopCommand);

    foreach (var name in Util.Split(Config.OnCommands))
      AddCommand(name, "GoBhop'a gitmeyi acar", OnOnBhopCommand);

    foreach (var name in Util.Split(Config.OffCommands))
      AddCommand(name, "GoBhop'u kapatir ve icindekileri cikarir", OnOffBhopCommand);

    foreach (var name in Util.Split(Config.SetCommands))
      AddCommand(name, "GoBhop noktasini kaydeder", OnSetCommand);

    foreach (var name in Util.Split(Config.DelCommands))
      AddCommand(name, "Isimli GoBhop noktasini siler", OnDelCommand);

    foreach (var name in Util.Split(Config.ResetCommands))
      AddCommand(name, "Haritanin kayitli GoBhop noktalarini siler", OnResetCommand);

    foreach (var name in Util.Split(Config.BlockedCommands))
      AddCommandListener(name, OnBlockedCommand, HookMode.Pre);

    AddCommandListener("drop", OnDropCommand, HookMode.Pre);
    AddCommandListener("kill", OnKillCommand, HookMode.Pre);
    AddCommandListener("explode", OnKillCommand, HookMode.Pre);

    RegisterListener<OnMapStart>(_ =>
    {
      ResetState();
      LoadPoints();
    });

    RegisterListener<OnClientPutInServer>(OnClientPutInServer);
    RegisterListener<OnClientDisconnect>(OnClientDisconnect);

    RegisterEventHandler<EventRoundPrestart>(OnRoundPrestart);
    RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
    RegisterEventHandler<EventPlayerDeath>(OnPlayerDeathPre, HookMode.Pre);
    RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
    RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    RegisterEventHandler<EventItemPickup>(OnItemPickup);
    RegisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Pre);

    Server.NextWorldUpdate(() =>
    {
      foreach (var player in Utilities.GetPlayers())
        Track(player);
    });
  }

  public override void Unload(bool hotReload)
  {
    _menus.Clear();
    RemoveAll(slay: true);
    SetHooks(false);
  }

  private void OnClientPutInServer(int slot)
  {
    var player = Utilities.GetPlayerFromSlot(slot);
    if (player != null)
      Track(player);
  }

  private void OnClientDisconnect(int slot)
  {
    if ((uint)slot >= MaxSlots)
      return;

    _players[slot] = null;
    _controllers[slot] = 0;
    _pawns[slot] = 0;
    _member[slot] = false;
    ulong keep = ~(1UL << slot);
    _memberMask &= keep;
    _sideMask &= keep;
    _viewerMask &= keep;
  }

  private void Track(CCSPlayerController player)
  {
    int slot = player.Slot;
    if ((uint)slot >= MaxSlots || !player.IsValid)
      return;

    _players[slot] = player;
    _controllers[slot] = player.Handle;
  }

  private void OnGoBhopCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    bool inBhop = InBhop(player);

    if (!inBhop && !ValidateEntry(player))
      return;

    if (!_points.TryGetValue(Server.MapName, out var mapPoints) || mapPoints.Count == 0)
    {
      if (!inBhop)
      {
        player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.no_point"]}");
        return;
      }
      mapPoints = new(StringComparer.OrdinalIgnoreCase);
    }

    if (!inBhop && mapPoints.Count == 1)
    {
      TryEnterBhop(player, mapPoints.Values.First());
      return;
    }

    var items = new List<WasdItem>();

    if (inBhop)
      items.Add(new WasdItem
      {
        Text = Localizer["gobhop.menu_exit"],
        OnSelect = p =>
        {
          _menus.Close(p);
          if (p.IsValid && InBhop(p))
            ExitBhop(p, slay: true);
        }
      });

    foreach (var kv in mapPoints)
    {
      var point = kv.Value;
      items.Add(new WasdItem
      {
        Text = kv.Key,
        OnSelect = p =>
        {
          _menus.Close(p);
          if (!p.IsValid)
            return;

          if (InBhop(p))
            TeleportInBhop(p, point);
          else
            TryEnterBhop(p, point);
        }
      });
    }

    _menus.Open(player, Localizer["gobhop.menu_title"], items);
  }

  private void TeleportInBhop(CCSPlayerController player, GoBhopPoint point)
  {
    if (point.Pos.Length < 3)
      return;

    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
      return;

    TeleportToPoint(pawn, point);
  }

  private static void TeleportToPoint(CCSPlayerPawn pawn, GoBhopPoint point)
  {
    pawn.Teleport(new Vector(point.Pos[0], point.Pos[1], point.Pos[2]),
      new QAngle(point.Ang.Length > 1 ? point.Ang[0] : 0, point.Ang.Length > 1 ? point.Ang[1] : 0, 0), Vector.Zero);
  }

  private bool ValidateEntry(CCSPlayerController player)
  {
    if (InBhop(player))
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.already_in"]}");
      return false;
    }

    if (player.Team != CsTeam.Terrorist)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.only_t"]}");
      return false;
    }

    if (IsAlive(player))
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.must_be_dead"]}");
      return false;
    }

    if (_adminClosed || _roundClosed)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.closed"]}");
      return false;
    }

    if (CountRealAliveT() < Config.MinAliveT)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.not_enough_t"]}");
      return false;
    }

    return true;
  }

  private void TryEnterBhop(CCSPlayerController? player, GoBhopPoint point)
  {
    if (player == null || !player.IsValid || point.Pos.Length < 3)
      return;

    if (!ValidateEntry(player))
      return;

    int userId = Util.UserId(player);
    if (userId < 0)
      return;

    _bhop[userId] = false;
    UpdateMembers();
    player.Respawn();
    SetupPawn(player, point);
    Server.NextFrame(() => SetupPawn(player, point));
    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.entered"]}");
  }

  private void OnOnBhopCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (!Util.HasAccess(player, Config.AdminFlag))
    {
      info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.no_permission"]}");
      return;
    }

    if (!_adminClosed)
    {
      info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.already_open"]}");
      return;
    }

    _adminClosed = false;
    Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.admin_opened"]}");
  }

  private void OnOffBhopCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (!Util.HasAccess(player, Config.AdminFlag))
    {
      info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.no_permission"]}");
      return;
    }

    if (_adminClosed && ActiveCount == 0)
    {
      info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.already_closed"]}");
      return;
    }

    _adminClosed = true;
    RemoveAll(slay: true);
    Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.admin_closed"]}");
  }

  private void OnSetCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    if (!Util.HasAccess(player, Config.SetFlag))
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.no_permission"]}");
      return;
    }

    string name = info.GetArg(1).Trim();
    if (name.Length == 0)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.set_usage", Util.Split(Config.SetCommands).FirstOrDefault() ?? ""]}");
      return;
    }

    var pawn = player.PlayerPawn.Value;
    var pos = pawn?.AbsOrigin;
    if (pawn == null || pos == null)
      return;

    if (!_points.TryGetValue(Server.MapName, out var mapPoints))
    {
      mapPoints = new(StringComparer.OrdinalIgnoreCase);
      _points[Server.MapName] = mapPoints;
    }

    mapPoints[name] = new GoBhopPoint
    {
      Pos = new[] { pos.X, pos.Y, pos.Z },
      Ang = new[] { 0f, pawn.EyeAngles.Y, 0f }
    };
    SavePoints();
    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.point_saved", name, Server.MapName]}");
  }

  private void OnDelCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    if (!Util.HasAccess(player, Config.SetFlag))
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.no_permission"]}");
      return;
    }

    string name = info.GetArg(1).Trim();
    if (name.Length == 0)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.set_usage", Util.Split(Config.DelCommands).FirstOrDefault() ?? ""]}");
      return;
    }

    if (!_points.TryGetValue(Server.MapName, out var mapPoints) || !mapPoints.Remove(name))
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.point_not_found", name]}");
      return;
    }

    if (mapPoints.Count == 0)
      _points.Remove(Server.MapName);
    SavePoints();
    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.point_deleted", name, Server.MapName]}");
  }

  private void OnResetCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    if (!Util.HasAccess(player, Config.SetFlag))
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.no_permission"]}");
      return;
    }

    int count = _points.TryGetValue(Server.MapName, out var mapPoints) ? mapPoints.Count : 0;
    _points.Remove(Server.MapName);
    SavePoints();
    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.points_reset", count, Server.MapName]}");
  }

  private HookResult OnBlockedCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || !InBhop(player))
      return HookResult.Continue;

    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.cmd_blocked"]}");
    return HookResult.Handled;
  }

  private HookResult OnDropCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || !InBhop(player))
      return HookResult.Continue;

    return HookResult.Handled;
  }

  private HookResult OnKillCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || !InBhop(player))
      return HookResult.Continue;

    ExitBhop(player, slay: true);
    return HookResult.Handled;
  }

  private HookResult FilterBySource(UserMessage um, string field, bool handle, bool fromMembers)
  {
    ulong mask = um.Recipients.GetRecipientMask();
    ulong humans = mask & _viewerMask;
    if (humans == 0 || (!fromMembers && (humans & _sideMask) == 0))
      return HookResult.Continue;

    int index = handle ? (int)(um.ReadUInt(field) & 0x3FFF) : um.ReadInt(field);
    if ((humans & _sideMask) == 0)
      return IsMemberPawn(index) ? Send(um, mask, mask & ~humans) : HookResult.Continue;

    int owner = OwnerSlot(index);
    if (owner < 0)
      return HookResult.Continue;

    ulong rest = _member[owner] ? mask & ~(humans & ~_sideMask) : mask & ~_sideMask;
    return Send(um, mask, rest);
  }

  private HookResult FilterByHandles(UserMessage um, string key)
  {
    ulong mask = um.Recipients.GetRecipientMask();
    if ((mask & _sideMask) == 0)
      return HookResult.Continue;

    string debug = um.DebugString;
    int at = 0;
    while ((at = debug.IndexOf(key, at, StringComparison.Ordinal)) >= 0)
    {
      uint raw = 0;
      for (at += key.Length; at < debug.Length && char.IsAsciiDigit(debug[at]); at++)
        raw = raw * 10 + (uint)(debug[at] - '0');

      if (raw == 0 || raw == uint.MaxValue)
        continue;

      int owner = OwnerSlot((int)(raw & 0x3FFF));
      if (owner >= 0 && !_member[owner])
        return Send(um, mask, mask & ~_sideMask);
    }

    return HookResult.Continue;
  }

  private static HookResult Send(UserMessage um, ulong mask, ulong rest)
  {
    if (rest == mask)
      return HookResult.Continue;

    if (rest == 0)
      return HookResult.Stop;

    um.Recipients = new RecipientFilter(rest);
    return HookResult.Continue;
  }

  private int OwnerSlot(int index)
  {
    if (index <= 0 || index >= MaxEntities)
      return -1;

    for (int slot = 0; slot < MaxSlots; slot++)
    {
      if (_pawns[slot] == index)
        return slot;
    }

    int tick = Server.TickCount;
    for (int slot = 0; slot < MaxSlots; slot++)
    {
      if (_pawns[slot] == 0)
        continue;

      int count = Weapons(slot, tick);
      int start = slot * MaxWeapons;
      for (int w = 0; w < count; w++)
      {
        if (_weapons[start + w] == index)
          return slot;
      }
    }

    return -1;
  }

  private static HookResult OnTextMsg(UserMessage um)
  {
    if (um.GetRepeatedFieldCount("param") == 0)
      return HookResult.Continue;

    return um.ReadString("param", 0).StartsWith("#Player_Cash_Award_", StringComparison.Ordinal) ? HookResult.Stop : HookResult.Continue;
  }

  private bool IsMemberPawn(int index)
  {
    if (index <= 0)
      return false;

    ulong pending = _memberMask;
    while (pending != 0)
    {
      int slot = System.Numerics.BitOperations.TrailingZeroCount(pending);
      pending &= pending - 1;
      if (_pawns[slot] == index)
        return true;
    }
    return false;
  }

  private HookResult OnRoundPrestart(EventRoundPrestart @event, GameEventInfo info)
  {
    ResetState();
    return HookResult.Continue;
  }

  private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
  {
    RemoveAll(slay: true);
    return HookResult.Continue;
  }

  private HookResult OnPlayerDeathPre(EventPlayerDeath @event, GameEventInfo info)
  {
    int userId = Util.UserId(@event.Userid);
    if (!_bhop.TryGetValue(userId, out var pendingDeath) || !pendingDeath)
      return HookResult.Continue;

    info.DontBroadcast = true;
    _bhop.Remove(userId);
    UpdateMembers();
    return HookResult.Continue;
  }

  private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player != null && player.IsValid && InBhop(player))
      ExitBhop(player, slay: false);

    CheckLastT();
    return HookResult.Continue;
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    if (_bhop.Remove(Util.UserId(@event.Userid)))
      UpdateMembers();

    Server.NextFrame(CheckLastT);
    return HookResult.Continue;
  }

  private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player != null && player.IsValid && InBhop(player) && @event.Team != (byte)CsTeam.Terrorist)
      ExitBhop(player, slay: true);

    Server.NextFrame(CheckLastT);
    return HookResult.Continue;
  }

  private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || !InBhop(player))
      return HookResult.Continue;

    if (!_roundClosed && !_adminClosed)
      return HookResult.Continue;

    Server.NextFrame(() =>
    {
      if (player.IsValid && InBhop(player))
        ExitBhop(player, slay: true);
    });
    return HookResult.Continue;
  }

  private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || !InBhop(player))
      return HookResult.Continue;

    Server.NextFrame(() =>
    {
      if (player.IsValid && InBhop(player))
        player.RemoveWeapons();
    });
    return HookResult.Continue;
  }

  private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || !InBhop(player))
      return HookResult.Continue;

    info.DontBroadcast = true;
    player.RemoveWeapons();
    return HookResult.Continue;
  }

  private unsafe void OnTick()
  {
    for (int slot = 0; slot < MaxSlots; slot++)
    {
      nint controller = _controllers[slot];
      if (controller == 0)
      {
        _pawns[slot] = 0;
        continue;
      }

      uint raw = *(uint*)(controller + _pawnHandleOffset);
      _pawnHandles[slot] = raw;
      _pawns[slot] = raw == uint.MaxValue ? 0 : (int)(raw & 0x3FFF);

      if (_member[slot] && *(bool*)(controller + _pawnIsAliveOffset))
        SetScoreboardDead(_players[slot]!, dead: true);
    }
  }

  private unsafe void OnCheckTransmit(CCheckTransmitInfoList infoList)
  {
    int tick = Server.TickCount;
    nint* list = (nint*)infoList.Handle;
    nint* entries = (nint*)list[0];
    int count = (int)list[1];
    ulong sideMask = 0;
    ulong viewerMask = 0;

    for (int k = 0; k < count; k++)
    {
      nint entry = entries[k];
      int viewer = *(int*)(entry + _slotOffset);
      if ((uint)viewer >= MaxSlots)
        continue;

      viewerMask |= 1UL << viewer;
      bool side = _member[viewer] || IsMemberPawn(ObservedPawn(viewer));
      if (side)
        sideMask |= 1UL << viewer;

      uint* dont = *(uint**)(entry + 8);
      if (dont == null)
        continue;

      uint* bits = *(uint**)entry;
      for (int t = 0; t < MaxSlots; t++)
      {
        int pawn = _pawns[t];
        if (t == viewer || pawn <= 0 || pawn >= MaxEntities || _member[t] == side)
          continue;

        if ((bits[pawn >> 5] & (1u << (pawn & 31))) == 0)
          continue;

        Clear(bits, dont, pawn);

        int weapons = Weapons(t, tick);
        int start = t * MaxWeapons;
        for (int w = 0; w < weapons; w++)
          Clear(bits, dont, _weapons[start + w]);
      }
    }

    _sideMask = sideMask;
    if (sideMask != 0)
      _sideSeen = tick;
    SetSideHooks(sideMask != 0 || tick - _sideSeen < SideHookTicks);
    _viewerMask = viewerMask;
  }

  private unsafe int ObservedPawn(int slot)
  {
    nint controller = _controllers[slot];
    nint pawn = Entity(_pawnHandles[slot]);
    if (controller == 0 || pawn == 0 || *(byte*)(pawn + _lifeStateOffset) == (byte)LifeState_t.LIFE_ALIVE)
      return 0;

    nint observer = Entity(*(uint*)(controller + _observerPawnOffset));
    nint services = observer == 0 ? 0 : *(nint*)(observer + _observerServicesOffset);
    if (services == 0)
      return 0;

    uint raw = *(uint*)(services + _observerTargetOffset);
    return raw == uint.MaxValue ? 0 : (int)(raw & 0x3FFF);
  }

  private unsafe int Weapons(int slot, int tick)
  {
    if (_weaponTick[slot] == tick)
      return _weaponCount[slot];

    _weaponTick[slot] = tick;
    _weaponCount[slot] = 0;

    nint pawn = Entity(_pawnHandles[slot]);
    nint services = pawn == 0 ? 0 : *(nint*)(pawn + _weaponServicesOffset);
    if (services == 0)
      return 0;

    int count = *(int*)(services + _myWeaponsOffset);
    uint* handles = *(uint**)(services + _myWeaponsOffset + 8);
    if (count <= 0 || handles == null)
      return 0;

    int start = slot * MaxWeapons;
    int found = 0;
    for (int i = 0; i < count && found < MaxWeapons; i++)
    {
      uint raw = handles[i];
      if (raw != uint.MaxValue)
        _weapons[start + found++] = (int)(raw & 0x3FFF);
    }

    _weaponCount[slot] = found;
    return found;
  }

  private static nint Entity(uint raw) => raw == uint.MaxValue ? 0 : EntitySystem.GetEntityByHandle(raw) ?? 0;

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

  private void UpdateMembers()
  {
    Array.Clear(_member);
    _memberMask = 0;
    foreach (var (userId, pendingDeath) in _bhop)
    {
      if (pendingDeath)
        continue;

      var player = Util.FromUserId(userId);
      if (player != null && (uint)player.Slot < MaxSlots)
      {
        _member[player.Slot] = true;
        _memberMask |= 1UL << player.Slot;
        Track(player);
      }
    }

    _sideMask = _memberMask & _viewerMask;
    SetHooks(_memberMask != 0);
  }

  private void SetHooks(bool on)
  {
    if (on == _hooked)
      return;

    _hooked = on;

    if (on)
    {
      OnTick();
      RegisterListener(_onTick);
      RegisterListener(_onCheckTransmit);
      HookUserMessage(SoundEventMessage, _onSoundEvent, HookMode.Pre);
    }
    else
    {
      RemoveListener(_onTick);
      RemoveListener(_onCheckTransmit);
      UnhookUserMessage(SoundEventMessage, _onSoundEvent, HookMode.Pre);
      SetSideHooks(false);
      _sideMask = 0;
      _viewerMask = 0;
    }
  }

  private void SetSideHooks(bool on)
  {
    if (on == _sideHooked)
      return;

    _sideHooked = on;

    if (on)
    {
      HookUserMessage(WeaponSoundMessage, _onWeaponSound, HookMode.Pre);
      HookUserMessage(FireBulletsMessage, _onFireBullets, HookMode.Pre);
      HookUserMessage(ParticleMessage, _onParticle, HookMode.Pre);
      HookUserMessage(EffectMessage, _onEffect, HookMode.Pre);
    }
    else
    {
      UnhookUserMessage(WeaponSoundMessage, _onWeaponSound, HookMode.Pre);
      UnhookUserMessage(FireBulletsMessage, _onFireBullets, HookMode.Pre);
      UnhookUserMessage(ParticleMessage, _onParticle, HookMode.Pre);
      UnhookUserMessage(EffectMessage, _onEffect, HookMode.Pre);
    }
  }

  private void SetupPawn(CCSPlayerController player, GoBhopPoint point)
  {
    if (!player.IsValid || !InBhop(player))
      return;

    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
      return;

    TeleportToPoint(pawn, point);

    player.RemoveWeapons();

    pawn.TakesDamage = false;

    SetScoreboardDead(player, dead: true);
  }

  private void ExitBhop(CCSPlayerController player, bool slay)
  {
    int userId = Util.UserId(player);
    if (!_bhop.ContainsKey(userId))
      return;

    var pawn = player.PlayerPawn.Value;
    if (pawn != null && pawn.IsValid)
    {
      pawn.TakesDamage = true;

      if (pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
      {
        if (slay)
        {
          _bhop[userId] = true;
          UpdateMembers();
          Slay(player, pawn);
          SetScoreboardDead(player, dead: true);
          if (_bhop.Remove(userId))
            UpdateMembers();
          return;
        }

        SetScoreboardDead(player, dead: false);
      }
    }

    _bhop.Remove(userId);
    UpdateMembers();
  }

  private void Slay(CCSPlayerController player, CCSPlayerPawn pawn)
  {
    var stats = player.ActionTrackingServices?.MatchStats;
    int deaths = stats?.Deaths ?? 0;
    int score = player.Score;

    var money = new List<(CCSPlayerController Player, int Account)>();
    foreach (var p in Utilities.GetPlayers())
    {
      if (p.InGameMoneyServices != null)
        money.Add((p, p.InGameMoneyServices.Account));
    }

    HookUserMessage(TextMsgMessage, _onTextMsg, HookMode.Pre);
    pawn.CommitSuicide(false, true);
    UnhookUserMessage(TextMsgMessage, _onTextMsg, HookMode.Pre);

    foreach (var (p, account) in money)
    {
      var services = p.IsValid ? p.InGameMoneyServices : null;
      if (services == null || services.Account == account)
        continue;

      services.Account = account;
      NetState.Changed(p, "CCSPlayerController", "m_pInGameMoneyServices");
    }

    if (stats != null && stats.Deaths != deaths)
    {
      stats.Deaths = deaths;
      NetState.Changed(player, "CCSPlayerController", "m_pActionTrackingServices");
    }

    if (player.Score != score)
    {
      player.Score = score;
      NetState.Changed(player, "CCSPlayerController", "m_iScore");
    }
  }

  private void RemoveAll(bool slay)
  {
    foreach (var userId in _bhop.Keys.ToList())
    {
      var player = Util.FromUserId(userId);
      if (player != null)
        ExitBhop(player, slay);
      else
        _bhop.Remove(userId);
    }

    UpdateMembers();
  }

  private void CheckLastT()
  {
    if (CountRealAliveT() > 1)
      return;

    _roundClosed = true;

    if (ActiveCount == 0)
      return;

    RemoveAll(slay: true);
    Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.auto_closed"]}");
  }

  private int CountRealAliveT()
  {
    int count = 0;
    foreach (var p in Utilities.GetPlayers())
    {
      if (p == null || !p.IsValid || p.Team != CsTeam.Terrorist || _bhop.ContainsKey(Util.UserId(p)))
        continue;

      if (IsAlive(p))
        count++;
    }
    return count;
  }

  private void ResetState()
  {
    _bhop.Clear();
    _roundClosed = false;
    UpdateMembers();
  }

  private static void SetScoreboardDead(CCSPlayerController player, bool dead)
  {
    player.PawnIsAlive = !dead;
    NetState.Changed(player, "CCSPlayerController", "m_bPawnIsAlive");
  }

  private static bool IsAlive(CCSPlayerController player)
  {
    return player.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
  }

  private void LoadPoints()
  {
    try
    {
      if (!File.Exists(PointsPath))
      {
        _points = new(StringComparer.OrdinalIgnoreCase);
        return;
      }

      var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, GoBhopPoint>>>(File.ReadAllText(PointsPath));
      _points = new(StringComparer.OrdinalIgnoreCase);
      if (data != null)
        foreach (var kv in data)
          _points[kv.Key] = new(kv.Value, StringComparer.OrdinalIgnoreCase);
    }
    catch
    {
      _points = new(StringComparer.OrdinalIgnoreCase);
    }
  }

  private void SavePoints()
  {
    try
    {
      File.WriteAllText(PointsPath, JsonSerializer.Serialize(_points, new JsonSerializerOptions { WriteIndented = true }));
    }
    catch
    {
    }
  }
}
