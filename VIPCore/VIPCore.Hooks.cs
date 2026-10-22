using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
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
    private TransmitHook[] _transmitHooks = Array.Empty<TransmitHook>();
    private readonly TransmitView[] _transmitViews = new TransmitView[64];
    private int _transmitSlotOffset = -1;
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
        RegisterListener<OnClientPutInServer>(PlayersChanged);
        RegisterListener<OnClientDisconnect>(PlayersChanged);
        RegisterListener<OnClientDisconnectPost>(PlayersChanged);
        RegisterListener<OnClientAuthorized>((slot, _) => PlayersChanged(slot));
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
    public new void RegisterEventHandler<T>(GameEventHandler<T> handler, HookMode hookMode = HookMode.Post) where T : CounterStrikeSharp.API.Modules.Events.GameEvent
    {
        string label = $"Event {typeof(T).Name[5..]} {PerfLog.Describe(handler)}";
        base.RegisterEventHandler<T>((ev, info) =>
        {
            if (!PerfLog.Enabled)
                return handler(ev, info);

            long perf = PerfLog.Start();
            long alloc = PerfLog.Alloc();
            var result = handler(ev, info);
            PerfLog.Sample(perf, alloc, label);
            return result;
        }, hookMode);
    }

    public delegate void PostThinkHook(CCSPlayerController player, CCSPlayerPawn pawn);

    private PostThinkHook[] _postThinkHooks = Array.Empty<PostThinkHook>();
    private Func<DynamicHook, HookResult>? _postThinkListener;
    private int _pawnControllerOffset = -1;

    public void HookPostThink(PostThinkHook handler)
    {
        Add(ref _postThinkHooks, handler);
        if (_postThinkListener != null)
            return;

        _pawnControllerOffset = Schema.GetSchemaOffset("CBasePlayerPawn", "m_hController");
        _postThinkListener = DispatchPostThink;
        VirtualFunctions.CCSPlayerPawnBase_PostThinkFunc.Hook(_postThinkListener, HookMode.Post);
    }

    private void UnhookPostThink()
    {
        if (_postThinkListener == null)
            return;

        VirtualFunctions.CCSPlayerPawnBase_PostThinkFunc.Unhook(_postThinkListener, HookMode.Post);
        _postThinkListener = null;
        _postThinkHooks = Array.Empty<PostThinkHook>();
    }

    private unsafe HookResult DispatchPostThink(DynamicHook hook)
    {
        var hooks = _postThinkHooks;
        nint pointer = hook.GetParam<nint>(0);
        if (hooks.Length == 0 || pointer == 0)
            return HookResult.Continue;

        int slot = (int)(*(uint*)(pointer + _pawnControllerOffset) & 0x7FFF) - 1;
        var player = PlayerAt(slot);
        if (player == null || PlayerCache.Pointer(player) != pointer)
            return HookResult.Continue;

        var pawn = PlayerCache.Pawn(player)!;
        for (int i = 0; i < hooks.Length; i++)
        {
            long perf = PerfLog.Start();
            long alloc = PerfLog.Alloc();
            try { hooks[i](player, pawn); }
            catch (Exception ex) { HookFault(hooks[i], ex); }
            PerfLog.Sample(perf, alloc, "PostThink", hooks[i]);
        }

        return HookResult.Continue;
    }

    public void HookDamage(OnEntityTakeDamagePre handler) => Add(ref _damageHooks, handler);
    public void HookTransmit(TransmitHook handler) => Add(ref _transmitHooks, handler);
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
        PlayerCache.Invalidate();
        var hooks = _tickHooks;
        int tick = Server.TickCount;
        long total = PerfLog.Start();
        long totalAlloc = PerfLog.Alloc();

        for (int i = 0; i < hooks.Length; i++)
        {
            var hook = hooks[i];
            if (hook.Every > 1 && tick % hook.Every != hook.Phase)
                continue;

            long perf = PerfLog.Start();
            long alloc = PerfLog.Alloc();
            try { hook.Handler(); }
            catch (Exception ex) { HookFault(hook.Handler, ex); }
            PerfLog.Sample(perf, alloc, "Tick", hook.Handler);
        }

        PerfLog.Sample(total, totalAlloc, "Tick (total)");
    }

    private CEntityInstance? _damageEntity;
    private CTakeDamageInfo? _damageInfo;
    private bool _damageVictimDone;
    private bool _damageAttackerDone;
    private CCSPlayerController? _damageVictim;
    private CCSPlayerController? _damageAttacker;

    public CCSPlayerController? DamageVictim(CEntityInstance entity)
    {
        if (ReferenceEquals(entity, _damageEntity) && _damageVictimDone)
            return _damageVictim;

        var victim = VipModule.PawnController(entity);
        if (ReferenceEquals(entity, _damageEntity))
        {
            _damageVictim = victim;
            _damageVictimDone = true;
        }
        return victim;
    }

    public CCSPlayerController? DamageAttacker(CTakeDamageInfo info)
    {
        if (ReferenceEquals(info, _damageInfo) && _damageAttackerDone)
            return _damageAttacker;

        var attacker = VipModule.PawnController(info.Attacker?.Value);
        if (ReferenceEquals(info, _damageInfo))
        {
            _damageAttacker = attacker;
            _damageAttackerDone = true;
        }
        return attacker;
    }

    private HookResult DispatchDamage(CBaseEntity entity, CTakeDamageInfo info)
    {
        var hooks = _damageHooks;
        _damageEntity = entity;
        _damageInfo = info;
        _damageVictimDone = false;
        _damageAttackerDone = false;
        var result = HookResult.Continue;

        for (int i = 0; i < hooks.Length; i++)
        {
            long perf = PerfLog.Start();
            long alloc = PerfLog.Alloc();
            try
            {
                var single = hooks[i](entity, info);
                if (single > result)
                    result = single;
            }
            catch (Exception ex) { HookFault(hooks[i], ex); }
            PerfLog.Sample(perf, alloc, "Damage", hooks[i]);
        }

        return result;
    }

    private unsafe void DispatchTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
    {
        var hooks = _transmitHooks;
        if (hooks.Length == 0)
            return;

        long all = PerfLog.Start();
        long allAlloc = PerfLog.Alloc();

        if (_transmitSlotOffset < 0)
            _transmitSlotOffset = GameData.GetOffset("CheckTransmitPlayerSlot");

        nint* list = (nint*)infoList.Handle;
        nint* entries = (nint*)list[0];
        int count = (int)list[1];
        int views = 0;
        for (int k = 0; k < count && views < _transmitViews.Length; k++)
        {
            nint entry = entries[k];
            int slot = *(int*)(entry + _transmitSlotOffset);
            if ((uint)slot >= 64)
                continue;

            _transmitViews[views++] = new TransmitView(PlayerAt(slot), slot, *(uint**)entry, *(uint**)(entry + 8));
        }

        var span = new ReadOnlySpan<TransmitView>(_transmitViews, 0, views);
        for (int i = 0; i < hooks.Length; i++)
        {
            long perf = PerfLog.Start();
            long alloc = PerfLog.Alloc();
            try { hooks[i](span); }
            catch (Exception ex) { HookFault(hooks[i], ex); }
            PerfLog.Sample(perf, alloc, "Transmit", hooks[i]);
        }

        PerfLog.Sample(all, allAlloc, "Transmit (total)");
    }

    private void DispatchSpawn(CEntityInstance entity)
    {
        PlayerCache.Invalidate();
        var hooks = _spawnHooks;
        for (int i = 0; i < hooks.Length; i++)
        {
            long perf = PerfLog.Start();
            long alloc = PerfLog.Alloc();
            try { hooks[i](entity); }
            catch (Exception ex) { HookFault(hooks[i], ex); }
            PerfLog.Sample(perf, alloc, "Spawn", hooks[i]);
        }
    }

    private void DispatchDelete(CEntityInstance entity)
    {
        PlayerCache.Invalidate();
        var hooks = _deleteHooks;
        for (int i = 0; i < hooks.Length; i++)
        {
            long perf = PerfLog.Start();
            long alloc = PerfLog.Alloc();
            try { hooks[i](entity); }
            catch (Exception ex) { HookFault(hooks[i], ex); }
            PerfLog.Sample(perf, alloc, "Delete", hooks[i]);
        }
    }

    private void DispatchMapStart(string mapName)
    {
        PlayerCache.Invalidate();
        _playersDirty = true;
        Array.Clear(_identity);
        var hooks = _mapStartHooks;
        for (int i = 0; i < hooks.Length; i++)
        {
            long perf = PerfLog.Start();
            long alloc = PerfLog.Alloc();
            try { hooks[i](mapName); }
            catch (Exception ex) { HookFault(hooks[i], ex); }
            PerfLog.Sample(perf, alloc, "MapStart", hooks[i]);
        }
    }

    private void DispatchMapEnd()
    {
        var hooks = _mapEndHooks;
        for (int i = 0; i < hooks.Length; i++)
        {
            long perf = PerfLog.Start();
            long alloc = PerfLog.Alloc();
            try { hooks[i](); }
            catch (Exception ex) { HookFault(hooks[i], ex); }
            PerfLog.Sample(perf, alloc, "MapEnd", hooks[i]);
        }
    }
}
