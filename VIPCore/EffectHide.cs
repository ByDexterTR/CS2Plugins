using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

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
    public const int PlayerParticle = 7;
    public const int ModuleCount = 8;

    public static readonly string[] Names =
    {
        "BulletTrail", "C4Effect", "KillEffect", "PlayerTrail", "PlayerGlow", "GrenadeTrail", "SaySound", "PlayerParticle"
    };

    public const byte ModeAll = 0;
    public const byte ModeTeam = 1;
    public const byte ModeEnemy = 2;
    public const byte ModeSelf = 3;
    public const byte ModeOff = 4;
    public const byte ModeCount = 5;

    private static VIPCore? _owner;
    private static readonly byte[,] _mode = new byte[64, ModuleCount];
    private static readonly int[] _team = new int[64];
    private static readonly bool[] _locked = new bool[ModuleCount];
    private static int _transmitNonDefault;
    private static readonly Dictionary<uint, (int Module, int OwnerSlot)> _entities = new();
    private static readonly List<uint>[] _hidden = CreateBuckets();
    private static readonly byte[] _ownerMode = new byte[64];

    private static List<uint>[] CreateBuckets()
    {
        var buckets = new List<uint>[64];
        for (int i = 0; i < 64; i++)
            buckets[i] = new List<uint>();
        return buckets;
    }

    public static bool Locked(int module) => _locked[module];

    public static void Ensure(VIPCore core)
    {
        if (ReferenceEquals(_owner, core))
            return;

        _owner = core;
        _entities.Clear();
        Array.Clear(_mode);
        Array.Clear(_team);
        _transmitNonDefault = 0;

        for (int m = 0; m < ModuleCount; m++)
            _locked[m] = core.HideDefault(Names[m]).Equals("off", StringComparison.OrdinalIgnoreCase);

        core.HookTransmit(OnCheckTransmit);
        core.HookEntityDeleted(entity =>
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
        if (_owner == null || _locked[module] || ownerSlot < 0 || ownerSlot >= 64)
            return true;

        return _mode[ownerSlot, module] != ModeOff;
    }

    public static bool CanSee(byte mode, CCSPlayerController owner, CCSPlayerController viewer) =>
        CanSee(mode, owner.Slot, owner.TeamNum, viewer.Slot, viewer.TeamNum);

    private static bool CanSee(byte mode, int ownerSlot, int ownerTeam, int viewerSlot, int viewerTeam) => mode switch
    {
        ModeOff => false,
        ModeSelf => viewerSlot == ownerSlot,
        ModeTeam => viewerSlot == ownerSlot || (ownerTeam > 1 && viewerTeam == ownerTeam),
        ModeEnemy => viewerSlot == ownerSlot || (ownerTeam > 1 && viewerTeam > 1 && viewerTeam != ownerTeam),
        _ => true
    };

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
            if (raw is not ("all" or "team" or "enemy" or "self" or "hidden"))
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
        core.SetSetting(player, "HideVip@" + Names[module], Serialize(mode));
    }

    private static byte Parse(string value) => value switch
    {
        "team" => ModeTeam,
        "enemy" => ModeEnemy,
        "self" => ModeSelf,
        "hidden" => ModeOff,
        _ => ModeAll
    };

    private static string Serialize(byte mode) => mode switch
    {
        ModeTeam => "team",
        ModeEnemy => "enemy",
        ModeSelf => "self",
        ModeOff => "hidden",
        _ => "all"
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
    }

    private static void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        var core = _owner;
        if (core == null || _transmitNonDefault == 0 || _entities.Count == 0)
            return;

        Array.Clear(_team);
        foreach (var p in core.Players)
            if (p != null && p.IsValid && p.Slot < 64)
                _team[p.Slot] = p.TeamNum;

        for (int slot = 0; slot < 64; slot++)
        {
            _hidden[slot].Clear();
            _ownerMode[slot] = ModeAll;
        }

        bool any = false;
        foreach (var (index, entry) in _entities)
        {
            int ownerSlot = entry.OwnerSlot;
            if (ownerSlot < 0 || ownerSlot >= 64)
                continue;

            byte mode = _mode[ownerSlot, entry.Module];
            if (mode == ModeAll)
                continue;

            _ownerMode[ownerSlot] = mode;
            _hidden[ownerSlot].Add(index);
            any = true;
        }

        if (!any)
            return;

        foreach (var (info, viewer) in infoList)
        {
            if (viewer == null || !viewer.IsValid || viewer.Slot >= 64)
                continue;

            int viewerSlot = viewer.Slot;
            int viewerTeam = _team[viewerSlot];

            for (int ownerSlot = 0; ownerSlot < 64; ownerSlot++)
            {
                var bucket = _hidden[ownerSlot];
                if (bucket.Count == 0)
                    continue;
                if (CanSee(_ownerMode[ownerSlot], ownerSlot, _team[ownerSlot], viewerSlot, viewerTeam))
                    continue;

                foreach (uint index in bucket)
                    if (info.TransmitEntities.Contains(index))
                        info.TransmitEntities.Remove(index);
            }
        }
    }
}
