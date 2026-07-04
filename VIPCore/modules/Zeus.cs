using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class Zeus : VipModule
{
    public override string Name => "Zeus";
    public override string DisplayName => Core.Localizer["vip.module.zeus"];

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
