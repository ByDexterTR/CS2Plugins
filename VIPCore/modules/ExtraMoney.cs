using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class ExtraMoney : VipModule
{
    private class Cfg
    {
        public int Amount { get; set; } = 0;
    }

    public override string Name => "ExtraMoney";
    public override string DisplayName => Core.Localizer["vip.module.extramoney"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(player!) ?? new Cfg();
        if (cfg.Amount == 0)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            if (player == null || !player.IsValid)
                return;

            var money = player.InGameMoneyServices;
            if (money == null)
                return;

            money.Account += cfg.Amount;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        });

        return HookResult.Continue;
    }
}
