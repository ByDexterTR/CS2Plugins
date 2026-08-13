using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public static class GlowPool
{
    public const string DefaultColor = "#612D53";

    private static VIPCore? _owner;
    private static readonly (uint Glow, uint Relay)?[] _entities = new (uint, uint)?[64];
    private static readonly Color[] _colors = new Color[64];
    private static readonly ulong[] _visible = new ulong[64];
    private static int _users;
    private static int _builtAt = -1;

    public static void Ensure(VIPCore core)
    {
        if (ReferenceEquals(_owner, core))
            return;

        _owner = core;
        Array.Clear(_entities);
        Array.Clear(_colors);
        Array.Clear(_visible);
        _users = 0;
        _builtAt = -1;

        core.RegisterListener<CheckTransmit>(OnCheckTransmit);
        core.RegisterEventHandler<EventRoundStart>((_, _) => { DestroyAll(); return HookResult.Continue; });
        core.RegisterEventHandler<EventPlayerDeath>((ev, _) =>
        {
            int slot = ev.Userid?.Slot ?? -1;
            if (slot >= 0 && slot < 64)
                Destroy(slot);
            return HookResult.Continue;
        });
        core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) =>
        {
            int slot = ev.Userid?.Slot ?? -1;
            if (slot >= 0 && slot < 64)
                Destroy(slot);
            return HookResult.Continue;
        });
    }

    public static void Acquire() => _users++;

    public static void Release()
    {
        if (--_users > 0)
            return;

        _users = 0;
        DestroyAll();
    }

    public static void Show(int viewerSlot, int targetSlot, Color color)
    {
        if (viewerSlot < 0 || viewerSlot >= 64 || targetSlot < 0 || targetSlot >= 64)
            return;

        _visible[viewerSlot] |= 1UL << targetSlot;
        Tint(targetSlot, color);
    }

    private static void Tint(int slot, Color color)
    {
        if (_entities[slot] is not { } handles || _colors[slot].ToArgb() == color.ToArgb())
            return;

        var glow = Utilities.GetEntityFromIndex<CDynamicProp>((int)handles.Glow);
        if (glow == null || !glow.IsValid)
            return;

        glow.Glow.GlowColorOverride = color;
        Utilities.SetStateChanged(glow, "CGlowProperty", "m_glowColorOverride");
        _colors[slot] = color;
    }

    public static void Build()
    {
        var core = _owner;
        if (core == null || _users <= 0)
            return;

        int tick = Server.TickCount;
        if (_builtAt == tick)
            return;
        _builtAt = tick;

        foreach (var player in core.Players)
        {
            if (player == null || !player.IsValid || player.Slot >= 64)
                continue;

            int slot = player.Slot;
            bool alive = player.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;

            if (!alive || player.TeamNum < 2)
            {
                if (_entities[slot] != null)
                    Destroy(slot);
                continue;
            }

            if (_entities[slot] is { } handles)
            {
                var existing = Utilities.GetEntityFromIndex<CDynamicProp>((int)handles.Glow);
                if (existing != null && existing.IsValid)
                    continue;
                _entities[slot] = null;
            }

            _entities[slot] = Create(player);
            _colors[slot] = _entities[slot] != null ? TrailBeam.Resolve(DefaultColor) : default;
        }
    }

    private static (uint, uint)? Create(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return null;

        string? modelName = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState.ModelName;
        if (string.IsNullOrEmpty(modelName))
            return null;

        var relay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        var glow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (relay == null || !relay.IsValid || glow == null || !glow.IsValid)
            return null;

        var relayEntity = relay.CBodyComponent?.SceneNode?.Owner?.Entity;
        if (relayEntity != null)
            relayEntity.Flags = (uint)(relayEntity.Flags & ~(1 << 2));

        relay.SetModel(modelName);
        relay.Spawnflags = 256u;
        relay.RenderMode = RenderMode_t.kRenderNone;
        relay.DispatchSpawn();

        var glowEntity = glow.CBodyComponent?.SceneNode?.Owner?.Entity;
        if (glowEntity != null)
            glowEntity.Flags = (uint)(glowEntity.Flags & ~(1 << 2));

        glow.SetModel(modelName);
        glow.Spawnflags = 256u;
        glow.Render = Color.FromArgb(1, 255, 255, 255);
        glow.DispatchSpawn();

        glow.Glow.GlowColorOverride = TrailBeam.Resolve(DefaultColor);
        glow.Glow.GlowRange = 5000;
        glow.Glow.GlowTeam = -1;
        glow.Glow.GlowType = 3;
        glow.Glow.GlowRangeMin = 0;

        relay.AcceptInput("FollowEntity", pawn, relay, "!activator");
        glow.AcceptInput("FollowEntity", relay, glow, "!activator");

        return (glow.Index, relay.Index);
    }

    private static void Destroy(int slot)
    {
        if (_entities[slot] is { } handles)
        {
            var glow = Utilities.GetEntityFromIndex<CDynamicProp>((int)handles.Glow);
            if (glow != null && glow.IsValid && glow.DesignerName == "prop_dynamic")
                glow.Remove();

            var relay = Utilities.GetEntityFromIndex<CDynamicProp>((int)handles.Relay);
            if (relay != null && relay.IsValid && relay.DesignerName == "prop_dynamic")
                relay.Remove();
        }

        _entities[slot] = null;
        _colors[slot] = default;
    }

    private static void DestroyAll()
    {
        for (int slot = 0; slot < 64; slot++)
            Destroy(slot);

        Array.Clear(_visible);
        _builtAt = -1;
    }

    private static void OnCheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
    {
        if (_users <= 0)
            return;

        foreach (var (info, viewer) in infoList)
        {
            if (viewer == null || !viewer.IsValid || viewer.Slot >= 64)
                continue;

            ulong mask = _visible[viewer.Slot];

            for (int target = 0; target < 64; target++)
            {
                if (_entities[target] is not { } handles)
                    continue;
                if (target != viewer.Slot && (mask & (1UL << target)) != 0)
                    continue;

                if (info.TransmitEntities.Contains(handles.Glow))
                    info.TransmitEntities.Remove(handles.Glow);
                if (info.TransmitEntities.Contains(handles.Relay))
                    info.TransmitEntities.Remove(handles.Relay);
            }
        }

        Array.Clear(_visible);
    }
}
