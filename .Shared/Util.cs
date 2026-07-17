using System.Drawing;
using System.Globalization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;

namespace ByDexter.Shared;

public static class Util
{
  public static string[] Split(string names) =>
    names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

  public static bool HasAccess(CCSPlayerController? player, string flags)
  {
    if (player == null)
      return true;

    if (!player.IsValid)
      return false;

    if (AdminManager.PlayerHasPermissions(player, "@css/root"))
      return true;

    var list = Split(flags);
    if (list.Length == 0)
      return true;

    foreach (var flag in list)
    {
      if (AdminManager.PlayerHasPermissions(player, flag))
        return true;
    }

    return false;
  }

  public static bool IsAlive(CCSPlayerController? player)
  {
    var pawn = player?.PlayerPawn.Value;
    return pawn?.IsValid == true && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE;
  }

  public static Color ParseColor(string value, Color fallback)
  {
    if (string.IsNullOrWhiteSpace(value))
      return fallback;

    value = value.Trim();

    if (value.StartsWith('#') && value.Length == 7
        && int.TryParse(value.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hex))
      return Color.FromArgb(255, (hex >> 16) & 0xFF, (hex >> 8) & 0xFF, hex & 0xFF);

    var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length == 3
        && byte.TryParse(parts[0], out var r) && byte.TryParse(parts[1], out var g) && byte.TryParse(parts[2], out var b))
      return Color.FromArgb(255, r, g, b);

    return fallback;
  }
}
