using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class PlayerSize : VipModule
{
    public override string Name => "PlayerSize";
    public override string DisplayName => Core.Localizer["vip.module.playersize"];
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

    public override void OnSelect(CCSPlayerController player, string value)
    {
        if (value == "off" && IsAlive(player))
            Apply(player, 1f);
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        if (!float.TryParse(Setting(player!), NumberStyles.Float, CultureInfo.InvariantCulture, out float scale) || scale <= 0)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            if (IsAlive(player))
                Apply(player!, scale);
        });

        return HookResult.Continue;
    }

    private static void Apply(CCSPlayerController? player, float scale)
    {
        var pawn = player?.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        var skeleton = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance();
        if (skeleton != null)
        {
            if (Math.Abs(skeleton.Scale - 1f) > 0.01f && Math.Abs(skeleton.Scale - scale) > 0.01f)
                return;
            skeleton.Scale = scale;
        }

        pawn.AcceptInput("SetScale", null, null, scale.ToString(CultureInfo.InvariantCulture));
        Server.NextFrame(() =>
        {
            if (pawn.IsValid)
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
        });
    }
}
