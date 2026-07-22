using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public static class FireUtil
{
    private const float BurnWindow = 1.0f;
    private const float FireRadius = 250.0f;
    private const float ThrowRadius = 1500.0f;
    private const float ThrowWindow = 4.0f;

    private class Throw
    {
        public float X;
        public float Y;
        public float Z;
        public float Time;
    }

    private static BasePlugin? _owner;
    private static readonly Dictionary<int, Vector> _infernos = new();
    private static readonly List<Throw> _thrown = new();
    private static readonly float[] _lastBurn = new float[64];

    public static void Ensure(BasePlugin plugin)
    {
        if (ReferenceEquals(_owner, plugin))
            return;

        _owner = plugin;
        Reset();

        plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        plugin.RegisterEventHandler<EventInfernoStartburn>(OnInfernoStart);
        plugin.RegisterEventHandler<EventInfernoExtinguish>(OnInfernoGone);
        plugin.RegisterEventHandler<EventInfernoExpire>(OnInfernoExpire);
        plugin.RegisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown);
        plugin.RegisterEventHandler<EventMolotovDetonate>(OnMolotovDetonate);
        plugin.RegisterListener<OnEntityTakeDamagePre>(OnDamage);
    }

    public static bool Blocked(CCSPlayerController? player)
    {
        var origin = player?.PlayerPawn.Value?.AbsOrigin;
        if (origin == null)
            return false;

        return IsBurning(player!.Slot) || NearFire(origin) || NearThrow(origin);
    }

    private static void Reset()
    {
        _infernos.Clear();
        _thrown.Clear();
        Array.Clear(_lastBurn);
    }

    private static HookResult OnRoundStart(EventRoundStart ev, GameEventInfo info)
    {
        Reset();
        return HookResult.Continue;
    }

    private static HookResult OnInfernoStart(EventInfernoStartburn ev, GameEventInfo info)
    {
        _infernos[ev.Entityid] = new Vector(ev.X, ev.Y, ev.Z);
        return HookResult.Continue;
    }

    private static HookResult OnInfernoGone(EventInfernoExtinguish ev, GameEventInfo info)
    {
        _infernos.Remove(ev.Entityid);
        return HookResult.Continue;
    }

    private static HookResult OnInfernoExpire(EventInfernoExpire ev, GameEventInfo info)
    {
        _infernos.Remove(ev.Entityid);
        return HookResult.Continue;
    }

    private static HookResult OnGrenadeThrown(EventGrenadeThrown ev, GameEventInfo info)
    {
        if (ev.Weapon != "molotov" && ev.Weapon != "incgrenade")
            return HookResult.Continue;

        var origin = ev.Userid?.PlayerPawn.Value?.AbsOrigin;
        if (origin == null)
            return HookResult.Continue;

        _thrown.Add(new Throw { X = origin.X, Y = origin.Y, Z = origin.Z, Time = Server.CurrentTime });
        return HookResult.Continue;
    }

    private static HookResult OnMolotovDetonate(EventMolotovDetonate ev, GameEventInfo info)
    {
        if (_thrown.Count > 0)
            _thrown.RemoveAt(0);
        return HookResult.Continue;
    }

    private static HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        if (((long)info.BitsDamageType & (long)DamageTypes_t.DMG_BURN) == 0)
            return HookResult.Continue;

        if (entity == null || !entity.IsValid || entity.DesignerName != "player")
            return HookResult.Continue;

        int slot = entity.As<CCSPlayerPawn>().Controller.Value?.As<CCSPlayerController>()?.Slot ?? -1;
        if (slot >= 0 && slot < 64)
            _lastBurn[slot] = Server.CurrentTime;

        return HookResult.Continue;
    }

    private static bool IsBurning(int slot) =>
        slot >= 0 && slot < 64 && _lastBurn[slot] > 0 && Server.CurrentTime - _lastBurn[slot] < BurnWindow;

    private static bool NearFire(Vector origin)
    {
        foreach (var pos in _infernos.Values)
        {
            if (Within(origin, pos.X, pos.Y, pos.Z, FireRadius))
                return true;
        }
        return false;
    }

    private static bool NearThrow(Vector origin)
    {
        float now = Server.CurrentTime;
        _thrown.RemoveAll(t => now - t.Time > ThrowWindow);

        foreach (var t in _thrown)
        {
            if (Within(origin, t.X, t.Y, t.Z, ThrowRadius))
                return true;
        }
        return false;
    }

    private static bool Within(Vector a, float x, float y, float z, float radius)
    {
        float dx = a.X - x, dy = a.Y - y, dz = a.Z - z;
        return dx * dx + dy * dy + dz * dz <= radius * radius;
    }
}
