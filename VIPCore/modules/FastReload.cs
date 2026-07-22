using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class FastReload : VipModule
{
    private class Cfg
    {
        public string OnlyWithWeapon { get; set; } = "";

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWithWeapon);
    }

    private static readonly Cfg DefaultCfg = new();

    private static FastReload? _instance;

    public static bool WantsInstant(CCSPlayerController player, string? weaponName)
    {
        var self = _instance;
        if (self == null || !self.Active(player))
            return false;

        var allow = (self.GroupValue<Cfg>(player) ?? DefaultCfg).Allow;
        return allow.Count == 0 || (weaponName != null && WeaponUtil.MatchesAny(allow, weaponName));
    }

    public override string Name => "FastReload";
    public override string DisplayName => Core.Localizer["vip.module.fastreload"];

    public override void OnLoad()
    {
        _instance = this;
        Core.RegisterEventHandler<EventWeaponFire>(OnFire);
    }

    private HookResult OnFire(EventWeaponFire ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!IsAlive(player) || !Active(player))
            return HookResult.Continue;

        var weapon = player!.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (weapon == null || !weapon.IsValid || WeaponAmmo.Manages(weapon.Index))
            return HookResult.Continue;

        var vdata = weapon.As<CCSWeaponBase>().VData;
        if (vdata == null || vdata.MaxClip1 <= 1 || weapon.Clip1 > 1)
            return HookResult.Continue;

        if (weapon.ReserveAmmo.Length == 0 || weapon.ReserveAmmo[0] <= 0)
            return HookResult.Continue;

        var allow = (GroupValue<Cfg>(player) ?? DefaultCfg).Allow;
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(player)))
            return HookResult.Continue;

        int reserve = weapon.ReserveAmmo[0];

        if (vdata.ReserveAmmoAsClips)
        {
            weapon.Clip1 = vdata.MaxClip1;
            weapon.ReserveAmmo[0] = reserve - 1;
        }
        else
        {
            int take = Math.Min(vdata.MaxClip1, reserve);
            weapon.Clip1 = take;
            weapon.ReserveAmmo[0] = reserve - take;
        }

        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
        return HookResult.Continue;
    }
}
