using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
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
  public override string ModuleVersion => "1.0.1";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public GoBhopConfig Config { get; set; } = new();

  private string PointsPath => Path.Combine(ModuleDirectory, "positions.json");

  private Dictionary<string, Dictionary<string, GoBhopPoint>> _points = new(StringComparer.OrdinalIgnoreCase);
  private readonly Dictionary<int, bool> _bhop = new();
  private bool _adminClosed;
  private bool _roundClosed;

  private bool InBhop(int slot) => _bhop.TryGetValue(slot, out var pendingDeath) && !pendingDeath;

  private int ActiveCount => _bhop.Count(kv => !kv.Value);

  public void OnConfigParsed(GoBhopConfig config)
  {
    if (config.MinAliveT < 1)
      config.MinAliveT = 1;
    Config = config;
  }

  private WasdMenuManager _menus = null!;

  public override void Load(bool hotReload)
  {
    _menus = new WasdMenuManager(this,
      () => Localizer["menu.scroll"],
      () => Localizer["menu.select"],
      () => Localizer["menu.exit"]);
    LoadPoints();

    foreach (var name in Split(Config.Commands))
      AddCommand(name, "GoBhop menusunu acar", OnGoBhopCommand);

    foreach (var name in Split(Config.OnCommands))
      AddCommand(name, "GoBhop'a gitmeyi acar", OnOnBhopCommand);

    foreach (var name in Split(Config.OffCommands))
      AddCommand(name, "GoBhop'u kapatir ve icindekileri cikarir", OnOffBhopCommand);

    foreach (var name in Split(Config.SetCommands))
      AddCommand(name, "GoBhop noktasini kaydeder", OnSetCommand);

    foreach (var name in Split(Config.DelCommands))
      AddCommand(name, "Isimli GoBhop noktasini siler", OnDelCommand);

    foreach (var name in Split(Config.ResetCommands))
      AddCommand(name, "Haritanin kayitli GoBhop noktalarini siler", OnResetCommand);

    foreach (var name in Split(Config.BlockedCommands))
      AddCommandListener(name, OnBlockedCommand, HookMode.Pre);

    AddCommandListener("drop", OnDropCommand, HookMode.Pre);

    RegisterListener<OnMapStart>(_ =>
    {
      ResetState();
      LoadPoints();
    });

    RegisterListener<OnTick>(OnTick);
    RegisterListener<CheckTransmit>(OnCheckTransmit);

    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
    RegisterEventHandler<EventPlayerDeath>(OnPlayerDeathPre, HookMode.Pre);
    RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
    RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    RegisterEventHandler<EventItemPickup>(OnItemPickup);
    RegisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Pre);

    HookUserMessage(208, OnSoundEvent, HookMode.Pre);
  }

  public override void Unload(bool hotReload)
  {
    _menus.Clear();
    UnhookUserMessage(208, OnSoundEvent, HookMode.Pre);
    RemoveAll(slay: true);
  }

  private static string[] Split(string names) =>
    names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

  private void OnGoBhopCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    bool inBhop = InBhop(player.Slot);

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
          if (p.IsValid && InBhop(p.Slot))
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

          if (InBhop(p.Slot))
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
    if (InBhop(player.Slot))
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

    _bhop[player.Slot] = false;
    player.Respawn();
    SetupPawn(player, point);
    Server.NextFrame(() => SetupPawn(player, point));
    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.entered"]}");
  }

  private void OnOnBhopCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player != null && !AdminManager.PlayerHasPermissions(player, Config.AdminFlag))
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
    if (player != null && !AdminManager.PlayerHasPermissions(player, Config.AdminFlag))
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

    if (!AdminManager.PlayerHasPermissions(player, Config.SetFlag))
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.no_permission"]}");
      return;
    }

    string name = info.GetArg(1).Trim();
    if (name.Length == 0)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.set_usage", Split(Config.SetCommands)[0]]}");
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

    if (!AdminManager.PlayerHasPermissions(player, Config.SetFlag))
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.no_permission"]}");
      return;
    }

    string name = info.GetArg(1).Trim();
    if (name.Length == 0)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.set_usage", Split(Config.DelCommands)[0]]}");
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

    if (!AdminManager.PlayerHasPermissions(player, Config.SetFlag))
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
    if (player == null || !player.IsValid || !InBhop(player.Slot))
      return HookResult.Continue;

    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["gobhop.cmd_blocked"]}");
    return HookResult.Handled;
  }

  private HookResult OnDropCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || !InBhop(player.Slot))
      return HookResult.Continue;

    return HookResult.Handled;
  }

  private HookResult OnSoundEvent(UserMessage um)
  {
    if (_bhop.Count == 0)
      return HookResult.Continue;

    int entityIndex = um.ReadInt("source_entity_index");
    var entity = Utilities.GetEntityFromIndex<CBaseEntity>(entityIndex);
    if (entity == null || !entity.IsValid || entity.DesignerName != "player")
      return HookResult.Continue;

    var controller = entity.As<CCSPlayerPawn>().Controller.Value?.As<CCSPlayerController>();
    if (controller == null || !controller.IsValid || !InBhop(controller.Slot))
      return HookResult.Continue;

    for (int i = um.Recipients.Count - 1; i >= 0; i--)
    {
      var listener = um.Recipients[i];
      if (listener == null || !listener.IsValid || !InBhop(listener.Slot))
        um.Recipients.RemoveAt(i);
    }

    return um.Recipients.Count == 0 ? HookResult.Stop : HookResult.Continue;
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
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
    int slot = @event.Userid?.Slot ?? -1;
    if (_bhop.TryGetValue(slot, out var pendingDeath) && pendingDeath)
    {
      info.DontBroadcast = true;
      _bhop.Remove(slot);
    }
    return HookResult.Continue;
  }

  private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player != null && player.IsValid && InBhop(player.Slot))
      ExitBhop(player, slay: false);

    CheckLastT();
    return HookResult.Continue;
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    _bhop.Remove(@event.Userid?.Slot ?? -1);
    Server.NextFrame(CheckLastT);
    return HookResult.Continue;
  }

  private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player != null && player.IsValid && InBhop(player.Slot) && @event.Team != (byte)CsTeam.Terrorist)
      ExitBhop(player, slay: true);

    Server.NextFrame(CheckLastT);
    return HookResult.Continue;
  }

  private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || !InBhop(player.Slot))
      return HookResult.Continue;

    if (!_roundClosed && !_adminClosed)
      return HookResult.Continue;

    Server.NextFrame(() =>
    {
      if (player.IsValid && InBhop(player.Slot))
        ExitBhop(player, slay: true);
    });
    return HookResult.Continue;
  }

  private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || !InBhop(player.Slot))
      return HookResult.Continue;

    Server.NextFrame(() =>
    {
      if (player.IsValid && InBhop(player.Slot))
        player.RemoveWeapons();
    });
    return HookResult.Continue;
  }

  private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || !InBhop(player.Slot))
      return HookResult.Continue;

    info.DontBroadcast = true;
    player.RemoveWeapons();
    return HookResult.Continue;
  }

  private void OnTick()
  {
    if (_bhop.Count == 0)
      return;

    foreach (var kv in _bhop)
    {
      if (kv.Value)
        continue;

      var player = Utilities.GetPlayerFromSlot(kv.Key);
      if (player == null || !player.IsValid)
        continue;

      if (player.PawnIsAlive)
        SetScoreboardDead(player, dead: true);
    }
  }

  private void OnCheckTransmit(CCheckTransmitInfoList infoList)
  {
    if (_bhop.Count == 0)
      return;

    foreach ((CCheckTransmitInfo info, CCSPlayerController? viewer) in infoList)
    {
      bool viewerInBhop = viewer != null && viewer.IsValid && InBhop(viewer.Slot);

      foreach (var target in Utilities.GetPlayers())
      {
        if (target == null || !target.IsValid || (viewer != null && target.Slot == viewer.Slot))
          continue;

        if (InBhop(target.Slot) == viewerInBhop)
          continue;

        var pawn = target.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
          continue;

        info.TransmitEntities.Remove(pawn);

        if (pawn.WeaponServices == null)
          continue;

        foreach (var weapon in pawn.WeaponServices.MyWeapons)
        {
          if (weapon.Value != null && weapon.Value.IsValid)
            info.TransmitEntities.Remove(weapon.Value);
        }
      }
    }
  }

  private void SetupPawn(CCSPlayerController player, GoBhopPoint point)
  {
    if (!player.IsValid || !InBhop(player.Slot))
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
    if (!_bhop.ContainsKey(player.Slot))
      return;

    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid)
    {
      _bhop.Remove(player.Slot);
      return;
    }

    pawn.TakesDamage = true;

    if (pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
    {
      SetScoreboardDead(player, dead: false);
      if (slay)
      {
        _bhop[player.Slot] = true;
        pawn.CommitSuicide(false, true);
        return;
      }
    }

    _bhop.Remove(player.Slot);
  }

  private void RemoveAll(bool slay)
  {
    foreach (var slot in _bhop.Keys.ToList())
    {
      var player = Utilities.GetPlayerFromSlot(slot);
      if (player != null && player.IsValid)
        ExitBhop(player, slay);
      else
        _bhop.Remove(slot);
    }
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
      if (p == null || !p.IsValid || p.Team != CsTeam.Terrorist || _bhop.ContainsKey(p.Slot))
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
  }

  private static void SetScoreboardDead(CCSPlayerController player, bool dead)
  {
    player.PawnIsAlive = !dead;
    Utilities.SetStateChanged(player, "CCSPlayerController", "m_bPawnIsAlive");
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
