using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace ScreenText;

public class ScreenTextElement
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("size")]
    public float Size { get; set; } = 32f;

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#FFFFFF";

    [JsonPropertyName("justify")]
    public string Justify { get; set; } = "left";

    [JsonPropertyName("background")]
    public bool Background { get; set; }
}

public class ScreenTextConfig : BasePluginConfig
{
    [JsonPropertyName("screentext_cmd")]
    public string Commands { get; set; } = "css_hidetext";

    [JsonPropertyName("screentext_default_on")]
    public bool DefaultOn { get; set; } = true;

    [JsonPropertyName("screentext_font")]
    public string Font { get; set; } = "Arial Bold";

    [JsonPropertyName("screentext_forward")]
    public float Forward { get; set; } = 7f;

    [JsonPropertyName("screentext_units_per_px")]
    public float UnitsPerPx { get; set; } = 0.012f;

    [JsonPropertyName("screentext_texts")]
    public List<ScreenTextElement> Texts { get; set; } = new()
    {
        new ScreenTextElement { Text = "github.com/ByDexterTR", X = -6.4f, Y = 1.3f, Size = 32f, Color = "#FFFFFF", Justify = "left" },
        new ScreenTextElement { Text = "bydexter.com", X = 6.4f, Y = 2.3f, Size = 32f, Color = "#7CFC00", Justify = "right" }
    };
}

public class ScreenText : BasePlugin, IPluginConfig<ScreenTextConfig>
{
    public override string ModuleName => "ScreenText";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

    private string ChatPrefix => Localizer["chat_prefix"];

    public ScreenTextConfig Config { get; set; } = new();

    private const int MaxSlots = 64;

    private readonly List<(CPointWorldText Ent, ScreenTextElement El)>[] _texts = new List<(CPointWorldText, ScreenTextElement)>[MaxSlots];
    private readonly bool[] _hidden = new bool[MaxSlots];
    private readonly float[] _lastPitch = new float[MaxSlots];
    private readonly float[] _lastYaw = new float[MaxSlots];
    private readonly float[] _lastEyeZ = new float[MaxSlots];
    private readonly HashSet<ulong> _savedHidden = new();
    private readonly object _saveLock = new();

    private string SavePath => Path.Combine(ModuleDirectory, "ScreenText.json");

    public void OnConfigParsed(ScreenTextConfig config)
    {
        if (config.Forward < 1f)
            config.Forward = 7f;
        if (config.UnitsPerPx <= 0f)
            config.UnitsPerPx = 0.012f;
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        for (int slot = 0; slot < MaxSlots; slot++)
            _texts[slot] = new List<(CPointWorldText, ScreenTextElement)>();

        LoadSaved();

        foreach (var name in Config.Commands.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            AddCommand(name, "Ekran yazilarini acar/kapatir", OnToggleCommand);

        RegisterListener<OnTick>(OnTick);
        RegisterListener<CheckTransmit>(OnCheckTransmit);
        RegisterListener<OnMapEnd>(OnMapEnd);

        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

        if (hotReload)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
                    continue;

                _hidden[player.Slot] = IsHiddenFor(player.SteamID);
                CreateTexts(player);
            }
        }
    }

    public override void Unload(bool hotReload)
    {
        for (int slot = 0; slot < MaxSlots; slot++)
            RemoveTexts(slot);
    }

    private void LoadSaved()
    {
        try
        {
            if (!File.Exists(SavePath))
                return;

            var list = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(SavePath));
            if (list == null)
                return;

            foreach (var entry in list)
            {
                if (ulong.TryParse(entry, out var steamId))
                    _savedHidden.Add(steamId);
            }
        }
        catch
        {
        }
    }

    private bool IsHiddenFor(ulong steamId)
    {
        return !Config.DefaultOn || _savedHidden.Contains(steamId);
    }

    private void SaveAsync()
    {
        List<string> snapshot;
        lock (_saveLock)
            snapshot = _savedHidden.Select(id => id.ToString()).ToList();

        var path = SavePath;
        Task.Run(() =>
        {
            try
            {
                lock (_saveLock)
                    File.WriteAllText(path, JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
            }
        });
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
            return HookResult.Continue;

        _hidden[player.Slot] = IsHiddenFor(player.SteamID);
        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
            return HookResult.Continue;

        int slot = player.Slot;
        Server.NextFrame(() =>
        {
            var p = Utilities.GetPlayerFromSlot(slot);
            if (p != null && p.IsValid)
                CreateTexts(p);
        });
        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        int slot = @event.Userid?.Slot ?? -1;
        if (slot >= 0 && slot < MaxSlots)
            RemoveTexts(slot);
        return HookResult.Continue;
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        int slot = @event.Userid?.Slot ?? -1;
        if (slot >= 0 && slot < MaxSlots)
            RemoveTexts(slot);
        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        int slot = @event.Userid?.Slot ?? -1;
        if (slot >= 0 && slot < MaxSlots)
        {
            RemoveTexts(slot);
            _hidden[slot] = !Config.DefaultOn;
        }
        return HookResult.Continue;
    }

    private void OnMapEnd()
    {
        for (int slot = 0; slot < MaxSlots; slot++)
            _texts[slot].Clear();
    }

    private void OnToggleCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        int slot = player.Slot;
        _hidden[slot] = !_hidden[slot];

        lock (_saveLock)
        {
            if (_hidden[slot])
                _savedHidden.Add(player.SteamID);
            else
                _savedHidden.Remove(player.SteamID);
        }
        SaveAsync();

        if (_hidden[slot])
            RemoveTexts(slot);
        else
            CreateTexts(player);

        string key = _hidden[slot] ? "screentext.disabled" : "screentext.enabled";
        player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer[key]}");
    }

    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        foreach ((CCheckTransmitInfo info, CCSPlayerController? viewer) in infoList)
        {
            if (viewer == null || !viewer.IsValid)
                continue;

            int viewerSlot = viewer.Slot;
            for (int slot = 0; slot < MaxSlots; slot++)
            {
                if (slot == viewerSlot)
                    continue;

                var list = _texts[slot];
                if (list == null || list.Count == 0)
                    continue;

                foreach (var (ent, _) in list)
                {
                    if (ent.IsValid)
                        info.TransmitEntities.Remove(ent);
                }
            }
        }
    }

    private void CreateTexts(CCSPlayerController player)
    {
        int slot = player.Slot;
        RemoveTexts(slot);

        if (_hidden[slot] || Config.Texts.Count == 0)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE || pawn.AbsOrigin == null)
            return;

        foreach (var element in Config.Texts)
        {
            var ent = CreateWorldText(element);
            if (ent == null)
                continue;

            ent.AcceptInput("SetParent", pawn, null, "!activator");
            _texts[slot].Add((ent, element));
        }

        PlaceTexts(slot, pawn, true);
    }

    private void OnTick()
    {
        for (int slot = 0; slot < MaxSlots; slot++)
        {
            var list = _texts[slot];
            if (list == null || list.Count == 0)
                continue;

            var player = Utilities.GetPlayerFromSlot(slot);
            var pawn = player?.PlayerPawn.Value;
            if (player == null || pawn == null || !pawn.IsValid || pawn.AbsOrigin == null
                || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            {
                RemoveTexts(slot);
                continue;
            }

            PlaceTexts(slot, pawn, false);
        }
    }

    private void PlaceTexts(int slot, CCSPlayerPawn pawn, bool force)
    {
        var ea = pawn.EyeAngles;
        float eyeZ = pawn.ViewOffset?.Z ?? 64f;

        if (!force
            && MathF.Abs(_lastPitch[slot] - ea.X) < 0.01f
            && MathF.Abs(_lastYaw[slot] - ea.Y) < 0.01f
            && MathF.Abs(_lastEyeZ[slot] - eyeZ) < 0.01f)
            return;

        _lastPitch[slot] = ea.X;
        _lastYaw[slot] = ea.Y;
        _lastEyeZ[slot] = eyeZ;

        var origin = pawn.AbsOrigin!;

        double d2r = Math.PI / 180.0;
        double pitch = ea.X * d2r, yaw = ea.Y * d2r;
        double sp = Math.Sin(pitch), cp = Math.Cos(pitch), sy = Math.Sin(yaw), cy = Math.Cos(yaw);

        float fx = (float)(cp * cy), fy = (float)(cp * sy), fz = (float)(-sp);
        float rx = (float)sy, ry = (float)(-cy);
        float ux = (float)(sp * cy), uy = (float)(sp * sy), uz = (float)cp;

        float ex = origin.X, ey = origin.Y, ez = origin.Z + eyeZ;
        var angle = new QAngle(0f, ea.Y + 270f, 90f - ea.X);
        float forward = Config.Forward;

        foreach (var (ent, element) in _texts[slot])
        {
            if (!ent.IsValid)
                continue;

            var pos = new Vector(
                ex + fx * forward + rx * element.X + ux * element.Y,
                ey + fy * forward + ry * element.X + uy * element.Y,
                ez + fz * forward + uz * element.Y);

            ent.Teleport(pos, angle, null);
        }
    }

    private CPointWorldText? CreateWorldText(ScreenTextElement element)
    {
        var ent = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
        if (ent == null || ent.Handle == IntPtr.Zero)
            return null;

        ent.MessageText = element.Text;
        ent.Enabled = true;
        ent.Fullbright = true;
        ent.FontSize = element.Size;
        ent.WorldUnitsPerPx = Config.UnitsPerPx;
        if (!string.IsNullOrEmpty(Config.Font))
            ent.FontName = Config.Font;
        ent.Color = ParseColor(element.Color);
        ent.JustifyHorizontal = ParseJustify(element.Justify);
        ent.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_TOP;
        ent.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;
        ent.DrawBackground = element.Background;
        ent.BackgroundBorderHeight = 0.1f;
        ent.BackgroundBorderWidth = 0.1f;
        ent.DispatchSpawn();
        return ent;
    }

    private void RemoveTexts(int slot)
    {
        var list = _texts[slot];
        if (list == null || list.Count == 0)
            return;

        foreach (var (ent, _) in list)
        {
            if (ent.IsValid)
                ent.Remove();
        }
        list.Clear();
    }

    private static PointWorldTextJustifyHorizontal_t ParseJustify(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "center" => PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER,
            "right" => PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_RIGHT,
            _ => PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT
        };
    }

    private static Color ParseColor(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (value.StartsWith('#') && value.Length == 7
                && int.TryParse(value.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, null, out var hr)
                && int.TryParse(value.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, null, out var hg)
                && int.TryParse(value.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, null, out var hb))
                return Color.FromArgb(255, hr, hg, hb);

            var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 3 && int.TryParse(parts[0], out var r) && int.TryParse(parts[1], out var g) && int.TryParse(parts[2], out var b))
                return Color.FromArgb(255, Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
        }

        return Color.White;
    }
}

public static class CC
{
    public static char Default => '\x01';
    public static char Orchid => '\x0E';
}
