using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace ByDexter.Shared;

public static class NativeTrace
{
  public const ulong MaskShotPhysics = 0x2C3011;
  public const ulong MaskShotNoPlayers = 0x203011;

  private const float MaxWorldCoord = 131072f;
  private const float MaxDistance = 8192f;

  private static readonly Dictionary<ulong, TraceOptions> _options = new();
  private static Vector? _start;
  private static Vector? _end;
  private static bool _disabled;

  public static string? LastError { get; private set; }

  public static bool Available => !_disabled;

  public readonly record struct TraceHit(
    System.Numerics.Vector3 EndPos,
    System.Numerics.Vector3 Normal,
    IntPtr Entity,
    float Fraction)
  {
    public bool DidHit => Fraction < 1f;
  }

  private static void Log(string message)
  {
    Console.WriteLine($"[NativeTrace] {message}");
    try { Server.PrintToConsole($"[NativeTrace] {message}\n"); } catch { }
  }

  private static void SelfDisable(string reason)
  {
    _disabled = true;
    LastError = reason;
    Log($"DEVRE DISI: {reason}");
  }

  private static TraceOptions Options(ulong mask)
  {
    if (!_options.TryGetValue(mask, out var options))
    {
      options = new TraceOptions { InteractsWith = (Contents)mask };
      _options[mask] = options;
    }

    return options;
  }

  private static System.Numerics.Vector3 ToNumerics(Vector v) => new(v.X, v.Y, v.Z);

  private static bool IsSane(float fraction, System.Numerics.Vector3 end)
  {
    if (!float.IsFinite(fraction) || fraction < 0f || fraction > 1f)
      return false;

    if (!float.IsFinite(end.X) || !float.IsFinite(end.Y) || !float.IsFinite(end.Z))
      return false;

    return Math.Abs(end.X) <= MaxWorldCoord && Math.Abs(end.Y) <= MaxWorldCoord && Math.Abs(end.Z) <= MaxWorldCoord;
  }

  private static TraceHit? Trace(CCSPlayerPawn pawn, System.Numerics.Vector3 startPos, System.Numerics.Vector3 endPos, ulong mask)
  {
    if (_disabled)
      return null;

    TraceResult result;
    try
    {
      _start ??= new Vector();
      _end ??= new Vector();
      _start.X = startPos.X; _start.Y = startPos.Y; _start.Z = startPos.Z;
      _end.X = endPos.X; _end.Y = endPos.Y; _end.Z = endPos.Z;
      result = CounterStrikeSharp.API.Modules.Utils.Trace.TraceEndShape(_start, _end, pawn, Options(mask));
    }
    catch (Exception ex)
    {
      SelfDisable($"CounterStrikeSharp Trace API cagrisi basarisiz (v1.0.372+ gerekli): {ex.Message}");
      return null;
    }

    var end = ToNumerics(result.EndPos);
    if (!IsSane(result.Fraction, end))
    {
      SelfDisable("CounterStrikeSharp Trace API gecersiz sonuc dondurdu.");
      return null;
    }

    return new TraceHit(end, ToNumerics(result.Normal), result.HitEntity().Handle, result.Fraction);
  }

  public static TraceHit? TraceRay(CCSPlayerPawn pawn, System.Numerics.Vector3 startPos, System.Numerics.Vector3 endPos, ulong mask = MaskShotPhysics)
    => Trace(pawn, startPos, endPos, mask);

  public static System.Numerics.Vector3? TraceLine(CCSPlayerPawn pawn, System.Numerics.Vector3 startPos, System.Numerics.Vector3 endPos, ulong mask = MaskShotPhysics)
  {
    var hit = Trace(pawn, startPos, endPos, mask);
    return hit is { DidHit: true } ? hit.Value.EndPos : null;
  }

  public static System.Numerics.Vector3? TraceFromEyes(CCSPlayerPawn pawn, ulong mask = MaskShotPhysics)
  {
    var absOrigin = pawn.AbsOrigin;
    if (absOrigin == null)
      return null;

    var eye = new System.Numerics.Vector3(absOrigin.X, absOrigin.Y, absOrigin.Z + pawn.ViewOffset.Z);
    QAngle angles = pawn.EyeAngles;
    float pitch = angles.X * MathF.PI / 180f;
    float yaw = angles.Y * MathF.PI / 180f;
    var forward = new System.Numerics.Vector3(
      MathF.Cos(pitch) * MathF.Cos(yaw),
      MathF.Cos(pitch) * MathF.Sin(yaw),
      -MathF.Sin(pitch));

    var hit = Trace(pawn, eye, eye + forward * MaxDistance, mask);
    return hit is { DidHit: true } ? hit.Value.EndPos : null;
  }
}
