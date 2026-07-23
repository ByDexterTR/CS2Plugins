using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public static class EffectHide
{
    public const int BulletTrail = 0;
    public const int C4Effect = 1;
    public const int KillEffect = 2;
    public const int PlayerTrail = 3;
    public const int PlayerGlow = 4;
    public const int GrenadeTrail = 5;
    public const int SaySound = 6;
    public const int ModuleCount = 7;

    public static readonly string[] Names =
    {
        "BulletTrail", "C4Effect", "KillEffect", "PlayerTrail", "PlayerGlow", "GrenadeTrail", "SaySound"
    };

    public const byte ModeAll = 0;
    public const byte ModeSelf = 1;
    public const byte ModeOff = 2;

    private static VIPCore? _owner;
    private static readonly byte[,] _mode = new byte[64, ModuleCount];
    private static readonly bool[] _viewerHasTransmitPref = new bool[64];
    private static readonly bool[] _locked = new bool[ModuleCount];
    private static int _transmitNonDefault;
    private static readonly Dictionary<uint, (int Module, int OwnerSlot)> _entities = new();

    public static bool Locked(int module) => _locked[module];

    public static void Ensure(VIPCore core)
    {
        if (ReferenceEquals(_owner, core))
            return;

        _owner = core;
        _entities.Clear();
        Array.Clear(_mode);
        Array.Clear(_viewerHasTransmitPref);
        _transmitNonDefault = 0;

        for (int m = 0; m < ModuleCount; m++)
            _locked[m] = core.HideDefault(Names[m]).Equals("off", StringComparison.OrdinalIgnoreCase);

        core.RegisterListener<CheckTransmit>(OnCheckTransmit);
        core.RegisterListener<OnEntityDeleted>(entity =>
        {
            if (_entities.Count > 0)
                _entities.Remove(entity.Index);
        });
        core.RegisterEventHandler<EventPlayerConnectFull>((ev, _) => { LoadPrefs(ev.Userid); return HookResult.Continue; });
        core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) =>
        {
            int slot = ev.Userid?.Slot ?? -1;
            if (slot >= 0 && slot < 64)
                for (int m = 0; m < ModuleCount; m++)
                    SetModeInternal(slot, m, ModeAll);
            return HookResult.Continue;
        });

        try
        {
            foreach (var player in Utilities.GetPlayers())
                LoadPrefs(player);
        }
        catch { }
    }

    public static byte Mode(int slot, int module) =>
        slot >= 0 && slot < 64 ? _mode[slot, module] : ModeAll;

    public static bool AnyViewer(int module, int ownerSlot)
    {
        var core = _owner;
        if (core == null || _locked[module] || _transmitNonDefault == 0)
            return true;

        foreach (var viewer in core.Players)
        {
            if (viewer == null || !viewer.IsValid || viewer.IsBot || viewer.Slot >= 64)
                continue;

            byte mode = _mode[viewer.Slot, module];
            if (mode == ModeAll)
                return true;
            if (mode == ModeSelf && viewer.Slot == ownerSlot)
                return true;
        }

        return false;
    }

    public static void Track(int module, uint entityIndex, int ownerSlot)
    {
        if (_owner != null)
            _entities[entityIndex] = (module, ownerSlot);
    }

    public static void LoadPrefs(CCSPlayerController? player)
    {
        var core = _owner;
        if (core == null || player == null || !player.IsValid || player.IsBot || player.Slot >= 64)
            return;

        for (int m = 0; m < ModuleCount; m++)
        {
            if (_locked[m])
            {
                SetModeInternal(player.Slot, m, ModeAll);
                continue;
            }

            string raw = core.GetSetting(player.SteamID, "HideVip@" + Names[m]);
            if (raw is not ("all" or "self" or "hidden"))
                raw = core.HideDefault(Names[m]);

            SetModeInternal(player.Slot, m, Parse(raw));
        }
    }

    public static void SetMode(CCSPlayerController player, int module, byte mode)
    {
        var core = _owner;
        if (core == null || player.Slot >= 64 || _locked[module])
            return;

        SetModeInternal(player.Slot, module, mode);
        core.SetSetting(player, "HideVip@" + Names[module],
            mode == ModeSelf ? "self" : mode == ModeOff ? "hidden" : "all");
    }

    private static byte Parse(string value) => value switch
    {
        "self" => ModeSelf,
        "hidden" => ModeOff,
        _ => ModeAll
    };

    private static void SetModeInternal(int slot, int module, byte mode)
    {
        byte old = _mode[slot, module];
        if (old == mode)
            return;

        _mode[slot, module] = mode;

        if (module == SaySound)
            return;

        bool wasNonDefault = old != ModeAll;
        bool isNonDefault = mode != ModeAll;
        if (wasNonDefault != isNonDefault)
            _transmitNonDefault += isNonDefault ? 1 : -1;

        bool any = false;
        for (int m = 0; m < ModuleCount; m++)
        {
            if (m != SaySound && _mode[slot, m] != ModeAll)
            {
                any = true;
                break;
            }
        }
        _viewerHasTransmitPref[slot] = any;
    }

    private static void OnCheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
    {
        if (_transmitNonDefault == 0 || _entities.Count == 0)
            return;

        foreach (var (info, player) in infoList)
        {
            if (player == null || !player.IsValid || player.Slot >= 64 || !_viewerHasTransmitPref[player.Slot])
                continue;

            int viewerSlot = player.Slot;

            foreach (var (index, entry) in _entities)
            {
                byte mode = _mode[viewerSlot, entry.Module];
                if (mode == ModeAll)
                    continue;
                if (mode == ModeSelf && entry.OwnerSlot == viewerSlot)
                    continue;

                if (info.TransmitEntities.Contains(index))
                    info.TransmitEntities.Remove(index);
            }
        }
    }
}
