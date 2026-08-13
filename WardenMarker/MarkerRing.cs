using System.Drawing;
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace WardenMarker;

public class Marker
{
  public int OwnerSlot;
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

  public static Marker Create(int ownerSlot, System.Numerics.Vector3 center, MarkerSettings settings, WardenMarkerConfig config)
  {
    var marker = new Marker { OwnerSlot = ownerSlot, Center = center };

    float radius = settings.Ring.Size;
    var color = Resolve(settings.Ring.Color);
    var origin = new Vector(center.X, center.Y, center.Z);

    if (settings.Disc.Enabled)
    {
      var disc = SpawnDisc(origin, radius, settings.Disc.Alpha);
      if (disc != null)
      {
        marker.Disc = disc.Index;

        if (settings.Disc.Glow && config.Disc.Glow)
        {
          var glow = SpawnGlow(origin, radius, color, config.Disc.GlowRange);
          if (glow != null)
            marker.Glow = glow.Index;
        }
      }
    }

    SpawnCircle(marker, origin, radius, OuterSegments, color, settings.Ring.Width);
    SpawnCircle(marker, origin, radius * 0.5f, InnerSegments, color, settings.Ring.Width);

    return marker;
  }

  public static void Destroy(Marker marker)
  {
    foreach (uint index in marker.Beams)
    {
      var beam = Utilities.GetEntityFromIndex<CEnvBeam>((int)index);
      if (beam != null && beam.IsValid && beam.DesignerName == "env_beam")
        beam.Remove();
    }
    marker.Beams.Clear();

    RemoveProp(marker.Glow);
    RemoveProp(marker.Disc);
    marker.Glow = 0;
    marker.Disc = 0;
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

  private static void RemoveProp(uint index)
  {
    if (index == 0)
      return;

    var prop = Utilities.GetEntityFromIndex<CDynamicProp>((int)index);
    if (prop != null && prop.IsValid && prop.DesignerName == "prop_dynamic")
      prop.Remove();
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

    disc.SetModel(DiscModel);
    disc.Teleport(new Vector(center.X, center.Y, center.Z + 1f), new QAngle(), new Vector());
    disc.DispatchSpawn();

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

    glow.SetModel(DiscModel);
    glow.Spawnflags = 256u;
    glow.Render = Color.FromArgb(1, 255, 255, 255);
    glow.Teleport(new Vector(center.X, center.Y, center.Z + 1f), new QAngle(), new Vector());
    glow.DispatchSpawn();

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
        beam.Width = width;
        beam.Render = color;
        beam.SetModel(BeamSprite);
        beam.Teleport(previous, new QAngle(), new Vector());

        beam.EndPos.X = next.X;
        beam.EndPos.Y = next.Y;
        beam.EndPos.Z = next.Z;
        Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");

        marker.Beams.Add(beam.Index);
      }

      previous = next;
    }
  }

  private static Vector PointOn(Vector center, float radius, float angle) =>
    new(center.X + MathF.Cos(angle) * radius,
        center.Y + MathF.Sin(angle) * radius,
        center.Z + RingHeight);
}
