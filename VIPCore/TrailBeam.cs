using System.Drawing;
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public static class TrailBeam
{
    public const string Sprite = "materials/sprites/laserbeam.vmat";

    private const float RainbowDegreesPerSecond = 60f;

    public static Color Resolve(string color)
    {
        if (string.IsNullOrWhiteSpace(color) || color.Equals("rainbow", StringComparison.OrdinalIgnoreCase))
            return FromHue(Server.CurrentTime * RainbowDegreesPerSecond % 360.0);

        if (color.StartsWith('#') && color.Length == 7
            && int.TryParse(color.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hr)
            && int.TryParse(color.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hg)
            && int.TryParse(color.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hb))
            return Color.FromArgb(255, hr, hg, hb);

        var parts = color.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 3 && int.TryParse(parts[0], out var r) && int.TryParse(parts[1], out var g) && int.TryParse(parts[2], out var b))
            return Color.FromArgb(255, r, g, b);

        return Color.White;
    }

    public static bool IsRandom(string value) =>
        value.Equals("random", StringComparison.OrdinalIgnoreCase);

    public static Color RandomColor() =>
        FromHue(Random.Shared.NextDouble() * 360.0);

    public static List<VipFeatureOption> ParseColorOptions(List<string> colors)
    {
        var options = new List<VipFeatureOption>();
        foreach (var entry in colors)
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            var tokens = entry.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 0)
                continue;

            string value = tokens[^1];
            string name = tokens.Length > 1 ? string.Join(' ', tokens[..^1]) : value;
            options.Add(new VipFeatureOption(name, value));
        }
        return options;
    }

    public static void Create(BasePlugin plugin, Vector from, Vector to, Color color, float width, float lifetime,
        int hideModule = -1, int ownerSlot = -1)
    {
        if (hideModule >= 0 && !EffectHide.AnyViewer(hideModule, ownerSlot))
            return;

        var beam = Utilities.CreateEntityByName<CEnvBeam>("env_beam");
        if (beam == null || !beam.IsValid)
            return;

        if (hideModule >= 0)
            EffectHide.Track(hideModule, beam.Index, ownerSlot);

        beam.Width = width;
        beam.Render = color;
        beam.SetModel(Sprite);
        beam.Teleport(from, new QAngle(), new Vector());

        beam.EndPos.X = to.X;
        beam.EndPos.Y = to.Y;
        beam.EndPos.Z = to.Z;
        Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");

        plugin.AddTimer(lifetime, () =>
        {
            if (beam.IsValid)
                beam.Remove();
        });
    }

    public static float Distance(Vector a, Vector b)
    {
        float dx = a.X - b.X, dy = a.Y - b.Y, dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static Color FromHue(double h)
    {
        double x = 1 - Math.Abs(h / 60.0 % 2 - 1);
        double r = 0, g = 0, b = 0;
        if (h < 60) { r = 1; g = x; }
        else if (h < 120) { r = x; g = 1; }
        else if (h < 180) { g = 1; b = x; }
        else if (h < 240) { g = x; b = 1; }
        else if (h < 300) { r = x; b = 1; }
        else { r = 1; b = x; }
        return Color.FromArgb(255, (int)(r * 255), (int)(g * 255), (int)(b * 255));
    }
}
