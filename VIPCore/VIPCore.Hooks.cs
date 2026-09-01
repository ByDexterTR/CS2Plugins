using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using Microsoft.Extensions.Logging;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public partial class VIPCore
{
    private readonly struct TickHook(OnTick handler, int every, int phase)
    {
        public readonly OnTick Handler = handler;
        public readonly int Every = every;
        public readonly int Phase = phase;
    }

    private TickHook[] _tickHooks = Array.Empty<TickHook>();
    private OnEntityTakeDamagePre[] _damageHooks = Array.Empty<OnEntityTakeDamagePre>();
    private CheckTransmit[] _transmitHooks = Array.Empty<CheckTransmit>();
    private OnEntitySpawned[] _spawnHooks = Array.Empty<OnEntitySpawned>();
    private OnEntityDeleted[] _deleteHooks = Array.Empty<OnEntityDeleted>();
    private OnMapStart[] _mapStartHooks = Array.Empty<OnMapStart>();
    private OnMapEnd[] _mapEndHooks = Array.Empty<OnMapEnd>();
    private OnServerPrecacheResources[] _precacheHooks = Array.Empty<OnServerPrecacheResources>();

    private readonly HashSet<string> _hookFaults = new();

    private void InstallHooks()
    {
        RegisterListener<OnTick>(DispatchTick);
        RegisterListener<OnEntityTakeDamagePre>(DispatchDamage);
        RegisterListener<CheckTransmit>(DispatchTransmit);
        RegisterListener<OnEntitySpawned>(DispatchSpawn);
        RegisterListener<OnEntityDeleted>(DispatchDelete);
        RegisterListener<OnMapStart>(DispatchMapStart);
        RegisterListener<OnMapEnd>(DispatchMapEnd);
        RegisterListener<OnServerPrecacheResources>(manifest =>
        {
            var hooks = _precacheHooks;
            for (int i = 0; i < hooks.Length; i++)
            {
                try { hooks[i](manifest); }
                catch (Exception ex) { HookFault(hooks[i], ex); }
            }
        });
    }

    private static void Add<T>(ref T[] hooks, T handler) where T : Delegate
    {
        foreach (var existing in hooks)
            if (existing.Equals(handler))
                return;

        var next = new T[hooks.Length + 1];
        hooks.CopyTo(next, 0);
        next[^1] = handler;
        hooks = next;
    }

    public void HookTick(OnTick handler, int every = 1)
    {
        foreach (var existing in _tickHooks)
            if (existing.Handler.Equals(handler))
                return;

        every = Math.Max(every, 1);
        int phase = 0;
        foreach (var existing in _tickHooks)
            if (existing.Every == every)
                phase++;

        var next = new TickHook[_tickHooks.Length + 1];
        _tickHooks.CopyTo(next, 0);
        next[^1] = new TickHook(handler, every, phase % every);
        _tickHooks = next;
    }
    public void HookDamage(OnEntityTakeDamagePre handler) => Add(ref _damageHooks, handler);
    public void HookTransmit(CheckTransmit handler) => Add(ref _transmitHooks, handler);
    public void HookEntitySpawned(OnEntitySpawned handler) => Add(ref _spawnHooks, handler);
    public void HookEntityDeleted(OnEntityDeleted handler) => Add(ref _deleteHooks, handler);
    public void HookMapStart(OnMapStart handler) => Add(ref _mapStartHooks, handler);
    public void HookMapEnd(OnMapEnd handler) => Add(ref _mapEndHooks, handler);
    public void HookPrecache(OnServerPrecacheResources handler) => Add(ref _precacheHooks, handler);

    private void HookFault(Delegate handler, Exception ex)
    {
        string name = $"{handler.Method.DeclaringType?.Name}.{handler.Method.Name}";
        if (!_hookFaults.Add(name))
            return;

        Logger.LogError("VIPCore: {0} hook hatasi: {1}", name, ex);
    }

    private void DispatchTick()
    {
        var hooks = _tickHooks;
        int tick = Server.TickCount;

        for (int i = 0; i < hooks.Length; i++)
        {
            var hook = hooks[i];
            if (hook.Every > 1 && tick % hook.Every != hook.Phase)
                continue;

            try { hook.Handler(); }
            catch (Exception ex) { HookFault(hook.Handler, ex); }
        }
    }

    private HookResult DispatchDamage(CBaseEntity entity, CTakeDamageInfo info)
    {
        var hooks = _damageHooks;
        var result = HookResult.Continue;

        for (int i = 0; i < hooks.Length; i++)
        {
            try
            {
                var single = hooks[i](entity, info);
                if (single > result)
                    result = single;
            }
            catch (Exception ex) { HookFault(hooks[i], ex); }
        }

        return result;
    }

    private void DispatchTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
    {
        var hooks = _transmitHooks;
        for (int i = 0; i < hooks.Length; i++)
        {
            try { hooks[i](infoList); }
            catch (Exception ex) { HookFault(hooks[i], ex); }
        }
    }

    private void DispatchSpawn(CEntityInstance entity)
    {
        var hooks = _spawnHooks;
        for (int i = 0; i < hooks.Length; i++)
        {
            try { hooks[i](entity); }
            catch (Exception ex) { HookFault(hooks[i], ex); }
        }
    }

    private void DispatchDelete(CEntityInstance entity)
    {
        var hooks = _deleteHooks;
        for (int i = 0; i < hooks.Length; i++)
        {
            try { hooks[i](entity); }
            catch (Exception ex) { HookFault(hooks[i], ex); }
        }
    }

    private void DispatchMapStart(string mapName)
    {
        var hooks = _mapStartHooks;
        for (int i = 0; i < hooks.Length; i++)
        {
            try { hooks[i](mapName); }
            catch (Exception ex) { HookFault(hooks[i], ex); }
        }
    }

    private void DispatchMapEnd()
    {
        var hooks = _mapEndHooks;
        for (int i = 0; i < hooks.Length; i++)
        {
            try { hooks[i](); }
            catch (Exception ex) { HookFault(hooks[i], ex); }
        }
    }
}
