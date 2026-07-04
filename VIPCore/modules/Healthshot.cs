using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class Healthshot : VipModule
{
    public override string Name => "Healthshot";
    public override string DisplayName => Core.Localizer["vip.module.healthshot"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        int count = GroupValue<int>(player!);
        if (count <= 0)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            if (!IsAlive(player))
                return;

            for (int i = CountWeapon(player, "weapon_healthshot"); i < count; i++)
                player!.GiveNamedItem("weapon_healthshot");
        });

        return HookResult.Continue;
    }
}
