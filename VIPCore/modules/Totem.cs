using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class Totem : VipModule
{
    private class Cfg
    {
        public int Limit { get; set; } = 1;
        public string Color { get; set; } = "#FFC83C";
        public int Alpha { get; set; } = 140;
        public float Fade { get; set; } = 0.6f;
        public float Hold { get; set; } = 0.2f;
        public string SoundPath { get; set; } = "sounds/items/healthshot_success_01.vsnd";
        public string SoundEmit { get; set; } = "Healthshot.Success";
        public float SoundVolume { get; set; } = 1f;
    }

    private const int Buffer = 100000;
    private const float MaxMultiplier = 4f;

    private static readonly Cfg DefaultCfg = new();
    private readonly int[] _before = new int[64];

    public override string Name => "Totem";
    public override string DisplayName => Core.Localizer["vip.module.totem"];
    public override int Priority => -100;

    public override void OnLoad()
    {
        Core.HookDamage(OnDamage);
        Core.RegisterListener<Listeners.OnPlayerTakeDamagePost>(OnDamagePost);
        Core.RegisterEventHandler<EventPlayerHurt>(OnHurt, HookMode.Pre);
    }

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        if (info.Damage <= 0f)
            return HookResult.Continue;

        var victim = DamageVictim(entity);
        if (!Active(victim) || (uint)victim.Slot >= 64)
            return HookResult.Continue;

        var pawn = PlayerCache.Pawn(victim);
        if (pawn == null || !pawn.IsValid || pawn.Health <= 0 || info.Damage * MaxMultiplier < pawn.Health)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(victim) ?? DefaultCfg;
        if (LimitReached(victim.Slot, cfg.Limit))
            return HookResult.Continue;

        int slot = victim.Slot;
        if (_before[slot] > 0)
            return HookResult.Continue;

        _before[slot] = pawn.Health;
        pawn.Health += Buffer;

        uint handle = pawn.EntityHandle.Raw;
        Server.NextFrame(() =>
        {
            if (_before[slot] <= 0)
                return;

            var stale = new CHandle<CCSPlayerPawn>(handle).Value;
            if (stale != null && stale.IsValid)
                stale.Health = _before[slot];
            _before[slot] = 0;
        });

        return HookResult.Continue;
    }

    private HookResult OnHurt(EventPlayerHurt ev, GameEventInfo info)
    {
        int slot = ev.Userid?.Slot ?? -1;
        if ((uint)slot >= 64 || _before[slot] <= 0)
            return HookResult.Continue;

        ev.Health = Math.Max(_before[slot] - ev.DmgHealth, 1);
        return HookResult.Continue;
    }

    private void OnDamagePost(CCSPlayerPawn pawn, CTakeDamageInfo info, CTakeDamageResult result)
    {
        var victim = pawn.Controller.Value?.As<CCSPlayerController>();
        if (victim == null || !victim.IsValid || (uint)victim.Slot >= 64)
            return;

        int slot = victim.Slot;
        int before = _before[slot];
        if (before <= 0)
            return;

        _before[slot] = 0;
        int left = pawn.Health - Buffer;

        if (left > 0)
        {
            pawn.Health = left;
            NetState.Changed(pawn, "CBaseEntity", "m_iHealth");
            return;
        }

        pawn.Health = 1;
        NetState.Changed(pawn, "CBaseEntity", "m_iHealth");
        LimitUse(slot);
        Pop(victim);
    }

    private void Pop(CCSPlayerController victim)
    {
        var cfg = GroupValue<Cfg>(victim) ?? DefaultCfg;

        if (!victim.IsBot)
        {
            var color = TrailBeam.Resolve(cfg.Color);
            ScreenFade.Apply(victim, System.Drawing.Color.FromArgb(Math.Clamp(cfg.Alpha, 0, 255), color.R, color.G, color.B), cfg.Fade, cfg.Hold);
            victim.PrintToChat($" {CC.Orchid}{Core.ChatPrefix}{CC.Default} {Core.Localizer["vip.totem.used"]}");
        }

        var listeners = new List<CCSPlayerController>();
        if (!victim.IsBot)
            listeners.Add(victim);
        AddSpectators(victim, listeners);
        SoundUtil.PlayFor(victim, listeners, cfg.SoundPath, cfg.SoundEmit, cfg.SoundVolume);
    }
}
