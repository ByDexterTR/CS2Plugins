using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class BulletTrail : VipModule
{
    private class Cfg
    {
        public float Width { get; set; } = 1.0f;
        public float Lifetime { get; set; } = 0.6f;
        public string OnlyWithWeapon { get; set; } = "";
        public List<string> Colors { get; set; } = new();
    }

    public override string Name => "BulletTrail";
    public override string DisplayName => Core.Localizer["vip.module.bullettrail"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player) =>
        TrailBeam.ParseColorOptions(GroupValue<Cfg>(player)?.Colors ?? new());

    public override void OnLoad() => Core.RegisterEventHandler<EventBulletImpact>(OnImpact);

    private HookResult OnImpact(EventBulletImpact ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        var pawn = player!.PlayerPawn.Value;
        if (pawn == null || pawn.AbsOrigin == null)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        var allow = WeaponUtil.ParseCsv(cfg.OnlyWithWeapon);
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(player)))
            return HookResult.Continue;

        var origin = pawn.AbsOrigin;
        var eye = new Vector(origin.X, origin.Y, origin.Z + pawn.ViewOffset.Z);
        var impact = new Vector(ev.X, ev.Y, ev.Z);

        TrailBeam.Create(Core, eye, impact, TrailBeam.Resolve(Setting(player)), cfg.Width, cfg.Lifetime);
        return HookResult.Continue;
    }
}
