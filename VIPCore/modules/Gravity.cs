using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class Gravity : VipModule
{
    public override string Name => "Gravity";
    public override string DisplayName => Core.Localizer["vip.module.gravity"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var values = GroupValue<List<double>>(player) ?? new();
        return values.Select(v =>
        {
            string s = v.ToString(CultureInfo.InvariantCulture);
            return new VipFeatureOption($"x{s}", s);
        }).ToList();
    }

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);

    public override void OnSelect(CCSPlayerController player, string value) => Apply(player, value);

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        string value = Setting(player!);
        Server.NextFrame(() => Apply(player!, value));
        return HookResult.Continue;
    }

    private static void Apply(CCSPlayerController player, string value)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        double gravity = value != "off" && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var g) ? g : 1.0;
        pawn.GravityScale = (float)gravity;
    }
}
