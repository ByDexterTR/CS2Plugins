using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class ExtraSpeed : VipModule
{
    private class Cfg
    {
        public double Multiplier { get; set; } = 1.0;
        public string OnlyWithWeapon { get; set; } = "";
    }

    private readonly (float mult, List<string> weapons)?[] _cache = new (float, List<string>)?[64];

    public override string Name => "ExtraSpeed";
    public override string DisplayName => Core.Localizer["vip.module.extraspeed"];

    public override void OnLoad()
    {
        SpeedPool.Ensure(Core);
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.HookTick(OnTick);
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        int slot = player?.Slot ?? -1;
        if (slot < 0 || slot >= 64)
            return HookResult.Continue;

        if (!Active(player))
        {
            _cache[slot] = null;
            return HookResult.Continue;
        }

        var cfg = GroupValue<Cfg>(player!) ?? new Cfg();
        _cache[slot] = ((float)cfg.Multiplier, WeaponUtil.ParseCsv(cfg.OnlyWithWeapon));
        return HookResult.Continue;
    }

    private void OnTick()
    {
        for (int slot = 0; slot < 64; slot++)
        {
            var cached = _cache[slot];
            if (cached == null)
                continue;

            var player = Utilities.GetPlayerFromSlot(slot);
            if (!IsAlive(player) || !Active(player))
                continue;

            if (cached.Value.weapons.Count == 0 || WeaponUtil.MatchesAny(cached.Value.weapons, ActiveWeaponName(player!)))
                SpeedPool.Request(slot, SpeedPool.ExtraSpeed, cached.Value.mult);
        }
    }
}
