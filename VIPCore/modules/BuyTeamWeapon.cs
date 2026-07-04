using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace VIPCore;

public class BuyTeamWeapon : VipModule
{
    private static readonly Dictionary<string, (string Entity, int Team)> Weapons = new()
    {
        ["galil"] = ("weapon_galilar", 3),
        ["ak47"] = ("weapon_ak47", 3),
        ["sg553"] = ("weapon_sg556", 3),
        ["glock"] = ("weapon_glock", 3),
        ["mac10"] = ("weapon_mac10", 3),
        ["tec9"] = ("weapon_tec9", 3),
        ["sawedoff"] = ("weapon_sawedoff", 3),
        ["g3sg1"] = ("weapon_g3sg1", 3),
        ["p2000"] = ("weapon_hkp2000", 2),
        ["mag7"] = ("weapon_mag7", 2),
        ["fiveseven"] = ("weapon_fiveseven", 2),
        ["famas"] = ("weapon_famas", 2),
        ["m4a1"] = ("weapon_m4a1_silencer", 2),
        ["m4a4"] = ("weapon_m4a1", 2),
        ["aug"] = ("weapon_aug", 2),
        ["scar20"] = ("weapon_scar20", 2),
        ["mp9"] = ("weapon_mp9", 2),
        ["usp"] = ("weapon_usp_silencer", 2)
    };

    public override string Name => "BuyTeamWeapon";
    public override string DisplayName => Core.Localizer["vip.module.buyteamweapon"];
    public override bool ShowInMenu => false;

    public override void OnLoad()
    {
        foreach (var key in Weapons.Keys)
        {
            string cmd = key;
            Core.AddCommand($"css_{cmd}", "Karsi takim silahi alir", (player, info) => Buy(player, cmd, info));
        }
    }

    private void Buy(CCSPlayerController? player, string cmd, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        if (!Granted(player))
        {
            Reply(info, Core.Localizer["vip.no_access"]);
            return;
        }

        var (entity, team) = Weapons[cmd];
        if (player.TeamNum != team)
        {
            Reply(info, Core.Localizer["vip.buy_wrong_team"]);
            return;
        }

        var perms = GroupValue<Dictionary<string, bool>>(player);
        if (perms != null && perms.Count > 0)
        {
            bool allowed = (perms.TryGetValue("css_" + cmd, out var v1) && v1)
                || (perms.TryGetValue(cmd, out var v2) && v2);
            if (!allowed)
            {
                Reply(info, Core.Localizer["vip.buy_not_allowed"]);
                return;
            }
        }

        if (!IsAlive(player))
            return;

        player.GiveNamedItem(entity);
        Reply(info, Core.Localizer["vip.buy_given", cmd]);
    }

    private void Reply(CommandInfo info, string message) =>
        info.ReplyToCommand($" {CC.Orchid}{Core.Localizer["chat_prefix"]}{CC.Default} {message}");
}
