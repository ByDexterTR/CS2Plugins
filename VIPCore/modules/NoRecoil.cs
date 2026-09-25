using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class NoRecoil : VipModule
{
    private class Cfg
    {
        public string OnlyWithWeapon { get; set; } = "";

        public int OnlyStance { get; set; } = 0;

        [JsonPropertyName("recoilpercent")]
        public float RecoilPercent { get; set; } = 0f;

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWithWeapon);
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "NoRecoil";
    public override string DisplayName => Core.Localizer["vip.module.norecoil"];

    public override void OnLoad()
    {
        RecoilUtil.Ensure(Core);
        RecoilUtil.Provide(Keep);
    }

    public override void OnUnload() => RecoilUtil.Withdraw(Keep);

    private float Keep(CCSPlayerController player, CCSPlayerPawn pawn)
    {
        if (!Active(player))
            return 1f;

        var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;

        var allow = cfg.Allow;
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(player)))
            return 1f;

        if (!StanceFilter.Matches(cfg.OnlyStance, pawn))
            return 1f;

        return Math.Clamp(cfg.RecoilPercent, 0f, 1f);
    }
}
