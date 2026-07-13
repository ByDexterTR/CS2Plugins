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

    private readonly bool[] _applied = new bool[64];

    public override void OnLoad() => Core.RegisterListener<OnTick>(OnTick);

    public override void OnUnload()
    {
        for (int slot = 0; slot < 64; slot++)
            if (_applied[slot])
                Reset(slot);
    }

    private void OnTick()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || player.IsBot)
                continue;

            int slot = player.Slot;

            if (!IsAlive(player) || !Active(player))
            {
                if (_applied[slot])
                {
                    _applied[slot] = false;
                    Reset(slot);
                }
                continue;
            }

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            _applied[slot] = true;
            pawn.Render = TrailBeam.Resolve(Setting(player));
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }
    }

    private static void Reset(int slot)
    {
        var pawn = Utilities.GetPlayerFromSlot(slot)?.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        pawn.Render = System.Drawing.Color.FromArgb(255, 255, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }
}
