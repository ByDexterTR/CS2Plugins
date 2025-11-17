using System.Drawing;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace JBRace;

public class JBRaceConfig : BasePluginConfig
{
  [JsonPropertyName("chat_prefix")]
  public string ChatPrefix { get; set; } = "[ByDexter]";
}

public class JBRace : BasePlugin, IPluginConfig<JBRaceConfig>
{
  public override string ModuleName => "JBRace";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "JailBreak Yarış (Race)";

  private Vector? _startPos;
  private QAngle _startAngle = new(0, 0, 0);
  private Vector? _finishPos;
  private readonly float _finishRadius = 64f;
  private int _winnerTarget = 1;
  private bool _raceActive = false;
  private bool _waitingForWinnerInput = false;

  private CPhysicsPropOverride? _finishModel;
  private CBeam? _finishBeam;
  private CounterStrikeSharp.API.Modules.Timers.Timer? _countdownTimer = null;

  private readonly HashSet<ulong> _winners = new();
  private DateTime? _raceStartTime = null;
  private bool _showHud = false;
  private string _hudHtml = "";

  public JBRaceConfig Config { get; set; } = new();

  public override void Load(bool hotReload)
  {
    RegisterListener<OnTick>(OnTickCheckFinish);
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);
    AddCommandListener("say", OnPlayerChatHandler);
    AddCommandListener("say_team", OnPlayerChatHandler);
  }

  public void OnConfigParsed(JBRaceConfig config)
  {
    Config = config ?? new JBRaceConfig();
  }

  private void OnServerPrecacheResources(ResourceManifest res)
  {
    res.AddResource("models/coop/challenge_coin.vmdl");
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    ResetRace();
    return HookResult.Continue;
  }

  [ConsoleCommand("css_race", "Yarış menüsü")]
  [RequiresPermissionsOr("@css/generic", "@jailbreak/warden")]
  public void OnRaceCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    ShowRaceMenu(player);
  }

  public HookResult OnPlayerChatHandler(CCSPlayerController? player, CommandInfo message)
  {
    if (player == null || !player.IsValid || !_waitingForWinnerInput)
      return HookResult.Continue;

    var text = message.ArgString.Trim().Trim('"');
    if (int.TryParse(text, out var n) && n >= 1)
    {
      _winnerTarget = n;
      _waitingForWinnerInput = false;
      player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Kazanan sayısı {CC.Gold}{_winnerTarget}{CC.Default} olarak ayarlandı.");
      Server.NextFrame(() => ShowRaceMenu(player));
      return HookResult.Handled;
    }
    return HookResult.Continue;
  }

  private void ShowRaceMenu(CCSPlayerController player)
  {
    var menu = new CenterHtmlMenu("Race", this);

    if (!_raceActive)
    {
      menu.AddMenuOption("Yarışı Başlat", (p, o) =>
      {
        if (!ValidateCanStart(p))
        {
          ShowRaceMenu(player);
          return;
        }
        _winners.Clear();
        MenuManager.CloseActiveMenu(p!);
        StartRaceCountdown();
        Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{player?.PlayerName}{CC.Default}: Yarışı {CC.Green}başlattı{CC.Default}.");
      });
    }
    else
    {
      menu.AddMenuOption("Yarışı İptal Et", (p, o) =>
      {
        ResetRace();
        Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{player?.PlayerName}{CC.Default}: Yarışı {CC.Red}iptal etti{CC.Default}.");
        MenuManager.CloseActiveMenu(p!);
      });
    }

    menu.AddMenuOption("Başlangıç Ayarla", (p, o) =>
    {
      var pos = p?.PlayerPawn.Value?.AbsOrigin;
      var ang = p?.PlayerPawn.Value?.EyeAngles ?? new QAngle(0, 0, 0);
      if (pos != null)
      {
        _startPos = new Vector(pos.X, pos.Y, pos.Z);
        _startAngle = new QAngle(0, ang.Y, 0);
        p!.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Başlangıç {CC.Green}ayarlandı{CC.Default}.");
      }
      ShowRaceMenu(player);
    });

    menu.AddMenuOption("Bitiş Ayarla", (p, o) =>
    {
      var pos = p?.PlayerPawn.Value?.AbsOrigin;
      if (pos != null)
      {
        _finishPos = new Vector(pos.X, pos.Y, pos.Z);
        SpawnFinishMarker();
        p!.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Bitiş {CC.Green}ayarlandı{CC.Default}.");
      }
      ShowRaceMenu(player);
    });

    menu.AddMenuOption($"Kazanan Sayısı: {CC.Gold}{_winnerTarget}", (p, o) =>
    {
      _waitingForWinnerInput = true;
      p!.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Chate kazanan sayısı {CC.Gold}yazın{CC.Default}:");
      MenuManager.CloseActiveMenu(p);
    });

    menu.AddMenuOption("İşaretleri Temizle", (p, o) =>
    {
      RemoveFinishMarker();
      _finishPos = null;
      p!.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} İşaretler temizlendi.");
      ShowRaceMenu(player);
    });

    MenuManager.OpenCenterHtmlMenu(this, player, menu);
  }

  private bool ValidateCanStart(CCSPlayerController? player)
  {
    if (_startPos == null)
    {
      player?.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Önce {CC.Gold}başlangıç konumu{CC.Default} ayarlayın.");
      return false;
    }
    if (_finishPos == null)
    {
      player?.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Önce {CC.Gold}bitiş konumu{CC.Default} ayarlayın.");
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
      Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Yarış için {CC.Gold}T{CC.Default} bulunamadı!");
      return;
    }

    foreach (var pl in tPlayers)
    {
      var pawn = pl.PlayerPawn?.Value;
      if (pawn == null) continue;
      pawn.Teleport(_startPos, _startAngle, Vector.Zero);
      Freeze(pawn);
    }

    int countdown = 3;
    _countdownTimer?.Kill();
    _countdownTimer = AddTimer(1.0f, () =>
    {
      if (countdown > 0)
      {
        Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Yarış {CC.Gold}{countdown}{CC.Default} saniye sonra başlıyor...");
        _showHud = true;
        _hudHtml = $"<font color='#FFA500'>Yarış başlıyor</font>: {countdown}";
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

        Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Yarış {CC.Green}başladı{CC.Default}!");
        _showHud = false;

        foreach (var pl in Utilities.GetPlayers())
        {
          if (pl != null && pl.IsValid)
            pl.PrintToCenterHtml(" ");
        }

        _countdownTimer?.Kill();
        _countdownTimer = null;
      }
    }, TimerFlags.REPEAT);
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
        Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{pl.PlayerName}{CC.Default} yarışı {CC.Gold}{secs:0.00}{CC.Default} sn'de {CC.Green}bitirdi{CC.Default}! ({_winners.Count}/{_winnerTarget})");

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

      Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Hedef kazanan sayısına ulaşıldı! Kalan oyuncular {CC.Red}elendi{CC.Default}.");
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

    var model = Utilities.CreateEntityByName<CPhysicsPropOverride>("prop_physics_override");
    if (model != null)
    {
      var pos = new Vector(_finishPos.X, _finishPos.Y, _finishPos.Z + 32f);
      model.DispatchSpawn();
      model.SetModel("models/coop/challenge_coin.vmdl");
      try { model.AcceptInput("SetAnimation", value: "challenge_coin_idle"); } catch { }
      model.Teleport(pos, new QAngle(0, 0, 0), Vector.Zero);
      _finishModel = model;
    }

    try
    {
      var beam = Utilities.CreateEntityByName<CBeam>("beam");
      if (beam != null)
      {
        beam.Render = Color.FromArgb(255, 0, 255, 0);
        beam.Width = 3f;
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
    _waitingForWinnerInput = false;
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
}

