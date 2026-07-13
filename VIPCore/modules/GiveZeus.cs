using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class GiveZeus : VipModule
{
    public override string Name => "GiveZeus";
    public override string DisplayName => Core.Localizer["vip.module.givezeus"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);

    public override void OnSelect(CCSPlayerController player, string value)
    {
        if (value == "on" && IsAlive(player) && !HasWeapon(player, "weapon_taser"))
            player.GiveNamedItem("weapon_taser");
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            if (IsAlive(player) && !HasWeapon(player, "weapon_taser"))
                player!.GiveNamedItem("weapon_taser");
        });

        return HookResult.Continue;
    }
}
