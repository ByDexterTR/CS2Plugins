using CounterStrikeSharp.API.Core;

namespace ByDexter.Shared;

public static class Transmit
{
  public static void Hide(CCheckTransmitInfo info, int index)
  {
    if (index <= 0 || !info.TransmitEntities.Contains(index))
      return;

    info.TransmitEntities.Remove(index);
    info.TransmitAlways.Add(index);
  }

  public static void Hide(CCheckTransmitInfo info, uint index) => Hide(info, (int)index);

  public static void Hide(CCheckTransmitInfo info, CEntityInstance? entity)
  {
    if (entity != null && entity.IsValid)
      Hide(info, (int)entity.Index);
  }
}
