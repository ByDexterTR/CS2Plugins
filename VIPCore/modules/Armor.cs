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

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.RegisterEventHandler<EventItemPurchase>(OnPurchase);
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        Server.NextFrame(() => Apply(player!, false));
        return HookResult.Continue;
    }

    private HookResult OnPurchase(EventItemPurchase ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        string item = ev.Weapon ?? "";
        if (item != "item_kevlar" && item != "item_assaultsuit")
            return HookResult.Continue;

        Server.NextFrame(() => Apply(player!, true));
        return HookResult.Continue;
    }

    private void Apply(CCSPlayerController player, bool keepHigher)
    {
        if (!IsAlive(player) || !Active(player))
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        var cfg = GroupValue<Cfg>(player) ?? new Cfg();

        player.GiveNamedItem(cfg.Helmet ? "item_assaultsuit" : "item_kevlar");

        int value = keepHigher ? Math.Max(cfg.Value, pawn.ArmorValue) : cfg.Value;
        pawn.ArmorValue = value;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
    }
}
