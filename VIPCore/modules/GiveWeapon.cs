using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class GiveWeapon : VipModule
{
    public override string Name => "GiveWeapon";
    public override string DisplayName => Core.Localizer["vip.module.giveweapon"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var weapons = GroupValue<List<string>>(player) ?? new();
        return weapons.Select(w => new VipFeatureOption(Clean(w), w)).ToList();
    }

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        string value = Setting(player!);
        if (value == "off")
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            if (IsAlive(player) && !HasWeapon(player, value))
                player!.GiveNamedItem(value);
        });

        return HookResult.Continue;
    }

    private static string Clean(string weapon) =>
        weapon.StartsWith("weapon_") ? weapon["weapon_".Length..] : weapon;
}
