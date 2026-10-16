using System.Numerics;
using ByDexter.Shared.Source2;

namespace ShowPlayerClips.Source2;

public sealed class GroundClip(WorldBvh world)
{
  private const float Margin = 4f;
  private const float Lift = 2f;
  private const float MinLength = 1f;
  private const float PlayerHeight = 72f;

  private static readonly Vector3[] Around =
  [
    new(Margin, 0f, 0f),
    new(-Margin, 0f, 0f),
    new(0f, Margin, 0f),
    new(0f, -Margin, 0f),
  ];

  private readonly WorldBvh _world = world;

  public static bool IsGround(ClipAttribute attribute)
  {
    if (attribute.InteractAs.Count != 0)
      return false;

    foreach (string tag in attribute.InteractExclude)
    {
      if (tag.Equals("player", StringComparison.OrdinalIgnoreCase))
        return false;
    }

    return true;
  }

  public void Apply(List<ClipSegment> segments)
  {
    int write = 0;

    for (int i = 0; i < segments.Count; i++)
    {
      if (Clip(segments[i]) is { } clipped)
        segments[write++] = clipped;
    }

    segments.RemoveRange(write, segments.Count - write);
  }

  private ClipSegment? Clip(ClipSegment segment)
  {
    bool startBuried = Buried(segment.Start);
    bool endBuried = Buried(segment.End);

    if (!startBuried && !endBuried)
      return segment;

    if (startBuried && endBuried)
      return Buried(segment.Middle) ? null : segment;

    var open = startBuried ? segment.End : segment.Start;
    var closed = startBuried ? segment.Start : segment.End;

    if (Entry(open, closed) is not { } entry)
      return segment;

    if (Vector3.Distance(open, entry) < MinLength)
      return null;

    return startBuried
      ? segment with { Start = entry }
      : segment with { End = entry };
  }

  private bool Buried(Vector3 point)
  {
    if (!BuriedAt(point))
      return false;

    foreach (var offset in Around)
    {
      if (!BuriedAt(point + offset))
        return false;
    }

    return true;
  }

  private bool BuriedAt(Vector3 point)
  {
    var low = point + new Vector3(0f, 0f, Lift);
    float top = MathF.Max(_world.Max.Z, low.Z) + 16f;

    var up = default(WorldBvh.Face);
    float upFraction = _world.Fraction(low, new Vector3(point.X, point.Y, top), ref up);
    if (upFraction >= 1f || Normal(up).Z <= 0f)
      return false;

    float ceiling = low.Z + (top - low.Z) * upFraction;
    float bottom = MathF.Min(_world.Min.Z, point.Z) - 16f;

    var down = default(WorldBvh.Face);
    float downFraction = _world.Fraction(point, new Vector3(point.X, point.Y, bottom), ref down);
    if (downFraction >= 1f || Normal(down).Z < 0f)
      return true;

    float floor = point.Z + (bottom - point.Z) * downFraction;
    return ceiling - floor < PlayerHeight;
  }

  private Vector3? Entry(Vector3 from, Vector3 to)
  {
    var direction = to - from;
    float length = direction.Length();
    if (length < MinLength)
      return null;

    var step = direction / length * 0.05f;
    var start = from;

    for (int guard = 0; guard < 16; guard++)
    {
      var face = default(WorldBvh.Face);
      float fraction = _world.Fraction(start, to, ref face);
      if (fraction >= 1f)
        return null;

      var hit = start + (to - start) * fraction;
      if (Vector3.Dot(Normal(face), direction) < 0f)
        return hit;

      start = hit + step;
    }

    return null;
  }

  private static Vector3 Normal(in WorldBvh.Face face) => Vector3.Cross(face.E1, face.E2);
}
