using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace CommandMaker;

public partial class CommandMaker
{
  private bool ValidateArgument(string arg, CCSPlayerController? caller, (string? Type, int? Min, int? Max, int? Length, string? List, string? Default) spec, out string errorMsg)
  {
    errorMsg = "";

    switch (spec.Type)
    {
      case "number":
        {
          if (!int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
          {
            errorMsg = Message(Localizer["commandmaker.arg_number"]);
            return false;
          }

          return CheckRange(value, spec.Min, spec.Max, out errorMsg);
        }

      case "float":
        {
          if (!float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
          {
            errorMsg = Message(Localizer["commandmaker.arg_number"]);
            return false;
          }

          return CheckRange(value, spec.Min, spec.Max, out errorMsg);
        }

      case "word":
        {
          if (spec.Length.HasValue && arg.Length > spec.Length.Value)
          {
            errorMsg = Message(Localizer["commandmaker.arg_word_length", spec.Length.Value]);
            return false;
          }

          return true;
        }

      case "list":
        {
          var allowed = Util.Split(spec.List ?? "");
          if (allowed.Length == 0 || allowed.Any(a => string.Equals(a, arg, StringComparison.OrdinalIgnoreCase)))
            return true;

          errorMsg = Message(Localizer["commandmaker.arg_list", string.Join(", ", allowed)]);
          return false;
        }

      case "player":
        {
          if (FindTargets(arg, caller, out _).Count > 0)
            return true;

          errorMsg = Message(Localizer["commandmaker.player_not_found", arg]);
          return false;
        }
    }

    return true;
  }

  private bool CheckRange(float value, int? min, int? max, out string errorMsg)
  {
    errorMsg = "";

    if (min.HasValue && value < min.Value)
    {
      errorMsg = Message(Localizer["commandmaker.arg_min", min.Value]);
      return false;
    }

    if (max.HasValue && value > max.Value)
    {
      errorMsg = Message(Localizer["commandmaker.arg_max", max.Value]);
      return false;
    }

    return true;
  }

  private CCSPlayerController? NearestPlayer(CCSPlayerController? caller)
  {
    var origin = caller?.PlayerPawn.Value?.AbsOrigin;
    if (origin == null)
      return null;

    CCSPlayerController? best = null;
    float bestDistance = float.MaxValue;

    foreach (var candidate in Players)
    {
      if (candidate.Slot == caller!.Slot)
        continue;

      var pos = candidate.PlayerPawn.Value?.AbsOrigin;
      if (pos == null)
        continue;

      float dx = pos.X - origin.X, dy = pos.Y - origin.Y, dz = pos.Z - origin.Z;
      float distance = dx * dx + dy * dy + dz * dz;

      if (distance < bestDistance)
      {
        bestDistance = distance;
        best = candidate;
      }
    }

    return best;
  }

  private List<CCSPlayerController> FindTargets(string search, CCSPlayerController? caller, out string? groupLabel)
  {
    groupLabel = null;
    var players = Players;
    var s = search.ToLowerInvariant();

    switch (s)
    {
      case "@all":
        groupLabel = Localizer["commandmaker.group_all"];
        return players.ToList();
      case "@ct":
        groupLabel = Localizer["commandmaker.group_ct"];
        return players.Where(p => p.Team == CsTeam.CounterTerrorist).ToList();
      case "@t":
        groupLabel = Localizer["commandmaker.group_t"];
        return players.Where(p => p.Team == CsTeam.Terrorist).ToList();
      case "@alive":
        groupLabel = Localizer["commandmaker.group_alive"];
        return players.Where(p => p.PawnIsAlive).ToList();
      case "@dead":
        groupLabel = Localizer["commandmaker.group_dead"];
        return players.Where(p => !p.PawnIsAlive).ToList();
      case "@me":
        return caller != null
          ? new List<CCSPlayerController> { caller }
          : new List<CCSPlayerController>();
      case "@spec":
        groupLabel = Localizer["commandmaker.group_spec"];
        return players.Where(p => p.Team == CsTeam.Spectator).ToList();
      case "@bot":
        groupLabel = Localizer["commandmaker.group_bot"];
        return players.Where(p => p.IsBot).ToList();
      case "@human":
        groupLabel = Localizer["commandmaker.group_human"];
        return players.Where(p => !p.IsBot).ToList();
      case "@!me":
        groupLabel = Localizer["commandmaker.group_others"];
        return caller == null
          ? players.ToList()
          : players.Where(p => p.Slot != caller.Slot).ToList();
      case "@aim":
        {
          var aim = AimTarget(caller);
          return aim != null
            ? new List<CCSPlayerController> { aim }
            : new List<CCSPlayerController>();
        }
      case "@nearest":
        {
          var nearest = NearestPlayer(caller);
          return nearest != null
            ? new List<CCSPlayerController> { nearest }
            : new List<CCSPlayerController>();
        }
      case "@random":
        {
          var pool = players.Where(p => !p.IsBot).ToList();
          return pool.Count == 0
            ? new List<CCSPlayerController>()
            : new List<CCSPlayerController> { pool[Random.Shared.Next(pool.Count)] };
        }
    }

    if (s.StartsWith("#") && int.TryParse(s[1..], out int userid))
    {
      var byId = Utilities.GetPlayerFromUserid(userid);
      return byId != null && byId.IsValid
        ? new List<CCSPlayerController> { byId }
        : new List<CCSPlayerController>();
    }

    var byName = players.FirstOrDefault(p => string.Equals(p.PlayerName, search, StringComparison.OrdinalIgnoreCase))
              ?? players.FirstOrDefault(p => p.PlayerName.Contains(search, StringComparison.OrdinalIgnoreCase));
    return byName != null
      ? new List<CCSPlayerController> { byName }
      : new List<CCSPlayerController>();
  }
}
