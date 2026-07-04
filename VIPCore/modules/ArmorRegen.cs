using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace VIPCore;

public class ArmorRegen : VipModule
{
    private class Cfg
    {
        public int ArmorPerTick { get; set; } = 5;
        public float Interval { get; set; } = 1.0f;
        public float DelayAfterDmg { get; set; } = 3.0f;
        public int MaxArmor { get; set; } = 100;
        public bool GiveHelmetWhenFull { get; set; } = true;
    }

    private readonly Timer?[] _timers = new Timer?[64];
    private readonly float[] _lastHurt = new float[64];
    private readonly bool[] _helmetGiven = new bool[64];

    public override string Name => "ArmorRegen";
    public override string DisplayName => Core.Localizer["vip.module.armorregen"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.RegisterEventHandler<EventPlayerHurt>(OnHurt);
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
        _helmetGiven[slot] = false;

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

        if (pawn.ArmorValue < cfg.MaxArmor)
        {
            pawn.ArmorValue = Math.Min(pawn.ArmorValue + cfg.ArmorPerTick, cfg.MaxArmor);
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
        }

        if (cfg.GiveHelmetWhenFull && !_helmetGiven[slot] && pawn.ArmorValue >= cfg.MaxArmor)
        {
            _helmetGiven[slot] = true;
            player.GiveNamedItem("item_assaultsuit");
            pawn.ArmorValue = cfg.MaxArmor;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
        }
    }
}
