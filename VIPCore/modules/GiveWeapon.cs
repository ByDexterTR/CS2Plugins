using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class GiveWeapon : VipModule
{
    private static readonly HashSet<string> Pistols = new()
    {
        "weapon_glock", "weapon_hkp2000", "weapon_usp_silencer", "weapon_p250", "weapon_elite",
        "weapon_fiveseven", "weapon_tec9", "weapon_cz75a", "weapon_deagle", "weapon_revolver"
    };

    private static readonly HashSet<string> Utility = new()
    {
        "weapon_knife", "weapon_taser", "weapon_c4", "weapon_flashbang", "weapon_smokegrenade",
        "weapon_hegrenade", "weapon_molotov", "weapon_incgrenade", "weapon_decoy", "weapon_healthshot"
    };

    public override string Name => "GiveWeapon";
    public override string DisplayName => Core.Localizer["vip.module.giveweapon"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    private Dictionary<string, List<string>> Categories(CCSPlayerController player)
    {
        var dict = GroupValue<Dictionary<string, List<string>>>(player);
        if (dict != null && dict.Count > 0)
            return dict;

        var list = GroupValue<List<string>>(player);
        if (list != null && list.Count > 0)
            return new Dictionary<string, List<string>> { ["Silah"] = list };

        return new Dictionary<string, List<string>>();
    }

    private string CategoryDisplay(string key) => key.ToLowerInvariant() switch
    {
        "rifle" => Core.Localizer["vip.giveweapon.rifle"],
        "pistol" => Core.Localizer["vip.giveweapon.pistol"],
        _ => key
    };

    public override List<VipFeatureOption> SelectCategories(CCSPlayerController player)
    {
        var cats = Categories(player).Keys.Select(k => new VipFeatureOption(CategoryDisplay(k), k)).ToList();
        cats.Add(new VipFeatureOption(Core.Localizer["vip.giveweapon.force"], "force"));
        return cats;
    }

    public override List<VipFeatureOption> CategoryOptions(CCSPlayerController player, string category)
    {
        if (category == "force")
            return new() { new VipFeatureOption(Core.Localizer["vip.option_on"], "on") };

        var cats = Categories(player);
        if (!cats.TryGetValue(category, out var weapons))
            return new();

        return weapons.Select(w => new VipFeatureOption(Clean(w), w)).ToList();
    }

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        var give = new List<string>();
        foreach (var category in Categories(player!).Keys)
        {
            string value = CategorySetting(player!, category);
            if (value != "off" && value.Length > 0)
                give.Add(value);
        }

        if (give.Count == 0)
            return HookResult.Continue;

        bool force = CategorySetting(player!, "force") == "on";

        Server.NextFrame(() =>
        {
            if (!IsAlive(player))
                return;

            foreach (var weapon in give)
                GiveOne(player!, weapon, force);
        });

        return HookResult.Continue;
    }

    private static void GiveOne(CCSPlayerController player, string weaponName, bool force)
    {
        bool targetPistol = Pistols.Contains(weaponName);
        var occupied = SlotWeapons(player, targetPistol);

        if (occupied.Any(w => w.Name == weaponName))
            return;

        if (occupied.Count > 0)
        {
            if (!force)
                return;

            foreach (var (weapon, _) in occupied)
                weapon.Remove();
        }

        player.GiveNamedItem(weaponName);
    }

    private static List<(CBasePlayerWeapon Weapon, string Name)> SlotWeapons(CCSPlayerController player, bool pistol)
    {
        var result = new List<(CBasePlayerWeapon, string)>();
        var weapons = player.PlayerPawn.Value?.WeaponServices?.MyWeapons;
        if (weapons == null)
            return result;

        foreach (var handle in weapons)
        {
            var weapon = handle.Value;
            if (weapon == null || !weapon.IsValid || string.IsNullOrEmpty(weapon.DesignerName))
                continue;

            int itemDef = 0;
            try { itemDef = weapon.AttributeManager.Item.ItemDefinitionIndex; }
            catch { }

            string name = WeaponUtil.NormalizeWeaponName(weapon.DesignerName, itemDef);
            if (Utility.Contains(name) || name.Contains("knife") || name.Contains("bayonet"))
                continue;

            if (Pistols.Contains(name) == pistol)
                result.Add((weapon, name));
        }
        return result;
    }

    private static string Clean(string weapon) =>
        weapon.StartsWith("weapon_") ? weapon["weapon_".Length..] : weapon;
}
