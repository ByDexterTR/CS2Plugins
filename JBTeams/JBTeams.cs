using System.Drawing;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace JBTeams;

public class JBTeamsConfig : BasePluginConfig
{
  [JsonPropertyName("chat_prefix")]
  public string ChatPrefix { get; set; } = "[ByDexter]";
}

public class JBTeams : BasePlugin, IPluginConfig<JBTeamsConfig>
{
  public override string ModuleName => "JBTeams";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "Jailbreak Takım Sistemi";

  public JBTeamsConfig Config { get; set; } = new JBTeamsConfig();

  private int activeTeams = 0;
  private readonly Dictionary<int, int> playerTeams = new();

  private readonly Color[] teamColors = new[]
  {
    Color.FromArgb(255, 255, 0, 0),
    Color.FromArgb(255, 0, 255, 0),
    Color.FromArgb(255, 0, 0, 255),
    Color.FromArgb(255, 255, 255, 0),
    Color.FromArgb(255, 255, 0, 255)
  };

  private readonly string[] teamNames = new[]
  {
    "Kırmızı",
    "Yeşil",
    "Mavi",
    "Sarı",
    "Magenta"
  };

  public void OnConfigParsed(JBTeamsConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
    RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
  }

  public override void Unload(bool hotReload)
  {
    try { VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre); } catch { }
  }

  public HookResult OnTakeDamage(DynamicHook h)
  {
    if (activeTeams == 0)
      return HookResult.Continue;

    var victimEnt = h.GetParam<CEntityInstance>(0);
    var info = h.GetParam<CTakeDamageInfo>(1);

    if (victimEnt == null || info == null)
      return HookResult.Continue;

    var attackerEnt = info.Attacker.Value;
    if (attackerEnt == null)
      return HookResult.Continue;

    CCSPlayerPawn? victimPawn = victimEnt as CCSPlayerPawn ?? new CCSPlayerPawn(victimEnt.Handle);
    CCSPlayerPawn? attackerPawn = attackerEnt as CCSPlayerPawn ?? new CCSPlayerPawn(attackerEnt.Handle);

    var victimController = victimPawn?.OriginalController.Value;
    var attackerController = attackerPawn?.OriginalController.Value;

    if (victimController == null || attackerController == null)
      return HookResult.Continue;

    if (!victimController.UserId.HasValue || !attackerController.UserId.HasValue)
      return HookResult.Continue;

    if (victimController.UserId.Value == attackerController.UserId.Value)
      return HookResult.Continue;

    if (victimController.TeamNum != (int)CsTeam.Terrorist || attackerController.TeamNum != (int)CsTeam.Terrorist)
      return HookResult.Continue;

    if (playerTeams.TryGetValue(attackerController.UserId.Value, out int aTeam) &&
        playerTeams.TryGetValue(victimController.UserId.Value, out int vTeam) &&
        aTeam == vTeam)
    {
      info.Damage = 0f;
      return HookResult.Changed;
    }

    return HookResult.Continue;
  }

  [ConsoleCommand("css_takim", "Takım sistemi")]
  [RequiresPermissions("@css/generic")]
  public void OnTakimCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    if (info.ArgCount < 2)
    {
      player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Kullanım: !takim <0-5>");
      return;
    }

    if (!int.TryParse(info.ArgByIndex(1), out int teamCount) || teamCount < 0 || teamCount > 5)
    {
      player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} 0-5 arası bir sayı gir!");
      return;
    }

    if (teamCount <= 1)
    {
      DisableTeams(player);
      return;
    }

    var terrorists = Utilities.GetPlayers()
      .Where(p => p != null && p.IsValid && p.TeamNum == (int)CsTeam.Terrorist && IsAlive(p))
      .ToList();

    if (terrorists.Count == 0)
    {
      player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Yeterli oyuncu yok.");
      return;
    }

    if (terrorists.Count % teamCount != 0)
    {
      player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{terrorists.Count}{CC.Default} oyuncu {CC.Gold}{teamCount}{CC.Default} takıma eşit bölünemez.");
      return;
    }

    CreateTeams(player, terrorists, teamCount);
  }

  private void CreateTeams(CCSPlayerController admin, List<CCSPlayerController> terrorists, int teamCount)
  {
    activeTeams = teamCount;
    playerTeams.Clear();

    var random = new Random();
    var shuffled = terrorists.OrderBy(x => random.Next()).ToList();

    int playersPerTeam = shuffled.Count / teamCount;

    for (int i = 0; i < shuffled.Count; i++)
    {
      int teamIndex = i / playersPerTeam;
      if (teamIndex >= teamCount) teamIndex = teamCount - 1;

      var p = shuffled[i];
      if (p.UserId.HasValue)
      {
        playerTeams[p.UserId.Value] = teamIndex;
        ApplyTeamColor(p, teamIndex);
        p.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Takımın: {GetColoredTeamName(teamIndex)}");
      }
    }

    Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{admin.PlayerName}{CC.Default}: {CC.Gold}{teamCount}{CC.Default} takım oluşturuldu.");
  }

  private void DisableTeams(CCSPlayerController admin)
  {
    if (activeTeams == 0)
    {
      admin.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Aktif takım yok!");
      return;
    }

    activeTeams = 0;
    playerTeams.Clear();

    ResetAllTerroristColors();

    Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{admin.PlayerName}{CC.Default}: takımları kapattı.");
  }

  private void ApplyTeamColor(CCSPlayerController player, int teamIndex)
  {
    if (!player.IsValid || teamIndex < 0 || teamIndex >= teamColors.Length)
      return;

    var pawn = player.PlayerPawn.Value;
    if (pawn != null && pawn.IsValid)
    {
      pawn.Render = teamColors[teamIndex];
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
    return teamIndex switch
    {
      0 => $"{CC.Red}{teamNames[0]}{CC.Default}",
      1 => $"{CC.Green}{teamNames[1]}{CC.Default}",
      2 => $"{CC.Blue}{teamNames[2]}{CC.Default}",
      3 => $"{CC.Yellow}{teamNames[3]}{CC.Default}",
      4 => $"{CC.Orchid}{teamNames[4]}{CC.Default}",
      _ => teamNames[teamIndex]
    };
  }

  private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || !player.UserId.HasValue)
      return HookResult.Continue;

    ResetPlayerColor(player);
    playerTeams.Remove(player.UserId.Value);

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
      Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {GetColoredTeamName(winningTeam)} kazandı.");

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
    if (player == null || !player.IsValid || !player.UserId.HasValue)
      return HookResult.Continue;

    playerTeams.Remove(player.UserId.Value);

    return HookResult.Continue;
  }

  private static bool IsAlive(CCSPlayerController? player)
  {
    return player?.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
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
