using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class DefuseKit : VipModule
{
    public override string Name => "DefuseKit";
    public override string DisplayName => Core.Localizer["vip.module.defusekit"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player) || player!.TeamNum != 3)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            if (IsAlive(player))
                player!.GiveNamedItem("item_defuser");
        });

        return HookResult.Continue;
    }
}
