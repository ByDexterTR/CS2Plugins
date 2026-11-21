using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace VIPCore;

public class SpawnProtection : VipModule
{
    private class Cfg
    {
        public float Duration { get; set; } = 2f;
        public int Limit { get; set; } = 0;
        public bool EndOnFire { get; set; } = false;
        public string ScreenColor { get; set; } = "#FFD000";
        public int ScreenAlpha { get; set; } = 90;
    }

    private readonly float[] _until = new float[64];
    private readonly bool[] _endOnFire = new bool[64];

    public override string Name => "SpawnProtection";
    public override string DisplayName => Core.Localizer["vip.module.spawnprotection"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.RegisterEventHandler<EventWeaponFire>(OnFire);
        Core.HookDamage(OnDamage);
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        int slot = player?.Slot ?? -1;
        if (slot < 0 || slot >= 64)
            return HookResult.Continue;

        if (!Active(player))
        {
            _until[slot] = 0f;
            return HookResult.Continue;
        }

        var cfg = GroupValue<Cfg>(player!) ?? new Cfg();
        if (LimitReached(slot, cfg.Limit))
        {
            _until[slot] = 0f;
            return HookResult.Continue;
        }

        float seconds = cfg.Duration;
        if (seconds <= 0f)
        {
            _until[slot] = 0f;
            return HookResult.Continue;
        }

        LimitUse(slot);
        float until = Server.CurrentTime + seconds;
        _until[slot] = until;
        _endOnFire[slot] = cfg.EndOnFire;

        if (player!.IsBot)
            return HookResult.Continue;

        player.PrintToChat($" {CC.Orchid}{Core.Localizer["chat_prefix"]}{CC.Default} {Core.Localizer["vip.spawnprot.start", (int)seconds]}");

        if (cfg.ScreenAlpha > 0)
        {
            var color = TrailBeam.Resolve(cfg.ScreenColor);
            ScreenFade.Apply(player, System.Drawing.Color.FromArgb(Math.Clamp(cfg.ScreenAlpha, 0, 255), color.R, color.G, color.B), seconds, 0f);
        }

        Core.AddTimer(seconds, () =>
        {
            var p = Core.PlayerAt(slot);
            if (p == null || !p.IsValid || p.IsBot || !IsAlive(p))
                return;
            if (Math.Abs(_until[slot] - until) > 0.01f)
                return;

            ScreenFade.Clear(p);
            p.PrintToChat($" {CC.Orchid}{Core.Localizer["chat_prefix"]}{CC.Default} {Core.Localizer["vip.spawnprot.end"]}");
        }, TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }

    private HookResult OnFire(EventWeaponFire ev, GameEventInfo info)
    {
        var player = ev.Userid;
        int slot = player?.Slot ?? -1;
        if (slot < 0 || slot >= 64 || !_endOnFire[slot] || _until[slot] <= Server.CurrentTime)
            return HookResult.Continue;

        _until[slot] = 0f;
        ScreenFade.Clear(player);
        if (!player!.IsBot)
            player.PrintToChat($" {CC.Orchid}{Core.Localizer["chat_prefix"]}{CC.Default} {Core.Localizer["vip.spawnprot.end"]}");

        return HookResult.Continue;
    }

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        var victim = DamageVictim(entity);
        if (victim == null)
            return HookResult.Continue;

        int slot = victim.Slot;
        if (slot < 0 || slot >= 64 || _until[slot] <= Server.CurrentTime)
            return HookResult.Continue;

        info.Damage = 0;
        return HookResult.Handled;
    }
}
