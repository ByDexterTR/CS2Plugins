using System.Drawing;
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public static class DecoyRing
{
    public const string DiscModel = "models/dev/grenade_trajectory/grenade_target.vmdl";

    private const float DiscBaseRadius = 13.462f;
    private const int DiscAlpha = 178;
    private const int OuterSegments = 24;
    private const int InnerSegments = 18;
    private const float RingHeight = 3f;

    private class Ring
    {
        public uint Disc;
        public readonly List<uint> Beams = new();
    }

    private static readonly Dictionary<int, Ring> _rings = new();

    public static void Show(VIPCore core, int key, Vector center, float radius, Color color)
    {
        if (radius <= 0f || _rings.ContainsKey(key))
            return;

        var ring = new Ring();
        _rings[key] = ring;

        SpawnDisc(center, radius, ring);
        SpawnRing(core, center, radius, OuterSegments, color, ring);
        SpawnRing(core, center, radius * 0.5f, InnerSegments, color, ring);
    }

    public static void Hide(int key)
    {
        if (!_rings.Remove(key, out var ring))
            return;

        foreach (uint index in ring.Beams)
        {
            var beam = Utilities.GetEntityFromIndex<CEnvBeam>((int)index);
            if (beam != null && beam.IsValid && beam.DesignerName == "env_beam")
                beam.Remove();
        }

        if (ring.Disc != 0)
        {
            var disc = Utilities.GetEntityFromIndex<CDynamicProp>((int)ring.Disc);
            if (disc != null && disc.IsValid && disc.DesignerName == "prop_dynamic")
                disc.Remove();
        }
    }

    public static void HideAll()
    {
        foreach (int key in _rings.Keys.ToList())
            Hide(key);
    }

    private static void SpawnDisc(Vector center, float radius, Ring ring)
    {
        var disc = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (disc == null || !disc.IsValid)
            return;

        ring.Disc = disc.Index;

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

        disc.Render = Color.FromArgb(DiscAlpha, 255, 255, 255);
        Utilities.SetStateChanged(disc, "CBaseModelEntity", "m_clrRender");

        float scale = radius / DiscBaseRadius;
        var skeleton = disc.CBodyComponent?.SceneNode?.GetSkeletonInstance();
        if (skeleton != null)
        {
            skeleton.Scale = scale;
            disc.AcceptInput("SetScale", null, null, scale.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void SpawnRing(VIPCore core, Vector center, float radius, int segments, Color color, Ring ring)
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
                beam.Width = 1.2f;
                beam.Render = color;
                beam.SetModel(TrailBeam.Sprite);
                beam.Teleport(previous, new QAngle(), new Vector());

                beam.EndPos.X = next.X;
                beam.EndPos.Y = next.Y;
                beam.EndPos.Z = next.Z;
                Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");

                ring.Beams.Add(beam.Index);
            }

            previous = next;
        }
    }

    private static Vector PointOn(Vector center, float radius, float angle) =>
        new(center.X + MathF.Cos(angle) * radius,
            center.Y + MathF.Sin(angle) * radius,
            center.Z + RingHeight);
}
