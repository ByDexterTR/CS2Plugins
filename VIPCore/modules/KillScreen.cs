using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace VIPCore;

public class KillScreen : VipModule
{
    private class Cfg
    {
        public float Duration { get; set; } = 0.05f;
        public float Fade { get; set; } = 0.35f;
        public int Alpha { get; set; } = 90;
        public List<string> Colors { get; set; } = new();
    }

    private static ConVar? _cvFfa;

    public override string Name => "KillScreen";
    public override string DisplayName => Core.Localizer["vip.module.killscreen"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player) =>
        TrailBeam.ParseColorOptions(GroupValue<Cfg>(player)?.Colors ?? new());

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerDeath>(OnDeath);

    private HookResult OnDeath(EventPlayerDeath ev, GameEventInfo info)
    {
        var attacker = ev.Attacker;
        var victim = ev.Userid;
        if (!Active(attacker) || victim == null || !victim.IsValid || victim.Slot == attacker!.Slot)
            return HookResult.Continue;

        _cvFfa ??= ConVar.Find("mp_teammates_are_enemies");
        bool ffa = _cvFfa?.GetPrimitiveValue<bool>() ?? false;
        if (!ffa && victim.Team == attacker.Team)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker) ?? new Cfg();
        string setting = Setting(attacker);
        var color = TrailBeam.IsRandom(setting) ? TrailBeam.RandomColor() : TrailBeam.Resolve(setting);

        int alpha = Math.Clamp(cfg.Alpha, 0, 255);
        ScreenFade.Apply(attacker, System.Drawing.Color.FromArgb(alpha, color.R, color.G, color.B), cfg.Fade, cfg.Duration);
        return HookResult.Continue;
    }
}
