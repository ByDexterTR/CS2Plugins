using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace VIPCore;

public class HealthRegen : VipModule
{
    private class Cfg
    {
        public int HpPerTick { get; set; } = 5;
        public float Interval { get; set; } = 1.0f;
        public float DelayAfterDmg { get; set; } = 3.0f;
    }

    private readonly Timer?[] _timers = new Timer?[64];
    private readonly float[] _lastHurt = new float[64];

    public override string Name => "HealthRegen";
    public override string DisplayName => Core.Localizer["vip.module.healthregen"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.RegisterEventHandler<EventPlayerHurt>(OnHurt);
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) => { KillTimer(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { KillTimer(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
    }

    private void KillTimer(int slot)
    {
        if (slot < 0 || slot >= 64)
            return;

        _timers[slot]?.Kill();
        _timers[slot] = null;
    }

    private HookResult OnHurt(EventPlayerHurt ev, GameEventInfo info)
    {
        int slot = ev.Userid?.Slot ?? -1;
        if (slot >= 0 && slot < 64)
            _lastHurt[slot] = Server.CurrentTime;
        return HookResult.Continue;
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        int slot = player?.Slot ?? -1;
        if (slot < 0 || slot >= 64)
            return HookResult.Continue;

        _timers[slot]?.Kill();
        _timers[slot] = null;

        if (!Active(player))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(player!) ?? new Cfg();
        _lastHurt[slot] = Server.CurrentTime;
        _timers[slot] = Core.AddTimer(cfg.Interval, () => Regen(slot, cfg), TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        return HookResult.Continue;
    }

    private void Regen(int slot, Cfg cfg)
    {
        var player = Utilities.GetPlayerFromSlot(slot);
        if (!IsAlive(player) || !Active(player))
            return;

        if (Server.CurrentTime - _lastHurt[slot] < cfg.DelayAfterDmg)
            return;

        var pawn = player!.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        int maxHp = pawn.MaxHealth > 0 ? pawn.MaxHealth : 100;
        if (pawn.Health >= maxHp)
            return;

        pawn.Health = Math.Min(pawn.Health + cfg.HpPerTick, maxHp);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }
}
