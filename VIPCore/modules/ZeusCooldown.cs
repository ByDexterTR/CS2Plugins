using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace VIPCore;

public class ZeusCooldown : VipModule
{
    private class Cfg
    {
        public float Cooldown { get; set; } = 5f;
        public int Limit { get; set; } = 0;
    }

    private readonly int[] _used = new int[64];

    public override string Name => "ZeusCooldown";
    public override string DisplayName => Core.Localizer["vip.module.zeuscooldown"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventWeaponFire>(OnFire);
        Core.RegisterEventHandler<EventRoundStart>((_, __) => { Array.Clear(_used); return HookResult.Continue; });
    }

    private HookResult OnFire(EventWeaponFire ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player) || !IsAlive(player))
            return HookResult.Continue;

        var weapon = player!.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (weapon == null || !weapon.IsValid || weapon.DesignerName != "weapon_taser")
            return HookResult.Continue;

        int slot = player.Slot;
        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        if (cfg.Limit > 0 && _used[slot] >= cfg.Limit)
            return HookResult.Continue;

        _used[slot]++;

        var taser = weapon.As<CWeaponTaser>();
        Core.AddTimer(Math.Max(cfg.Cooldown, 0f), () =>
        {
            if (taser.IsValid)
            {
                taser.LastAttackTick = 0;
                taser.FireTime = 0;
            }
        }, TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }
}
