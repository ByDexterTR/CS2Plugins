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
    private readonly int[] _lastArgb = new int[64];
    private readonly bool[] _external = new bool[64];

    public override void OnLoad() => Core.RegisterListener<OnTick>(OnTick);

    public override void OnUnload()
    {
        for (int slot = 0; slot < 64; slot++)
            if (_applied[slot])
                Reset(slot);
    }

    private void OnTick()
    {
        foreach (var player in Core.Players)
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
                _external[slot] = false;
                continue;
            }

            if (_external[slot])
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            var current = pawn.Render;
            if (_applied[slot] && current.ToArgb() != _lastArgb[slot])
            {
                _external[slot] = true;
                _applied[slot] = false;
                continue;
            }

            string setting = Setting(player);
            var color = TrailBeam.IsRandom(setting) ? Core.RoundColor(slot) : TrailBeam.Resolve(setting);
            int alpha = PlayerModel.LegsHidden(slot) ? 254 : 255;
            var target = System.Drawing.Color.FromArgb(alpha, color.R, color.G, color.B);

            _applied[slot] = true;
            if (current.ToArgb() == target.ToArgb())
            {
                _lastArgb[slot] = target.ToArgb();
                continue;
            }

            pawn.Render = target;
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            _lastArgb[slot] = target.ToArgb();
        }
    }

    private static void Reset(int slot)
    {
        var pawn = Utilities.GetPlayerFromSlot(slot)?.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        int alpha = PlayerModel.LegsHidden(slot) ? 254 : 255;
        pawn.Render = System.Drawing.Color.FromArgb(alpha, 255, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }
}
