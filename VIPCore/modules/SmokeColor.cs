using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class SmokeColor : VipModule
{
    public override string Name => "SmokeColor";
    public override string DisplayName => Core.Localizer["vip.module.smokecolor"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var colors = GroupValue<List<string>>(player) ?? new();
        var options = new List<VipFeatureOption>();
        foreach (var entry in colors)
            if (TryParseEntry(entry, out var name, out var hex))
                options.Add(new VipFeatureOption(name, hex));
        return options;
    }

    public override void OnLoad() => Core.RegisterListener<OnEntitySpawned>(OnEntitySpawned);

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (entity == null || !entity.IsValid || entity.DesignerName != "smokegrenade_projectile")
            return;

        var smoke = entity.As<CSmokeGrenadeProjectile>();
        Server.NextFrame(() =>
        {
            if (smoke == null || !smoke.IsValid)
                return;

            var controller = smoke.Thrower.Value?.Controller.Value?.As<CCSPlayerController>();
            if (!Active(controller))
                return;

            if (Core.IsActive(controller!, "SmokeEffect"))
                return;

            string setting = Setting(controller!);
            int r, g, b;
            if (TrailBeam.IsRandom(setting))
            {
                var color = Core.RoundColor(controller!.Slot);
                r = color.R;
                g = color.G;
                b = color.B;
            }
            else if (!TryParseHex(setting, out r, out g, out b))
                return;

            smoke.SmokeColor.X = r;
            smoke.SmokeColor.Y = g;
            smoke.SmokeColor.Z = b;
        });
    }

    private static bool TryParseEntry(string entry, out string name, out string hex)
    {
        name = "";
        hex = "";
        if (string.IsNullOrWhiteSpace(entry))
            return false;

        var tokens = entry.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0 || (!tokens[^1].StartsWith('#') && !TrailBeam.IsRandom(tokens[^1])))
            return false;

        hex = tokens[^1];
        name = tokens.Length > 1 ? string.Join(' ', tokens[..^1]) : hex;
        return true;
    }

    private static bool TryParseHex(string hex, out int r, out int g, out int b)
    {
        r = g = b = 0;
        hex = hex.TrimStart('#');
        if (hex.Length != 6)
            return false;

        return int.TryParse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r)
            && int.TryParse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)
            && int.TryParse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
    }
}
