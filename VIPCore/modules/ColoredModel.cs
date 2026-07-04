using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class ColoredModel : VipModule
{
    public override string Name => "ColoredModel";
    public override string DisplayName => Core.Localizer["vip.module.coloredmodel"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player) =>
        TrailBeam.ParseColorOptions(GroupValue<List<string>>(player) ?? new());

    public override void OnLoad() => Core.RegisterListener<OnTick>(OnTick);

    private void OnTick()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || player.IsBot || !IsAlive(player) || !Active(player))
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            pawn.Render = TrailBeam.Resolve(Setting(player));
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }
    }
}
