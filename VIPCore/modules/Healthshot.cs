using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

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
            if (!IsAlive(player) || HasWeapon(player, "weapon_healthshot"))
                return;

            int limit = Limit();
            int give = limit > 0 ? Math.Min(count, limit) : count;

            for (int i = 0; i < give; i++)
                player!.GiveNamedItem("weapon_healthshot");
        });

        return HookResult.Continue;
    }

    private static int Limit()
    {
        try { return ConVar.Find("ammo_item_limit_healthshot")?.GetPrimitiveValue<int>() ?? 0; }
        catch { return 0; }
    }
}
