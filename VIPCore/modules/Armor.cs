using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class Armor : VipModule
{
    private class Cfg
    {
        public int Value { get; set; } = 100;
        public bool Helmet { get; set; } = true;
    }

    public override string Name => "Armor";
    public override string DisplayName => Core.Localizer["vip.module.armor"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(player!) ?? new Cfg();
        Server.NextFrame(() =>
        {
            if (!IsAlive(player))
                return;

            var pawn = player!.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                return;

            player.GiveNamedItem(cfg.Helmet ? "item_assaultsuit" : "item_kevlar");
            pawn.ArmorValue = cfg.Value;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
        });

        return HookResult.Continue;
    }
}
