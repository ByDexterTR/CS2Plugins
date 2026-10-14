using System.Globalization;
using CounterStrikeSharp.API.Core;

namespace ByDexter.Shared;

public static class Health
{
  public static void Set(CCSPlayerPawn pawn, int hp)
  {
    if (pawn == null || !pawn.IsValid)
      return;

    int max = pawn.MaxHealth;
    if (hp > max)
      pawn.MaxHealth = hp;

    pawn.AcceptInput("SetHealth", pawn, pawn, hp.ToString(CultureInfo.InvariantCulture));

    if (hp > max)
      pawn.MaxHealth = max;
  }

  public static int Heal(CCSPlayerPawn pawn, int amount, int max)
  {
    if (pawn == null || !pawn.IsValid || amount <= 0)
      return 0;

    int target = Math.Min(pawn.Health + amount, max);
    int healed = target - pawn.Health;
    if (healed <= 0)
      return 0;

    Set(pawn, target);
    return healed;
  }
}
