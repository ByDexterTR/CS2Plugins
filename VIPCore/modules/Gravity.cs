using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class Gravity : VipModule
{
    private readonly bool[] _applied = new bool[64];

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
            float gravity = 1f;
            bool wants = false;

            if (IsAlive(player) && Active(player))
            {
                string value = Setting(player);
                if (value != "off" && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var g) && g > 0)
                {
                    gravity = (float)g;
                    wants = true;
                }
            }

            if (!wants)
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
            if (Math.Abs(pawn.ActualGravityScale - gravity) > 0.001f)
                pawn.ActualGravityScale = gravity;
        }
    }

    private static void Reset(int slot)
    {
        var pawn = Utilities.GetPlayerFromSlot(slot)?.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        pawn.ActualGravityScale = 1f;
    }
}
