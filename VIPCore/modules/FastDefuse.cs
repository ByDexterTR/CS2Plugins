using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class FastDefuse : VipModule
{
    private class Cfg
    {
        public float Time { get; set; } = 1.0f;
        public bool ImmuneWhileBurning { get; set; } = false;
    }

    public override string Name => "FastDefuse";
    public override string DisplayName => Core.Localizer["vip.module.fastdefuse"];

    public override void OnLoad() => Core.RegisterEventHandler<EventBombBegindefuse>(OnDefuse);

    private HookResult OnDefuse(EventBombBegindefuse ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(player!) ?? new Cfg();

        Server.NextFrame(() =>
        {
            foreach (var c4 in Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4"))
            {
                if (c4 == null || !c4.IsValid || !c4.BeingDefused)
                    continue;

                c4.DefuseCountDown = Server.CurrentTime + cfg.Time;
                Utilities.SetStateChanged(c4, "CPlantedC4", "m_flDefuseCountDown");
            }
        });

        return HookResult.Continue;
    }
}
