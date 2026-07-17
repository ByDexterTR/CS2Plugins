using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace ByDexter.Shared;

public static unsafe class NativeTrace
{
  public const ulong MaskShotPhysics = 0x2C3011;

  private const int TraceShapeVtableOffsetWindows = 3;
  private const int TraceShapeVtableOffsetLinux = 5;

  private const string SigFilterVtableWindows = "4C 8D 2D ? ? ? ? 24";
  private const string SigFilterVtableLinux = "48 8D 0D ? ? ? ? 66 89 95";

  private const float MaxWorldCoord = 131072f;

  [StructLayout(LayoutKind.Explicit, Size = 48)]
  private struct Ray
  {
    [FieldOffset(0)] public System.Numerics.Vector3 StartOffset;
    [FieldOffset(12)] public float Radius;
    [FieldOffset(40)] public int Type;
  }

  [StructLayout(LayoutKind.Explicit, Size = 72)]
  private struct TraceFilter
  {
    [FieldOffset(0x00)] public void* Vtable;
    [FieldOffset(0x08)] public ulong InteractsWith;
    [FieldOffset(0x10)] public ulong InteractsExclude;
    [FieldOffset(0x18)] public ulong InteractsAs;
    [FieldOffset(0x20)] public fixed uint OwnerIdsToIgnore[2];
    [FieldOffset(0x28)] public fixed uint EntityIdsToIgnore[2];
    [FieldOffset(0x30)] public fixed ushort HierarchyIds[2];
    [FieldOffset(0x34)] public byte ObjectSetMask;
    [FieldOffset(0x35)] public byte CollisionGroup;
    [FieldOffset(0x36)] public byte Bits;
    [FieldOffset(0x37)] public bool HitEntities;
    [FieldOffset(0x38)] public bool HitTriggers;
    [FieldOffset(0x39)] public bool TestHitboxes;
    [FieldOffset(0x3A)] public bool TraceComplexEntities;
    [FieldOffset(0x3B)] public bool OnlyHitIfHasPhysics;
    [FieldOffset(0x3C)] public bool IterateEntities;
  }

  [StructLayout(LayoutKind.Explicit, Size = 0xB8)]
  private struct GameTrace
  {
    [FieldOffset(0x78)] public System.Numerics.Vector3 StartPos;
    [FieldOffset(0x84)] public System.Numerics.Vector3 EndPos;
    [FieldOffset(0x9C)] public System.Numerics.Vector3 Position;
    [FieldOffset(0xAC)] public float Fraction;
  }

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate bool TraceShapeDelegate(IntPtr thisPtr, Ray* ray, IntPtr vecStart, IntPtr vecEnd, TraceFilter* filter, GameTrace* trace);

  private static TraceShapeDelegate? _traceShape;
  private static void* _filterVtable;
  private static bool _disabled;

  public static string? LastError { get; private set; }

  public static bool Available => _traceShape != null && !_disabled;

  private static void SelfDisable(string reason)
  {
    _disabled = true;
    LastError = reason;
    Console.WriteLine($"[NativeTrace] DEVRE DISI: {reason}");
  }

  private static bool EnsureInit()
  {
    if (_disabled) return false;
    if (_traceShape != null) return true;

    try
    {
      bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

      IntPtr vtable = NativeAPI.FindVirtualTable(Addresses.ServerPath, "CNavPhysicsInterface");
      if (vtable == IntPtr.Zero)
        throw new Exception("CNavPhysicsInterface vtable bulunamadi.");

      int offset = isLinux ? TraceShapeVtableOffsetLinux : TraceShapeVtableOffsetWindows;
      IntPtr fn = *(IntPtr*)(vtable + offset * sizeof(nint));
      if (fn == IntPtr.Zero)
        throw new Exception("TraceShape vtable girisi bos.");

      IntPtr filterSig = NativeAPI.FindSignature(Addresses.ServerPath,
        isLinux ? SigFilterVtableLinux : SigFilterVtableWindows);
      if (filterSig == IntPtr.Zero)
        throw new Exception("CTraceFilter vtable imzasi bulunamadi.");

      _filterVtable = (void*)GetAbsoluteAddress(filterSig, 3, 7);
      _traceShape = Marshal.GetDelegateForFunctionPointer<TraceShapeDelegate>(fn);
      Console.WriteLine("[NativeTrace] Native trace hazir (CNavPhysicsInterface).");
      return true;
    }
    catch (Exception ex)
    {
      SelfDisable(ex.Message);
      return false;
    }
  }

  private static IntPtr GetAbsoluteAddress(IntPtr addr, int offset, int size)
  {
    int code = *(int*)(addr + offset);
    return addr + code + size;
  }

  private static bool IsSane(GameTrace* trace)
  {
    if (!float.IsFinite(trace->Fraction) || trace->Fraction < 0f || trace->Fraction > 1f)
      return false;

    var p = trace->EndPos;
    if (!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z))
      return false;

    return Math.Abs(p.X) <= MaxWorldCoord && Math.Abs(p.Y) <= MaxWorldCoord && Math.Abs(p.Z) <= MaxWorldCoord;
  }

  private static System.Numerics.Vector3? Trace(CCSPlayerPawn pawn, Vector start, Vector end, ulong mask)
  {
    if (!EnsureInit())
      return null;

    ushort hierarchyId = 0xFFFF;
    try { hierarchyId = pawn.Collision.CollisionAttribute.HierarchyId; } catch { }

    TraceFilter* filter = stackalloc TraceFilter[1];
    *filter = default;
    filter->Vtable = _filterVtable;
    filter->InteractsWith = mask;
    filter->InteractsExclude = 0;
    filter->InteractsAs = 0;
    filter->OwnerIdsToIgnore[0] = 0xFFFFFFFF;
    filter->OwnerIdsToIgnore[1] = 0xFFFFFFFF;
    filter->EntityIdsToIgnore[0] = pawn.Index;
    filter->EntityIdsToIgnore[1] = 0xFFFFFFFF;
    filter->HierarchyIds[0] = hierarchyId;
    filter->HierarchyIds[1] = 0xFFFF;
    filter->ObjectSetMask = 7;
    filter->CollisionGroup = 4;
    filter->Bits = 0b01000001;
    filter->HitEntities = true;
    filter->HitTriggers = false;
    filter->TestHitboxes = true;
    filter->TraceComplexEntities = false;
    filter->OnlyHitIfHasPhysics = false;
    filter->IterateEntities = true;

    Ray* ray = stackalloc Ray[1];
    *ray = default;

    GameTrace* trace = stackalloc GameTrace[1];
    *trace = default;

    try
    {
      _traceShape!(IntPtr.Zero, ray, start.Handle, end.Handle, filter, trace);
    }
    catch (Exception ex)
    {
      SelfDisable($"TraceShape cagrisi basarisiz: {ex.Message}");
      return null;
    }

    if (!IsSane(trace))
    {
      SelfDisable("TraceShape gecersiz sonuc dondurdu (imza/offset kaymis olabilir).");
      return null;
    }

    if (trace->Fraction >= 1.0f)
      return null;

    return trace->EndPos;
  }

  public static System.Numerics.Vector3? TraceLine(CCSPlayerPawn pawn, System.Numerics.Vector3 startPos, System.Numerics.Vector3 endPos, ulong mask = MaskShotPhysics)
  {
    Vector start = new(startPos.X, startPos.Y, startPos.Z);
    Vector end = new(endPos.X, endPos.Y, endPos.Z);
    return Trace(pawn, start, end, mask);
  }

  public static System.Numerics.Vector3? TraceFromEyes(CCSPlayerPawn pawn, ulong mask = MaskShotPhysics)
  {
    var absOrigin = pawn.AbsOrigin;
    if (absOrigin == null)
      return null;

    Vector eye = new(absOrigin.X, absOrigin.Y, absOrigin.Z + pawn.ViewOffset.Z);
    QAngle eyeAngles = pawn.EyeAngles;
    Vector forward = new();
    NativeAPI.AngleVectors(eyeAngles.Handle, forward.Handle, 0, 0);
    Vector end = new(eye.X + forward.X * 8192f, eye.Y + forward.Y * 8192f, eye.Z + forward.Z * 8192f);

    return Trace(pawn, eye, end, mask);
  }
}
