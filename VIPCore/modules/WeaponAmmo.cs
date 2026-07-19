using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class WeaponAmmo : VipModule
{
    private class Entry
    {
        public string WeaponName { get; set; } = "";
        public int Ammo { get; set; }
        public int Reserve { get; set; }
    }

    private class State
    {
        public int Clip;
        public int Reserve;
        public int FullClip;
        public bool ManageClip;
        public bool ManageReserve;
        public bool WasReloading;
    }

    private readonly Dictionary<uint, State> _states = new();
    private static WeaponAmmo? _instance;

    public static bool Manages(uint weaponIndex) => _instance?._states.ContainsKey(weaponIndex) == true;

    public override string Name => "WeaponAmmo";
    public override string DisplayName => Core.Localizer["vip.module.weaponammo"];

    public override void OnLoad()
    {
        _instance = this;
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.RegisterEventHandler<EventItemPickup>(OnPickup);
        Core.RegisterEventHandler<EventWeaponFire>(OnFire);
        Core.RegisterEventHandler<EventRoundStart>((_, __) =>
        {
            _states.Clear();
            return HookResult.Continue;
        });
        Core.RegisterListener<OnTick>(OnTick);
    }

    public override void OnUnload() => _states.Clear();

    public override void OnSelect(CCSPlayerController player, string value)
    {
        if (value == "on")
            ApplyAll(player);
        else
            ResetPlayer(player);
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        Server.NextFrame(() => ApplyAll(player));
        return HookResult.Continue;
    }

    private HookResult OnPickup(EventItemPickup ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player) || !IsAlive(player))
            return HookResult.Continue;

        Server.NextFrame(() => ApplyAll(player));
        return HookResult.Continue;
    }

    private HookResult OnFire(EventWeaponFire ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        var weapon = player!.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (weapon == null || !weapon.IsValid)
            return HookResult.Continue;

        if (_states.TryGetValue(weapon.Index, out var state) && state.ManageClip && state.Clip > 0
            && !InfiniteAmmo.Covers(player!, weapon.DesignerName))
        {
            state.Clip--;
            TryInstantRefill(player!, weapon, state);
        }

        return HookResult.Continue;
    }

    private void TryInstantRefill(CCSPlayerController player, CBasePlayerWeapon weapon, State state)
    {
        if (state.Clip > 1 || state.Clip >= state.FullClip)
            return;

        if (!FastReload.WantsInstant(player, WeaponName(weapon)))
            return;

        if (state.ManageReserve)
        {
            if (state.Reserve <= 0)
                return;

            state.Reserve--;
            if (weapon.ReserveAmmo.Length > 0)
            {
                weapon.ReserveAmmo[0] = state.Reserve;
                Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
            }
        }
        else
        {
            if (weapon.ReserveAmmo.Length == 0 || weapon.ReserveAmmo[0] <= 0)
                return;

            int reserve = weapon.ReserveAmmo[0];
            var vdata = weapon.As<CCSWeaponBase>().VData;
            if (vdata?.ReserveAmmoAsClips == true)
                weapon.ReserveAmmo[0] = reserve - 1;
            else
                weapon.ReserveAmmo[0] = reserve - Math.Min(state.FullClip, reserve);
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
        }

        state.Clip = state.FullClip;
        weapon.Clip1 = state.Clip;
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
    }

    private static string? WeaponName(CBasePlayerWeapon weapon)
    {
        if (string.IsNullOrEmpty(weapon.DesignerName))
            return null;

        int itemDef = 0;
        try { itemDef = weapon.AttributeManager.Item.ItemDefinitionIndex; }
        catch { }

        return WeaponUtil.NormalizeWeaponName(weapon.DesignerName, itemDef);
    }

    private void ApplyAll(CCSPlayerController? player)
    {
        if (!IsAlive(player) || !Active(player))
            return;

        var entries = GroupValue<List<Entry>>(player!) ?? new();
        if (entries.Count == 0)
            return;

        var weapons = player!.PlayerPawn.Value?.WeaponServices?.MyWeapons;
        if (weapons == null)
            return;

        foreach (var handle in weapons)
        {
            var weapon = handle.Value;
            if (weapon == null || !weapon.IsValid)
                continue;

            string? name = WeaponName(weapon);
            var entry = name == null ? null : entries.FirstOrDefault(e => e.WeaponName == name);
            if (entry == null)
                continue;

            if (InfiniteAmmo.Covers(player, weapon.DesignerName))
            {
                _states.Remove(weapon.Index);
                continue;
            }

            var state = new State
            {
                ManageClip = entry.Ammo > 0,
                ManageReserve = entry.Reserve >= 0 && weapon.ReserveAmmo.Length > 0,
                FullClip = entry.Ammo,
                Clip = entry.Ammo > 0 ? entry.Ammo : weapon.Clip1,
                Reserve = entry.Reserve >= 0 && weapon.ReserveAmmo.Length > 0 ? entry.Reserve : 0
            };
            _states[weapon.Index] = state;

            if (state.ManageClip && weapon.Clip1 != state.Clip)
            {
                weapon.Clip1 = state.Clip;
                Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
            }

            if (state.ManageReserve && weapon.ReserveAmmo[0] != state.Reserve)
            {
                weapon.ReserveAmmo[0] = state.Reserve;
                Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
            }
        }
    }

    private readonly List<uint> _deadStates = new();

    private void OnTick()
    {
        if (_states.Count == 0)
            return;

        _deadStates.Clear();

        foreach (var (index, state) in _states)
        {
            var weapon = Utilities.GetEntityFromIndex<CBasePlayerWeapon>((int)index);
            if (weapon == null || !weapon.IsValid)
            {
                _deadStates.Add(index);
                continue;
            }

            var owner = PawnController(weapon.OwnerEntity.Value);
            if (owner == null || owner.IsBot || !IsAlive(owner) || !Active(owner))
                continue;

            if (InfiniteAmmo.Covers(owner, weapon.DesignerName))
                continue;

            bool inReload = weapon.As<CCSWeaponBase>().InReload;

            if (state.WasReloading && !inReload && state.ManageClip && state.Clip < state.FullClip)
            {
                if (!state.ManageReserve || state.Reserve > 0)
                {
                    state.Clip = state.FullClip;
                    if (state.ManageReserve)
                        state.Reserve--;
                }
            }
            state.WasReloading = inReload;

            if (!inReload && state.ManageClip && weapon.Clip1 != state.Clip)
            {
                weapon.Clip1 = state.Clip;
                Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
            }

            if (state.ManageReserve && weapon.ReserveAmmo.Length > 0 && weapon.ReserveAmmo[0] != state.Reserve)
            {
                weapon.ReserveAmmo[0] = state.Reserve;
                Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
            }
        }

        foreach (var index in _deadStates)
            _states.Remove(index);
    }

    private void ResetPlayer(CCSPlayerController? player)
    {
        var weapons = player?.PlayerPawn.Value?.WeaponServices?.MyWeapons;
        if (weapons == null)
            return;

        foreach (var handle in weapons)
        {
            var weapon = handle.Value;
            if (weapon == null || !weapon.IsValid)
                continue;

            _states.Remove(weapon.Index);

            var vdata = weapon.As<CCSWeaponBase>().VData;
            if (vdata == null)
                continue;

            if (vdata.MaxClip1 > 0 && weapon.Clip1 > vdata.MaxClip1)
            {
                weapon.Clip1 = vdata.MaxClip1;
                Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
            }

            if (weapon.ReserveAmmo.Length > 0 && weapon.ReserveAmmo[0] > vdata.PrimaryReserveAmmoMax)
            {
                weapon.ReserveAmmo[0] = vdata.PrimaryReserveAmmoMax;
                Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
            }
        }
    }
}
