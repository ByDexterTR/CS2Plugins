using System.Drawing;
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace WardenMarker;

public class Marker
{
  public uint Disc;
  public uint Glow;
  public readonly List<uint> Beams = new();
  public System.Numerics.Vector3 Center;
}

public static class MarkerRing
{
  public const string DiscModel = "models/dev/grenade_trajectory/grenade_target.vmdl";
  public const string BeamSprite = "materials/sprites/laserbeam.vmat";

  private const float DiscBaseRadius = 13.462f;
  private const int OuterSegments = 24;
  private const int InnerSegments = 24;
  private const float RingHeight = 3f;

  public static Marker Create(System.Numerics.Vector3 center, MarkerSettings settings, WardenMarkerConfig config)
  {
    var marker = new Marker { Center = center };

    float radius = settings.Ring.Size;
    var color = Resolve(settings.Ring.Color);
    var origin = new Vector(center.X, center.Y, center.Z);

    if (settings.Disc.Enabled)
    {
      var disc = SpawnDisc(origin, radius, settings.Disc.Alpha);
      if (disc != null)
      {
        marker.Disc = disc.EntityHandle.Raw;

        if (settings.Disc.Glow && config.Disc.Glow)
        {
          var glow = SpawnGlow(origin, radius, color, config.Disc.GlowRange);
          if (glow != null)
            marker.Glow = glow.EntityHandle.Raw;
        }
      }
    }

    SpawnCircle(marker, origin, radius, OuterSegments, color, settings.Ring.Width);
    SpawnCircle(marker, origin, radius * 0.5f, InnerSegments, color, settings.Ring.Width);

    return marker;
  }

  public static void Destroy(Marker marker)
  {
    foreach (uint handle in marker.Beams)
      Get(handle)?.Remove();
    marker.Beams.Clear();

    Get(marker.Glow)?.Remove();
    Get(marker.Disc)?.Remove();
    marker.Glow = 0;
    marker.Disc = 0;
  }

  public static bool IsAlive(Marker marker)
  {
    if (marker.Beams.Count == 0 || (marker.Disc != 0 && Get(marker.Disc) == null) || (marker.Glow != 0 && Get(marker.Glow) == null))
      return false;

    foreach (uint handle in marker.Beams)
    {
      if (Get(handle) == null)
        return false;
    }

    return true;
  }

  public static bool Move(Marker marker, System.Numerics.Vector3 center)
  {
    var beams = new List<CEnvBeam>(marker.Beams.Count);
    foreach (uint handle in marker.Beams)
    {
      if (Get(handle) is not { } entity)
        return false;

      beams.Add(entity.As<CEnvBeam>());
    }

    var disc = Get(marker.Disc);
    var glow = Get(marker.Glow);
    if (beams.Count == 0 || (marker.Disc != 0 && disc == null) || (marker.Glow != 0 && glow == null))
      return false;

    var delta = center - marker.Center;
    foreach (var beam in beams)
    {
      var origin = beam.AbsOrigin;
      if (origin == null)
        return false;

      beam.Teleport(new Vector(origin.X + delta.X, origin.Y + delta.Y, origin.Z + delta.Z), new QAngle(), new Vector());
      beam.EndPos.X += delta.X;
      beam.EndPos.Y += delta.Y;
      beam.EndPos.Z += delta.Z;
      Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");
    }

    var propOrigin = new Vector(center.X, center.Y, center.Z + 1f);
    disc?.Teleport(propOrigin, new QAngle(), new Vector());
    glow?.Teleport(propOrigin, new QAngle(), new Vector());

    marker.Center = center;
    return true;
  }

  public static Color Resolve(string color)
  {
    if (color.StartsWith('#') && color.Length == 7
        && int.TryParse(color.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hr)
        && int.TryParse(color.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hg)
        && int.TryParse(color.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hb))
      return Color.FromArgb(255, hr, hg, hb);

    var parts = color.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 3 && int.TryParse(parts[0], out var r) && int.TryParse(parts[1], out var g) && int.TryParse(parts[2], out var b))
      return Color.FromArgb(255, Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));

    return Color.White;
  }

  private static CBaseEntity? Get(uint handle)
  {
    if (handle == 0)
      return null;

    var entity = new CHandle<CBaseEntity>(handle).Value;
    return entity != null && entity.IsValid ? entity : null;
  }

  private static CDynamicProp? SpawnDisc(Vector center, float radius, int alpha)
  {
    var disc = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
    if (disc == null || !disc.IsValid)
      return null;

    var node = disc.CBodyComponent?.SceneNode?.Owner?.Entity;
    if (node != null)
      node.Flags = (uint)(node.Flags & ~(1 << 2));

    var collision = disc.Collision;
    if (collision != null)
    {
      collision.SolidType = SolidType_t.SOLID_NONE;
      collision.SolidFlags = 12;
    }

    using (var keyValues = new CEntityKeyValues())
    {
      keyValues.SetString("model", DiscModel);
      keyValues.SetInt("solid", 0);
      disc.DispatchSpawn(keyValues);
    }

    disc.Teleport(new Vector(center.X, center.Y, center.Z + 1f), new QAngle(), new Vector());

    disc.Render = Color.FromArgb(Math.Clamp(alpha, 1, 255), 255, 255, 255);
    Utilities.SetStateChanged(disc, "CBaseModelEntity", "m_clrRender");

    Scale(disc, radius);
    return disc;
  }

  private static CDynamicProp? SpawnGlow(Vector center, float radius, Color color, int range)
  {
    var glow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
    if (glow == null || !glow.IsValid)
      return null;

    var node = glow.CBodyComponent?.SceneNode?.Owner?.Entity;
    if (node != null)
      node.Flags = (uint)(node.Flags & ~(1 << 2));

    var collision = glow.Collision;
    if (collision != null)
    {
      collision.SolidType = SolidType_t.SOLID_NONE;
      collision.SolidFlags = 12;
    }

    glow.Render = Color.FromArgb(1, 255, 255, 255);

    using (var keyValues = new CEntityKeyValues())
    {
      keyValues.SetString("model", DiscModel);
      keyValues.SetInt("solid", 0);
      keyValues.SetInt("spawnflags", 256);
      glow.DispatchSpawn(keyValues);
    }

    glow.Teleport(new Vector(center.X, center.Y, center.Z + 1f), new QAngle(), new Vector());

    glow.Glow.GlowColorOverride = color;
    glow.Glow.GlowRange = range;
    glow.Glow.GlowTeam = -1;
    glow.Glow.GlowType = 3;
    glow.Glow.GlowRangeMin = 0;

    Scale(glow, radius);
    return glow;
  }

  private static void Scale(CDynamicProp prop, float radius)
  {
    float scale = radius / DiscBaseRadius;
    var skeleton = prop.CBodyComponent?.SceneNode?.GetSkeletonInstance();
    if (skeleton == null)
      return;

    skeleton.Scale = scale;
    prop.AcceptInput("SetScale", null, null, scale.ToString(CultureInfo.InvariantCulture));
  }

  private static void SpawnCircle(Marker marker, Vector center, float radius, int segments, Color color, float width)
  {
    if (segments < 3 || radius <= 0f)
      return;

    float step = MathF.Tau / segments;
    var previous = PointOn(center, radius, 0f);

    for (int i = 1; i <= segments; i++)
    {
      var next = PointOn(center, radius, step * i);

      var beam = Utilities.CreateEntityByName<CEnvBeam>("env_beam");
      if (beam != null && beam.IsValid)
      {
        beam.DispatchSpawn();
        beam.AcceptInput("TurnOn");

        beam.SetModel(BeamSprite);
        beam.Width = width;
        Utilities.SetStateChanged(beam, "CBeam", "m_fWidth");
        beam.Render = color;
        Utilities.SetStateChanged(beam, "CBaseModelEntity", "m_clrRender");
        beam.Teleport(previous, new QAngle(), new Vector());

        beam.EndPos.X = next.X;
        beam.EndPos.Y = next.Y;
        beam.EndPos.Z = next.Z;
        Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");

        marker.Beams.Add(beam.EntityHandle.Raw);
      }

      previous = next;
    }
  }

  private static Vector PointOn(Vector center, float radius, float angle) =>
    new(center.X + MathF.Cos(angle) * radius,
        center.Y + MathF.Sin(angle) * radius,
        center.Z + RingHeight);
}
