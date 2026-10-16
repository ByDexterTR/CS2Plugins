using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class ColoredModel : VipModule
{
    public override string Name => "ColoredModel";
    public override string DisplayName => Core.Localizer["vip.module.coloredmodel"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    [JsonConverter(typeof(CfgConverter))]
    private class Cfg
    {
        public string Team { get; set; } = "";
        public List<string> Colors { get; set; } = new();
    }

    private class CfgConverter : JsonConverter<Cfg>
    {
        public override Cfg Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            var cfg = new Cfg();
            if (root.ValueKind == JsonValueKind.Array)
            {
                cfg.Colors = Strings(root);
                return cfg;
            }

            if (root.ValueKind != JsonValueKind.Object)
                return cfg;

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.NameEquals("team") && prop.Value.ValueKind == JsonValueKind.String)
                    cfg.Team = prop.Value.GetString() ?? "";
                else if (prop.NameEquals("colors") && prop.Value.ValueKind == JsonValueKind.Array)
                    cfg.Colors = Strings(prop.Value);
            }
            return cfg;
        }

        private static List<string> Strings(JsonElement array) =>
            array.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString() ?? "").ToList();

        public override void Write(Utf8JsonWriter writer, Cfg value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, new { team = value.Team, colors = value.Colors });
    }

    private static readonly Cfg DefaultCfg = new();

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player) =>
        TrailBeam.ParseColorOptions((GroupValue<Cfg>(player) ?? DefaultCfg).Colors);

    public override void OnSelect(CCSPlayerController player, string value)
    {
        if ((uint)player.Slot < 64)
            _external[player.Slot] = false;
    }

    private const int RgbMask = 0xFFFFFF;

    private readonly bool[] _applied = new bool[64];
    private readonly int[] _lastArgb = new int[64];
    private readonly bool[] _external = new bool[64];

    public override void OnLoad() => Core.HookTick(OnTick, 8);

    public override void OnUnload()
    {
        for (int slot = 0; slot < 64; slot++)
            if (_applied[slot])
                Reset(slot);
    }

    private void OnTick()
    {
        foreach (var player in Core.Players)
        {
            if (player == null || !player.IsValid || player.IsBot)
                continue;

            int slot = player.Slot;

            if (!IsAlive(player) || !Active(player) || !TeamMatches(player, (GroupValue<Cfg>(player) ?? DefaultCfg).Team))
            {
                if (_applied[slot])
                {
                    _applied[slot] = false;
                    Reset(slot);
                }
                _external[slot] = false;
                continue;
            }

            if (_external[slot])
                continue;

            var pawn = PlayerCache.Pawn(player);
            if (pawn == null || !pawn.IsValid)
                continue;

            var current = pawn.Render;
            if (_applied[slot] && (current.ToArgb() & RgbMask) != (_lastArgb[slot] & RgbMask))
            {
                _external[slot] = true;
                _applied[slot] = false;
                continue;
            }

            string setting = Setting(player);
            var color = TrailBeam.IsRandom(setting) ? Core.RoundColor(slot) : TrailBeam.Resolve(setting);
            var target = System.Drawing.Color.FromArgb(InvisPool.Alpha(slot), color.R, color.G, color.B);

            _applied[slot] = true;
            if (current.ToArgb() == target.ToArgb())
            {
                _lastArgb[slot] = target.ToArgb();
                continue;
            }

            Paint(pawn, target);
            _lastArgb[slot] = target.ToArgb();
        }
    }

    private static void Paint(CCSPlayerPawn pawn, System.Drawing.Color target)
    {
        pawn.AcceptInput("Color", pawn, pawn, $"{target.R} {target.G} {target.B}");
        if (pawn.Render.ToArgb() == target.ToArgb())
            return;

        pawn.Render = target;
        NetState.Changed(pawn, "CBaseModelEntity", "m_clrRender");
    }

    private static void Reset(int slot)
    {
        var pawn = PlayerCache.Pawn(Utilities.GetPlayerFromSlot(slot));
        if (pawn == null || !pawn.IsValid)
            return;

        Paint(pawn, System.Drawing.Color.FromArgb(InvisPool.Alpha(slot), 255, 255, 255));
    }
}
