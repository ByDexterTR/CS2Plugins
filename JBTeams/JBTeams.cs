using System.Drawing;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace JBTeams;

public class TeamEntry
{
  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  [JsonPropertyName("color")]
  public int[] TeamColor { get; set; } = [255, 255, 255];
}

public class JBTeamsConfig : BasePluginConfig
{
  [JsonPropertyName("teams_cmd")]
  public string TeamsCommands { get; set; } = "css_takim,css_team";

  [JsonPropertyName("teams_flag")]
  public string TeamsFlag { get; set; } = "@jailbreak/warden,@css/generic";

  [JsonPropertyName("teams")]
  public List<TeamEntry> Teams { get; set; } =
  [
    new TeamEntry { Name = "Ember", TeamColor = [255, 42, 42] },
    new TeamEntry { Name = "Abyss", TeamColor = [15, 82, 186] },
    new TeamEntry { Name = "Spark", TeamColor = [255, 215, 0] },
    new TeamEntry { Name = "Grove", TeamColor = [27, 138, 90] },
    new TeamEntry { Name = "Nebula", TeamColor = [106, 13, 173] }
  ];

  [JsonPropertyName("force_balance")]
  public bool ForceBalance { get; set; } = true;
}

public class JBTeams : BasePlugin, IPluginConfig<JBTeamsConfig>
{
  public override string ModuleName => "JBTeams";
  public override string ModuleVersion => "1.0.5";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public JBTeamsConfig Config { get; set; } = new();

  private int activeTeams = 0;
  private readonly Dictionary<ulong, int> playerTeams = new();

  private static readonly char[] ChatPalette =
  [
    '\x07', '\x0B', '\x10', '\x04', '\x0E', '\x09', '\x06', '\x0A'
  ];

  public void OnConfigParsed(JBTeamsConfig config)
  {
    config.Teams.RemoveAll(t => string.IsNullOrWhiteSpace(t.Name) || t.TeamColor.Length != 3);
    Config = config;
  }

  private Color TeamRenderColor(int teamIndex)
  {
    var c = Config.Teams[teamIndex].TeamColor;
    return Color.FromArgb(255, c[0], c[1], c[2]);
  }

  public override void Load(bool hotReload)
  {
    RegisterListener<OnEntityTakeDamagePre>(HandleEntityDamage);
    RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterEventHandler<EventRoundEnd>(OnRoundEnd);

    foreach (var name in Util.Split(Config.TeamsCommands))
      AddCommand(name, "Takim sistemi", OnTakimCommand);
  }

  public override void Unload(bool hotReload)
  {
    if (activeTeams > 0)
    {
      activeTeams = 0;
      playerTeams.Clear();
      ResetAllTerroristColors();
    }
  }

  private HookResult HandleEntityDamage(CEntityInstance victimEnt, CTakeDamageInfo info)
  {
    if (activeTeams == 0)
      return HookResult.Continue;

    CCSPlayerPawn? victimPawn = victimEnt as CCSPlayerPawn ?? new CCSPlayerPawn(victimEnt.Handle);
    CCSPlayerPawn? attackerPawn = info.Attacker.Value as CCSPlayerPawn;

    var victimController = victimPawn?.OriginalController.Value;
    var attackerController = attackerPawn?.OriginalController.Value;

    if (victimController == null || attackerController == null)
      return HookResult.Continue;

    if (victimController.SteamID == attackerController.SteamID)
      return HookResult.Continue;

    if (victimController.TeamNum != (int)CsTeam.Terrorist || attackerController.TeamNum != (int)CsTeam.Terrorist)
      return HookResult.Continue;

    if (playerTeams.TryGetValue(attackerController.SteamID, out int aTeam) &&
        playerTeams.TryGetValue(victimController.SteamID, out int vTeam) &&
        aTeam == vTeam)
    {
      info.Damage = 0f;
    }

    return HookResult.Continue;
  }

  public void OnTakimCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    if (!Util.HasAccess(player, Config.TeamsFlag))
      return;

    int maxTeams = Config.Teams.Count;

    if (info.ArgCount < 2)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbteams.usage", maxTeams]}");
      return;
    }

    if (!int.TryParse(info.ArgByIndex(1), out int teamCount) || teamCount < 0 || teamCount > maxTeams)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbteams.invalid_input", maxTeams]}");
      return;
    }

    if (teamCount <= 1)
    {
      DisableTeams(player);
      return;
    }

    var terrorists = Utilities.GetPlayers()
      .Where(p => p != null && p.IsValid && p.Team == CsTeam.Terrorist && IsAlive(p))
      .ToList();

    if (terrorists.Count == 0)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbteams.no_players"]}");
      return;
    }

    if (!Config.ForceBalance && terrorists.Count % teamCount != 0)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbteams.uneven", terrorists.Count, teamCount]}");
      return;
    }

    CreateTeams(player, terrorists, teamCount);
  }

  private void CreateTeams(CCSPlayerController admin, List<CCSPlayerController> terrorists, int teamCount)
  {
    activeTeams = teamCount;
    playerTeams.Clear();

    var shuffled = terrorists.OrderBy(x => Random.Shared.Next()).ToList();

    for (int i = 0; i < shuffled.Count; i++)
    {
      int teamIndex = i % teamCount;

      var p = shuffled[i];
      playerTeams[p.SteamID] = teamIndex;
      ApplyTeamColor(p, teamIndex);
      p.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbteams.team_assigned", GetColoredTeamName(teamIndex)]}");
    }

    Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbteams.teams_created", admin.PlayerName, teamCount]}");
  }

  private void DisableTeams(CCSPlayerController admin)
  {
    if (activeTeams == 0)
    {
      admin.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbteams.no_active"]}");
      return;
    }

    activeTeams = 0;
    playerTeams.Clear();

    ResetAllTerroristColors();

    Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbteams.teams_disabled", admin.PlayerName]}");
  }

  private void ApplyTeamColor(CCSPlayerController player, int teamIndex)
  {
    if (!player.IsValid || teamIndex < 0 || teamIndex >= Config.Teams.Count)
      return;

    var pawn = player.PlayerPawn.Value;
    if (pawn != null && pawn.IsValid)
    {
      pawn.Render = TeamRenderColor(teamIndex);
      Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }
  }

  private void ResetPlayerColor(CCSPlayerController player)
  {
    if (!player.IsValid)
      return;

    var pawn = player.PlayerPawn.Value;
    if (pawn != null && pawn.IsValid)
    {
      pawn.Render = Color.FromArgb(255, 255, 255, 255);
      Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }
  }

  private string GetColoredTeamName(int teamIndex)
  {
    if (teamIndex < 0 || teamIndex >= Config.Teams.Count)
      return "";

    var chatColor = ChatPalette[teamIndex % ChatPalette.Length];
    return $"{chatColor}{Config.Teams[teamIndex].Name}{CC.Default}";
  }

  private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid)
      return HookResult.Continue;

    ResetPlayerColor(player);
    playerTeams.Remove(player.SteamID);

    CheckTeamWin();

    return HookResult.Continue;
  }

  private void CheckTeamWin()
  {
    if (activeTeams == 0 || playerTeams.Count == 0)
      return;

    var remainingTeams = playerTeams.Values.Distinct().ToList();

    if (remainingTeams.Count == 1)
    {
      int winningTeam = remainingTeams[0];
      Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["jbteams.team_won", GetColoredTeamName(winningTeam)]}");

      activeTeams = 0;
      playerTeams.Clear();

      ResetAllTerroristColors();
    }
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    if (activeTeams > 0)
    {
      activeTeams = 0;
      playerTeams.Clear();

      ResetAllTerroristColors();
    }

    return HookResult.Continue;
  }

  private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
  {
    if (activeTeams > 0)
    {
      activeTeams = 0;
      playerTeams.Clear();

      ResetAllTerroristColors();
    }

    return HookResult.Continue;
  }

  private void ResetAllTerroristColors()
  {
    foreach (var p in Utilities.GetPlayers())
    {
      if (p != null && p.IsValid && p.TeamNum == (int)CsTeam.Terrorist)
      {
        ResetPlayerColor(p);
      }
    }
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid)
      return HookResult.Continue;

    playerTeams.Remove(player.SteamID);

    return HookResult.Continue;
  }

  private static bool IsAlive(CCSPlayerController? player)
  {
    return player?.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
  }
}
