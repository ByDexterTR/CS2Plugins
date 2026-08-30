using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace TeamShuffle;

public class TeamShuffleConfig : BasePluginConfig
{
  [JsonPropertyName("shuffle_mode")]
  public string ShuffleMode { get; set; } = "streak";

  [JsonPropertyName("shuffle_streak_round")]
  public int ShuffleStreakRound { get; set; } = 3;

  [JsonPropertyName("shuffle_interval_round")]
  public int ShuffleIntervalRound { get; set; } = 5;

  [JsonPropertyName("shuffle_cmd")]
  public string ShuffleCommands { get; set; } = "css_shuffle,css_karistir";

  [JsonPropertyName("shuffle_cmd_flag")]
  public string ShuffleCommandFlag { get; set; } = "@css/generic,@css/ban";

  [JsonPropertyName("shuffle_power_cmd")]
  public string ShufflePowerCommands { get; set; } = "css_power,css_guc";

  [JsonPropertyName("shuffle_power_flag")]
  public string ShufflePowerFlag { get; set; } = "@css/generic,@css/ban";

  [JsonPropertyName("disable_valve_balance")]
  public bool DisableValveBalance { get; set; } = true;

  [JsonPropertyName("disable_changeteam")]
  public bool DisableChangeTeam { get; set; } = true;

  [JsonPropertyName("disable_select_spec")]
  public bool DisableSelectSpec { get; set; } = true;

  [JsonPropertyName("shuffle_spec_immune_flag")]
  public string ShuffleSpecImmuneFlag { get; set; } = "@css/ban";

  [JsonPropertyName("shuffle_min_players")]
  public int ShuffleMinPlayers { get; set; } = 4;

  [JsonPropertyName("shuffle_limitteams")]
  public int ShuffleLimitTeams { get; set; } = 2;

  [JsonPropertyName("reset_on_map_change")]
  public bool ResetOnMapChange { get; set; } = true;

  [JsonPropertyName("shuffle_damage_rating")]
  public int ShuffleDamageRating { get; set; } = 1;

  [JsonPropertyName("shuffle_kill_rating")]
  public int ShuffleKillRating { get; set; } = 50;

  [JsonPropertyName("shuffle_mvp_rating")]
  public int ShuffleMvpRating { get; set; } = 25;

  [JsonPropertyName("shuffle_balance_tolerance")]
  public int ShuffleBalanceTolerance { get; set; } = 10;

  [JsonPropertyName("shuffle_announce")]
  public bool ShuffleAnnounce { get; set; } = true;
}

public class PlayerStats
{
  public float Damage;
  public int Kills;
  public int Mvps;
  public int Rounds;

  public float RoundDamage;
  public int RoundKills;
  public int RoundMvps;
  public bool Played;
}

public class TeamShuffle : BasePlugin, IPluginConfig<TeamShuffleConfig>
{
  public override string ModuleName => "TeamShuffle";
  public override string ModuleVersion => "1.0.1";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private const string BlockSound = "EndMatch.ItemRevealSingle";

  private string ChatPrefix => Localizer["chat_prefix"];

  public TeamShuffleConfig Config { get; set; } = new();

  private readonly Dictionary<ulong, PlayerStats> stats = new();
  private readonly Dictionary<int, int> health = new();
  private readonly Dictionary<int, CsTeam> planned = new();
  private int lastWinner;
  private int winStreak;
  private int roundsSinceShuffle;
  private bool pending;
  private bool wasWarmup = true;
  private bool pistolRound;
  private CCSGameRulesProxy? gameRulesProxy;
  private ConVar? cvHalftime;
  private ConVar? cvMaxRounds;

  public void OnConfigParsed(TeamShuffleConfig config)
  {
    config.ShuffleStreakRound = Math.Max(1, config.ShuffleStreakRound);
    config.ShuffleIntervalRound = Math.Max(1, config.ShuffleIntervalRound);
    config.ShuffleMinPlayers = Math.Max(2, config.ShuffleMinPlayers);
    config.ShuffleLimitTeams = Math.Max(2, config.ShuffleLimitTeams);
    config.ShuffleDamageRating = Math.Max(1, config.ShuffleDamageRating);
    config.ShuffleKillRating = Math.Max(1, config.ShuffleKillRating);
    config.ShuffleMvpRating = Math.Max(1, config.ShuffleMvpRating);
    config.ShuffleBalanceTolerance = Math.Max(0, config.ShuffleBalanceTolerance);
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
    RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    RegisterEventHandler<EventRoundMvp>(OnRoundMvp);
    RegisterEventHandler<EventWarmupEnd>(OnWarmupEnd);
    RegisterEventHandler<EventRoundPrestart>(OnRoundPrestart);
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
    RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
    AddCommandListener("jointeam", OnJoinTeam);
    RegisterListener<OnMapStart>(OnMapStarted);

    foreach (var name in Util.Split(Config.ShuffleCommands))
      AddCommand(name, "Takim karistirma", OnShuffleCommand);

    foreach (var name in Util.Split(Config.ShufflePowerCommands))
      AddCommand(name, "Takim gucu", OnPowerCommand);

    ApplyConVars();
  }

  private void OnMapStarted(string mapName)
  {
    health.Clear();
    planned.Clear();

    if (Config.ResetOnMapChange)
      stats.Clear();

    lastWinner = 0;
    winStreak = 0;
    roundsSinceShuffle = 0;
    pending = false;
    wasWarmup = true;

    AddTimer(2.0f, ApplyConVars);
  }

  private void ApplyConVars()
  {
    if (Config.DisableValveBalance)
    {
      Server.ExecuteCommand("mp_autoteambalance 0");
      Server.ExecuteCommand("mp_limitteams 0");
    }
  }

  private CCSGameRules? GameRules()
  {
    if (gameRulesProxy == null || !gameRulesProxy.IsValid)
      gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();

    return gameRulesProxy?.GameRules;
  }

  private bool IsWarmup() => GameRules()?.WarmupPeriod == true;

  private bool IsPistolRound()
  {
    var rules = GameRules();
    if (rules == null)
      return false;

    cvHalftime ??= ConVar.Find("mp_halftime");
    cvMaxRounds ??= ConVar.Find("mp_maxrounds");

    bool halftime = cvHalftime?.GetPrimitiveValue<bool>() ?? false;
    int maxRounds = cvMaxRounds?.GetPrimitiveValue<int>() ?? 0;

    return rules.TotalRoundsPlayed == 0
      || (halftime && maxRounds / 2 == rules.TotalRoundsPlayed)
      || rules.GameRestart;
  }

  private bool BelowMinPlayers() =>
    Utilities.GetPlayers().Count(IsPlaying) < Config.ShuffleMinPlayers;

  private static bool IsPlaying(CCSPlayerController? player) =>
    player != null && player.IsValid && !player.IsBot && !player.IsHLTV
    && (player.Team == CsTeam.Terrorist || player.Team == CsTeam.CounterTerrorist);

  private PlayerStats? GetStats(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
      return null;

    ulong key = Util.SteamId(player);
    if (key == 0UL)
      return null;

    if (!stats.TryGetValue(key, out var entry))
    {
      entry = new PlayerStats();
      stats[key] = entry;
    }

    return entry;
  }

  private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
  {
    if (IsWarmup())
      return HookResult.Continue;

    var attacker = @event.Attacker;
    var victim = @event.Userid;

    if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid)
      return HookResult.Continue;

    if (attacker.Handle == victim.Handle || attacker.TeamNum == victim.TeamNum)
      return HookResult.Continue;

    var entry = GetStats(attacker);
    if (entry != null)
      entry.RoundDamage += AppliedDamage(victim, @event.Health, @event.DmgHealth);

    return HookResult.Continue;
  }

  private int AppliedDamage(CCSPlayerController victim, int healthAfter, int reportedDamage)
  {
    int victimId = Util.UserId(victim);
    healthAfter = Math.Max(0, healthAfter);
    reportedDamage = Math.Max(0, reportedDamage);

    int maxHealth = victim.PlayerPawn.Value?.MaxHealth ?? 100;
    if (maxHealth <= 0)
      maxHealth = 100;

    int healthBefore = health.TryGetValue(victimId, out int cached) && cached > healthAfter
      ? cached
      : Math.Min(healthAfter + reportedDamage, maxHealth);

    if (victimId >= 0)
      health[victimId] = healthAfter;

    return Math.Clamp(healthBefore - healthAfter, 0, reportedDamage);
  }

  private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
  {
    int userId = Util.UserId(@event.Userid);
    if (userId < 0)
      return HookResult.Continue;

    AddTimer(0.2f, () =>
    {
      var player = Util.FromUserId(userId);
      var pawn = player?.PlayerPawn.Value;

      if (pawn != null && pawn.IsValid)
        health[userId] = pawn.Health;
    });

    return HookResult.Continue;
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    int userId = Util.UserId(@event.Userid);
    if (userId >= 0)
      health.Remove(userId);

    return HookResult.Continue;
  }

  private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
  {
    int victimId = Util.UserId(@event.Userid);
    if (victimId >= 0)
      health[victimId] = 0;

    if (IsWarmup())
      return HookResult.Continue;

    var attacker = @event.Attacker;
    var victim = @event.Userid;

    if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid)
      return HookResult.Continue;

    if (attacker.Handle == victim.Handle || attacker.TeamNum == victim.TeamNum)
      return HookResult.Continue;

    var entry = GetStats(attacker);
    if (entry != null)
      entry.RoundKills++;

    return HookResult.Continue;
  }

  private HookResult OnRoundMvp(EventRoundMvp @event, GameEventInfo info)
  {
    if (IsWarmup())
      return HookResult.Continue;

    CCSPlayerController? mvp;

    try
    {
      mvp = @event.Userid;
    }
    catch (NativeException)
    {
      return HookResult.Continue;
    }

    var entry = GetStats(mvp);
    if (entry != null)
      entry.RoundMvps++;

    return HookResult.Continue;
  }

  private HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo info)
  {
    LeaveWarmup();
    return HookResult.Continue;
  }

  private void LeaveWarmup()
  {
    wasWarmup = false;

    ResetCounters();
    pending = false;
    planned.Clear();

    Equalize();
    ApplyPlanned();
  }

  private HookResult OnRoundPrestart(EventRoundPrestart @event, GameEventInfo info)
  {
    if (IsWarmup())
    {
      planned.Clear();
      return HookResult.Continue;
    }

    ApplyPlanned();
    Equalize();
    ApplyPlanned();

    return HookResult.Continue;
  }

  private void ApplyPlanned()
  {
    if (planned.Count == 0)
      return;

    foreach (var (userId, team) in planned)
    {
      var player = Util.FromUserId(userId);
      if (!IsPlaying(player) || player!.Team == team)
        continue;

      player.SwitchTeam(team);
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["teamshuffle.moved", TeamName(team)]}");
    }

    planned.Clear();
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    foreach (var entry in stats.Values)
    {
      entry.RoundDamage = 0f;
      entry.RoundKills = 0;
      entry.RoundMvps = 0;
      entry.Played = false;
    }

    if (IsWarmup())
    {
      wasWarmup = true;
      return HookResult.Continue;
    }

    pistolRound = IsPistolRound();

    if (wasWarmup)
      LeaveWarmup();

    foreach (var player in Utilities.GetPlayers())
    {
      var entry = IsPlaying(player) ? GetStats(player) : null;
      if (entry != null)
        entry.Played = true;
    }

    return HookResult.Continue;
  }

  private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
  {
    if (IsWarmup())
      return HookResult.Continue;

    CommitRound();

    int winner = @event.Winner;
    if (!pistolRound && (winner == (int)CsTeam.Terrorist || winner == (int)CsTeam.CounterTerrorist))
    {
      if (winner == lastWinner)
        winStreak++;
      else
      {
        lastWinner = winner;
        winStreak = 1;
      }
    }

    roundsSinceShuffle++;

    string mode = Config.ShuffleMode.Trim().ToLowerInvariant();
    bool trigger = pending;
    string reason = Localizer["teamshuffle.reason_manual"];

    if (pistolRound)
      trigger = false;

    if (!trigger && mode == "streak" && winStreak >= Config.ShuffleStreakRound)
    {
      trigger = true;
      reason = Localizer["teamshuffle.reason_streak", winStreak];
    }

    if (!trigger && (mode == "interval" || mode == "auto") && roundsSinceShuffle >= Config.ShuffleIntervalRound)
    {
      trigger = true;
      reason = Localizer["teamshuffle.reason_interval", roundsSinceShuffle];
    }

    if (!trigger)
      return HookResult.Continue;

    pending = false;

    if (Shuffle(reason))
      ResetCounters();

    return HookResult.Continue;
  }

  private void ResetCounters()
  {
    winStreak = 0;
    lastWinner = 0;
    roundsSinceShuffle = 0;
  }

  private void CommitRound()
  {
    foreach (var entry in stats.Values)
    {
      if (!entry.Played)
        continue;

      entry.Damage += entry.RoundDamage;
      entry.Kills += entry.RoundKills;
      entry.Mvps += entry.RoundMvps;
      entry.Rounds++;

      entry.RoundDamage = 0f;
      entry.RoundKills = 0;
      entry.RoundMvps = 0;
      entry.Played = false;
    }
  }

  private float RatingOf(CCSPlayerController player)
  {
    ulong key = Util.SteamId(player);
    if (key == 0UL || !stats.TryGetValue(key, out var entry) || entry.Rounds == 0)
      return -1f;

    return entry.Damage / entry.Rounds * Config.ShuffleDamageRating
      + (float)entry.Kills / entry.Rounds * Config.ShuffleKillRating
      + (float)entry.Mvps / entry.Rounds * Config.ShuffleMvpRating;
  }

  public void OnShuffleCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (!Util.HasAccess(player, Config.ShuffleCommandFlag))
    {
      Reply(player, info, Localizer["teamshuffle.no_access"]);
      return;
    }

    if (IsWarmup())
    {
      Reply(player, info, Localizer["teamshuffle.warmup"]);
      return;
    }

    int count = Utilities.GetPlayers().Count(IsPlaying);
    if (count < Config.ShuffleMinPlayers || count < 2)
    {
      Reply(player, info, Localizer["teamshuffle.not_enough", Config.ShuffleMinPlayers]);
      return;
    }

    pending = true;
    Reply(player, info, Localizer["teamshuffle.queued"]);
  }

  public void OnPowerCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (!Util.HasAccess(player, Config.ShufflePowerFlag))
    {
      Reply(player, info, Localizer["teamshuffle.no_access"]);
      return;
    }

    var entries = Rated(Utilities.GetPlayers().Where(IsPlaying).ToList());

    float ctPower = entries.Where(e => e.Player.Team == CsTeam.CounterTerrorist).Sum(e => e.Rating);
    float tPower = entries.Where(e => e.Player.Team == CsTeam.Terrorist).Sum(e => e.Rating);
    int ctCount = entries.Count(e => e.Player.Team == CsTeam.CounterTerrorist);
    int tCount = entries.Count - ctCount;

    Reply(player, info, Localizer["teamshuffle.power_command",
      (int)Math.Round(ctPower), ctCount, (int)Math.Round(tPower), tCount]);
  }

  private void Reply(CCSPlayerController? player, CommandInfo info, string message)
  {
    if (player != null && player.IsValid)
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {message}");
    else
      info.ReplyToCommand($"[{ChatPrefix}] {message}");
  }

  private bool Shuffle(string reason)
  {
    var players = Utilities.GetPlayers().Where(IsPlaying).ToList();

    if (players.Count < Config.ShuffleMinPlayers || players.Count < 2)
      return false;

    var entries = Rated(players);

    if (AlreadyBalanced(entries))
    {
      if (Config.ShuffleAnnounce)
        Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["teamshuffle.already_balanced"]}");

      return true;
    }

    List<CCSPlayerController> groupA = [];
    List<CCSPlayerController> groupB = [];
    float sumA = 0f;
    float sumB = 0f;

    int total = entries.Count;
    int capA = (total + 1) / 2;
    int capB = total / 2;

    foreach (var entry in entries)
    {
      bool toA = groupB.Count >= capB || (groupA.Count < capA && sumA <= sumB);

      if (toA)
      {
        groupA.Add(entry.Player);
        sumA += entry.Rating;
      }
      else
      {
        groupB.Add(entry.Player);
        sumB += entry.Rating;
      }
    }

    int keepA = groupA.Count(p => p.Team == CsTeam.CounterTerrorist) + groupB.Count(p => p.Team == CsTeam.Terrorist);
    int keepB = groupA.Count(p => p.Team == CsTeam.Terrorist) + groupB.Count(p => p.Team == CsTeam.CounterTerrorist);

    var teamA = keepA >= keepB ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
    var teamB = teamA == CsTeam.CounterTerrorist ? CsTeam.Terrorist : CsTeam.CounterTerrorist;

    planned.Clear();
    int moved = Plan(groupA, teamA) + Plan(groupB, teamB);

    float ctPower = teamA == CsTeam.CounterTerrorist ? sumA : sumB;
    float tPower = teamA == CsTeam.CounterTerrorist ? sumB : sumA;

    if (Config.ShuffleAnnounce)
    {
      Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["teamshuffle.shuffled", reason, moved]}");
      Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["teamshuffle.power", (int)Math.Round(ctPower), (int)Math.Round(tPower)]}");
    }

    return true;
  }

  private List<(CCSPlayerController Player, float Rating)> Rated(List<CCSPlayerController> players)
  {
    var rated = players.Select(p => (Player: p, Rating: RatingOf(p))).ToList();
    var known = rated.Where(e => e.Rating >= 0f).Select(e => e.Rating).ToList();
    float fallback = known.Count > 0 ? known.Average() : 0f;

    return rated
      .Select(e => (e.Player, Rating: e.Rating >= 0f ? e.Rating : fallback))
      .OrderByDescending(e => e.Rating)
      .ThenBy(_ => Random.Shared.Next())
      .ToList();
  }

  private bool AlreadyBalanced(List<(CCSPlayerController Player, float Rating)> entries)
  {
    float ct = entries.Where(e => e.Player.Team == CsTeam.CounterTerrorist).Sum(e => e.Rating);
    float t = entries.Where(e => e.Player.Team == CsTeam.Terrorist).Sum(e => e.Rating);
    int ctCount = entries.Count(e => e.Player.Team == CsTeam.CounterTerrorist);
    int tCount = entries.Count - ctCount;

    if (Math.Abs(ctCount - tCount) > 1)
      return false;

    float top = Math.Max(ct, t);
    if (top <= 0f)
      return true;

    return Math.Abs(ct - t) / top * 100f <= Config.ShuffleBalanceTolerance;
  }

  private int Plan(List<CCSPlayerController> group, CsTeam team)
  {
    int count = 0;

    foreach (var player in group)
    {
      if (!player.IsValid || player.Team == team)
        continue;

      int userId = Util.UserId(player);
      if (userId < 0)
        continue;

      planned[userId] = team;
      count++;
    }

    return count;
  }

  private void Equalize()
  {
    var players = Utilities.GetPlayers().Where(IsPlaying).ToList();

    if (players.Count < Config.ShuffleMinPlayers || players.Count < 2)
      return;

    var entries = Rated(players);

    var ct = entries.Where(e => e.Player.Team == CsTeam.CounterTerrorist).ToList();
    var t = entries.Where(e => e.Player.Team == CsTeam.Terrorist).ToList();

    if (Math.Abs(ct.Count - t.Count) < Config.ShuffleLimitTeams)
      return;

    var bigger = ct.Count > t.Count ? ct : t;
    var smaller = ct.Count > t.Count ? t : ct;
    var target = ct.Count > t.Count ? CsTeam.Terrorist : CsTeam.CounterTerrorist;

    float biggerPower = bigger.Sum(e => e.Rating);
    float smallerPower = smaller.Sum(e => e.Rating);

    int moves = (bigger.Count - smaller.Count) / 2;
    var pool = bigger.ToList();

    planned.Clear();

    for (int i = 0; i < moves && pool.Count > 0; i++)
    {
      var pick = pool
        .OrderBy(e => Math.Abs(biggerPower - e.Rating - (smallerPower + e.Rating)))
        .First();

      pool.Remove(pick);
      biggerPower -= pick.Rating;
      smallerPower += pick.Rating;

      int userId = Util.UserId(pick.Player);
      if (userId >= 0)
        planned[userId] = target;
    }

    if (planned.Count == 0)
      return;

    ResetCounters();

    if (Config.ShuffleAnnounce)
      Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["teamshuffle.equalized", planned.Count]}");
  }

  private static void PlayBlocked(CCSPlayerController player)
  {
    if (!player.IsValid || player.IsBot)
      return;

    player.EmitSound(BlockSound, new RecipientFilter(player), 0.8f);
  }

  private string TeamName(CsTeam team) => team == CsTeam.CounterTerrorist
    ? $"{CC.Blue}{Localizer["teamshuffle.team_ct"]}{CC.Default}"
    : $"{CC.Gold}{Localizer["teamshuffle.team_t"]}{CC.Default}";

  private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
  {
    if (!Config.DisableChangeTeam)
      return HookResult.Continue;

    int userId = Util.UserId(@event.Userid);
    if (userId < 0)
      return HookResult.Continue;

    AddTimer(1.0f, () =>
    {
      var player = Util.FromUserId(userId);
      if (player == null || player.IsBot || player.IsHLTV || IsWarmup() || BelowMinPlayers())
        return;

      if (player.Team == CsTeam.Terrorist || player.Team == CsTeam.CounterTerrorist)
        return;

      AutoAssign(player);
    });

    return HookResult.Continue;
  }

  public HookResult OnJoinTeam(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || player.IsBot || player.IsHLTV || IsWarmup() || BelowMinPlayers())
      return HookResult.Continue;

    if (!int.TryParse(info.GetArg(1), out int target))
      return HookResult.Continue;

    if (target == (int)CsTeam.Spectator || target == (int)CsTeam.None)
    {
      if (!Config.DisableSelectSpec || Util.HasAccess(player, Config.ShuffleSpecImmuneFlag))
        return HookResult.Continue;

      PlayBlocked(player);
      return HookResult.Handled;
    }

    if (!Config.DisableChangeTeam)
      return HookResult.Continue;

    if (player.Team == CsTeam.Terrorist || player.Team == CsTeam.CounterTerrorist)
    {
      PlayBlocked(player);
      return HookResult.Handled;
    }

    AutoAssign(player);
    return HookResult.Handled;
  }

  private void AutoAssign(CCSPlayerController player)
  {
    var others = Utilities.GetPlayers()
      .Where(p => IsPlaying(p) && p.Handle != player.Handle)
      .ToList();

    int ctCount = others.Count(p => p.Team == CsTeam.CounterTerrorist);
    int tCount = others.Count - ctCount;

    CsTeam team;

    if (ctCount != tCount)
      team = ctCount < tCount ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
    else
    {
      var entries = Rated(others);
      float ctPower = entries.Where(e => e.Player.Team == CsTeam.CounterTerrorist).Sum(e => e.Rating);
      float tPower = entries.Where(e => e.Player.Team == CsTeam.Terrorist).Sum(e => e.Rating);
      team = ctPower <= tPower ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
    }

    player.ChangeTeam(team);
    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["teamshuffle.auto_assigned", TeamName(team)]}");
  }
}
