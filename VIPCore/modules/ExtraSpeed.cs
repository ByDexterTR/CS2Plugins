using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class ExtraSpeed : VipModule
{
    private class Cfg
    {
        public double Multiplier { get; set; } = 1.0;
        public string OnlyWithWeapon { get; set; } = "";
    }

    private readonly (float mult, List<string> weapons)?[] _cache = new (float, List<string>)?[64];
    private readonly bool[] _applied = new bool[64];

    public override string Name => "ExtraSpeed";
    public override string DisplayName => Core.Localizer["vip.module.extraspeed"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.RegisterListener<OnTick>(OnTick);
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        int slot = player?.Slot ?? -1;
        if (slot < 0 || slot >= 64)
            return HookResult.Continue;

        _applied[slot] = false;

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

            var pawn = player!.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            bool match = cached.Value.weapons.Count == 0 || WeaponUtil.MatchesAny(cached.Value.weapons, ActiveWeaponName(player));

            if (match)
            {
                _applied[slot] = true;
                if (Math.Abs(pawn.VelocityModifier - cached.Value.mult) > 0.001f)
                {
                    pawn.VelocityModifier = cached.Value.mult;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
            else if (_applied[slot])
            {
                _applied[slot] = false;
                if (Math.Abs(pawn.VelocityModifier - 1f) > 0.001f)
                {
                    pawn.VelocityModifier = 1f;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
        }
    }
}
