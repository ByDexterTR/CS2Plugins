using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class PlayerGlow : VipModule
{
    private class Cfg
    {
        public int Range { get; set; } = 300;
        public int Team { get; set; } = -1;
        public List<string> Colors { get; set; } = new();
    }

    private readonly (uint Glow, uint Relay)?[] _glows = new (uint, uint)?[64];

    public override string Name => "PlayerGlow";
    public override string DisplayName => Core.Localizer["vip.module.playerglow"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player) =>
        TrailBeam.ParseColorOptions((GroupValue<Cfg>(player) ?? new()).Colors);

    public override void OnLoad()
    {
        EffectHide.Ensure(Core);
        Core.RegisterListener<OnTick>(OnTick);
        Core.RegisterEventHandler<EventRoundStart>((_, _) => { Array.Clear(_glows); return HookResult.Continue; });
    }

    private Color ResolveColor(CCSPlayerController player)
    {
        string setting = Setting(player);
        return TrailBeam.IsRandom(setting) ? Core.RoundColor(player.Slot) : TrailBeam.Resolve(setting);
    }

    public override void OnUnload()
    {
        for (int slot = 0; slot < 64; slot++)
            Destroy(slot);
    }

    private void OnTick()
    {
        foreach (var player in Core.Players)
        {
            if (player == null || !player.IsValid || player.IsBot)
                continue;

            int slot = player.Slot;

            CDynamicProp? glow = null;
            if (_glows[slot] is { } handles)
            {
                glow = Utilities.GetEntityFromIndex<CDynamicProp>((int)handles.Glow);
                if (glow == null || !glow.IsValid)
                {
                    _glows[slot] = null;
                    glow = null;
                }
            }

            if (!Active(player) || !IsAlive(player))
            {
                if (_glows[slot] != null)
                    Destroy(slot);
                continue;
            }

            if (glow == null)
            {
                _glows[slot] = Create(player);
                if (_glows[slot] is { } created)
                    glow = Utilities.GetEntityFromIndex<CDynamicProp>((int)created.Glow);
            }

            if (glow != null && glow.IsValid)
            {
                var color = ResolveColor(player);
                var current = glow.Glow.GlowColorOverride;
                if (current.R != color.R || current.G != color.G || current.B != color.B || current.A != color.A)
                {
                    glow.Glow.GlowColorOverride = color;
                    Utilities.SetStateChanged(glow, "CGlowProperty", "m_glowColorOverride");
                }
            }
        }
    }

    private (uint, uint)? Create(CCSPlayerController player)
    {
        if (!EffectHide.AnyViewer(EffectHide.PlayerGlow, player.Slot))
            return null;

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

        var cfg = GroupValue<Cfg>(player) ?? new();
        glow.Glow.GlowColorOverride = ResolveColor(player);
        glow.Glow.GlowRange = cfg.Range;
        glow.Glow.GlowTeam = cfg.Team;
        glow.Glow.GlowType = 3;
        glow.Glow.GlowRangeMin = 0;

        relay.AcceptInput("FollowEntity", pawn, relay, "!activator");
        glow.AcceptInput("FollowEntity", relay, glow, "!activator");

        EffectHide.Track(EffectHide.PlayerGlow, glow.Index, player.Slot);

        return (glow.Index, relay.Index);
    }

    private void Destroy(int slot)
    {
        if (_glows[slot] is { } handles)
        {
            var glow = Utilities.GetEntityFromIndex<CDynamicProp>((int)handles.Glow);
            if (glow != null && glow.IsValid)
                glow.Remove();

            var relay = Utilities.GetEntityFromIndex<CDynamicProp>((int)handles.Relay);
            if (relay != null && relay.IsValid)
                relay.Remove();
        }

        _glows[slot] = null;
    }
}
