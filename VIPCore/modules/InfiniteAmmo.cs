using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class InfiniteAmmo : VipModule
{
    private class Cfg
    {
        public string OnlyWeapon { get; set; } = "";

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWeapon);
    }

    private static readonly Cfg DefaultCfg = new();

    private static InfiniteAmmo? _instance;

    public static bool Covers(CCSPlayerController player, string? weaponName)
    {
        var self = _instance;
        if (self == null || !self.Active(player))
            return false;

        var allow = (self.GroupValue<Cfg>(player) ?? DefaultCfg).Allow;
        return allow.Count == 0 || (weaponName != null && WeaponUtil.MatchesAny(allow, weaponName));
    }

    public override string Name => "InfiniteAmmo";
    public override string DisplayName => Core.Localizer["vip.module.infiniteammo"];

    public override void OnLoad()
    {
        _instance = this;
        Core.RegisterEventHandler<EventWeaponFire>(OnFire);
    }

    private HookResult OnFire(EventWeaponFire ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        var weapon = player!.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (weapon == null || !weapon.IsValid)
            return HookResult.Continue;

        string name = weapon.DesignerName;
        if (name.Contains("knife") || name.Contains("bayonet") || name.Contains("taser"))
            return HookResult.Continue;

        var allow = (GroupValue<Cfg>(player) ?? DefaultCfg).Allow;
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, name))
            return HookResult.Continue;

        int maxClip = weapon.As<CCSWeaponBase>().VData?.MaxClip1 ?? 0;
        if (maxClip <= 0)
            return HookResult.Continue;

        weapon.Clip1 = maxClip;
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
        return HookResult.Continue;
    }
}
