using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using static CounterStrikeSharp.API.Core.Listeners;
using CoreMenuManager = CounterStrikeSharp.API.Modules.Menu.MenuManager;

namespace Speedometer;

public class Speedometer : BasePlugin
{
    public override string ModuleName => "Speedometer";
    public override string ModuleVersion => "1.0.1";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

    private string ChatPrefix => Localizer["chat_prefix"];

    private const int MaxSlots = 64;

    private static readonly (float Speed, byte R, byte G, byte B)[] ColorStops =
    {
        (0f, 255, 255, 255),
        (1000f, 0, 128, 255),
        (2000f, 255, 165, 0),
        (3000f, 255, 0, 0)
    };

    private readonly bool[] _enabled = new bool[MaxSlots];
    private int _enabledCount;

    private readonly HashSet<ulong> _savedDisabled = new();
    private readonly object _saveLock = new();
    private bool _lateInitDone;

    private string SavePath => Path.Combine(ModuleDirectory, "Speedometer.json");

    public override void Load(bool hotReload)
    {
        LoadSaved();

        RegisterListener<OnTick>(OnTick);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    }
    private void InitExistingPlayers()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
                continue;

            int slot = player.Slot;
            if (slot >= 0 && slot < MaxSlots && IsHudEnabledFor(player.SteamID) && !_enabled[slot])
            {
                _enabled[slot] = true;
                _enabledCount++;
            }
        }
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
                    _savedDisabled.Add(steamId);
            }
        }
        catch
        {
        }
    }

    private bool IsHudEnabledFor(ulong steamId)
    {
        return !_savedDisabled.Contains(steamId);
    }

    private void SaveAsync()
    {
        List<string> snapshot;
        lock (_saveLock)
            snapshot = _savedDisabled.Select(id => id.ToString()).ToList();

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

        int slot = player.Slot;
        if (slot >= 0 && slot < MaxSlots && IsHudEnabledFor(player.SteamID) && !_enabled[slot])
        {
            _enabled[slot] = true;
            _enabledCount++;
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        int slot = @event.Userid?.Slot ?? -1;
        if (slot >= 0 && slot < MaxSlots && _enabled[slot])
        {
            _enabled[slot] = false;
            _enabledCount--;
        }
        return HookResult.Continue;
    }

    [ConsoleCommand("css_hiz", "Hiz gostergesini acar/kapatir")]
    [ConsoleCommand("css_hız", "Hiz gostergesini acar/kapatir")]
    public void OnSpeedCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        int slot = player.Slot;
        if (slot < 0 || slot >= MaxSlots)
            return;

        _enabled[slot] = !_enabled[slot];
        _enabledCount += _enabled[slot] ? 1 : -1;

        lock (_saveLock)
        {
            if (_enabled[slot])
                _savedDisabled.Remove(player.SteamID);
            else
                _savedDisabled.Add(player.SteamID);
        }
        SaveAsync();

        player.PrintToChat(_enabled[slot]
            ? $" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["speedometer.enabled"]}"
            : $" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["speedometer.disabled"]}");
    }

    private void OnTick()
    {
        if (!_lateInitDone)
        {
            _lateInitDone = true;
            InitExistingPlayers();
        }

        if (_enabledCount == 0)
            return;

        for (int slot = 0; slot < MaxSlots; slot++)
        {
            if (!_enabled[slot])
                continue;

            var player = Utilities.GetPlayerFromSlot(slot);
            if (player == null || !player.IsValid)
                continue;

            if (CoreMenuManager.GetActiveMenu(player) != null)
                continue;

            var targetPawn = GetDisplayPawn(player);
            if (targetPawn == null)
                continue;

            var vel = targetPawn.AbsVelocity;
            float speed = MathF.Sqrt(vel.X * vel.X + vel.Y * vel.Y);

            player.PrintToCenterHtml($"<font color='{SpeedColor(speed)}' class='fontSize-l'><img src='https://raw.githubusercontent.com/ByDexterTR/CS2Plugins/refs/heads/main/img/speedometer.png'> {(int)speed} u/s</font>");
        }
    }

    private static CCSPlayerPawn? GetDisplayPawn(CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn != null && playerPawn.IsValid && playerPawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
            return playerPawn;

        var observerTarget = player.Pawn.Value?.ObserverServices?.ObserverTarget.Value;
        if (observerTarget == null || !observerTarget.IsValid || observerTarget.DesignerName != "player")
            return null;

        var targetPawn = observerTarget.As<CCSPlayerPawn>();
        return targetPawn.IsValid && targetPawn.LifeState == (byte)LifeState_t.LIFE_ALIVE ? targetPawn : null;
    }

    private static string SpeedColor(float speed)
    {
        var last = ColorStops[^1];
        if (speed >= last.Speed)
            return $"#{last.R:X2}{last.G:X2}{last.B:X2}";

        for (int i = 1; i < ColorStops.Length; i++)
        {
            if (speed > ColorStops[i].Speed)
                continue;

            var a = ColorStops[i - 1];
            var b = ColorStops[i];
            float t = (speed - a.Speed) / (b.Speed - a.Speed);

            byte r = (byte)(a.R + (b.R - a.R) * t);
            byte g = (byte)(a.G + (b.G - a.G) * t);
            byte bl = (byte)(a.B + (b.B - a.B) * t);

            return $"#{r:X2}{g:X2}{bl:X2}";
        }

        return "#FFFFFF";
    }
}

public static class CC
{
    public static char Default => '\x01';
    public static char Red => '\x07';
    public static char Orchid => '\x0E';
    public static char Green => '\x04';
}
