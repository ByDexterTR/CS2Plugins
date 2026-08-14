using System.Globalization;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace Ads;

public partial class Ads
{
  private float _statsAt = -1f;
  private int _cPlayers, _cBots, _cAlive, _cDead;
  private int _cT, _cCt, _cSpec, _cAliveT, _cAliveCt, _cDeadT, _cDeadCt;

  private static bool HasTokens(string? text) =>
    !string.IsNullOrEmpty(text) && text.IndexOf('{') >= 0;

  private string Fill(string text, CCSPlayerController? viewer)
  {
    if (!HasTokens(text))
      return text;

    var sb = new StringBuilder(text.Length + 32);
    int i = 0;

    while (i < text.Length)
    {
      int open = text.IndexOf('{', i);
      if (open < 0)
      {
        sb.Append(text, i, text.Length - i);
        break;
      }

      int close = text.IndexOf('}', open + 1);
      if (close < 0)
      {
        sb.Append(text, i, text.Length - i);
        break;
      }

      sb.Append(text, i, open - i);

      string key = text.Substring(open + 1, close - open - 1);
      string? value = Resolve(key, viewer);

      if (value == null)
        sb.Append(text, open, close - open + 1);
      else
        sb.Append(value);

      i = close + 1;
    }

    return sb.ToString();
  }

  private string? Resolve(string key, CCSPlayerController? viewer)
  {
    switch (key)
    {
      case "map": return Server.MapName;
      case "hostname": return ConVar.Find("hostname")?.StringValue ?? "";
      case "ip": return ConVar.Find("ip")?.StringValue ?? "";
      case "port": return ConVar.Find("hostport")?.GetPrimitiveValue<int>().ToString(CultureInfo.InvariantCulture) ?? "27015";
      case "maxplayers": return Server.MaxPlayers.ToString(CultureInfo.InvariantCulture);

      case "players": EnsureCounts(); return _cPlayers.ToString(CultureInfo.InvariantCulture);
      case "bots": EnsureCounts(); return _cBots.ToString(CultureInfo.InvariantCulture);
      case "alive": EnsureCounts(); return _cAlive.ToString(CultureInfo.InvariantCulture);
      case "dead": EnsureCounts(); return _cDead.ToString(CultureInfo.InvariantCulture);
      case "t_count": EnsureCounts(); return _cT.ToString(CultureInfo.InvariantCulture);
      case "ct_count": EnsureCounts(); return _cCt.ToString(CultureInfo.InvariantCulture);
      case "spec_count": EnsureCounts(); return _cSpec.ToString(CultureInfo.InvariantCulture);
      case "alive_t": EnsureCounts(); return _cAliveT.ToString(CultureInfo.InvariantCulture);
      case "alive_ct": EnsureCounts(); return _cAliveCt.ToString(CultureInfo.InvariantCulture);
      case "dead_t": EnsureCounts(); return _cDeadT.ToString(CultureInfo.InvariantCulture);
      case "dead_ct": EnsureCounts(); return _cDeadCt.ToString(CultureInfo.InvariantCulture);

      case "round": return RoundNumber().ToString(CultureInfo.InvariantCulture);
      case "t_score": return TeamScore(2).ToString(CultureInfo.InvariantCulture);
      case "ct_score": return TeamScore(3).ToString(CultureInfo.InvariantCulture);

      case "time": return DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
      case "date": return DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

      case "player": return viewer?.PlayerName ?? "";
      case "steamid": return viewer != null ? viewer.SteamID.ToString(CultureInfo.InvariantCulture) : "";
      case "team": return ViewerTeam(viewer);
      case "kills": return Stat(viewer, 0);
      case "deaths": return Stat(viewer, 1);
      case "assists": return Stat(viewer, 2);
      case "score": return viewer != null ? viewer.Score.ToString(CultureInfo.InvariantCulture) : "";

      default: return null;
    }
  }

  private void EnsureCounts()
  {
    float now = Server.CurrentTime;
    if (_statsAt == now)
      return;

    _statsAt = now;
    _cPlayers = _cBots = _cAlive = _cDead = 0;
    _cT = _cCt = _cSpec = _cAliveT = _cAliveCt = _cDeadT = _cDeadCt = 0;

    foreach (var player in Utilities.GetPlayers())
    {
      if (player == null || !player.IsValid || player.IsHLTV)
        continue;

      if (player.IsBot)
        _cBots++;
      else
        _cPlayers++;

      bool alive = player.PawnIsAlive;
      if (alive)
        _cAlive++;
      else
        _cDead++;

      switch (player.TeamNum)
      {
        case (byte)CsTeam.Terrorist:
          _cT++;
          if (alive) _cAliveT++; else _cDeadT++;
          break;

        case (byte)CsTeam.CounterTerrorist:
          _cCt++;
          if (alive) _cAliveCt++; else _cDeadCt++;
          break;

        case (byte)CsTeam.Spectator:
          _cSpec++;
          break;
      }
    }
  }

  private static int RoundNumber()
  {
    foreach (var proxy in Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules"))
    {
      var rules = proxy?.GameRules;
      if (rules != null)
        return rules.TotalRoundsPlayed + 1;
    }

    return 1;
  }

  private static string ViewerTeam(CCSPlayerController? player) => player?.TeamNum switch
  {
    (byte)CsTeam.Terrorist => "T",
    (byte)CsTeam.CounterTerrorist => "CT",
    (byte)CsTeam.Spectator => "Spectator",
    _ => ""
  };

  private static int TeamScore(int teamNum)
  {
    foreach (var team in Utilities.FindAllEntitiesByDesignerName<CTeam>("cs_team_manager"))
    {
      if (team != null && team.IsValid && team.TeamNum == teamNum)
        return team.Score;
    }

    return 0;
  }

  private static string Stat(CCSPlayerController? player, int kind)
  {
    var stats = player?.ActionTrackingServices?.MatchStats;
    if (stats == null)
      return "0";

    int value = kind switch
    {
      0 => stats.Kills,
      1 => stats.Deaths,
      _ => stats.Assists
    };

    return value.ToString(CultureInfo.InvariantCulture);
  }
}
