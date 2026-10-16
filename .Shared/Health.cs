using CounterStrikeSharp.API.Core;

namespace ByDexter.Shared;

public static class Health
{
  public static void Set(CCSPlayerPawn pawn, int hp)
  {
    if (pawn == null || !pawn.IsValid)
      return;

    pawn.Health = hp;
    NetState.Changed(pawn, "CBaseEntity", "m_iHealth");
  }

  public static void SetMax(CCSPlayerPawn pawn, int max)
  {
    if (pawn == null || !pawn.IsValid)
      return;

    pawn.MaxHealth = max;
    NetState.Changed(pawn, "CBaseEntity", "m_iMaxHealth");
  }

  public static int Heal(CCSPlayerPawn pawn, int amount, int max)
  {
    if (pawn == null || !pawn.IsValid || amount <= 0)
      return 0;

    int current = pawn.Health;
    int target = Math.Min(current + amount, max);
    int healed = target - current;

    if (healed <= 0)
      return 0;

    pawn.Health = target;
    NetState.Changed(pawn, "CBaseEntity", "m_iHealth");
    return healed;
  }
}
