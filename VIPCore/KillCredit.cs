using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public static class KillCredit
{
    private const int Lifetime = 64;

    private static VIPCore? _owner;
    private static readonly (int AttackerSlot, string Weapon, int ExpireTick)?[] _pending = new (int, string, int)?[64];

    public static void Ensure(VIPCore core)
    {
        if (ReferenceEquals(_owner, core))
            return;

        _owner = core;
        Array.Clear(_pending);

        core.RegisterEventHandler<EventPlayerDeath>(OnDeath, HookMode.Pre);
        core.RegisterEventHandler<EventRoundStart>((_, _) => { Array.Clear(_pending); return HookResult.Continue; });
    }

    public static void Register(int victimSlot, int attackerSlot, string weapon)
    {
        if (victimSlot < 0 || victimSlot >= 64 || attackerSlot < 0 || attackerSlot >= 64)
            return;

        _pending[victimSlot] = (attackerSlot, weapon, Server.TickCount + Lifetime);
    }

    private static HookResult OnDeath(EventPlayerDeath ev, GameEventInfo info)
    {
        int slot = ev.Userid?.Slot ?? -1;
        if (slot < 0 || slot >= 64 || _pending[slot] is not { } credit)
            return HookResult.Continue;

        _pending[slot] = null;

        if (credit.ExpireTick < Server.TickCount)
            return HookResult.Continue;

        var attacker = Utilities.GetPlayerFromSlot(credit.AttackerSlot);
        if (attacker == null || !attacker.IsValid)
            return HookResult.Continue;

        ev.Attacker = attacker;
        if (credit.Weapon.Length > 0)
            ev.Weapon = credit.Weapon;

        var stats = attacker.ActionTrackingServices?.MatchStats;
        if (stats != null)
        {
            stats.Kills++;
            attacker.Score += 2;
            Utilities.SetStateChanged(attacker, "CCSPlayerController", "m_pActionTrackingServices");
            Utilities.SetStateChanged(attacker, "CCSPlayerController", "m_iScore");
        }

        return HookResult.Changed;
    }
}
