using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class Fov : VipModule
{
    public override string Name => "Fov";
    public override string DisplayName => Core.Localizer["vip.module.fov"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var fovs = GroupValue<List<int>>(player) ?? new();
        return fovs.Where(f => f > 0).Select(f => new VipFeatureOption(f.ToString(), f.ToString())).ToList();
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
        if (player == null || !player.IsValid)
            return;

        uint fov = value != "off" && uint.TryParse(value, out var f) ? f : 0;
        player.DesiredFOV = fov;
        Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
    }
}
