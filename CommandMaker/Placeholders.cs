using System.Globalization;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace CommandMaker;

public sealed class Placeholder
{
  public readonly string Name;

  public Placeholder(string name) => Name = name;
}

public partial class CommandMaker
{
  private static readonly Dictionary<string, string> ColorTokens = new(StringComparer.OrdinalIgnoreCase)
  {
    ["DEFAULT"] = $"{CC.Default}",
    ["RED"] = $"{CC.Red}",
    ["LIGHTRED"] = $"{CC.LightRed}",
    ["DARKRED"] = $"{CC.DarkRed}",
    ["BLUEGREY"] = $"{CC.BlueGrey}",
    ["BLUE"] = $"{CC.Blue}",
    ["DARKBLUE"] = $"{CC.DarkBlue}",
    ["PURPLE"] = $"{CC.Purple}",
    ["ORCHID"] = $"{CC.Orchid}",
    ["YELLOW"] = $"{CC.Yellow}",
    ["GOLD"] = $"{CC.Gold}",
    ["LIGHTGREEN"] = $"{CC.LightGreen}",
    ["GREEN"] = $"{CC.Green}",
    ["LIME"] = $"{CC.Lime}",
    ["GREY"] = $"{CC.Grey}",
    ["GREY2"] = $"{CC.Grey2}"
  };

  private static readonly HashSet<string> KnownPlaceholders = new(StringComparer.OrdinalIgnoreCase)
  {
    "PLAYER", "PLAYERNAME", "PLAYERHEALTH", "PLAYERARMOR", "PLAYERMONEY", "PLAYERSTEAMID",
    "PLAYERTEAM", "PLAYERWEAPON", "PLAYERCOORDINATE",
    "TARGET", "PLAYER/TARGET", "TARGETHEALTH", "TARGETARMOR", "TARGETMONEY", "TARGETSTEAMID",
    "TARGETTEAM", "TARGETWEAPON", "TARGETCOORDINATE",
    "ARG1", "ARG2", "ARG3",
    "PLAYERCOUNT", "ALIVECOUNT", "TCOUNT", "CTCOUNT", "SPECCOUNT", "ALIVET", "ALIVECT",
    "RANDOMPLAYER", "RANDOMT", "RANDOMCT", "RANDOMALIVE", "RANDOMDEAD",
    "RANDOMTALIVE", "RANDOMTDEAD", "RANDOMCTALIVE", "RANDOMCTDEAD",
    "ROUND", "CTSCORE", "TSCORE", "SERVERIP", "SERVERPORT", "HOSTNAME", "MAPNAME", "TIME",
    "PLAYERKILLS", "PLAYERDEATHS", "PLAYERASSISTS", "PLAYERSCORE", "PLAYERKDR",
    "TARGETKILLS", "TARGETDEATHS", "TARGETASSISTS", "TARGETSCORE", "TARGETKDR",
    "PLAYERUSERID", "TARGETUSERID", "PLAYERPING", "TARGETPING", "PLAYERCLAN", "TARGETCLAN",
    "PLAYERANGLE", "TARGETANGLE", "PLAYERCLIP", "PLAYERAMMO", "TARGETCLIP", "TARGETAMMO",
    "PLAYERAIMTARGET", "PLAYERAIM", "TARGETDISTANCE",
    "BOTCOUNT", "MAXPLAYERS", "DATE", "DEADCOUNT", "DEADT", "DEADCT", "TIMELEFT", "WARMUP"
  };

  private readonly Dictionary<string, object[]> _templates = new(StringComparer.Ordinal);

  private List<CCSPlayerController> _tickPlayers = new();
  private int _tickPlayersAt = -1;

  private CCSGameRules? _gameRules;
  private CTeam? _teamT;
  private CTeam? _teamCT;

  private IReadOnlyList<CCSPlayerController> Players
  {
    get
    {
      int tick = Server.TickCount;
      if (_tickPlayersAt != tick)
      {
        _tickPlayersAt = tick;
        _tickPlayers = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV).ToList();
      }

      return _tickPlayers;
    }
  }

  private CCSGameRules? GameRules
  {
    get
    {
      _gameRules ??= Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
      return _gameRules;
    }
  }

  private void RefreshTeamEntities()
  {
    if (_teamT?.IsValid == true && _teamCT?.IsValid == true)
      return;

    _teamT = null;
    _teamCT = null;

    foreach (var team in Utilities.FindAllEntitiesByDesignerName<CTeam>("cs_team_manager"))
    {
      if (team.TeamNum == (byte)CsTeam.CounterTerrorist)
        _teamCT = team;
      else if (team.TeamNum == (byte)CsTeam.Terrorist)
        _teamT = team;
    }
  }

  private void ClearEntityCache()
  {
    _gameRules = null;
    _teamT = null;
    _teamCT = null;
  }

  private static readonly char[] UnsafeCommandChars = { ';', '\n', '\r', '\"', '\0' };

  public static string SanitizeCommandValue(string value)
  {
    if (value.IndexOfAny(UnsafeCommandChars) < 0)
      return value;

    var buffer = new StringBuilder(value.Length);
    foreach (var c in value)
    {
      if (Array.IndexOf(UnsafeCommandChars, c) < 0)
        buffer.Append(c);
    }

    return buffer.ToString();
  }

  public static string SanitizePlayerName(string value)
  {
    var buffer = new StringBuilder(value.Length);
    foreach (var c in value)
    {
      if (c >= ' ' && c != '\u007f')
        buffer.Append(c);
    }

    var name = buffer.ToString().Trim();
    return name.Length > 32 ? name[..32] : name;
  }

  private static string TeamName(CsTeam team) => team switch
  {
    CsTeam.Terrorist => "T",
    CsTeam.CounterTerrorist => "CT",
    CsTeam.Spectator => "SPEC",
    _ => "NONE"
  };

  private static string ActiveWeaponName(CCSPlayerController p)
  {
    var name = p.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value?.DesignerName;
    if (string.IsNullOrEmpty(name))
      return "-";

    return name.StartsWith("weapon_") ? name[7..] : name;
  }

  private static string FormatVector(Vector pos) =>
    string.Create(CultureInfo.InvariantCulture, $"{pos.X:0.##} {pos.Y:0.##} {pos.Z:0.##}");

  private static string PickRandomName(List<CCSPlayerController> pool) =>
    pool.Count == 0 ? "-" : pool[Random.Shared.Next(pool.Count)].PlayerName;

  private float _countsAt = -1f;
  private int _cHumans, _cBots, _cAlive, _cDead, _cT, _cCT, _cSpec, _cAliveT, _cAliveCT, _cDeadT, _cDeadCT;

  private float _bucketsAt = -1f;
  private readonly List<CCSPlayerController> _bAlive = new();
  private readonly List<CCSPlayerController> _bDead = new();
  private readonly List<CCSPlayerController> _bT = new();
  private readonly List<CCSPlayerController> _bCT = new();
  private readonly List<CCSPlayerController> _bTAlive = new();
  private readonly List<CCSPlayerController> _bTDead = new();
  private readonly List<CCSPlayerController> _bCTAlive = new();
  private readonly List<CCSPlayerController> _bCTDead = new();

  private void EnsureCounts()
  {
    float now = Server.CurrentTime;
    if (_countsAt == now)
      return;

    _countsAt = now;
    _cHumans = _cBots = _cAlive = _cDead = 0;
    _cT = _cCT = _cSpec = _cAliveT = _cAliveCT = _cDeadT = _cDeadCT = 0;

    foreach (var player in Players)
    {
      if (player.IsBot)
        _cBots++;
      else
        _cHumans++;

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
          _cCT++;
          if (alive) _cAliveCT++; else _cDeadCT++;
          break;
        case (byte)CsTeam.Spectator:
          _cSpec++;
          break;
      }
    }
  }

  private void EnsureBuckets()
  {
    float now = Server.CurrentTime;
    if (_bucketsAt == now)
      return;

    _bucketsAt = now;
    _bAlive.Clear(); _bDead.Clear(); _bT.Clear(); _bCT.Clear();
    _bTAlive.Clear(); _bTDead.Clear(); _bCTAlive.Clear(); _bCTDead.Clear();

    foreach (var player in Players)
    {
      if (player.IsBot)
        continue;

      bool alive = player.PawnIsAlive;
      if (alive)
        _bAlive.Add(player);
      else
        _bDead.Add(player);

      switch (player.TeamNum)
      {
        case (byte)CsTeam.Terrorist:
          _bT.Add(player);
          if (alive) _bTAlive.Add(player); else _bTDead.Add(player);
          break;
        case (byte)CsTeam.CounterTerrorist:
          _bCT.Add(player);
          if (alive) _bCTAlive.Add(player); else _bCTDead.Add(player);
          break;
      }
    }
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

  private static string Kdr(CCSPlayerController? player)
  {
    var stats = player?.ActionTrackingServices?.MatchStats;
    if (stats == null)
      return "0.00";

    float ratio = stats.Deaths == 0 ? stats.Kills : (float)stats.Kills / stats.Deaths;
    return ratio.ToString("0.00", CultureInfo.InvariantCulture);
  }

  private static string FormatAngle(CCSPlayerController? player)
  {
    if (player?.PlayerPawn.Value is not CCSPlayerPawn pawn || !pawn.IsValid)
      return "0 0 0";

    var angles = pawn.EyeAngles;
    return string.Create(CultureInfo.InvariantCulture, $"{angles.X:0.##} {angles.Y:0.##} {angles.Z:0.##}");
  }

  private static string WeaponValue(CCSPlayerController? player, bool reserve)
  {
    var weapon = (player?.PlayerPawn.Value as CCSPlayerPawn)?.WeaponServices?.ActiveWeapon?.Value as CBasePlayerWeapon;
    if (weapon == null)
      return "0";

    return (reserve ? weapon.ReserveAmmo[0] : weapon.Clip1).ToString(CultureInfo.InvariantCulture);
  }

  private static string DistanceBetween(CCSPlayerController? from, CCSPlayerController? to)
  {
    var a = from?.PlayerPawn.Value?.AbsOrigin;
    var b = to?.PlayerPawn.Value?.AbsOrigin;
    if (a == null || b == null)
      return "0";

    float dx = a.X - b.X, dy = a.Y - b.Y, dz = a.Z - b.Z;
    return MathF.Sqrt(dx * dx + dy * dy + dz * dz).ToString("0", CultureInfo.InvariantCulture);
  }

  private static string RandomRange(string name)
  {
    var range = name[7..].Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (range.Length != 2
        || !int.TryParse(range[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int min)
        || !int.TryParse(range[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int max))
      return "0";

    if (min > max)
      (min, max) = (max, min);

    return Random.Shared.Next(min, max + 1).ToString(CultureInfo.InvariantCulture);
  }

  private static string AimPoint(CCSPlayerController? player)
  {
    if (player?.PlayerPawn.Value is not CCSPlayerPawn pawn || !pawn.IsValid || pawn.AbsOrigin == null)
      return "0 0 0";

    var angles = pawn.EyeAngles;
    float yaw = angles.Y * MathF.PI / 180f;
    float pitch = angles.X * MathF.PI / 180f;

    var forward = new System.Numerics.Vector3(
      MathF.Cos(pitch) * MathF.Cos(yaw),
      MathF.Cos(pitch) * MathF.Sin(yaw),
      -MathF.Sin(pitch));

    var origin = pawn.AbsOrigin;
    var eye = new System.Numerics.Vector3(origin.X, origin.Y, origin.Z + pawn.ViewOffset.Z);
    var end = eye + forward * 8192f;

    var hit = NativeTrace.TraceLine(pawn, eye, end) ?? (eye + forward * 128f);
    var spot = hit - forward * 24f;
    spot.Z += 6f;

    return string.Create(CultureInfo.InvariantCulture, $"{spot.X:0.##} {spot.Y:0.##} {spot.Z:0.##}");
  }

  internal CCSPlayerController? AimTarget(CCSPlayerController? viewer)
  {
    if (viewer?.PlayerPawn.Value is not CCSPlayerPawn pawn || !pawn.IsValid)
      return null;

    var origin = pawn.AbsOrigin;
    if (origin == null)
      return null;

    var eye = new System.Numerics.Vector3(origin.X, origin.Y, origin.Z + pawn.ViewOffset.Z);

    var forward = new Vector();
    NativeAPI.AngleVectors(pawn.EyeAngles.Handle, forward.Handle, 0, 0);
    var dir = new System.Numerics.Vector3(forward.X, forward.Y, forward.Z);

    if (dir.LengthSquared() < 0.001f)
      return null;

    dir = System.Numerics.Vector3.Normalize(dir);

    CCSPlayerController? best = null;
    float bestDot = 0.96f;

    foreach (var candidate in Players)
    {
      if (candidate.Slot == viewer.Slot || !candidate.PawnIsAlive)
        continue;

      var pos = candidate.PlayerPawn.Value?.AbsOrigin;
      if (pos == null)
        continue;

      var to = new System.Numerics.Vector3(pos.X - eye.X, pos.Y - eye.Y, pos.Z + 55f - eye.Z);
      float distance = to.Length();
      if (distance < 1f)
        continue;

      float dot = System.Numerics.Vector3.Dot(dir, to / distance);
      if (dot < bestDot)
        continue;

      if (NativeTrace.Available)
      {
        var blocked = NativeTrace.TraceLine(pawn, eye, eye + to);
        if (blocked.HasValue && (blocked.Value - eye).Length() < distance - 40f)
          continue;
      }

      bestDot = dot;
      best = candidate;
    }

    return best;
  }

  private void PrecompileTemplate(string? text)
  {
    if (!string.IsNullOrEmpty(text) && text.Contains('['))
      GetTemplate(text);
  }

  private void PrecompileTemplates(List<string>? lines)
  {
    if (lines == null)
      return;

    foreach (var line in lines)
      PrecompileTemplate(line);
  }

  private object[] GetTemplate(string message)
  {
    if (_templates.TryGetValue(message, out var cached))
      return cached;

    var tokens = new List<object>();
    var literal = new StringBuilder();
    int i = 0;

    while (i < message.Length)
    {
      char c = message[i];
      if (c != '[')
      {
        literal.Append(c);
        i++;
        continue;
      }

      int close = message.IndexOf(']', i + 1);
      if (close < 0)
      {
        literal.Append(message, i, message.Length - i);
        break;
      }

      string name = message[(i + 1)..close];

      if (ColorTokens.TryGetValue(name, out var color))
      {
        literal.Append(color);
      }
      else if (KnownPlaceholders.Contains(name) || name.StartsWith("RANDOM:", StringComparison.OrdinalIgnoreCase))
      {
        if (literal.Length > 0)
        {
          tokens.Add(literal.ToString());
          literal.Clear();
        }

        tokens.Add(new Placeholder(name.ToUpperInvariant()));
      }
      else
      {
        literal.Append(message, i, close - i + 1);
      }

      i = close + 1;
    }

    if (literal.Length > 0)
      tokens.Add(literal.ToString());

    var result = tokens.ToArray();
    _templates[message] = result;
    return result;
  }

  private string FormatMessage(string message, CCSPlayerController? player, CCSPlayerController? target, string? arg1, string? arg2, string? arg3, string? targetLabel = null, bool sanitize = false)
  {
    if (!message.Contains('['))
      return message;

    var tokens = GetTemplate(message);

    if (tokens.Length == 0)
      return "";

    if (tokens.Length == 1 && tokens[0] is string onlyLiteral)
      return onlyLiteral;

    string Clean(string value) => sanitize ? SanitizeCommandValue(value) : value;

    string Resolve(string name)
    {
      switch (name)
      {
        case "PLAYER":
        case "PLAYERNAME":
          return player == null ? "" : Clean(player.PlayerName);
        case "PLAYERHEALTH":
          return (player?.PlayerPawn.Value?.Health ?? 0).ToString();
        case "PLAYERARMOR":
          return (player?.PlayerPawn.Value?.ArmorValue ?? 0).ToString();
        case "PLAYERMONEY":
          return (player?.InGameMoneyServices?.Account ?? 0).ToString();
        case "PLAYERSTEAMID":
          return (player?.SteamID ?? 0).ToString();
        case "PLAYERTEAM":
          return player == null ? "NONE" : TeamName(player.Team);
        case "PLAYERWEAPON":
          return player == null ? "-" : Clean(ActiveWeaponName(player));
        case "PLAYERCOORDINATE":
          {
            var pos = player?.PlayerPawn.Value?.AbsOrigin;
            return pos == null ? "0 0 0" : FormatVector(pos);
          }

        case "TARGET":
        case "PLAYER/TARGET":
          return target == null ? "" : Clean(targetLabel ?? target.PlayerName);
        case "TARGETHEALTH":
          return (target?.PlayerPawn.Value?.Health ?? 0).ToString();
        case "TARGETARMOR":
          return (target?.PlayerPawn.Value?.ArmorValue ?? 0).ToString();
        case "TARGETMONEY":
          return (target?.InGameMoneyServices?.Account ?? 0).ToString();
        case "TARGETSTEAMID":
          return (target?.SteamID ?? 0).ToString();
        case "TARGETTEAM":
          return target == null ? "NONE" : TeamName(target.Team);
        case "TARGETWEAPON":
          return target == null ? "-" : Clean(ActiveWeaponName(target));
        case "TARGETCOORDINATE":
          {
            var pos = target?.PlayerPawn.Value?.AbsOrigin;
            return pos == null ? "0 0 0" : FormatVector(pos);
          }

        case "ARG1":
          return arg1 == null ? "" : Clean(arg1);
        case "ARG2":
          return arg2 == null ? "" : Clean(arg2);
        case "ARG3":
          return arg3 == null ? "" : Clean(arg3);

        case "PLAYERCOUNT":
          EnsureCounts();
          return _cHumans.ToString();
        case "ALIVECOUNT":
          EnsureCounts();
          return _cAlive.ToString();
        case "TCOUNT":
          EnsureCounts();
          return _cT.ToString();
        case "CTCOUNT":
          EnsureCounts();
          return _cCT.ToString();
        case "SPECCOUNT":
          EnsureCounts();
          return _cSpec.ToString();
        case "ALIVET":
          EnsureCounts();
          return _cAliveT.ToString();
        case "ALIVECT":
          EnsureCounts();
          return _cAliveCT.ToString();

        case "RANDOMPLAYER":
        case "RANDOMALIVE":
          EnsureBuckets();
          return Clean(PickRandomName(_bAlive));
        case "RANDOMDEAD":
          EnsureBuckets();
          return Clean(PickRandomName(_bDead));
        case "RANDOMT":
          EnsureBuckets();
          return Clean(PickRandomName(_bT));
        case "RANDOMCT":
          EnsureBuckets();
          return Clean(PickRandomName(_bCT));
        case "RANDOMTALIVE":
          EnsureBuckets();
          return Clean(PickRandomName(_bTAlive));
        case "RANDOMTDEAD":
          EnsureBuckets();
          return Clean(PickRandomName(_bTDead));
        case "RANDOMCTALIVE":
          EnsureBuckets();
          return Clean(PickRandomName(_bCTAlive));
        case "RANDOMCTDEAD":
          EnsureBuckets();
          return Clean(PickRandomName(_bCTDead));

        case "ROUND":
          return ((GameRules?.TotalRoundsPlayed ?? 0) + 1).ToString();
        case "CTSCORE":
          RefreshTeamEntities();
          return (_teamCT?.Score ?? 0).ToString();
        case "TSCORE":
          RefreshTeamEntities();
          return (_teamT?.Score ?? 0).ToString();

        case "SERVERIP":
          return ConVar.Find("ip")?.StringValue ?? "unknown";
        case "SERVERPORT":
          return ConVar.Find("hostport")?.GetPrimitiveValue<int>().ToString() ?? "27015";
        case "HOSTNAME":
          return Clean(ConVar.Find("hostname")?.StringValue ?? "unknown");
        case "MAPNAME":
          return Clean(Server.MapName);
        case "TIME":
          return DateTime.Now.ToString("HH:mm:ss");
        case "DATE":
          return DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        case "MAXPLAYERS":
          return Server.MaxPlayers.ToString(CultureInfo.InvariantCulture);

        case "PLAYERKILLS":
          return Stat(player, 0);
        case "PLAYERDEATHS":
          return Stat(player, 1);
        case "PLAYERASSISTS":
          return Stat(player, 2);
        case "PLAYERSCORE":
          return (player?.Score ?? 0).ToString(CultureInfo.InvariantCulture);
        case "PLAYERKDR":
          return Kdr(player);
        case "TARGETKILLS":
          return Stat(target, 0);
        case "TARGETDEATHS":
          return Stat(target, 1);
        case "TARGETASSISTS":
          return Stat(target, 2);
        case "TARGETSCORE":
          return (target?.Score ?? 0).ToString(CultureInfo.InvariantCulture);
        case "TARGETKDR":
          return Kdr(target);

        case "PLAYERUSERID":
          return Util.UserId(player).ToString(CultureInfo.InvariantCulture);
        case "TARGETUSERID":
          return Util.UserId(target).ToString(CultureInfo.InvariantCulture);
        case "PLAYERPING":
          return (player?.Ping ?? 0).ToString(CultureInfo.InvariantCulture);
        case "TARGETPING":
          return (target?.Ping ?? 0).ToString(CultureInfo.InvariantCulture);
        case "PLAYERCLAN":
          return player == null ? "" : Clean(player.Clan);
        case "TARGETCLAN":
          return target == null ? "" : Clean(target.Clan);

        case "PLAYERANGLE":
          return FormatAngle(player);
        case "TARGETANGLE":
          return FormatAngle(target);
        case "PLAYERCLIP":
          return WeaponValue(player, false);
        case "PLAYERAMMO":
          return WeaponValue(player, true);
        case "TARGETCLIP":
          return WeaponValue(target, false);
        case "TARGETAMMO":
          return WeaponValue(target, true);

        case "PLAYERAIM":
          return AimPoint(player);
        case "PLAYERAIMTARGET":
          return Clean(AimTarget(player)?.PlayerName ?? "-");
        case "TARGETDISTANCE":
          return DistanceBetween(player, target);

        case "BOTCOUNT":
          EnsureCounts();
          return _cBots.ToString();
        case "DEADCOUNT":
          EnsureCounts();
          return _cDead.ToString();
        case "DEADT":
          EnsureCounts();
          return _cDeadT.ToString();
        case "DEADCT":
          EnsureCounts();
          return _cDeadCT.ToString();

        case "WARMUP":
          return GameRules?.WarmupPeriod == true ? "1" : "0";
        case "TIMELEFT":
          {
            var rules = GameRules;
            if (rules == null)
              return "0";

            float left = rules.RoundTime - (Server.CurrentTime - rules.RoundStartTime);
            return ((int)MathF.Max(0f, left)).ToString(CultureInfo.InvariantCulture);
          }
      }

      if (name.StartsWith("RANDOM:", StringComparison.OrdinalIgnoreCase))
        return RandomRange(name);

      return "";
    }

    var builder = new StringBuilder(message.Length + 32);

    foreach (var token in tokens)
    {
      if (token is string literal)
        builder.Append(literal);
      else
        builder.Append(Resolve(((Placeholder)token).Name));
    }

    return builder.ToString();
  }
}
