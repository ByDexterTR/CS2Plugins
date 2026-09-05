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
        public List<ParticleEntry> Particles { get; set; } = new();

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWithWeapon);
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "BulletTrail";
    public override string DisplayName => Core.Localizer["vip.module.bullettrail"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
        var options = TrailBeam.ParseColorOptions(cfg.Colors);
        ParticleTrail.AddOptions(options, cfg.Particles);
        return options;
    }

    public override void OnLoad()
    {
        EffectHide.Ensure(Core);
        Core.RegisterEventHandler<EventBulletImpact>(OnImpact);
        Core.HookPrecache(manifest =>
        {
            foreach (var cfg in Core.GetAllGroupValues<Cfg>(Name))
                ParticleTrail.Precache(manifest, cfg.Particles);
        });
    }

    private HookResult OnImpact(EventBulletImpact ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        var pawn = player!.PlayerPawn.Value;
        if (pawn == null || pawn.AbsOrigin == null)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
        var allow = cfg.Allow;
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(player)))
            return HookResult.Continue;

        var origin = pawn.AbsOrigin;
        var eye = new Vector(origin.X, origin.Y, origin.Z + pawn.ViewOffset.Z);
        var impact = new Vector(ev.X, ev.Y, ev.Z);

        string setting = Setting(player);

        var entry = ParticleTrail.Find(cfg.Particles, setting);
        if (entry != null)
        {
            ParticleTrail.Tracer(Core, eye, impact, entry, cfg.Lifetime, EffectHide.BulletTrail, player.Slot);
            return HookResult.Continue;
        }

        var color = TrailBeam.IsRandom(setting) ? Core.RoundColor(player.Slot) : TrailBeam.Resolve(setting);
        TrailBeam.Create(Core, eye, impact, color, cfg.Width, cfg.Lifetime, EffectHide.BulletTrail, player.Slot);
        return HookResult.Continue;
    }
}
