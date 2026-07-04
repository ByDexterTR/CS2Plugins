using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class ExtraHP : VipModule
{
    public override string Name => "ExtraHP";
    public override string DisplayName => Core.Localizer["vip.module.extrahp"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        int hp = GroupValue<int>(player!);
        if (hp <= 0)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            if (!IsAlive(player))
                return;

            var pawn = player!.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                return;

            pawn.MaxHealth = hp;
            pawn.Health = hp;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        });

        return HookResult.Continue;
    }
}
