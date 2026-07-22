using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace VIPCore;

public class ExtraMoney : VipModule
{
    private class Cfg
    {
        public int Amount { get; set; } = 0;
    }

    private static ConVar? _cvMaxMoney;

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
            if (!IsAlive(player))
                return;

            var money = player!.InGameMoneyServices;
            if (money == null)
                return;

            _cvMaxMoney ??= ConVar.Find("mp_maxmoney");
            int maxMoney = _cvMaxMoney?.GetPrimitiveValue<int>() ?? 16000;
            int target = Math.Clamp(money.Account + cfg.Amount, 0, maxMoney);
            if (target == money.Account)
                return;

            money.Account = target;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        });

        return HookResult.Continue;
    }
}
