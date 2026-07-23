using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class FallDamage : VipModule
{
    private class Cfg
    {
        public int Percent { get; set; } = 100;
        public int Limit { get; set; } = 0;
    }

    private readonly int[] _used = new int[64];

    public override string Name => "FallDamage";
    public override string DisplayName => Core.Localizer["vip.module.falldamage"];

    public override void OnLoad()
    {
        Core.RegisterListener<OnEntityTakeDamagePre>(OnDamage);
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        int slot = ev.Userid?.Slot ?? -1;
        if (slot >= 0 && slot < 64)
            _used[slot] = 0;
        return HookResult.Continue;
    }

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        if (((long)info.BitsDamageType & (long)DamageTypes_t.DMG_FALL) == 0)
            return HookResult.Continue;

        var victim = PawnController(entity);
        if (!Active(victim))
            return HookResult.Continue;

        int slot = victim!.Slot;
        var cfg = GroupValue<Cfg>(victim) ?? new Cfg();
        if (cfg.Percent >= 100)
            return HookResult.Continue;

        if (cfg.Limit > 0 && _used[slot] >= cfg.Limit)
            return HookResult.Continue;

        if (cfg.Percent <= 0)
            info.Damage = 0;
        else
            info.Damage *= cfg.Percent / 100f;

        _used[slot]++;
        return HookResult.Changed;
    }
}
