using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class GrenadeKit : VipModule
{
    private class Cfg
    {
        public int Flash { get; set; }
        public int Smoke { get; set; }
        public int He { get; set; }
        public int Molotov { get; set; }
        public int Decoy { get; set; }
    }

    private record GrenadeDef(string ThrowName, string GiveWeapon, string[] ThrownNames, string[] DesignerNames, Func<Cfg, int> Count);

    private static readonly GrenadeDef[] Grenades =
    {
        new("flash", "weapon_flashbang", new[] { "flashbang" }, new[] { "weapon_flashbang" }, c => c.Flash),
        new("smoke", "weapon_smokegrenade", new[] { "smokegrenade" }, new[] { "weapon_smokegrenade" }, c => c.Smoke),
        new("he", "weapon_hegrenade", new[] { "hegrenade" }, new[] { "weapon_hegrenade" }, c => c.He),
        new("molotov", "weapon_molotov", new[] { "molotov", "incgrenade" }, new[] { "weapon_molotov", "weapon_incgrenade" }, c => c.Molotov),
        new("decoy", "weapon_decoy", new[] { "decoy" }, new[] { "weapon_decoy" }, c => c.Decoy),
    };

    private readonly Dictionary<string, int>?[] _remaining = new Dictionary<string, int>?[64];

    private static string GiveName(GrenadeDef def, CCSPlayerController player) =>
        def.ThrowName == "molotov" && player.Team == CsTeam.CounterTerrorist ? "weapon_incgrenade" : def.GiveWeapon;

    public override string Name => "GrenadeKit";
    public override string DisplayName => Core.Localizer["vip.module.grenadekit"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.RegisterEventHandler<EventGrenadeThrown>(OnThrown);
        Core.RegisterEventHandler<EventItemEquip>((ev, _) => { RefreshCount(ev.Userid, ev.Item); return HookResult.Continue; });
        Core.RegisterEventHandler<EventItemPickup>((ev, _) => { RefreshCount(ev.Userid, ev.Item); return HookResult.Continue; });
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        int slot = player?.Slot ?? -1;
        if (slot < 0 || slot >= 64)
            return HookResult.Continue;

        var remaining = new Dictionary<string, int>();
        _remaining[slot] = remaining;

        if (!Active(player))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(player!) ?? new Cfg();

        Server.NextFrame(() =>
        {
            if (!IsAlive(player))
                return;

            foreach (var def in Grenades)
            {
                int c = def.Count(cfg);
                if (c <= 0)
                    continue;

                if (c > 1)
                    remaining[def.ThrowName] = c;

                if (!def.DesignerNames.Any(n => HasWeapon(player, n)))
                    player!.GiveNamedItem(GiveName(def, player));
                else if (c > 1)
                    UpdateCount(player!, def.DesignerNames, c);
            }
        });

        return HookResult.Continue;
    }

    private HookResult OnThrown(EventGrenadeThrown ev, GameEventInfo info)
    {
        var player = ev.Userid;
        int slot = player?.Slot ?? -1;
        if (slot < 0 || slot >= 64 || !Active(player))
            return HookResult.Continue;

        if (Core.IsActive(player!, "InfiniteAmmo"))
            return HookResult.Continue;

        var remaining = _remaining[slot];
        if (remaining == null)
            return HookResult.Continue;

        var def = Grenades.FirstOrDefault(d => d.ThrownNames.Contains(ev.Weapon));
        if (def == null || !remaining.TryGetValue(def.ThrowName, out int pending) || pending <= 1)
            return HookResult.Continue;

        int next = pending - 1;
        remaining[def.ThrowName] = next;
        player!.GiveNamedItem(GiveName(def, player));
        if (next > 1)
            UpdateCount(player, def.DesignerNames, next);

        return HookResult.Continue;
    }

    private void RefreshCount(CCSPlayerController? player, string item)
    {
        int slot = player?.Slot ?? -1;
        if (slot < 0 || slot >= 64 || !Active(player))
            return;

        var remaining = _remaining[slot];
        if (remaining == null)
            return;

        var def = Grenades.FirstOrDefault(d => d.ThrownNames.Contains(item));
        if (def == null || !remaining.TryGetValue(def.ThrowName, out int pending) || pending <= 1)
            return;

        UpdateCount(player!, def.DesignerNames, pending);
    }

    private void UpdateCount(CCSPlayerController player, string[] designerNames, int count)
    {
        var weapon = player.PlayerPawn.Value?.WeaponServices?.MyWeapons
            .Select(h => h.Value)
            .FirstOrDefault(w => w != null && w.IsValid && designerNames.Contains(w.DesignerName));
        if (weapon == null)
            return;

        weapon.Clip1 = count;
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");

        Core.AddTimer(0.1f, () =>
        {
            if (weapon.IsValid)
                weapon.Clip1 = 1;
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }
}
