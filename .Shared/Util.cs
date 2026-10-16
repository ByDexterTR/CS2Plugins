using System.Drawing;
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;

namespace ByDexter.Shared;

public static class Util
{
  public const int HudTickRate = 4;

  public static bool IsHudFrame() => Server.TickCount % HudTickRate == 0;

  private const int PlayerCacheTicks = 32;

  private static readonly List<CCSPlayerController> CachedPlayers = new(64);
  private static int _playerStamp = int.MinValue;

  public static List<CCSPlayerController> Players()
  {
    int tick = Server.TickCount;

    if (tick - _playerStamp < PlayerCacheTicks && tick >= _playerStamp)
      return CachedPlayers;

    _playerStamp = tick;
    CachedPlayers.Clear();

    foreach (var player in Utilities.GetPlayers())
    {
      if (player != null && player.IsValid)
        CachedPlayers.Add(player);
    }

    return CachedPlayers;
  }

  public static void InvalidatePlayers() => _playerStamp = int.MinValue;

  public static int UserId(CCSPlayerController? player) =>
    player != null && player.IsValid ? player.UserId ?? -1 : -1;

  public static ulong SteamId(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid)
      return 0UL;

    ulong id = player.AuthorizedSteamID?.SteamId64 ?? 0UL;
    return id > 76561197960265728UL && (id & 0xFFFFFFFFUL) != 0xFFFFFFFFUL ? id : 0UL;
  }

  public static CCSPlayerController? FromUserId(int userId)
  {
    if (userId < 0)
      return null;

    var player = Utilities.GetPlayerFromUserid(userId);
    if (player == null || !player.IsValid || player.UserId != userId)
      return null;

    return player;
  }

  public static CCSPlayerController? PawnController(CEntityInstance? entity)
  {
    if (entity == null || !entity.IsValid || entity.DesignerName != "player")
      return null;

    return entity.As<CCSPlayerPawn>().Controller.Value?.As<CCSPlayerController>();
  }

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

  public static bool TryGetButtons(CCSPlayerController? player, out PlayerButtons buttons)
  {
    buttons = 0;

    var pawn = player?.Pawn.Value;
    if (pawn == null || !pawn.IsValid || pawn.MovementServices == null)
      return false;

    try
    {
      buttons = (PlayerButtons)pawn.MovementServices.Buttons.ButtonStates[0];
      return true;
    }
    catch
    {
      return false;
    }
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
