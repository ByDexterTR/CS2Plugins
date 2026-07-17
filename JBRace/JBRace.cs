using System.Drawing;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace JBRace;

public class JBRaceConfig : BasePluginConfig
{
  [JsonPropertyName("race_cmd")]
  public string RaceCommands { get; set; } = "css_race,css_yaris";

  [JsonPropertyName("race_flag")]
  public string RaceFlag { get; set; } = "@jailbreak/warden,@css/generic";

  [JsonPropertyName("race_model")]
  public string RaceModel { get; set; } = "models/coop/challenge_coin.vmdl";

  [JsonPropertyName("race_countdown")]
  public int RaceCountdown { get; set; } = 3;
}

public class JBRace : BasePlugin, IPluginConfig<JBRaceConfig>
{
  public override string ModuleName => "JBRace";
  public override string ModuleVersion => "1.0.6";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  private Vector? _startPos;
  private QAngle _startAngle = new(0, 0, 0);
  private Vector? _finishPos;
  private readonly float _finishRadius = 64f;
  private int _winnerTarget = 1;
  private bool _raceActive = false;
  private ulong _winnerInputSteamId = 0;

  private CDynamicProp? _finishModel;
  private CBeam? _finishBeam;
  private CounterStrikeSharp.API.Modules.Timers.Timer? _countdownTimer = null;

  private readonly HashSet<ulong> _winners = new();
  private DateTime? _raceStartTime = null;
  private bool _showHud = false;
  private string _hudHtml = "";

  private bool _coinModelPrecached;

  public JBRaceConfig Config { get; set; } = new();

  private WasdMenuManager _menus = null!;

  public void OnConfigParsed(JBRaceConfig config)
  {
    Config = config;
    if (config.RaceCountdown < 1)
      config.RaceCountdown = 1;
  }

  public override void Load(bool hotReload)
  {
    _menus = new WasdMenuManager(this,
      () => Localizer["menu.scroll"],
      () => Localizer["menu.select"],
      () => Localizer["menu.exit"]);
    RegisterListener<OnTick>(OnTickCheckFinish);
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);
    AddCommandListener("say", OnPlayerChatHandler);
    AddCommandListener("say_team", OnPlayerChatHandler);

    foreach (var name in Util.Split(Config.RaceCommands))
      AddCommand(name, "Yaris menusunu acar", OnRaceCommand);
  }

  public override void Unload(bool hotReload)
  {
    _menus.Clear();
    ResetRace();
  }

  private void OnServerPrecacheResources(ResourceManifest res)
  {
    if (string.IsNullOrWhiteSpace(Config.RaceModel))
      return;

    res.AddResource(Config.RaceModel);
    _coinModelPrecached = true;
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    ResetRace();
    return HookResult.Continue;
  }

  public void OnRaceCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    if (!Util.HasAccess(player, Config.RaceFlag))
      return;

    ShowRaceMenu(player);
  }

  public HookResult OnPlayerChatHandler(CCSPlayerController? player, CommandInfo message)
  {
    if (player == null || !player.IsValid || _winnerInputSteamId == 0 || player.SteamID != _winnerInputSteamId)
      return HookResult.Continue;

    var text = message.ArgString.Trim().Trim('"');
    if (int.TryParse(text, out var n) && n >= 1)
    {
      _winnerTarget = n;
      _winnerInputSteamId = 0;
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbrace.winner_count_set", _winnerTarget]}");
      Server.NextFrame(() => ShowRaceMenu(player));
      return HookResult.Handled;
    }
    return HookResult.Continue;
  }

  private void ShowRaceMenu(CCSPlayerController player)
  {
    var items = new List<WasdItem>();

    if (!_raceActive)
    {
      items.Add(new WasdItem
      {
        Text = Localizer["jbrace.menu_start_race"],
        OnSelect = p =>
        {
          if (!ValidateCanStart(p))
            return;

          _winners.Clear();
          _menus.Close(p);
          StartRaceCountdown();
          Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbrace.started", p.PlayerName]}");
        }
      });
    }
    else
    {
      items.Add(new WasdItem
      {
        Text = Localizer["jbrace.menu_cancel_race"],
        OnSelect = p =>
        {
          ResetRace();
          Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbrace.cancelled", p.PlayerName]}");
          _menus.Close(p);
        }
      });
    }

    items.Add(new WasdItem
    {
      Text = Localizer["jbrace.menu_set_start"],
      OnSelect = p =>
      {
        var pos = p.PlayerPawn.Value?.AbsOrigin;
        var ang = p.PlayerPawn.Value?.EyeAngles ?? new QAngle(0, 0, 0);
        if (pos != null)
        {
          _startPos = new Vector(pos.X, pos.Y, pos.Z);
          _startAngle = new QAngle(0, ang.Y, 0);
          p.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbrace.start_set"]}");
        }
      }
    });

    items.Add(new WasdItem
    {
      Text = Localizer["jbrace.menu_set_finish"],
      OnSelect = p =>
      {
        var pos = p.PlayerPawn.Value?.AbsOrigin;
        if (pos != null)
        {
          _finishPos = new Vector(pos.X, pos.Y, pos.Z);
          SpawnFinishMarker();
          p.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbrace.finish_set"]}");
        }
      }
    });

    items.Add(new WasdItem
    {
      Text = Localizer["jbrace.winner_count", _winnerTarget],
      OnSelect = p =>
      {
        _winnerInputSteamId = p.SteamID;
        p.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbrace.enter_winner_count"]}");
        _menus.Close(p);
      }
    });

    items.Add(new WasdItem
    {
      Text = Localizer["jbrace.menu_clear_markers"],
      OnSelect = p =>
      {
        RemoveFinishMarker();
        _finishPos = null;
        p.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbrace.markers_cleared"]}");
      }
    });

    _menus.Open(player, Localizer["jbrace.menu_title"], items);
  }

  private bool ValidateCanStart(CCSPlayerController? player)
  {
    if (_startPos == null)
    {
      player?.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbrace.no_start"]}");
      return false;
    }
    if (_finishPos == null)
    {
      player?.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbrace.no_finish"]}");
      return false;
    }
    if (_winnerTarget < 1)
    {
      _winnerTarget = 1;
    }
    SpawnFinishMarker();
    return true;
  }

  private void StartRaceCountdown()
  {
    if (_startPos == null) return;

    var tPlayers = Utilities.GetPlayers()
      .Where(p => p != null && p.IsValid && p.PawnIsAlive && p.Team == CsTeam.Terrorist)
      .ToList();

    if (tPlayers.Count == 0)
    {
      Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbrace.no_terrorists"]}");
      return;
    }

    foreach (var pl in tPlayers)
    {
      var pawn = pl.PlayerPawn?.Value;
      if (pawn == null) continue;
      pawn.Teleport(_startPos, _startAngle, Vector.Zero);
      Freeze(pawn);
    }

    int countdown = Config.RaceCountdown;
    _countdownTimer?.Kill();
    _countdownTimer = AddTimer(1.0f, () =>
    {
      if (countdown > 0)
      {
        Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbrace.countdown", countdown]}");
        _showHud = true;
        _hudHtml = $"<font color='#bab9be' class='fontSize-l'><img src='https://raw.githubusercontent.com/ByDexterTR/CS2Plugins/refs/heads/main/img/flag.png'> {Localizer["jbrace.hud_countdown", countdown]}</font>";
        countdown--;
      }
      else
      {
        _raceActive = true;
        _raceStartTime = DateTime.Now;

        foreach (var pl in tPlayers)
        {
          if (!pl.IsValid || !pl.PawnIsAlive) continue;

          var pawn = pl.PlayerPawn?.Value;
          if (pawn != null)
          {
            Unfreeze(pawn);
            pawn.Render = Color.FromArgb(255, 255, 0, 0);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
          }
        }

        Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbrace.race_started"]}");
        _showHud = false;

        _countdownTimer?.Kill();
        _countdownTimer = null;
      }
    }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
  }

  private void OnTickCheckFinish()
  {
    if (_showHud)
    {
      foreach (var p in Utilities.GetPlayers())
      {
        if (p != null && p.IsValid)
          p.PrintToCenterHtml(_hudHtml);
      }
    }

    if (!_raceActive || _finishPos == null) return;

    var alivePlayers = Utilities.GetPlayers()
      .Where(p => p != null && p.IsValid && p.PawnIsAlive && p.Team == CsTeam.Terrorist)
      .ToList();

    foreach (var pl in alivePlayers)
    {
      if (_winners.Contains(pl.SteamID)) continue;

      var pos = pl.PlayerPawn?.Value?.AbsOrigin;
      if (pos == null) continue;

      if (CalculateDistance(new Vector(pos.X, pos.Y, pos.Z), _finishPos) <= _finishRadius)
      {
        _winners.Add(pl.SteamID);
        var secs = _raceStartTime.HasValue ? (DateTime.Now - _raceStartTime.Value).TotalSeconds : 0.0;
        Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbrace.winner", _winners.Count, pl.PlayerName]}");

        var pawn = pl.PlayerPawn?.Value;
        if (pawn != null)
        {
          pawn.Render = Color.FromArgb(255, 0, 255, 0);
          Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }
      }
    }

    if (_winners.Count >= _winnerTarget)
    {
      foreach (var pl in alivePlayers)
      {
        if (!_winners.Contains(pl.SteamID))
          pl.PlayerPawn.Value?.CommitSuicide(false, true);
      }

      ResetRace();
    }
  }

  private static float CalculateDistance(Vector v1, Vector v2)
  {
    var dx = v1.X - v2.X;
    var dy = v1.Y - v2.Y;
    var dz = v1.Z - v2.Z;
    return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
  }

  private void SpawnFinishMarker()
  {
    RemoveFinishMarker();
    if (_finishPos == null) return;

    if (_coinModelPrecached)
    {
      var model = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
      if (model != null)
      {
        var pos = new Vector(_finishPos.X, _finishPos.Y, _finishPos.Z + 48f);
        model.DispatchSpawn();
        model.SetModel(Config.RaceModel);
        Server.NextWorldUpdate(() => model.AcceptInput("SetAnimation", value: "challenge_coin_idle"));
        model.Teleport(pos, new QAngle(0, 0, 0), Vector.Zero);
        _finishModel = model;
      }
    }

    try
    {
      var beam = Utilities.CreateEntityByName<CBeam>("beam");
      if (beam != null)
      {
        beam.Render = Color.FromArgb(255, 0, 255, 0);
        beam.Width = 1.5f;
        beam.Teleport(new Vector(_finishPos.X, _finishPos.Y, _finishPos.Z), new QAngle(0, 0, 0), Vector.Zero);
        beam.EndPos.X = _finishPos.X;
        beam.EndPos.Y = _finishPos.Y;
        beam.EndPos.Z = _finishPos.Z + 10000f;
        beam.DispatchSpawn();
        _finishBeam = beam;
      }
    }
    catch
    {
    }
  }

  private void RemoveFinishMarker()
  {
    try { _finishModel?.Remove(); } catch { }
    _finishModel = null;

    try { _finishBeam?.Remove(); } catch { }
    _finishBeam = null;
  }

  private void ResetRace()
  {
    _raceActive = false;
    _winners.Clear();
    _winnerInputSteamId = 0;
    _countdownTimer?.Kill();
    _countdownTimer = null;
    _raceStartTime = null;
    _showHud = false;

    foreach (var p in Utilities.GetPlayers())
    {
      if (p == null || !p.IsValid) continue;

      if (p.PawnIsAlive && p.PlayerPawn?.Value != null)
      {
        var pawn = p.PlayerPawn.Value;
        Unfreeze(pawn);
        pawn.Render = Color.FromArgb(255, 255, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
      }
    }

    RemoveFinishMarker();
  }

  private static void Freeze(CBasePlayerPawn pawn)
  {
    pawn.MoveType = MoveType_t.MOVETYPE_INVALID;
    Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 11);
    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
  }

  private static void Unfreeze(CBasePlayerPawn pawn)
  {
    pawn.MoveType = MoveType_t.MOVETYPE_WALK;
    Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 2);
    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
  }
}
