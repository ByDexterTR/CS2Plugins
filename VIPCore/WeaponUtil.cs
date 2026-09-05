namespace VIPCore;

public static class WeaponUtil
{
    public static readonly IReadOnlyDictionary<int, string> KnifeNames = new Dictionary<int, string>
    {
        [500] = "weapon_bayonet",
        [503] = "weapon_knife_css",
        [505] = "weapon_knife_flip",
        [506] = "weapon_knife_gut",
        [507] = "weapon_knife_karambit",
        [508] = "weapon_knife_m9_bayonet",
        [509] = "weapon_knife_tactical",
        [512] = "weapon_knife_falchion",
        [514] = "weapon_knife_survival_bowie",
        [515] = "weapon_knife_butterfly",
        [516] = "weapon_knife_push",
        [517] = "weapon_knife_cord",
        [518] = "weapon_knife_canis",
        [519] = "weapon_knife_ursus",
        [520] = "weapon_knife_gypsy_jackknife",
        [521] = "weapon_knife_outdoor",
        [522] = "weapon_knife_stiletto",
        [523] = "weapon_knife_widowmaker",
        [525] = "weapon_knife_skeleton",
        [526] = "weapon_knife_kukri"
    };

    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["weapon_ak47"] = "AK-47",
        ["weapon_m4a1"] = "M4A4",
        ["weapon_m4a1_silencer"] = "M4A1-S",
        ["weapon_awp"] = "AWP",
        ["weapon_ssg08"] = "SSG 08",
        ["weapon_scar20"] = "SCAR-20",
        ["weapon_g3sg1"] = "G3SG1",
        ["weapon_aug"] = "AUG",
        ["weapon_sg556"] = "SG 553",
        ["weapon_famas"] = "FAMAS",
        ["weapon_galilar"] = "Galil AR",
        ["weapon_deagle"] = "Desert Eagle",
        ["weapon_revolver"] = "R8 Revolver",
        ["weapon_glock"] = "Glock-18",
        ["weapon_usp_silencer"] = "USP-S",
        ["weapon_hkp2000"] = "P2000",
        ["weapon_p250"] = "P250",
        ["weapon_cz75a"] = "CZ75-Auto",
        ["weapon_fiveseven"] = "Five-SeveN",
        ["weapon_tec9"] = "Tec-9",
        ["weapon_elite"] = "Dual Berettas",
        ["weapon_mac10"] = "MAC-10",
        ["weapon_mp9"] = "MP9",
        ["weapon_mp7"] = "MP7",
        ["weapon_mp5sd"] = "MP5-SD",
        ["weapon_ump45"] = "UMP-45",
        ["weapon_p90"] = "P90",
        ["weapon_bizon"] = "PP-Bizon",
        ["weapon_nova"] = "Nova",
        ["weapon_xm1014"] = "XM1014",
        ["weapon_sawedoff"] = "Sawed-Off",
        ["weapon_mag7"] = "MAG-7",
        ["weapon_m249"] = "M249",
        ["weapon_negev"] = "Negev",
        ["weapon_taser"] = "Zeus x27",
        ["weapon_hegrenade"] = "HE Grenade",
        ["weapon_flashbang"] = "Flashbang",
        ["weapon_smokegrenade"] = "Smoke Grenade",
        ["weapon_molotov"] = "Molotov",
        ["weapon_incgrenade"] = "Incendiary",
        ["weapon_decoy"] = "Decoy",
        ["weapon_c4"] = "C4",
        ["weapon_healthshot"] = "Healthshot",
        ["weapon_knife"] = "Knife",
        ["weapon_bayonet"] = "Bayonet",
        ["weapon_knife_css"] = "Classic Knife",
        ["weapon_knife_flip"] = "Flip Knife",
        ["weapon_knife_gut"] = "Gut Knife",
        ["weapon_knife_karambit"] = "Karambit",
        ["weapon_knife_m9_bayonet"] = "M9 Bayonet",
        ["weapon_knife_tactical"] = "Huntsman Knife",
        ["weapon_knife_falchion"] = "Falchion Knife",
        ["weapon_knife_survival_bowie"] = "Bowie Knife",
        ["weapon_knife_butterfly"] = "Butterfly Knife",
        ["weapon_knife_push"] = "Shadow Daggers",
        ["weapon_knife_cord"] = "Paracord Knife",
        ["weapon_knife_canis"] = "Survival Knife",
        ["weapon_knife_ursus"] = "Ursus Knife",
        ["weapon_knife_gypsy_jackknife"] = "Navaja Knife",
        ["weapon_knife_outdoor"] = "Nomad Knife",
        ["weapon_knife_stiletto"] = "Stiletto Knife",
        ["weapon_knife_widowmaker"] = "Talon Knife",
        ["weapon_knife_skeleton"] = "Skeleton Knife",
        ["weapon_knife_kukri"] = "Kukri Knife"
    };

    public static string Label(string weaponName)
    {
        if (Labels.TryGetValue(weaponName, out string? label))
            return label;

        string trimmed = weaponName.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase)
            ? weaponName[7..]
            : weaponName;

        return trimmed.Length > 0 ? char.ToUpperInvariant(trimmed[0]) + trimmed[1..] : weaponName;
    }

    public static bool IsKnife(string? name) =>
        !string.IsNullOrEmpty(name) &&
        (name.Contains("knife", StringComparison.OrdinalIgnoreCase) ||
         name.Contains("bayonet", StringComparison.OrdinalIgnoreCase));

    public static bool Matches(string configName, string? actual)
    {
        if (string.IsNullOrEmpty(actual))
            return false;

        if (configName.Equals("weapon_knife", StringComparison.OrdinalIgnoreCase))
            return IsKnife(actual);

        return configName.Equals(actual, StringComparison.OrdinalIgnoreCase);
    }

    public static bool MatchesAny(IEnumerable<string> names, string? actual) =>
        names.Any(n => Matches(n, actual));

    public static List<string> ParseCsv(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? new()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    public static string NormalizeWeaponName(string designerName, int itemDef)
    {
        if (IsKnife(designerName))
            return KnifeNames.TryGetValue(itemDef, out string? knife) ? knife : "weapon_knife";

        return (designerName, itemDef) switch
        {
            ("weapon_m4a1", 60) => "weapon_m4a1_silencer",
            ("weapon_hkp2000", 61) => "weapon_usp_silencer",
            ("weapon_deagle", 64) => "weapon_revolver",
            ("weapon_mp7", 23) => "weapon_mp5sd",
            _ => designerName
        };
    }
}
